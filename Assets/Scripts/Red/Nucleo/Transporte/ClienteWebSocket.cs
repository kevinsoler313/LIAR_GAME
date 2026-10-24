using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiarGame.Red.Transporte
{
    // Cliente WebSocket usando ClientWebSocket, que viene incluido en .NET (no es un paquete).
    //  - Recibe en segundo plano y guarda lo que llega en una cola.
    //  - Envía en orden: los mensajes salen uno detrás de otro, nunca dos a la vez.
    //  - Los eventos se disparan en el hilo principal con ProcesarEventos() (desde Update).
    public sealed class ClienteWebSocket
    {
        public event Action Conectado;
        public event Action<string> MensajeRecibido;
        public event Action<string> Desconectado;     // con el motivo, para mostrarlo en pantalla

        readonly ConcurrentQueue<Action> eventos = new ConcurrentQueue<Action>();
        readonly ConcurrentQueue<byte[]> porEnviar = new ConcurrentQueue<byte[]>();
        readonly object candadoEnvio = new object();
        bool enviando;

        ClientWebSocket socket;
        CancellationTokenSource cancelar;
        volatile bool cerradoPorMi;

        public bool EstaConectado => socket != null && socket.State == WebSocketState.Open;

        // ---------------------------------------------------------------- Conectar y cerrar

        public void Conectar(string url, int segundosEspera = 5)
        {
            Cerrar();
            cerradoPorMi = false;
            socket = new ClientWebSocket();
            cancelar = new CancellationTokenSource();
            _ = EjecutarAsync(socket, new Uri(url), segundosEspera, cancelar.Token);
        }

        // Cierre pedido por nosotros: no se dispara Desconectado
        public void Cerrar()
        {
            cerradoPorMi = true;
            try { cancelar?.Cancel(); } catch { }
            try { socket?.Abort(); } catch { }
            try { socket?.Dispose(); } catch { }
            socket = null;
            while (porEnviar.TryDequeue(out _)) { }
        }

        // ---------------------------------------------------------------- Enviar

        public void Enviar(string texto)
        {
            if (!EstaConectado) return;
            porEnviar.Enqueue(Encoding.UTF8.GetBytes(texto));

            // Si nadie está enviando, arrancamos el bucle que vacía la cola
            lock (candadoEnvio)
            {
                if (enviando) return;
                enviando = true;
            }
            _ = BucleEnviarAsync(socket);
        }

        async Task BucleEnviarAsync(ClientWebSocket s)
        {
            try
            {
                while (true)
                {
                    if (!porEnviar.TryDequeue(out var datos))
                    {
                        lock (candadoEnvio)
                        {
                            if (porEnviar.IsEmpty) { enviando = false; return; }
                        }
                        continue;
                    }
                    await s.SendAsync(new ArraySegment<byte>(datos), WebSocketMessageType.Text, true, CancellationToken.None)
                           .ConfigureAwait(false);
                }
            }
            catch (Exception)
            {
                lock (candadoEnvio) enviando = false;   // la conexión se cayó: el bucle de recepción lo avisará
            }
        }

        // ---------------------------------------------------------------- Hilo principal

        public void ProcesarEventos()
        {
            while (eventos.TryDequeue(out var accion)) accion();
        }

        // ---------------------------------------------------------------- Segundo plano

        async Task EjecutarAsync(ClientWebSocket s, Uri url, int segundosEspera, CancellationToken token)
        {
            string motivo = "Se perdió la conexión";
            try
            {
                using (var limite = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    limite.CancelAfter(TimeSpan.FromSeconds(segundosEspera));
                    await s.ConnectAsync(url, limite.Token).ConfigureAwait(false);
                }
                eventos.Enqueue(() => Conectado?.Invoke());

                motivo = await BucleRecibirAsync(s, token).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                motivo = "No se encontró la partida (tiempo de espera agotado)";
            }
            catch (WebSocketException)
            {
                motivo = s.State == WebSocketState.Open || s.State == WebSocketState.Aborted
                    ? "Se perdió la conexión con el host"
                    : "No se encontró la partida";
            }
            catch (Exception e)
            {
                motivo = "Error de conexión: " + e.Message;
            }

            if (!cerradoPorMi)
            {
                string final = motivo;
                eventos.Enqueue(() => Desconectado?.Invoke(final));
            }
        }

        // Devuelve el motivo por el que terminó
        async Task<string> BucleRecibirAsync(ClientWebSocket s, CancellationToken token)
        {
            var buffer = new byte[8192];
            var mensaje = new MemoryStream();

            while (s.State == WebSocketState.Open && !token.IsCancellationRequested)
            {
                var r = await s.ReceiveAsync(new ArraySegment<byte>(buffer), token).ConfigureAwait(false);

                if (r.MessageType == WebSocketMessageType.Close)
                {
                    string descripcion = s.CloseStatusDescription;
                    return string.IsNullOrEmpty(descripcion) ? "El host cerró la partida" : descripcion;
                }

                mensaje.Write(buffer, 0, r.Count);
                if (mensaje.Length > 64 * 1024) return "Mensaje demasiado grande";

                if (r.EndOfMessage)
                {
                    string texto = Encoding.UTF8.GetString(mensaje.ToArray());
                    mensaje.SetLength(0);
                    eventos.Enqueue(() => MensajeRecibido?.Invoke(texto));
                }
            }
            return "Se perdió la conexión";
        }
    }
}
