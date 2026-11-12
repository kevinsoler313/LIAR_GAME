using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class ControladorRocola : MonoBehaviour
{
    [Tooltip("Arrastra aquí tus canciones")]
    public AudioClip[] canciones;

    private AudioSource reproductor;
    private int indiceActual = 0;

    void Start()
    {
        reproductor = GetComponent<AudioSource>();

        // Evita que la canción se repita sobre sí misma
        reproductor.loop = false;

        if (canciones.Length > 0)
        {
            ReproducirCancion();
        }
    }

    void Update()
    {
        // Si el reproductor no está sonando, pasa a la siguiente pista
        if (!reproductor.isPlaying && canciones.Length > 0)
        {
            indiceActual++;

            // Si llega al final de la lista, vuelve a empezar
            if (indiceActual >= canciones.Length)
            {
                indiceActual = 0;
            }

            ReproducirCancion();
        }
    }

    void ReproducirCancion()
    {
        reproductor.clip = canciones[indiceActual];
        reproductor.Play();
    }
}