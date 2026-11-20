using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Cuenta regresiva 5-4-3-2-1 al entrar a JuegoPrincipal. Muestra los números
/// en la parte superior, bloquea toda la UI mientras corre y le avisa al
/// sistema de pedidos para que no empiece a contar hasta que termine.
/// </summary>
public class ControladorCuentaRegresiva : MonoBehaviour
{
    private const string EscenaJuego = "JuegoPrincipal";
    private const string NombreInstancia = "CAFY - Cuenta regresiva";

    public static ControladorCuentaRegresiva Instancia { get; private set; }
    public static bool Terminado { get; private set; }

    [Header("Cuenta regresiva")]
    public int desde = 5;
    public float segundosPorNumero = 1f;
    public Color colorTexto = new Color(1f, 0.88f, 0.45f, 1f);

    private TextMeshProUGUI texto;
    private GameObject bloqueo;
    private Coroutine rutina;

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
        go.AddComponent<ControladorCuentaRegresiva>();
    }

    private void Awake()
    {
        if (Instancia != null && Instancia != this)
        {
            Destroy(gameObject);
            return;
        }
        Instancia = this;
        Terminado = false;
    }

    private void OnEnable()
    {
        EstadoPartida.PartidaReiniciada += Reiniciar;
    }

    private void OnDisable()
    {
        EstadoPartida.PartidaReiniciada -= Reiniciar;
        if (Instancia == this) Instancia = null;
    }

    private void Start()
    {
        AsegurarUI();
        IniciarCuenta();
    }

    /// <summary>Crea el texto superior y un bloqueo invisible que captura clics.</summary>
    private void AsegurarUI()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        if (canvas == null) return;

        if (bloqueo == null)
        {
            bloqueo = new GameObject("BloqueoCuentaRegresiva",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            bloqueo.transform.SetParent(canvas.transform, false);

            RectTransform rect = bloqueo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image imagen = bloqueo.GetComponent<Image>();
            imagen.color = new Color(0f, 0f, 0f, 0f);
            imagen.raycastTarget = true;
            bloqueo.transform.SetAsLastSibling();
        }

        if (texto == null)
        {
            GameObject go = new GameObject("TextoCuentaRegresiva",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(canvas.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.35f, 0.86f);
            rect.anchorMax = new Vector2(0.65f, 0.98f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            texto = go.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;
            texto.fontSize = 110f;
            texto.fontStyle = FontStyles.Bold;
            texto.alignment = TextAlignmentOptions.Center;
            texto.color = colorTexto;
            texto.raycastTarget = false;
            go.transform.SetAsLastSibling();
        }
    }

    private void IniciarCuenta()
    {
        if (rutina != null) StopCoroutine(rutina);
        Terminado = false;
        if (bloqueo != null) bloqueo.SetActive(true);
        if (texto != null) texto.gameObject.SetActive(true);
        rutina = StartCoroutine(CorrerCuenta());
    }

    private IEnumerator CorrerCuenta()
    {
        for (int n = Mathf.Max(1, desde); n >= 1; n--)
        {
            if (texto != null) texto.text = n.ToString();
            yield return new WaitForSeconds(segundosPorNumero);
        }

        if (texto != null)
        {
            texto.text = "";
            texto.gameObject.SetActive(false);
        }
        if (bloqueo != null) bloqueo.SetActive(false);

        Terminado = true;
        rutina = null;
    }

    private void Reiniciar()
    {
        if (isActiveAndEnabled) IniciarCuenta();
    }
}
