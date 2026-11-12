using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class InteraccionBotella : MonoBehaviour
{
    private AudioSource fuenteAudio;

    void Start()
    {
        // Captura el AudioSource automáticamente al iniciar
        fuenteAudio = GetComponent<AudioSource>();
    }

    // Unity llama a este método automáticamente cuando haces clic sobre el objeto
    void OnMouseDown()
    {
        ReproducirSonido();
    }

    // Llama a este método si usas un sistema de Raycast en lugar de clics del ratón
    public void ReproducirSonido()
    {
        if (fuenteAudio != null && !fuenteAudio.isPlaying)
        {
            fuenteAudio.Play();
        }
    }
}