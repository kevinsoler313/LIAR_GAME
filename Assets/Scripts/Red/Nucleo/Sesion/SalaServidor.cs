using System.Collections.Generic;
using LiarGame.Red.Protocolo;
using LiarGame.Red.Transporte;

namespace LiarGame.Red.Sesion
{
    // La "sala" que corre dentro del juego del host. NO sabe de cartas ni de turnos:
    // solo reparte ids, presenta a los jugadores y reenvía mensajes.
    //
    // Reglas de reenvío (para que nadie pueda hacer trampa mandando órdenes falsas):
    //   - Al reenviar, el servidor escribe "de" con el id real de quien mandó el mensaje.
    //   - Los jugadores SOLO pueden hablarle al host. Si piden "para = Todos" o a otro jugador, se cambia a Host.
    //   - Solo el host puede mandar a Todos, a un jugador en particular, o expulsar.
    public sealed class SalaServidor
    {
        readonly ServidorWebSocket servidor = new ServidorWebSocket();
        readonly List<int> conectados = new List<int>();
        int idHost = -1;   // la primera conexión desde este mismo equipo (el host se conecta a 127.0.0.1)

        public void Iniciar(int puerto)
        {
            servidor.ClienteConectado += AlConectar;
            servidor.MensajeRecibido += AlRecibir;
            servidor.ClienteDesconectado += AlDesconectar;
            servidor.Iniciar(puerto);
        }

        public void Detener()
        {
            servidor.ClienteConectado -= AlConectar;
            servidor.MensajeRecibido -= AlRecibir;
            servidor.ClienteDesconectado -= AlDesconectar;
            servidor.Detener();
            conectados.Clear();
            idHost = -1;
        }

        // Desde Update del hilo principal
        public void ProcesarEventos() => servidor.ProcesarEventos();

        // ---------------------------------------------------------------- Eventos del transporte

        void AlConectar(int id)
        {
            if (idHost < 0)
            {
                // El primero en llegar debe ser el propio host. Si llega alguien de afuera antes, se rechaza.
                if (!servidor.EsLocal(id))
                {
                    servidor.Desconectar(id, "La partida todavía no está lista");
                    return;
                }
                idHost = id;
            }

            conectados.Add(id);
            EnviarDelServidor(id, TiposSistema.Bienvenida, new DatosBienvenida { tuId = id, idHost = idHost });

            if (id != idHost)
                EnviarDelServidor(idHost, TiposSistema.JugadorEntro, new DatosJugador { id = id });
        }

        void AlDesconectar(int id)
        {
            if (!conectados.Remove(id)) return;
            if (id != idHost && conectados.Contains(idHost))
                EnviarDelServidor(idHost, TiposSistema.JugadorSalio, new DatosJugador { id = id });
        }

        void AlRecibir(int de, string texto)
        {
            if (!Mensaje.IntentarLeer(texto, out var m)) return;   // basura: se ignora

            m.de = de;   // el remitente real, aunque el mensaje dijera otra cosa
            bool esHost = de == idHost;

            if (!esHost)
            {
                m.para = Destino.Host;   // un jugador normal solo puede hablarle al host
            }
            else if (m.tipo == TiposSistema.Expulsar)
            {
                var d = m.Leer<DatosJugador>();
                servidor.Desconectar(d.id, string.IsNullOrEmpty(d.motivo) ? "Te sacaron de la partida" : d.motivo);
                return;
            }

            string salida = m.ATexto();
            if (m.para == Destino.Todos)
            {
                foreach (int id in conectados) servidor.Enviar(id, salida);
            }
            else
            {
                int destino = m.para == Destino.Host ? idHost : m.para;
                servidor.Enviar(destino, salida);
            }
        }

        void EnviarDelServidor(int para, string tipo, object datos)
        {
            var m = Mensaje.Crear(tipo, para, datos);
            m.de = Destino.Servidor;
            servidor.Enviar(para, m.ATexto());
        }
    }
}
