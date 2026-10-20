using UnityEngine;
using UnityEngine.UI;
using TMPro;

// Letrero de madera arriba al centro: el número que toca, si la escalera sube o baja
// y si va en orden normal, pares o impares. Cuando un dato cambia, da un pequeño "golpe".
public class HUDPartida : MonoBehaviour
{
    [SerializeField] EstadoPartida estado;

    [SerializeField] TMP_Text textoNumero;
    [SerializeField] RectTransform flechaSentido;     // apunta arriba al subir, abajo al bajar
    [SerializeField] TMP_Text textoSentido;
    [SerializeField] TMP_Text textoOrden;

    [Header("Golpe al cambiar")]
    [SerializeField] float escalaGolpe = 1.35f;
    [SerializeField] float velocidad = 10f;

    int numero = -1;
    SentidoEscalera sentido;
    OrdenEscalera orden;
    bool primera = true;

    void OnEnable()
    {
        if (estado == null) return;
        estado.alCambiar.AddListener(Refrescar);
        primera = true;
        Refrescar();
    }

    void OnDisable() { if (estado != null) estado.alCambiar.RemoveListener(Refrescar); }

    void Refrescar()
    {
        if (primera || estado.Numero != numero) { textoNumero.text = estado.Numero.ToString(); Golpe(textoNumero.transform); }
        if (primera || estado.Sentido != sentido)
        {
            bool sube = estado.Sentido == SentidoEscalera.Subiendo;
            textoSentido.text = sube ? "SUBIENDO" : "BAJANDO";
            flechaSentido.localRotation = Quaternion.Euler(0, 0, sube ? 0 : 180);
            Golpe(flechaSentido); Golpe(textoSentido.transform);
        }
        if (primera || estado.Orden != orden)
        {
            textoOrden.text = estado.Orden == OrdenEscalera.Normal ? "NORMAL" : estado.Orden == OrdenEscalera.Pares ? "PARES" : "IMPARES";
            Golpe(textoOrden.transform);
        }

        numero = estado.Numero; sentido = estado.Sentido; orden = estado.Orden;
        if (primera) { primera = false; ResetEscalas(); }
    }

    void Golpe(Transform t) => t.localScale = Vector3.one * escalaGolpe;

    void ResetEscalas()
    {
        textoNumero.transform.localScale = flechaSentido.localScale = textoSentido.transform.localScale = textoOrden.transform.localScale = Vector3.one;
    }

    void Update()
    {
        // todo vuelve suave a su tamaño normal después del golpe
        float paso = 1f - Mathf.Exp(-velocidad * Time.unscaledDeltaTime);
        Suavizar(textoNumero.transform, paso);
        Suavizar(flechaSentido, paso);
        Suavizar(textoSentido.transform, paso);
        Suavizar(textoOrden.transform, paso);
    }

    static void Suavizar(Transform t, float paso)
    {
        if (t.localScale != Vector3.one) t.localScale = Vector3.Lerp(t.localScale, Vector3.one, paso);
    }
}
