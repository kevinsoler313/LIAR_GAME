using LiarGame.Red;
using LiarGame.Red.Protocolo;
using UnityEngine;
using UnityEngine.Rendering;

// Los 4 personajes animados, uno por silla y en el mismo orden que SillasRed (Puesto_0..3).
// No cambia nada de la partida: solo mira lo que pasa y lo actúa.
//  - Animación: Esperar o MiTurno según el turno de PartidaRed, Ganar si ganó la partida,
//    Jugar cuando le sale una carta de la mano, Comer cuando le entran cartas (castigo) y Acusar si gritó "¡Mentiroso!".
//  - Cartas: el ancla "Mano en mano" de cada silla sigue la mano del personaje, así las cartas del juego quedan en sus manos.
//  - Mirada: gira la cabeza hacia quien juega, hacia el monto cuando cae una carta y hacia el acusado.
//  - La cámara de este equipo va dentro de la cabeza del personaje de su silla: ese queda solo con sombra.
public class PersonajesEnSillas : MonoBehaviour
{
    [SerializeField] GameObject[] personajes;       // mallas de Puesto_0..3: MujerS, MujerC, Inseguro, Viejo
    [SerializeField] Animator[] animadores;
    [SerializeField] Transform[] cabezas;           // hueso "head" de cada uno
    [SerializeField] Transform[] manosIzquierdas;   // hueso "hand.L": la que sostiene las cartas
    [SerializeField] Transform[] puestos;           // PUESTOS/Puesto_0..3 (su +Z mira al centro de la mesa)
    [SerializeField] Transform[] anclasCartas;      // Puesto_0..3/Ojos/Mano en mano: ahí arma ManoJugador la fila de cada silla

    [Header("Juego (solo se lee)")]
    [SerializeField] PartidaRed partida;
    [SerializeField] MesaRed mesa;
    [SerializeField] RedJuego red;
    [SerializeField] ManoJugador[] manos;           // Mano_0..3
    [SerializeField] Transform monto;

    [Header("Ajustes")]
    [SerializeField] bool ocultarSillasVacias = true;   // conectado: solo se ven los personajes de las sillas ocupadas
    [SerializeField] Vector3 cartasDesdeMano = new Vector3(0.14f, 0.05f, 0.05f);   // (derecha, arriba, adelante) del puesto
    [SerializeField] float velocidadCartas = 12f;
    [SerializeField] float giroCabezaMax = 55f;
    [SerializeField] float velocidadMirada = 4f;
    [SerializeField] float segundosMirarJugada = 1.8f;
    [SerializeField] float segundosAcusacion = 3f;

    static readonly int miTurno = Animator.StringToHash("MiTurno");
    static readonly int gano = Animator.StringToHash("Gano");
    static readonly int jugar = Animator.StringToHash("Jugar");
    static readonly int comer = Animator.StringToHash("Comer");
    static readonly int acusar = Animator.StringToHash("Acusar");

    Renderer[][] mallas;
    bool[] visibles;
    int[] cartasAntes;
    Vector3[] anclaOriginal;     // localPosition de cada ancla, para devolverla si el personaje no está
    Vector3[] frenteCabeza;      // hacia dónde mira cada cabeza, en su propio espacio (se mide al empezar)
    Vector3[] miradaActual;
    float[] pesoMirada;
    int sillaOculta = -2;        // -2 = todavía no se ha aplicado nada

    int ultimaJugada = -1;       // silla que tiró la última carta (los demás miran el monto un momento)
    float finMirarJugada;
    int acusador = -1, acusado = -1;
    float finAcusacion;

    int Cantidad => personajes.Length;

    void Awake()
    {
        mallas = new Renderer[Cantidad][];
        visibles = new bool[Cantidad];
        cartasAntes = new int[Cantidad];
        anclaOriginal = new Vector3[Cantidad];
        frenteCabeza = new Vector3[Cantidad];
        miradaActual = new Vector3[Cantidad];
        pesoMirada = new float[Cantidad];
        for (int i = 0; i < Cantidad; i++)
        {
            mallas[i] = personajes[i].GetComponentsInChildren<Renderer>(true);
            visibles[i] = true;
            if (anclasCartas[i] != null) anclaOriginal[i] = anclasCartas[i].localPosition;
            // en la pose de reposo la cabeza mira al frente del puesto
            frenteCabeza[i] = Quaternion.Inverse(cabezas[i].rotation) * puestos[i].forward;
            miradaActual[i] = cabezas[i].position + puestos[i].forward;
        }
    }

    void OnEnable() { if (red != null) red.Escuchar(TiposJuego.Acusacion, AlAcusar, Origen.Host); }
    void OnDisable() { if (red != null) red.DejarDeEscuchar(TiposJuego.Acusacion, AlAcusar); }

    // "¡Mentiroso!": el que acusó golpea la mesa y todos miran al acusado
    void AlAcusar(Mensaje m)
    {
        var d = m.Leer<DatosAcusacion>();
        acusador = d.acusador;
        acusado = d.acusado;
        finAcusacion = Time.time + segundosAcusacion;
        if (acusador >= 0 && acusador < Cantidad) animadores[acusador].SetTrigger(acusar);
    }

    void Update()
    {
        int local = SillasRed.Instancia != null ? SillasRed.Instancia.SillaLocal : -1;
        bool[] ocupadas = ocultarSillasVacias && red != null && red.Conectado && mesa != null ? mesa.SillasOcupadas() : null;
        bool enJuego = partida != null && partida.EnJuego;

        for (int i = 0; i < Cantidad; i++)
        {
            Mostrar(i, ocupadas == null || ocupadas[i]);

            var animador = animadores[i];
            animador.SetBool(miTurno, enJuego && partida.Turno == i);
            animador.SetBool(gano, partida != null && !enJuego && partida.Ganador == i);

            // Una carta menos: la jugó. Varias de golpe: es un reparto nuevo que vació la mano.
            // Más cartas con la mano ya armada: se las comió por una acusación.
            int ahora = manos[i] != null ? manos[i].Cartas.Count : 0;
            if (cartasAntes[i] > 0 && ahora == cartasAntes[i] - 1)
            {
                animador.SetTrigger(jugar);
                ultimaJugada = i;
                finMirarJugada = Time.time + segundosMirarJugada;
            }
            else if (cartasAntes[i] > 0 && ahora > cartasAntes[i]) animador.SetTrigger(comer);
            cartasAntes[i] = ahora;
        }

        if (local != sillaOculta)
        {
            sillaOculta = local;
            for (int i = 0; i < Cantidad; i++)
                foreach (var malla in mallas[i])
                    malla.shadowCastingMode = i == local ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
        }
    }

    void Mostrar(int i, bool visible)
    {
        if (visibles[i] == visible) return;
        visibles[i] = visible;
        foreach (var malla in mallas[i]) malla.enabled = visible;
    }

    // Después del Animator: las cartas siguen la mano y la cabeza se gira hacia lo que pasa en la mesa
    void LateUpdate()
    {
        int local = SillasRed.Instancia != null ? SillasRed.Instancia.SillaLocal : -1;
        float paso = 1f - Mathf.Exp(-velocidadCartas * Time.deltaTime);
        float pasoMirada = 1f - Mathf.Exp(-velocidadMirada * Time.deltaTime);

        for (int i = 0; i < Cantidad; i++)
        {
            var ancla = anclasCartas[i];
            if (ancla != null)
            {
                // la silla local no usa este ancla (sus cartas van pegadas a la cámara)
                if (visibles[i] && i != local)
                {
                    var p = puestos[i];
                    var destino = manosIzquierdas[i].position + p.right * cartasDesdeMano.x
                                + Vector3.up * cartasDesdeMano.y + p.forward * cartasDesdeMano.z;
                    ancla.position = Vector3.Lerp(ancla.position, destino, paso);
                }
                else ancla.localPosition = anclaOriginal[i];
            }

            if (!visibles[i]) continue;
            bool hay = ObjetivoMirada(i, out var objetivo);
            pesoMirada[i] = Mathf.Lerp(pesoMirada[i], hay ? 1f : 0f, pasoMirada);
            if (hay) miradaActual[i] = Vector3.Lerp(miradaActual[i], objetivo, pasoMirada);
            if (pesoMirada[i] < 0.01f) continue;

            var cabeza = cabezas[i];
            var actual = cabeza.rotation * frenteCabeza[i];
            var giro = Quaternion.FromToRotation(actual, miradaActual[i] - cabeza.position);
            giro = Quaternion.RotateTowards(Quaternion.identity, giro, giroCabezaMax);
            cabeza.rotation = Quaternion.Slerp(Quaternion.identity, giro, pesoMirada[i]) * cabeza.rotation;
        }
    }

    // Hacia dónde mira el personaje de la silla i (false = deja la mirada de su animación)
    bool ObjetivoMirada(int i, out Vector3 objetivo)
    {
        objetivo = Vector3.zero;
        if (Time.time < finAcusacion && acusado >= 0 && acusador >= 0)
        {
            objetivo = cabezas[i == acusado ? acusador : acusado].position;
            return true;
        }
        if (Time.time < finMirarJugada && ultimaJugada >= 0 && ultimaJugada != i && monto != null)
        {
            objetivo = monto.position;
            return true;
        }
        if (partida == null) return false;
        if (partida.EnJuego && partida.Turno >= 0 && partida.Turno != i)
        {
            objetivo = cabezas[partida.Turno].position;
            return true;
        }
        if (!partida.EnJuego && partida.Ganador >= 0 && partida.Ganador != i)
        {
            objetivo = cabezas[partida.Ganador].position;
            return true;
        }
        return false;
    }
}
