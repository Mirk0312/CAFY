using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Audio global de CAFY: música de fondo entre escenas, efectos por paso del
/// minijuego, aviso de pedido, daño al perder vidas, fin de partida y sonido al
/// pulsar cualquier botón. Persiste en DontDestroyOnLoad. El botón de audio abre
/// un mini menú con mute independiente para música y efectos; un doble clic
/// alterna ambos a la vez.
/// </summary>
public class ControladorAudioCAFY : MonoBehaviour
{
    // Clave antigua: se conserva para migrar partidas que ya tenían el mute global.
    private const string ClaveSilenciado = "CAFY_Silenciado";
    private const string ClaveMusica = "CAFY_SilenciarMusica";
    private const string ClaveEfectos = "CAFY_SilenciarEfectos";
    private const float SegundosCierreMenu = 7f;

    private static ControladorAudioCAFY instancia;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly HashSet<Button> botonesConectados = new HashSet<Button>();

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    // Fuentes separadas para los avisos importantes: así entrega, descarte y
    // subida de nivel pueden oírse a la vez sin cortarse entre sí.
    private AudioSource fuenteEventos;
    private AudioSource fuenteSubida;
    // El daño suena al perder una vida, en fuente propia para no cortar
    // entregas, descartes ni avisos de subida de nivel.
    private AudioSource fuenteDano;
    // Los sonidos de preparación de ingredientes van en fuente propia y más
    // altos: antes se oían demasiado bajos sobre la música de fondo.
    private AudioSource fuentePreparacion;
    private const float VolumenPreparacion = 1.45f;
    private int idPreparacion;

    private GameObject menuAudio;
    private TextMeshProUGUI textoMusica;
    private TextMeshProUGUI textoEfectos;
    private Image[] lineasBoton;
    private BotonMuteCAFY botonMute;
    private Coroutine rutinaCierreMenu;

    private bool musicaSilenciada;
    private bool efectosSilenciados;
    private int vidasAnteriores;
    private int idEfecto;
    private int idEvento;
    private int idSubida;

    private bool TodoSilenciado => musicaSilenciada && efectosSilenciados;
    private bool AlgunSilenciado => musicaSilenciada || efectosSilenciados;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void CrearAutomaticamente()
    {
        if (instancia != null) return;
        if (GameObject.Find("CAFY - Audio global") != null) return;

        GameObject go = new GameObject("CAFY - Audio global");
        go.AddComponent<ControladorAudioCAFY>();
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        instancia = this;
        DontDestroyOnLoad(gameObject);

        vidasAnteriores = EstadoPartida.Vidas;
        EstadoPartida.VidasCambiadas += AlCambiarVidas;

        bool legacy = PlayerPrefs.GetInt(ClaveSilenciado, 0) == 1;
        musicaSilenciada = PlayerPrefs.GetInt(ClaveMusica, legacy ? 1 : 0) == 1;
        efectosSilenciados = PlayerPrefs.GetInt(ClaveEfectos, legacy ? 1 : 0) == 1;
        GuardarPreferencias();

        CargarClips();
        CrearFuentes();
        CrearBotonMute();
        AplicarSilencio();

        StartCoroutine(ConectarBotonesNuevos());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        EstadoPartida.VidasCambiadas -= AlCambiarVidas;
        DetenerCierreMenu();
    }

    /// <summary>Suena daño solo cuando bajan las vidas, nunca al restaurarlas.</summary>
    private void AlCambiarVidas(int vidas)
    {
        if (vidas < vidasAnteriores) ReproducirDano();
        vidasAnteriores = vidas;
    }

    private void CargarClips()
    {
        string[] nombres =
        {
            "AzucarCafe", "HaciendoCafe", "Leche", "JarabeChocolate", "GameOver",
            "Basura", "Entrega", "SubidaNivel", "daño", "vaporizado",
            "musicaFondo", "notiTelefono", "SonidoApartado"
        };

        foreach (string nombre in nombres) ObtenerClip(nombre);
        StartCoroutine(VerificarClips(nombres));
    }

    /// <summary>
    /// Busca el clip por nombre y reintenta más adelante si Unity todavía no
    /// terminó de importarlo (evita el aviso de "falta sonido" al arrancar).
    /// </summary>
    private AudioClip ObtenerClip(string nombre)
    {
        if (clips.TryGetValue(nombre, out AudioClip clip) && clip != null) return clip;

        clip = Resources.Load<AudioClip>("sonido/" + nombre);
        if (clip == null)
        {
            AudioClip[] todos = Resources.LoadAll<AudioClip>("sonido");
            if (todos != null)
            {
                foreach (AudioClip candidato in todos)
                {
                    if (candidato != null && candidato.name == nombre)
                    {
                        clip = candidato;
                        break;
                    }
                }
            }
        }

        if (clip != null) clips[nombre] = clip;
        return clip;
    }

    private IEnumerator VerificarClips(string[] nombres)
    {
        yield return new WaitForSeconds(1.5f);
        foreach (string nombre in nombres)
        {
            if (ObtenerClip(nombre) == null)
                Debug.LogWarning("CAFY Audio: falta sonido/" + nombre);
        }
    }

    private void CrearFuentes()
    {
        fuenteMusica = gameObject.AddComponent<AudioSource>();
        fuenteMusica.playOnAwake = false;
        fuenteMusica.loop = true;
        fuenteMusica.spatialBlend = 0f;
        fuenteMusica.volume = 0.55f;

        fuenteEfectos = gameObject.AddComponent<AudioSource>();
        fuenteEfectos.playOnAwake = false;
        fuenteEfectos.loop = false;
        fuenteEfectos.spatialBlend = 0f;
        fuenteEfectos.volume = 0.9f;

        fuenteEventos = gameObject.AddComponent<AudioSource>();
        fuenteEventos.playOnAwake = false;
        fuenteEventos.loop = false;
        fuenteEventos.spatialBlend = 0f;
        fuenteEventos.volume = 0.9f;

        fuenteSubida = gameObject.AddComponent<AudioSource>();
        fuenteSubida.playOnAwake = false;
        fuenteSubida.loop = false;
        fuenteSubida.spatialBlend = 0f;
        fuenteSubida.volume = 0.9f;

        fuenteDano = gameObject.AddComponent<AudioSource>();
        fuenteDano.playOnAwake = false;
        fuenteDano.loop = false;
        fuenteDano.spatialBlend = 0f;
        fuenteDano.volume = 0.9f;

        fuentePreparacion = gameObject.AddComponent<AudioSource>();
        fuentePreparacion.playOnAwake = false;
        fuentePreparacion.loop = false;
        fuentePreparacion.spatialBlend = 0f;
        fuentePreparacion.volume = 1f;

        ReproducirMusica();
    }

    private void OnSceneLoaded(Scene escena, LoadSceneMode modo)
    {
        ReproducirMusica();
        StartCoroutine(ConectarBotonesNuevos());
    }

    private void ReproducirMusica()
    {
        if (fuenteMusica == null || clips == null) return;
        AudioClip musica = ObtenerClip("musicaFondo");
        if (musica == null) return;
        if (fuenteMusica.isPlaying && fuenteMusica.clip == musica) return;

        fuenteMusica.clip = musica;
        fuenteMusica.Play();
    }

    /// <summary>Reproduce un efecto por su nombre lógico en la fuente principal.</summary>
    public static void Reproducir(string nombre)
    {
        Reproducir(nombre, -1f);
    }

    /// <summary>
    /// Reproduce un efecto y lo corta al terminar la animación correspondiente.
    /// Con duración menor o igual a 0 se deja que el clip termine solo.
    /// </summary>
    public static void Reproducir(string nombre, float duracionMaxima)
    {
        if (instancia == null) return;
        AudioClip clip = instancia.ObtenerClip(nombre);
        if (clip == null || instancia.fuenteEfectos == null) return;
        instancia.ReproducirEn(instancia.fuenteEfectos, clip, duracionMaxima, 0);
    }

    /// <summary>
    /// Sonido de un paso del minijuego. Se corta cuando termina su animación,
    /// pero suena más alto que un efecto normal para destacar sobre la música.
    /// </summary>
    public static void ReproducirPreparacion(string nombre, float duracionMaxima)
    {
        if (instancia == null || instancia.fuentePreparacion == null) return;
        AudioClip clip = instancia.ObtenerClip(nombre);
        if (clip == null) return;

        AudioSource fuente = instancia.fuentePreparacion;
        int id = ++instancia.idPreparacion;
        fuente.Stop();
        fuente.PlayOneShot(clip, VolumenPreparacion);
        if (duracionMaxima > 0f)
            instancia.StartCoroutine(instancia.DetenerPreparacion(id, duracionMaxima));
    }

    private IEnumerator DetenerPreparacion(int id, float duracion)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duracion));
        if (id == idPreparacion && fuentePreparacion != null) fuentePreparacion.Stop();
    }

    /// <summary>Aviso importante (entrega, descarte) en su propia fuente.</summary>
    public static void ReproducirEvento(string nombre, float duracionMaxima = -1f)
    {
        if (instancia == null) return;
        AudioClip clip = instancia.ObtenerClip(nombre);
        if (clip == null || instancia.fuenteEventos == null) return;
        instancia.ReproducirEn(instancia.fuenteEventos, clip, duracionMaxima, 1);
    }

    // 0 = efectos, 1 = eventos, 2 = subida de nivel.
    private void ReproducirEn(AudioSource fuente, AudioClip clip, float duracionMaxima, int tipo)
    {
        if (fuente == null || clip == null) return;

        if (duracionMaxima > 0f)
        {
            int id = ObtenerNuevoId(tipo);
            fuente.Stop();
            fuente.clip = clip;
            fuente.time = 0f;
            fuente.Play();
            StartCoroutine(DetenerEfecto(fuente, id, tipo, duracionMaxima));
            return;
        }

        // Sin duración: PlayOneShot directo. No llamamos a Stop() aquí porque
        // podía cortar el one-shot que acabamos de iniciar en el mismo frame.
        fuente.PlayOneShot(clip);
    }

    private int ObtenerNuevoId(int tipo)
    {
        switch (tipo)
        {
            case 1: idEvento++; return idEvento;
            case 2: idSubida++; return idSubida;
            default: idEfecto++; return idEfecto;
        }
    }

    private IEnumerator DetenerEfecto(AudioSource fuente, int id, int tipo, float duracion)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, duracion));
        bool sigueVigente = tipo == 0 ? id == idEfecto
            : tipo == 1 ? id == idEvento
            : id == idSubida;
        if (sigueVigente && fuente != null) fuente.Stop();
    }

    /// <summary>
    /// Aviso de subida de nivel en su propia fuente: suena justo cuando aparece
    /// el mensaje, sin esperar a que termine el sonido de entrega.
    /// </summary>
    public static void ReproducirSubida(string nombre, float duracionMaxima = 1.3f)
    {
        if (instancia == null || instancia.fuenteSubida == null) return;
        AudioClip clip = instancia.ObtenerClip(nombre);
        if (clip == null) return;
        instancia.ReproducirEn(instancia.fuenteSubida, clip, duracionMaxima, 2);
    }

    /// <summary>Daño al perder una vida. Fuente separada para no cortar otros avisos.</summary>
    public static void ReproducirDano()
    {
        if (instancia == null || instancia.fuenteDano == null) return;
        AudioClip clip = instancia.ObtenerClip("daño");
        if (clip == null) return;
        instancia.fuenteDano.PlayOneShot(clip);
    }

    /// <summary>Efecto con retardo: se usa para el aviso del teléfono.</summary>
    public static void ReproducirDiferido(string nombre, float retardo)
    {
        if (instancia == null) return;
        instancia.StartCoroutine(instancia.ReproducirDiferidoInterno(nombre, retardo));
    }

    private IEnumerator ReproducirDiferidoInterno(string nombre, float retardo)
    {
        yield return new WaitForSeconds(Mathf.Max(0f, retardo));
        Reproducir(nombre);
    }

    private IEnumerator ConectarBotonesNuevos()
    {
        yield return null;

        while (true)
        {
            Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
            foreach (Button boton in botones)
            {
                if (boton == null || botonesConectados.Contains(boton)) continue;
                botonesConectados.Add(boton);
                boton.onClick.AddListener(SonidoBoton);
            }
            yield return new WaitForSeconds(0.5f);
        }
    }

    private void SonidoBoton()
    {
        Reproducir("SonidoApartado");
    }

    private void CrearBotonMute()
    {
        GameObject canvasGO = new GameObject("CAFY - Audio UI",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGO.transform.SetParent(transform, false);

        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;

        CanvasScaler escalador = canvasGO.GetComponent<CanvasScaler>();
        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1920f, 1080f);
        escalador.matchWidthOrHeight = 0.5f;

        GameObject botonGO = new GameObject("btnSilenciar",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image),
            typeof(Button), typeof(BotonMuteCAFY));
        botonGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = botonGO.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.zero;
        rect.pivot = Vector2.zero;
        rect.anchoredPosition = new Vector2(2f, 2f);
        rect.sizeDelta = new Vector2(52f, 44f);

        Image imagen = botonGO.GetComponent<Image>();
        imagen.color = new Color(0.16f, 0.10f, 0.08f, 0.92f);

        Button boton = botonGO.GetComponent<Button>();
        boton.targetGraphic = imagen;

        botonMute = botonGO.GetComponent<BotonMuteCAFY>();
        botonMute.controlador = this;
        botonMute.intervaloDobleClick = 0.3f;

        lineasBoton = new Image[3];
        for (int i = 0; i < lineasBoton.Length; i++)
        {
            GameObject lineaGO = new GameObject("Linea" + (i + 1),
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lineaGO.transform.SetParent(botonGO.transform, false);

            RectTransform lineaRect = lineaGO.GetComponent<RectTransform>();
            lineaRect.anchorMin = new Vector2(0.5f, 0.5f);
            lineaRect.anchorMax = new Vector2(0.5f, 0.5f);
            lineaRect.pivot = new Vector2(0.5f, 0.5f);
            lineaRect.anchoredPosition = new Vector2(0f, 9f - i * 9f);
            lineaRect.sizeDelta = new Vector2(28f, 4f);

            Image linea = lineaGO.GetComponent<Image>();
            linea.color = Color.white;
            linea.raycastTarget = false;
            lineasBoton[i] = linea;
        }

        menuAudio = new GameObject("MenuAudio",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        menuAudio.transform.SetParent(canvasGO.transform, false);

        RectTransform menuRect = menuAudio.GetComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.zero;
        menuRect.pivot = Vector2.zero;
        menuRect.anchoredPosition = new Vector2(2f, 52f);
        menuRect.sizeDelta = new Vector2(232f, 126f);

        Image fondoMenu = menuAudio.GetComponent<Image>();
        fondoMenu.color = new Color(0.16f, 0.10f, 0.08f, 0.97f);

        CrearTextoMenu("AUDIO", -8f, 18f, 0.78f);

        textoMusica = CrearOpcionMenu("OpcionMusica", -38f, AlternarMusica);
        textoEfectos = CrearOpcionMenu("OpcionEfectos", -82f, AlternarEfectos);

        menuAudio.SetActive(false);
        ActualizarIndicadores();
    }

    private TextMeshProUGUI CrearTextoMenu(string valor, float y, float alto, float tamano)
    {
        GameObject textoGO = new GameObject("Titulo",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(menuAudio.transform, false);

        RectTransform rect = textoGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(10f, y);
        rect.sizeDelta = new Vector2(212f, alto);

        TextMeshProUGUI tmp = textoGO.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.text = valor;
        tmp.fontSize = tamano;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(1f, 0.88f, 0.70f);
        tmp.raycastTarget = false;
        return tmp;
    }

    private TextMeshProUGUI CrearOpcionMenu(string nombre, float y, UnityEngine.Events.UnityAction accion)
    {
        GameObject opcionGO = new GameObject(nombre,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        opcionGO.transform.SetParent(menuAudio.transform, false);

        RectTransform rect = opcionGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = new Vector2(8f, y);
        rect.sizeDelta = new Vector2(216f, 38f);

        Image imagen = opcionGO.GetComponent<Image>();
        imagen.color = new Color(0.30f, 0.20f, 0.15f, 0.95f);

        Button boton = opcionGO.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);

        GameObject textoGO = new GameObject("Texto",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(opcionGO.transform, false);

        RectTransform textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(10f, 0f);
        textoRect.offsetMax = new Vector2(-6f, 0f);

        TextMeshProUGUI tmp = textoGO.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) tmp.font = TMP_Settings.defaultFontAsset;
        tmp.fontSize = 17f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = Color.white;
        tmp.raycastTarget = false;
        return tmp;
    }

    public void AlternarMenu()
    {
        if (menuAudio == null) return;

        bool abrir = !menuAudio.activeSelf;
        menuAudio.SetActive(abrir);
        if (abrir) ReiniciarCierreMenu();
        else DetenerCierreMenu();
    }

    /// <summary>El menú se cierra solo a los 7 s sin que lo toquen.</summary>
    private void ReiniciarCierreMenu()
    {
        if (menuAudio == null || !menuAudio.activeSelf) return;
        DetenerCierreMenu();
        // Tiempo real: también debe cerrarse si el juego está en pausa.
        rutinaCierreMenu = StartCoroutine(CerrarMenuTrasInactividad());
    }

    private void DetenerCierreMenu()
    {
        if (rutinaCierreMenu == null) return;
        StopCoroutine(rutinaCierreMenu);
        rutinaCierreMenu = null;
    }

    private IEnumerator CerrarMenuTrasInactividad()
    {
        yield return new WaitForSecondsRealtime(SegundosCierreMenu);
        if (menuAudio != null) menuAudio.SetActive(false);
        rutinaCierreMenu = null;
    }

    public void AlternarMusica()
    {
        musicaSilenciada = !musicaSilenciada;
        GuardarPreferencias();
        AplicarSilencio();
        ReiniciarCierreMenu();
    }

    public void AlternarEfectos()
    {
        efectosSilenciados = !efectosSilenciados;
        GuardarPreferencias();
        AplicarSilencio();
        ReiniciarCierreMenu();
    }

    /// <summary>
    /// Doble clic en el botón de audio: alterna música y efectos juntos.
    /// Si ambos están muteados, los reactiva; en cualquier otro caso los mutea.
    /// </summary>
    public void AlternarTodo()
    {
        bool silenciar = !TodoSilenciado;
        musicaSilenciada = silenciar;
        efectosSilenciados = silenciar;
        GuardarPreferencias();
        AplicarSilencio();
        DetenerCierreMenu();
        if (menuAudio != null) menuAudio.SetActive(false);
    }

    private void GuardarPreferencias()
    {
        PlayerPrefs.SetInt(ClaveMusica, musicaSilenciada ? 1 : 0);
        PlayerPrefs.SetInt(ClaveEfectos, efectosSilenciados ? 1 : 0);
        PlayerPrefs.SetInt(ClaveSilenciado, TodoSilenciado ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void AplicarSilencio()
    {
        if (fuenteMusica != null) fuenteMusica.mute = musicaSilenciada;
        if (fuenteEfectos != null) fuenteEfectos.mute = efectosSilenciados;
        if (fuenteEventos != null) fuenteEventos.mute = efectosSilenciados;
        if (fuenteSubida != null) fuenteSubida.mute = efectosSilenciados;
        if (fuenteDano != null) fuenteDano.mute = efectosSilenciados;
        if (fuentePreparacion != null) fuentePreparacion.mute = efectosSilenciados;
        ActualizarIndicadores();
    }

    private void ActualizarIndicadores()
    {
        if (textoMusica != null)
            textoMusica.text = "Música: " + (musicaSilenciada ? "OFF" : "ON");
        if (textoEfectos != null)
            textoEfectos.text = "Sonidos: " + (efectosSilenciados ? "OFF" : "ON");

        if (lineasBoton == null) return;
        Color color = TodoSilenciado
            ? new Color(0.95f, 0.30f, 0.25f)
            : AlgunSilenciado
                ? new Color(1f, 0.78f, 0.25f)
                : Color.white;
        foreach (Image linea in lineasBoton)
        {
            if (linea != null) linea.color = color;
        }
    }
}

/// <summary>
/// Diferencia un clic (abre el menú de audio) de un doble clic (alterna mute total).
/// Se usa un retardo en tiempo real para que el primer clic no abra el menú
/// antes de saber si viene un segundo clic.
/// </summary>
public class BotonMuteCAFY : MonoBehaviour, IPointerClickHandler
{
    public ControladorAudioCAFY controlador;
    public float intervaloDobleClick = 0.3f;

    private Coroutine rutinaClic;
    private float ultimoClic = -999f;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controlador == null) return;

        float ahora = Time.unscaledTime;
        if (ahora - ultimoClic <= intervaloDobleClick)
        {
            if (rutinaClic != null)
            {
                StopCoroutine(rutinaClic);
                rutinaClic = null;
            }
            ultimoClic = -999f;
            controlador.AlternarTodo();
            return;
        }

        ultimoClic = ahora;
        rutinaClic = StartCoroutine(ClicSimpleDiferido());
    }

    private IEnumerator ClicSimpleDiferido()
    {
        yield return new WaitForSecondsRealtime(intervaloDobleClick);
        if (controlador != null) controlador.AlternarMenu();
        rutinaClic = null;
    }
}
