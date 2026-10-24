using System;
using UnityEngine;

namespace LiarGame.Red.Protocolo
{
    // El "sobre" de todo lo que viaja por la red, convertido a JSON.
    //   tipo  → qué es (por ejemplo "juego.reparto"); cada script escucha los tipos que le interesan
    //   de    → quién lo mandó. Lo escribe el SERVIDOR al reenviarlo, así nadie puede hacerse pasar por otro
    //   para  → a quién va: Destino.Host, Destino.Todos o el id de un jugador
    //   datos → el contenido, también en JSON (cada tipo tiene su propia clase de datos)
    [Serializable]
    public class Mensaje
    {
        public string tipo;
        public int de;
        public int para;
        public string datos;

        public static Mensaje Crear(string tipo, int para, object contenido = null)
        {
            return new Mensaje
            {
                tipo = tipo,
                para = para,
                datos = contenido != null ? JsonUtility.ToJson(contenido) : ""
            };
        }

        // Convierte "datos" en la clase que corresponde a este tipo de mensaje
        public T Leer<T>() => JsonUtility.FromJson<T>(datos);

        public string ATexto() => JsonUtility.ToJson(this);

        public static bool IntentarLeer(string texto, out Mensaje mensaje)
        {
            try
            {
                mensaje = JsonUtility.FromJson<Mensaje>(texto);
                return mensaje != null && !string.IsNullOrEmpty(mensaje.tipo);
            }
            catch (Exception)
            {
                mensaje = null;   // JSON mal formado: se ignora
                return false;
            }
        }
    }

    // Valores especiales para los campos "de" y "para"
    public static class Destino
    {
        public const int Host = 0;        // para: el jugador que creó la partida
        public const int Todos = -1;      // para: todos los conectados (incluido quien lo manda)
        public const int Servidor = -2;   // de: mensajes que genera el propio servidor
    }
}
