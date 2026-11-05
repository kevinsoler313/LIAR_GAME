using System.Collections;
using UnityEngine;

// Ambiente del bar: cada tanto suena una jugada de billar en una mesa al fondo del cuarto
// y botellas chocando detrás de la barra, en otra esquina. Todo al azar, bajito y en 3D,
// para que el bar se sienta vivo sin tapar los sonidos de las cartas.
// Cada grupo sale de su propio punto del cuarto (los hijos "Mesa de billar" y "Barra").
public class AmbienteBar : MonoBehaviour
{
    [System.Serializable]
    class Grupo
    {
        public string nombre;
        public Transform punto;                     // de dónde sale el sonido
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volumen = 0.3f;
        public Vector2 espera = new Vector2(6f, 15f);   // segundos entre uno y otro (mínimo, máximo)
        [Range(0f, 1f)] public float probabilidadDoble = 0f;   // a veces suenan dos seguidos
        public float variacionTono = 0.05f;
        public float corteAgudos = 22000f;          // < 22000 = suena apagado, como si estuviera lejos
        [HideInInspector] public AudioSource fuente;
        [HideInInspector] public int ultimo = -1;
    }

    [SerializeField] Grupo billar = new Grupo { nombre = "Billar", volumen = 0.35f, espera = new Vector2(8f, 18f), corteAgudos = 5000f };
    [SerializeField] Grupo botellas = new Grupo { nombre = "Botellas", volumen = 0.25f, espera = new Vector2(3f, 9f), probabilidadDoble = 0.3f };

    [Header("Distancia")]
    [SerializeField] float distanciaMinima = 3f;    // hasta aquí suena a volumen completo
    [SerializeField] float distanciaMaxima = 20f;
    [SerializeField, Range(0f, 1f)] float sonido3D = 0.8f;   // 1 = totalmente posicional, 0 = igual en ambos oídos

    void Start()
    {
        Preparar(billar);
        Preparar(botellas);
        StartCoroutine(Tocar(billar));
        StartCoroutine(Tocar(botellas));
    }

    void Preparar(Grupo g)
    {
        var go = g.punto != null ? g.punto.gameObject : gameObject;
        g.fuente = go.AddComponent<AudioSource>();
        g.fuente.playOnAwake = false;
        g.fuente.spatialBlend = sonido3D;
        g.fuente.rolloffMode = AudioRolloffMode.Logarithmic;
        g.fuente.minDistance = distanciaMinima;
        g.fuente.maxDistance = distanciaMaxima;
        g.fuente.dopplerLevel = 0f;
        if (g.corteAgudos < 22000f)
        {
            var filtro = go.AddComponent<AudioLowPassFilter>();
            filtro.cutoffFrequency = g.corteAgudos;
        }
    }

    IEnumerator Tocar(Grupo g)
    {
        if (g.clips == null || g.clips.Length == 0) yield break;
        // que no arranquen los dos a la vez al entrar a la escena
        yield return new WaitForSeconds(Random.Range(1f, g.espera.y * 0.5f));
        while (true)
        {
            Uno(g);
            if (Random.value < g.probabilidadDoble)
            {
                yield return new WaitForSeconds(Random.Range(0.25f, 0.8f));
                Uno(g);
            }
            yield return new WaitForSeconds(Random.Range(g.espera.x, g.espera.y));
        }
    }

    // Un clip al azar, sin repetir el anterior
    void Uno(Grupo g)
    {
        int i = Random.Range(0, g.clips.Length);
        if (g.clips.Length > 1 && i == g.ultimo) i = (i + 1) % g.clips.Length;
        g.ultimo = i;
        g.fuente.pitch = 1f + Random.Range(-g.variacionTono, g.variacionTono);
        g.fuente.PlayOneShot(g.clips[i], g.volumen * Random.Range(0.75f, 1f));
    }
}
