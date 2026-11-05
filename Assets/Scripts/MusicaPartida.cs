using System.Collections;
using UnityEngine;

// Música de fondo de la partida: canciones de bar sonando una tras otra en orden aleatorio, muy bajito.
// Va en la escena Bar. Arranca al entrar a la partida (crearla o unirse) y se apaga con un fundido
// al volver a la sala; la música del menú (MusicaFondo) hace justo lo contrario.
// Las canciones se barajan como un mazo: suenan todas antes de que se repita alguna, y al rebarajar
// nunca vuelve a empezar con la que acaba de sonar. Los filtros de este mismo objeto
// (paso alto, paso bajo, distorsión) son los mismos de la música del menú: radio vieja de bar.
[RequireComponent(typeof(AudioSource))]
public class MusicaPartida : MonoBehaviour
{
    [SerializeField] AudioClip[] canciones;
    [SerializeField, Range(0f, 1f)] float volumen = 0.02f;   // por debajo de la del menú: que no tape las cartas
    [SerializeField] float duracionFundido = 1.5f;
    [SerializeField] float pausaEntreCanciones = 1.5f;       // silencio entre una y otra

    static MusicaPartida instancia;
    AudioSource fuente;
    Coroutine rutina;
    int[] orden;
    int siguiente;

    void Awake()
    {
        instancia = this;
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;   // 2D
        fuente.volume = 0f;
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }

    // Entraste a la partida
    public static void Encender()
    {
        if (instancia == null || instancia.canciones == null || instancia.canciones.Length == 0) return;
        if (instancia.rutina != null) instancia.StopCoroutine(instancia.rutina);
        instancia.rutina = instancia.StartCoroutine(instancia.Reproducir());
    }

    // De vuelta en la sala
    public static void Apagar()
    {
        if (instancia == null || instancia.rutina == null) return;
        instancia.StopCoroutine(instancia.rutina);
        instancia.rutina = instancia.StartCoroutine(instancia.Fundir(0f));
    }

    IEnumerator Reproducir()
    {
        if (!fuente.isPlaying)   // si venía apagándose, sigue la misma canción
        {
            fuente.volume = 0f;
            fuente.clip = Siguiente();
            fuente.Play();
        }
        yield return Fundir(volumen);

        while (true)
        {
            // esperar a que termine la canción (con el audio en pausa isPlaying también es false)
            while (fuente.isPlaying || AudioListener.pause) yield return null;
            yield return new WaitForSecondsRealtime(pausaEntreCanciones);
            fuente.clip = Siguiente();
            fuente.volume = volumen;
            fuente.Play();
        }
    }

    IEnumerator Fundir(float objetivo)
    {
        float inicial = fuente.volume;
        for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
        {
            fuente.volume = Mathf.Lerp(inicial, objetivo, t / duracionFundido);
            yield return null;
        }
        fuente.volume = objetivo;
        if (objetivo == 0f)
        {
            fuente.Stop();
            rutina = null;
        }
    }

    AudioClip Siguiente()
    {
        if (orden == null || siguiente >= orden.Length) Barajar();
        return canciones[orden[siguiente++]];
    }

    void Barajar()
    {
        int anterior = orden != null && orden.Length > 0 ? orden[orden.Length - 1] : -1;
        if (orden == null || orden.Length != canciones.Length)
        {
            orden = new int[canciones.Length];
            for (int i = 0; i < orden.Length; i++) orden[i] = i;
        }
        for (int i = orden.Length - 1; i > 0; i--)
        {
            int j = Random.Range(0, i + 1);
            (orden[i], orden[j]) = (orden[j], orden[i]);
        }
        if (orden.Length > 1 && orden[0] == anterior) (orden[0], orden[1]) = (orden[1], orden[0]);
        siguiente = 0;
    }
}
