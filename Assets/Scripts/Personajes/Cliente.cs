using UnityEngine;

// Cliente sentado en una de las mesas del bar: conversa (mira a los lados, explica con la mano)
// y cada tanto toma un sorbo de su cerveza. Cada uno empieza en otro punto para que no se muevan igual.
// Es solo ambiente: no tiene nada que ver con la partida ni con la red.
public class Cliente : MonoBehaviour
{
    [SerializeField] Animator animador;
    [SerializeField] Vector2 segundosEntreSorbos = new Vector2(7f, 16f);

    const string charlar = "Charlar", beber = "Beber";
    const float largoCharlar = 10f;   // segundos del loop de Charlar (240 cuadros a 24 fps)

    float proximoSorbo;
    bool bebiendo;

    void Start()
    {
        animador.Play(charlar, 0, Random.value);
        proximoSorbo = Time.time + Random.Range(2f, segundosEntreSorbos.y);
    }

    void Update()
    {
        if (bebiendo)
        {
            var estado = animador.GetCurrentAnimatorStateInfo(0);
            if (estado.IsName(beber) && estado.normalizedTime >= 0.95f)
            {
                bebiendo = false;
                animador.CrossFadeInFixedTime(charlar, 0.4f, 0, Random.Range(0f, largoCharlar));
                proximoSorbo = Time.time + Random.Range(segundosEntreSorbos.x, segundosEntreSorbos.y);
            }
            return;
        }
        if (Time.time < proximoSorbo) return;
        bebiendo = true;
        animador.CrossFadeInFixedTime(beber, 0.3f);
    }
}
