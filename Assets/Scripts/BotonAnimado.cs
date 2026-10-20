using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

// Animación de hover/clic para los botones del menú: crece un poco, se ilumina el fondo,
// cambia el color del texto y aparece un resplandor de farol detrás.
public class BotonAnimado : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
{
    [Header("Escala")]
    [SerializeField] float escalaHover = 1.06f;
    [SerializeField] float escalaPresionado = 0.95f;
    [SerializeField] float velocidad = 14f;          // qué tan rápido llega a su estado (más = más brusco)

    [Header("Colores")]
    [SerializeField] Graphic fondo;
    [SerializeField] Color fondoHover = Color.white;
    [SerializeField] TMP_Text texto;
    [SerializeField] Color textoHover = Color.white;

    [Header("Resplandor")]
    [SerializeField] Graphic resplandor;              // imagen de brillo detrás del botón (opcional)
    [SerializeField] float alfaResplandor = 0.55f;

    Color fondoNormal, textoNormal;
    Vector3 escalaBase;
    bool encima, presionado;
    float t;                                          // 0 = normal, 1 = hover

    void Awake()
    {
        escalaBase = transform.localScale;
        if (fondo != null) fondoNormal = fondo.color;
        if (texto != null) textoNormal = texto.color;
        if (resplandor != null) resplandor.canvasRenderer.SetAlpha(0f);
    }

    void OnDisable()
    {
        // si el panel se cierra con el mouse encima, que vuelva limpio la próxima vez
        encima = presionado = false;
        t = 0f;
        transform.localScale = escalaBase;
        Aplicar();
    }

    void Update()
    {
        float paso = 1f - Mathf.Exp(-velocidad * Time.unscaledDeltaTime);   // suavizado independiente del framerate

        t = Mathf.Lerp(t, encima ? 1f : 0f, paso);
        float objetivo = presionado ? escalaPresionado : Mathf.Lerp(1f, escalaHover, t);
        transform.localScale = Vector3.Lerp(transform.localScale, escalaBase * objetivo, paso);

        Aplicar();
    }

    void Aplicar()
    {
        if (fondo != null) fondo.color = Color.Lerp(fondoNormal, fondoHover, t);
        if (texto != null) texto.color = Color.Lerp(textoNormal, textoHover, t);
        if (resplandor != null) resplandor.canvasRenderer.SetAlpha(alfaResplandor * t);
    }

    public void OnPointerEnter(PointerEventData e) => encima = true;
    public void OnPointerExit(PointerEventData e) { encima = false; presionado = false; }
    public void OnPointerDown(PointerEventData e) => presionado = true;
    public void OnPointerUp(PointerEventData e) => presionado = false;
}
