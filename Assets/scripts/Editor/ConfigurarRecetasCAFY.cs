#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Crea las recetas como ScriptableObjects y añade un PanelRecetas nuevo al
/// Canvas. Solo añade objetos: no borra ni mueve nada de lo que ya está en la
/// escena, para respetar los botones colocados manualmente.
/// </summary>
public static class ConfigurarRecetasCAFY
{
    private const string RutaJuego = "Assets/Scenes/JuegoPrincipal.unity";
    private const string CarpetaRecetas = "Assets/Resources/Recetas";
    private const string Marcador = "Temp/cafy_recetas_configuradas";

    private static readonly Color Ambar = new Color(0.95f, 0.62f, 0.22f, 0.95f);
    private static readonly Color Crema = new Color(0.95f, 0.88f, 0.72f, 0.95f);
    private static readonly Color Marron = new Color(0.55f, 0.30f, 0.16f, 0.95f);
    private static readonly Color Amarillo = new Color(0.95f, 0.82f, 0.30f, 0.95f);
    private static readonly Color Caramelo = new Color(0.78f, 0.45f, 0.18f, 0.95f);

    // Auto-configuración desactivada: sobrescribía la escena al recargar Unity.
    // [InitializeOnLoadMethod]
    // private static void ConfigurarAlRecompilar()
    // {
    //     EditorApplication.delayCall += () =>
    //     {
    //         if (!File.Exists(Marcador)) Configurar();
    //     };
    // }

    [MenuItem("Herramientas/CAFY/Configurar recetas")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("CAFY: sal del modo Play antes de configurar las recetas.");
            return;
        }

        if (File.Exists(Marcador))
        {
            Debug.Log("CAFY: recetas ya configuradas; no se modifica la escena.");
            return;
        }

        Receta[] recetas = CrearRecetas();

        Scene scene = EditorSceneManager.GetSceneByPath(RutaJuego);
        bool yaAbierta = scene.isLoaded;
        if (!yaAbierta) scene = EditorSceneManager.OpenScene(RutaJuego, OpenSceneMode.Additive);

        Canvas canvas = BuscarEnHijos<Canvas>(scene);
        if (canvas == null)
        {
            Debug.LogError("CAFY: no se encontró Canvas en JuegoPrincipal.");
            return;
        }

        GameObject panel = Buscar(scene, "PanelRecetas");
        if (panel != null && panel.GetComponent<ControladorRecetas>() != null)
        {
            File.WriteAllText(Marcador, System.DateTime.Now.ToString("s"));
            Debug.Log("CAFY: PanelRecetas ya está en la escena; no se modifica.");
            if (!yaAbierta) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        if (panel == null) panel = CrearPanel(canvas.transform);
        panel.SetActive(false);

        ControladorRecetas control = panel.GetComponent<ControladorRecetas>();
        if (control == null) control = panel.AddComponent<ControladorRecetas>();

        RectTransform contenedor = panel.transform.Find("ContenedorRecetas") as RectTransform;
        Button cerrar = BuscarBoton(panel.transform, "btnCerrarRecetas");

        control.panel = panel;
        control.contenedor = contenedor;
        control.recetas = recetas;
        control.botonCerrar = cerrar;

        if (cerrar != null)
        {
            UnityEventTools.AddPersistentListener(cerrar.onClick, new UnityAction(control.Cerrar));
            EditorUtility.SetDirty(cerrar);
        }

        ControladorTelefono telefono = BuscarEnHijos<ControladorTelefono>(scene);
        if (telefono != null)
        {
            telefono.panelRecetas = control;
            EditorUtility.SetDirty(telefono);
        }

        EditorUtility.SetDirty(control);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!yaAbierta) EditorSceneManager.CloseScene(scene, true);

        File.WriteAllText(Marcador, System.DateTime.Now.ToString("s"));
        Debug.Log("CAFY: recetas y PanelRecetas configurados correctamente.");
    }

    private static Receta[] CrearRecetas()
    {
        if (!AssetDatabase.IsValidFolder(CarpetaRecetas))
        {
            AssetDatabase.CreateFolder("Assets", "Recetas");
        }

        RuntimeAnimatorController cafetera = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animaciones/ExitoCafetera.controller");
        RuntimeAnimatorController jarabe = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animaciones/Jarabe.controller");

        Receta espresso = CrearReceta(
            "Espresso", "Café concentrado. Controla la presión de extracción.",
            "Assets/Imagenes/taza CAFY.png",
            new[] { Ingrediente.Cafe },
            new[] { "Extracción" },
            new[] { cafetera },
            new[] { 1.1f },
            new[] { 0.24f },
            new[] { Ambar });

        Receta capuchino = CrearReceta(
            "Capuchino", "Espresso con leche vaporizada y espuma.",
            "Assets/Imagenes/Leche CAFY.png",
            new[] { Ingrediente.Cafe, Ingrediente.Leche },
            new[] { "Extracción", "Vaporizado de leche" },
            new RuntimeAnimatorController[] { cafetera, null },
            new[] { 1.1f, 1.7f },
            new[] { 0.24f, 0.16f },
            new[] { Ambar, Crema });

        Receta mocha = CrearReceta(
            "Mocha", "Café, leche y chocolate con latte art final.",
            "Assets/Imagenes/Chocolate CAFY.png",
            new[] { Ingrediente.Cafe, Ingrediente.Leche, Ingrediente.Chocolate },
            new[] { "Extracción", "Vaporizado de leche", "Latte art" },
            new RuntimeAnimatorController[] { cafetera, null, null },
            new[] { 1.1f, 1.7f, 2.5f },
            new[] { 0.24f, 0.16f, 0.10f },
            new[] { Ambar, Crema, Marron });

        Receta azucarado = CrearReceta(
            "Café azucarado", "Espresso endulzado al punto justo.",
            "Assets/Imagenes/Azucar CAFY.png",
            new[] { Ingrediente.Cafe, Ingrediente.Azucar },
            new[] { "Extracción", "Endulzado" },
            new RuntimeAnimatorController[] { cafetera, null },
            new[] { 1.1f, 1.4f },
            new[] { 0.24f, 0.18f },
            new[] { Ambar, Amarillo });

        Receta caramelo = CrearReceta(
            "Café con caramelo", "Espresso con un toque de jarabe.",
            "Assets/Sprites/Jarabe CAFY.png",
            new[] { Ingrediente.Cafe, Ingrediente.Jarabe },
            new[] { "Extracción", "Toque de jarabe" },
            new RuntimeAnimatorController[] { cafetera, jarabe },
            new[] { 1.1f, 1.5f },
            new[] { 0.24f, 0.18f },
            new[] { Ambar, Caramelo });

        AssetDatabase.SaveAssets();
        return new[] { espresso, capuchino, mocha, azucarado, caramelo };
    }

    private static Receta CrearReceta(string nombre, string descripcion, string rutaIcono,
        Ingrediente[] ingredientes, string[] pasos, RuntimeAnimatorController[] animaciones,
        float[] velocidades, float[] anchos, Color[] colores)
    {
        string ruta = CarpetaRecetas + "/" + nombre + ".asset";
        Receta receta = AssetDatabase.LoadAssetAtPath<Receta>(ruta);
        if (receta == null)
        {
            receta = ScriptableObject.CreateInstance<Receta>();
            AssetDatabase.CreateAsset(receta, ruta);
        }

        receta.nombre = nombre;
        receta.descripcion = descripcion;
        receta.icono = AssetDatabase.LoadAssetAtPath<Sprite>(rutaIcono);
        receta.ingredientes = ingredientes;
        receta.nombresPaso = pasos;
        receta.animacionesExito = animaciones;
        receta.velocidades = velocidades;
        receta.anchosZona = anchos;
        receta.coloresZona = colores;
        EditorUtility.SetDirty(receta);
        return receta;
    }

    private static GameObject CrearPanel(Transform canvas)
    {
        GameObject panel = new GameObject("PanelRecetas",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        panel.transform.SetParent(canvas, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.05f, 0.04f, 0.04f, 0.92f);

        CrearTexto(panel.transform, "TituloRecetas", "Recetas CAFY",
            new Vector2(0.15f, 0.87f), new Vector2(0.85f, 0.97f), 48f, Color.white);

        GameObject contenedor = new GameObject("ContenedorRecetas",
            typeof(RectTransform), typeof(VerticalLayoutGroup));
        contenedor.transform.SetParent(panel.transform, false);
        RectTransform contenedorRect = contenedor.GetComponent<RectTransform>();
        contenedorRect.anchorMin = new Vector2(0.22f, 0.12f);
        contenedorRect.anchorMax = new Vector2(0.78f, 0.84f);
        contenedorRect.offsetMin = Vector2.zero;
        contenedorRect.offsetMax = Vector2.zero;

        VerticalLayoutGroup layout = contenedor.GetComponent<VerticalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(10, 10, 10, 10);
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;

        CrearBoton(panel.transform, "btnCerrarRecetas", "X",
            new Vector2(0.90f, 0.88f), new Vector2(0.97f, 0.97f));

        return panel;
    }

    private static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido,
        Vector2 min, Vector2 max, float tamaño, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI texto = go.GetComponent<TextMeshProUGUI>();
        texto.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
                "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        texto.text = contenido;
        texto.fontSize = tamaño;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = color;
        texto.raycastTarget = false;
        return texto;
    }

    private static Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = go.GetComponent<Image>();
        imagen.color = new Color(0.55f, 0.22f, 0.22f, 0.95f);

        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = imagen;
        CrearTexto(go.transform, "Texto", etiqueta, Vector2.zero, Vector2.one, 28f, Color.white);
        return boton;
    }

    private static Button BuscarBoton(Transform padre, string nombre)
    {
        foreach (Button boton in padre.GetComponentsInChildren<Button>(true))
        {
            if (boton.name == nombre) return boton;
        }
        return null;
    }

    private static T BuscarEnHijos<T>(Scene scene) where T : Component
    {
        foreach (GameObject raiz in scene.GetRootGameObjects())
        {
            T componente = raiz.GetComponent<T>();
            if (componente != null) return componente;
            T[] hijos = raiz.GetComponentsInChildren<T>(true);
            if (hijos.Length > 0) return hijos[0];
        }
        return null;
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
