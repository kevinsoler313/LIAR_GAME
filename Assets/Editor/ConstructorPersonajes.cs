using System.Linq;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Sienta los 4 personajes animados en las sillas de la escena Bar.
// El modelo sale de Blender/Personajes_grupo.blend exportado a Assets/Models/Personajes/Personajes.fbx
// (ya viene en la posición de cada Puesto, con la animación de 192 cuadros horneada).
// Menú: LIAR GAME > Colocar personajes en el bar. Se puede volver a correr: reemplaza el objeto "Personajes".
public static class ConstructorPersonajes
{
    const string carpeta = "Assets/Models/Personajes";
    const string rutaModelo = carpeta + "/Personajes.fbx";
    const string rutaControlador = carpeta + "/Personajes.controller";
    const string carpetaMateriales = carpeta + "/Materiales";
    const string rutaEscena = "Assets/Scenes/Bar.unity";

    // Mismo orden que los puestos de SillasRed (Puesto_0..3)
    static readonly string[] nombres = { "MujerS", "MujerC", "Inseguro", "Viejo" };

    [MenuItem("LIAR GAME/Colocar personajes en el bar")]
    public static void Construir()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        ConfigurarModelo();
        var controlador = CrearControlador();

        var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
        var anterior = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "Personajes");
        if (anterior != null) Object.DestroyImmediate(anterior);

        var modelo = AssetDatabase.LoadAssetAtPath<GameObject>(rutaModelo);
        var raiz = (GameObject)PrefabUtility.InstantiatePrefab(modelo, escena);
        raiz.name = "Personajes";
        // Blender (x, y) llega a Unity como (-x, -z): con 180 grados cada personaje cae en su Puesto
        raiz.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));
        raiz.transform.localScale = Vector3.one;

        var animator = raiz.GetComponent<Animator>();
        if (animator == null) animator = raiz.AddComponent<Animator>();
        animator.runtimeAnimatorController = controlador;
        animator.avatar = AssetDatabase.LoadAllAssetsAtPath(rutaModelo).OfType<Avatar>().First();
        animator.applyRootMotion = false;

        // Unity deja cada malla al lado de su esqueleto (MujerS junto a MujerS_Rig), no adentro
        var sillas = raiz.AddComponent<PersonajesEnSillas>();
        var datos = new SerializedObject(sillas);
        var lista = datos.FindProperty("personajes");
        lista.arraySize = nombres.Length;
        for (int i = 0; i < nombres.Length; i++)
            lista.GetArrayElementAtIndex(i).objectReferenceValue = raiz.transform.Find(nombres[i]).gameObject;
        datos.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Personajes] Colocados en " + rutaEscena);
    }

    // Rig genérico, ejes de Blender convertidos, animación en bucle y materiales URP con la textura horneada
    static void ConfigurarModelo()
    {
        var importador = (ModelImporter)AssetImporter.GetAtPath(rutaModelo);
        importador.animationType = ModelImporterAnimationType.Generic;
        importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;   // sin Avatar el Animator no mueve nada
        importador.bakeAxisConversion = true;
        importador.importCameras = false;
        importador.importLights = false;
        importador.importBlendShapes = false;
        importador.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        if (!AssetDatabase.IsValidFolder(carpetaMateriales))
            AssetDatabase.CreateFolder(carpeta, "Materiales");
        var lit = Shader.Find("Universal Render Pipeline/Lit");
        foreach (var nombre in nombres)
        {
            string ruta = carpetaMateriales + "/" + nombre + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
            if (material == null)
            {
                material = new Material(lit);
                AssetDatabase.CreateAsset(material, ruta);
            }
            material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/Texturas/" + nombre + ".png"));
            material.SetColor("_BaseColor", Color.white);
            material.SetFloat("_Smoothness", 0.15f);
            AsignarNormal(material, carpeta + "/Texturas/" + nombre + "_normal.png");
            EditorUtility.SetDirty(material);
            importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre), material);
        }

        var clips = importador.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = "Partida";
            clip.loopTime = true;
        }
        importador.clipAnimations = clips.Take(1).ToArray();
        importador.SaveAndReimport();
    }

    // Los modelos de Tripo traen normal map: se importa como "Normal map" y va en _BumpMap
    public static void AsignarNormal(Material material, string rutaNormal)
    {
        var importador = AssetImporter.GetAtPath(rutaNormal) as TextureImporter;
        if (importador == null) return;
        if (importador.textureType != TextureImporterType.NormalMap)
        {
            importador.textureType = TextureImporterType.NormalMap;
            importador.SaveAndReimport();
        }
        material.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(rutaNormal));
        material.SetFloat("_BumpScale", 1f);
        material.EnableKeyword("_NORMALMAP");
        EditorUtility.SetDirty(material);
    }

    static AnimatorController CrearControlador()
    {
        var clip = AssetDatabase.LoadAllAssetsAtPath(rutaModelo).OfType<AnimationClip>()
            .First(c => !c.name.StartsWith("__preview__"));
        AssetDatabase.DeleteAsset(rutaControlador);
        return AnimatorController.CreateAnimatorControllerAtPathWithClip(rutaControlador, clip);
    }
}
