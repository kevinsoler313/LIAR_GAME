using System;

namespace LiarGame.Red.Protocolo
{
    // Mensajes que maneja la propia red (no el juego). Empiezan con "sis." para distinguirlos.
    public static class TiposSistema
    {
        public const string Bienvenida = "sis.bienvenida";   // servidor → jugador nuevo: tu id y quién es el host
        public const string JugadorEntro = "sis.entro";      // servidor → host: se conectó alguien
        public const string JugadorSalio = "sis.salio";      // servidor → host: se fue alguien
        public const string Expulsar = "sis.expulsar";       // host → servidor: saca a un jugador
    }

    [Serializable]
    public class DatosBienvenida
    {
        public int tuId;
        public int idHost;
    }

    [Serializable]
    public class DatosJugador
    {
        public int id;
        public string motivo;   // solo para Expulsar
    }
}
