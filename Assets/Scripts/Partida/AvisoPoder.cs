using UnityEngine;
using TMPro;

// Cartelito debajo del letrero que anuncia el último poder jugado ("PASS · Salto al 8").
// Aparece con un pequeño golpe y se desvanece después de unos segundos.
public class AvisoPoder : MonoBehaviour
{
    [SerializeField] CanvasGroup grupo;
    [SerializeField] TMP_Text textoPoder;
    [SerializeField] TMP_Text textoDetalle;
    [SerializeField] float duracion = 4f;          // segundos visible (0 = se queda hasta el próximo poder)
    [SerializeField] float desvanecer = 0.6f;

    float restante;

    void Awake()
    {
        grupo.alpha = 0f;
        grupo.blocksRaycasts = false;
    }

    public void Mostrar(string poder, string detalle)
    {
        textoPoder.text = poder;
        textoDetalle.text = detalle;
        restante = duracion > 0 ? duracion : float.PositiveInfinity;
        grupo.alpha = 1f;
        transform.localScale = Vector3.one * 1.25f;
    }

    void Update()
    {
        transform.localScale = Vector3.Lerp(transform.localScale, Vector3.one, 1f - Mathf.Exp(-10f * Time.unscaledDeltaTime));
        if (grupo.alpha <= 0f) return;

        restante -= Time.unscaledDeltaTime;
        if (restante < desvanecer) grupo.alpha = Mathf.Clamp01(restante / desvanecer);
    }
}
