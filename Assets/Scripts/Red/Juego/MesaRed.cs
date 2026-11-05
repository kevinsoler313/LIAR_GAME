using System;
using System.Collections.Generic;
using LiarGame.Red;
using LiarGame.Red.Protocolo;
using UnityEngine;

// Quién está sentado en cada silla (reemplaza a JugadorRed).
//  - HOST: cuando alguien entra le da la primera silla libre; si la mesa está llena, lo saca.
//          Cada vez que algo cambia, le manda a todos la lista completa (mensaje "juego.sillas").
//  - TODOS: al recibir la lista, buscan su propio id y sientan su cámara en esa silla.
// Nombres: cada jugador le manda el suyo al host al conectarse; el host los reparte junto con las sillas.
public class MesaRed : MonoBehaviour
{
    public const int CantidadSillas = 4;

    [SerializeField] RedJuego red;
    [SerializeField] SillasRed sillas;

    int[] idPorSilla = SillasVacias();
    string[] nombrePorSilla = new string[CantidadSillas];

    // El nombre que escribió este jugador en el menú (MenuRed lo llena antes de conectarse)
    public static string NombreLocal = "";
    const int largoMaximoNombre = 16;

    readonly Dictionary<int, string> nombrePorId = new Dictionary<int, string>();   // solo el host

    public event Action AlCambiar;

    // ---------------------------------------------------------------- Consultas

    public int MiSilla => SillaDe(red.MiId);
    public int IdEnSilla(int silla) => idPorSilla[silla];

    // "Kevin", o "Jugador 3" si todavía no llegó su nombre
    public string NombreEnSilla(int silla)
    {
        if (silla < 0 || silla >= CantidadSillas) return "";
        string n = nombrePorSilla != null && silla < nombrePorSilla.Length ? nombrePorSilla[silla] : null;
        return string.IsNullOrEmpty(n) ? "Jugador " + (silla + 1) : n;
    }

    public int CantidadJugadores
    {
        get
        {
            int n = 0;
            foreach (int id in idPorSilla) if (id >= 0) n++;
            return n;
        }
    }

    public bool[] SillasOcupadas()
    {
        var ocupadas = new bool[CantidadSillas];
        for (int s = 0; s < CantidadSillas; s++) ocupadas[s] = idPorSilla[s] >= 0;
        return ocupadas;
    }

    public int SillaDe(int idJugador) => idJugador < 0 ? -1 : Array.IndexOf(idPorSilla, idJugador);

    // ---------------------------------------------------------------- Suscripciones

    void OnEnable()
    {
        red.AlConectarse += AlConectarse;
        red.AlDesconectarse += AlDesconectarse;
        red.Escuchar(TiposSistema.JugadorEntro, AlEntrarJugador, Origen.Servidor);
        red.Escuchar(TiposSistema.JugadorSalio, AlSalirJugador, Origen.Servidor);
        red.Escuchar(TiposJuego.Sillas, AlRecibirSillas, Origen.Host);
        red.Escuchar(TiposJuego.Nombre, AlRecibirNombre, Origen.Cualquiera);   // cualquiera dice su nombre; el host lo anota
    }

    void OnDisable()
    {
        red.AlConectarse -= AlConectarse;
        red.AlDesconectarse -= AlDesconectarse;
        red.DejarDeEscuchar(TiposSistema.JugadorEntro, AlEntrarJugador);
        red.DejarDeEscuchar(TiposSistema.JugadorSalio, AlSalirJugador);
        red.DejarDeEscuchar(TiposJuego.Sillas, AlRecibirSillas);
        red.DejarDeEscuchar(TiposJuego.Nombre, AlRecibirNombre);
    }

    // ---------------------------------------------------------------- Solo el host decide

    void AlConectarse()
    {
        if (red.EsHost)
        {
            idPorSilla = SillasVacias();
            nombrePorId.Clear();
            idPorSilla[0] = red.MiId;   // el host siempre se sienta en la silla 0
            Publicar();
        }
        red.EnviarAlHost(TiposJuego.Nombre, new DatosNombre { nombre = NombreLocal });   // el host también: le llega a sí mismo
    }

    void AlRecibirNombre(Mensaje m)
    {
        if (!red.EsHost) return;
        nombrePorId[m.de] = Limpiar(m.Leer<DatosNombre>().nombre, m.de);   // "de" lo escribe el servidor
        Publicar();
    }

    // Sin etiquetas de texto enriquecido, sin espacios de sobra, corto y sin repetir el de otro jugador
    string Limpiar(string nombre, int id)
    {
        nombre = (nombre ?? "").Replace("<", "").Replace(">", "").Trim();
        if (nombre.Length > largoMaximoNombre) nombre = nombre.Substring(0, largoMaximoNombre).Trim();
        if (nombre == "") return "";

        string unico = nombre;
        for (int n = 2; EstaUsado(unico, id); n++) unico = nombre + " " + n;
        return unico;
    }

    bool EstaUsado(string nombre, int id)
    {
        foreach (var par in nombrePorId)
            if (par.Key != id && string.Equals(par.Value, nombre, StringComparison.OrdinalIgnoreCase)) return true;
        return false;
    }

    void AlEntrarJugador(Mensaje m)
    {
        if (!red.EsHost) return;
        int id = m.Leer<DatosJugador>().id;

        int libre = Array.IndexOf(idPorSilla, -1);
        if (libre < 0)
        {
            red.Expulsar(id, "La mesa está llena");
            return;
        }
        idPorSilla[libre] = id;
        Publicar();
    }

    void AlSalirJugador(Mensaje m)
    {
        if (!red.EsHost) return;
        int silla = SillaDe(m.Leer<DatosJugador>().id);
        if (silla < 0) return;
        idPorSilla[silla] = -1;   // la silla queda libre para el siguiente que entre
        nombrePorId.Remove(m.Leer<DatosJugador>().id);
        Publicar();
    }

    void Publicar()
    {
        var nombres = new string[CantidadSillas];
        for (int s = 0; s < CantidadSillas; s++)
            nombres[s] = idPorSilla[s] >= 0 && nombrePorId.TryGetValue(idPorSilla[s], out var n) ? n : "";
        red.EnviarATodos(TiposJuego.Sillas, new DatosSillas { idPorSilla = idPorSilla, nombrePorSilla = nombres });
    }

    // ---------------------------------------------------------------- Todos

    void AlRecibirSillas(Mensaje m)
    {
        var lista = m.Leer<DatosSillas>().idPorSilla;
        if (lista == null || lista.Length != CantidadSillas) return;
        idPorSilla = lista;
        var nombres = m.Leer<DatosSillas>().nombrePorSilla;
        nombrePorSilla = nombres != null && nombres.Length == CantidadSillas ? nombres : new string[CantidadSillas];

        int mia = MiSilla;
        if (mia >= 0 && mia != sillas.SillaLocal) sillas.Sentar(mia);
        AlCambiar?.Invoke();
    }

    void AlDesconectarse(string motivo)
    {
        idPorSilla = SillasVacias();
        nombrePorSilla = new string[CantidadSillas];
        nombrePorId.Clear();
        AlCambiar?.Invoke();
    }

    static int[] SillasVacias()
    {
        var vacias = new int[CantidadSillas];
        for (int i = 0; i < vacias.Length; i++) vacias[i] = -1;
        return vacias;
    }
}
