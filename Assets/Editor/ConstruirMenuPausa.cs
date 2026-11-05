using System.IO;
using LiarGame.Red;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Arma en la escena Bar el menú de pausa (botón redondo arriba a la izquierda + menú + instrucciones)
// con el mismo estilo de los demás menús. Se puede volver a correr: borra el anterior y lo arma de nuevo.
// Menú de Unity: LIAR GAME → Construir menú de pausa  (con la escena Bar abierta y sin Play)
public static class ConstruirMenuPausa
{
    const string RutaCirculo = "Assets/UI/HUD/Circulo.png";
    const string RutaAro = "Assets/UI/HUD/Aro.png";

    static readonly Color Oscuro = new Color(0.07f, 0.04f, 0.03f, 0.95f);
    static readonly Color Dorado = new Color(0.561f, 0.451f, 0.227f, 1f);
    static readonly Color DoradoClaro = new Color(0.788f, 0.635f, 0.29f, 1f);
    static readonly Color Naranja = new Color(0.851f, 0.4f, 0.122f, 1f);
    static readonly Color Crema = new Color(0.902f, 0.831f, 0.702f, 1f);

    // Título de cada parte en naranja; el resto, frases cortas y sencillas
    const string Seccion = "<color=#D9661F><size=120%>";
    const string FinSeccion = "</size></color>";

    const string TextoInstrucciones =
        Seccion + "¿DE QUÉ SE TRATA?" + FinSeccion + "\n" +
        "Gana el primero que se quede <b>sin cartas</b>. Entre todos van armando una escalera de números... y se vale <b>mentir</b>.\n\n" +

        Seccion + "EN TU TURNO" + FinSeccion + "\n" +
        "<b>1.</b>  Mira en el letrero de arriba qué número toca.\n" +
        "<b>2.</b>  Elige una carta con las <b>flechas</b> y tírala con <b>E</b>. Cae boca abajo: nadie la ve.\n" +
        "<b>3.</b>  ¿No tienes ese número? Tira otra y haz como si fuera. El <b>comodín</b> vale por cualquiera.\n\n" +

        Seccion + "¿CREES QUE TE MINTIERON?" + FinSeccion + "\n" +
        "Presiona <b>M</b> para gritar <b>¡Mentiroso!</b> a la última carta que cayó.\n" +
        "Si era mentira, el mentiroso se come 3 cartas. Si era verdad... te las comes tú.\n\n" +

        Seccion + "LOS PODERES" + FinSeccion + "\n" +
        "<b>Pass</b> salta al número que quieras.   <b>Return</b> cambia el sentido.   <b>Break</b> elige pares, impares o normal.\n" +
        "Tirar un poder no gasta tu turno: después igual tiras tu carta. Con un poder no se puede ganar, y si solo te quedan poderes, robas 3.\n\n" +

        Seccion + "CONTROLES" + FinSeccion + "\n" +
        "<b>Clic derecho + mouse</b> mirar  ·  <b>Flechas</b> elegir  ·  <b>E</b> tirar  ·  <b>M</b> ¡mentiroso!  ·  <b>H</b> ayuda  ·  <b>Esc</b> pausa";

    [MenuItem("LIAR GAME/Construir menú de pausa")]
    public static void Construir()
    {
        if (EditorApplication.isPlaying) { Debug.LogWarning("[MenuPausa] Detén el Play antes de construir el menú."); return; }

        var menuRed = Object.FindAnyObjectByType<MenuRed>(FindObjectsInactive.Include);
        var hud = GameObject.Find("HUD");
        if (menuRed == null || hud == null) { Debug.LogWarning("[MenuPausa] Abre la escena Bar primero."); return; }

        var red = Object.FindAnyObjectByType<RedJuego>(FindObjectsInactive.Include);
        var botonCrear = menuRed.transform.Find("Panel/Boton Crear");
        var botonUnirse = menuRed.transform.Find("Panel/Boton Unirse");
        var rye = hud.transform.Find("Letrero/Numero").GetComponent<TMP_Text>().font;
        var elite = hud.transform.Find("Aviso Poder/Detalle").GetComponent<TMP_Text>().font;
        var spriteFondo = hud.transform.Find("Ayuda Controles/Completa/Fondo").GetComponent<Image>().sprite;
        var spriteBorde = hud.transform.Find("Ayuda Controles/Completa/Borde").GetComponent<Image>().sprite;
        var brillo = botonCrear.Find("Resplandor").GetComponent<Image>().sprite;
        var circulo = SpriteCirculo(RutaCirculo, false);
        var aro = SpriteCirculo(RutaAro, true);

        var viejo = GameObject.Find("Menu Pausa");
        if (viejo != null) Object.DestroyImmediate(viejo);

        // ---------- Canvas propio, encima del HUD ----------
        var raiz = new GameObject("Menu Pausa", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        raiz.layer = 5;
        var canvas = raiz.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;
        var scaler = raiz.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        var menu = raiz.AddComponent<MenuPausa>();

        // ---------- Botón redondo con el símbolo de pausa ----------
        var boton = Nuevo("Boton Pausa", raiz.transform);
        Anclar(boton, new Vector2(0, 1), new Vector2(28, -28), new Vector2(76, 76));
        var resplandor = Imagen("Resplandor", boton, brillo, Naranja);
        Estirar(resplandor.rectTransform, 70);
        resplandor.raycastTarget = false;
        var fondo = Imagen("Fondo", boton, circulo, new Color(0.07f, 0.04f, 0.03f, 0.88f));
        Estirar(fondo.rectTransform, 0);
        var anillo = Imagen("Aro", boton, aro, Dorado);
        Estirar(anillo.rectTransform, 0);
        anillo.raycastTarget = false;
        for (int i = 0; i < 2; i++)
        {
            var barra = Imagen(i == 0 ? "Barra Izquierda" : "Barra Derecha", boton, null, DoradoClaro);
            Anclar(barra.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(i == 0 ? -8f : 8f, 0f), new Vector2(9f, 28f));
            barra.raycastTarget = false;
        }
        var btn = boton.gameObject.AddComponent<Button>();
        btn.transition = Selectable.Transition.None;
        btn.targetGraphic = fondo;
        UnityEventTools.AddPersistentListener(btn.onClick, new UnityAction(menu.Abrir));
        var anim = new SerializedObject(boton.gameObject.AddComponent<BotonAnimado>());
        anim.FindProperty("fondo").objectReferenceValue = fondo;
        anim.FindProperty("fondoHover").colorValue = new Color(0.32f, 0.16f, 0.06f, 0.95f);
        anim.FindProperty("resplandor").objectReferenceValue = resplandor;
        anim.ApplyModifiedPropertiesWithoutUndo();

        // ---------- Lo que se abre: fondo oscuro + cartel de opciones + cartel de instrucciones ----------
        var panel = Nuevo("Panel", raiz.transform);
        Estirar(panel, 0);
        var oscuro = Imagen("Oscuro", panel, null, new Color(0f, 0f, 0f, 0.65f));   // también bloquea los clics al juego
        Estirar(oscuro.rectTransform, 0);

        var opciones = Caja("Opciones", panel, new Vector2(620, 560), spriteFondo, spriteBorde);
        Texto("Titulo", opciones, "PAUSA", rye, 64, DoradoClaro, new Vector2(0, 205), new Vector2(560, 80), TextAlignmentOptions.Center);
        string[] textos = { "CONTINUAR", "SALIR DE LA PARTIDA", "INSTRUCCIONES", "SALIR DEL JUEGO" };
        UnityAction[] acciones = { menu.Continuar, menu.SalirDePartida, menu.VerInstrucciones, menu.SalirDelJuego };
        for (int i = 0; i < textos.Length; i++)
            Boton(i == 0 ? botonCrear : botonUnirse, opciones, textos[i], acciones[i], new Vector2(0, 95 - i * 95), new Vector2(480, 76));

        var instrucciones = Caja("Instrucciones", panel, new Vector2(1180, 900), spriteFondo, spriteBorde);
        Texto("Titulo", instrucciones, "CÓMO SE JUEGA", rye, 52, DoradoClaro, new Vector2(0, 390), new Vector2(1100, 70), TextAlignmentOptions.Center);
        var cuerpo = Texto("Texto", instrucciones, TextoInstrucciones, elite, 22, Crema, new Vector2(0, 15), new Vector2(1080, 680), TextAlignmentOptions.TopLeft);
        cuerpo.lineSpacing = 6;
        cuerpo.textWrappingMode = TextWrappingModes.Normal;
        Boton(botonUnirse, instrucciones, "VOLVER", menu.VolverDeInstrucciones, new Vector2(0, -385), new Vector2(320, 68));

        // ---------- Conectar el script ----------
        var sm = new SerializedObject(menu);
        sm.FindProperty("red").objectReferenceValue = red;
        sm.FindProperty("botonPausa").objectReferenceValue = boton.gameObject;
        sm.FindProperty("panel").objectReferenceValue = panel.gameObject;
        sm.FindProperty("opciones").objectReferenceValue = opciones.gameObject;
        sm.FindProperty("instrucciones").objectReferenceValue = instrucciones.gameObject;
        sm.FindProperty("menuRed").objectReferenceValue = menuRed.transform.Find("Panel").gameObject;
        sm.ApplyModifiedPropertiesWithoutUndo();

        instrucciones.gameObject.SetActive(false);
        panel.gameObject.SetActive(false);

        EditorSceneManager.MarkSceneDirty(raiz.scene);
        EditorSceneManager.SaveScene(raiz.scene);
        Selection.activeGameObject = raiz;
        Debug.Log("[MenuPausa] Menú de pausa construido y escena guardada.");
    }

    // ================================================================ Ayudas

    static RectTransform Nuevo(string nombre, Transform padre)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;
        var rt = (RectTransform)go.transform;
        rt.SetParent(padre, false);
        return rt;
    }

    static void Anclar(RectTransform rt, Vector2 ancla, Vector2 pos, Vector2 tam)
    {
        rt.anchorMin = rt.anchorMax = rt.pivot = ancla;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
    }

    static void Estirar(RectTransform rt, float extra)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = new Vector2(extra, extra);
    }

    static Image Imagen(string nombre, Transform padre, Sprite sprite, Color color)
    {
        var img = Nuevo(nombre, padre).gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        return img;
    }

    static RectTransform Caja(string nombre, Transform padre, Vector2 tam, Sprite fondo, Sprite borde)
    {
        var caja = Nuevo(nombre, padre);
        Anclar(caja, new Vector2(0.5f, 0.5f), Vector2.zero, tam);
        var f = Imagen("Fondo", caja, fondo, Oscuro);
        f.type = Image.Type.Sliced;
        Estirar(f.rectTransform, 0);
        var b = Imagen("Borde", caja, borde, Dorado);
        b.type = Image.Type.Sliced;
        b.raycastTarget = false;
        Estirar(b.rectTransform, 0);
        return caja;
    }

    static TextMeshProUGUI Texto(string nombre, Transform padre, string texto, TMP_FontAsset fuente, float tam, Color color,
                                 Vector2 pos, Vector2 caja, TextAlignmentOptions alineacion)
    {
        var t = Nuevo(nombre, padre).gameObject.AddComponent<TextMeshProUGUI>();
        Anclar(t.rectTransform, new Vector2(0.5f, 0.5f), pos, caja);
        t.font = fuente;
        t.fontSize = tam;
        t.color = color;
        t.text = texto;
        t.alignment = alineacion;
        t.richText = true;
        t.raycastTarget = false;
        return t;
    }

    // Copia un botón del menú de red (con su animación y estilo) y le cambia el texto y la acción
    static void Boton(Transform plantilla, Transform padre, string texto, UnityAction accion, Vector2 pos, Vector2 tam)
    {
        var go = Object.Instantiate(plantilla.gameObject, padre);
        go.name = "Boton " + texto;
        var rt = (RectTransform)go.transform;
        Anclar(rt, new Vector2(0.5f, 0.5f), pos, tam);
        var b = go.GetComponent<Button>();
        b.onClick = new Button.ButtonClickedEvent();   // quita las acciones del menú de red
        UnityEventTools.AddPersistentListener(b.onClick, accion);
        var t = go.transform.Find("Texto").GetComponent<TMP_Text>();
        t.text = texto;
        t.rectTransform.sizeDelta = tam;
    }

    // Círculo (o aro) blanco con borde suave, guardado como sprite
    static Sprite SpriteCirculo(string ruta, bool soloAro)
    {
        if (!File.Exists(ruta))
        {
            const int n = 256;
            const float grosor = 12f;
            var tex = new Texture2D(n, n, TextureFormat.RGBA32, false);
            float r = n / 2f - 2f;
            var centro = new Vector2(n / 2f, n / 2f);
            for (int y = 0; y < n; y++)
                for (int x = 0; x < n; x++)
                {
                    float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centro);
                    float a = Mathf.Clamp01(r - d + 0.5f);
                    if (soloAro) a *= Mathf.Clamp01(d - (r - grosor) + 0.5f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            File.WriteAllBytes(ruta, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(ruta);
            var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
            imp.textureType = TextureImporterType.Sprite;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }
}
