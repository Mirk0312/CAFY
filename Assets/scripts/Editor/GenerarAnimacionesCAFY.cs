#if UNITY_EDITOR
using System;
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
/// Genera los clips y Animator Controllers de los sprites animados de CAFY
/// y cablea el teléfono del mostrador. Se ejecuta una sola vez al importarse;
/// también se puede relanzar desde Herramientas > CAFY.
/// </summary>
[InitializeOnLoad]
public static class GenerarAnimacionesCAFY
{
    private const string CarpetaAnimaciones = "Assets/Animaciones";
    private const string ArchivoMark = "Temp/cafy_animaciones_generadas";
    private const string ArchivoLog = "Temp/cafy_animaciones.log";

    private const string RutaMenu = "Assets/Scenes/MenuPrincipal.unity";
    private const string RutaJuego = "Assets/Scenes/JuegoPrincipal.unity";
    private const string RutaGameOver = "Assets/Scenes/GameOver.unity";

    static GenerarAnimacionesCAFY()
    {
        EditorApplication.delayCall += EjecutarUnaVez;
    }

    [MenuItem("Herramientas/CAFY/Generar animaciones y teléfono")]
    public static void GenerarTodo()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("CAFY: sal del modo Play antes de generar las animaciones.");
            return;
        }

        if (!AssetDatabase.IsValidFolder(CarpetaAnimaciones))
        {
            AssetDatabase.CreateFolder("Assets", "Animaciones");
        }

        // Hojas de sprites -> clips.
        AnimationClip motero = CrearClip("Motero", "Assets/Sprites/Motero sprite.png", 8f, true);
        AnimationClip cafe = CrearClip("CoffeeInicio", "Assets/Sprites/coffee INICIO.png", 2.5f, true);
        AnimationClip final = CrearClip("CafyFinal", "Assets/Sprites/CAFY FINAL.png", 2.5f, true);
        AnimationClip jarabe = CrearClip("Jarabe", "Assets/Sprites/Jarabe CAFY.png", 4f, true);
        // El frame _0 se dejó fuera de la animación a petición del usuario.
        AnimationClip telefonoArriba = CrearClip(
            "TelefonoSube", "Assets/Sprites/SpriteTelefonoArriba.png", 12f, false, 1);
        AnimationClip telefonoAbajo = CrearClip("TelefonoBaja", "Assets/Sprites/SpriteTelefonoAbajo.png", 13f, false);
        AnimationClip telefonoNoti = CrearClip("TelefonoNoti", "Assets/Sprites/SpriteTelefonoNoti.png", 6f, true);

        Sprite spriteBase = CargarSprite("Assets/Sprites/SpriteTelefonoBase.png", 0);
        // En reposo se ve solo la parte de arriba del teléfono: usamos el
        // último frame de la bajada, no el primero (que es el teléfono completo).
        Sprite[] framesAbajo = CargarSprites("Assets/Sprites/SpriteTelefonoAbajo.png");
        Sprite spriteAbajo = framesAbajo.Length > 0 ? framesAbajo[framesAbajo.Length - 1] : null;
        AnimationClip arribaQuieto = CrearClipUnico("TelefonoArribaQuieto", spriteBase);
        AnimationClip abajoQuieto = CrearClipUnico("TelefonoAbajoQuieto", spriteAbajo);

        RuntimeAnimatorController ctrlMotero = CrearControllerLoop("Motero", motero);
        RuntimeAnimatorController ctrlCafe = CrearControllerLoop("CoffeeInicio", cafe);
        RuntimeAnimatorController ctrlFinal = CrearControllerLoop("CafyFinal", final);
        RuntimeAnimatorController ctrlJarabe = CrearControllerLoop("Jarabe", jarabe);
        RuntimeAnimatorController ctrlNoti = CrearControllerLoop("TelefonoNoti", telefonoNoti);
        RuntimeAnimatorController ctrlTelefono = CrearControllerTelefono(
            abajoQuieto, telefonoArriba, arribaQuieto, telefonoAbajo);

        ConfigurarScene(RutaMenu, scene =>
        {
            AsignarAnimator(scene, "ArteCafeInicio", ctrlCafe);
        });

        ConfigurarScene(RutaGameOver, scene =>
        {
            AsignarAnimator(scene, "ArteCafyFinal", ctrlFinal);
        });

        ConfigurarScene(RutaJuego, scene =>
        {
            AsignarAnimator(scene, "Motero", ctrlMotero);
            // El Jarabe ya no se anima solo en el panel: solo se mueve en la vista previa al hacer hover.

            GameObject telefono = Buscar(scene, "Telefono");
            if (telefono == null)
            {
                Debug.LogWarning("CAFY: no se encontró 'Telefono' en JuegoPrincipal.");
                return;
            }

            // El sprite serializado del Image debe coincidir con el estado de
            // reposo del Animator (solo la parte de arriba del teléfono).
            UnityEngine.UI.Image imagenTelefono = telefono.GetComponent<UnityEngine.UI.Image>();
            if (imagenTelefono != null && spriteAbajo != null)
            {
                imagenTelefono.sprite = spriteAbajo;
                EditorUtility.SetDirty(imagenTelefono);
            }

            Animator animador = telefono.GetComponent<Animator>();
            if (animador == null) animador = telefono.AddComponent<Animator>();
            animador.runtimeAnimatorController = ctrlTelefono;
            animador.applyRootMotion = false;

            ControladorTelefono control = telefono.GetComponent<ControladorTelefono>();
            if (control == null) control = telefono.AddComponent<ControladorTelefono>();
            control.animador = animador;
            control.duracionSubida = telefonoArriba.length;
            control.duracionBajada = telefonoAbajo.length;

            Button botonTelefono = telefono.GetComponent<Button>();
            if (botonTelefono == null) botonTelefono = telefono.AddComponent<Button>();
            botonTelefono.targetGraphic = telefono.GetComponent<Graphic>();
            UnityEventTools.AddPersistentListener(
                botonTelefono.onClick,
                new UnityAction(control.AlternarTelefono));

            Transform panel = CrearPanelOpciones(telefono.transform, control);
            control.panelOpciones = panel.gameObject;
            control.notificacion = CrearNotificacion(telefono.transform, ctrlNoti).gameObject;

            EditorUtility.SetDirty(control);
            EditorUtility.SetDirty(botonTelefono);
            EditorUtility.SetDirty(animador);
        });

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        string mensaje = "OK - animaciones y teléfono generados";
        File.WriteAllText(ArchivoLog, mensaje);
        File.WriteAllText(ArchivoMark, DateTime.Now.ToString("s"));
        Debug.Log("CAFY: " + mensaje);
    }

    private static void EjecutarUnaVez()
    {
        if (File.Exists(ArchivoMark)) return;
        if (EditorApplication.isPlayingOrWillChangePlaymode) return; // se reintenta al salir de Play

        // Esperamos a que la compilación y la importación de los PNG terminen.
        EditorApplication.delayCall -= EjecutarUnaVez;
        GenerarTodo();
    }

    private static AnimationClip CrearClip(string nombre, string rutaTextura, float fps, bool loop, int frameInicial = 0)
    {
        Sprite[] frames = CargarSprites(rutaTextura);
        if (frames.Length == 0)
        {
            Debug.LogError("CAFY: no hay sprites en " + rutaTextura);
            return null;
        }

        string ruta = CarpetaAnimaciones + "/" + nombre + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimationClip clip = new AnimationClip
        {
            name = nombre,
            frameRate = fps,
            wrapMode = loop ? WrapMode.Loop : WrapMode.Clamp
        };

        EditorCurveBinding enlace = EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite");
        ObjectReferenceKeyframe[] claves = frames
            .Select((sprite, i) => new { sprite, i })
            .Where(x => x.i >= frameInicial)
            .Select(x => new ObjectReferenceKeyframe { time = x.i / fps, value = x.sprite })
            .ToArray();
        AnimationUtility.SetObjectReferenceCurve(clip, enlace, claves);

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        AssetDatabase.CreateAsset(clip, ruta);
        return clip;
    }

    private static AnimationClip CrearClipUnico(string nombre, Sprite sprite)
    {
        if (sprite == null)
        {
            Debug.LogError("CAFY: sprite nulo al crear " + nombre);
            return null;
        }

        string ruta = CarpetaAnimaciones + "/" + nombre + ".anim";
        if (AssetDatabase.LoadAssetAtPath<AnimationClip>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimationClip clip = new AnimationClip
        {
            name = nombre,
            frameRate = 1f,
            wrapMode = WrapMode.Loop
        };

        EditorCurveBinding enlace = EditorCurveBinding.PPtrCurve(string.Empty, typeof(Image), "m_Sprite");
        AnimationUtility.SetObjectReferenceCurve(
            clip,
            enlace,
            new[] { new ObjectReferenceKeyframe { time = 0f, value = sprite } });

        AnimationClipSettings ajustes = AnimationUtility.GetAnimationClipSettings(clip);
        ajustes.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(clip, ajustes);

        AssetDatabase.CreateAsset(clip, ruta);
        return clip;
    }

    private static RuntimeAnimatorController CrearControllerLoop(string nombre, AnimationClip clip)
    {
        if (clip == null) return null;

        string ruta = CarpetaAnimaciones + "/" + nombre + ".controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState estado = maquina.AddState("Loop", new Vector3(320f, 100f, 0f));
        estado.motion = clip;
        maquina.defaultState = estado;
        return controller;
    }

    private static RuntimeAnimatorController CrearControllerTelefono(
        AnimationClip abajo, AnimationClip subiendo, AnimationClip arriba, AnimationClip bajando)
    {
        string ruta = CarpetaAnimaciones + "/Telefono.controller";
        if (AssetDatabase.LoadAssetAtPath<AnimatorController>(ruta) != null)
        {
            AssetDatabase.DeleteAsset(ruta);
        }

        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        controller.AddParameter("Subir", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Bajar", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState eAbajo = maquina.AddState("Abajo", new Vector3(220f, 120f, 0f));
        AnimatorState eSubiendo = maquina.AddState("Subiendo", new Vector3(470f, 120f, 0f));
        AnimatorState eArriba = maquina.AddState("Arriba", new Vector3(720f, 120f, 0f));
        AnimatorState eBajando = maquina.AddState("Bajando", new Vector3(470f, -60f, 0f));

        eAbajo.motion = abajo;
        eSubiendo.motion = subiendo;
        eArriba.motion = arriba;
        eBajando.motion = bajando;
        maquina.defaultState = eAbajo;

        AnimatorStateTransition aSubir = eAbajo.AddTransition(eSubiendo);
        aSubir.hasExitTime = false;
        aSubir.duration = 0f;
        aSubir.AddCondition(AnimatorConditionMode.If, 0f, "Subir");

        AnimatorStateTransition finSubida = eSubiendo.AddTransition(eArriba);
        finSubida.hasExitTime = true;
        finSubida.exitTime = 0.99f;
        finSubida.duration = 0f;

        AnimatorStateTransition aBajar = eArriba.AddTransition(eBajando);
        aBajar.hasExitTime = false;
        aBajar.duration = 0f;
        aBajar.AddCondition(AnimatorConditionMode.If, 0f, "Bajar");

        AnimatorStateTransition finBajada = eBajando.AddTransition(eAbajo);
        finBajada.hasExitTime = true;
        finBajada.exitTime = 0.99f;
        finBajada.duration = 0f;

        return controller;
    }

    private static Transform CrearPanelOpciones(Transform telefono, ControladorTelefono control)
    {
        GameObject panel = new GameObject("PanelOpcionesTelefono", typeof(RectTransform));
        panel.transform.SetParent(telefono, false);
        RectTransform rect = panel.GetComponent<RectTransform>();
        // El ancho coincide con el cuerpo del teléfono (16 de 32 px del sprite).
        rect.anchorMin = new Vector2(0.25f, 0.12f);
        rect.anchorMax = new Vector2(0.75f, 0.62f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI texto = CrearTexto(
            panel.transform,
            "TextoOpciones",
            "Elige una opción",
            new Vector2(0.05f, 0.68f),
            new Vector2(0.95f, 0.98f),
            30f);
        control.textoOpciones = texto;

        Button pedidos = CrearBoton(
            panel.transform, "btnPedidos", "Pedidos",
            new Vector2(0.08f, 0.36f), new Vector2(0.92f, 0.62f));
        UnityEventTools.AddPersistentListener(
            pedidos.onClick, new UnityAction(control.MostrarPedidos));

        Button recetas = CrearBoton(
            panel.transform, "btnRecetas", "Recetas",
            new Vector2(0.08f, 0.06f), new Vector2(0.92f, 0.32f));
        UnityEventTools.AddPersistentListener(
            recetas.onClick, new UnityAction(control.MostrarRecetas));

        panel.SetActive(false);
        return panel.transform;
    }

    private static Transform CrearNotificacion(Transform telefono, RuntimeAnimatorController controller)
    {
        GameObject noti = new GameObject(
            "NotificacionPedido",
            typeof(RectTransform),
            typeof(CanvasRenderer),
            typeof(Image),
            typeof(Animator));
        noti.transform.SetParent(telefono, false);

        RectTransform rect = noti.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.58f, 0.78f);
        rect.anchorMax = new Vector2(0.74f, 0.96f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = noti.GetComponent<Image>();
        Sprite[] frames = CargarSprites("Assets/Sprites/SpriteTelefonoNoti.png");
        if (frames.Length > 4) imagen.sprite = frames[4];
        else if (frames.Length > 0) imagen.sprite = frames[0];
        imagen.raycastTarget = false;
        imagen.preserveAspect = true;

        // Notificación estática: usamos solo SpriteTelefonoNoti_4, sin animar.
        Animator animador = noti.GetComponent<Animator>();
        if (animador != null)
        {
            animador.enabled = false;
            animador.runtimeAnimatorController = null;
        }

        noti.SetActive(false);
        return noti.transform;
    }

    private static Button CrearBoton(Transform padre, string nombre, string etiqueta, Vector2 min, Vector2 max)
    {
        GameObject go = new GameObject(
            nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = go.GetComponent<Image>();
        imagen.color = new Color(0.12f, 0.12f, 0.16f, 0.92f);

        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = imagen;

        CrearTexto(go.transform, "Texto", etiqueta, Vector2.zero, Vector2.one, 30f);
        return boton;
    }

    private static TextMeshProUGUI CrearTexto(
        Transform padre, string nombre, string contenido, Vector2 min, Vector2 max, float tamaño)
    {
        GameObject go = new GameObject(
            nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        TextMeshProUGUI texto = go.GetComponent<TextMeshProUGUI>();
        texto.font = FuenteTMP();
        texto.text = contenido;
        texto.fontSize = tamaño;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;
        return texto;
    }

    private static TMP_FontAsset FuenteTMP()
    {
        if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }

    private static Sprite[] CargarSprites(string rutaTextura)
    {
        return AssetDatabase.LoadAllAssetsAtPath(rutaTextura)
            .OfType<Sprite>()
            .OrderBy(s => NumeroFrame(s.name))
            .ToArray();
    }

    private static int NumeroFrame(string nombre)
    {
        Match coincidencia = Regex.Match(nombre, "_(\\d+)$");
        return coincidencia.Success ? int.Parse(coincidencia.Groups[1].Value) : 0;
    }

    private static Sprite CargarSprite(string rutaTextura, int indice)
    {
        Sprite[] frames = CargarSprites(rutaTextura);
        return frames.Length > indice ? frames[indice] : null;
    }

    private static void AsignarAnimator(Scene scene, string nombre, RuntimeAnimatorController controller)
    {
        if (controller == null) return;

        GameObject objeto = Buscar(scene, nombre);
        if (objeto == null)
        {
            Debug.LogWarning("CAFY: no se encontró '" + nombre + "' en " + scene.path);
            return;
        }

        Animator animador = objeto.GetComponent<Animator>();
        if (animador == null) animador = objeto.AddComponent<Animator>();
        animador.runtimeAnimatorController = controller;
        animador.applyRootMotion = false;
        EditorUtility.SetDirty(animador);
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

    private static void ConfigurarScene(string ruta, Action<Scene> accion)
    {
        Scene scene = EditorSceneManager.GetSceneByPath(ruta);
        bool yaAbierta = scene.isLoaded;
        if (!yaAbierta)
        {
            scene = EditorSceneManager.OpenScene(ruta, OpenSceneMode.Additive);
        }

        accion(scene);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        if (!yaAbierta)
        {
            EditorSceneManager.CloseScene(scene, true);
        }
    }
}
#endif
