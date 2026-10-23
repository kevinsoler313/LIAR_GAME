using UnityEngine;
using UnityEngine.Rendering;

// Los 4 personajes animados (Personajes.fbx), uno por silla y en el mismo orden que SillasRed.
// La cámara de este equipo va en los ojos de su silla, o sea dentro de la cabeza de ese personaje:
// a ese lo dejamos solo con sombra y los otros tres se ven normal.
public class PersonajesEnSillas : MonoBehaviour
{
    [SerializeField] GameObject[] personajes;     // mallas de Puesto_0..3: MujerS, MujerC, Inseguro, Viejo

    Renderer[][] mallas;
    int sillaOculta = -2;                          // -2 = todavía no se ha aplicado nada

    void Awake()
    {
        mallas = new Renderer[personajes.Length][];
        for (int i = 0; i < personajes.Length; i++)
            mallas[i] = personajes[i].GetComponentsInChildren<Renderer>(true);
    }

    // Se revisa cada cuadro porque la silla cambia cuando el jugador se conecta a una partida
    void LateUpdate()
    {
        int silla = SillasRed.Instancia != null ? SillasRed.Instancia.SillaLocal : -1;
        if (silla == sillaOculta) return;
        sillaOculta = silla;

        for (int i = 0; i < mallas.Length; i++)
            foreach (var malla in mallas[i])
                malla.shadowCastingMode = i == silla ? ShadowCastingMode.ShadowsOnly : ShadowCastingMode.On;
    }
}
