using UnityEngine;
using UnityEngine.Events;

public enum SentidoEscalera { Subiendo, Bajando }
public enum OrdenEscalera { Normal, Pares, Impares }

// El estado de la escalera: qué número toca, hacia dónde va y si salta de a dos (pares/impares).
// El número que toca siempre se calcula desde el último jugado, así los poderes lo recalculan bien:
// tras un 5, Return hace que toque el 4 y Break a pares hace que toque el 6.
// Los poderes lo modifican y el HUD lo muestra escuchando alCambiar.
public class EstadoPartida : MonoBehaviour
{
    public const int Minimo = 1, Maximo = 10;

    [SerializeField] int numero = 1;                       // el número que toca jugar
    [SerializeField] SentidoEscalera sentido = SentidoEscalera.Subiendo;
    [SerializeField] OrdenEscalera orden = OrdenEscalera.Normal;

    public UnityEvent alCambiar;

    int ultimo;                                            // último número jugado al monto (0 = ninguno todavía)

    public int Numero => numero;
    public int Ultimo => ultimo;
    public SentidoEscalera Sentido => sentido;
    public OrdenEscalera Orden => orden;

    // Al jugar una carta al monto, cuenta como el número que tocaba y la escalera avanza
    public void Avanzar()
    {
        ultimo = numero;
        numero = Siguiente(ultimo);
        alCambiar.Invoke();
    }

    // Return
    public void InvertirSentido()
    {
        sentido = sentido == SentidoEscalera.Subiendo ? SentidoEscalera.Bajando : SentidoEscalera.Subiendo;
        if (ultimo > 0) numero = Siguiente(ultimo);
        alCambiar.Invoke();
    }

    // Break
    public void FijarOrden(OrdenEscalera nuevo)
    {
        orden = nuevo;
        if (ultimo > 0) numero = Siguiente(ultimo);
        else if (!Cumple(numero)) numero = Siguiente(numero);
        alCambiar.Invoke();
    }

    // Pass: la escalera salta al número elegido; la próxima carta jugada cuenta como ese número
    public void SaltarA(int n)
    {
        numero = Mathf.Clamp(n, Minimo, Maximo);
        alCambiar.Invoke();
    }

    public void Reiniciar()
    {
        numero = Minimo;
        ultimo = 0;
        sentido = SentidoEscalera.Subiendo;
        orden = OrdenEscalera.Normal;
        alCambiar.Invoke();
    }

    // El siguiente número válido después de n en el sentido actual.
    // Pasado el 10 vuelve a empezar desde abajo (y pasado el 1, desde arriba), respetando pares/impares.
    public int Siguiente(int n)
    {
        int paso = sentido == SentidoEscalera.Subiendo ? 1 : -1;
        for (int i = 0; i < Maximo; i++)
        {
            n += paso;
            if (n > Maximo) n = Minimo;
            if (n < Minimo) n = Maximo;
            if (Cumple(n)) return n;
        }
        return n;
    }

    public bool Cumple(int n) =>
        orden == OrdenEscalera.Normal ||
        (orden == OrdenEscalera.Pares && n % 2 == 0) ||
        (orden == OrdenEscalera.Impares && n % 2 == 1);

    // Para probar desde el Inspector (clic derecho en el componente)
    [ContextMenu("Probar/Avanzar")] void ProbarAvanzar() => Avanzar();
    [ContextMenu("Probar/Invertir sentido")] void ProbarInvertir() => InvertirSentido();
    [ContextMenu("Probar/Orden: Normal")] void ProbarNormal() => FijarOrden(OrdenEscalera.Normal);
    [ContextMenu("Probar/Orden: Pares")] void ProbarPares() => FijarOrden(OrdenEscalera.Pares);
    [ContextMenu("Probar/Orden: Impares")] void ProbarImpares() => FijarOrden(OrdenEscalera.Impares);

    void OnValidate() { if (Application.isPlaying) alCambiar?.Invoke(); }
}
