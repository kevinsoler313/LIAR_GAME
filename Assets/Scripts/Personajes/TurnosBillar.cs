using UnityEngine;

// Va en el objeto "Billar" (centro de la mesa; su Z es el largo). Los jugadores tiran por turnos:
// al que le toca se inclina y tira; cuando la punta del taco toca la blanca, la bola sale en la dirección del tiro,
// rebota en la banda si hace falta y se detiene donde tira el siguiente. Después le toca a ese, y así en rueda.
// Es solo ambiente: nada que ver con la partida ni con la red.
public class TurnosBillar : MonoBehaviour
{
    [SerializeField] Billarista[] jugadores;          // en el orden en que tiran
    [SerializeField] Vector3[] puntosBola;            // dónde queda la blanca para el tiro de cada uno (local a este objeto)
    [SerializeField] Transform bola;                  // la blanca (no puede ser estática: si no, Unity no la mueve)
    [SerializeField] Vector2 bandas = new Vector2(0.51f, 0.98f);   // hasta dónde llega el centro de la bola (x, z locales)
    [SerializeField] float momentoTiro = 84f / 192f;  // en el loop de Tirar, cuando la punta toca la bola
    [SerializeField] float finTiro = 135f / 192f;     // ya vio la jugada: vuelve a esperar
    [SerializeField] float velocidad = 1.3f;          // metros por segundo al salir (se va frenando)
    [SerializeField] Vector2 segundosEntreTurnos = new Vector2(1f, 2.5f);

    int turno;
    bool tiro, volvio;
    float proximoTurno;
    Vector3[] camino;            // puntos locales que recorre la bola en este tiro
    float largoCamino, inicioRodar, duracionRodar;
    float radio;

    void Start()
    {
        var r = bola.GetComponent<Renderer>();
        radio = r != null ? Mathf.Max(0.005f, r.bounds.extents.y) : 0.03f;
        bola.position = transform.TransformPoint(puntosBola[0]);
        proximoTurno = Time.time + Random.Range(1f, 3f);
        turno = -1;
    }

    void Update()
    {
        if (turno < 0)
        {
            if (Time.time >= proximoTurno) Empezar(0);
            return;
        }
        var jugador = jugadores[turno];
        float avance = jugador.AvanceTiro;
        if (!tiro && avance >= momentoTiro) Golpe();
        if (tiro && !volvio && avance >= finTiro)
        {
            volvio = true;
            jugador.Esperar();
        }
        if (tiro) Rodar();
        if (tiro && volvio && Time.time >= inicioRodar + duracionRodar && Time.time >= proximoTurno)
            Empezar((turno + 1) % jugadores.Length);
    }

    void Empezar(int i)
    {
        turno = i;
        tiro = volvio = false;
        bola.position = transform.TransformPoint(puntosBola[i]);   // por si quedó corrida
        jugadores[i].Tirar();
    }

    // sale derecho hacia donde mira el que tira; si el siguiente punto no queda en esa línea, rebota en la banda
    void Golpe()
    {
        tiro = true;
        var desde = puntosBola[turno];
        var hasta = puntosBola[(turno + 1) % jugadores.Length];
        var dir = transform.InverseTransformDirection(jugadores[turno].transform.forward);
        dir.y = 0f;
        dir.Normalize();
        var alSiguiente = hasta - desde;
        float enLinea = Vector3.Dot(alSiguiente, dir);
        if (enLinea > 0f && (alSiguiente - dir * enLinea).magnitude < 0.03f)
            camino = new[] { desde, hasta };
        else
            camino = new[] { desde, Banda(desde, dir), hasta };
        largoCamino = 0f;
        for (int k = 1; k < camino.Length; k++) largoCamino += Vector3.Distance(camino[k - 1], camino[k]);
        duracionRodar = 2f * largoCamino / velocidad;    // frenando parejo: arranca a "velocidad" y llega en cero
        inicioRodar = Time.time;
        proximoTurno = Time.time + duracionRodar + Random.Range(segundosEntreTurnos.x, segundosEntreTurnos.y);
    }

    Vector3 Banda(Vector3 desde, Vector3 dir)
    {
        float t = float.MaxValue;
        if (Mathf.Abs(dir.x) > 1e-4f) t = Mathf.Min(t, ((dir.x > 0 ? bandas.x : -bandas.x) - desde.x) / dir.x);
        if (Mathf.Abs(dir.z) > 1e-4f) t = Mathf.Min(t, ((dir.z > 0 ? bandas.y : -bandas.y) - desde.z) / dir.z);
        return desde + dir * t;
    }

    void Rodar()
    {
        float u = Mathf.Clamp01((Time.time - inicioRodar) / duracionRodar);
        float recorrido = largoCamino * (1f - (1f - u) * (1f - u));
        float resto = recorrido;
        var p = camino[camino.Length - 1];
        for (int k = 1; k < camino.Length; k++)
        {
            float tramo = Vector3.Distance(camino[k - 1], camino[k]);
            if (resto <= tramo)
            {
                p = Vector3.Lerp(camino[k - 1], camino[k], tramo > 0f ? resto / tramo : 1f);
                break;
            }
            resto -= tramo;
        }
        var antes = bola.position;
        bola.position = transform.TransformPoint(p);
        var mov = bola.position - antes;
        mov.y = 0f;
        if (mov.sqrMagnitude > 1e-8f)
            bola.rotation = Quaternion.AngleAxis(mov.magnitude / radio * Mathf.Rad2Deg, Vector3.Cross(Vector3.up, mov.normalized)) * bola.rotation;
    }
}
