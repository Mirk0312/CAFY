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
/// Configura la selección de ingredientes en PanelPreparación: convierte los
/// ingredientes en botones, crea la fila visual y cambia el botón Preparar
/// para que use los ingredientes elegidos. No mueve ni borra nada.
/// </summary>
public static class ConfigurarPreparacionCAFY
{
    private const string RutaJuego = "Assets/Scenes/JuegoPrincipal.unity";
    private const string Marcador = "Temp/cafy_preparacion_configurada";

    // Auto-configuración desactivada: sobrescribía la escena al recargar Unity.
    // [InitializeOnLoadMethod]
    // private static void ConfigurarAlRecompilar()
    // {
    //     EditorApplication.delayCall += () =>
    //     {
    //         if (!File.Exists(Marcador)) Configurar();
    //     };
    // }

    [MenuItem("Herramientas/CAFY/Configurar preparación")]
    public static void Configurar()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("CAFY: sal del modo Play antes de configurar la preparación.");
            return;
        }

        if (File.Exists(Marcador))
        {
            Debug.Log("CAFY: preparación ya configurada; no se modifica la escena.");
            return;
        }

        Scene scene = EditorSceneManager.GetSceneByPath(RutaJuego);
        bool yaAbierta = scene.isLoaded;
        if (!yaAbierta) scene = EditorSceneManager.OpenScene(RutaJuego, OpenSceneMode.Additive);

        GameObject panelPreparacion = Buscar(scene, "PanelPreparacion");
        GameObject panelMini = Buscar(scene, "PanelMiniJuego");
        if (panelPreparacion == null || panelMini == null)
        {
            Debug.LogError("CAFY: no se encontró PanelPreparacion o PanelMiniJuego.");
            return;
        }

        ControladorMinijuego mini = panelMini.GetComponent<ControladorMinijuego>();
        if (mini == null)
        {
            Debug.LogError("CAFY: PanelMiniJuego no tiene ControladorMinijuego.");
            return;
        }

        if (panelPreparacion.GetComponent<ControladorPreparacion>() != null &&
            BuscarTransform(panelPreparacion.transform, "FilaIngredientes") != null)
        {
            File.WriteAllText(Marcador, System.DateTime.Now.ToString("s"));
            Debug.Log("CAFY: fila de ingredientes ya está en la escena; no se modifica.");
            if (!yaAbierta) EditorSceneManager.CloseScene(scene, true);
            return;
        }

        mini.animacionCafetera = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animaciones/ExitoCafetera.controller");
        mini.animacionJarabe = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            "Assets/Animaciones/Jarabe.controller");
        EditorUtility.SetDirty(mini);

        ControladorPreparacion preparacion = panelPreparacion.GetComponent<ControladorPreparacion>();
        if (preparacion == null) preparacion = panelPreparacion.AddComponent<ControladorPreparacion>();

        RectTransform fila = BuscarRect(panelPreparacion.transform, "FilaIngredientes");
        if (fila == null) fila = CrearFila(panelPreparacion.transform);

        TextMeshProUGUI textoVacio = BuscarTexto(fila, "TextoFilaVacia");
        TextMeshProUGUI feedback = BuscarTexto(panelPreparacion.transform, "FeedbackPreparacion");
        if (feedback == null)
        {
            feedback = CrearTexto(panelPreparacion.transform, "FeedbackPreparacion", "",
                new Vector2(0.34f, 0.20f), new Vector2(0.66f, 0.27f), 26f, Color.white);
        }

        preparacion.contenedorFila = fila;
        preparacion.textoVacio = textoVacio;
        preparacion.feedback = feedback;
        preparacion.minijuego = mini;
        preparacion.interfaz = BuscarEnHijos<ControladorInterfazPrincipal>(scene);

        ConectarIngrediente(preparacion, panelPreparacion.transform, "Cafe", Ingrediente.Cafe);
        ConectarIngrediente(preparacion, panelPreparacion.transform, "Azucar", Ingrediente.Azucar);
        ConectarIngrediente(preparacion, panelPreparacion.transform, "Leche", Ingrediente.Leche);
        ConectarIngrediente(preparacion, panelPreparacion.transform, "Chocolate", Ingrediente.Chocolate);
        ConectarIngrediente(preparacion, panelPreparacion.transform, "Jarabe", Ingrediente.Jarabe);

        Button btnPreparar = BuscarBoton(panelPreparacion.transform, "btnPreparar");
        if (btnPreparar != null)
        {
            UnityEventBase eventos = btnPreparar.onClick;
            for (int i = eventos.GetPersistentEventCount() - 1; i >= 0; i--)
            {
                if (eventos.GetPersistentMethodName(i) == "IniciarMinijuego")
                {
                    UnityEventTools.RemovePersistentListener(btnPreparar.onClick, i);
                }
            }
            UnityEventTools.AddPersistentListener(btnPreparar.onClick, new UnityAction(preparacion.Preparar));
            EditorUtility.SetDirty(btnPreparar);
        }
        else
        {
            Debug.LogWarning("CAFY: no se encontró btnPreparar para reconectar.");
        }

        EditorUtility.SetDirty(preparacion);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        if (!yaAbierta) EditorSceneManager.CloseScene(scene, true);

        File.WriteAllText(Marcador, System.DateTime.Now.ToString("s"));
        Debug.Log("CAFY: preparación con fila de ingredientes configurada correctamente.");
    }

    private static void ConectarIngrediente(ControladorPreparacion preparacion, Transform panel,
        string nombre, Ingrediente ingrediente)
    {
        Transform objeto = BuscarTransform(panel, nombre);
        if (objeto == null)
        {
            Debug.LogWarning("CAFY: no se encontró el ingrediente " + nombre);
            return;
        }

        Button boton = objeto.GetComponent<Button>();
        if (boton == null)
        {
            boton = objeto.gameObject.AddComponent<Button>();
            Image imagen = objeto.GetComponent<Image>();
            if (imagen != null)
            {
                boton.targetGraphic = imagen;
                imagen.raycastTarget = true;
            }
        }

        if (boton.onClick.GetPersistentEventCount() == 0)
        {
            UnityEventTools.AddIntPersistentListener(
                boton.onClick,
                new UnityAction<int>(preparacion.AgregarIngrediente),
                (int)ingrediente);
        }

        EditorUtility.SetDirty(boton);
    }

    private static RectTransform CrearFila(Transform panel)
    {
        GameObject fila = new GameObject("FilaIngredientes",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(HorizontalLayoutGroup));
        fila.transform.SetParent(panel, false);

        RectTransform rect = fila.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.18f, 0.05f);
        rect.anchorMax = new Vector2(0.82f, 0.18f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image fondo = fila.GetComponent<Image>();
        fondo.color = new Color(0.06f, 0.05f, 0.04f, 0.75f);
        fondo.raycastTarget = false;

        HorizontalLayoutGroup layout = fila.GetComponent<HorizontalLayoutGroup>();
        layout.spacing = 12f;
        layout.padding = new RectOffset(14, 14, 8, 8);
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        CrearTexto(fila.transform, "TextoFilaVacia", "Ingredientes seleccionados: ninguno",
            Vector2.zero, Vector2.one, 22f, new Color(0.8f, 0.75f, 0.7f));

        return rect;
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

    private static TextMeshProUGUI BuscarTexto(Transform padre, string nombre)
    {
        foreach (TextMeshProUGUI texto in padre.GetComponentsInChildren<TextMeshProUGUI>(true))
        {
            if (texto.name == nombre) return texto;
        }
        return null;
    }

    private static Button BuscarBoton(Transform padre, string nombre)
    {
        foreach (Button boton in padre.GetComponentsInChildren<Button>(true))
        {
            if (boton.name == nombre) return boton;
        }
        return null;
    }

    private static RectTransform BuscarRect(Transform padre, string nombre)
    {
        Transform hijo = BuscarTransform(padre, nombre);
        return hijo != null ? hijo as RectTransform : null;
    }

    private static Transform BuscarTransform(Transform padre, string nombre)
    {
        foreach (Transform hijo in padre.GetComponentsInChildren<Transform>(true))
        {
            if (hijo.name == nombre) return hijo;
        }
        return null;
    }

    private static T BuscarEnHijos<T>(Scene scene) where T : Component
    {
        foreach (GameObject raiz in scene.GetRootGameObjects())
        {
            T[] componentes = raiz.GetComponentsInChildren<T>(true);
            if (componentes.Length > 0) return componentes[0];
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
