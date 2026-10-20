using System.Collections.Generic;
using UnityEngine;

// El mazo sobre la mesa: mezcla la baraja y apila todas las cartas boca abajo.
// Se arma también en el editor para verlo en la escena; las cartas no se guardan en la escena
// (se regeneran al abrirla o al darle Play, ya mezcladas).
[ExecuteAlways]
public class MazoMesa : MonoBehaviour
{
    [SerializeField] Baraja baraja;

    [Header("Materiales")]
    [SerializeField] Material materialReverso;
    [SerializeField] Material materialFrente;
    [SerializeField] Material materialCanto;

    [Header("Medidas (metros, el doble de una carta de póker para que se lea desde la silla)")]
    [SerializeField] float ancho = 0.126f;
    [SerializeField] float largo = 0.176f;
    [SerializeField] float grosor = 0.0004f;
    [SerializeField] float radioEsquina = 0.007f;

    [Header("Desorden")]
    [SerializeField] float desordenPosicion = 0.002f;   // cuánto se corre cada carta
    [SerializeField] float desordenGiro = 1.2f;           // grados

    readonly List<CartaVisual> cartas = new List<CartaVisual>();

    public int Restantes => cartas.Count;
    public float Grosor => grosor;

    void OnEnable() => Construir();
    void OnDisable() => Limpiar();

    void OnValidate()
    {
        // reconstruir al tocar valores en el Inspector (no se puede crear objetos dentro de OnValidate)
#if UNITY_EDITOR
        // solo en edición: en Play lo arma OnEnable y reconstruir a mitad de partida duplicaría cartas
        if (Application.isPlaying) return;
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null && isActiveAndEnabled && !UnityEditor.EditorApplication.isPlayingOrWillChangePlaymode) Construir();
        };
#endif
    }

    [ContextMenu("Mezclar de nuevo")]
    public void Construir()
    {
        Limpiar();
        if (baraja == null) return;

        var malla = MallaCarta.Obtener(ancho, largo, grosor, radioEsquina);
        var mazo = baraja.Mezclar();

        for (int i = 0; i < mazo.Count; i++)
        {
            var go = new GameObject("Carta " + (i + 1) + " (" + mazo[i] + ")", typeof(MeshFilter), typeof(MeshRenderer), typeof(CartaVisual));
            go.hideFlags = HideFlags.DontSave;   // se regenera siempre; no ensucia la escena
            go.transform.SetParent(transform, false);
            go.transform.localPosition = new Vector3(
                Random.Range(-desordenPosicion, desordenPosicion),
                grosor * (i + 0.5f),
                Random.Range(-desordenPosicion, desordenPosicion));
            go.transform.localRotation = Quaternion.Euler(0, Random.Range(-desordenGiro, desordenGiro), 0);

            var carta = go.GetComponent<CartaVisual>();
            carta.Configurar(mazo[i], malla, materialReverso, materialFrente, materialCanto);
            cartas.Add(carta);
        }
    }

    // Saca la carta de arriba (para repartir más adelante)
    public CartaVisual Robar()
    {
        if (cartas.Count == 0) return null;
        var carta = cartas[cartas.Count - 1];
        cartas.RemoveAt(cartas.Count - 1);
        return carta;
    }

    void Limpiar()
    {
        cartas.Clear();
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var hijo = transform.GetChild(i).gameObject;
            if (Application.isPlaying) Destroy(hijo);
            else DestroyImmediate(hijo);
        }
    }
}
