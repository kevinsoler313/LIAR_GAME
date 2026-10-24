using System;
using LiarGame.Red;
using LiarGame.Red.Protocolo;
using UnityEngine;

// Quién está sentado en cada silla (reemplaza a JugadorRed).
//  - HOST: cuando alguien entra le da la primera silla libre; si la mesa está llena, lo saca.
//          Cada vez que algo cambia, le manda a todos la lista completa (mensaje "juego.sillas").
//  - TODOS: al recibir la lista, buscan su propio id y sientan su cámara en esa silla.
public class MesaRed : MonoBehaviour
{
    public const int CantidadSillas = 4;

    [SerializeField] RedJuego red;
    [SerializeField] SillasRed sillas;

    int[] idPorSilla = SillasVacias();

    public event Action AlCambiar;

    // ---------------------------------------------------------------- Consultas

    public int MiSilla => SillaDe(red.MiId);
    public int IdEnSilla(int silla) => idPorSilla[silla];

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
    }

    void OnDisable()
    {
        red.AlConectarse -= AlConectarse;
        red.AlDesconectarse -= AlDesconectarse;
        red.DejarDeEscuchar(TiposSistema.JugadorEntro, AlEntrarJugador);
        red.DejarDeEscuchar(TiposSistema.JugadorSalio, AlSalirJugador);
        red.DejarDeEscuchar(TiposJuego.Sillas, AlRecibirSillas);
    }

    // ---------------------------------------------------------------- Solo el host decide

    void AlConectarse()
    {
        if (!red.EsHost) return;
        idPorSilla = SillasVacias();
        idPorSilla[0] = red.MiId;   // el host siempre se sienta en la silla 0
        Publicar();
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
        Publicar();
    }

    void Publicar() => red.EnviarATodos(TiposJuego.Sillas, new DatosSillas { idPorSilla = idPorSilla });

    // ---------------------------------------------------------------- Todos

    void AlRecibirSillas(Mensaje m)
    {
        var lista = m.Leer<DatosSillas>().idPorSilla;
        if (lista == null || lista.Length != CantidadSillas) return;
        idPorSilla = lista;

        int mia = MiSilla;
        if (mia >= 0 && mia != sillas.SillaLocal) sillas.Sentar(mia);
        AlCambiar?.Invoke();
    }

    void AlDesconectarse(string motivo)
    {
        idPorSilla = SillasVacias();
        AlCambiar?.Invoke();
    }

    static int[] SillasVacias()
    {
        var vacias = new int[CantidadSillas];
        for (int i = 0; i < vacias.Length; i++) vacias[i] = -1;
        return vacias;
    }
}
