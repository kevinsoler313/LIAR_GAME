using System.Linq;
using LiarGame.Red;
using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;

// Sienta los 4 personajes animados en las sillas de la escena Bar y los conecta con la partida.
// Cada uno sale de Blender/Personajes_<nombre>.blend exportado a Assets/Models/Personajes/<nombre>.fbx
// (ya viene en la posición de su Puesto, con sus acciones: Descansar, Esperar, MiTurno, Jugar, Comer, Acusar y Ganar).
// Menú: LIAR GAME > Colocar personajes en el bar. Se puede volver a correr: reemplaza el objeto "Personajes".
public static class ConstructorPersonajes
{
    const string carpeta = "Assets/Models/Personajes";
    const string carpetaMateriales = carpeta + "/Materiales";
    const string carpetaControladores = carpeta + "/Controladores";
    const string rutaEscena = "Assets/Scenes/Bar.unity";

    // Mismo orden que los puestos de SillasRed (Puesto_0..3)
    static readonly string[] nombres = { "MujerS", "MujerC", "Inseguro", "Viejo" };
    static readonly string[] enBucle = { "Esperar", "Descansar", "MiTurno", "Ganar" };
    static readonly string[] acciones = { "Jugar", "Comer", "Acusar" };
    // luz chica y cálida frente a cada cara: la única luz de la escena es la lámpara de arriba
    // y el ala del sombrero les dejaba la cara en negro
    const float intensidadLuzCara = 0.22f;
    const float alcanceLuzCara = 0.45f;
    static readonly Color colorLuzCara = new Color(1f, 0.82f, 0.62f);

    [MenuItem("LIAR GAME/Colocar personajes en el bar")]
    public static void Construir()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(carpetaControladores))
            AssetDatabase.CreateFolder(carpeta, "Controladores");
        var controladores = nombres.Select(n => { ConfigurarModelo(n); return CrearControlador(n); }).ToArray();

        var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
        // si su modelo ya no existe, Unity lo renombra "Personajes (Missing Prefab ...)": también se borra
        foreach (var anterior in escena.GetRootGameObjects().Where(g => g.name == "Personajes" || g.name.StartsWith("Personajes (Missing Prefab")))
            Object.DestroyImmediate(anterior);

        // Blender (x, y) llega a Unity como (-x, -z): con 180 grados cada personaje cae en su Puesto
        var raiz = new GameObject("Personajes");
        raiz.transform.SetPositionAndRotation(Vector3.zero, Quaternion.Euler(0, 180, 0));

        var mallas = new GameObject[nombres.Length];
        var animadores = new Animator[nombres.Length];
        var cabezas = new Transform[nombres.Length];
        var luces = new Light[nombres.Length];
        var manosIzquierdas = new Transform[nombres.Length];
        var puestos = new Transform[nombres.Length];
        var anclas = new Transform[nombres.Length];
        var puestosRaiz = escena.GetRootGameObjects().First(g => g.name == "PUESTOS").transform;
        for (int i = 0; i < nombres.Length; i++)
        {
            string ruta = carpeta + "/" + nombres[i] + ".fbx";
            var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), escena);
            modelo.transform.SetParent(raiz.transform, false);

            var animador = modelo.GetComponent<Animator>();
            if (animador == null) animador = modelo.AddComponent<Animator>();
            animador.runtimeAnimatorController = controladores[i];
            animador.avatar = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Avatar>().First();
            animador.applyRootMotion = false;
            animador.cullingMode = AnimatorCullingMode.AlwaysAnimate;   // la cabeza y las cartas dependen de los huesos

            // Unity deja la malla al lado de su esqueleto (MujerS junto a MujerS_Rig), no adentro
            mallas[i] = modelo.transform.Find(nombres[i]).gameObject;
            animadores[i] = animador;
            cabezas[i] = Buscar(modelo.transform, "head");
            manosIzquierdas[i] = Buscar(modelo.transform, "hand.L");
            puestos[i] = puestosRaiz.Find("Puesto_" + i);
            luces[i] = CrearLuzCara(cabezas[i], puestos[i]);
            anclas[i] = puestos[i].Find("Ojos/Mano en mano");
        }

        var personajes = raiz.AddComponent<PersonajesEnSillas>();
        var datos = new SerializedObject(personajes);
        Llenar(datos, "personajes", mallas);
        Llenar(datos, "animadores", animadores);
        Llenar(datos, "cabezas", cabezas);
        Llenar(datos, "lucesCara", luces);
        Llenar(datos, "manosIzquierdas", manosIzquierdas);
        Llenar(datos, "puestos", puestos);
        Llenar(datos, "anclasCartas", anclas);
        Llenar(datos, "manos", Object.FindObjectsByType<ManoJugador>().OrderBy(m => m.Jugador).ToArray());
        datos.FindProperty("partida").objectReferenceValue = Object.FindFirstObjectByType<PartidaRed>();
        datos.FindProperty("mesa").objectReferenceValue = Object.FindFirstObjectByType<MesaRed>();
        datos.FindProperty("red").objectReferenceValue = Object.FindFirstObjectByType<RedJuego>();
        datos.FindProperty("monto").objectReferenceValue = escena.GetRootGameObjects().First(g => g.name == "Mesa").transform.Find("Monto");
        datos.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Personajes] Colocados en " + rutaEscena);
    }

    // Hija del hueso de la cabeza, un poco al frente y abajo de la cara: gira con ella y se mete bajo el ala
    static Light CrearLuzCara(Transform cabeza, Transform puesto)
    {
        var luz = new GameObject("LuzCara").AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorLuzCara;
        luz.intensity = intensidadLuzCara;
        luz.range = alcanceLuzCara;
        luz.shadows = LightShadows.None;
        luz.transform.SetParent(cabeza, false);
        luz.transform.position = cabeza.position + puesto.forward * 0.35f + Vector3.down * 0.05f;
        return luz;
    }

    static Transform Buscar(Transform raiz, string nombre) =>
        raiz.GetComponentsInChildren<Transform>(true).First(t => t.name == nombre);

    static void Llenar(SerializedObject datos, string campo, Object[] valores)
    {
        var lista = datos.FindProperty(campo);
        lista.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            lista.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
    }

    // Rig genérico, ejes de Blender convertidos, una toma por acción y el material URP del personaje
    static void ConfigurarModelo(string nombre)
    {
        var importador = (ModelImporter)AssetImporter.GetAtPath(carpeta + "/" + nombre + ".fbx");
        importador.animationType = ModelImporterAnimationType.Generic;
        importador.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;   // sin Avatar el Animator no mueve nada
        importador.bakeAxisConversion = true;
        importador.importCameras = false;
        importador.importLights = false;
        importador.importBlendShapes = false;
        importador.materialImportMode = ModelImporterMaterialImportMode.ImportStandard;

        if (!AssetDatabase.IsValidFolder(carpetaMateriales))
            AssetDatabase.CreateFolder(carpeta, "Materiales");
        string rutaMaterial = carpetaMateriales + "/" + nombre + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, rutaMaterial);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/Texturas/" + nombre + ".png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.15f);
        AsignarNormal(material, carpeta + "/Texturas/" + nombre + "_normal.png");
        // sin emisión (la cara la ilumina su LuzCara)
        material.SetTexture("_EmissionMap", null);
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre), material);

        // Blender nombra cada toma "<nombre>_Rig|<acción>": el clip se queda solo con la acción
        var clips = importador.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = clip.takeName.Split('|').Last();
            clip.loopTime = enBucle.Contains(clip.name);
        }
        importador.clipAnimations = clips;
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

    // Descansar (sin cartas) ⇄ Esperar (con cartas) ⇄ MiTurno según el turno; Ganar mientras gane;
    // Jugar, Comer y Acusar desde cualquier estado y al terminar vuelven a MiTurno, Esperar o Descansar.
    static AnimatorController CrearControlador(string nombre)
    {
        string ruta = carpeta + "/" + nombre + ".fbx";
        var clips = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);

        string rutaControlador = carpetaControladores + "/" + nombre + ".controller";
        AssetDatabase.DeleteAsset(rutaControlador);
        var controlador = AnimatorController.CreateAnimatorControllerAtPath(rutaControlador);
        controlador.AddParameter("MiTurno", AnimatorControllerParameterType.Bool);
        controlador.AddParameter("ConCartas", AnimatorControllerParameterType.Bool);
        controlador.AddParameter("Gano", AnimatorControllerParameterType.Bool);
        foreach (var accion in acciones) controlador.AddParameter(accion, AnimatorControllerParameterType.Trigger);

        var maquina = controlador.layers[0].stateMachine;
        var descansar = Estado(maquina, "Descansar", clips);
        var esperar = Estado(maquina, "Esperar", clips);
        var turno = Estado(maquina, "MiTurno", clips);
        var ganar = Estado(maquina, "Ganar", clips);
        maquina.defaultState = descansar;   // antes de repartir nadie tiene cartas

        Transicion(descansar.AddTransition(esperar), 0.4f, ("ConCartas", true));
        Transicion(esperar.AddTransition(descansar), 0.4f, ("ConCartas", false));
        Transicion(esperar.AddTransition(turno), 0.3f, ("MiTurno", true));
        Transicion(turno.AddTransition(esperar), 0.3f, ("MiTurno", false), ("ConCartas", true));
        Transicion(turno.AddTransition(descansar), 0.3f, ("MiTurno", false), ("ConCartas", false));
        var aGanar = maquina.AddAnyStateTransition(ganar);
        aGanar.canTransitionToSelf = false;
        Transicion(aGanar, 0.3f, ("Gano", true));
        Transicion(ganar.AddTransition(descansar), 0.3f, ("Gano", false));

        foreach (var accion in acciones)
        {
            var estado = Estado(maquina, accion, clips);
            var entrar = maquina.AddAnyStateTransition(estado);
            entrar.canTransitionToSelf = false;
            Transicion(entrar, 0.15f, (accion, true));
            Transicion(estado.AddTransition(turno), 0.25f, true, ("MiTurno", true));
            Transicion(estado.AddTransition(esperar), 0.25f, true, ("MiTurno", false), ("ConCartas", true));
            Transicion(estado.AddTransition(descansar), 0.25f, true, ("MiTurno", false), ("ConCartas", false));
        }
        AssetDatabase.SaveAssets();
        return controlador;
    }

    static AnimatorState Estado(AnimatorStateMachine maquina, string nombre, System.Collections.Generic.Dictionary<string, AnimationClip> clips)
    {
        var estado = maquina.AddState(nombre);
        estado.motion = clips[nombre];
        return estado;
    }

    // Cambia apenas se cumplen las condiciones
    static void Transicion(AnimatorStateTransition t, float duracion, params (string parametro, bool valor)[] condiciones) =>
        Transicion(t, duracion, false, condiciones);

    // alTerminar: las salidas de Jugar/Comer/Acusar además esperan a que termine la acción
    static void Transicion(AnimatorStateTransition t, float duracion, bool alTerminar, params (string parametro, bool valor)[] condiciones)
    {
        t.hasExitTime = alTerminar;
        if (alTerminar) t.exitTime = 0.92f;
        t.duration = duracion;
        foreach (var (parametro, valor) in condiciones)
            t.AddCondition(valor ? AnimatorConditionMode.If : AnimatorConditionMode.IfNot, 0, parametro);
    }
}
