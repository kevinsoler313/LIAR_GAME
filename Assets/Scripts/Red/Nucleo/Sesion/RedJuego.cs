using System;
using System.Collections.Generic;
using System.Net.Sockets;
using LiarGame.Red.Protocolo;
using LiarGame.Red.Sesion;
using LiarGame.Red.Transporte;
using UnityEngine;

namespace LiarGame.Red
{
    // De quién se acepta un tipo de mensaje
    public enum Origen
    {
        Host,        // solo si lo mandó el host (estado de la partida, sillas, reparto…)
        Servidor,    // solo si lo generó el servidor (alguien entró o salió)
        Cualquiera   // cualquier jugador (por ejemplo, "quiero jugar esta carta": lo valida el host)
    }

    // El punto de entrada a la red para el resto del juego (reemplaza al NetworkManager).
    //   - CrearPartida(): abre el servidor en este equipo y se conecta a él como un jugador más.
    //   - Unirse(): se conecta al servidor de otro equipo.
    //   - Escuchar(tipo, metodo, origen): "cuando llegue un mensaje de este tipo, llama a este método".
    //   - EnviarAlHost / EnviarATodos / EnviarA: mandar mensajes.
    // Vive en la escena Bar: al salir de la escena se destruye y cierra todo solo.
    [DisallowMultipleComponent]
    public class RedJuego : MonoBehaviour
    {
        [SerializeField] bool registrarMensajes = false;   // actívalo para ver en la consola todo lo que llega

        public event Action AlConectarse;
        public event Action<string> AlDesconectarse;   // con el motivo

        public bool Conectado { get; private set; }
        public bool EsHost { get; private set; }
        public int MiId { get; private set; } = -1;
        public int IdHost { get; private set; } = -1;

        SalaServidor sala;          // solo existe en el host
        ClienteWebSocket cliente;

        sealed class Suscripcion
        {
            public Action<Mensaje> Metodo;
            public Origen Origen;
        }
        readonly Dictionary<string, List<Suscripcion>> suscripciones = new Dictionary<string, List<Suscripcion>>();

        // ---------------------------------------------------------------- Conectar

        // Devuelve false (con el motivo en "error") si no se pudo abrir el puerto
        public bool CrearPartida(int puerto, out string error)
        {
            Desconectar();
            sala = new SalaServidor();
            try
            {
                sala.Iniciar(puerto);
            }
            catch (SocketException)
            {
                sala = null;
                error = "No se pudo abrir el puerto " + puerto + ". ¿Ya hay una partida abierta en este equipo?";
                return false;
            }

            Unirse("127.0.0.1", puerto);   // el host también es un jugador: se conecta a su propio servidor
            error = null;
            return true;
        }

        public void Unirse(string ip, int puerto)
        {
            CerrarCliente();
            cliente = new ClienteWebSocket();
            cliente.MensajeRecibido += AlRecibirTexto;
            cliente.Desconectado += AlPerderConexion;
            cliente.Conectar("ws://" + ip + ":" + puerto + "/");
        }

        // Salida voluntaria: no dispara AlDesconectarse
        public void Desconectar()
        {
            CerrarCliente();
            sala?.Detener();
            sala = null;
            Reiniciar();
        }

        // ---------------------------------------------------------------- Escuchar

        public void Escuchar(string tipo, Action<Mensaje> metodo, Origen origen)
        {
            if (!suscripciones.TryGetValue(tipo, out var lista))
                suscripciones[tipo] = lista = new List<Suscripcion>();
            lista.Add(new Suscripcion { Metodo = metodo, Origen = origen });
        }

        public void DejarDeEscuchar(string tipo, Action<Mensaje> metodo)
        {
            if (suscripciones.TryGetValue(tipo, out var lista))
                lista.RemoveAll(s => s.Metodo == metodo);
        }

        // ---------------------------------------------------------------- Enviar

        public void EnviarAlHost(string tipo, object datos = null) => Enviar(tipo, Destino.Host, datos);

        // Solo tiene efecto si lo manda el host (el servidor no deja que un jugador hable a todos)
        public void EnviarATodos(string tipo, object datos = null) => Enviar(tipo, Destino.Todos, datos);

        // Mensaje privado: el servidor se lo entrega únicamente a ese jugador. Solo para el host.
        public void EnviarA(int idJugador, string tipo, object datos = null) => Enviar(tipo, idJugador, datos);

        public void Expulsar(int idJugador, string motivo)
        {
            if (EsHost) Enviar(TiposSistema.Expulsar, Destino.Host, new DatosJugador { id = idJugador, motivo = motivo });
        }

        void Enviar(string tipo, int para, object datos)
        {
            if (cliente == null || !cliente.EstaConectado)
            {
                Debug.LogWarning("[Red] No se envió '" + tipo + "': no hay conexión");
                return;
            }
            cliente.Enviar(Mensaje.Crear(tipo, para, datos).ATexto());
        }

        // ---------------------------------------------------------------- Ciclo de Unity

        void Awake()
        {
            // Sin esto, Unity pausa el juego cuando su ventana pierde el foco:
            // el host dejaría de reenviar mensajes y la partida se congelaría para todos.
            Application.runInBackground = true;
        }

        void Update()
        {
            // Los hilos de red dejan todo en colas; aquí, en el hilo principal, se procesa
            sala?.ProcesarEventos();
            cliente?.ProcesarEventos();
        }

        void OnDestroy() => Desconectar();
        void OnApplicationQuit() => Desconectar();

        // ---------------------------------------------------------------- Recepción

        void AlRecibirTexto(string texto)
        {
            if (!Mensaje.IntentarLeer(texto, out var m))
            {
                Debug.LogWarning("[Red] Mensaje inválido ignorado");
                return;
            }
            if (registrarMensajes) Debug.Log("[Red] ← " + m.tipo + " de " + m.de + ": " + m.datos);

            if (m.tipo == TiposSistema.Bienvenida)
            {
                if (m.de != Destino.Servidor) return;
                var d = m.Leer<DatosBienvenida>();
                MiId = d.tuId;
                IdHost = d.idHost;
                EsHost = MiId == IdHost;
                Conectado = true;
                AlConectarse?.Invoke();
                return;
            }

            if (!suscripciones.TryGetValue(m.tipo, out var lista)) return;

            // Copia: un método podría dejar de escuchar mientras recorremos la lista
            foreach (var s in lista.ToArray())
                if (OrigenValido(m, s.Origen)) s.Metodo(m);
        }

        bool OrigenValido(Mensaje m, Origen origen)
        {
            switch (origen)
            {
                case Origen.Host: return m.de == IdHost;
                case Origen.Servidor: return m.de == Destino.Servidor;
                default: return true;
            }
        }

        void AlPerderConexion(string motivo)
        {
            sala?.Detener();
            sala = null;
            CerrarCliente();
            Reiniciar();
            AlDesconectarse?.Invoke(motivo);
        }

        void CerrarCliente()
        {
            if (cliente == null) return;
            cliente.MensajeRecibido -= AlRecibirTexto;
            cliente.Desconectado -= AlPerderConexion;
            cliente.Cerrar();
            cliente = null;
        }

        void Reiniciar()
        {
            Conectado = false;
            EsHost = false;
            MiId = -1;
            IdHost = -1;
        }
    }
}
