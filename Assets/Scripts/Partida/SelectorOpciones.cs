using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

// Ventana sencilla para elegir una opción (Break: pares/impares/normal; Pass: a qué número saltar).
// Se maneja con ← → y E (o Enter) para confirmar, Esc para cancelar. También con el mouse.
public class SelectorOpciones : MonoBehaviour
{
    [SerializeField] GameObject panel;
    [SerializeField] TMP_Text titulo;
    [SerializeField] TMP_Text pregunta;
    [SerializeField] RectTransform contenedor;        // fila donde se crean las opciones

    [Header("Aspecto de las opciones")]
    [SerializeField] TMP_FontAsset fuente;
    [SerializeField] Sprite fondoOpcion;
    [SerializeField] Sprite bordeOpcion;
    [SerializeField] Color fondoNormal = new Color(0.07f, 0.04f, 0.03f, 0.9f);
    [SerializeField] Color fondoElegida = new Color(0.85f, 0.4f, 0.12f, 1f);
    [SerializeField] Color textoNormal = new Color(0.79f, 0.64f, 0.29f, 1f);
    [SerializeField] Color textoElegida = new Color(0.04f, 0.03f, 0.02f, 1f);
    [SerializeField] Color borde = new Color(0.56f, 0.45f, 0.23f, 1f);
    [SerializeField] float alto = 64f;
    [SerializeField] float anchoMinimo = 64f;

    // Mientras el selector está abierto (o se cerró en este mismo frame), la mano no lee el teclado
    static int frameCerrado = -1;
    public static bool Abierto { get; private set; }
    public static bool BloqueaEntrada => Abierto || Time.frameCount == frameCerrado;

    readonly List<(RectTransform caja, Image fondo, TMP_Text texto)> opciones = new List<(RectTransform, Image, TMP_Text)>();
    Action<int> alElegir;
    Action alCancelar;
    int elegida;
    int frameAbierto;

    void Awake() => panel.SetActive(false);

    public void Mostrar(string tituloTexto, string preguntaTexto, string[] textos, int inicial, Action<int> elegir, Action cancelar = null)
    {
        titulo.text = tituloTexto;
        pregunta.text = preguntaTexto;
        alElegir = elegir;
        alCancelar = cancelar;

        foreach (var o in opciones) Destroy(o.caja.gameObject);
        opciones.Clear();
        for (int i = 0; i < textos.Length; i++) opciones.Add(CrearOpcion(textos[i], i));

        elegida = Mathf.Clamp(inicial, 0, textos.Length - 1);
        frameAbierto = Time.frameCount;
        Abierto = true;
        panel.SetActive(true);
        Pintar();
    }

    (RectTransform, Image, TMP_Text) CrearOpcion(string texto, int indice)
    {
        var go = new GameObject("Opcion " + texto, typeof(RectTransform), typeof(Image), typeof(Button), typeof(LayoutElement));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(contenedor, false);

        var fondo = go.GetComponent<Image>();
        fondo.sprite = fondoOpcion; fondo.type = Image.Type.Sliced;
        var largo = go.GetComponent<LayoutElement>();
        largo.minWidth = Mathf.Max(anchoMinimo, texto.Length * 22f + 36f);
        largo.preferredHeight = alto;

        var marco = new GameObject("Borde", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        marco.transform.SetParent(rt, false);
        Estirar((RectTransform)marco.transform);
        marco.sprite = bordeOpcion; marco.type = Image.Type.Sliced; marco.color = borde; marco.raycastTarget = false;

        var t = new GameObject("Texto", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        t.transform.SetParent(rt, false);
        Estirar((RectTransform)t.transform);
        t.font = fuente; t.text = texto; t.fontSize = 30; t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.NoWrap; t.raycastTarget = false;

        go.GetComponent<Button>().transition = Selectable.Transition.None;
        go.GetComponent<Button>().onClick.AddListener(() => { elegida = indice; Confirmar(); });
        return (rt, fondo, t);
    }

    static void Estirar(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (!Abierto) return;

        // la opción bajo el mouse queda elegida
        if (Mouse.current != null && Mouse.current.delta.ReadValue() != Vector2.zero)
            for (int i = 0; i < opciones.Count; i++)
                if (RectTransformUtility.RectangleContainsScreenPoint(opciones[i].caja, Mouse.current.position.ReadValue(), null))
                    elegida = i;

        var teclado = Keyboard.current;
        if (teclado != null && Time.frameCount != frameAbierto)   // la E que abrió el selector no lo confirma
        {
            if (teclado.rightArrowKey.wasPressedThisFrame) Mover(+1);
            if (teclado.leftArrowKey.wasPressedThisFrame) Mover(-1);
            if (teclado.eKey.wasPressedThisFrame || teclado.enterKey.wasPressedThisFrame) { Confirmar(); return; }
            if (teclado.escapeKey.wasPressedThisFrame) { Cerrar(); alCancelar?.Invoke(); return; }
        }
        Pintar();
    }

    void Pintar()
    {
        float paso = 1f - Mathf.Exp(-18f * Time.unscaledDeltaTime);
        for (int i = 0; i < opciones.Count; i++)
        {
            bool si = i == elegida;
            opciones[i].fondo.color = si ? fondoElegida : fondoNormal;
            opciones[i].texto.color = si ? textoElegida : textoNormal;
            opciones[i].caja.localScale = Vector3.Lerp(opciones[i].caja.localScale, Vector3.one * (si ? 1.12f : 1f), paso);
        }
    }

    public void Mover(int delta) => elegida = Mathf.Clamp(elegida + delta, 0, opciones.Count - 1);

    public void Confirmar()
    {
        if (!Abierto) return;
        int i = elegida;
        var accion = alElegir;
        Cerrar();
        accion?.Invoke(i);
    }

    void Cerrar()
    {
        Abierto = false;
        frameCerrado = Time.frameCount;
        panel.SetActive(false);
    }
}
