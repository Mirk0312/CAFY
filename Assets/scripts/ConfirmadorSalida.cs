using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Confirmación antes de salir del juego o volver al menú. Se crea en runtime
/// para no editar las escenas ajustadas a mano. Intercepta los btnSalir.
/// </summary>
public class ConfirmadorSalida : MonoBehaviour
{
    private const string NombreInstancia = "ConfirmadorSalidaCAFY";

    private GameObject panel;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Crear()
    {
        if (FindAnyObjectByType<ConfirmadorSalida>() != null) return;
        GameObject go = new GameObject(NombreInstancia);
        go.AddComponent<ConfirmadorSalida>();
    }

    private void Awake()
    {
        panel = ConstruirPanel();
    }

    private void Start()
    {
        Conectar();
    }

    private void Conectar()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button boton in botones)
        {
            if (boton.name != "btnSalir" || boton.gameObject.scene != gameObject.scene) continue;
            QuitarListenersOriginales(boton);
            boton.onClick.AddListener(Preguntar);
        }
    }

    /// <summary>Quita los listeners persistentes (win el Inspector) para que el
    /// botón no ejecute la salida antes de confirmar.</summary>
    private static void QuitarListenersOriginales(Button boton)
    {
        int total = boton.onClick.GetPersistentEventCount();
        for (int i = total - 1; i >= 0; i--)
            boton.onClick.SetPersistentListenerState(i, UnityEventCallState.Off);
    }

    private void OnDestroy()
    {
        if (panel != null) Destroy(panel);
    }

    public void Preguntar()
    {
        if (panel == null) panel = ConstruirPanel();
        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
    }

    private void Ocultar()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void Confirmar()
    {
        Ocultar();
        string escena = SceneManager.GetActiveScene().name;
        Time.timeScale = 1f;
        if (escena == "MenuPrincipal")
        {
            Application.Quit();
        }
        else
        {
            if (SceneManager.GetSceneByName("Pausa").isLoaded)
                SceneManager.UnloadSceneAsync("Pausa");
            SceneManager.LoadScene("MenuPrincipal");
        }
    }

    private GameObject ConstruirPanel()
    {
        Canvas canvas = FindAnyObjectByType<Canvas>();
        GameObject raiz = new GameObject("ConfirmacionSalidaCAFY",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (canvas != null) raiz.transform.SetParent(canvas.transform, false);

        RectTransform rect = raiz.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image fondo = raiz.GetComponent<Image>();
        fondo.color = new Color(0f, 0f, 0f, 0.8f);

        GameObject caja = new GameObject("Caja", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        caja.transform.SetParent(raiz.transform, false);
        RectTransform cajaRect = caja.GetComponent<RectTransform>();
        cajaRect.anchorMin = new Vector2(0.28f, 0.34f);
        cajaRect.anchorMax = new Vector2(0.72f, 0.66f);
        cajaRect.offsetMin = Vector2.zero;
        cajaRect.offsetMax = Vector2.zero;
        caja.GetComponent<Image>().color = new Color(0.12f, 0.09f, 0.08f, 1f);

        TextMeshProUGUI mensaje = CrearTexto(caja.transform, "Mensaje",
            "¿Seguro que quieres salir?", new Vector2(0.05f, 0.52f), new Vector2(0.95f, 0.92f),
            44f, Color.white);
        mensaje.alignment = TextAlignmentOptions.Center;

        CrearBoton(caja.transform, "btnSi", "Sí", Confirmar,
            new Vector2(0.12f, 0.12f), new Vector2(0.47f, 0.42f), new Color(0.7f, 0.22f, 0.2f, 1f));
        CrearBoton(caja.transform, "btnNo", "No", Ocultar,
            new Vector2(0.53f, 0.12f), new Vector2(0.88f, 0.42f), new Color(0.3f, 0.3f, 0.35f, 1f));

        raiz.SetActive(false);
        return raiz;
    }

    private static Button CrearBoton(Transform padre, string nombre, string texto, UnityAction accion,
        Vector2 min, Vector2 max, Color color)
    {
        GameObject go = new GameObject(nombre, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        go.transform.SetParent(padre, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        Image imagen = go.GetComponent<Image>();
        imagen.color = color;
        Button boton = go.GetComponent<Button>();
        boton.targetGraphic = imagen;
        boton.onClick.AddListener(accion);

        TextMeshProUGUI label = CrearTexto(go.transform, "Texto", texto,
            Vector2.zero, Vector2.one, 34f, Color.white);
        label.alignment = TextAlignmentOptions.Center;
        return boton;
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

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        tmp.text = contenido;
        tmp.fontSize = tamaño;
        tmp.color = color;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.raycastTarget = false;
        return tmp;
    }
}
