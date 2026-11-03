using LiarGame.Red;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

// Menú de pausa de la partida: un botón redondo arriba a la izquierda (o la tecla Esc) lo abre.
//   - Continuar: cierra el menú.
//   - Salir de la partida: se desconecta (si eres el host, se cierra la partida para todos)
//     y recarga la escena, que arranca otra vez en la pantalla de crear o unirse a una partida.
//   - Instrucciones: muestra cómo se juega y los controles.
//   - Salir del juego: cierra el juego (en el editor, detiene el Play).
// Es un juego en red: abrir el menú NO detiene la partida para los demás, solo bloquea tus teclas.
public class MenuPausa : MonoBehaviour
{
    [SerializeField] RedJuego red;
    [SerializeField] GameObject botonPausa;      // el círculo con el símbolo de pausa
    [SerializeField] GameObject panel;           // fondo oscuro + menú (todo lo que se abre)
    [SerializeField] GameObject opciones;        // el cartel con los 4 botones
    [SerializeField] GameObject instrucciones;   // el cartel de instrucciones
    [SerializeField] GameObject menuRed;         // panel de crear/unirse: mientras se vea, no hay botón de pausa

    // Mientras esté abierto, la mano, la acusación (M) y la cámara no leen el teclado ni el mouse
    public static bool Abierto { get; private set; }

    void Awake() => Cerrar();
    void OnDestroy() => Abierto = false;

    void Update()
    {
        bool enMesa = menuRed == null || !menuRed.activeInHierarchy;
        if (!enMesa && Abierto) Cerrar();

        bool mostrarBoton = enMesa && !Abierto;
        if (botonPausa.activeSelf != mostrarBoton) botonPausa.SetActive(mostrarBoton);

        // Esc abre y cierra. Si el selector de un poder está abierto, Esc es para cancelarlo a él.
        if (enMesa && !SelectorOpciones.BloqueaEntrada && Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            if (!Abierto) Abrir();
            else if (instrucciones.activeSelf) VolverDeInstrucciones();
            else Continuar();
        }
    }

    // ---------- Botones ----------

    public void Abrir()
    {
        Abierto = true;
        panel.SetActive(true);
        opciones.SetActive(true);
        instrucciones.SetActive(false);
    }

    public void Continuar() => Cerrar();

    public void VerInstrucciones()
    {
        opciones.SetActive(false);
        instrucciones.SetActive(true);
    }

    public void VolverDeInstrucciones()
    {
        instrucciones.SetActive(false);
        opciones.SetActive(true);
    }

    public void SalirDePartida()
    {
        Cerrar();
        if (red != null) red.Desconectar();
        // Recargar la escena deja la mesa limpia y vuelve a mostrar el menú de crear/unirse (MenuRed)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void SalirDelJuego()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    void Cerrar()
    {
        Abierto = false;
        panel.SetActive(false);
    }
}
