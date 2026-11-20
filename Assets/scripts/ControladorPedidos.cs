using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Sistema completo de pedidos del mostrador: pedidos que llegan al teléfono,
/// inventario de bebidas preparadas, entrega a cliente y descarte.
/// Se crea solo en JuegoPrincipal, sin necesidad de tocar la escena a mano.
/// </summary>
public class ControladorPedidos : MonoBehaviour
{
    private enum ModoPanel { Ninguno, Pedidos, ElegirBebida, ElegirCliente, Desechar }

    private const string EscenaJuego = "JuegoPrincipal";
    private const string NombreInstancia = "CAFY - Sistema de pedidos";

    [Header("Llegada de pedidos")]
    public float primerPedidoEn = 60f;
    public float intervaloBase = 60f;
    public float intervaloMinimo = 20f;
    // Nivel 1 = 1 pedido a la vez; después se suma 1 por nivel (2, 3, 4...).
    public int maximoPedidosNivel1 = 1;
    public int maximoPedidosMaximo = 4;

    [Header("HUD")]
    public TextMeshProUGUI textoNivel;
    public TextMeshProUGUI textoPuntos;

    [Header("Bote de basura")]
    public Image boteImagen;
    public Sprite boteCerrado;
    public Sprite boteAbierto;

    [Header("Colores de los clientes")]
    public Color[] coloresCliente =
    {
        new Color(0.30f, 0.70f, 0.95f, 1f),
        new Color(0.95f, 0.55f, 0.30f, 1f),
        new Color(0.65f, 0.40f, 0.90f, 1f),
        new Color(0.35f, 0.80f, 0.55f, 1f)
    };

    public static ControladorPedidos Instancia { get; private set; }

    /// <summary>Cuantos pedidos esperan respuesta; lo usa la mini notificación del teléfono.</summary>
    public int PedidosPendientes => pedidos.Count;

    private readonly List<PedidoCliente> pedidos = new List<PedidoCliente>();
    private readonly List<BebidaPreparada> inventario = new List<BebidaPreparada>();

    private Receta[] recetas;
    private Sprite spriteNoti;
    private ControladorTelefono telefono;
    private ControladorPreparacion preparacion;
    private TextMeshProUGUI feedbackJuego;

    private GameObject panelPedidos;
    private GameObject panelAcciones;
    private TextMeshProUGUI tituloAcciones;
    private Transform contenedorAcciones;
    private Transform contenedorPedidos;

    private ModoPanel modo;
    private BebidaPreparada bebidaSeleccionada;
    private PedidoCliente pedidoSeleccionado;
    private Coroutine rutinaNoti;

    private int contadorPedidos = 1;
    private int contadorBebidas = 1;
    private int entregasCorrectas;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modoCarga)
    {
        if (escena.name != EscenaJuego) return;
        if (GameObject.Find(NombreInstancia) != null) return;

        GameObject go = new GameObject(NombreInstancia);
        go.AddComponent<ControladorPedidos>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;

        recetas = Resources.LoadAll<Receta>("Recetas");
        spriteNoti = CargarSpriteNotificacion();

        telefono = FindAnyObjectByType<ControladorTelefono>();
        preparacion = FindAnyObjectByType<ControladorPreparacion>();
        ControladorPedido pedidoVisual = FindAnyObjectByType<ControladorPedido>();
        if (pedidoVisual != null) feedbackJuego = pedidoVisual.feedback;

        OcultarNotificacionesDuplicadas();
        PrepararNotificacionEstatica();

        UILogica.PanelBase panelP = UILogica.CrearPanel("PanelPedidosCAFY", "Pedidos entrantes", CerrarPaneles);
        UILogica.PanelBase panelA = UILogica.CrearPanel("PanelAccionesCAFY", "Pedidos", CerrarPaneles);
        panelPedidos = panelP.raiz;
        contenedorPedidos = panelP.contenedor;
        panelAcciones = panelA.raiz;
        tituloAcciones = panelA.titulo;
        contenedorAcciones = panelA.contenedor;

        ConectarBoton("btnPedidos", AbrirPanelPedidos);
        ConectarBoton("btn_entregar", AbrirEntrega);
        ConectarBoton("PedidoEntregar", AbrirEntrega);
        ConectarBoton("PedidoDesechar", AbrirDesechar);

        AsegurarTextoNivel();
        AsegurarTextoPuntos();
        ConfigurarBoteBasura();
    }

    private void OnEnable()
    {
        EstadoPartida.VidasCambiadas += AplicarVidas;
        EstadoPartida.PuntosCambiados += AplicarPuntos;
        EstadoPartida.NivelCambiado += AplicarNivel;
        EstadoPartida.NivelSubido += AlSubirNivel;
        EstadoPartida.PartidaReiniciada += AlReiniciarPartida;
        EstadoPartida.PartidaPerdida += AlPerderPartida;
    }

    private void OnDisable()
    {
        EstadoPartida.VidasCambiadas -= AplicarVidas;
        EstadoPartida.PuntosCambiados -= AplicarPuntos;
        EstadoPartida.NivelCambiado -= AplicarNivel;
        EstadoPartida.NivelSubido -= AlSubirNivel;
        EstadoPartida.PartidaReiniciada -= AlReiniciarPartida;
        EstadoPartida.PartidaPerdida -= AlPerderPartida;
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        AplicarVidas(EstadoPartida.Vidas);
        AplicarPuntos(EstadoPartida.Puntos);
        AplicarNivel(EstadoPartida.Nivel);
        StartCoroutine(CicloPedidos());
    }

    private void ConectarBoton(string nombre, UnityAction accion)
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button boton in botones)
        {
            if (boton.name != nombre) continue;
            if (boton.gameObject.scene != gameObject.scene) continue;
            boton.onClick.AddListener(accion);
        }
    }

    private void AlReiniciarPartida()
    {
        pedidos.Clear();
        inventario.Clear();
        contadorPedidos = 1;
        contadorBebidas = 1;
        entregasCorrectas = 0;
        bebidaSeleccionada = null;
        pedidoSeleccionado = null;
        CerrarPaneles();
    }

    private void AlPerderPartida()
    {
        GestorEscenas gestor = FindAnyObjectByType<GestorEscenas>();
        if (gestor != null) gestor.IrAGameOver();
    }

    private void AplicarVidas(int vidas)
    {
        for (int i = 1; i <= EstadoPartida.VidasIniciales; i++)
        {
            GameObject icono = GameObject.Find("Vida" + i);
            if (icono != null) icono.SetActive(i <= vidas);
        }
    }

    private void AsegurarTextoNivel()
    {
        if (textoNivel != null) return;
        GameObject go = GameObject.Find("Nivel");
        if (go != null) textoNivel = go.GetComponent<TextMeshProUGUI>();
    }

    /// <summary>Texto de puntos justo a la derecha de las vidas (Vida1..3).</summary>
    private void AsegurarTextoPuntos()
    {
        if (textoPuntos != null) return;

        GameObject existente = GameObject.Find("TextoPuntosCAFY");
        if (existente != null)
        {
            textoPuntos = existente.GetComponent<TextMeshProUGUI>();
            if (textoPuntos != null) return;
        }

        // No cabe a la derecha de Vida3 (ya está en la esquina), así que va en
        // la misma fila que los labels "Nivel:" y "Vidas:", entre los dos.
        Transform padre = null;
        GameObject referencia = GameObject.Find("TituloVidas");
        if (referencia == null) referencia = GameObject.Find("Nivel");
        if (referencia != null) padre = referencia.transform.parent;

        GameObject go = new GameObject("TextoPuntosCAFY",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        if (padre != null) go.transform.SetParent(padre, false);

        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(455f, 371f);
        rect.sizeDelta = new Vector2(180f, 50f);

        textoPuntos = go.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) textoPuntos.font = TMP_Settings.defaultFontAsset;
        textoPuntos.fontSize = 30f;
        textoPuntos.alignment = TextAlignmentOptions.Center;
        textoPuntos.color = new Color(1f, 0.85f, 0.35f);
        textoPuntos.raycastTarget = false;
    }

    private void AplicarPuntos(int puntos)
    {
        if (textoPuntos != null) textoPuntos.text = "Puntos: " + puntos;
    }

    /// <summary>Entregas rápidas valen más; se mide desde que llegó el pedido.</summary>
    private static int PuntosPorEntrega(float segundos)
    {
        if (segundos <= 15f) return 300;
        if (segundos <= 30f) return 200;
        if (segundos <= 60f) return 100;
        return 50;
    }

    private void AplicarNivel(int nivel)
    {
        if (textoNivel == null) AsegurarTextoNivel();
        if (textoNivel != null) textoNivel.text = "Nivel: " + Mathf.Max(1, nivel);
    }

    /// <summary>El botón PedidoDesechar ya trae el sprite cerrado; buscamos el abierto en Resources.</summary>
    private void ConfigurarBoteBasura()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button boton in botones)
        {
            if (boton.name != "PedidoDesechar") continue;
            if (boton.gameObject.scene != gameObject.scene) continue;

            boteImagen = boton.GetComponent<Image>();
            if (boteImagen != null) boteCerrado = boteImagen.sprite;
            break;
        }

        boteAbierto = CargarSpriteBote("BoteBasura_5");
    }

    private static Sprite CargarSpriteBote(string nombre)
    {
        Sprite[] frames = Resources.LoadAll<Sprite>("Sprites/BoteBasura");
        if (frames == null) return null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null && frames[i].name == nombre) return frames[i];
        }
        return null;
    }

    private void SetBoteAbierto(bool abierto)
    {
        if (boteImagen == null) return;
        Sprite objetivo = abierto && boteAbierto != null ? boteAbierto : boteCerrado;
        if (objetivo != null) boteImagen.sprite = objetivo;
    }

    private float IntervaloActual()
    {
        int nivel = Mathf.Max(1, EstadoPartida.Nivel);
        // Nivel 1: 60 s. Cada nivel baja 10 s, con mínimo de 20 s.
        return Mathf.Max(intervaloMinimo, intervaloBase - 10f * (nivel - 1));
    }

    private int MaximoPedidosActual()
    {
        int nivel = Mathf.Max(1, EstadoPartida.Nivel);
        // Nivel 1: 1 pedido. Nivel 2: 2. Nivel 3: 3. Nivel 4+: 4 (tope).
        int maximo = maximoPedidosNivel1 + (nivel - 1);
        return Mathf.Clamp(maximo, 1, maximoPedidosMaximo);
    }

    private IEnumerator CicloPedidos()
    {
        // El primer pedido empieza a contarse cuando termina el 5-4-3-2-1.
        while (ControladorCuentaRegresiva.Instancia != null &&
               !ControladorCuentaRegresiva.Terminado)
        {
            yield return null;
        }
        yield return new WaitForSeconds(primerPedidoEn);

        while (true)
        {
            while (pedidos.Count >= MaximoPedidosActual()) yield return new WaitForSeconds(1f);
            CrearPedido();
            yield return new WaitForSeconds(IntervaloActual());
        }
    }

    private void CrearPedido()
    {
        if (recetas == null || recetas.Length == 0)
        {
            recetas = Resources.LoadAll<Receta>("Recetas");
        }

        Receta receta = recetas != null && recetas.Length > 0
            ? recetas[Random.Range(0, recetas.Length)]
            : null;

        List<Ingrediente> ingredientes = new List<Ingrediente>();
        if (receta != null && receta.ingredientes != null)
            ingredientes.AddRange(receta.ingredientes);
        else
            ingredientes.Add(Ingrediente.Cafe);

        // Nivel 2 en adelante: algunos pedidos piden cantidad extra de un
        // ingrediente (+2 de azúcar, +4 de chocolate...). El texto del pedido
        // solo muestra el nombre de la receta y ese aviso, nunca la receta entera.
        string extra = "";
        if (EstadoPartida.Nivel >= 2 && Random.value < 0.5f)
        {
            Ingrediente[] todos = (Ingrediente[])System.Enum.GetValues(typeof(Ingrediente));
            Ingrediente elegido = todos[Random.Range(0, todos.Length)];
            int cantidad = Random.Range(2, 5);
            for (int i = 0; i < cantidad; i++) ingredientes.Add(elegido);
            extra = "+" + cantidad + " de " + ControladorRecetas.Nombre(elegido);
        }

        PedidoCliente pedido = new PedidoCliente
        {
            id = contadorPedidos++,
            receta = receta,
            ingredientes = ingredientes.ToArray(),
            extra = extra,
            creadoEn = Time.time,
            color = coloresCliente != null && coloresCliente.Length > 0
                ? coloresCliente[(contadorPedidos - 2) % coloresCliente.Length]
                : Color.white
        };
        pedidos.Add(pedido);

        MostrarNotificacion(pedido);
        if (panelPedidos != null && panelPedidos.activeSelf) AbrirPanelPedidos();
    }

    /// <summary>Usa solo el frame 4 del sprite sliceado, no la hoja completa.</summary>
    private static Sprite CargarSpriteNotificacion()
    {
        Sprite[] frames = Resources.LoadAll<Sprite>("Sprites/SpriteTelefonoNoti");
        if (frames == null) return null;

        for (int i = 0; i < frames.Length; i++)
        {
            if (frames[i] != null && frames[i].name == "SpriteTelefonoNoti_4") return frames[i];
        }
        return frames.Length > 4 ? frames[4] : (frames.Length > 0 ? frames[0] : null);
    }

    /// <summary>La escena tiene varios NotificacionPedido; dejamos solo el del teléfono.</summary>
    private void OcultarNotificacionesDuplicadas()
    {
        if (telefono == null) return;
        GameObject oficial = telefono.notificacion;

        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "NotificacionPedido") continue;
            if (t.gameObject.scene != gameObject.scene) continue;
            if (t.gameObject == oficial) continue;
            t.gameObject.SetActive(false);
        }
    }

    /// <summary>Desactiva el Animator de la notificación para que no sobreescriba el sprite.</summary>
    private void PrepararNotificacionEstatica()
    {
        if (telefono == null || telefono.notificacion == null) return;

        GameObject noti = telefono.notificacion;
        foreach (Animator animador in noti.GetComponentsInChildren<Animator>(true))
        {
            animador.enabled = false;
            animador.runtimeAnimatorController = null;
        }

        Image imagen = noti.GetComponent<Image>();
        if (imagen == null) imagen = noti.GetComponentInChildren<Image>(true);
        if (imagen != null && spriteNoti != null)
        {
            imagen.sprite = spriteNoti;
            imagen.enabled = true;
            imagen.preserveAspect = true;
        }
    }

    private void MostrarNotificacion(PedidoCliente pedido)
    {
        if (telefono == null) telefono = FindAnyObjectByType<ControladorTelefono>();
        if (telefono == null) return;

        GameObject notificacion = telefono.notificacion;
        if (notificacion != null)
        {
            PrepararNotificacionEstatica();

            TextMeshProUGUI texto = notificacion.GetComponentInChildren<TextMeshProUGUI>(true);
            if (texto == null)
            {
                GameObject go = new GameObject("TextoPedido", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
                go.transform.SetParent(notificacion.transform, false);
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero;
                rect.anchorMax = Vector2.one;
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;
                texto = go.GetComponent<TextMeshProUGUI>();
                if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;
                texto.alignment = TextAlignmentOptions.Center;
                texto.fontSize = 20f;
                texto.color = Color.white;
                texto.raycastTarget = false;
            }
            texto.text = pedido.Titulo;
        }

        telefono.MostrarNotificacion(true);
        if (rutinaNoti != null) StopCoroutine(rutinaNoti);
        rutinaNoti = StartCoroutine(OcultarNotificacion(5f));
    }

    private IEnumerator OcultarNotificacion(float espera)
    {
        yield return new WaitForSeconds(espera);
        if (telefono != null) telefono.MostrarNotificacion(false);
        rutinaNoti = null;
    }

    /// <summary>Llamado por el minijuego al terminar un armado.</summary>
    public void GuardarBebida(Receta receta)
    {
        Ingrediente[] copia = receta != null && receta.ingredientes != null
            ? (Ingrediente[])receta.ingredientes.Clone()
            : new Ingrediente[0];

        BebidaPreparada bebida = new BebidaPreparada
        {
            id = contadorBebidas++,
            nombre = receta != null ? receta.nombre : "Bebida",
            icono = receta != null ? receta.icono : null,
            ingredientes = copia
        };
        inventario.Add(bebida);
    }

    /// <summary>Se acabó el tiempo: se pierde el armado y se limpia la fila.</summary>
    public void CancelarArmado()
    {
        LimpiarIngredientes();
    }

    public void LimpiarIngredientes()
    {
        if (preparacion == null) preparacion = FindAnyObjectByType<ControladorPreparacion>();
        if (preparacion != null) preparacion.Limpiar();
    }

    public void AbrirPanelPedidos()
    {
        if (panelPedidos == null) return;
        CerrarPaneles();
        if (telefono != null) telefono.MarcarPedidosVistos();
        panelPedidos.SetActive(true);
        UILogica.Limpiar(contenedorPedidos);

        if (pedidos.Count == 0)
        {
            UILogica.CrearTarjeta(contenedorPedidos, "Todavía no hay pedidos", null, Color.gray);
            return;
        }

        foreach (PedidoCliente pedido in pedidos)
        {
            string texto = pedido.Titulo + "\n" + pedido.Descripcion;
            UILogica.CrearTarjeta(contenedorPedidos, texto, null, pedido.color);
        }
    }

    public void AbrirEntrega()
    {
        if (panelAcciones == null) return;
        CerrarPaneles();
        if (telefono != null) telefono.MarcarPedidosVistos();
        bebidaSeleccionada = null;
        pedidoSeleccionado = null;
        modo = ModoPanel.ElegirBebida;
        panelAcciones.SetActive(true);
        RedibujarAcciones();
    }

    public void AbrirDesechar()
    {
        if (panelAcciones == null) return;
        CerrarPaneles();
        if (telefono != null) telefono.MarcarPedidosVistos();
        bebidaSeleccionada = null;
        pedidoSeleccionado = null;
        modo = ModoPanel.Desechar;
        panelAcciones.SetActive(true);
        SetBoteAbierto(true);
        RedibujarAcciones();
    }

    private void RedibujarAcciones()
    {
        UILogica.Limpiar(contenedorAcciones);

        if (modo == ModoPanel.ElegirBebida || modo == ModoPanel.Desechar)
        {
            bool entregar = modo == ModoPanel.ElegirBebida;
            tituloAcciones.text = entregar ? "Entregar: elige una bebida" : "Desechar: elige una bebida";

            if (inventario.Count == 0)
            {
                UILogica.CrearTarjeta(contenedorAcciones,
                    entregar ? "No hay bebidas preparadas" : "El inventario está vacío", null, Color.gray);
                return;
            }

            foreach (BebidaPreparada bebida in inventario)
            {
                BebidaPreparada elegida = bebida;
                string texto = bebida.Titulo + "\n" + bebida.Descripcion;
                Color color = bebidaSeleccionada == bebida
                    ? new Color(0.55f, 0.40f, 0.20f, 1f)
                    : new Color(0.36f, 0.25f, 0.18f, 1f);
                UILogica.CrearTarjeta(contenedorAcciones, texto, () =>
                {
                    bebidaSeleccionada = elegida;
                    if (entregar) modo = ModoPanel.ElegirCliente;
                    RedibujarAcciones();
                }, color);
            }

            if (!entregar && bebidaSeleccionada != null)
            {
                UILogica.CrearBotonFila(contenedorAcciones, "Descartar " + bebidaSeleccionada.Titulo,
                    ConfirmarDesechar, new Color(0.55f, 0.22f, 0.22f, 1f));
                UILogica.CrearBotonFila(contenedorAcciones, "Cancelar", AbrirDesechar,
                    new Color(0.35f, 0.35f, 0.4f, 1f));
            }
            return;
        }

        tituloAcciones.text = "Entregar: elige al cliente";
        if (pedidos.Count == 0)
        {
            UILogica.CrearTarjeta(contenedorAcciones, "No hay clientes esperando", null, Color.gray);
        }
        else
        {
            foreach (PedidoCliente pedido in pedidos)
            {
                PedidoCliente elegido = pedido;
                string texto = pedido.Titulo + "\n" + pedido.Descripcion;
                Color color = pedidoSeleccionado == pedido
                    ? new Color(0.25f, 0.65f, 0.35f, 1f)
                    : pedido.color;
                UILogica.CrearTarjeta(contenedorAcciones, texto,
                    () => { pedidoSeleccionado = elegido; RedibujarAcciones(); }, color);
            }
        }

        if (pedidoSeleccionado != null)
        {
            UILogica.CrearBotonFila(contenedorAcciones,
                "Entregar a " + pedidoSeleccionado.Titulo, ConfirmarEntrega,
                new Color(0.20f, 0.50f, 0.30f, 1f));
        }
        UILogica.CrearBotonFila(contenedorAcciones, "Volver", AbrirEntrega, new Color(0.35f, 0.35f, 0.4f, 1f));
    }

    private void ConfirmarEntrega()
    {
        if (bebidaSeleccionada == null || pedidoSeleccionado == null) return;

        bool correcto = bebidaSeleccionada.CoincideCon(pedidoSeleccionado);
        string titulo = pedidoSeleccionado.Titulo;

        if (correcto)
        {
            float segundos = Mathf.Max(0f, Time.time - pedidoSeleccionado.creadoEn);
            int puntos = PuntosPorEntrega(segundos);
            EstadoPartida.SumarPuntos(puntos);

            inventario.Remove(bebidaSeleccionada);
            pedidos.Remove(pedidoSeleccionado);
            entregasCorrectas++;

            bool subioDeNivel = false;
            if (entregasCorrectas >= 3)
            {
                entregasCorrectas = 0;
                EstadoPartida.SubirNivel();
                subioDeNivel = true;
            }

            string aviso = (subioDeNivel ? "¡Nivel " + EstadoPartida.Nivel + "! " : "")
                + "Pedido entregado a " + titulo + "  +" + puntos + " puntos";
            MostrarAviso(aviso, new Color(0.4f, 1f, 0.5f));
        }
        else
        {
            EstadoPartida.PerderVida();
            inventario.Remove(bebidaSeleccionada);
            pedidos.Remove(pedidoSeleccionado);
            MostrarAviso(titulo + " se enojó: el orden estaba mal", new Color(1f, 0.45f, 0.35f));
        }

        bebidaSeleccionada = null;
        pedidoSeleccionado = null;
        CerrarPaneles();
    }

    public void ConfirmarDesechar()
    {
        if (bebidaSeleccionada == null) return;
        inventario.Remove(bebidaSeleccionada);
        MostrarAviso("Bebida descartada", new Color(1f, 0.75f, 0.3f));
        bebidaSeleccionada = null;
        CerrarPaneles();
    }

    private void MostrarAviso(string texto, Color color)
    {
        if (feedbackJuego == null)
        {
            ControladorPedido pedidoVisual = FindAnyObjectByType<ControladorPedido>();
            if (pedidoVisual != null) feedbackJuego = pedidoVisual.feedback;
        }
        if (feedbackJuego == null) return;
        feedbackJuego.text = texto;
        feedbackJuego.color = color;
    }

    /// <summary>Banner grande al subir de nivel: nivel nuevo + vidas restauradas.</summary>
    private void AlSubirNivel(int nivel)
    {
        if (!gameObject.activeInHierarchy) return;
        StartCoroutine(MostrarBannerNivel(nivel));
    }

    private IEnumerator MostrarBannerNivel(int nivel)
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) yield break;

        GameObject banner = new GameObject("BannerNivelCAFY",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        banner.transform.SetParent(canvas.transform, false);

        RectTransform rect = banner.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.22f, 0.38f);
        rect.anchorMax = new Vector2(0.78f, 0.62f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = banner.GetComponent<Image>();
        imagen.color = new Color(0.08f, 0.06f, 0.04f, 0.92f);
        imagen.raycastTarget = false;

        TextMeshProUGUI texto = UILogica.CrearTexto(banner.transform, "Texto",
            "¡Nivel " + nivel + "!\n¡Vidas restauradas!",
            Vector2.zero, Vector2.one, 64f, new Color(1f, 0.85f, 0.3f));
        texto.alignment = TextAlignmentOptions.Center;

        banner.transform.SetAsLastSibling();
        yield return new WaitForSeconds(3f);
        if (banner != null) Destroy(banner);
    }

    private void CerrarPaneles()
    {
        modo = ModoPanel.Ninguno;
        if (panelPedidos != null) panelPedidos.SetActive(false);
        if (panelAcciones != null) panelAcciones.SetActive(false);
        SetBoteAbierto(false);
    }

    /// <summary>Utilidades de UI compartidas por los paneles de pedidos.</summary>
    private static class UILogica
    {
        internal class PanelBase
        {
            internal GameObject raiz;
            internal TextMeshProUGUI titulo;
            internal Transform contenedor;
        }

        internal static PanelBase CrearPanel(string nombre, string titulo, UnityAction cerrarAccion)
        {
            Canvas canvas = Object.FindAnyObjectByType<Canvas>();
            GameObject raiz = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            if (canvas != null) raiz.transform.SetParent(canvas.transform, false);

            RectTransform rect = raiz.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image fondo = raiz.GetComponent<Image>();
            fondo.color = new Color(0.04f, 0.03f, 0.03f, 0.95f);

            TextMeshProUGUI texto = CrearTexto(raiz.transform, "Titulo", titulo,
                new Vector2(0.1f, 0.87f), new Vector2(0.9f, 0.97f), 42f, Color.white);

            GameObject contenedor = new GameObject("Contenedor", typeof(RectTransform), typeof(VerticalLayoutGroup));
            contenedor.transform.SetParent(raiz.transform, false);
            RectTransform contRect = contenedor.GetComponent<RectTransform>();
            contRect.anchorMin = new Vector2(0.17f, 0.12f);
            contRect.anchorMax = new Vector2(0.83f, 0.84f);
            contRect.offsetMin = Vector2.zero;
            contRect.offsetMax = Vector2.zero;

            VerticalLayoutGroup layout = contenedor.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 10f;
            layout.padding = new RectOffset(8, 8, 8, 8);
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlWidth = true;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            Button cerrar = CrearBoton(raiz.transform, "btnCerrar", cerrarAccion,
                new Color(0.45f, 0.2f, 0.2f, 1f));
            RectTransform cerrarRect = cerrar.GetComponent<RectTransform>();
            cerrarRect.anchorMin = new Vector2(0.90f, 0.88f);
            cerrarRect.anchorMax = new Vector2(0.97f, 0.96f);
            cerrarRect.offsetMin = Vector2.zero;
            cerrarRect.offsetMax = Vector2.zero;

            raiz.SetActive(false);
            return new PanelBase { raiz = raiz, titulo = texto, contenedor = contenedor.transform };
        }

        internal static void Limpiar(Transform contenedor)
        {
            if (contenedor == null) return;
            List<Transform> hijos = new List<Transform>();
            foreach (Transform hijo in contenedor) hijos.Add(hijo);
            foreach (Transform hijo in hijos) Object.Destroy(hijo.gameObject);
        }

        internal static void CrearTarjeta(Transform contenedor, string texto, UnityAction accion, Color color)
        {
            GameObject tarjeta = new GameObject("Tarjeta", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            tarjeta.transform.SetParent(contenedor, false);

            Image imagen = tarjeta.GetComponent<Image>();
            imagen.color = color;

            LayoutElement layout = tarjeta.AddComponent<LayoutElement>();
            layout.preferredHeight = 92f;
            layout.minHeight = 70f;

            if (accion != null)
            {
                Button boton = tarjeta.AddComponent<Button>();
                boton.targetGraphic = imagen;
                boton.onClick.AddListener(accion);
            }

            CrearTexto(tarjeta.transform, "Texto", texto,
                Vector2.zero, Vector2.one, 24f, Color.white, new Vector2(16f, 6f), new Vector2(-16f, -6f));
        }

        internal static Button CrearBoton(Transform padre, string nombre, UnityAction accion, Color color)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
            go.transform.SetParent(padre, false);

            Image imagen = go.GetComponent<Image>();
            imagen.color = color;

            Button boton = go.GetComponent<Button>();
            boton.targetGraphic = imagen;
            if (accion != null) boton.onClick.AddListener(accion);

            CrearTexto(go.transform, "Texto", nombre == "btnCerrar" ? "X" : nombre,
                Vector2.zero, Vector2.one, 26f, Color.white);
            return boton;
        }

        internal static void CrearBotonFila(Transform contenedor, string texto, UnityAction accion, Color color)
        {
            GameObject botonGO = new GameObject("Fila_" + texto, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
            botonGO.transform.SetParent(contenedor, false);

            Image imagen = botonGO.GetComponent<Image>();
            imagen.color = color;

            Button boton = botonGO.GetComponent<Button>();
            boton.targetGraphic = imagen;
            boton.onClick.AddListener(accion);

            LayoutElement layout = botonGO.GetComponent<LayoutElement>();
            layout.preferredHeight = 60f;
            layout.minHeight = 50f;

            CrearTexto(botonGO.transform, "Texto", texto, Vector2.zero, Vector2.one, 26f, Color.white);
        }

        internal static TextMeshProUGUI CrearTexto(Transform padre, string nombre, string contenido,
            Vector2 min, Vector2 max, float tamaño, Color color,
            Vector2? offsetMin = null, Vector2? offsetMax = null)
        {
            GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(padre, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = min;
            rect.anchorMax = max;
            rect.offsetMin = offsetMin ?? Vector2.zero;
            rect.offsetMax = offsetMax ?? Vector2.zero;

            TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
            tmp.text = contenido;
            tmp.fontSize = tamaño;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color = color;
            tmp.raycastTarget = false;
            return tmp;
        }
    }
}
