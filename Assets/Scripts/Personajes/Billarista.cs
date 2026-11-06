using UnityEngine;

// Cliente jugando billar con su taco: espera de pie apoyado en el taco (Esperar_Taco) y, cuando TurnosBillar
// le da el turno, se inclina, hace sus amagues y tira (Tirar). Es solo ambiente: nada que ver con la partida ni con la red.
public class Billarista : MonoBehaviour
{
    [SerializeField] Animator animador;

    const string esperar = "Esperar_Taco", tirar = "Tirar";
    static readonly int estadoTirar = Animator.StringToHash(tirar);

    void Start()
    {
        animador.speed = Random.Range(0.95f, 1.05f);
        animador.Play(esperar, 0, Random.value);   // cada uno en otro punto: no se mueven igual
    }

    public void Tirar() => animador.CrossFadeInFixedTime(tirar, 0.7f, 0, 0f);
    public void Esperar() => animador.CrossFadeInFixedTime(esperar, 0.7f, 0, Random.Range(0f, 4f));

    // cuánto lleva del tiro (0..1 del loop de Tirar); -1 si no está tirando
    public float AvanceTiro
    {
        get
        {
            var estado = animador.IsInTransition(0) ? animador.GetNextAnimatorStateInfo(0) : animador.GetCurrentAnimatorStateInfo(0);
            return estado.shortNameHash == estadoTirar ? estado.normalizedTime : -1f;
        }
    }
}
