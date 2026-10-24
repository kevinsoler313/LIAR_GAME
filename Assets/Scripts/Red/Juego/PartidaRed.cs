using System.Collections.Generic;
using LiarGame.Red;
using LiarGame.Red.Protocolo;
using UnityEngine;
using UnityEngine.InputSystem;

// El cerebro de la partida en red. Hay una copia en cada equipo,
// pero solo el host toma decisiones: baraja, reparte, valida jugadas, maneja los turnos y juzga las acusaciones.
//
// Reglas (las de la landing):
//   - Abre quien tenga la carta con el número 1. Después el turno pasa a la derecha (silla siguiente).
//   - En tu turno tiras UNA carta al monto, boca abajo. Puedes mentir.
//   - Un poder (Pass, Return, Break) NO gasta el turno: lo tiras y luego tiras tu carta al monto.
//   - Cualquiera (menos quien la tiró) puede gritar "¡Mentiroso!" (tecla M) a la ÚLTIMA carta del monto.
//       Si era mentira, el mentiroso se come 3 cartas del mazo. Si era verdad, se las come el acusador.
//   - Quien se queda sin cartas gana… si nadie lo acusa en los segundos siguientes.
//
// Cómo viaja cada cosa:
//   Jugar:   jugador → "juego.jugar" → host valida → aplica en su equipo → "juego.jugada" a los demás.
//   Acusar:  jugador → "juego.acusar" → host revisa la carta real → aplica en su equipo → "juego.acusacion" a los demás
//            (+ "juego.comer" en privado al castigado, para que vea qué cartas se comió).
//   El host aplica primero en su propio equipo y DESPUÉS avisa: así, si llegan dos pedidos seguidos,
//   el segundo ya se valida con el turno y la escalera actualizados.
public class PartidaRed : MonoBehaviour
{
    [SerializeField] RedJuego red;
    [SerializeField] MesaRed mesa;
    [SerializeField] Baraja baraja;
    [SerializeField] int cartasPorJugador = 8;     // el mismo número que tiene tu Repartidor
    [SerializeField] GameObject botonEmpezar;
    [SerializeField] MazoMesa mazoMesa;
    [SerializeField] Repartidor repartidor;

    [Header("Jugadas")]
    [SerializeField] ManoJugador[] manos;          // Mano_0..3, en el orden de las sillas
    [SerializeField] ReglasPoderes reglas;         // aplica el efecto de los poderes
    [SerializeField] EstadoPartida estado;         // la escalera
    [SerializeField] AvisoPoder aviso;             // carteles: rechazos y resultado de las acusaciones
    [SerializeField] Monto montoCentro;            // el monto boca abajo (para voltear la carta acusada)

    [Header("Reglas")]
    [SerializeField] int cartasCastigo = 3;        // las que se come quien pierde una acusación
    [SerializeField] float segundosParaAcusar = 5f;   // tiempo para acusar a quien se quedó sin cartas

    // ================================================================ Lo que todos saben

    public bool EnJuego { get; private set; }
    public int Turno { get; private set; } = -1;    // silla que juega (-1 = nadie)
    public int Ganador { get; private set; } = -1;  // silla que ganó la última partida (-1 = nadie)

    // La última carta que cayó al monto: quién la tiró, qué número "decía" ser, y si todavía se puede acusar
    public int UltimaSilla { get; private set; } = -1;
    public int NumeroDicho { get; private set; }
    public bool HayAcusable { get; private set; }

    public int MiSilla => mesa.MiSilla;
    public bool EsMiTurno => EnJuego && Turno >= 0 && Turno == MiSilla;
    public bool PuedoAcusar => EnJuego && HayAcusable && MiSilla >= 0 && UltimaSilla != MiSilla;
    public bool EsperandoAcusacionFinal => EnJuego && Turno < 0 && UltimaSilla >= 0;   // alguien se quedó sin cartas

    bool acusacionEnviada;   // para no mandar dos "¡Mentiroso!" seguidos
    int[] misComidas;        // ids de las cartas que me como (llegan en privado justo antes de la acusación)
    int[] cartasPorRevelar;  // ids de mi mano en el reparto

    // ================================================================ SECRETO: solo tiene datos en el host

    readonly List<int>[] manosIds = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
    readonly List<int> montoIds = new List<int>();   // las cartas reales del monto, en orden
    readonly List<int> mazoIds = new List<int>();    // lo que quedó en el mazo después de repartir (de aquí se come)
    bool[] ocupadasPartida = new bool[MesaRed.CantidadSillas];

    int cartaAcusable = -1;      // id real de la última carta del monto
    int esperadoAcusable;        // el número que tocaba cuando se tiró
    int posibleGanador = -1;     // silla que se quedó sin cartas y espera a ver si la acusan
    float finEspera;             // hasta cuándo se puede acusar a posibleGanador

    // ================================================================ Suscripciones

    void OnEnable()
    {
        red.Escuchar(TiposJuego.Estado, AlRecibirEstado, Origen.Host);
        red.Escuchar(TiposJuego.Reparto, RecibirReparto, Origen.Host);
        red.Escuchar(TiposJuego.Jugar, AlPedirJugada, Origen.Cualquiera);      // cualquiera pide; el host valida
        red.Escuchar(TiposJuego.Jugada, AlRecibirJugada, Origen.Host);
        red.Escuchar(TiposJuego.Rechazo, AlRecibirRechazo, Origen.Host);
        red.Escuchar(TiposJuego.Acusar, AlPedirAcusacion, Origen.Cualquiera);  // cualquiera acusa; el host juzga
        red.Escuchar(TiposJuego.Acusacion, AlRecibirAcusacion, Origen.Host);
        red.Escuchar(TiposJuego.Comer, AlRecibirComer, Origen.Host);
        red.AlConectarse += AlConectarse;
        red.AlDesconectarse += AlDesconectarse;
    }

    void OnDisable()
    {
        red.DejarDeEscuchar(TiposJuego.Estado, AlRecibirEstado);
        red.DejarDeEscuchar(TiposJuego.Reparto, RecibirReparto);
        red.DejarDeEscuchar(TiposJuego.Jugar, AlPedirJugada);
        red.DejarDeEscuchar(TiposJuego.Jugada, AlRecibirJugada);
        red.DejarDeEscuchar(TiposJuego.Rechazo, AlRecibirRechazo);
        red.DejarDeEscuchar(TiposJuego.Acusar, AlPedirAcusacion);
        red.DejarDeEscuchar(TiposJuego.Acusacion, AlRecibirAcusacion);
        red.DejarDeEscuchar(TiposJuego.Comer, AlRecibirComer);
        red.AlConectarse -= AlConectarse;
        red.AlDesconectarse -= AlDesconectarse;
    }

    // Conectado: las manos dejan de jugar solas y le piden al host
    void AlConectarse()
    {
        foreach (var m in manos) m.pedirAlHost = PedirAlHost;
    }

    // ================================================================ Cada frame

    void Update()
    {
        // Botón EMPEZAR: solo el host, sin partida en curso y con al menos 2 jugadores
        if (botonEmpezar != null)
        {
            bool mostrar = red.Conectado && red.EsHost && !EnJuego && mesa.CantidadJugadores >= 2;
            if (botonEmpezar.activeSelf != mostrar) botonEmpezar.SetActive(mostrar);
        }

        // Tecla M: "¡Mentiroso!"
        if (red.Conectado && PuedoAcusar && !acusacionEnviada && !SelectorOpciones.BloqueaEntrada
            && Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame)
            Acusar();

        // HOST: se acabó el tiempo para acusar a quien se quedó sin cartas → gana
        if (red.EsHost && EnJuego && posibleGanador >= 0 && Time.time >= finEspera)
            Terminar(posibleGanador);
    }

    // ================================================================ Empezar y repartir (host)

    // Lo llama el botón EMPEZAR (solo existe en el host).
    public void Empezar()
    {
        if (!red.EsHost || EnJuego) return;   // nadie más puede empezar, y no dos veces

        ocupadasPartida = mesa.SillasOcupadas();
        var mazo = baraja.MezclarIds();       // todo el mazo en números

        foreach (var m in manosIds) m.Clear();
        montoIds.Clear();
        cartaAcusable = -1;
        posibleGanador = -1;

        // Igual que tu Repartidor: una carta a cada silla ocupada, ronda por ronda, sacando la de arriba
        for (int ronda = 0; ronda < cartasPorJugador; ronda++)
            for (int s = 0; s < MesaRed.CantidadSillas; s++)
            {
                if (!ocupadasPartida[s] || mazo.Count == 0) continue;
                int arriba = mazo.Count - 1;
                manosIds[s].Add(mazo[arriba]);
                mazo.RemoveAt(arriba);
            }

        mazoIds.Clear();
        mazoIds.AddRange(mazo);               // lo que sobra queda en el mazo, en el mismo orden que el mazo de la mesa

        EnJuego = true;
        Ganador = -1;
        Turno = QuienAbre();
        red.EnviarATodos(TiposJuego.Estado, new DatosEstado { enJuego = true, turno = Turno, ganador = -1 });

        for (int s = 0; s < MesaRed.CantidadSillas; s++)
        {
            if (!ocupadasPartida[s]) continue;
            string texto = "";
            foreach (int id in manosIds[s]) texto += baraja.PorId(id) + " ";
            Debug.Log("[PartidaRed] Silla " + s + ": " + texto);
        }
        Debug.Log("[PartidaRed] Abre la silla " + Turno);

        // A cada jugador SOLO sus cartas: mensaje privado
        for (int s = 0; s < MesaRed.CantidadSillas; s++)
            if (ocupadasPartida[s])
                red.EnviarA(mesa.IdEnSilla(s), TiposJuego.Reparto,
                    new DatosReparto { mano = manosIds[s].ToArray(), ocupadas = ocupadasPartida });
    }

    // Abre quien tenga el 1. Si nadie lo tiene (quedó en el mazo), abre la primera silla ocupada.
    int QuienAbre()
    {
        for (int s = 0; s < MesaRed.CantidadSillas; s++)
            foreach (int id in manosIds[s])
            {
                var c = baraja.PorId(id);
                if (c.tipo == TipoCarta.Numero && c.valor == 1) return s;
            }
        for (int s = 0; s < MesaRed.CantidadSillas; s++)
            if (ocupadasPartida[s]) return s;
        return 0;
    }

    // La silla siguiente a la derecha que esté en la partida y todavía tenga cartas
    int Siguiente(int silla)
    {
        for (int i = 1; i <= MesaRed.CantidadSillas; i++)
        {
            int s = (silla + i) % MesaRed.CantidadSillas;
            if (ocupadasPartida[s] && manosIds[s].Count > 0) return s;
        }
        return -1;
    }

    // ================================================================ Jugar una carta

    // 1) EL JUGADOR PIDE. Lo llama la ManoJugador local al presionar E (después de elegir la opción, si es un poder)
    void PedirAlHost(CartaVisual carta, int opcion)
    {
        red.EnviarAlHost(TiposJuego.Jugar, new DatosJugar { carta = baraja.IdDe(carta.Datos), opcion = opcion });
    }

    // 2) EL HOST VALIDA, decide el turno siguiente, aplica y avisa a todos
    void AlPedirJugada(Mensaje m)
    {
        if (!red.EsHost) return;

        int silla = mesa.SillaDe(m.de);   // "de" lo escribe el servidor: nadie puede hacerse pasar por otro
        var pedido = m.Leer<DatosJugar>();

        string error = ValidarJugada(silla, pedido);
        if (error != null)
        {
            red.EnviarA(m.de, TiposJuego.Rechazo, new DatosRechazo { motivo = error });
            return;
        }

        bool poder = baraja.PorId(pedido.carta).EsPoder;
        manosIds[silla].Remove(pedido.carta);

        if (!poder)
        {
            // Lo que se podrá revisar si alguien grita "¡Mentiroso!": la carta real y el número que tocaba
            montoIds.Add(pedido.carta);
            cartaAcusable = pedido.carta;
            esperadoAcusable = estado.Numero;   // la escalera todavía no avanzó: es el número que tocaba
        }

        bool sinCartas = manosIds[silla].Count == 0;
        int turno;
        if (sinCartas && !poder)
        {
            // Se quedó sin cartas con una carta al monto: gana si nadie lo acusa a tiempo
            posibleGanador = silla;
            finEspera = Time.time + segundosParaAcusar;
            turno = -1;
        }
        else if (sinCartas) turno = -1;          // su última carta fue un poder: gana ya (no hay nada que acusar)
        else if (poder) turno = silla;           // un poder no gasta el turno: ahora tira su carta al monto
        else turno = Siguiente(silla);           // turno a la derecha

        var jugada = new DatosJugada
        {
            silla = silla,
            poder = poder,
            carta = poder ? pedido.carta : -1,   // la del monto no se cuenta: va boca abajo
            opcion = pedido.opcion,
            turno = turno
        };
        AplicarJugada(jugada);                    // primero en el host…
        red.EnviarATodos(TiposJuego.Jugada, jugada);   // …y después en los demás

        if (sinCartas && poder) Terminar(silla);
    }

    // Devuelve el motivo si la jugada NO se puede hacer, o null si está bien
    string ValidarJugada(int silla, DatosJugar pedido)
    {
        if (!EnJuego) return "La partida no ha empezado";
        if (silla < 0) return "No estás sentado en la mesa";
        if (Turno < 0) return "Alguien se quedó sin cartas: ¿lo acusan?";
        if (silla != Turno) return "No es tu turno: le toca al Jugador " + (Turno + 1);
        if (!manosIds[silla].Contains(pedido.carta)) return "Esa carta no está en tu mano";

        var datos = baraja.PorId(pedido.carta);
        if (datos.EsPoder && !reglas.OpcionValida(datos.tipo, pedido.opcion)) return "Opción no válida para " + datos;
        return null;
    }

    // 3) TODOS (los demás equipos; el host ya lo aplicó)
    void AlRecibirJugada(Mensaje m)
    {
        if (red.EsHost) return;
        AplicarJugada(m.Leer<DatosJugada>());
    }

    // Mover la carta y aplicar su efecto. Se hace igual en todos los equipos y en el mismo orden,
    // por eso el letrero (número, sentido, pares/impares) queda igual en todos.
    void AplicarJugada(DatosJugada d)
    {
        if (d.silla < 0 || d.silla >= manos.Length) return;
        Turno = d.turno;
        var mano = manos[d.silla];

        // Si fui yo, muevo justo la carta que elegí. Si fue otro, sus cartas están ocultas para mí: muevo cualquiera.
        CartaVisual carta = mano.Pendiente;
        if (carta == null && mano.Cartas.Count > 0) carta = mano.Cartas[mano.Cartas.Count - 1];
        if (carta == null) return;

        CartaDatos datos = null;
        if (d.poder)
        {
            datos = baraja.PorId(d.carta);
            carta.Revelar(datos);   // los poderes van boca arriba: ahora todos saben cuál era
        }
        else
        {
            UltimaSilla = d.silla;
            NumeroDicho = estado.Numero;   // "dice que es un N" (antes de que la escalera avance)
            HayAcusable = true;
            acusacionEnviada = false;
        }

        mano.JugarCarta(carta, d.poder);   // al monto: la escalera avanza ahí mismo
        if (datos != null) reglas.Aplicar(datos.tipo, d.opcion);
    }

    // ================================================================ "¡Mentiroso!"

    // 1) CUALQUIERA ACUSA (tecla M)
    public void Acusar()
    {
        if (!PuedoAcusar || acusacionEnviada) return;
        acusacionEnviada = true;
        red.EnviarAlHost(TiposJuego.Acusar);
    }

    // 2) EL HOST JUZGA: mira la carta real y decide quién se come las cartas
    void AlPedirAcusacion(Mensaje m)
    {
        if (!red.EsHost) return;
        int acusador = mesa.SillaDe(m.de);
        int acusado = UltimaSilla;

        string error = null;
        if (!EnJuego) error = "La partida no ha empezado";
        else if (acusador < 0) error = "No estás sentado en la mesa";
        else if (!HayAcusable || cartaAcusable < 0) error = "No hay ninguna carta para acusar";
        else if (acusador == acusado) error = "No puedes acusarte a ti mismo";
        if (error != null)
        {
            red.EnviarA(m.de, TiposJuego.Rechazo, new DatosRechazo { motivo = error });
            return;
        }

        bool mentira = !EsVerdad(baraja.PorId(cartaAcusable), esperadoAcusable);
        int castigado = mentira ? acusado : acusador;

        // Se come las de arriba del mazo (las mismas que va a sacar el mazo de la mesa)
        var comidas = new List<int>();
        while (comidas.Count < cartasCastigo && mazoIds.Count > 0)
        {
            int arriba = mazoIds.Count - 1;
            comidas.Add(mazoIds[arriba]);
            mazoIds.RemoveAt(arriba);
        }
        manosIds[castigado].AddRange(comidas);

        // ¿Cambia el turno? Solo si estábamos esperando a ver si acusaban a quien se quedó sin cartas
        int turno = Turno;
        int ganador = -1;
        if (posibleGanador >= 0)
        {
            if (manosIds[posibleGanador].Count == 0) ganador = posibleGanador;   // era verdad (o el mazo se acabó): gana
            else turno = Siguiente(posibleGanador);                              // mintió: se come las cartas y el juego sigue
            posibleGanador = -1;
        }

        // Las cartas comidas: en privado al castigado (el host, si es él, ya las tiene aquí)
        if (castigado == MiSilla) misComidas = comidas.ToArray();
        else red.EnviarA(mesa.IdEnSilla(castigado), TiposJuego.Comer, new DatosComer { cartas = comidas.ToArray() });

        var d = new DatosAcusacion
        {
            acusador = acusador,
            acusado = acusado,
            carta = cartaAcusable,
            esperado = esperadoAcusable,
            mentira = mentira,
            castigado = castigado,
            cantidad = comidas.Count,
            turno = ganador >= 0 ? -1 : turno
        };
        cartaAcusable = -1;
        AplicarAcusacion(d);
        red.EnviarATodos(TiposJuego.Acusacion, d);

        if (ganador >= 0) Terminar(ganador);
    }

    // Es verdad si es justo el número que tocaba. El comodín vale por cualquier número: nunca es mentira.
    static bool EsVerdad(CartaDatos carta, int esperado)
    {
        if (carta == null) return false;
        if (carta.tipo == TipoCarta.Comodin) return true;
        return carta.tipo == TipoCarta.Numero && carta.valor == esperado;
    }

    void AlRecibirComer(Mensaje m) => misComidas = m.Leer<DatosComer>().cartas;

    // 3) TODOS: voltear la carta acusada, mostrar el resultado y darle las cartas al castigado
    void AlRecibirAcusacion(Mensaje m)
    {
        if (red.EsHost) return;
        AplicarAcusacion(m.Leer<DatosAcusacion>());
    }

    void AplicarAcusacion(DatosAcusacion d)
    {
        HayAcusable = false;
        acusacionEnviada = false;
        Turno = d.turno;

        var real = baraja.PorId(d.carta);
        var acusada = montoCentro != null ? montoCentro.Ultima : null;
        if (acusada != null)
        {
            acusada.Revelar(real);
            montoCentro.Voltear(acusada);
        }

        string era = "Era " + NombreCarta(real) + ".";
        if (d.mentira) aviso.Mostrar("¡MENTIROSO!", era + " Tocaba el " + d.esperado + ". " + SeCome(d.castigado, d.cantidad));
        else aviso.Mostrar("¡ERA VERDAD!", era + " " + SeCome(d.castigado, d.cantidad));

        // El castigado saca cartas del mazo de la mesa. Solo él ve cuáles son.
        var mano = manos[d.castigado];
        for (int i = 0; i < d.cantidad; i++)
        {
            var carta = mazoMesa.Robar();
            if (carta == null) break;
            if (d.castigado == MiSilla && misComidas != null && i < misComidas.Length)
                carta.Revelar(baraja.PorId(misComidas[i]));
            mano.Agregar(carta);
        }
        misComidas = null;
    }

    // ================================================================ Fin de la partida

    void Terminar(int ganador)
    {
        if (!red.EsHost) return;
        EnJuego = false;
        Turno = -1;
        Ganador = ganador;
        posibleGanador = -1;
        cartaAcusable = -1;
        HayAcusable = false;
        Debug.Log("[PartidaRed] Ganó la silla " + ganador);
        red.EnviarATodos(TiposJuego.Estado, new DatosEstado { enJuego = false, turno = -1, ganador = ganador });
    }

    // ================================================================ Mensajes que solo reciben los demás

    void AlRecibirEstado(Mensaje m)
    {
        var d = m.Leer<DatosEstado>();
        EnJuego = d.enJuego;
        Turno = d.turno;
        Ganador = d.ganador;
        if (!EnJuego) HayAcusable = false;
    }

    void AlRecibirRechazo(Mensaje m)
    {
        string motivo = m.Leer<DatosRechazo>().motivo;
        foreach (var mano in manos) mano.CancelarPendiente();
        acusacionEnviada = false;
        if (aviso != null) aviso.Mostrar("NO", motivo);
    }

    void RecibirReparto(Mensaje m)
    {
        var d = m.Leer<DatosReparto>();
        cartasPorRevelar = d.mano;

        // Limpiar la partida anterior: cartas en las manos y en la mesa, la escalera y la última jugada
        foreach (var mano in manos) mano.Vaciar();
        repartidor.Recoger();
        if (estado != null) estado.Reiniciar();
        UltimaSilla = -1;
        HayAcusable = false;
        acusacionEnviada = false;
        misComidas = null;

        mazoMesa.ConstruirOculto();
        repartidor.FijarActivos(d.ocupadas);
        repartidor.antesDeTerminar.AddListener(RevelarMisCartas);
        repartidor.Repartir();
    }

    void RevelarMisCartas()
    {
        repartidor.antesDeTerminar.RemoveListener(RevelarMisCartas);

        int miSilla = SillasRed.Instancia.SillaLocal;
        var misCartas = repartidor.Mano(miSilla);
        int cuantas = Mathf.Min(misCartas.Count, cartasPorRevelar.Length);
        for (int i = 0; i < cuantas; i++)
            misCartas[i].Revelar(baraja.PorId(cartasPorRevelar[i]));
    }

    void AlDesconectarse(string motivo)
    {
        EnJuego = false;
        Turno = -1;
        HayAcusable = false;
        posibleGanador = -1;
        foreach (var mano in manos)
        {
            mano.pedirAlHost = null;   // sin red, las manos vuelven a jugar solas
            mano.CancelarPendiente();
        }
    }

    // ================================================================ Textos

    // "Tú" si es mi silla; si no, "Jugador 2" (las sillas empiezan en 0, los jugadores en 1)
    public string Nombre(int silla) => silla == MiSilla ? "Tú" : "Jugador " + (silla + 1);

    string SeCome(int silla, int cantidad)
    {
        if (cantidad == 0) return "El mazo se acabó: nadie come.";
        string cartas = cantidad == 1 ? "1 carta" : cantidad + " cartas";
        return silla == MiSilla ? "Te comes " + cartas + "." : Nombre(silla) + " se come " + cartas + ".";
    }

    static string NombreCarta(CartaDatos c)
    {
        if (c == null) return "una carta";
        if (c.tipo == TipoCarta.Comodin) return "el comodín";
        return "un " + c;
    }
}
