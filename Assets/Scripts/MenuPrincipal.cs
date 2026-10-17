using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;   // para cerrar las instrucciones con Escape
using TMPro;

public class MenuPrincipal : MonoBehaviour
{
    [SerializeField] string escenaJuego = "Bar";          // la escena que se carga al darle a Jugar

    [Header("Instrucciones")]
    [SerializeField] GameObject panelInstrucciones;      // el panel que tapa el menú
    [SerializeField] GameObject[] paginas;                // una página por tema (cómo se juega, mentiroso, poderes...)
    [SerializeField] TMP_Text textoPagina;                // "1 / 4"
    [SerializeField] GameObject botonAnterior;
    [SerializeField] GameObject botonSiguiente;

    [Header("Otros")]
    [SerializeField] GameObject botonSalir;               // en WebGL no se puede salir, así que se esconde

    int paginaActual;

    void Start()
    {
        panelInstrucciones.SetActive(false);

        if (Application.platform == RuntimePlatform.WebGLPlayer && botonSalir != null)
            botonSalir.SetActive(false);
    }

    void Update()
    {
        if (!panelInstrucciones.activeSelf) return;
        if (Keyboard.current == null) return;

        if (Keyboard.current.escapeKey.wasPressedThisFrame) CerrarInstrucciones();
        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) PaginaSiguiente();
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) PaginaAnterior();
    }

    public void Jugar()
    {
        SceneManager.LoadScene(escenaJuego);
    }

    public void MostrarInstrucciones()
    {
        panelInstrucciones.SetActive(true);
        IrAPagina(0);   // siempre empieza desde la primera
    }

    public void CerrarInstrucciones()
    {
        panelInstrucciones.SetActive(false);
    }

    public void PaginaSiguiente() => IrAPagina(paginaActual + 1);
    public void PaginaAnterior() => IrAPagina(paginaActual - 1);

    void IrAPagina(int indice)
    {
        paginaActual = Mathf.Clamp(indice, 0, paginas.Length - 1);

        for (int i = 0; i < paginas.Length; i++)
            paginas[i].SetActive(i == paginaActual);

        textoPagina.text = (paginaActual + 1) + " / " + paginas.Length;
        botonAnterior.SetActive(paginaActual > 0);
        botonSiguiente.SetActive(paginaActual < paginas.Length - 1);
    }

    public void Salir()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;   // en el editor solo para el Play
#else
        Application.Quit();
#endif
    }
}
