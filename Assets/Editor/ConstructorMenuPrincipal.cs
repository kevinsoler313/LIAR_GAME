using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.TextCore.LowLevel;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using TMPro;

// Arma la escena MainMenu con la estética de bar vaquero (la misma de la landing web).
// Menú: LIAR GAME > Construir menú principal. Se puede volver a correr cuando quieras;
// si pones las fuentes en Assets/UI/Fuentes (Rye, Special Elite, Crimson Pro) las usa solas.
public static class ConstructorMenuPrincipal
{
    const string carpeta = "Assets/UI/MenuPrincipal";
    const string carpetaFuentes = "Assets/UI/Fuentes";
    const string rutaEscena = "Assets/Scenes/MainMenu.unity";
    const string rutaEscenaJuego = "Assets/Scenes/Bar.unity";

    // Paleta: café, negro, amarillo cafesoso y naranja
    static readonly Color negro = Hex("0b0705");
    static readonly Color cafeOscuro = Hex("241510");
    static readonly Color cafe = Hex("3d2417");
    static readonly Color cafeMedio = Hex("5a3620");
    static readonly Color mostaza = Hex("c9a24a");
    static readonly Color mostazaTenue = Hex("8f733a");
    static readonly Color naranja = Hex("d9661f");
    static readonly Color naranjaBrillo = Hex("ff8a3d");
    static readonly Color texto = Hex("e6d4b3");
    static readonly Color textoTenue = Hex("a8916d");
    static readonly Color papel = Hex("d9c39a");
    static readonly Color tinta = Hex("2a1a10");
    static readonly Color rojoOxido = Hex("6a1e08");

    static TMP_FontAsset fuenteTitulos, fuenteTexto, fuenteMaquina;
    static Sprite spriteRedondeado, spriteBorde, spriteCarta, spriteBrillo, spriteVineta;
    static Texture2D texturaMadera;

    [MenuItem("LIAR GAME/Construir menú principal")]
    public static void Construir()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Directory.CreateDirectory(carpeta + "/Texturas");
        CrearTexturas();
        CargarFuentes();

        var escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Cámara: solo limpia la pantalla en negro, la UI va en overlay
        var camara = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
        camara.tag = "MainCamera";
        var cam = camara.GetComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = negro;

        new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));

        var canvasGO = new GameObject("Canvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.layer = 5;   // capa UI
        canvasGO.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920, 1080);
        escalador.matchWidthOrHeight = 0.5f;
        var canvas = canvasGO.transform;

        var menu = canvasGO.AddComponent<MenuPrincipal>();

        CrearFondo(canvas);
        var botonSalir = CrearPortada(canvas, menu);
        CrearPanelInstrucciones(canvas, menu, botonSalir);

        EditorSceneManager.SaveScene(escena, rutaEscena);

        // MainMenu primero, luego el juego
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(rutaEscena, true),
            new EditorBuildSettingsScene(rutaEscenaJuego, true),
        };

        Debug.Log("Menú principal construido en " + rutaEscena);
    }

    // ------------------------------------------------------------------ Fondo

    static void CrearFondo(Transform canvas)
    {
        Imagen(Estirar("Fondo", canvas), null, negro);

        var madera = Estirar("Madera", canvas).gameObject.AddComponent<RawImage>();
        madera.texture = texturaMadera;
        madera.uvRect = new Rect(0, 0, 1920f / texturaMadera.width, 1);
        madera.color = new Color(0.5f, 0.5f, 0.5f, 1);
        madera.raycastTarget = false;

        // Viga de madera arriba
        var viga = Nuevo("Viga", canvas, new Vector2(0, -11), new Vector2(0, 22));
        viga.anchorMin = new Vector2(0, 1); viga.anchorMax = new Vector2(1, 1);
        Imagen(viga, null, cafeMedio);

        // Faroles a los lados y un resplandor detrás del título
        Farol("Farol Izquierdo", canvas, new Vector2(-960, 230), 950, 0.32f);
        Farol("Farol Derecho", canvas, new Vector2(960, 230), 950, 0.32f);
        Farol("Resplandor Titulo", canvas, new Vector2(0, 260), 1300, 0.14f);

        Imagen(Estirar("Viñeta", canvas), spriteVineta, Color.white, false);
    }

    static void Farol(string nombre, Transform padre, Vector2 pos, float tam, float alfa)
    {
        var img = Imagen(Nuevo(nombre, padre, pos, new Vector2(tam, tam)), spriteBrillo, new Color(naranja.r, naranja.g, naranja.b, alfa), false);
        img.gameObject.AddComponent<ParpadeoLuz>();
    }

    // ------------------------------------------------------------------ Portada

    static GameObject CrearPortada(Transform canvas, MenuPrincipal menu)
    {
        var portada = Estirar("Portada", canvas);

        var etiqueta = Texto("Etiqueta", portada, "SALOON · ABIERTO TODA LA NOCHE", fuenteMaquina, 24, naranja, new Vector2(0, 445), new Vector2(1200, 40));
        etiqueta.characterSpacing = 25;

        var arriba = Texto("El bar de los", portada, "EL BAR DE LOS", fuenteMaquina, 32, mostazaTenue, new Vector2(0, 380), new Vector2(1200, 50));
        arriba.characterSpacing = 40;

        // Título con relieve: dos copias desplazadas detrás hacen de sombra
        Texto("Titulo Sombra 2", portada, "LIAR GAME", fuenteTitulos, 210, negro, new Vector2(0, 243), new Vector2(1800, 240));
        Texto("Titulo Sombra 1", portada, "LIAR GAME", fuenteTitulos, 210, Hex("5a3f17"), new Vector2(0, 249), new Vector2(1800, 240));
        Texto("Titulo", portada, "LIAR GAME", fuenteTitulos, 210, mostaza, new Vector2(0, 255), new Vector2(1800, 240));

        var abajo = Texto("Mentirosos", portada, "MENTIROSOS", fuenteMaquina, 32, mostazaTenue, new Vector2(0, 130), new Vector2(1200, 50));
        abajo.characterSpacing = 40;

        // Tres cartas boca abajo en abanico
        Carta(portada, new Vector2(-85, -25), 14);
        Carta(portada, new Vector2(85, -25), -14);
        Carta(portada, new Vector2(0, -5), 0);

        Boton("Boton Jugar", portada, "JUGAR", true, new Vector2(0, -200), new Vector2(460, 88), menu.Jugar);
        Boton("Boton Instrucciones", portada, "INSTRUCCIONES", false, new Vector2(0, -305), new Vector2(460, 72), menu.MostrarInstrucciones);
        var salir = Boton("Boton Salir", portada, "SALIR", false, new Vector2(0, -395), new Vector2(460, 64), menu.Salir);

        var pie = Texto("Pie", portada, "Cartas boca abajo. Caras de póker. Gana el que se queda sin cartas.", fuenteTexto, 26, textoTenue, new Vector2(0, -495), new Vector2(1400, 40));
        pie.fontStyle = FontStyles.Italic;

        return salir.gameObject;
    }

    static void Carta(Transform padre, Vector2 pos, float giro)
    {
        var rt = Nuevo("Carta", padre, pos, new Vector2(124, 180));
        rt.localRotation = Quaternion.Euler(0, 0, giro);
        Imagen(rt, spriteCarta, Color.white, false);
    }

    // ------------------------------------------------------------------ Instrucciones

    static void CrearPanelInstrucciones(Transform canvas, MenuPrincipal menu, GameObject botonSalir)
    {
        var panel = Estirar("Panel Instrucciones", canvas);
        var oscuro = Imagen(panel, null, new Color(0, 0, 0, 0.93f));
        oscuro.raycastTarget = true;   // tapa los botones del menú de atrás

        // Marco de madera con clavos y una "pizarra" oscura adentro
        var marco = Nuevo("Marco", panel, Vector2.zero, new Vector2(1400, 860));
        Imagen(marco, spriteRedondeado, cafeMedio);
        Imagen(Estirar("Borde", marco, -4), spriteBorde, negro);
        var interior = Estirar("Interior", marco, 18);
        Imagen(interior, spriteRedondeado, new Color(negro.r, negro.g, negro.b, 0.95f));
        Imagen(Estirar("Borde Interior", interior), spriteBorde, mostazaTenue);
        foreach (var esquina in new[] { new Vector2(-1, 1), new Vector2(1, 1), new Vector2(-1, -1), new Vector2(1, -1) })
            Imagen(Nuevo("Clavo", marco, new Vector2(esquina.x * 691, esquina.y * 421), new Vector2(12, 12)), spriteRedondeado, mostazaTenue);

        var cabecera = Texto("Cabecera", interior, "INSTRUCCIONES", fuenteMaquina, 22, naranja, new Vector2(0, 372), new Vector2(600, 36));
        cabecera.characterSpacing = 30;

        var cerrar = Boton("Boton Cerrar", interior, "X", false, new Vector2(625, 365), new Vector2(64, 64), menu.CerrarInstrucciones);
        cerrar.GetComponentInChildren<TMP_Text>().fontSize = 30;

        var paginas = new[]
        {
            PaginaComoSeJuega(interior),
            PaginaMentiroso(interior),
            PaginaPoderes(interior),
            PaginaVictoria(interior, menu),
        };

        var anterior = Boton("Boton Anterior", interior, "< ANTERIOR", false, new Vector2(-480, -360), new Vector2(280, 64), menu.PaginaAnterior);
        var siguiente = Boton("Boton Siguiente", interior, "SIGUIENTE >", true, new Vector2(480, -360), new Vector2(280, 64), menu.PaginaSiguiente);
        var numero = Texto("Numero Pagina", interior, "1 / 4", fuenteMaquina, 24, textoTenue, new Vector2(0, -360), new Vector2(200, 40));

        // Conectar todo con el controlador
        var so = new SerializedObject(menu);
        so.FindProperty("panelInstrucciones").objectReferenceValue = panel.gameObject;
        var listaPaginas = so.FindProperty("paginas");
        listaPaginas.arraySize = paginas.Length;
        for (int i = 0; i < paginas.Length; i++)
            listaPaginas.GetArrayElementAtIndex(i).objectReferenceValue = paginas[i];
        so.FindProperty("textoPagina").objectReferenceValue = numero;
        so.FindProperty("botonAnterior").objectReferenceValue = anterior.gameObject;
        so.FindProperty("botonSiguiente").objectReferenceValue = siguiente.gameObject;
        so.FindProperty("botonSalir").objectReferenceValue = botonSalir;
        so.ApplyModifiedPropertiesWithoutUndo();

        panel.gameObject.SetActive(false);
    }

    static RectTransform Pagina(Transform interior, string nombre, string titulo)
    {
        var pagina = Estirar(nombre, interior);
        if (titulo != null)
        {
            Texto("Titulo", pagina, titulo, fuenteTitulos, 64, mostaza, new Vector2(0, 300), new Vector2(1100, 90));
            Imagen(Nuevo("Linea", pagina, new Vector2(0, 252), new Vector2(520, 2)), null, mostazaTenue);
        }
        return pagina;
    }

    static GameObject PaginaComoSeJuega(Transform interior)
    {
        var p = Pagina(interior, "Pagina Como Se Juega", "Cómo se juega");

        Texto("Intro", p, "Un juego de cartas tipo escalera, con todas las cartas ocultas. Entre todos completan la escalera; cada uno intenta deshacerse de su mano.",
            fuenteTexto, 28, textoTenue, new Vector2(0, 195), new Vector2(1100, 80));

        string[,] mazo = { { "1 – 10", "CARTAS NUMÉRICAS" }, { "3", "PODERES" }, { "1", "COMODÍN" } };
        for (int i = 0; i < 3; i++)
        {
            var caja = Nuevo("Mazo " + mazo[i, 1], p, new Vector2((i - 1) * 290, 85), new Vector2(260, 110));
            Imagen(caja, spriteRedondeado, new Color(cafeOscuro.r, cafeOscuro.g, cafeOscuro.b, 0.8f));
            Imagen(Estirar("Borde", caja), spriteBorde, cafeMedio);
            Texto("Numero", caja, mazo[i, 0], fuenteTitulos, 50, naranja, new Vector2(0, 14), new Vector2(240, 60));
            var etiqueta = Texto("Etiqueta", caja, mazo[i, 1], fuenteMaquina, 17, textoTenue, new Vector2(0, -32), new Vector2(240, 30));
            etiqueta.characterSpacing = 10;
        }

        string[,] pasos =
        {
            { "I", "Abre el del 1", "El jugador que tenga la carta con el número <color=#c9a24a>1</color> la pone primero en el monto." },
            { "II", "Turno a la derecha", "Sigue el jugador de la derecha. Desde aquí, la escalera se completa entre todos." },
            { "III", "Boca abajo, siempre", "Nadie ve lo que tiras. Puedes jugar la carta que toca... o una que no es." },
            { "IV", "Vacía tu mano", "Cada jugador intenta quedarse sin cartas antes que los demás." },
        };
        for (int i = 0; i < 4; i++)
        {
            var caja = Nuevo("Paso " + pasos[i, 0], p, new Vector2(-465 + i * 310, -150), new Vector2(290, 240));
            Imagen(caja, spriteRedondeado, new Color(cafe.r, cafe.g, cafe.b, 0.45f));
            Imagen(Estirar("Borde", caja), spriteBorde, cafeMedio);
            Imagen(Nuevo("Linea Superior", caja, new Vector2(0, 118), new Vector2(280, 4)), null, mostazaTenue);

            var ficha = Nuevo("Ficha", caja, new Vector2(-95, 120), new Vector2(54, 54));
            Imagen(ficha, spriteRedondeado, naranja);
            Imagen(Estirar("Borde", ficha, -3), spriteBorde, negro);
            Texto("Numero", ficha, pasos[i, 0], fuenteTitulos, 22, negro, Vector2.zero, new Vector2(54, 54));

            var tituloPaso = Texto("Titulo", caja, pasos[i, 1], fuenteTitulos, 26, mostaza, new Vector2(0, 58), new Vector2(260, 44));
            tituloPaso.textWrappingMode = TextWrappingModes.NoWrap;   // siempre en una línea; se achica si no cabe
            tituloPaso.enableAutoSizing = true;
            tituloPaso.fontSizeMin = 16;
            tituloPaso.fontSizeMax = 26;
            var cuerpo = Texto("Texto", caja, pasos[i, 2], fuenteTexto, 22, textoTenue, new Vector2(0, -42), new Vector2(256, 140));
            cuerpo.alignment = TextAlignmentOptions.Top;
        }
        return p.gameObject;
    }

    static GameObject PaginaMentiroso(Transform interior)
    {
        var p = Pagina(interior, "Pagina Mentiroso", null);

        // Póster de "Se busca" en papel viejo, un poco torcido
        var poster = Nuevo("Poster", p, new Vector2(0, -10), new Vector2(980, 600));
        poster.localRotation = Quaternion.Euler(0, 0, -1.2f);
        Imagen(poster, spriteRedondeado, papel);
        Imagen(Estirar("Manchas", poster), spriteVineta, new Color(0.35f, 0.18f, 0.06f, 0.75f), false);
        Imagen(Nuevo("Clavo", poster, new Vector2(0, 280), new Vector2(16, 16)), spriteRedondeado, Hex("3a2a18"));

        var seBusca = Texto("Se Busca", poster, "SE BUSCA", fuenteTitulos, 42, cafe, new Vector2(0, 225), new Vector2(800, 56));
        seBusca.characterSpacing = 20;
        Texto("Mentiroso", poster, "¡MENTIROSO!", fuenteTitulos, 96, rojoOxido, new Vector2(0, 135), new Vector2(940, 120));
        Texto("Texto", poster, "Miente tirando cartas que no son. O acusa a quien crees que mintió.", fuenteTexto, 28, tinta, new Vector2(0, 45), new Vector2(860, 50));

        string[,] veredictos =
        {
            { "LO DESCUBRISTE", "El que mintió <color=#6a1e08><b>se come 3 cartas</b></color>." },
            { "ACUSASTE EN FALSO", "Si no era mentira, el que acusó <color=#6a1e08><b>se come 3 cartas</b></color>." },
        };
        for (int i = 0; i < 2; i++)
        {
            var caja = Nuevo("Veredicto " + (i + 1), poster, new Vector2(i == 0 ? -220 : 220, -85), new Vector2(410, 150));
            Imagen(caja, spriteBorde, new Color(tinta.r, tinta.g, tinta.b, 0.45f));
            var titulo = Texto("Titulo", caja, veredictos[i, 0], fuenteMaquina, 22, tinta, new Vector2(0, 38), new Vector2(380, 36));
            titulo.characterSpacing = 8;
            Texto("Texto", caja, veredictos[i, 1], fuenteTexto, 25, tinta, new Vector2(0, -20), new Vector2(370, 80));
        }

        Imagen(Nuevo("Linea", poster, new Vector2(0, -195), new Vector2(860, 3)), null, new Color(tinta.r, tinta.g, tinta.b, 0.5f));
        Texto("Recompensa", poster, "Recompensa: 3 cartas", fuenteTitulos, 36, tinta, new Vector2(0, -240), new Vector2(800, 56));
        return p.gameObject;
    }

    static GameObject PaginaPoderes(Transform interior)
    {
        var p = Pagina(interior, "Pagina Poderes", "Los poderes");

        string[,] poderes =
        {
            { "P", "PASS", "Se pone <color=#c9a24a>boca arriba</color> al lado del monto y salta la escalera al número que el jugador decida, poniendo ese número en el monto boca abajo." },
            { "R", "RETURN", "El sentido de la escalera <color=#c9a24a>se invierte</color> e inicia en el número que el jugador decida lanzar, boca abajo." },
            { "B", "BRAKE", "Elige el orden de la escalera: <color=#c9a24a>pares, impares o normal</color>. Inicia con la carta que el jugador decida siguiendo ese orden, boca abajo en el monto." },
        };
        for (int i = 0; i < 3; i++)
        {
            var carta = Nuevo("Poder " + poderes[i, 1], p, new Vector2((i - 1) * 410, -40), new Vector2(370, 470));
            Imagen(carta, spriteRedondeado, cafe);
            Imagen(Nuevo("Brillo", carta, new Vector2(0, 200), new Vector2(520, 300)), spriteBrillo, new Color(naranja.r, naranja.g, naranja.b, 0.25f), false);
            Imagen(Estirar("Borde", carta), spriteBorde, mostazaTenue);

            var sello = Nuevo("Sello", carta, new Vector2(0, 160), new Vector2(92, 92));
            Imagen(sello, spriteRedondeado, negro);
            Imagen(Estirar("Borde", sello), spriteBorde, naranja);
            Texto("Letra", sello, poderes[i, 0], fuenteTitulos, 50, naranjaBrillo, new Vector2(0, 2), new Vector2(92, 92));

            Texto("Nombre", carta, poderes[i, 1], fuenteTitulos, 44, mostaza, new Vector2(0, 75), new Vector2(340, 60));
            var cuerpo = Texto("Texto", carta, poderes[i, 2], fuenteTexto, 23, texto, new Vector2(0, -95), new Vector2(320, 240));
            cuerpo.alignment = TextAlignmentOptions.Top;
        }
        return p.gameObject;
    }

    static GameObject PaginaVictoria(Transform interior, MenuPrincipal menu)
    {
        var p = Pagina(interior, "Pagina Ley Y Victoria", "Ley del saloon");

        var caja = Nuevo("Ley", p, new Vector2(0, 110), new Vector2(980, 210));
        Imagen(caja, spriteRedondeado, new Color(0.23f, 0.07f, 0.04f, 0.6f));
        Imagen(Estirar("Borde", caja), spriteBorde, naranja);
        var barra = Nuevo("Barra", caja, new Vector2(5, 0), new Vector2(10, 0));
        barra.anchorMin = new Vector2(0, 0); barra.anchorMax = new Vector2(0, 1);
        Imagen(barra, null, naranja);

        Texto("Regla", caja, "Un poder <color=#c9a24a>no anula</color> a un poder diferente.", fuenteTexto, 32, texto, new Vector2(0, 55), new Vector2(900, 50));
        Texto("No", caja, "<color=#ff8a3d><b>NO</b></color>   Return no anula un Brake.", fuenteTexto, 30, texto, new Vector2(0, -5), new Vector2(900, 46));
        Texto("Si", caja, "<color=#c9a24a><b>SÍ</b></color>   Return puede anular un Return.", fuenteTexto, 30, texto, new Vector2(0, -55), new Vector2(900, 46));

        var etiqueta = Texto("Etiqueta", p, "LA ÚNICA REGLA QUE IMPORTA", fuenteMaquina, 22, naranja, new Vector2(0, -70), new Vector2(900, 36));
        etiqueta.characterSpacing = 25;
        Texto("Victoria", p, "Gana el que se queda sin cartas", fuenteTitulos, 62, mostaza, new Vector2(0, -145), new Vector2(1250, 90));

        Boton("Boton Jugar", p, "¡A JUGAR!", true, new Vector2(0, -250), new Vector2(340, 72), menu.Jugar);
        return p.gameObject;
    }

    // ------------------------------------------------------------------ Piezas de UI

    static RectTransform Nuevo(string nombre, Transform padre, Vector2 pos, Vector2 tam)
    {
        var go = new GameObject(nombre, typeof(RectTransform));
        go.layer = 5;   // capa UI
        go.transform.SetParent(padre, false);
        var rt = (RectTransform)go.transform;
        rt.anchoredPosition = pos;
        rt.sizeDelta = tam;
        return rt;
    }

    static RectTransform Estirar(string nombre, Transform padre, float margen = 0)
    {
        var rt = Nuevo(nombre, padre, Vector2.zero, Vector2.zero);
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = new Vector2(margen, margen);
        rt.offsetMax = new Vector2(-margen, -margen);
        return rt;
    }

    static Image Imagen(RectTransform rt, Sprite sprite, Color color, bool cortada = true)
    {
        var img = rt.gameObject.AddComponent<Image>();
        img.sprite = sprite;
        img.type = sprite != null && cortada ? Image.Type.Sliced : Image.Type.Simple;
        img.color = color;
        img.raycastTarget = false;
        return img;
    }

    static TextMeshProUGUI Texto(string nombre, Transform padre, string contenido, TMP_FontAsset fuente, float tam, Color color, Vector2 pos, Vector2 caja)
    {
        var t = Nuevo(nombre, padre, pos, caja).gameObject.AddComponent<TextMeshProUGUI>();
        if (fuente != null) t.font = fuente;
        t.text = contenido;
        t.fontSize = tam;
        t.color = color;
        t.alignment = TextAlignmentOptions.Center;
        t.textWrappingMode = TextWrappingModes.Normal;
        t.raycastTarget = false;
        return t;
    }

    static Button Boton(string nombre, Transform padre, string etiqueta, bool primario, Vector2 pos, Vector2 tam, UnityAction accion)
    {
        var rt = Nuevo(nombre, padre, pos, tam);
        var fondo = Imagen(rt, spriteRedondeado, primario ? naranja : mostaza);
        fondo.raycastTarget = true;

        if (primario)
        {
            // doble borde como en la web: negro y luego mostaza
            Imagen(Estirar("Borde Negro", rt, -3), spriteBorde, negro);
            Imagen(Estirar("Borde", rt, -7), spriteBorde, mostazaTenue);
        }
        else
        {
            Imagen(Estirar("Borde", rt), spriteBorde, mostazaTenue);
        }

        Texto("Texto", rt, etiqueta, fuenteTitulos, primario ? 36 : 28, primario ? negro : mostaza, Vector2.zero, tam);

        var boton = rt.gameObject.AddComponent<Button>();
        boton.targetGraphic = fondo;
        var colores = boton.colors;
        if (primario)
        {
            colores.normalColor = new Color(0.88f, 0.88f, 0.88f);
            colores.highlightedColor = Color.white;
            colores.pressedColor = new Color(0.7f, 0.7f, 0.7f);
        }
        else
        {
            // el fondo es mostaza: lo oscurecemos casi a negro y al pasar el mouse se ilumina un poco
            colores.normalColor = new Color(0.07f, 0.05f, 0.04f, 0.9f);
            colores.highlightedColor = new Color(0.3f, 0.24f, 0.17f, 1f);
            colores.pressedColor = new Color(0.45f, 0.36f, 0.25f, 1f);
        }
        colores.selectedColor = colores.normalColor;
        colores.fadeDuration = 0.12f;
        boton.colors = colores;
        boton.navigation = new Navigation { mode = Navigation.Mode.None };

        UnityEventTools.AddPersistentListener(boton.onClick, accion);
        return boton;
    }

    // ------------------------------------------------------------------ Fuentes

    static void CargarFuentes()
    {
        var porDefecto = TMP_Settings.defaultFontAsset;
        fuenteTitulos = Fuente("Rye-Regular.ttf", porDefecto);
        fuenteMaquina = Fuente("SpecialElite-Regular.ttf", porDefecto);
        fuenteTexto = Fuente("CrimsonPro-Regular.ttf", porDefecto);
    }

    static TMP_FontAsset Fuente(string archivo, TMP_FontAsset respaldo)
    {
        string ruta = carpetaFuentes + "/" + archivo;
        var ttf = AssetDatabase.LoadAssetAtPath<Font>(ruta);
        if (ttf == null) return respaldo;

        string rutaAsset = carpetaFuentes + "/" + Path.GetFileNameWithoutExtension(archivo) + " SDF.asset";
        var existente = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(rutaAsset);
        if (existente != null) return existente;

        var fa = TMP_FontAsset.CreateFontAsset(ttf, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024);
        AssetDatabase.CreateAsset(fa, rutaAsset);
        fa.atlasTextures[0].name = ttf.name + " Atlas";
        AssetDatabase.AddObjectToAsset(fa.atlasTextures[0], fa);
        fa.material.name = ttf.name + " Material";
        AssetDatabase.AddObjectToAsset(fa.material, fa);
        AssetDatabase.SaveAssets();
        return fa;
    }

    // ------------------------------------------------------------------ Texturas generadas

    static void CrearTexturas()
    {
        spriteRedondeado = ComoSprite(Guardar(Redondeado(64, 64, 12, 0), "Redondeado"), new Vector4(16, 16, 16, 16));
        spriteBorde = ComoSprite(Guardar(Redondeado(64, 64, 12, 3), "Borde"), new Vector4(16, 16, 16, 16));
        spriteCarta = ComoSprite(Guardar(CartaBocaAbajo(248, 360), "Carta"), Vector4.zero);
        spriteBrillo = ComoSprite(Guardar(Brillo(256), "Brillo"), Vector4.zero);
        spriteVineta = ComoSprite(Guardar(Vineta(256), "Vineta"), Vector4.zero);

        string rutaMadera = Guardar(Madera(512, 1024), "Madera");
        var imp = (TextureImporter)AssetImporter.GetAtPath(rutaMadera);
        imp.wrapModeU = TextureWrapMode.Repeat;
        imp.wrapModeV = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        texturaMadera = AssetDatabase.LoadAssetAtPath<Texture2D>(rutaMadera);
    }

    static string Guardar(Texture2D tex, string nombre)
    {
        string ruta = carpeta + "/Texturas/" + nombre + ".png";
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        return ruta;
    }

    static Sprite ComoSprite(string ruta, Vector4 borde)
    {
        var imp = (TextureImporter)AssetImporter.GetAtPath(ruta);
        imp.textureType = TextureImporterType.Sprite;
        imp.spriteImportMode = SpriteImportMode.Single;
        imp.spriteBorder = borde;
        imp.alphaIsTransparency = true;
        imp.mipmapEnabled = false;
        imp.wrapMode = TextureWrapMode.Clamp;
        imp.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    // Distancia con signo a un rectángulo redondeado (negativa adentro)
    static float DistanciaRect(float x, float y, float w, float h, float r)
    {
        float qx = Mathf.Abs(x - w * 0.5f) - (w * 0.5f - r);
        float qy = Mathf.Abs(y - h * 0.5f) - (h * 0.5f - r);
        float ox = Mathf.Max(qx, 0), oy = Mathf.Max(qy, 0);
        return Mathf.Sqrt(ox * ox + oy * oy) + Mathf.Min(Mathf.Max(qx, qy), 0) - r;
    }

    static Texture2D NuevaTextura(int w, int h) => new Texture2D(w, h, TextureFormat.RGBA32, false);

    // grosor 0 = relleno; grosor > 0 = solo el contorno
    static Texture2D Redondeado(int w, int h, float radio, float grosor)
    {
        var tex = NuevaTextura(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistanciaRect(x + 0.5f, y + 0.5f, w, h, radio);
                float a = Mathf.Clamp01(0.5f - d);
                if (grosor > 0) a *= Mathf.Clamp01(d + grosor + 0.5f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D CartaBocaAbajo(int w, int h)
    {
        var tex = NuevaTextura(w, h);
        var centro = new Vector2(w * 0.5f, h * 0.5f);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = DistanciaRect(x + 0.5f, y + 0.5f, w, h, 16);
                float dc = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), centro);

                Color c = ((x + y) / 14) % 2 == 0 ? cafe : cafeOscuro;   // rayas diagonales
                if (dc < 38 && dc > 34) c = mostazaTenue;                  // aro dorado
                if (dc < 22) c = naranja;                                  // centro naranja
                if (d > -20 && d < -14) c = negro;                         // marco negro interior
                if (d > -5) c = mostazaTenue;                              // borde exterior

                c.a = Mathf.Clamp01(0.5f - d);
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Texture2D Brillo(int tam)
    {
        var tex = NuevaTextura(tam, tam);
        float mitad = tam * 0.5f;
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float t = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(mitad, mitad)) / mitad;
                tex.SetPixel(x, y, new Color(1, 1, 1, Mathf.Pow(Mathf.Clamp01(1 - t), 2.2f)));
            }
        tex.Apply();
        return tex;
    }

    static Texture2D Vineta(int tam)
    {
        var tex = NuevaTextura(tam, tam);
        for (int y = 0; y < tam; y++)
            for (int x = 0; x < tam; x++)
            {
                float dx = (x + 0.5f) / tam - 0.5f, dy = (y + 0.5f) / tam - 0.5f;
                float t = Mathf.Sqrt(dx * dx + dy * dy) / 0.7071f;
                tex.SetPixel(x, y, new Color(0, 0, 0, Mathf.SmoothStep(0, 0.9f, Mathf.InverseLerp(0.45f, 1f, t))));
            }
        tex.Apply();
        return tex;
    }

    // Tablones verticales; el ancho es múltiplo del tablón para que se repita sin costura
    static Texture2D Madera(int w, int h)
    {
        const int anchoTablon = 128;
        var tex = NuevaTextura(w, h);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                int tablon = x / anchoTablon;
                int local = x % anchoTablon;
                float tono = 0.8f + 0.25f * Mathf.Repeat(Mathf.Sin(tablon * 12.9898f) * 43758.5453f, 1f);
                float veta = Mathf.PerlinNoise(x * 0.09f + tablon * 13f, y * 0.004f);
                float fibra = Mathf.PerlinNoise(x * 0.7f, y * 0.03f + tablon * 7f);
                Color c = cafe * (tono * Mathf.Lerp(0.7f, 1.15f, veta) * Mathf.Lerp(0.9f, 1.05f, fibra));

                bool juntaVertical = local < 3 || local >= anchoTablon - 1;
                bool juntaHorizontal = Mathf.Abs(y - (tablon * 331 + 200) % h) < 2;
                if (juntaVertical || juntaHorizontal) c = negro;

                c.a = 1;
                tex.SetPixel(x, y, c);
            }
        tex.Apply();
        return tex;
    }

    static Color Hex(string hex)
    {
        ColorUtility.TryParseHtmlString("#" + hex, out var c);
        return c;
    }
}
