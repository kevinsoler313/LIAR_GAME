using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;

namespace LiarGame.Red.Transporte
{
    // Lo mínimo del protocolo WebSocket (RFC 6455) que necesita nuestro servidor:
    //  1. El "apretón de manos": el cliente llega hablando HTTP y le respondemos "101 Switching Protocols".
    //  2. Leer y escribir tramas (frames): cada mensaje viaja con una cabecera corta + los datos.
    internal static class ProtocoloWebSocket
    {
        // Texto fijo que define el estándar para calcular Sec-WebSocket-Accept
        const string GuidEstandar = "258EAFA5-E914-47DA-95CA-C5AB0DC85B11";

        // Límite de seguridad: ningún mensaje del juego se acerca a esto
        public const int TamanoMaximo = 64 * 1024;

        public enum Opcode : byte
        {
            Continuacion = 0x0,
            Texto = 0x1,
            Binario = 0x2,
            Cerrar = 0x8,
            Ping = 0x9,
            Pong = 0xA
        }

        public struct Trama
        {
            public bool Fin;          // true = es el último pedazo del mensaje
            public Opcode Codigo;
            public byte[] Datos;
        }

        // ---------------------------------------------------------------- Handshake

        // Lee la petición HTTP del cliente y le responde aceptando el cambio a WebSocket.
        public static bool HacerHandshake(Stream flujo)
        {
            string peticion = LeerCabeceraHttp(flujo);
            if (peticion == null) return false;

            string clave = null;
            foreach (var linea in peticion.Split(new[] { "\r\n" }, StringSplitOptions.None))
            {
                int dosPuntos = linea.IndexOf(':');
                if (dosPuntos <= 0) continue;
                string nombre = linea.Substring(0, dosPuntos).Trim();
                if (nombre.Equals("Sec-WebSocket-Key", StringComparison.OrdinalIgnoreCase))
                    clave = linea.Substring(dosPuntos + 1).Trim();
            }
            if (string.IsNullOrEmpty(clave)) return false;   // no era un cliente WebSocket

            string aceptar;
            using (var sha1 = SHA1.Create())
                aceptar = Convert.ToBase64String(sha1.ComputeHash(Encoding.ASCII.GetBytes(clave + GuidEstandar)));

            string respuesta =
                "HTTP/1.1 101 Switching Protocols\r\n" +
                "Upgrade: websocket\r\n" +
                "Connection: Upgrade\r\n" +
                "Sec-WebSocket-Accept: " + aceptar + "\r\n\r\n";

            var bytes = Encoding.ASCII.GetBytes(respuesta);
            flujo.Write(bytes, 0, bytes.Length);
            return true;
        }

        // Lee byte a byte hasta la línea en blanco (\r\n\r\n) que cierra la cabecera HTTP
        static string LeerCabeceraHttp(Stream flujo)
        {
            var buffer = new MemoryStream();
            int b;
            while ((b = flujo.ReadByte()) != -1)
            {
                buffer.WriteByte((byte)b);
                long n = buffer.Length;
                if (n > 8192) return null;   // cabecera absurda: no es un cliente nuestro

                if (n >= 4)
                {
                    var a = buffer.GetBuffer();
                    if (a[n - 4] == '\r' && a[n - 3] == '\n' && a[n - 2] == '\r' && a[n - 1] == '\n')
                        return Encoding.ASCII.GetString(a, 0, (int)n);
                }
            }
            return null;
        }

        // ---------------------------------------------------------------- Tramas

        // Lee una trama completa. Devuelve null si la conexión se cerró.
        // Los clientes SIEMPRE enmascaran sus datos (el estándar lo exige), así que aquí se desenmascaran.
        public static Trama? LeerTrama(Stream flujo)
        {
            var cabecera = new byte[2];
            if (!LeerExacto(flujo, cabecera, 2)) return null;

            bool fin = (cabecera[0] & 0x80) != 0;
            var codigo = (Opcode)(cabecera[0] & 0x0F);
            bool conMascara = (cabecera[1] & 0x80) != 0;
            long largo = cabecera[1] & 0x7F;

            // 126 y 127 son "marcas": el largo real viene en los siguientes 2 u 8 bytes
            if (largo == 126)
            {
                var ext = new byte[2];
                if (!LeerExacto(flujo, ext, 2)) return null;
                largo = (ext[0] << 8) | ext[1];
            }
            else if (largo == 127)
            {
                var ext = new byte[8];
                if (!LeerExacto(flujo, ext, 8)) return null;
                largo = 0;
                for (int i = 0; i < 8; i++) largo = (largo << 8) | ext[i];
            }

            if (largo < 0 || largo > TamanoMaximo)
                throw new IOException("Trama demasiado grande: " + largo + " bytes");

            var mascara = new byte[4];
            if (conMascara && !LeerExacto(flujo, mascara, 4)) return null;

            var datos = new byte[largo];
            if (!LeerExacto(flujo, datos, (int)largo)) return null;

            if (conMascara)
                for (int i = 0; i < datos.Length; i++) datos[i] ^= mascara[i % 4];

            return new Trama { Fin = fin, Codigo = codigo, Datos = datos };
        }

        // Arma una trama completa (un solo pedazo). El servidor nunca enmascara.
        public static byte[] CrearTrama(Opcode codigo, byte[] datos)
        {
            int largo = datos.Length;
            int tamCabecera = largo < 126 ? 2 : (largo <= ushort.MaxValue ? 4 : 10);
            var trama = new byte[tamCabecera + largo];

            trama[0] = (byte)(0x80 | (byte)codigo);   // FIN = 1 + opcode
            if (largo < 126)
            {
                trama[1] = (byte)largo;
            }
            else if (largo <= ushort.MaxValue)
            {
                trama[1] = 126;
                trama[2] = (byte)(largo >> 8);
                trama[3] = (byte)largo;
            }
            else
            {
                trama[1] = 127;
                for (int i = 0; i < 8; i++) trama[2 + i] = (byte)((long)largo >> (56 - 8 * i));
            }

            Buffer.BlockCopy(datos, 0, trama, tamCabecera, largo);
            return trama;
        }

        // Datos de una trama de cierre: código de 2 bytes + motivo en texto (máx. 123 bytes)
        public static byte[] DatosCierre(ushort codigo, string motivo)
        {
            var texto = Encoding.UTF8.GetBytes(motivo ?? "");
            if (texto.Length > 123) Array.Resize(ref texto, 123);
            var datos = new byte[2 + texto.Length];
            datos[0] = (byte)(codigo >> 8);
            datos[1] = (byte)codigo;
            Buffer.BlockCopy(texto, 0, datos, 2, texto.Length);
            return datos;
        }

        static bool LeerExacto(Stream flujo, byte[] destino, int cantidad)
        {
            int leidos = 0;
            while (leidos < cantidad)
            {
                int n = flujo.Read(destino, leidos, cantidad - leidos);
                if (n <= 0) return false;   // el otro lado cerró
                leidos += n;
            }
            return true;
        }
    }
}
