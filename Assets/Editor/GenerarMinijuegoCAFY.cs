#if UNITY_EDITOR
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Fase 2: crea el clip de la cafetera en uso, monta la UI de la barra de
/// precisión en PanelMiniJuego y añade el botón invisible de entrega del
/// pedido. Se ejecuta desde Herramientas > CAFY > Generar minijuego y pedido.
/// </summary>
public static class GenerarMinijuegoCAFY
{
    private const string RutaJuego = "Assets/Scenes/JuegoPrincipal.unity";
    private const string CarpetaAnimaciones = "Assets/Animaciones";
    private const string Marcador = "Temp/cafy_minijuego_generado";

    [InitializeOnLoadMethod]
    private static void GenerarAlRecompilar()
    {
        EditorApplication.delayCall += () =>
        {
            if (!File.Exists(Marcador)) Generar();
        };
    }

    [MenuItem("Herramientas/CAFY/Generar minijuego y pedido")]
    public static void Generar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("CAFY: sal del modo Play antes de generar el minijuego.");
            return;
        }

        // La escena ya tiene el minijuego y el botón de entrega colocado a mano.
        // No regenerar para no sobrescribir esos RectTransforms. Si hace falta
        // reconstruir, borrar primero Temp/cafy_minijuego_generado.
        if (File.Exists(Marcador))
        {
            Debug.Log("CAFY: minijuego ya generado; se conserva la distribución manual de la escena.");
            return;
        }

        RuntimeAnimatorController clipCafetera = CrearAnimacionCafetera();

        Scene scene = EditorSceneManager.GetSceneByPath(RutaJuego);
        bool yaAbierta = scene.isLoaded;
        if (!yaAbierta) scene = EditorSceneManager.OpenScene(RutaJuego, OpenSceneMode.Additive);

        GameObject panelMini = Buscar(scene, "PanelMiniJuego");
        GameObject controlador = Buscar(scene, "Controlador");
        Canvas canvas = BuscarConComponente<Canvas>(scene);
        GameObject canvasRaiz = canvas != null ? canvas.gameObject : null;

        if (panelMini == null || controlador == null || canvasRaiz == null)
        {
            Debug.LogError("CAFY: no se encontró PanelMiniJuego, Controlador o Canvas en JuegoPrincipal.");
            return;
        }

        ControladorMinijuego mini = ConstruirMinijuego(panelMini.transform, clipCafetera);
        ControladorPedido pedido = ConstruirPedido(canvasRaiz.transform);

        ControladorInterfazPrincipal interfaz = controlador.GetComponent<ControladorInterfazPrincipal>();
        if (interfaz == null) interfaz = controlador.AddComponent<ControladorInterfazPrincipal>();
        interfaz.minijuego = mini;
        EditorUtility.SetDirty(interfaz);

        mini.pedido = pedido;
        mini.interfaz = interfaz;
        EditorUtility.SetDirty(mini);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!yaAbierta) EditorSceneManager.CloseScene(scene, true);

        File.WriteAllText(Marcador, System.DateTime.Now.ToString("s"));
        Debug.Log("CAFY: minijuego y pedido generados correctamente.");
    }

    private static RuntimeAnimatorController CrearAnimacionCafetera()
    {
        string rutaClip = CarpetaAnimaciones + "/ExitoCafetera.anim";
        string rutaCtrl = CarpetaAnimaciones + "/ExitoCafetera.controller";

        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath("Assets/Sprites/CafeteraUso.png")
            .OfType<Sprite>()
            .OrderBy(s => NumeroFrame(s.name))
            .ToArray();

        if (frames.Length == 0)
        {
            Debug.LogError("CAFY: no hay sprites en CafeteraUso.png");
            return null;
        }

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaClip);
        if (clip == null)
        {
            clip = new AnimationClip { name = "ExitoCafetera" };
            AssetDatabase.CreateAsset(clip, rutaClip);
        }

        clip.frameRate = 12f;
        clip.wrapMode = WrapMode.Loop;
        EditorCurveBinding enlace = EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(clip, enlace, frames
            .Select((s, i) => new ObjectReferenceKeyframe { time = i / 12f, value = s })
            .ToArray());
        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);
        EditorUtility.SetDirty(clip);

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(rutaCtrl);
        if (controller == null)
        {
            controller = AnimatorController.CreateAnimatorControllerAtPath(rutaCtrl);
        }
        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        foreach (ChildAnimatorState estado in maquina.states.ToArray())
        {
            maquina.RemoveState(estado.state);
        }
        AnimatorState loop = maquina.AddState("Loop", new Vector3(320f, 100f, 0f));
        loop.motion = clip;
        maquina.defaultState = loop;
        EditorUtility.SetDirty(controller);

        AssetDatabase.SaveAssets();
        return controller;
    }

    private static ControladorMinijuego ConstruirMinijuego(Transform panel, RuntimeAnimatorController clipCafetera)
    {
        // Limpiamos el placeholder y cualquier generación anterior.
        Limpiar(panel, new[]
        {
            "Text (TMP)", "Titulo", "Instruccion", "Feedback", "BarraFondo",
            "btnAccionar", "ArteExito"
        });

        CrearTexto(panel, "Titulo", "Paso 1 de 3",
            new Vector2(0.2f, 0.86f), new Vector2(0.8f, 0.96f), 46f, Color.white);

        CrearTexto(panel, "Instruccion", "Pulsa cuando el cursor esté en la zona verde",
            new Vector2(0.2f, 0.75f), new Vector2(0.8f, 0.85f), 28f, new Color(0.85f, 0.85f, 0.85f));

        RectTransform barra = CrearImagen(panel, "BarraFondo",
            new Vector2(0.15f, 0.54f), new Vector2(0.85f, 0.62f),
            new Color(0.12f, 0.12f, 0.16f, 0.95f)).rectTransform;

        RectTransform zona = CrearImagen(barra, "ZonaVerde",
            new Vector2(0.38f, 0f), new Vector2(0.62f, 1f),
            new Color(0.25f, 0.75f, 0.35f, 0.9f)).rectTransform;

        RectTransform cursor = CrearImagen(barra, "Cursor",
            Vector2.zero, Vector2.zero, Color.white).rectTransform;
        cursor.anchorMin = new Vector2(0f, 0f);
        cursor.anchorMax = new Vector2(0f, 1f);
        cursor.offsetMin = new Vector2(-6f, 0f);
        cursor.offsetMax = new Vector2(6f, 0f);

        TextMeshProUGUI feedback = CrearTexto(panel, "Feedback", "",
            new Vector2(0.2f, 0.40f), new Vector2(0.8f, 0.51f), 38f, Color.white);

        Button accionar = CrearBoton(panel, "btnAccionar", "¡Accionar!",
            new Vector2(0.42f, 0.22f), new Vector2(0.58f, 0.33f),
            new Color(0.2f, 0.45f, 0.75f, 1f));

        GameObject arte = new GameObject("ArteExito", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
        arte.transform.SetParent(panel, false);
        RectTransform arteRect = arte.GetComponent<RectTransform>();
        arteRect.anchorMin = new Vector2(0.42f, 0.56f);
        arteRect.anchorMax = new Vector2(0.58f, 0.90f);
        arteRect.offsetMin = Vector2.zero;
        arteRect.offsetMax = Vector2.zero;
        Image arteImagen = arte.GetComponent<Image>();
        arteImagen.preserveAspect = true;
        arteImagen.raycastTarget = false;
        arte.SetActive(false);

        ControladorMinijuego mini = panel.gameObject.GetComponent<ControladorMinijuego>();
        if (mini == null) mini = panel.gameObject.AddComponent<ControladorMinijuego>();

        mini.barra = barra;
        mini.zonaVerde = zona;
        mini.cursor = cursor;
        mini.titulo = CrearTextoRef(panel, "Titulo");
        mini.instruccion = CrearTextoRef(panel, "Instruccion");
        mini.feedback = feedback;
        mini.btnAccionar = accionar;
        mini.arteExito = arte;
        mini.animadorExito = arte.GetComponent<Animator>();
        mini.animacionesExito = new RuntimeAnimatorController[3];
        if (clipCafetera != null) mini.animacionesExito[0] = clipCafetera;

        EditorUtility.SetDirty(mini);
        return mini;
    }

    private static ControladorPedido ConstruirPedido(Transform canvas)
    {
        Limpiar(canvas, new[] { "PedidoTexto", "PedidoFeedback", "PedidoEntregar", "PedidoDesechar" });

        TextMeshProUGUI texto = CrearTexto(canvas, "PedidoTexto", "Sin pedido",
            new Vector2(0.40f, 0.30f), new Vector2(0.60f, 0.35f), 26f, Color.white);

        TextMeshProUGUI feedback = CrearTexto(canvas, "PedidoFeedback", "",
            new Vector2(0.40f, 0.25f), new Vector2(0.60f, 0.30f), 24f, Color.white);

        // Botón invisible sobre el cuadro gris del cartel de entregas.
        // El usuario lo colocará manualmente en el Editor.
        Button entregar = CrearBoton(canvas, "PedidoEntregar", "",
            new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.18f),
            new Color(1f, 1f, 1f, 0f), "");
        RectTransform rectEntregar = entregar.GetComponent<RectTransform>();
        rectEntregar.sizeDelta = new Vector2(380f, 180f);

        Button desechar = CrearBoton(canvas, "PedidoDesechar", "Desechar",
            new Vector2(0.42f, 0.10f), new Vector2(0.58f, 0.15f),
            new Color(0.55f, 0.22f, 0.22f, 0.95f));

        GameObject soporte = BuscarONuevo<ControladorPedido>(canvas.gameObject.scene, "PedidoSoporte");
        ControladorPedido pedido = soporte.GetComponent<ControladorPedido>();
        if (pedido == null) pedido = soporte.AddComponent<ControladorPedido>();

        pedido.textoPedido = texto;
        pedido.feedback = feedback;

        UnityEventTools.AddPersistentListener(entregar.onClick, new UnityAction(pedido.EntregarPedido));
        UnityEventTools.AddPersistentListener(desechar.onClick, new UnityAction(pedido.DescartarPedido));

        EditorUtility.SetDirty(pedido);
        EditorUtility.SetDirty(entregar);
        EditorUtility.SetDirty(desechar);
        return pedido;
    }

    private static GameObject BuscarONuevo<T>(Scene scene, string nombre) where T : Component
    {
        T existente = BuscarConComponente<T>(scene);
        if (existente != null) return existente.gameObject;
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(null);
        SceneManager.MoveGameObjectToScene(go, scene);
        return go;
    }

    private static T BuscarConComponente<T>(Scene scene) where T : Component
    {
        foreach (GameObject raiz in scene.GetRootGameObjects())
        {
            T componente = raiz.GetComponent<T>();
            if (componente != null) return componente;
        }
        return null;
    }

    private static void Limpiar(Transform padre, string[] nombres)
    {
        foreach (string nombre in nombres)
        {
            Transform hijo = padre.Find(nombre);
            if (hijo != null) Object.DestroyImmediate(hijo.gameObject);
        }
    }

    private static Image CrearImagen(Transform padre, string nombre, Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(padre, false);
        AplicarRect(go.GetComponent<RectTransform>(), min, max);
        Image imagen = go.GetComponent<Image>();
        imagen.color = color;
        imagen.raycastTarget = false;
        return imagen;
    }

    private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido,
        Vector2 min, Vector2 max, float tamaño, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        AplicarRect(go.GetComponent<RectTransform>(), min, max);

        TextMeshProUGUI texto = go.GetComponent<TextMeshProUGUI>();
        texto.font = FuenteTMP();
        texto.text = contenido;
        texto.fontSize = tamaño;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = color;
        texto.raycastTarget = false;
        return texto;
    }

    private static TextMeshProUGUI CrearTextoRef(Transform padre, string nombre)
    {
        Transform hijo = padre.Find(nombre);
        return hijo != null ? hijo.GetComponent<TextMeshProUGUI>() : null;
    }

    private static Button CrearBoton(Transform padre, string nombre, string etiqueta,
        Vector2 min, Vector2 max, Color color, string _ = null)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        AplicarRect(go.GetComponent<RectTransform>(), min, max);

        Image imagen = go.GetComponent<Image>();
        imagen.color = color;
        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = imagen;

        if (!string.IsNullOrEmpty(etiqueta))
        {
            CrearTexto(go.transform, "Texto", etiqueta, Vector2.zero, Vector2.one, 26f, Color.white);
        }
        return boton;
    }

    private static void AplicarRect(RectTransform rect, Vector2 min, Vector2 max)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static TMP_FontAsset FuenteTMP()
    {
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }

    private static int NumeroFrame(string nombre)
    {
        Match m = Regex.Match(nombre, "_(\\d+)$");
        return m.Success ? int.Parse(m.Groups[1].Value) : 0;
    }

    private static GameObject Buscar(Scene scene, string nombre)
    {
        foreach (GameObject raiz in scene.GetRootGameObjects())
        {
            foreach (Transform hijo in raiz.GetComponentsInChildren<Transform>(true))
            {
                if (hijo.name == nombre) return hijo.gameObject;
            }
        }
        return null;
    }
}
#endif
