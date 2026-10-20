using System.Collections.Generic;
using UnityEngine;

// La baraja completa: qué cartas tiene y cuántas copias de cada una.
[CreateAssetMenu(menuName = "LIAR GAME/Baraja", fileName = "Baraja")]
public class Baraja : ScriptableObject
{
    [System.Serializable]
    public class Entrada
    {
        public CartaDatos carta;
        [Min(0)] public int copias = 1;
    }

    public Sprite reverso;
    public List<Entrada> cartas = new List<Entrada>();

    // Todas las cartas (con sus copias) ya barajadas
    public List<CartaDatos> Mezclar()
    {
        var mazo = new List<CartaDatos>();
        foreach (var e in cartas)
            for (int i = 0; i < e.copias; i++)
                mazo.Add(e.carta);

        for (int i = mazo.Count - 1; i > 0; i--)   // Fisher–Yates
        {
            int j = Random.Range(0, i + 1);
            (mazo[i], mazo[j]) = (mazo[j], mazo[i]);
        }
        return mazo;
    }
}
