using UnityEngine;

// Cliente jugando billar con su taco: el que tira repite su tiro (amagues, tiro y mira la jugada)
// y los que esperan se apoyan en el taco. Cada uno empieza en otro punto y a un ritmo apenas distinto.
// Es solo ambiente: no tiene nada que ver con la partida ni con la red.
public class Billarista : MonoBehaviour
{
    [SerializeField] Animator animador;
    [SerializeField] string accion = "Esperar_Taco";   // Tirar o Esperar_Taco

    void Start()
    {
        animador.speed = Random.Range(0.93f, 1.07f);
        animador.Play(accion, 0, Random.value);
    }
}
