using UnityEngine;

public enum TipoCarta { Numero, Pass, Return, Break, Comodin }

// Una carta de la baraja: qué es y cómo se ve. Se crea una por diseño (1–10, poderes, comodín).
[CreateAssetMenu(menuName = "LIAR GAME/Carta", fileName = "Carta")]
public class CartaDatos : ScriptableObject
{
    public TipoCarta tipo;
    [Range(1, 10)] public int valor = 1;     // solo cuenta si tipo == Numero
    public Sprite frente;

    public bool EsPoder => tipo == TipoCarta.Pass || tipo == TipoCarta.Return || tipo == TipoCarta.Break;

    public override string ToString() => tipo == TipoCarta.Numero ? valor.ToString() : tipo.ToString().ToUpper();
}
