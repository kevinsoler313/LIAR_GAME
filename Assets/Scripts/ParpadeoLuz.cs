using UnityEngine;
using UnityEngine.UI;

// Hace que una imagen de la UI parpadee como la llama de un farol
public class ParpadeoLuz : MonoBehaviour
{
    [SerializeField] float intensidad = 0.4f;   // cuánto baja la luz en el peor momento (0 = nada, 1 = se apaga)
    [SerializeField] float velocidad = 2.5f;    // qué tan rápido tiembla

    Graphic grafico;
    float alfaBase;
    float semilla;   // para que cada farol parpadee distinto

    void Awake()
    {
        grafico = GetComponent<Graphic>();
        alfaBase = grafico.color.a;
        semilla = Random.value * 100f;
    }

    void Update()
    {
        float ruido = Mathf.PerlinNoise(semilla, Time.time * velocidad);
        Color c = grafico.color;
        c.a = alfaBase * Mathf.Lerp(1f - intensidad, 1f, ruido);
        grafico.color = c;
    }
}
