using System;

// Los mensajes propios del juego: su nombre (tipo) y la clase con los datos que lleva cada uno.
// Si agregas un mensaje nuevo, va aquí.
public static class TiposJuego
{
    public const string Sillas = "juego.sillas";         // host → todos: quién está sentado en cada silla
    public const string Estado = "juego.estado";         // host → todos: si la partida va, de quién es el turno y quién ganó
    public const string Reparto = "juego.reparto";       // host → UN jugador: sus cartas (privado)
    public const string Jugar = "juego.jugar";           // jugador → host: "quiero jugar esta carta"
    public const string Jugada = "juego.jugada";         // host → todos: "la silla X jugó una carta" (ya validada)
    public const string Rechazo = "juego.rechazo";       // host → UN jugador: "no puedes hacer eso" y por qué
    public const string Acusar = "juego.acusar";         // jugador → host: "¡Mentiroso!" (a la última carta del monto)
    public const string Acusacion = "juego.acusacion";   // host → todos: el resultado de la acusación
    public const string Comer = "juego.comer";           // host → UN jugador: qué cartas se comió (privado)
    public const string Castigo = "juego.castigo";       // host → todos: alguien quiso ganar con un poder y se come cartas
}
[Serializable]
public class DatosCastigo{
    public int silla;
    public int cantidad =3;
}
[Serializable]
public class DatosSillas
{
    public int[] idPorSilla;   // idPorSilla[2] = id del jugador sentado en la silla 2, o -1 si está libre
}

[Serializable]
public class DatosEstado
{
    public bool enJuego;
    public int turno;          // silla que juega (-1 = nadie)
    public int ganador = -1;   // silla que ganó (-1 = nadie todavía)
}

[Serializable]
public class DatosReparto
{
    public int[] mano;         // ids de MIS cartas (ver Baraja.PorId)
    public bool[] ocupadas;    // qué sillas reciben cartas, para que todos animen el mismo reparto
}

[Serializable]
public class DatosJugar
{
    public int carta;          // id de la carta que quiero jugar (Baraja.IdDe)
    public int opcion;         // la opción del poder (ver ReglasPoderes); -1 si es una carta normal
}

[Serializable]
public class DatosJugada
{
    public int silla;          // quién jugó
    public bool poder;         // true = va a la pila de poderes (boca arriba); false = al monto (boca abajo)
    public int carta;          // id de la carta SOLO si es poder (todos la ven). Si va al monto es -1: es secreta
    public int opcion;         // la opción del poder, para que todos apliquen el mismo efecto
    public int turno;          // de quién es el turno DESPUÉS de esta jugada (-1 = nadie: esperando acusaciones)
}

[Serializable]
public class DatosRechazo
{
    public string motivo;
}

[Serializable]
public class DatosAcusacion
{
    public int acusador;       // silla que gritó "¡Mentiroso!"
    public int acusado;        // silla que tiró la última carta del monto
    public int carta;          // la carta acusada, ya revelada (todos la ven)
    public int esperado;       // el número que tocaba cuando la tiró
    public bool mentira;       // true = no era el número que tocaba
    public int castigado;      // silla que se come las cartas
    public int cantidad;       // cuántas se come (normalmente 3; menos si el mazo se acaba)
    public int turno;          // de quién es el turno después
}

[Serializable]
public class DatosComer
{
    public int[] cartas;       // ids de las cartas que me comí
}
