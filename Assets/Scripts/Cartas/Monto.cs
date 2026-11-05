using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

// Una pila de cartas jugadas sobre la mesa. Se usa para dos pilas:
//  - el monto del centro, donde las cartas van boca abajo;
//  - la pila de poderes (Pass, Return, Break) a la izquierda del monto, boca arriba.
// Cualquier jugador (local o bot) juega con Jugar(carta).
public class Monto : MonoBehaviour
{
    [SerializeField] MazoMesa mazo;                   // para el grosor de las cartas
    [SerializeField] bool bocaArriba;

    [Header("Animación")]
    [SerializeField] float duracion = 0.45f;
    [SerializeField] float alturaArco = 0.1f;
    [SerializeField] float desordenPosicion = 0.008f;
    [SerializeField] float desordenGiro = 10f;

    [System.Serializable] public class EventoCarta : UnityEvent<CartaVisual> { }
    public EventoCarta alRecibir;                     // se dispara cuando la carta aterriza

    readonly List<CartaVisual> cartas = new List<CartaVisual>();
    public IReadOnlyList<CartaVisual> Cartas => cartas;
    public CartaVisual Ultima => cartas.Count > 0 ? cartas[cartas.Count - 1] : null;

    public void Jugar(CartaVisual carta)
    {
        cartas.Add(carta);
        StartCoroutine(Volar(carta, cartas.Count - 1));
    }

    readonly HashSet<CartaVisual> enElAire = new HashSet<CartaVisual>();
    readonly HashSet<CartaVisual> porVoltear = new HashSet<CartaVisual>();

    // Pone boca arriba una carta del monto (la acusada en un "¡Mentiroso!").
    // Si todavía va volando, se voltea apenas aterrice.
    public void Voltear(CartaVisual carta)
    {
        if (carta == null || !cartas.Contains(carta)) return;
        porVoltear.Add(carta);
        if (!enElAire.Contains(carta)) carta.BocaArriba = true;
    }

    [Header("Exhibir (carta acusada)")]
    [SerializeField] float alturaExhibir = 0.22f;      // metros sobre el monto
    [SerializeField] float escalaExhibir = 1.8f;
    [SerializeField] float vueltasExhibir = 2f;        // giros por todo el recorrido (a velocidad base): así todos la ven derecha
    [SerializeField] float giroArriba = 2f;            // mientras está arriba gira este tanto más rápido

    // Levanta la carta sobre el centro de la mesa, boca arriba y girando, para que toda la mesa la vea,
    // y la vuelve a dejar en el monto (boca arriba). Tarda "subir" en llegar arriba, se queda "arriba"
    // segundos y tarda "bajar" en volver. Se puede esperar con yield.
    public Coroutine Exhibir(CartaVisual carta, float subir, float arriba, float bajar)
    {
        if (carta == null || !cartas.Contains(carta)) return null;
        porVoltear.Add(carta);
        return StartCoroutine(RutinaExhibir(carta, subir, arriba, bajar));
    }

    IEnumerator RutinaExhibir(CartaVisual carta, float subir, float quieta, float bajar)
    {
        while (enElAire.Contains(carta)) yield return null;   // si todavía venía volando, que aterrice primero

        var t = carta.transform;
        Vector3 posMesa = t.localPosition;
        float giroMesa = t.localEulerAngles.y;
        float yaw = giroMesa;
        Vector3 posArriba = posMesa + transform.InverseTransformDirection(Vector3.up) * alturaExhibir;
        float segundos = subir + quieta + bajar;

        for (float tiempo = 0f; tiempo < segundos; tiempo += Time.deltaTime)
        {
            // 0 → 1 al subir, 1 mientras está arriba, 1 → 0 al bajar
            float arriba = tiempo < subir ? tiempo / Mathf.Max(subir, 0.01f)
                         : tiempo > segundos - bajar ? (segundos - tiempo) / Mathf.Max(bajar, 0.01f)
                         : 1f;
            arriba = Mathf.SmoothStep(0f, 1f, arriba);

            // velocidad base, que sube suave hasta "giroArriba" veces mientras la carta está arriba
            yaw += 360f * vueltasExhibir / segundos * Mathf.Lerp(1f, giroArriba, arriba) * Time.deltaTime;
            float volteo = Mathf.Lerp(0f, 180f, Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(tiempo / Mathf.Max(subir, 0.01f))));   // se da vuelta al subir
            t.localPosition = Vector3.Lerp(posMesa, posArriba, arriba);
            t.localRotation = Quaternion.Euler(0f, yaw, volteo);
            t.localScale = Vector3.one * Mathf.Lerp(1f, escalaExhibir, arriba);
            yield return null;
        }

        t.localPosition = posMesa;
        t.localRotation = Quaternion.Euler(0f, yaw, 180f);   // queda boca arriba en el monto, como terminó de girar
        t.localScale = Vector3.one;
    }

    // Quita todas las cartas del monto (para un reparto nuevo)
    public void Recoger()
    {
        StopAllCoroutines();
        enElAire.Clear();
        porVoltear.Clear();
        foreach (var c in cartas) if (c != null) Destroy(c.gameObject);
        cartas.Clear();
    }

    IEnumerator Volar(CartaVisual carta, int indice)
    {
        var t = carta.transform;
        t.SetParent(transform, true);
        enElAire.Add(carta);

        Vector3 desde = t.localPosition;
        Quaternion giroDesde = t.localRotation;
        Vector3 escalaDesde = t.localScale;

        Vector3 hasta = new Vector3(
            Random.Range(-desordenPosicion, desordenPosicion),
            mazo.Grosor * (indice + 0.5f),
            Random.Range(-desordenPosicion, desordenPosicion));
        // boca arriba = volteada sobre su eje largo, así el frente se lee derecho desde la silla del jugador 0
        Quaternion giroHasta = Quaternion.Euler(0, Random.Range(-desordenGiro, desordenGiro), bocaArriba ? 180f : 0f);
        Vector3 arriba = transform.InverseTransformDirection(Vector3.up) * alturaArco;

        for (float tiempo = 0; tiempo < duracion; tiempo += Time.deltaTime)
        {
            float p = tiempo / duracion;
            float suave = 1f - Mathf.Pow(1f - p, 3f);                 // sale rápido y frena al caer
            t.localPosition = Vector3.Lerp(desde, hasta, suave) + arriba * (4f * p * (1f - p));
            t.localRotation = Quaternion.Slerp(giroDesde, giroHasta, suave);
            t.localScale = Vector3.Lerp(escalaDesde, Vector3.one, suave);
            yield return null;
        }
        t.localPosition = hasta;
        t.localRotation = giroHasta;
        t.localScale = Vector3.one;
        enElAire.Remove(carta);
        if (porVoltear.Contains(carta)) carta.BocaArriba = true;
        Sonidos.Tirar();   // la carta pega contra el paño

        alRecibir.Invoke(carta);
    }
}
