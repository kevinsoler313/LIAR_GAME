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
    static readonly string[] acciones = { "Jugar", "Comer", "Acusar", "Beber" };
    // la botella de cerveza que cada uno tiene en la mesa viene en su FBX (Botella_Cerveza.blend):
    // material de Blender -> (material de Unity, color, textura, brillo, metal)
    static readonly (string blender, string unity, Color color, string textura, float brillo, float metal)[] materialesBotella =
    {
        ("Vidrio_Ambar", "Botella_Vidrio", new Color(0.8f, 0.42f, 0.1f), null, 0.9f, 0f),
        ("Cerveza_Liquido", "Botella_Cerveza", new Color(0.55f, 0.28f, 0.04f), null, 0.85f, 0f),
        ("Etiqueta_Cerveza", "Botella_Etiqueta", Color.white, "Etiqueta_Cerveza", 0.2f, 0f),
        ("Etiqueta_Cuello", "Botella_Cuello", Color.white, "Etiqueta_Cuello", 0.25f, 0f),
        ("Tapa_Metal", "Botella_Tapa", new Color(0.75f, 0.6f, 0.3f), null, 0.65f, 1f),
    };
    // luz chica y cálida frente a cada cara: la única luz de la escena es la lámpara de arriba
    // y el ala del sombrero les dejaba la cara en negro
    const float intensidadLuzCara = 0.22f;
    const float alcanceLuzCara = 0.45f;
    static readonly Color colorLuzCara = new Color(1f, 0.82f, 0.62f);

    // Bartender y mesero, de pie (Saludar y Entregar son de una vez)
    static readonly string[] personal = { "Bartender", "Mesero" };
    static readonly string[] enBuclePersonal = { "Limpiar_Vaso", "Limpiar_Barra", "Esperar", "Caminar", "Charlar", "Tirar", "Esperar_Taco" };
    // clientes sentados (Personajes_Cliente/Clienta.blend): salen con la silla en el origen y la mesa al frente,
    // a la distancia de cada uno (la clienta tiene los brazos más cortos y se sienta más cerca)
    static readonly (string nombre, float distanciaMesa)[] clientes = { ("Cliente", 0.9f), ("Clienta", 0.85f) };
    const float escalaPersonal = 0.9f;      // como los jugadores
    // tres jugando billar alrededor del objeto "Billar" (centro de la mesa; su Z es el largo y su +X el lado del salón).
    // Personajes_Cliente_Billar/Clienta_Billar.blend: de pie con su taco, acciones Tirar y Esperar_Taco
    const string sufijoBillar = "_Billar";
    const string carpetaMaterialesBillar = "Assets/Models/Ambiente/Billar/Materiales";
    // de los pies del que tira al centro de la blanca: la punta del taco la toca justo en el tiro (Tirar de lyer_billar.py)
    const float distanciaTiro = 0.825f;
    // Tiran por turnos (TurnosBillar). Cada uno tiene el punto donde le queda la blanca y hacia dónde tira (locales a
    // "Billar"); se para a distanciaTiro detrás de ese punto. El primero tira desde la punta hacia el segundo punto
    // (la bola queda en su línea); los otros dos tiran desde el lado del salón y la bola rebota en la banda de la pared.
    // El primer punto es donde viene la blanca en el modelo (su z se lee de la mesa); el triángulo está en la otra punta.
    static readonly (string modelo, float zBola, Vector3 tira)[] jugadoresBillar =
    {
        ("Cliente_Billar", float.NaN, Vector3.forward),
        ("Clienta_Billar", -0.25f, Vector3.left),
        ("Cliente_Billar", 0.32f, Vector3.left),
    };
    // su animación apoya las manos en una barra de 1,07 m (x0,9 = 0,96 m) y la barra del bar quedó a 0,83 m:
    // se hunde detrás del mostrador, que le tapa los pies
    const float hundirBartender = 0.13f;

    [MenuItem("LIAR GAME/Colocar personajes en el bar")]
    public static void Construir()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        if (!AssetDatabase.IsValidFolder(carpetaControladores))
            AssetDatabase.CreateFolder(carpeta, "Controladores");
        var controladores = nombres.Select(n => { ConfigurarModelo(n, enBucle); return CrearControlador(n); }).ToArray();

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
            luces[i] = CrearLuzCara(cabezas[i], puestos[i].forward);
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

    // Bartender detrás de la barra, mesero caminando con su bandeja, clientes en las mesas nuevas y tres jugando billar
    // (Personajes_Bartender/Mesero/Cliente/Clienta.blend). Van en su propio objeto "PersonalBar": no se mezclan con los jugadores. Se puede volver a correr.
    [MenuItem("LIAR GAME/Colocar bartender y mesero")]
    public static void ConstruirPersonal()
    {
        if (!Application.isBatchMode && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        foreach (var n in personal) ConfigurarModelo(n, enBuclePersonal);
        var controladores = personal.Select(CrearControladorPersonal).ToArray();

        var escena = EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
        foreach (var anterior in escena.GetRootGameObjects().Where(g => g.name == "PersonalBar" || g.name.StartsWith("PersonalBar (Missing Prefab")))
            Object.DestroyImmediate(anterior);
        var raiz = new GameObject("PersonalBar").transform;

        // la barra marca dónde va cada uno: el bartender entre el mostrador y la contrabarra, el mesero por delante
        var barra = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "BarraLicor");
        float frenteBarra = barra ? Limites(barra).min.z : 2.3f;
        float contrabarra = barra ? barra.GetComponentsInChildren<Renderer>().First(r => r.name == "Contrabarra").bounds.min.z : 4.0f;

        // los modelos miran a +Z: el bartender gira para mirar a la mesa
        var bartender = ColocarPersonal("Bartender", controladores[0], raiz,
            new Vector3(-0.8f, -hundirBartender, contrabarra - 0.38f), Quaternion.Euler(0, 180, 0));
        var comp = bartender.AddComponent<Bartender>();
        var datos = new SerializedObject(comp);
        datos.FindProperty("animador").objectReferenceValue = bartender.GetComponentInChildren<Animator>();
        datos.ApplyModifiedPropertiesWithoutUndo();

        // ruta del mesero: recoge en la barra y atiende las mesas de MesasExtra (cada punto "Mesa_i" mira a su mesa);
        // los "Paso" lo hacen rodear la mesa del juego y las sillas
        var ruta = new GameObject("RutaMesero").transform;
        ruta.SetParent(raiz, false);
        float frente = frenteBarra - 0.42f;
        var extras = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "MesasExtra");
        Vector3 MesaExtra(int i, Vector3 siNoHay) => extras && extras.transform.Find("Mesa_" + i) ? extras.transform.Find("Mesa_" + i).position : siNoHay;
        var puntos = new (string nombre, Vector3 pos, Vector3 mira)[]
        {
            ("Barra", new Vector3(-1.2f, 0, frente), new Vector3(-1.2f, 0, frenteBarra)),
            ("Paso_1", new Vector3(-1.55f, 0, 0.9f), Vector3.zero),   // entre la mesa del juego y los de billar
            ("Mesa_1", new Vector3(-2.0f, 0, -1.05f), MesaExtra(1, Vector3.zero)),
            ("Paso_2", new Vector3(0f, 0, -2.25f), Vector3.zero),
            ("Mesa_2", new Vector3(2.0f, 0, -1.05f), MesaExtra(2, Vector3.zero)),
            ("Mesa_3", new Vector3(2.0f, 0, 1.15f), MesaExtra(3, Vector3.zero)),
            ("Paso_3", new Vector3(1.6f, 0, frente), Vector3.zero),   // vuelve a la barra por delante de ella
        };
        foreach (var (nombre, pos, mira) in puntos)
        {
            var p = new GameObject(nombre).transform;
            p.SetParent(ruta, false);
            p.position = pos;
            var hacia = mira - pos;
            hacia.y = 0;
            if (hacia.sqrMagnitude > 1e-4f) p.rotation = Quaternion.LookRotation(hacia);
        }

        var mesero = ColocarPersonal("Mesero", controladores[1], raiz, puntos[0].pos, Quaternion.identity);
        // el cuarto solo está iluminado en la mesa y la barra: una luz suave lo acompaña para que no se pierda en lo oscuro
        var luz = new GameObject("LuzMesero").AddComponent<Light>();
        luz.transform.SetParent(mesero.transform, false);
        luz.transform.localPosition = new Vector3(0, 1.9f, 0.5f);
        luz.type = LightType.Point;
        luz.color = new Color(1f, 0.72f, 0.45f);
        luz.intensity = 0.7f;
        luz.range = 2.2f;
        luz.shadows = LightShadows.None;
        var compMesero = mesero.AddComponent<Mesero>();
        datos = new SerializedObject(compMesero);
        datos.FindProperty("animador").objectReferenceValue = mesero.GetComponentInChildren<Animator>();
        datos.FindProperty("ruta").objectReferenceValue = ruta;
        datos.FindProperty("bartender").objectReferenceValue = comp;
        datos.ApplyModifiedPropertiesWithoutUndo();

        // clientes en todas las sillas de MesasExtra, alternando cliente y clienta (cada mesa empieza con otro)
        if (extras)
        {
            var controladoresClientes = clientes.ToDictionary(c => c.nombre, c =>
            {
                ConfigurarModelo(c.nombre, enBuclePersonal);
                return CrearControladorPersonal(c.nombre);
            });
            int k = 0, mesaNum = 0;
            foreach (Transform mesa in extras.transform)
            {
                var sillas = mesa.Cast<Transform>().Where(t => t.name.StartsWith("Chair")).ToArray();
                for (int i = 0; i < sillas.Length; i++)
                {
                    var (nombre, distancia) = clientes[(i + mesaNum) % clientes.Length];
                    var hacia = mesa.position - sillas[i].position;
                    hacia.y = 0;
                    // cada animación espera la mesa a su distancia: si la silla quedó más lejos, se sienta más adelante
                    var pos = sillas[i].position + hacia.normalized * (hacia.magnitude - distancia);
                    pos.y = 0;
                    var c = ColocarPersonal(nombre, controladoresClientes[nombre], raiz, pos, Quaternion.LookRotation(hacia));
                    c.name = nombre + "_" + (++k);
                    var compCliente = c.AddComponent<Cliente>();
                    datos = new SerializedObject(compCliente);
                    datos.FindProperty("animador").objectReferenceValue = c.GetComponentInChildren<Animator>();
                    datos.ApplyModifiedPropertiesWithoutUndo();
                }
                mesaNum++;
            }
        }

        var billar = escena.GetRootGameObjects().FirstOrDefault(g => g.name == "Billar");
        if (billar)
        {
            var controladoresBillar = jugadoresBillar.Select(j => j.modelo).Distinct().ToDictionary(n => n, n =>
            {
                ConfigurarModelo(n, enBuclePersonal);
                return CrearControladorPersonal(n);
            });
            var blanca = billar.GetComponentsInChildren<Transform>().First(x => x.name == "Bola_0");
            blanca.gameObject.isStatic = false;   // estática, Unity la junta con la mesa y no se puede mover
            var enMesa = billar.transform.InverseTransformPoint(blanca.position);
            var puntosBola = new Vector3[jugadoresBillar.Length];
            var turnos = new Billarista[jugadoresBillar.Length];
            for (int b = 0; b < jugadoresBillar.Length; b++)
            {
                var (modelo, zBola, tira) = jugadoresBillar[b];
                puntosBola[b] = new Vector3(0f, enMesa.y, float.IsNaN(zBola) ? enMesa.z : zBola);
                var pos = billar.transform.TransformPoint(puntosBola[b] - tira * distanciaTiro);
                pos.y = 0;
                var j = ColocarPersonal(modelo, controladoresBillar[modelo], raiz, pos, Quaternion.LookRotation(billar.transform.TransformDirection(tira)));
                j.name = "Billar_" + (b + 1) + "_" + modelo.Replace(sufijoBillar, "");
                turnos[b] = j.AddComponent<Billarista>();
                datos = new SerializedObject(turnos[b]);
                datos.FindProperty("animador").objectReferenceValue = j.GetComponentInChildren<Animator>();
                datos.ApplyModifiedPropertiesWithoutUndo();
            }
            var anterior = billar.GetComponent<TurnosBillar>();
            if (anterior) Object.DestroyImmediate(anterior);
            datos = new SerializedObject(billar.AddComponent<TurnosBillar>());
            Llenar(datos, "jugadores", turnos);
            var lista = datos.FindProperty("puntosBola");
            lista.arraySize = puntosBola.Length;
            for (int b = 0; b < puntosBola.Length; b++) lista.GetArrayElementAtIndex(b).vector3Value = puntosBola[b];
            datos.FindProperty("bola").objectReferenceValue = blanca;
            datos.ApplyModifiedPropertiesWithoutUndo();
        }

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena);
        Debug.Log("[Personajes] Bartender, mesero, clientes y billar colocados en " + rutaEscena);
    }

    // Devuelve el objeto que se mueve (su +Z es hacia donde mira); el modelo va adentro girado 180 grados
    // porque estos dos salen de Blender mirando a -Z
    static GameObject ColocarPersonal(string nombre, AnimatorController controlador, Transform raiz, Vector3 posicion, Quaternion giro)
    {
        var cuerpo = new GameObject(nombre).transform;
        cuerpo.SetParent(raiz, false);
        cuerpo.SetPositionAndRotation(posicion, giro);

        string ruta = carpeta + "/" + nombre + ".fbx";
        var modelo = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(ruta), raiz.gameObject.scene);
        modelo.transform.SetParent(cuerpo, false);
        modelo.transform.localRotation = Quaternion.Euler(0, 180, 0);
        modelo.transform.localScale = Vector3.one * escalaPersonal;

        var animador = modelo.GetComponent<Animator>();
        if (animador == null) animador = modelo.AddComponent<Animator>();
        animador.runtimeAnimatorController = controlador;
        animador.avatar = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Avatar>().First();
        animador.applyRootMotion = false;
        animador.cullingMode = AnimatorCullingMode.CullUpdateTransforms;   // fuera de cámara no mueve huesos, pero sigue el estado

        var cabeza = Buscar(modelo.transform, "head");
        CrearLuzCara(cabeza, cuerpo.forward);
        return cuerpo.gameObject;
    }

    // Un estado por toma, sin transiciones: Bartender y Mesero eligen qué hacer con CrossFade
    static AnimatorController CrearControladorPersonal(string nombre)
    {
        var clips = AssetDatabase.LoadAllAssetsAtPath(carpeta + "/" + nombre + ".fbx").OfType<AnimationClip>()
            .Where(c => !c.name.StartsWith("__preview__")).ToDictionary(c => c.name);
        string rutaControlador = carpetaControladores + "/" + nombre + ".controller";
        AssetDatabase.DeleteAsset(rutaControlador);
        var controlador = AnimatorController.CreateAnimatorControllerAtPath(rutaControlador);
        var maquina = controlador.layers[0].stateMachine;
        foreach (var clip in clips.Keys.OrderBy(c => c)) Estado(maquina, clip, clips);
        maquina.defaultState = maquina.states.Select(s => s.state).FirstOrDefault(s => s.name == "Esperar") ?? maquina.states[0].state;
        AssetDatabase.SaveAssets();
        return controlador;
    }

    static Bounds Limites(GameObject go)
    {
        var rs = go.GetComponentsInChildren<Renderer>();
        var b = rs[0].bounds;
        foreach (var r in rs) b.Encapsulate(r.bounds);
        return b;
    }

    // Hija del hueso de la cabeza, un poco al frente y abajo de la cara: gira con ella y se mete bajo el ala
    static Light CrearLuzCara(Transform cabeza, Vector3 frente)
    {
        var luz = new GameObject("LuzCara").AddComponent<Light>();
        luz.type = LightType.Point;
        luz.color = colorLuzCara;
        luz.intensity = intensidadLuzCara;
        luz.range = alcanceLuzCara;
        luz.shadows = LightShadows.None;
        luz.transform.SetParent(cabeza, false);
        luz.transform.position = cabeza.position + frente * 0.35f + Vector3.down * 0.05f;
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
    static void ConfigurarModelo(string nombre, string[] bucles)
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
        // los jugadores de billar (Cliente_Billar...) son el mismo modelo del cliente: usan su material y su textura
        string base_ = nombre.EndsWith(sufijoBillar) ? nombre.Substring(0, nombre.Length - sufijoBillar.Length) : nombre;
        string rutaMaterial = carpetaMateriales + "/" + base_ + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(rutaMaterial);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, rutaMaterial);
        }
        material.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/Texturas/" + base_ + ".png"));
        material.SetColor("_BaseColor", Color.white);
        material.SetFloat("_Smoothness", 0.15f);
        AsignarNormal(material, carpeta + "/Texturas/" + base_ + "_normal.png");
        // sin emisión (la cara la ilumina su LuzCara)
        material.SetTexture("_EmissionMap", null);
        material.SetColor("_EmissionColor", Color.black);
        material.DisableKeyword("_EMISSION");
        EditorUtility.SetDirty(material);
        importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), nombre), material);
        if (nombres.Contains(nombre))   // solo los de la mesa del juego traen botella
            foreach (var m in materialesBotella)
                importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.blender), MaterialBotella(m));
        // el taco usa los mismos materiales que la mesa de billar (Taco_Arce, Billar_Laton...)
        if (nombre.EndsWith(sufijoBillar) && AssetDatabase.IsValidFolder(carpetaMaterialesBillar))
            foreach (var guid in AssetDatabase.FindAssets("t:Material", new[] { carpetaMaterialesBillar }))
            {
                var m = AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(guid));
                importador.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material), m.name), m);
            }

        // Blender nombra cada toma "<nombre>_Rig|<acción>": el clip se queda solo con la acción
        var clips = importador.defaultClipAnimations;
        foreach (var clip in clips)
        {
            clip.name = clip.takeName.Split('|').Last();
            clip.loopTime = bucles.Contains(clip.name);
        }
        importador.clipAnimations = clips;
        importador.SaveAndReimport();
    }

    static Material MaterialBotella((string blender, string unity, Color color, string textura, float brillo, float metal) m)
    {
        string ruta = carpetaMateriales + "/" + m.unity + ".mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(ruta);
        if (material == null)
        {
            material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            AssetDatabase.CreateAsset(material, ruta);
        }
        material.SetTexture("_BaseMap", m.textura == null ? null : AssetDatabase.LoadAssetAtPath<Texture2D>(carpeta + "/Texturas/" + m.textura + ".png"));
        material.SetColor("_BaseColor", m.color);
        material.SetFloat("_Smoothness", m.brillo);
        material.SetFloat("_Metallic", m.metal);
        EditorUtility.SetDirty(material);
        return material;
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
        controlador.AddParameter("Desfase", AnimatorControllerParameterType.Float);   // cada uno empieza sus loops en otro punto

        var maquina = controlador.layers[0].stateMachine;
        var descansar = Estado(maquina, "Descansar", clips);
        var esperar = Estado(maquina, "Esperar", clips);
        var turno = Estado(maquina, "MiTurno", clips);
        var ganar = Estado(maquina, "Ganar", clips);
        maquina.defaultState = descansar;   // antes de repartir nadie tiene cartas
        // sin esto, al terminar el reparto los cuatro entran a Esperar a la vez y hacen los mismos gestos sincronizados
        foreach (var bucle in new[] { descansar, esperar, turno })
        {
            bucle.cycleOffsetParameter = "Desfase";
            bucle.cycleOffsetParameterActive = true;
        }

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
