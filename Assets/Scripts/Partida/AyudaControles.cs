using UnityEngine;
using UnityEngine.InputSystem;

// Recuadro pequeño con los controles, en la esquina inferior izquierda.
//  - Al aparecer se ve claro unos segundos y después se atenúa para no estorbar.
//  - Con H se reduce a una etiqueta mínima ("H · controles") y vuelve con H. Se recuerda entre partidas.
//  - No se muestra mientras está abierto el menú de red, ni cuando aparece el selector
//    de un poder (ese ya trae su propia línea de ayuda).
[RequireComponent(typeof(CanvasGroup))]
public class AyudaControles : MonoBehaviour
{
    [SerializeField] GameObject completa;       // el recuadro con todas las teclas
    [SerializeField] GameObject mini;           // la etiqueta "H · controles"
    [SerializeField] GameObject menuRed;        // panel del menú de red: mientras se vea, la ayuda no

    [Header("Visibilidad")]
    [SerializeField] float alfaClaro = 0.95f;
    [SerializeField] float alfaTenue = 0.45f;
    [SerializeField] float segundosClaro = 8f;  // cuánto se ve claro al aparecer o al cambiar con H
    [SerializeField] float velocidadFundido = 3f;

    const string ClaveMinimizada = "liar.ayudaControles.minimizada";

    CanvasGroup grupo;
    bool minimizada;
    bool estabaVisible;
    float tiempoClaro;

    void Awake()
    {
        grupo = GetComponent<CanvasGroup>();
        grupo.alpha = 0f;
        grupo.interactable = false;
        grupo.blocksRaycasts = false;   // nunca roba clics al juego

        minimizada = PlayerPrefs.GetInt(ClaveMinimizada, 0) == 1;
        AplicarTamano();
    }

    void Update()
    {
        bool visible = (menuRed == null || !menuRed.activeInHierarchy) && !SelectorOpciones.Abierto;

        if (visible && Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame)
        {
            minimizada = !minimizada;
            PlayerPrefs.SetInt(ClaveMinimizada, minimizada ? 1 : 0);
            AplicarTamano();
            tiempoClaro = 0f;
        }

        if (visible && !estabaVisible) tiempoClaro = 0f;   // acaba de aparecer: un rato bien visible
        estabaVisible = visible;
        if (visible) tiempoClaro += Time.unscaledDeltaTime;

        float meta = !visible ? 0f : (tiempoClaro < segundosClaro ? alfaClaro : alfaTenue);
        grupo.alpha = Mathf.MoveTowards(grupo.alpha, meta, velocidadFundido * Time.unscaledDeltaTime);
    }

    void AplicarTamano()
    {
        completa.SetActive(!minimizada);
        mini.SetActive(minimizada);
    }
}
