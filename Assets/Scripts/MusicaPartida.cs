using System.Collections;
using UnityEngine;

// Música de fondo de la partida: canciones de bar sonando una tras otra en orden aleatorio, muy bajito.
// Va en la escena Bar. Arranca al entrar a la partida (crearla o unirse) y se apaga con un fundido
// al volver a la sala; la música del menú (MusicaFondo) hace justo lo contrario.
// Las canciones se barajan como un mazo: suenan todas antes de que se repita alguna, y al rebarajar
// nunca vuelve a empezar con la que acaba de sonar. Los filtros de este mismo objeto
// (distorsión, paso alto, paso bajo) la hacen sonar como un bafle viejo.
// La distorsión solo rompe el sonido si le llega fuerte, así que la fuente va a volumen completo
// y el volumen real se aplica al final, en OnAudioFilterRead: este componente va después de los filtros.
[RequireComponent(typeof(AudioSource))]
public class MusicaPartida : MonoBehaviour
{
    [SerializeField] AudioClip[] canciones;
    [SerializeField, Range(0f, 1f)] float volumen = 0.04f;   // igual que la del menú: que no tape las cartas
    [SerializeField, Range(0f, 1f)] float compensacionBafle = 0.2f;   // la distorsión sube mucho el nivel: esto lo devuelve a un 'volumen' normal
    [SerializeField] float duracionFundido = 1.5f;
    [SerializeField] float pausaEntreCanciones = 1.5f;       // silencio entre una y otra

    static MusicaPartida instancia;
    AudioSource fuente;
    Coroutine rutina;
    int[] orden;
    int siguiente;
    float ganancia;   // volumen actual, con los fundidos: lo lee el hilo de audio
    float compensacion;   // copia de compensacionBafle para el hilo de audio

    void Awake()
    {
        instancia = this;
        fuente = GetComponent<AudioSource>();
        fuente.playOnAwake = false;
        fuente.loop = false;
        fuente.spatialBlend = 0f;   // 2D
        fuente.volume = 1f;
        compensacion = compensacionBafle;
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
            ganancia = 0f;
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
            ganancia = volumen;
            fuente.Play();
        }
    }

    IEnumerator Fundir(float objetivo)
    {
        float inicial = ganancia;
        for (float t = 0f; t < duracionFundido; t += Time.unscaledDeltaTime)
        {
            ganancia = Mathf.Lerp(inicial, objetivo, t / duracionFundido);
            yield return null;
        }
        ganancia = objetivo;
        if (objetivo == 0f)
        {
            fuente.Stop();
            rutina = null;
        }
    }

    void OnAudioFilterRead(float[] datos, int canales)
    {
        float g = ganancia * compensacion;
        for (int i = 0; i < datos.Length; i++) datos[i] *= g;
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
