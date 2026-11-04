using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Golpe de pantalla al gritar "¡MENTIROSO!": el borde de la pantalla se pone rojo, la cámara tiembla un momento
// y aparece un cartel grande "¡MENTIROSO!" en el centro, que entra de golpe y se desvanece antes de revelar la carta.
// Va en una Image que cubre todo el HUD; la textura del borde (rojo en las orillas, transparente al centro)
// se genera por código, así que no hace falta ningún sprite.
[RequireComponent(typeof(Image))]
public class EfectoMentiroso : MonoBehaviour
{
    [SerializeField] Color color = new Color(0.85f, 0.04f, 0.02f, 0.6f);
    [SerializeField] float subida = 0.12f;          // segundos que tarda en ponerse rojo
    [SerializeField] float duracionBorde = 1.8f;    // segundos hasta que se borra del todo
    [SerializeField] float duracionTemblor = 0.45f;
    [SerializeField] float fuerzaTemblor = 0.024f;  // metros que se mueve la cámara como máximo

    [Header("Cartel")]
    [SerializeField] TMP_Text cartel;               // texto grande en el centro de la pantalla
    [SerializeField] float duracionCartel = 1.8f;   // termina justo antes de que la carta empiece a subir
    [SerializeField] float escalaEntrada = 2.2f;    // entra grande y se asienta en su tamaño
    [SerializeField] float temblorCartel = 6f;      // píxeles que tiembla mientras tiembla la cámara

    Image borde;
    float tiempo = float.PositiveInfinity;          // desde el último disparo
    Transform camara;
    Vector3 desplazamiento;                         // lo que movimos la cámara el frame anterior

    void Awake()
    {
        borde = GetComponent<Image>();
        borde.sprite = CrearBorde();
        borde.raycastTarget = false;
        if (cartel != null) cartel.raycastTarget = false;
        Pintar(0f);
        PintarCartel();
    }

    public void Disparar()
    {
        tiempo = 0f;
        camara = Camera.main != null ? Camera.main.transform : null;
    }

    void Update()
    {
        if (tiempo > Mathf.Max(duracionBorde, duracionCartel)) return;
        tiempo += Time.unscaledDeltaTime;

        float fuerza = tiempo < subida
            ? tiempo / subida                                                   // entra rápido
            : 1f - Mathf.SmoothStep(0f, 1f, (tiempo - subida) / (duracionBorde - subida));   // y se va suave
        Pintar(tiempo > duracionBorde ? 0f : fuerza);
        PintarCartel();
    }

    void PintarCartel()
    {
        if (cartel == null) return;
        bool visible = tiempo < duracionCartel;
        if (cartel.gameObject.activeSelf != visible) cartel.gameObject.SetActive(visible);
        if (!visible) return;

        // golpe de entrada (0.15 s), quieto, y se desvanece en el último tercio
        float entrada = Mathf.Clamp01(tiempo / 0.15f);
        float escala = Mathf.Lerp(escalaEntrada, 1f, 1f - (1f - entrada) * (1f - entrada));
        float salida = Mathf.InverseLerp(duracionCartel, duracionCartel * 0.66f, tiempo);
        cartel.alpha = Mathf.Min(entrada * 2f, salida);
        cartel.transform.localScale = Vector3.one * escala;

        // tiembla junto con la cámara
        float t = tiempo * 30f;
        float sacudida = tiempo < duracionTemblor ? temblorCartel * (1f - tiempo / duracionTemblor) : 0f;
        cartel.rectTransform.anchoredPosition = new Vector2(Mathf.PerlinNoise(t, 3f) - 0.5f, Mathf.PerlinNoise(3f, t) - 0.5f) * 2f * sacudida;
    }

    void LateUpdate()
    {
        if (camara == null) return;
        camara.localPosition -= desplazamiento;     // deshacer el temblor anterior: la cámara vuelve a su lugar
        desplazamiento = Vector3.zero;
        if (tiempo > duracionTemblor) return;

        float t = tiempo * 30f;
        float amplitud = fuerzaTemblor * (1f - tiempo / duracionTemblor);   // cada vez más suave
        desplazamiento = new Vector3(Mathf.PerlinNoise(t, 0f) - 0.5f, Mathf.PerlinNoise(0f, t) - 0.5f, 0f) * 2f * amplitud;
        camara.localPosition += desplazamiento;
    }

    void OnDisable()
    {
        if (camara != null) camara.localPosition -= desplazamiento;
        desplazamiento = Vector3.zero;
    }

    void Pintar(float fuerza)
    {
        var c = color;
        c.a *= Mathf.Clamp01(fuerza);
        borde.color = c;
    }

    // Blanco con alfa: 0 en el centro y sube hacia las orillas (el color lo pone la Image)
    static Sprite CrearBorde()
    {
        const int lado = 128;
        var tex = new Texture2D(lado, lado, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        var px = new Color32[lado * lado];
        for (int y = 0; y < lado; y++)
            for (int x = 0; x < lado; x++)
            {
                float u = (x + 0.5f) / lado * 2f - 1f, v = (y + 0.5f) / lado * 2f - 1f;
                float d = Mathf.Sqrt(u * u + v * v) / 1.41421f;          // 0 al centro, 1 en las esquinas
                float a = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.45f, 1f, d));
                a = Mathf.Max(a, Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.75f, 1f, Mathf.Max(Mathf.Abs(u), Mathf.Abs(v)))));   // orillas rectas también
                px[y * lado + x] = new Color32(255, 255, 255, (byte)(a * 255));
            }
        tex.SetPixels32(px);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, lado, lado), new Vector2(0.5f, 0.5f));
    }
}
