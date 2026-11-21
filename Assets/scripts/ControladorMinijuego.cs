using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Minijuego de barra de precisión. Los tres pasos usan la misma mecánica:
/// un cursor rebota de lado a lado y hay que pulsar cuando esté en la zona
/// verde. Cada paso tiene la zona más estrecha y el cursor más rápido.
/// Al completar los tres se guarda el pedido y se vuelve al mostrador.
/// Sin vidas ni puntos: eso se conectará más adelante.
/// </summary>
public class ControladorMinijuego : MonoBehaviour
{
    [Header("UI de la barra")]
    public RectTransform barra;
    public RectTransform zonaVerde;
    public RectTransform cursor;
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI instruccion;
    public TextMeshProUGUI feedback;
    public Button btnAccionar;
    public GameObject arteExito;
    public Animator animadorExito;

    [Header("Configuración de los 3 pasos")]
    public float[] velocidades = { 0.7f, 1.0f, 1.4f };
    public float[] anchosZona = { 0.24f, 0.16f, 0.10f };
    public RuntimeAnimatorController[] animacionesExito = new RuntimeAnimatorController[3];
    public float duracionExito = 1.4f;
    public float duracionMensaje = 0.9f;

    [Header("Temporizador")]
    public TextMeshProUGUI textoTiempo;
    public Image barraTiempo;
    public float segundosPorPaso = 12f;

    [Header("Curva de dificultad niveles 1 a 5")]
    [Tooltip("Nivel 1 y 2 fáciles, 3 normal, 4 normal-difícil, 5 difícil. No depende de los ingredientes.")]
    public float[] multiplicadoresVelocidadPorNivel = { 0.8f, 0.9f, 1.05f, 1.25f, 1.5f };
    [Tooltip("Ancho de la zona verde por nivel.")]
    public float[] multiplicadoresAnchoPorNivel = { 1.2f, 1.1f, 1f, 0.85f, 0.7f };
    [Tooltip("Tiempo por paso. 1 = 12 s, nivel 5 = 80 % de ese tiempo.")]
    public float[] multiplicadoresTiempoPorNivel = { 1.2f, 1.1f, 1f, 0.9f, 0.8f };

    [Header("Dificultad al fallar")]
    [Tooltip("Cada fallo reduce la zona verde a este factor del ancho actual.")]
    public float reduccionPorFallo = 0.88f;
    [Tooltip("La zona nunca baja de este factor del ancho inicial del paso.")]
    public float reduccionMinima = 0.35f;

    [Header("Referencias")]
    public ControladorPedido pedido;
    public ControladorInterfazPrincipal interfaz;
    public Receta receta;

    [Header("Ingredientes elegidos en Preparación")]
    public Ingrediente[] ingredientes;
    public RuntimeAnimatorController animacionCafetera;
    public RuntimeAnimatorController animacionJarabe;
    public RuntimeAnimatorController animacionVaporizado;
    public RuntimeAnimatorController animacionAzucar;
    public RuntimeAnimatorController animacionLeche;
    public RuntimeAnimatorController animacionChocolate;
    public ControladorPedidos sistemaPedidos;

    private Receta recetaTemporal;
    private float tiempoRestante;
    private float zonaOffset;
    private float factorZona = 1f;
    private ControladorPreparacion preparacion;

    private int paso;
    private float posicion;
    private int direccion = 1;
    private bool activo;
    private bool ocupado;
    private Coroutine rutina;

    private void Awake()
    {
        if (pedido == null) pedido = FindAnyObjectByType<ControladorPedido>();
        if (interfaz == null) interfaz = FindAnyObjectByType<ControladorInterfazPrincipal>();
        if (sistemaPedidos == null) sistemaPedidos = FindAnyObjectByType<ControladorPedidos>();
        if (preparacion == null) preparacion = FindAnyObjectByType<ControladorPreparacion>();
        if (animacionVaporizado == null)
            animacionVaporizado = Resources.Load<RuntimeAnimatorController>("Animaciones/Vaporizado");
        if (animacionJarabe == null)
            animacionJarabe = Resources.Load<RuntimeAnimatorController>("Animaciones/PreviewJarabe");
        if (animacionAzucar == null)
            animacionAzucar = Resources.Load<RuntimeAnimatorController>("Animaciones/ExitoAzucar");
        if (animacionLeche == null)
            animacionLeche = Resources.Load<RuntimeAnimatorController>("Animaciones/ExitoLeche");
        if (animacionChocolate == null)
            animacionChocolate = Resources.Load<RuntimeAnimatorController>("Animaciones/ExitoChocolate");

        if (btnAccionar != null) btnAccionar.onClick.AddListener(Accionar);
        if (arteExito != null) arteExito.SetActive(false); arteExito.transform.localScale = Vector3.one;
        AsegurarUITiempo();
        Detener();
    }

    /// <summary>Crea (o reutiliza) el texto y la barra del temporizador.</summary>
    private void AsegurarUITiempo()
    {
        Transform panel = GetComponent<RectTransform>() != null
            ? transform
            : (GameObject.Find("PanelMiniJuego") != null ? GameObject.Find("PanelMiniJuego").transform : null);
        if (panel == null) return;

        if (textoTiempo == null)
        {
            Transform encontrado = panel.Find("TextoTiempo");
            if (encontrado != null) textoTiempo = encontrado.GetComponent<TextMeshProUGUI>();
        }
        if (textoTiempo == null)
        {
            GameObject go = new GameObject("TextoTiempo", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(panel, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.30f, 0.66f);
            rect.anchorMax = new Vector2(0.70f, 0.74f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            textoTiempo = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) textoTiempo.font = TMP_Settings.defaultFontAsset;
            textoTiempo.fontSize = 34f;
            textoTiempo.alignment = TextAlignmentOptions.Center;
            textoTiempo.color = new Color(1f, 0.85f, 0.45f);
            textoTiempo.raycastTarget = false;
        }

        if (barraTiempo == null)
        {
            Transform encontrado = panel.Find("BarraTiempo");
            if (encontrado != null) barraTiempo = encontrado.GetComponent<Image>();
        }
        if (barraTiempo == null)
        {
            GameObject go = new GameObject("BarraTiempo", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(panel, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.15f, 0.635f);
            rect.anchorMax = new Vector2(0.85f, 0.66f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            barraTiempo = go.GetComponent<Image>();
            barraTiempo.color = new Color(1f, 0.75f, 0.25f, 0.95f);
        }

        // Image.Type.Filled necesita un sprite: sin él el fillAmount no se ve.
        if (barraTiempo.sprite == null) barraTiempo.sprite = SpriteBlanco();
        barraTiempo.type = Image.Type.Filled;
        barraTiempo.fillMethod = Image.FillMethod.Horizontal;
        barraTiempo.fillOrigin = 0;
        barraTiempo.raycastTarget = false;
    }

    private static Sprite spriteBlanco;

    private static Sprite SpriteBlanco()
    {
        if (spriteBlanco != null) return spriteBlanco;

        Texture2D textura = new Texture2D(1, 1, TextureFormat.RGBA32, false);
        textura.SetPixel(0, 0, Color.white);
        textura.Apply();
        textura.hideFlags = HideFlags.DontSave;

        spriteBlanco = Sprite.Create(textura, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f));
        spriteBlanco.hideFlags = HideFlags.DontSave;
        return spriteBlanco;
    }

    /// <summary>Recibe los ingredientes marcados en el panel de preparación.</summary>
    public void EstablecerIngredientes(Ingrediente[] seleccionados)
    {
        ingredientes = seleccionados;
        if (recetaTemporal != null)
        {
            Destroy(recetaTemporal);
            recetaTemporal = null;
        }
        receta = null;
    }

    /// <summary>Llama el botón Preparar al abrir el panel del minijuego.</summary>
    public void Iniciar()
    {
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        if (sistemaPedidos == null) sistemaPedidos = FindAnyObjectByType<ControladorPedidos>();

        if (receta == null && ingredientes != null && ingredientes.Length > 0)
        {
            recetaTemporal = ConstruirRecetaDesdeIngredientes(ingredientes);
            receta = recetaTemporal;
        }

        paso = 0;
        posicion = 0f;
        direccion = 1;
        zonaOffset = 0f;
        factorZona = 1f;
        ocupado = false;
        activo = true;

        if (arteExito != null) arteExito.SetActive(false); arteExito.transform.localScale = Vector3.one;
        ConfigurarPaso();
    }

    /// <summary>Llama el botón Volver del minijuego.</summary>
    public void Detener()
    {
        activo = false;
        ocupado = false;
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        if (arteExito != null) arteExito.SetActive(false); arteExito.transform.localScale = Vector3.one;
        if (textoTiempo != null) textoTiempo.text = "";
        if (barraTiempo != null) barraTiempo.fillAmount = 1f;
    }

    private void ConfigurarPaso()
    {
        // Cada paso tiene su propio tiempo; no se arrastra el del paso anterior.
        tiempoRestante = SegundosPorPaso();
        ActualizarZona();

        if (zonaVerde != null)
        {
            Image imagenZona = zonaVerde.GetComponent<Image>();
            if (imagenZona != null)
            {
                imagenZona.color = receta != null
                    ? receta.ColorZona(paso, new Color(0.25f, 0.75f, 0.35f, 0.9f))
                    : new Color(0.25f, 0.75f, 0.35f, 0.9f);
            }
        }

        string nombrePaso = receta != null ? receta.NombrePaso(paso) : "Paso " + (paso + 1);
        if (titulo != null) titulo.text = nombrePaso + "  (" + (paso + 1) + "/" + NumeroPasos() + ")";
        if (instruccion != null)
        {
            instruccion.text = receta != null && !string.IsNullOrEmpty(receta.descripcion)
                ? receta.descripcion
                : "Pulsa cuando el cursor esté en la zona marcada";
        }
        if (feedback != null)
        {
            feedback.text = "";
            feedback.color = Color.white;
        }

        ActualizarCursor();
        ActualizarUITiempo();
    }

    private void ActualizarCursor()
    {
        if (cursor == null) return;
        cursor.anchorMin = new Vector2(posicion, 0f);
        cursor.anchorMax = new Vector2(posicion, 1f);
        cursor.offsetMin = new Vector2(-6f, 0f);
        cursor.offsetMax = new Vector2(6f, 0f);
    }

    private void Update()
    {
        if (!activo || ocupado) return;

        if (tiempoRestante > 0f)
        {
            tiempoRestante -= Time.deltaTime;
            ActualizarUITiempo();
            if (tiempoRestante <= 0f)
            {
                TiempoAgotado();
                return;
            }
        }

        posicion += Time.deltaTime * VelocidadActual() * direccion;
        if (posicion >= 1f) { posicion = 1f; direccion = -1; }
        else if (posicion <= 0f) { posicion = 0f; direccion = 1; }
        ActualizarCursor();
        ActualizarZona();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Accionar();
        }
    }

    /// <summary>Botón Accionar o barra espaciadora.</summary>
    public void Accionar()
    {
        if (!activo || ocupado) return;

        float ancho = AnchoActual();
        bool acierto = Mathf.Abs(posicion - (0.5f + zonaOffset)) <= ancho * 0.5f;

        if (acierto)
        {
            rutina = StartCoroutine(MostrarExito());
            return;
        }

        if (feedback != null)
        {
            feedback.text = "Fallaste, intenta de nuevo";
            feedback.color = new Color(1f, 0.45f, 0.35f);
        }
        // Cada fallo encoge un poco la zona verde de este paso (con suelo),
        // así no sirve pulsar siempre en el mismo sitio esperando acertar.
        factorZona = Mathf.Max(reduccionMinima, factorZona * reduccionPorFallo);
        ActualizarZona();

        posicion = 0f;
        direccion = 1;
        ActualizarCursor();
    }

    private IEnumerator MostrarExito()
    {
        ocupado = true;
        if (feedback != null)
        {
            feedback.text = "¡Perfecto!";
            feedback.color = new Color(0.4f, 1f, 0.5f);
        }

        RuntimeAnimatorController controller = receta != null
            ? receta.Animacion(paso)
            : (animacionesExito != null && paso < animacionesExito.Length ? animacionesExito[paso] : null);

        // Duración real del clip: el efecto se corta cuando termina la animación.
        float duracionAnimacion = 0f;
        AnimationClip[] clips = controller != null ? controller.animationClips : null;
        if (clips != null && clips.Length > 0) duracionAnimacion = clips[0].length;

        ReproducirSonidoDelPaso(duracionAnimacion > 0f ? duracionAnimacion : duracionMensaje);

        if (controller != null && animadorExito != null && arteExito != null)
        {
            animadorExito.runtimeAnimatorController = controller;
            animadorExito.enabled = true;
            arteExito.SetActive(true);

            // Leche, vaporizado y chocolate se muestran al 70 % para que se vean
            // mejor; el resto conserva su tamaño original.
            Ingrediente ingrediente = IngredienteDelPaso();
            float escala = (ingrediente == Ingrediente.Leche ||
                            ingrediente == Ingrediente.Vaporizado ||
                            ingrediente == Ingrediente.Chocolate) ? 0.7f : 1f;
            arteExito.transform.localScale = Vector3.one * escala;
            animadorExito.Play(0, 0, 0f);

            // Esperamos lo que dure el clip real para que la animación no se corte.
            float espera = duracionAnimacion > 0f
                ? Mathf.Max(duracionExito, duracionAnimacion + 0.15f)
                : duracionMensaje;

            yield return new WaitForSeconds(espera);
            arteExito.SetActive(false); arteExito.transform.localScale = Vector3.one;
        }
        else
        {
            // Los pasos 2 y 3 todavía no tienen sprite: mostramos el mensaje.
            yield return new WaitForSeconds(duracionMensaje);
        }

        if (paso + 1 < NumeroPasos())
        {
            paso++;
            posicion = 0f;
            direccion = 1;
            factorZona = 1f;
            ocupado = false;
            ConfigurarPaso();
            yield break;
        }

        if (sistemaPedidos == null) sistemaPedidos = FindAnyObjectByType<ControladorPedidos>();
        if (sistemaPedidos != null)
        {
            sistemaPedidos.GuardarBebida(receta);
            sistemaPedidos.LimpiarIngredientes();
        }
        else if (pedido != null)
        {
            pedido.GuardarPedido(receta);
        }
        if (feedback != null)
        {
            feedback.text = "¡Pedido listo!";
            feedback.color = new Color(0.4f, 1f, 0.5f);
        }
        yield return new WaitForSeconds(duracionMensaje);

        activo = false;
        if (interfaz != null) interfaz.RegresarAlMostrador();
    }

    /// <summary>Suena el recurso asociado al ingrediente de este paso al acertar.</summary>
    private Ingrediente IngredienteDelPaso()
    {
        if (receta != null && receta.ingredientes != null && paso < receta.ingredientes.Length)
            return receta.ingredientes[paso];
        if (ingredientes != null && paso < ingredientes.Length)
            return ingredientes[paso];
        return Ingrediente.Cafe;
    }

    private void ReproducirSonidoDelPaso(float duracionAnimacion)
    {
        Ingrediente ingrediente = IngredienteDelPaso();

        string clip = null;
        switch (ingrediente)
        {
            case Ingrediente.Cafe: clip = "HaciendoCafe"; break;
            case Ingrediente.Azucar: clip = "AzucarCafe"; break;
            case Ingrediente.Leche:
            case Ingrediente.Vaporizado: clip = "Leche"; break;
            case Ingrediente.Jarabe: clip = "JarabeChocolate"; break;
        }

        if (!string.IsNullOrEmpty(clip)) ControladorAudioCAFY.Reproducir(clip, duracionAnimacion);
    }

    private Receta ConstruirRecetaDesdeIngredientes(Ingrediente[] seleccionados)
    {
        List<string> pasos = new List<string>();
        List<RuntimeAnimatorController> animaciones = new List<RuntimeAnimatorController>();
        List<float> velocidades = new List<float>();
        List<float> anchos = new List<float>();
        List<Color> colores = new List<Color>();
        List<string> nombres = new List<string>();

        foreach (Ingrediente ingrediente in seleccionados)
        {
            nombres.Add(ControladorRecetas.Nombre(ingrediente));
            switch (ingrediente)
            {
                case Ingrediente.Cafe:
                    pasos.Add("Extracción");
                    animaciones.Add(animacionCafetera);
                    velocidades.Add(1.1f);
                    anchos.Add(0.24f);
                    colores.Add(new Color(0.95f, 0.62f, 0.22f, 0.95f));
                    break;
                case Ingrediente.Azucar:
                    pasos.Add("Endulzado");
                    animaciones.Add(animacionAzucar);
                    velocidades.Add(1.4f);
                    anchos.Add(0.18f);
                    colores.Add(new Color(0.95f, 0.82f, 0.30f, 0.95f));
                    break;
                case Ingrediente.Leche:
                    pasos.Add("Vaciado de leche");
                    animaciones.Add(animacionLeche);
                    velocidades.Add(1.7f);
                    anchos.Add(0.16f);
                    colores.Add(new Color(0.95f, 0.88f, 0.72f, 0.95f));
                    break;
                case Ingrediente.Vaporizado:
                    pasos.Add("Vaporizado de leche");
                    animaciones.Add(animacionVaporizado);
                    velocidades.Add(1.7f);
                    anchos.Add(0.16f);
                    colores.Add(new Color(0.86f, 0.86f, 0.90f, 0.95f));
                    break;
                case Ingrediente.Chocolate:
                    pasos.Add("Latte art");
                    animaciones.Add(animacionChocolate);
                    velocidades.Add(2.5f);
                    anchos.Add(0.10f);
                    colores.Add(new Color(0.55f, 0.30f, 0.16f, 0.95f));
                    break;
                case Ingrediente.Jarabe:
                    pasos.Add("Toque de jarabe");
                    animaciones.Add(animacionJarabe);
                    velocidades.Add(1.5f);
                    anchos.Add(0.18f);
                    colores.Add(new Color(0.78f, 0.45f, 0.18f, 0.95f));
                    break;
            }
        }

        Receta receta = ScriptableObject.CreateInstance<Receta>();
        receta.nombre = string.Join(" + ", nombres);
        receta.descripcion = "Prepara " + receta.nombre;
        receta.ingredientes = seleccionados;
        receta.nombresPaso = pasos.ToArray();
        receta.animacionesExito = animaciones.ToArray();
        receta.velocidades = velocidades.ToArray();
        receta.anchosZona = anchos.ToArray();
        receta.coloresZona = colores.ToArray();
        return receta;
    }

    private int NumeroPasos()
    {
        return receta != null ? receta.NumeroPasos : 3;
    }

    private int NivelActual()
    {
        return Mathf.Max(1, EstadoPartida.Nivel);
    }

    private int IndiceNivel()
    {
        // Nivel 1 -> índice 0; nivel 5 o más -> índice 4 (techo de dificultad).
        return Mathf.Clamp(NivelActual() - 1, 0, 4);
    }

    /// <summary>Curva explícita de velocidad para los niveles 1 a 5.</summary>
    private float MultiplicadorVelocidad()
    {
        return Obtener(multiplicadoresVelocidadPorNivel, IndiceNivel(), 1f);
    }

    /// <summary>Curva explícita de ancho de zona para los niveles 1 a 5.</summary>
    private float MultiplicadorAncho()
    {
        return Obtener(multiplicadoresAnchoPorNivel, IndiceNivel(), 1f);
    }

    /// <summary>Curva explícita de tiempo por paso para los niveles 1 a 5.</summary>
    private float SegundosPorPaso()
    {
        return segundosPorPaso * Obtener(multiplicadoresTiempoPorNivel, IndiceNivel(), 1f);
    }

    /// <summary>Desde el nivel 3 la zona objetivo se desplaza de lado a lado.</summary>
    private bool ZonaMovil()
    {
        return EstadoPartida.Nivel >= 3;
    }

    private void ActualizarZona()
    {
        if (zonaVerde == null) return;

        float ancho = AnchoActual();
        float rango = Mathf.Max(0f, 0.5f - ancho * 0.5f);
        zonaOffset = ZonaMovil() ? Mathf.Sin(Time.time * 1.4f) * rango * 0.75f : 0f;

        zonaVerde.anchorMin = new Vector2(0.5f + zonaOffset - ancho * 0.5f, 0f);
        zonaVerde.anchorMax = new Vector2(0.5f + zonaOffset + ancho * 0.5f, 1f);
        zonaVerde.offsetMin = Vector2.zero;
        zonaVerde.offsetMax = Vector2.zero;
    }

    private void ActualizarUITiempo()
    {
        float total = Mathf.Max(0.01f, SegundosPorPaso());
        if (textoTiempo != null)
        {
            textoTiempo.text = activo
                ? Mathf.CeilToInt(Mathf.Max(0f, tiempoRestante)).ToString()
                : "";
        }
        if (barraTiempo != null)
        {
            barraTiempo.fillAmount = activo ? Mathf.Clamp01(tiempoRestante / total) : 1f;
            barraTiempo.color = tiempoRestante < total * 0.3f
                ? new Color(0.95f, 0.30f, 0.25f, 0.95f)
                : new Color(1f, 0.75f, 0.25f, 0.95f);
        }
    }

    private void TiempoAgotado()
    {
        activo = false;
        ocupado = false;
        ActualizarUITiempo();
        if (feedback != null)
        {
            feedback.text = "¡Se acabó el tiempo!";
            feedback.color = new Color(1f, 0.45f, 0.35f);
        }
        if (sistemaPedidos == null) sistemaPedidos = FindAnyObjectByType<ControladorPedidos>();
        if (sistemaPedidos != null) sistemaPedidos.CancelarArmado();

        EstadoPartida.PerderVida();
        // Si aún quedan vidas, el jugador vuelve al panel de preparación para
        // volver a intentarlo. Con la última vida se carga GameOver automáticamente.
        if (EstadoPartida.SinVidas) return;

        if (preparacion == null) preparacion = FindAnyObjectByType<ControladorPreparacion>();
        if (preparacion != null)
            preparacion.MostrarMensaje("¡Se acabó el tiempo! Vuelve a intentarlo",
                new Color(1f, 0.45f, 0.35f));

        if (interfaz != null) interfaz.RegresarAPreparacion();
    }

    private float VelocidadActual()
    {
        // La dificultad depende solo del nivel, nunca de cuántos ingredientes
        // tenga la receta. Así el nivel 1 sigue siendo fácil aunque el pedido
        // tenga tres o cuatro pasos.
        float baseVelocidad = Obtener(velocidades, paso, 1f);
        return baseVelocidad * MultiplicadorVelocidad();
    }

    private float AnchoActual()
    {
        // Igual que la velocidad: se ignora el ancho por receta para que añadir
        // ingredientes no reduzca la zona de precisión.
        float baseAncho = Obtener(anchosZona, paso, 0.2f);
        return baseAncho * MultiplicadorAncho() * factorZona;
    }

    private static float Obtener(float[] valores, int indice, float porDefecto)
    {
        return valores != null && indice < valores.Length ? valores[indice] : porDefecto;
    }
}
