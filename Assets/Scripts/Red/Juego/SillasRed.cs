using UnityEngine;

// Las 4 sillas de la mesa. No sabe nada de red: solo "sienta" la cámara de ESTE equipo
// en la silla que le digan, y decide qué mano se ve de frente (la mía) y cuáles no.
public class SillasRed : MonoBehaviour
{
    [SerializeField] Transform camara;            // la única cámara de la escena
    [SerializeField] Transform manoLocal;         // punto pegado a la cámara donde se sostiene MI mano
    [SerializeField] Transform[] ojos;            // Puesto_0..3/Ojos
    [SerializeField] Transform[] manosEnSilla;    // Puesto_0..3/Ojos/Mano en mano (para las manos de los demás)
    [SerializeField] ManoJugador[] manos;         // Mano_0..3, en el mismo orden que las sillas
    [SerializeField] int sillaInicial = 0;        // mientras no estés conectado

    // Hay una sola en la escena: así JugadorRed la encuentra sin arrastrar referencias
    public static SillasRed Instancia { get; private set; }

    public int SillaLocal { get; private set; } = -1;
    public int Cantidad => ojos.Length;

    void Awake() { Instancia = this; }
    void OnDestroy() { if (Instancia == this) Instancia = null; }

    void Start()
    {
        if (SillaLocal < 0) Sentar(sillaInicial);
    }

    public void Sentar(int silla)
    {
        if (silla < 0 || silla >= ojos.Length) return;
        SillaLocal = silla;

        // 1. La cámara toma el lugar de los ojos de esa silla.
        //    Se hace hija del puesto (no de Ojos) para que MirarConMouse siga girando igual que antes.
        camara.SetParent(ojos[silla].parent, false);
        camara.localPosition = ojos[silla].localPosition;
        camara.localRotation = ojos[silla].localRotation;
        var mirar = camara.GetComponent<MirarConMouse>();
        if (mirar != null) mirar.Recentrar();

        // 2. Mi mano se pega a la cámara (abajo en la pantalla, de frente).
        //    Las de los demás se quedan frente a los ojos de su silla.
        for (int i = 0; i < manos.Length; i++)
            manos[i].FijarLocal(i == silla, i == silla ? manoLocal : manosEnSilla[i]);
    }

    public Vector3 PosicionSilla(int silla) => ojos[silla].parent.position;
    public Quaternion RotacionSilla(int silla) => ojos[silla].parent.rotation;
}
