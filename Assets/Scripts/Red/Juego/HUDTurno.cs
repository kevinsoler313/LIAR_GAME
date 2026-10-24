using TMPro;
using UnityEngine;

// Cartel arriba a la derecha que dice de quién es el turno y qué pasó con la última carta.
//   TU TURNO / TURNO DE JUGADOR 3 / ¡GANASTE! / GANÓ JUGADOR 2
//   "Jugador 2 dice que tiró un 5"
//   [M] ¡MENTIROSO!   (solo cuando puedes acusar)
// No guarda nada: cada frame le pregunta a PartidaRed cómo va la partida y lo escribe.
public class HUDTurno : MonoBehaviour
{
    [SerializeField] PartidaRed partida;
    [SerializeField] GameObject panel;        // se esconde cuando no hay nada que mostrar
    [SerializeField] TMP_Text titulo;
    [SerializeField] TMP_Text detalle;
    [SerializeField] GameObject pista;        // la fila "[M] ¡MENTIROSO!"

    [Header("Colores")]
    [SerializeField] Color colorMiTurno = new Color(0.95f, 0.55f, 0.18f);   // naranja: te toca
    [SerializeField] Color colorNormal = new Color(0.93f, 0.86f, 0.70f);    // crema: le toca a otro
    [SerializeField] float pulso = 0.06f;     // cuánto "late" el título cuando es tu turno

    void Update()
    {
        bool hayAlgo = partida.EnJuego || partida.Ganador >= 0;
        if (panel.activeSelf != hayAlgo) panel.SetActive(hayAlgo);
        if (!hayAlgo) return;

        // ---------- Título ----------
        bool resaltar = false;
        if (!partida.EnJuego)
        {
            resaltar = partida.Ganador == partida.MiSilla;
            titulo.text = resaltar ? "¡GANASTE!" : "GANÓ " + partida.Nombre(partida.Ganador).ToUpper();
        }
        else if (partida.EsperandoAcusacionFinal)
        {
            titulo.text = partida.UltimaSilla == partida.MiSilla
                ? "¡TE QUEDASTE SIN CARTAS!"
                : partida.Nombre(partida.UltimaSilla).ToUpper() + " SIN CARTAS";
        }
        else if (partida.EsMiTurno)
        {
            resaltar = true;
            titulo.text = "TU TURNO";
        }
        else titulo.text = "TURNO DE " + partida.Nombre(partida.Turno).ToUpper();

        titulo.color = resaltar ? colorMiTurno : colorNormal;
        float escala = resaltar ? 1f + pulso * (0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 5f)) : 1f;
        titulo.transform.localScale = Vector3.one * escala;

        // ---------- Detalle ----------
        if (!partida.EnJuego) detalle.text = "";
        else if (partida.EsperandoAcusacionFinal)
            detalle.text = partida.UltimaSilla == partida.MiSilla
                ? "Si nadie te acusa, ganas"
                : "Si mintió, acúsalo antes de que gane";
        else if (partida.HayAcusable)
            detalle.text = partida.UltimaSilla == partida.MiSilla
                ? "Dijiste que tiraste un " + partida.NumeroDicho
                : partida.Nombre(partida.UltimaSilla) + " dice que tiró un " + partida.NumeroDicho;
        else if (partida.EsMiTurno) detalle.text = "Elige con las flechas y tira con E";
        else detalle.text = "";
        detalle.gameObject.SetActive(detalle.text != "");

        // ---------- Pista para acusar ----------
        bool acusar = partida.PuedoAcusar;
        if (pista.activeSelf != acusar) pista.SetActive(acusar);
    }
}
