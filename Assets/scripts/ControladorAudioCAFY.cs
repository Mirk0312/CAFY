using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Audio global de CAFY: música de fondo entre escenas, efectos por paso del
/// minijuego, aviso de pedido y sonido al pulsar cualquier botón. Persiste en
/// DontDestroyOnLoad y crea su propio botón de silenciar.
/// </summary>
public class ControladorAudioCAFY : MonoBehaviour
{
    private const string ClaveSilenciado = "CAFY_Silenciado";

    private static ControladorAudioCAFY instancia;

    private readonly Dictionary<string, AudioClip> clips = new Dictionary<string, AudioClip>();
    private readonly HashSet<Button> botonesConectados = new HashSet<Button>();

    private AudioSource fuenteMusica;
    private AudioSource fuenteEfectos;
    private Button botonSilenciar;
    private TextMeshProUGUI textoBoton;

    private bool silenciado;

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

        silenciado = PlayerPrefs.GetInt(ClaveSilenciado, 0) == 1;

        CargarClips();
        CrearFuentes();
        CrearBotonSilenciar();
        AplicarSilencio();

        StartCoroutine(ConectarBotonesNuevos());
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        if (instancia == this) instancia = null;
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void CargarClips()
    {
        string[] nombres =
        {
            "AzucarCafe", "HaciendoCafe", "Leche", "musicaFondo",
            "notiTelefono", "SonidoApartado"
        };

        foreach (string nombre in nombres)
        {
            AudioClip clip = Resources.Load<AudioClip>("sonido/" + nombre);
            if (clip != null) clips[nombre] = clip;
            else Debug.LogWarning("CAFY Audio: falta sonido/" + nombre);
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
        if (!clips.TryGetValue("musicaFondo", out AudioClip musica) || musica == null) return;
        if (fuenteMusica.isPlaying && fuenteMusica.clip == musica) return;

        fuenteMusica.clip = musica;
        fuenteMusica.Play();
    }

    /// <summary>Reproduce un efecto por su nombre lógico.</summary>
    public static void Reproducir(string nombre)
    {
        if (instancia == null) return;
        if (instancia.clips == null || !instancia.clips.TryGetValue(nombre, out AudioClip clip) || clip == null) return;
        if (instancia.fuenteEfectos == null) return;
        instancia.fuenteEfectos.PlayOneShot(clip);
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

    private void CrearBotonSilenciar()
    {
        GameObject canvasGO = new GameObject("CAFY - Silenciar UI",
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
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        botonGO.transform.SetParent(canvasGO.transform, false);

        RectTransform rect = botonGO.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(1f, 1f);
        rect.anchorMax = new Vector2(1f, 1f);
        rect.pivot = new Vector2(1f, 1f);
        rect.anchoredPosition = new Vector2(-18f, -18f);
        rect.sizeDelta = new Vector2(150f, 54f);

        Image imagen = botonGO.GetComponent<Image>();
        imagen.color = new Color(0.16f, 0.10f, 0.08f, 0.92f);

        botonSilenciar = botonGO.GetComponent<Button>();
        botonSilenciar.targetGraphic = imagen;
        botonSilenciar.onClick.AddListener(AlternarSilencio);

        GameObject textoGO = new GameObject("Texto",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(botonGO.transform, false);

        RectTransform textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;

        textoBoton = textoGO.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) textoBoton.font = TMP_Settings.defaultFontAsset;
        textoBoton.fontSize = 24f;
        textoBoton.alignment = TextAlignmentOptions.Center;
        textoBoton.color = Color.white;
        textoBoton.raycastTarget = false;
        ActualizarTextoBoton();
    }

    public void AlternarSilencio()
    {
        silenciado = !silenciado;
        PlayerPrefs.SetInt(ClaveSilenciado, silenciado ? 1 : 0);
        PlayerPrefs.Save();
        AplicarSilencio();
        ActualizarTextoBoton();
    }

    private void AplicarSilencio()
    {
        if (fuenteMusica != null) fuenteMusica.mute = silenciado;
        if (fuenteEfectos != null) fuenteEfectos.mute = silenciado;
    }

    private void ActualizarTextoBoton()
    {
        if (textoBoton != null) textoBoton.text = silenciado ? "♪ OFF" : "♪ ON";
    }
}
