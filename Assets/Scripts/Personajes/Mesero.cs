using UnityEngine;

// El mesero recorre la ruta (los hijos de "ruta", en orden y en bucle) con su bandeja de cervezas.
// Según el nombre de cada punto:
//  - "Barra...": espera ahí un rato (le sirven) y el bartender lo saluda.
//  - "Mesa...":  entrega (baja la bandeja) mirando hacia donde apunta el punto (su +Z) y espera un momento.
//  - "Paso...":  solo pasa por ahí, para esquivar la mesa del juego, las sillas o la barra.
// Para las mesas nuevas: mover los puntos "Mesa" junto a cada mesa y girarlos hacia ella (o duplicarlos).
// Es solo ambiente: no tiene nada que ver con la partida ni con la red.
public class Mesero : MonoBehaviour
{
    [SerializeField] Animator animador;
    [SerializeField] Transform ruta;
    [SerializeField] Bartender bartender;
    [SerializeField] float velocidad = 0.47f;          // al paso de su animación: así los pies no patinan
    [SerializeField] float velocidadGiro = 220f;       // grados por segundo
    [SerializeField] Vector2 segundosEnBarra = new Vector2(3f, 6f);
    [SerializeField] Vector2 segundosEnMesa = new Vector2(1.5f, 3f);

    enum Fase { Caminando, Girando, Entregando, Esperando }

    const string caminar = "Caminar", esperar = "Esperar", entregar = "Entregar";

    int punto;
    Fase fase;
    float finEspera;

    void Start()
    {
        punto = 0;
        Caminar();
    }

    void Update()
    {
        if (ruta == null || ruta.childCount == 0) return;
        var destino = ruta.GetChild(punto);
        switch (fase)
        {
            case Fase.Caminando:
                var falta = destino.position - transform.position;
                falta.y = 0;
                if (falta.magnitude < 0.03f)
                {
                    Llegar(destino);
                    break;
                }
                // en las esquinas gira casi en el sitio antes de seguir avanzando
                float angulo = Girar(falta);
                float avance = velocidad * Time.deltaTime * Mathf.Clamp01(1f - (angulo - 25f) / 60f);
                transform.position += falta.normalized * Mathf.Min(avance, falta.magnitude);
                break;

            case Fase.Girando:
                if (Girar(destino.forward) < 3f) Atender(destino);
                break;

            case Fase.Entregando:
                var estado = animador.GetCurrentAnimatorStateInfo(0);
                if (estado.IsName(entregar) && estado.normalizedTime >= 0.95f) Esperar(segundosEnMesa);
                break;

            case Fase.Esperando:
                if (Time.time >= finEspera) Siguiente();
                break;
        }
    }

    void Llegar(Transform destino)
    {
        if (destino.name.StartsWith("Paso"))
        {
            Siguiente();
            return;
        }
        fase = Fase.Girando;
        animador.CrossFadeInFixedTime(esperar, 0.3f);
    }

    void Atender(Transform destino)
    {
        if (destino.name.StartsWith("Barra"))
        {
            if (bartender) bartender.Saludar();
            Esperar(segundosEnBarra);
        }
        else
        {
            fase = Fase.Entregando;
            animador.CrossFadeInFixedTime(entregar, 0.25f);
        }
    }

    void Esperar(Vector2 segundos)
    {
        fase = Fase.Esperando;
        finEspera = Time.time + Random.Range(segundos.x, segundos.y);
        animador.CrossFadeInFixedTime(esperar, 0.35f);
    }

    void Siguiente()
    {
        punto = (punto + 1) % ruta.childCount;
        Caminar();
    }

    void Caminar()
    {
        fase = Fase.Caminando;
        animador.CrossFadeInFixedTime(caminar, 0.3f);
    }

    // gira hacia la dirección y devuelve cuántos grados le faltaban
    float Girar(Vector3 direccion)
    {
        direccion.y = 0;
        if (direccion.sqrMagnitude < 1e-6f) return 0f;
        var objetivo = Quaternion.LookRotation(direccion);
        float angulo = Quaternion.Angle(transform.rotation, objetivo);
        transform.rotation = Quaternion.RotateTowards(transform.rotation, objetivo, velocidadGiro * Time.deltaTime);
        return angulo;
    }
}
