using System.Collections.Generic;
using UnityEngine;

// Aro fino en el centro de la mesa con una flechita que apunta al cuarto de la mesa del jugador que juega.
// Gira suave cuando cambia el turno.
//   naranja = te toca a ti · crema = le toca a otro · rojo = alguien se quedó sin cartas (¿lo acusan?)
// No guarda nada: cada frame le pregunta a PartidaRed cómo va la partida.
// La malla (aro + flecha) se arma por código al empezar; solo necesita el material.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class IndicadorTurno : MonoBehaviour
{
    [SerializeField] PartidaRed partida;
    [SerializeField] SillasRed sillas;

    [Header("Forma (metros)")]
    [SerializeField] float radio = 0.26f;            // por fuera del mazo, el monto y los poderes
    [SerializeField] float grosor = 0.01f;
    [SerializeField] float largoFlecha = 0.06f;
    [SerializeField] float anchoFlecha = 0.08f;

    [Header("Colores")]
    [SerializeField] Color colorMiTurno = new Color(0.95f, 0.55f, 0.18f);   // los mismos del cartel de turno
    [SerializeField] Color colorOtro = new Color(0.93f, 0.86f, 0.70f);
    [SerializeField] Color colorSinCartas = new Color(0.9f, 0.22f, 0.15f);

    [Header("Animación")]
    [SerializeField] float velocidadGiro = 7f;
    [SerializeField] float velocidadFundido = 5f;

    Material material;
    Color color;
    float alfa;          // 0 = escondido, 1 = visible
    float angulo;        // grados alrededor de Y
    bool primeraVez = true;

    void Awake()
    {
        GetComponent<MeshFilter>().mesh = CrearMalla();
        material = GetComponent<MeshRenderer>().material;   // copia propia para cambiarle el color
        color = colorOtro;
        Aplicar();
    }

    void Update()
    {
        int silla = SillaAMostrar(out Color objetivo);
        float paso = 1f - Mathf.Exp(-velocidadFundido * Time.deltaTime);

        alfa = Mathf.Lerp(alfa, silla >= 0 ? 1f : 0f, paso);
        color = Color.Lerp(color, objetivo, paso);

        if (silla >= 0)
        {
            Vector3 hacia = sillas.PosicionSilla(silla) - transform.position;
            float destino = Mathf.Atan2(hacia.x, hacia.z) * Mathf.Rad2Deg;
            // la primera vez aparece ya apuntando; después gira por el camino corto
            angulo = primeraVez ? destino : Mathf.LerpAngle(angulo, destino, 1f - Mathf.Exp(-velocidadGiro * Time.deltaTime));
            primeraVez = false;
        }
        else if (alfa < 0.01f) primeraVez = true;

        transform.rotation = Quaternion.Euler(0f, angulo, 0f);
        Aplicar();
    }

    // A quién apuntar y de qué color (-1 = nada que mostrar)
    int SillaAMostrar(out Color objetivo)
    {
        objetivo = colorOtro;
        if (partida == null || sillas == null || !partida.EnJuego) return -1;

        if (partida.EsperandoAcusacionFinal)
        {
            objetivo = colorSinCartas;
            return partida.UltimaSilla;
        }
        if (partida.Turno < 0) return -1;

        if (partida.EsMiTurno) objetivo = colorMiTurno;
        return partida.Turno;
    }

    void Aplicar()
    {
        var c = color;
        c.a = alfa;
        material.SetColor("_BaseColor", c);
    }

    // Aro (borde interior y exterior) + flecha que sale hacia +Z
    Mesh CrearMalla()
    {
        var v = new List<Vector3>();
        var col = new List<Color>();
        var tri = new List<int>();

        float[] radios = { radio - grosor * 0.5f, radio + grosor * 0.5f };
        const int segmentos = 128;
        for (int s = 0; s <= segmentos; s++)
        {
            float a = s * Mathf.PI * 2f / segmentos;
            var dir = new Vector3(Mathf.Sin(a), 0f, Mathf.Cos(a));
            for (int k = 0; k < radios.Length; k++)
            {
                v.Add(dir * radios[k]);
                col.Add(Color.white);
            }
        }
        for (int s = 0; s < segmentos; s++)
            for (int k = 0; k < radios.Length - 1; k++)
            {
                int a = s * radios.Length + k, b = a + radios.Length;
                tri.AddRange(new[] { a, b, a + 1, a + 1, b, b + 1 });
            }

        // Flecha: triángulo pegado al borde exterior del aro
        float baseZ = radio + grosor * 0.5f;
        int i = v.Count;
        v.Add(new Vector3(-anchoFlecha * 0.5f, 0f, baseZ));
        v.Add(new Vector3(0f, 0f, baseZ + largoFlecha));
        v.Add(new Vector3(anchoFlecha * 0.5f, 0f, baseZ));
        for (int k = 0; k < 3; k++) col.Add(Color.white);
        tri.AddRange(new[] { i, i + 1, i + 2 });

        var malla = new Mesh { name = "IndicadorTurno" };
        malla.SetVertices(v);
        malla.SetColors(col);
        malla.SetTriangles(tri, 0);
        malla.RecalculateNormals();
        malla.RecalculateBounds();
        return malla;
    }
}
