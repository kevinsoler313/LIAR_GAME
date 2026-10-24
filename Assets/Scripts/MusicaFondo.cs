using UnityEngine;

// Música de fondo de todo el juego. Va en MainMenu y en Bar:
// la primera que aparece se queda viva entre escenas (DontDestroyOnLoad)
// y cualquier copia que aparezca después se borra, así la canción nunca se corta ni suena doble.
[RequireComponent(typeof(AudioSource))]
public class MusicaFondo : MonoBehaviour
{
    [SerializeField] AudioClip cancion;
    [SerializeField, Range(0f, 1f)] float volumen = 0.35f;   // de fondo: que no tape los efectos

    static MusicaFondo instancia;

    void Awake()
    {
        if (instancia != null)
        {
            Destroy(gameObject);   // ya hay una sonando desde otra escena
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);

        var fuente = GetComponent<AudioSource>();
        fuente.clip = cancion;
        fuente.volume = volumen;
        fuente.loop = true;            // vuelve a empezar al terminar
        fuente.playOnAwake = false;
        fuente.spatialBlend = 0f;      // 2D: se oye igual en cualquier silla
        fuente.Play();
    }

    void OnDestroy()
    {
        if (instancia == this) instancia = null;
    }
}
