using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Reparte las cartas del mazo, una a la vez, a cada jugador activo (en orden, hacia la derecha)
// hasta que todos tengan su mano completa. Cada carta vuela en arco y cae boca abajo en la pila del jugador.
public class Repartidor : MonoBehaviour
{
    [SerializeField] MazoMesa mazo;
    [SerializeField] Monto monto;                        // se vacían al recoger para un reparto nuevo
    [SerializeField] Monto montoPoderes;
    [SerializeField] Transform[] manos;                  // un punto por silla, sobre la mesa frente al jugador
    [SerializeField] bool[] activos = { true, true, true, true };
    [SerializeField] int cartasPorJugador = 8;
    [SerializeField] bool repartirAlIniciar = true;

    [Header("Animación")]
    [SerializeField] float intervalo = 0.12f;            // tiempo entre una carta y la siguiente
    [SerializeField] float duracionVuelo = 0.45f;
    [SerializeField] float alturaArco = 0.12f;           // metros sobre la mesa en la mitad del vuelo
    [SerializeField] float giroExtra = 160f;             // grados de giro en el aire, se va frenando al caer
    [SerializeField] float desordenPila = 0.006f;
    [SerializeField] float desordenGiroPila = 6f;

    public UnityEvent alTerminar;

    readonly List<CartaVisual>[] cartasEnMano = { new List<CartaVisual>(), new List<CartaVisual>(), new List<CartaVisual>(), new List<CartaVisual>() };
    public bool Repartiendo { get; private set; }

    public IReadOnlyList<CartaVisual> Mano(int jugador) => cartasEnMano[jugador];
    public float GrosorCarta => mazo.Grosor;

    void Start()
    {
        if (repartirAlIniciar) Repartir();
    }

    [ContextMenu("Repartir")]
    public void Repartir()
    {
        if (!Application.isPlaying || Repartiendo) return;
        StartCoroutine(RutinaRepartir());
    }

    // Devuelve todo al mazo (mezclado de nuevo) y reparte otra vez
    [ContextMenu("Recoger y repartir")]
    public void RecogerYRepartir()
    {
        if (!Application.isPlaying || Repartiendo) return;
        for (int j = 0; j < cartasEnMano.Length; j++)
        {
            foreach (var c in cartasEnMano[j]) if (c != null) Destroy(c.gameObject);
            cartasEnMano[j].Clear();
        }
        if (monto != null) monto.Recoger();
        if (montoPoderes != null) montoPoderes.Recoger();
        mazo.Construir();
        Repartir();
    }

    IEnumerator RutinaRepartir()
    {
        Repartiendo = true;
        var vuelos = new List<Coroutine>();

        for (int ronda = 0; ronda < cartasPorJugador; ronda++)
            for (int j = 0; j < manos.Length; j++)
            {
                if (!activos[j] || manos[j] == null) continue;

                var carta = mazo.Robar();
                if (carta == null) { ronda = cartasPorJugador; break; }   // se acabó el mazo

                int indice = cartasEnMano[j].Count;
                cartasEnMano[j].Add(carta);
                vuelos.Add(StartCoroutine(Volar(carta, manos[j], indice)));
                yield return new WaitForSeconds(intervalo);
            }

        foreach (var v in vuelos) yield return v;   // esperar a que aterricen las últimas
        Repartiendo = false;
        alTerminar.Invoke();
    }

    IEnumerator Volar(CartaVisual carta, Transform mano, int indice)
    {
        var t = carta.transform;
        t.SetParent(mano, true);

        Vector3 desde = t.localPosition;
        Quaternion giroDesde = t.localRotation;

        // destino en la pila: un poco desordenado y apilado según el orden de llegada
        Vector3 hasta = new Vector3(
            Random.Range(-desordenPila, desordenPila),
            mazo.Grosor * (indice + 0.5f),
            Random.Range(-desordenPila, desordenPila));
        Quaternion giroHasta = Quaternion.Euler(0, Random.Range(-desordenGiroPila, desordenGiroPila), 0);

        // altura del arco en el espacio de la mano (la mesa está un poco inclinada)
        Vector3 arriba = mano.InverseTransformDirection(Vector3.up) * alturaArco;
        float sentido = Random.value < 0.5f ? -1f : 1f;

        for (float tiempo = 0; tiempo < duracionVuelo; tiempo += Time.deltaTime)
        {
            float p = tiempo / duracionVuelo;
            float suave = 1f - Mathf.Pow(1f - p, 3f);                 // sale rápido y frena al caer
            t.localPosition = Vector3.LerpUnclamped(desde, hasta, suave) + arriba * (4f * p * (1f - p));
            t.localRotation = Quaternion.Slerp(giroDesde, giroHasta, suave) * Quaternion.Euler(0, sentido * giroExtra * (1f - suave), 0);
            yield return null;
        }

        t.localPosition = hasta;
        t.localRotation = giroHasta;
    }
}
