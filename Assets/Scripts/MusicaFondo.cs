using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Música de todo el menú: menú principal, instrucciones y la sala de red del Bar (crear / unirse / esperar jugadores).
// Va solo en MainMenu y se queda viva entre escenas (DontDestroyOnLoad); si aparece otra copia se borra.
// Al entrar a una partida (crearla o unirse) se apaga con un fundido, y vuelve a sonar
// al regresar a la sala o al menú. Los filtros de este mismo objeto (paso alto, paso bajo, distorsión)
// le dan el sonido de radio vieja de bar.
[RequireComponent(typeof(AudioSource))]
public class MusicaFondo : MonoBehaviour
{
    [SerializeField] AudioClip cancion;
    [SerializeField, Range(0f, 1f)] float volumen = 0.04f;   // de fondo: que no tape los efectos
    [SerializeField] float duracionFundido = 1.5f;           // segundos que tarda en apagarse / encenderse

    static MusicaFondo instancia;
    AudioSource fuente;
    Coroutine fundido;
    bool apagada;

    void Awake()
    {
        if (instancia != null)
        {
            Destroy(gameObject);   // ya hay una sonando desde otra escena
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);
        SceneManager.sceneLoaded += AlCargarEscena;

        fuente = GetComponent<AudioSource>();
        fuente.clip = cancion;
        fuente.volume = volumen;
        fuente.loop = true;            // vuelve a empezar al terminar
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;      // 2D
        fuente.Play();
    }

    void OnDestroy()
    {
        if (instancia != this) return;
        instancia = null;
        SceneManager.sceneLoaded -= AlCargarEscena;
    }

    // Cualquier escena que se carga empieza en un menú (MainMenu o la sala del Bar), así que la música vuelve
    void AlCargarEscena(Scene escena, LoadSceneMode modo) => Encender();

    // Empieza la partida con cartas
    public static void Apagar()
    {
        if (instancia == null || instancia.apagada) return;
        instancia.apagada = true;
        instancia.Fundir(0f);
    }

    // De vuelta en la sala o en el menú
    public static void Reanudar()
    {
        if (instancia != null) instancia.Encender();
    }

    void Encender()
    {
        if (!apagada) return;
        apagada = false;
        if (!fuente.isPlaying)
        {
            fuente.volume = 0f;
            fuente.Play();         // desde el principio de la canción
        }
        Fundir(volumen);
    }

    void Fundir(float objetivo)
    {
        if (fundido != null) StopCoroutine(fundido);
        fundido = StartCoroutine(RutinaFundido(objetivo));
    }

    IEnumerator RutinaFundido(float objetivo)
    {
        float inicial = fuente.volume;
        for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
        {
            fuente.volume = Mathf.Lerp(inicial, objetivo, t / duracionFundido);
            yield return null;
        }
        fuente.volume = objetivo;
        if (objetivo == 0f) fuente.Stop();
        fundido = null;
    }
}
