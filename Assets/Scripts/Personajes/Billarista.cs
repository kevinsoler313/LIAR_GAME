using UnityEngine;

// Cliente jugando billar con su taco: el que tira repite su tiro (amagues, tiro y mira la jugada)
// y los que esperan se apoyan en el taco. Cada uno empieza en otro punto y a un ritmo apenas distinto.
// Al que tira se le da la bola blanca: cuando la punta del taco llega, sale rodando hacia la banda del fondo,
// rebota y vuelve a su lugar antes del siguiente tiro (así el loop no salta).
// Es solo ambiente: no tiene nada que ver con la partida ni con la red.
public class Billarista : MonoBehaviour
{
    [SerializeField] Animator animador;
    [SerializeField] string accion = "Esperar_Taco";   // Tirar o Esperar_Taco

    [Header("Solo el que tira")]
    [SerializeField] Transform bola;                     // la blanca de la mesa (Bola_0)
    [SerializeField] float momentoTiro = 84f / 192f;     // en el loop de Tirar, cuando la punta toca la bola
    [SerializeField] float hastaLaBanda = 0.48f;         // de la bola a la banda del fondo (metros)
    [SerializeField] float segundosRodando = 2.5f;       // ida y vuelta

    Vector3 inicio;
    Quaternion giroInicio;
    float radio;

    void Start()
    {
        animador.speed = Random.Range(0.93f, 1.07f);
        animador.Play(accion, 0, bola != null ? 0f : Random.value);   // el que tira arranca con la bola en su lugar
        if (bola == null) return;
        inicio = bola.position;
        giroInicio = bola.rotation;
        var r = bola.GetComponent<Renderer>();
        radio = r != null ? Mathf.Max(0.005f, r.bounds.extents.y) : 0.025f;
    }

    void Update()
    {
        if (bola == null) return;
        var estado = animador.GetCurrentAnimatorStateInfo(0);
        if (!estado.IsName(accion)) return;

        // segundos desde el último tiro (antes del primero queda quieta en su lugar)
        float desde = estado.normalizedTime - momentoTiro;
        float segundos = desde < 0f ? float.MaxValue : (desde % 1f) * estado.length;
        float t = Mathf.Clamp01(segundos / segundosRodando);
        float recorrido = 2f * hastaLaBanda * (1f - (1f - t) * (1f - t));   // sale rápido y se va frenando
        float avance = recorrido <= hastaLaBanda ? recorrido : 2f * hastaLaBanda - recorrido;

        var direccion = transform.forward;
        direccion.y = 0f;
        direccion.Normalize();
        bola.position = inicio + direccion * avance;
        var eje = Vector3.Cross(Vector3.up, direccion);
        bola.rotation = Quaternion.AngleAxis(avance / radio * Mathf.Rad2Deg, eje) * giroInicio;
    }
}
