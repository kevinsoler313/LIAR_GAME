using System.Collections.Generic;
using UnityEngine;

// Malla de una carta: un rectángulo de esquinas redondeadas con grosor.
// Submallas: 0 = cara de arriba (reverso), 1 = cara de abajo (frente), 2 = canto.
// Acostada sobre XZ: ancho en X, largo en Z, grosor en Y.
public static class MallaCarta
{
    static readonly Dictionary<Vector4, Mesh> cache = new Dictionary<Vector4, Mesh>();

    public static Mesh Obtener(float ancho, float largo, float grosor, float radio, int segmentosEsquina = 5)
    {
        var clave = new Vector4(ancho, largo, grosor, radio);
        if (cache.TryGetValue(clave, out var m) && m != null) return m;

        // Contorno en sentido antihorario visto desde arriba
        var contorno = new List<Vector2>();
        float hx = ancho * 0.5f - radio, hz = largo * 0.5f - radio;
        Vector2[] centros = { new Vector2(hx, hz), new Vector2(-hx, hz), new Vector2(-hx, -hz), new Vector2(hx, -hz) };
        for (int c = 0; c < 4; c++)
            for (int s = 0; s <= segmentosEsquina; s++)
            {
                float ang = (c * 90f + 90f * s / segmentosEsquina) * Mathf.Deg2Rad;
                contorno.Add(centros[c] + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * radio);
            }

        var v = new List<Vector3>();
        var n = new List<Vector3>();
        var uv = new List<Vector2>();
        var arriba = new List<int>();
        var abajo = new List<int>();
        var canto = new List<int>();
        float y = grosor * 0.5f;

        // Cara de arriba (reverso): abanico desde el centro
        int c0 = v.Count;
        v.Add(new Vector3(0, y, 0)); n.Add(Vector3.up); uv.Add(new Vector2(0.5f, 0.5f));
        foreach (var p in contorno) { v.Add(new Vector3(p.x, y, p.y)); n.Add(Vector3.up); uv.Add(new Vector2(p.x / ancho + 0.5f, p.y / largo + 0.5f)); }
        for (int i = 0; i < contorno.Count; i++) { arriba.Add(c0); arriba.Add(c0 + 1 + (i + 1) % contorno.Count); arriba.Add(c0 + 1 + i); }

        // Cara de abajo (frente): u invertida para que se lea bien al voltear la carta sobre su eje largo (Z)
        int c1 = v.Count;
        v.Add(new Vector3(0, -y, 0)); n.Add(Vector3.down); uv.Add(new Vector2(0.5f, 0.5f));
        foreach (var p in contorno) { v.Add(new Vector3(p.x, -y, p.y)); n.Add(Vector3.down); uv.Add(new Vector2(0.5f - p.x / ancho, p.y / largo + 0.5f)); }
        for (int i = 0; i < contorno.Count; i++) { abajo.Add(c1); abajo.Add(c1 + 1 + i); abajo.Add(c1 + 1 + (i + 1) % contorno.Count); }

        // Canto: una banda alrededor con normales suaves hacia afuera
        int c2 = v.Count;
        foreach (var p in contorno)
        {
            var normal = new Vector3(p.x, 0, p.y);
            normal.x -= Mathf.Clamp(p.x, -hx, hx);
            normal.z -= Mathf.Clamp(p.y, -hz, hz);
            normal = normal.sqrMagnitude > 0 ? normal.normalized : Vector3.forward;
            v.Add(new Vector3(p.x, y, p.y)); n.Add(normal); uv.Add(Vector2.zero);
            v.Add(new Vector3(p.x, -y, p.y)); n.Add(normal); uv.Add(Vector2.zero);
        }
        for (int i = 0; i < contorno.Count; i++)
        {
            int a = c2 + i * 2, b = c2 + ((i + 1) % contorno.Count) * 2;
            canto.AddRange(new[] { a, b, a + 1, b, b + 1, a + 1 });
        }

        m = new Mesh { name = "Carta" };
        m.SetVertices(v);
        m.SetNormals(n);
        m.SetUVs(0, uv);
        m.subMeshCount = 3;
        m.SetTriangles(arriba, 0);
        m.SetTriangles(abajo, 1);
        m.SetTriangles(canto, 2);
        m.RecalculateBounds();
        m.RecalculateTangents();
        m.hideFlags = HideFlags.DontSave;
        cache[clave] = m;
        return m;
    }
}
