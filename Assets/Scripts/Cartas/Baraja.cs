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
    public List<CartaDatos> Mezclar() => MezclarIds().ConvertAll(PorId);
    
    public int IdDe(CartaDatos carta){
        for(int i =0;i<cartas.Count;i++){
            if(cartas[i].carta == carta){
                return i;
            }
        }
        return -1;
    }

    public CartaDatos PorId(int id)
    {
        if(id<0|| id>= cartas.Count){
            return null; 
        }
        return cartas[id].carta;
    }
public List<int> MezclarIds()
{
    var mazo = new List<int>();
    for (int i = 0; i < cartas.Count; i++)          
        for (int c = 0; c < cartas[i].copias; c++)  
            mazo.Add(i);

    for (int i = mazo.Count - 1; i > 0; i--)        
    {
        int j = Random.Range(0, i + 1);
        (mazo[i], mazo[j]) = (mazo[j], mazo[i]);
    }
    return mazo;
}


[ContextMenu("Probar ids")]
void ProbarIds()
{
    bool todoBien = true;
    foreach (var e in cartas)
    {
        int id = IdDe(e.carta);
        if (PorId(id) != e.carta)
        {
            Debug.LogError("Falla con " + e.carta + " (id " + id + ")");
            todoBien = false;
        }
    }
    Debug.Log(todoBien ? "Ida y vuelta OK" : "Hay errores");

    Debug.Log("PorId(-1) = " + (PorId(-1) == null ? "null (bien)" : "ERROR"));

    var mazo = MezclarIds();
    int total = 0;
    foreach (var e in cartas) total += e.copias;
    Debug.Log("Mazo: " + mazo.Count + " cartas (deberían ser " + total + ")");

    string primeras = "";
    for (int i = 0; i < 10 && i < mazo.Count; i++)
        primeras += PorId(mazo[i]) + " ";
    Debug.Log("Primeras 10: " + primeras);
}

}
