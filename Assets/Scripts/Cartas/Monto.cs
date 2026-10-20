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

    // Quita todas las cartas del monto (para un reparto nuevo)
    public void Recoger()
    {
        StopAllCoroutines();
        foreach (var c in cartas) if (c != null) Destroy(c.gameObject);
        cartas.Clear();
    }

    IEnumerator Volar(CartaVisual carta, int indice)
    {
        var t = carta.transform;
        t.SetParent(transform, true);

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

        alRecibir.Invoke(carta);
    }
}
