using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Reloj del turno con forma de torta: un círculo que se va vaciando en sentido horario y los segundos al centro.
// Crema mientras sobra tiempo, rojo y latiendo en los últimos segundos; se esconde cuando el reloj no corre
// (reparto, revelación de un "¡Mentiroso!", fin de la partida).
// No guarda nada: cada frame le pregunta a PartidaRed cuánto queda. Los círculos se dibujan por código.
public class TemporizadorTurno : MonoBehaviour
{
    [SerializeField] PartidaRed partida;
    [SerializeField] CanvasGroup grupo;
    [SerializeField] Image fondo;          // disco oscuro detrás
    [SerializeField] Image torta;          // Filled · Radial 360: la parte que queda
    [SerializeField] Image centro;         // disquito oscuro al centro donde se leen los segundos
    [SerializeField] TMP_Text segundos;

    [Header("Colores")]
    [SerializeField] Color colorNormal = new Color(0.93f, 0.86f, 0.70f);
    [SerializeField] Color colorMiTurno = new Color(0.95f, 0.55f, 0.18f);   // los mismos del cartel de turno
    [SerializeField] Color colorUrgente = new Color(0.9f, 0.15f, 0.1f);
    [SerializeField] float segundosUrgente = 3f;

    float alfa;

    void Awake()
    {
        var disco = CrearDisco();
        fondo.sprite = disco;
        torta.sprite = disco;
        if (centro != null) centro.sprite = disco;
        torta.type = Image.Type.Filled;
        torta.fillMethod = Image.FillMethod.Radial360;
        torta.fillOrigin = (int)Image.Origin360.Top;
        torta.fillClockwise = false;   // se vacía en sentido horario desde las 12
        grupo.alpha = 0f;
    }

    void Update()
    {
        bool visible = partida != null && partida.RelojCorriendo;
        alfa = Mathf.MoveTowards(alfa, visible ? 1f : 0f, Time.unscaledDeltaTime * 6f);
        grupo.alpha = alfa;
        if (partida == null || alfa <= 0f) return;

        float total = Mathf.Max(0.01f, partida.SegundosPorTurno);
        float queda = partida.TiempoRestante;
        torta.fillAmount = queda / total;
        segundos.text = Mathf.CeilToInt(queda).ToString();

        bool urgente = queda <= segundosUrgente;
        var color = urgente ? colorUrgente : partida.EsMiTurno ? colorMiTurno : colorNormal;
        torta.color = color;
        segundos.color = urgente ? colorUrgente : colorNormal;

        // los últimos segundos late
        float latido = urgente ? 1f + 0.08f * Mathf.Abs(Mathf.Sin(queda * Mathf.PI)) : 1f;
        transform.localScale = Vector3.one * latido;
    }

    // Disco blanco con borde suave (el color lo pone cada Image)
    static Sprite CrearDisco()
    {
        const int lado = 128;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[lado * lado];
        float radio = lado * 0.5f;
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(radio, radio));
                byte a = (byte)(Mathf.Clamp01(radio - d) * 255);   // 1 píxel de suavizado en el borde
                px[y * lado + x] = new Color32(255, 255, 255, a);
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f));
    }
}
