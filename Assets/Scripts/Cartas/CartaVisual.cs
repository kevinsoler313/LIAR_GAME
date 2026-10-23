using UnityEngine;

// Una carta física en la mesa. Muestra el reverso arriba; al voltearla se ve el frente.
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class CartaVisual : MonoBehaviour
{
    static readonly int idBaseMap = Shader.PropertyToID("_BaseMap");

    public CartaDatos Datos { get; private set; }

    public void Configurar(CartaDatos datos, Mesh malla, Material reverso, Material frente, Material canto)
    {
        Datos = datos;
        GetComponent<MeshFilter>().sharedMesh = malla;

        var render = GetComponent<MeshRenderer>();
        render.sharedMaterials = new[] { reverso, frente, canto };

        // Todas comparten el material del frente; solo cambia la textura (índice 1 = cara de abajo)
        var bloque = new MaterialPropertyBlock();
        if (datos != null && datos.frente != null) bloque.SetTexture(idBaseMap, datos.frente.texture);
        render.SetPropertyBlock(bloque, 1);
    }

    public bool BocaArriba
    {
        get => Vector3.Dot(transform.up, Vector3.up) < 0f;
        set
        {
            var e = transform.localEulerAngles;
            transform.localRotation = Quaternion.Euler(e.x, e.y, value ? 180f : 0f);
        }
    }
    public void Revelar(CartaDatos datos){
        Datos=datos;
        var render = GetComponent<MeshRenderer>();
        var bloque = new MaterialPropertyBlock();
        render.GetPropertyBlock(bloque,1);
        if(datos!=null && datos.frente != null){
            bloque.SetTexture(idBaseMap,datos.frente.texture);
        }
        render.SetPropertyBlock(bloque,1);
    }
}
