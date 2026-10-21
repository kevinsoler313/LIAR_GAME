using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// La mano de un jugador: cuando termina el reparto levanta su pila de la mesa
// y la sostiene en una fila horizontal frente a sus ojos. Para el jugador local el ancla va
// pegada a la cámara, así las cartas quedan en la parte inferior de la pantalla.
// El jugador local puede bajarlas a la mesa (boca abajo) y volver a levantarlas con Espacio.
public class ManoJugador : MonoBehaviour
{
    [SerializeField] Repartidor repartidor;
    [SerializeField] Monto monto;                  // centro de la mesa, boca abajo
    [SerializeField] Monto montoPoderes;           // a la izquierda del monto, boca arriba (Pass, Return, Break)
    [SerializeField] ReglasPoderes reglas;         // pregunta y aplica el efecto de los poderes
    [SerializeField] int jugador;
    [SerializeField] Transform ancla;              // punto frente a los ojos del jugador (la fila se arma ahí)
    [SerializeField] bool ordenar = true;          // números de menor a mayor, luego poderes y comodín

    [Header("Jugador local")]
    [SerializeField] bool esJugadorLocal;          // su mano se ve en pantalla: siempre iluminada y sin sombras
    [SerializeField] Material frenteEnMano;        // material sin sombreado para que el frente se lea en la oscuridad

    [Header("En la mano")]
    [SerializeField] float escala = 0.6f;          // tamaño de las cartas en la mano respecto a la mesa
    [SerializeField] float separacion = 0.05f;     // distancia entre cartas (deja ver el número de cada una)
    [SerializeField] float separacionProfundidad = 0.0015f;   // cada carta un poco más cerca que la anterior

    [Header("Selección (flechas ← →)")]
    [SerializeField] float elevacionSeleccion = 0.03f;   // cuánto sube la carta seleccionada
    [SerializeField] float adelanteSeleccion = 0.01f;    // la trae al frente para que se vea completa
    [SerializeField] float velocidadSeleccion = 18f;

    [Header("Bajadas en la mesa")]
    [SerializeField] float separacionMesa = 0.035f;

    [Header("Animación")]
    [SerializeField] float intervalo = 0.07f;
    [SerializeField] float duracion = 0.5f;
    [SerializeField] float alturaArco = 0.08f;

    readonly List<CartaVisual> cartas = new List<CartaVisual>();
    public IReadOnlyList<CartaVisual> Cartas => cartas;

    public bool EnMano { get; private set; }
    public bool Moviendo { get; private set; }

    public int Jugador => jugador;

    public int IndiceSeleccionado { get; private set; }
    public CartaVisual Seleccionada => esJugadorLocal && EnMano && cartas.Count > 0 ? cartas[IndiceSeleccionado] : null;

    Material frenteMesa;   // material original del frente, para devolverlo al bajar las cartas

    void OnEnable() { if (repartidor != null) repartidor.alTerminar.AddListener(RecibirReparto); }
    void OnDisable() { if (repartidor != null) repartidor.alTerminar.RemoveListener(RecibirReparto); }

    // Cada reparto trae cartas nuevas: se toman de la pila y se levantan
    void RecibirReparto()
    {
        StopAllCoroutines();
        Moviendo = false;
        EnMano = false;
        cartas.Clear();
        IndiceSeleccionado = 0;
        Levantar();
    }

    void Update()
    {
        if (!esJugadorLocal || Moviendo || cartas.Count == 0 || Keyboard.current == null) return;
        if (SelectorOpciones.BloqueaEntrada) return;   // eligiendo la opción de un poder
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (EnMano) Bajar();
            else Levantar();
            return;
        }
        if (!EnMano) return;

        if (Keyboard.current.rightArrowKey.wasPressedThisFrame) Seleccionar(IndiceSeleccionado + 1);
        if (Keyboard.current.leftArrowKey.wasPressedThisFrame) Seleccionar(IndiceSeleccionado - 1);
        if (Keyboard.current.eKey.wasPressedThisFrame) JugarSeleccionada();
        if (cartas.Count == 0) return;

        // cada carta se acomoda suave hacia su lugar: la seleccionada arriba y al frente
        float paso = 1f - Mathf.Exp(-velocidadSeleccion * Time.deltaTime);
        for (int i = 0; i < cartas.Count; i++)
        {
            LugarEnMano(i, cartas.Count, out var pos, out _);
            var t = cartas[i].transform;
            t.localPosition = Vector3.Lerp(t.localPosition, pos, paso);
        }
    }

    // La red decide en qué silla estoy: esa mano pasa a ser la mía (de frente y pegada a la cámara)
    // y las demás vuelven a su punto frente a los ojos de su silla.
    public void FijarLocal(bool local, Transform nuevaAncla)
    {
        esJugadorLocal = local;
        ancla = nuevaAncla;
        if (!EnMano) return;

        // si las cartas ya estaban levantadas, se mudan al nuevo ancla conservando su lugar en la fila
        foreach (var c in cartas)
        {
            c.transform.SetParent(ancla, false);
            AspectoEnMano(c, local);
        }
    }

    public void Seleccionar(int indice) => IndiceSeleccionado = Mathf.Clamp(indice, 0, Mathf.Max(0, cartas.Count - 1));

    // Manda la carta seleccionada a la mesa: los poderes a su pila boca arriba, el resto al monto boca abajo.
    // Si es un poder, primero se aplica su regla (que puede preguntar algo al jugador).
    public void JugarSeleccionada()
    {
        var carta = Seleccionada;
        if (carta == null || Moviendo) return;

        bool esPoder = carta.Datos != null && carta.Datos.EsPoder;
        if (esPoder && reglas != null) reglas.Pedir(carta.Datos.tipo, () => Enviar(carta, montoPoderes));
        else Enviar(carta, esPoder ? montoPoderes : monto);
    }

    void Enviar(CartaVisual carta, Monto destino)
    {
        int i = cartas.IndexOf(carta);
        if (i < 0 || destino == null) return;

        cartas.RemoveAt(i);
        Seleccionar(IndiceSeleccionado);   // queda seleccionada la vecina (la de la derecha, o la última)
        AspectoEnMano(carta, false);       // sobre la mesa vuelve a verse como las demás
        destino.Jugar(carta);
    }

    // Toma la pila de la mesa (justo después del reparto) o las cartas bajadas, y las pone en la mano
    public void Levantar()
    {
        if (Moviendo) return;
        if (cartas.Count == 0)
        {
            cartas.AddRange(repartidor.Mano(jugador));
            if (ordenar) cartas.Sort((a, b) => Orden(a.Datos).CompareTo(Orden(b.Datos)));
        }
        StartCoroutine(Mover(true));
    }

    public void Bajar()
    {
        if (Moviendo || cartas.Count == 0) return;
        StartCoroutine(Mover(false));
    }

    static int Orden(CartaDatos d)
    {
        if (d == null) return 999;
        switch (d.tipo)
        {
            case TipoCarta.Numero: return d.valor;
            case TipoCarta.Pass: return 100;
            case TipoCarta.Return: return 101;
            case TipoCarta.Break: return 102;
            default: return 200;   // comodín al final
        }
    }

    // Lugar de la carta i en la mano (local al ancla): fila horizontal, la de la derecha encima.
    // El frente mira hacia el jugador (-Z del ancla) y la carta va "parada" a lo largo de +Y.
    void LugarEnMano(int i, int total, out Vector3 pos, out Quaternion rot)
    {
        pos = new Vector3((i - (total - 1) * 0.5f) * separacion, 0f, -separacionProfundidad * i);
        if (esJugadorLocal && i == IndiceSeleccionado)
            pos += new Vector3(0f, elevacionSeleccion, -adelanteSeleccion);
        rot = Quaternion.LookRotation(Vector3.up, Vector3.forward);
    }

    // Lugar de la carta i bajada en la mesa (local a la pila): fila boca abajo, la de la derecha encima
    void LugarEnMesa(int i, int total, out Vector3 pos, out Quaternion rot)
    {
        pos = new Vector3((i - (total - 1) * 0.5f) * separacionMesa, repartidor.GrosorCarta * (i + 0.5f), 0f);
        rot = Quaternion.identity;
    }

    IEnumerator Mover(bool aLaMano)
    {
        Moviendo = true;

        // al levantar sale primero la de la derecha (la de arriba); al bajar, la de la izquierda
        Coroutine ultima = null;
        for (int n = 0; n < cartas.Count; n++)
        {
            int i = aLaMano ? cartas.Count - 1 - n : n;
            ultima = StartCoroutine(Volar(cartas[i], i, aLaMano));
            yield return new WaitForSeconds(intervalo);
        }
        yield return ultima;

        EnMano = aLaMano;
        Moviendo = false;
    }

    IEnumerator Volar(CartaVisual carta, int indice, bool aLaMano)
    {
        var t = carta.transform;
        t.SetParent(aLaMano ? ancla : transform, true);
        if (esJugadorLocal) AspectoEnMano(carta, aLaMano);

        Vector3 desde = t.localPosition;
        Quaternion giroDesde = t.localRotation;
        Vector3 escalaDesde = t.localScale;

        Vector3 hasta; Quaternion giroHasta;
        if (aLaMano) LugarEnMano(indice, cartas.Count, out hasta, out giroHasta);
        else LugarEnMesa(indice, cartas.Count, out hasta, out giroHasta);
        Vector3 escalaHasta = Vector3.one * (aLaMano ? escala : 1f);
        Vector3 arriba = t.parent.InverseTransformDirection(Vector3.up) * alturaArco;

        for (float tiempo = 0; tiempo < duracion; tiempo += Time.deltaTime)
        {
            float p = tiempo / duracion;
            float suave = p * p * (3f - 2f * p);                              // arranca y llega suave
            t.localPosition = Vector3.Lerp(desde, hasta, suave) + arriba * (4f * p * (1f - p));
            t.localRotation = Quaternion.Slerp(giroDesde, giroHasta, suave);
            t.localScale = Vector3.Lerp(escalaDesde, escalaHasta, suave);
            yield return null;
        }
        t.localPosition = hasta;
        t.localRotation = giroHasta;
        t.localScale = escalaHasta;
    }

    // En la mano: material sin sombreado y sin sombras (tan cerca de la cámara haría sombras gigantes).
    // En la mesa: vuelven a verse como las demás cartas.
    void AspectoEnMano(CartaVisual carta, bool enMano)
    {
        var render = carta.GetComponent<MeshRenderer>();
        render.shadowCastingMode = enMano ? UnityEngine.Rendering.ShadowCastingMode.Off : UnityEngine.Rendering.ShadowCastingMode.On;
        if (frenteEnMano == null) return;

        var mats = render.sharedMaterials;
        if (frenteMesa == null) frenteMesa = mats[1];
        mats[1] = enMano ? frenteEnMano : frenteMesa;   // la textura de la carta sigue en el property block del índice 1
        render.sharedMaterials = mats;
    }
}
