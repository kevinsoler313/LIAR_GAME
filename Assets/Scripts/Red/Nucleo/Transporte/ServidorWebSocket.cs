using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using Opcode = LiarGame.Red.Transporte.ProtocoloWebSocket.Opcode;

namespace LiarGame.Red.Transporte
{
    // Servidor WebSocket propio, sin paquetes externos.
    //  - Escucha conexiones TCP en un puerto y hace el handshake WebSocket.
    //  - Cada conexión tiene un id (1, 2, 3…) y su propio hilo de lectura.
    //  - Los hilos de red NO tocan Unity: guardan lo que pasa en una cola, y ProcesarEventos()
    //    (llamado desde Update, en el hilo principal) dispara los eventos.
    public sealed class ServidorWebSocket
    {
        public event Action<int> ClienteConectado;
        public event Action<int, string> MensajeRecibido;
        public event Action<int> ClienteDesconectado;

        enum TipoEvento { Conectado, Mensaje, Desconectado }

        struct Evento
        {
            public TipoEvento Tipo;
            public int Id;
            public string Texto;
        }

        sealed class Conexion
        {
            public int Id;
            public TcpClient Tcp;
            public NetworkStream Flujo;
            public bool EsLocal;                                 // viene de este mismo equipo (127.0.0.1)
            public readonly object CandadoEscritura = new object();   // un solo hilo escribe a la vez
            public volatile bool Cerrada;
        }

        readonly ConcurrentQueue<Evento> eventos = new ConcurrentQueue<Evento>();
        readonly ConcurrentDictionary<int, Conexion> conexiones = new ConcurrentDictionary<int, Conexion>();

        TcpListener escucha;
        int ultimoId;
        volatile bool activo;

        public bool Activo => activo;

        // ---------------------------------------------------------------- Arrancar y parar

        // Lanza SocketException si el puerto ya está en uso
        public void Iniciar(int puerto)
        {
            if (activo) return;
            escucha = new TcpListener(IPAddress.Any, puerto);   // Any = acepta desde todas las redes del equipo
            escucha.Start();
            activo = true;
            new Thread(BucleAceptar) { IsBackground = true, Name = "WS aceptar" }.Start();
        }

        public void Detener()
        {
            if (!activo) return;
            activo = false;
            try { escucha.Stop(); } catch { /* ya estaba cerrado */ }

            foreach (var c in conexiones.Values) Cerrar(c, 1001, "El servidor se cerró");
            conexiones.Clear();
        }

        // ---------------------------------------------------------------- Enviar

        public void Enviar(int id, string texto)
        {
            if (conexiones.TryGetValue(id, out var c))
                EnviarTrama(c, Opcode.Texto, Encoding.UTF8.GetBytes(texto));
        }

        // Cierra la conexión de un cliente avisándole el motivo
        public void Desconectar(int id, string motivo)
        {
            if (conexiones.TryGetValue(id, out var c)) Cerrar(c, 1000, motivo);
        }

        public bool EsLocal(int id) => conexiones.TryGetValue(id, out var c) && c.EsLocal;

        // ---------------------------------------------------------------- Hilo principal

        public void ProcesarEventos()
        {
            while (eventos.TryDequeue(out var e))
            {
                switch (e.Tipo)
                {
                    case TipoEvento.Conectado: ClienteConectado?.Invoke(e.Id); break;
                    case TipoEvento.Mensaje: MensajeRecibido?.Invoke(e.Id, e.Texto); break;
                    case TipoEvento.Desconectado: ClienteDesconectado?.Invoke(e.Id); break;
                }
            }
        }

        // ---------------------------------------------------------------- Hilos de red

        void BucleAceptar()
        {
            while (activo)
            {
                TcpClient tcp;
                try { tcp = escucha.AcceptTcpClient(); }
                catch { break; }   // Detener() cierra el listener y AcceptTcpClient falla: salimos

                tcp.NoDelay = true;   // mensajes pequeños: que salgan de una vez, sin esperar a juntar más
                var remoto = (IPEndPoint)tcp.Client.RemoteEndPoint;
                var c = new Conexion
                {
                    Id = Interlocked.Increment(ref ultimoId),
                    Tcp = tcp,
                    Flujo = tcp.GetStream(),
                    EsLocal = IPAddress.IsLoopback(remoto.Address)
                };
                new Thread(() => BucleLeer(c)) { IsBackground = true, Name = "WS cliente " + c.Id }.Start();
            }
        }

        void BucleLeer(Conexion c)
        {
            bool registrada = false;
            try
            {
                if (!ProtocoloWebSocket.HacerHandshake(c.Flujo)) return;

                conexiones[c.Id] = c;
                registrada = true;
                eventos.Enqueue(new Evento { Tipo = TipoEvento.Conectado, Id = c.Id });

                // Un mensaje puede llegar partido en varias tramas: se juntan hasta la que trae Fin = true
                var mensaje = new MemoryStream();
                while (activo && !c.Cerrada)
                {
                    var leida = ProtocoloWebSocket.LeerTrama(c.Flujo);
                    if (leida == null) break;
                    var trama = leida.Value;

                    switch (trama.Codigo)
                    {
                        case Opcode.Texto:
                        case Opcode.Binario:
                        case Opcode.Continuacion:
                            mensaje.Write(trama.Datos, 0, trama.Datos.Length);
                            if (mensaje.Length > ProtocoloWebSocket.TamanoMaximo)
                                throw new IOException("Mensaje demasiado grande");
                            if (trama.Fin)
                            {
                                string texto = Encoding.UTF8.GetString(mensaje.ToArray());
                                mensaje.SetLength(0);
                                eventos.Enqueue(new Evento { Tipo = TipoEvento.Mensaje, Id = c.Id, Texto = texto });
                            }
                            break;

                        case Opcode.Ping:
                            EnviarTrama(c, Opcode.Pong, trama.Datos);   // el estándar pide responder con el mismo contenido
                            break;

                        case Opcode.Cerrar:
                            Cerrar(c, 1000, "");   // el cliente se despidió: le devolvemos el cierre
                            break;
                    }
                }
            }
            catch (Exception)
            {
                // conexión rota o datos inválidos: se trata igual que una desconexión
            }
            finally
            {
                bool estaba = conexiones.TryRemove(c.Id, out _);
                Cerrar(c, 1000, "");
                if (registrada && estaba)
                    eventos.Enqueue(new Evento { Tipo = TipoEvento.Desconectado, Id = c.Id });
            }
        }

        void EnviarTrama(Conexion c, Opcode codigo, byte[] datos)
        {
            if (c.Cerrada) return;
            var trama = ProtocoloWebSocket.CrearTrama(codigo, datos);
            try
            {
                lock (c.CandadoEscritura) c.Flujo.Write(trama, 0, trama.Length);
            }
            catch
            {
                Cerrar(c, 1000, "");   // no se pudo escribir: el cliente ya no está
            }
        }

        void Cerrar(Conexion c, ushort codigo, string motivo)
        {
            if (c.Cerrada) return;
            try
            {
                var trama = ProtocoloWebSocket.CrearTrama(Opcode.Cerrar, ProtocoloWebSocket.DatosCierre(codigo, motivo));
                lock (c.CandadoEscritura) c.Flujo.Write(trama, 0, trama.Length);
            }
            catch { /* si ya se cayó, no importa */ }

            c.Cerrada = true;
            try { c.Tcp.Close(); } catch { }
        }
    }
}
