using System;
using System.Collections.Generic;
using UnityEngine;

// Qué hace cada poder al jugarlo:
//  - Return: invierte el sentido de la escalera.
//  - Break: el jugador elige si la escalera sigue en pares, impares o normal.
//  - Pass: el jugador elige a qué número salta la escalera; luego juega su carta como "ese número".
//
// Está dividido en dos pasos para que funcione en red:
//  1. Preguntar: abre el selector y devuelve la "opción" elegida (un número). No cambia nada todavía.
//  2. Aplicar: con esa opción, cambia la escalera y muestra el aviso.
// En red, entre el paso 1 y el 2 la jugada viaja al host, y el paso 2 ocurre en TODOS los equipos.
//
// Qué significa "opcion" en cada poder:
//  - Return: no se usa (0).
//  - Break: el OrdenEscalera elegido, como número (0 = Normal, 1 = Pares, 2 = Impares).
//  - Pass: el número al que salta la escalera (1 a 10).
public class ReglasPoderes : MonoBehaviour
{
    [SerializeField] EstadoPartida estado;
    [SerializeField] SelectorOpciones selector;
    [SerializeField] AvisoPoder aviso;

    public EstadoPartida Estado => estado;

    static readonly OrdenEscalera[] ordenes = { OrdenEscalera.Pares, OrdenEscalera.Impares, OrdenEscalera.Normal };

    // Sin red: pregunta, y si el jugador confirma, juega la carta y aplica el efecto. Si cancela, la carta se queda en la mano.
    public void Pedir(TipoCarta tipo, Action jugar)
    {
        Preguntar(tipo, opcion =>
        {
            jugar();
            Aplicar(tipo, opcion);
        });
    }

    // Paso 1: solo pregunta. Llama a elegir(opcion) cuando el jugador confirma.
    public void Preguntar(TipoCarta tipo, Action<int> elegir)
    {
        switch (tipo)
        {
            case TipoCarta.Return:
                elegir(0);   // no hay nada que preguntar
                break;

            case TipoCarta.Break:
                selector.Mostrar("BREAK", "¿Cómo sigue la escalera?", new[] { "PARES", "IMPARES", "NORMAL" },
                    Array.IndexOf(ordenes, estado.Orden),
                    i => elegir((int)ordenes[i]));
                break;

            case TipoCarta.Pass:
                var validos = NumerosValidos();
                selector.Mostrar("PASS", "¿A qué número saltas?", validos.ConvertAll(n => n.ToString()).ToArray(),
                    Mathf.Max(0, validos.IndexOf(estado.Numero)),
                    i => elegir(validos[i]));
                break;
        }
    }

    // Paso 2: aplica el efecto con la opción que se eligió
    public void Aplicar(TipoCarta tipo, int opcion)
    {
        switch (tipo)
        {
            case TipoCarta.Return: AplicarReturn(); break;
            case TipoCarta.Break: AplicarBreak((OrdenEscalera)opcion); break;
            case TipoCarta.Pass: AplicarPass(opcion); break;
        }
    }

    // El host lo usa para no aceptar opciones inventadas (por ejemplo, un Pass al 15)
    public bool OpcionValida(TipoCarta tipo, int opcion)
    {
        switch (tipo)
        {
            case TipoCarta.Return: return true;
            case TipoCarta.Break: return Array.IndexOf(ordenes, (OrdenEscalera)opcion) >= 0;
            case TipoCarta.Pass: return NumerosValidos().Contains(opcion);
            default: return false;
        }
    }

    List<int> NumerosValidos()
    {
        var validos = new List<int>();
        for (int n = EstadoPartida.Minimo; n <= EstadoPartida.Maximo; n++)
            if (estado.Cumple(n)) validos.Add(n);
        return validos;
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
        aviso.Mostrar("PASS", "Salto al " + numero + ": la siguiente carta cuenta como " + numero);
    }
}
