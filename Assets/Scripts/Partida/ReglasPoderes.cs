using System;
using System.Collections.Generic;
using UnityEngine;

// Qué hace cada poder al jugarlo:
//  - Return: invierte el sentido de la escalera.
//  - Break: el jugador elige si la escalera sigue en pares, impares o normal.
//  - Pass: el jugador elige a qué número salta la escalera; luego juega su carta como "ese número".
// Para el jugador local pregunta con el selector; los bots (más adelante) usarán Aplicar directamente.
public class ReglasPoderes : MonoBehaviour
{
    [SerializeField] EstadoPartida estado;
    [SerializeField] SelectorOpciones selector;
    [SerializeField] AvisoPoder aviso;

    static readonly OrdenEscalera[] ordenes = { OrdenEscalera.Pares, OrdenEscalera.Impares, OrdenEscalera.Normal };

    // Pregunta lo que haga falta y, si el jugador confirma, llama a jugar() (que manda la carta a la mesa)
    // y aplica el efecto. Si cancela, la carta se queda en la mano.
    public void Pedir(TipoCarta tipo, Action jugar)
    {
        switch (tipo)
        {
            case TipoCarta.Return:
                jugar();
                AplicarReturn();
                break;

            case TipoCarta.Break:
                selector.Mostrar("BREAK", "¿Cómo sigue la escalera?", new[] { "PARES", "IMPARES", "NORMAL" },
                    Array.IndexOf(ordenes, estado.Orden),
                    i => { jugar(); AplicarBreak(ordenes[i]); });
                break;

            case TipoCarta.Pass:
                var validos = new List<int>();
                for (int n = EstadoPartida.Minimo; n <= EstadoPartida.Maximo; n++)
                    if (estado.Cumple(n)) validos.Add(n);
                selector.Mostrar("PASS", "¿A qué número saltas?", validos.ConvertAll(n => n.ToString()).ToArray(),
                    Mathf.Max(0, validos.IndexOf(estado.Numero)),
                    i => { jugar(); AplicarPass(validos[i]); });
                break;
        }
    }

    public void AplicarReturn()
    {
        estado.InvertirSentido();
        aviso.Mostrar("RETURN", estado.Sentido == SentidoEscalera.Subiendo ? "La escalera ahora va subiendo" : "La escalera ahora va bajando");
    }

    public void AplicarBreak(OrdenEscalera orden)
    {
        estado.FijarOrden(orden);
        aviso.Mostrar("BREAK", orden == OrdenEscalera.Normal ? "La escalera sigue normal" : "La escalera sigue en " + (orden == OrdenEscalera.Pares ? "pares" : "impares"));
    }

    public void AplicarPass(int numero)
    {
        estado.SaltarA(numero);
        aviso.Mostrar("PASS", "Salto al " + numero + ": ahora va tu carta");
    }
}
