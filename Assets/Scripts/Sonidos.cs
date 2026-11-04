using System.Collections.Generic;
using UnityEngine;

// Efectos de sonido del juego (botones, cartas, turnos). No hace falta ponerlo en ninguna escena:
// la primera vez que se pide un sonido crea sus propios AudioSource y los mantiene vivos entre escenas.
// Los clips salen de Assets/Resources/Sonidos/<nombre>.wav; para cambiar uno basta con reemplazar ese archivo.
public static class Sonidos
{
    // Botones del menú
    public static void Hover() => Tocar("Hover", 0.35f, 0.06f);
    public static void Clic() => Tocar("Clic", 0.7f);

    // Cartas
    public static void Repartir() => Tocar("Repartir", 0.35f, 0.12f);
    public static void Levantar() => Tocar("Levantar", 0.3f, 0.1f);
    public static void Bajar() => Tocar("Bajar", 0.3f, 0.1f);
    public static void Seleccionar() => Tocar("Seleccionar", 0.4f, 0.08f);
    public static void Tirar() => Tocar("Tirar", 0.7f, 0.08f);

    // Turnos
    public static void Turno() => Tocar("Turno", 0.45f);
    public static void TuTurno() => Tocar("TuTurno", 0.55f);

    // "¡Mentiroso!"
    public static void Mentiroso() => Tocar("Mentiroso", 1f);

    // Varias fuentes que se turnan: al repartir suenan muchas cartas seguidas
    // y cada una puede llevar su propio tono sin cambiarle el tono a las que siguen sonando.
    const int cantidadFuentes = 8;
    static AudioSource[] fuentes;
    static int siguiente;
    static readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();

    static void Tocar(string nombre, float volumen, float variacionTono = 0f)
    {
        if (!clips.TryGetValue(nombre, out var clip))
        {
            clip = Resources.Load<AudioClip>("Sonidos/" + nombre);
            clips[nombre] = clip;
        }
        if (clip == null) return;

        if (fuentes == null || fuentes[0] == null) CrearFuentes();

        var fuente = fuentes[siguiente];
        siguiente = (siguiente + 1) % cantidadFuentes;
        fuente.pitch = 1f + Random.Range(-variacionTono, variacionTono);   // pequeño cambio para que no suene robótico
        fuente.PlayOneShot(clip, volumen);
    }

    static void CrearFuentes()
    {
        var go = new GameObject("Sonidos");
        Object.DontDestroyOnLoad(go);
        fuentes = new AudioSource[cantidadFuentes];
        for (int i = 0; i < cantidadFuentes; i++)
        {
            var f = go.AddComponent<AudioSource>();
            f.playOnAwake = false;
            f.spatialBlend = 0f;           // 2D
            f.ignoreListenerPause = true;  // que suene también con el juego en pausa
            fuentes[i] = f;
        }
    }
}
