using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorEscenas : MonoBehaviour
{
    private const string EscenaMenu = "MenuPrincipal";
    private const string EscenaJuego = "JuegoPrincipal";
    private const string EscenaPausa = "Pausa";
    private const string EscenaGameOver = "GameOver";
    private const string NombreOscurecidoJuego = "OscurecidoJuego";

    public TMP_InputField inputNombreJugador;

    /// <summary>Botón INICIAR del menú principal.</summary>
    public void IrAJuego()
    {
        // Solo el menú puede guardar el nombre. IrAJuego también lo llamaba el
        // botón "volver" de la preparación y sobrescribía el nombre con "Cafetero".
        if (SceneManager.GetActiveScene().name == EscenaMenu && inputNombreJugador != null)
        {
            PerfilJugador.Nombre = inputNombreJugador.text;
        }

        IniciarPartidaNueva();
    }

    /// <summary>
    /// Botón REANUDAR de Pausa y de GameOver. Si hay una partida en curso,
    /// solo cierra el overlay de pausa; si venimos de GameOver, empezamos una nueva.
    /// </summary>
    public void ReanudarJuego()
    {
        bool juegoCargado = SceneManager.GetSceneByName(EscenaJuego).isLoaded;
        bool pausaCargada = SceneManager.GetSceneByName(EscenaPausa).isLoaded;

        if (juegoCargado && pausaCargada)
        {
            QuitarOscurecidoJuego();
            Time.timeScale = 1f;
            SceneManager.UnloadSceneAsync(EscenaPausa);
            return;
        }

        IniciarPartidaNueva();
    }

    /// <summary>
    /// Pausa la partida cargando "Pausa" de forma aditiva, sin descargar
    /// JuegoPrincipal. Así no se pierden vidas, nivel, puntos ni pedidos.
    /// </summary>
    public void IrAPausa()
    {
        if (!SceneManager.GetSceneByName(EscenaJuego).isLoaded) return;
        if (SceneManager.GetSceneByName(EscenaPausa).isLoaded) return;

        AsyncOperation carga = SceneManager.LoadSceneAsync(EscenaPausa, LoadSceneMode.Additive);
        if (carga != null)
        {
            carga.completed += _ => ConfigurarOverlayPausa();
        }
        else
        {
            ConfigurarOverlayPausa();
        }
    }

    /// <summary>ESC abre o cierra la pausa.</summary>
    public void AlternarPausa()
    {
        if (SceneManager.GetSceneByName(EscenaPausa).isLoaded) ReanudarJuego();
        else IrAPausa();
    }

    private void ConfigurarOverlayPausa()
    {
        Scene escenaPausa = SceneManager.GetSceneByName(EscenaPausa);
        if (!escenaPausa.isLoaded) return;

        foreach (GameObject raiz in escenaPausa.GetRootGameObjects())
        {
            // La escena Pausa trae su propia cámara, EventSystem y luz. Si los
            // dejamos activos se duplican el render y el input sobre JuegoPrincipal.
            bool esCamara = raiz.GetComponent<Camera>() != null;
            bool esEventSystem = raiz.GetComponent<UnityEngine.EventSystems.EventSystem>() != null;
            bool esLuz = raiz.GetComponent<Light>() != null || raiz.name.Contains("Light");

            if (esCamara || esEventSystem || esLuz)
            {
                raiz.SetActive(false);
                continue;
            }

            Canvas canvas = raiz.GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.overrideSorting = true;
                canvas.sortingOrder = 100; // por encima del HUD del juego
                AsegurarFondoOscurecido(canvas.transform);
            }
        }

        AsegurarOscurecidoJuego();
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Oscurece un poco el Canvas de JuegoPrincipal (incluido el panel de
    /// preparación) mientras la pausa está abierta. Es una capa distinta del
    /// negro del menú de Pausa, que se mantiene intacto.
    /// </summary>
    private void AsegurarOscurecidoJuego()
    {
        Scene escenaJuego = SceneManager.GetSceneByName(EscenaJuego);
        if (!escenaJuego.isLoaded) return;

        foreach (GameObject raiz in escenaJuego.GetRootGameObjects())
        {
            Canvas canvas = raiz.GetComponent<Canvas>();
            if (canvas == null) continue;

            Transform existente = canvas.transform.Find(NombreOscurecidoJuego);
            if (existente != null)
            {
                existente.gameObject.SetActive(true);
                return;
            }

            GameObject oscurecido = new GameObject(
                NombreOscurecidoJuego,
                typeof(RectTransform),
                typeof(CanvasRenderer),
                typeof(UnityEngine.UI.Image));

            oscurecido.transform.SetParent(canvas.transform, false);
            oscurecido.transform.SetAsLastSibling();

            RectTransform rect = (RectTransform)oscurecido.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            UnityEngine.UI.Image imagen = oscurecido.GetComponent<UnityEngine.UI.Image>();
            imagen.color = new Color(0f, 0f, 0f, 0.28f);
            imagen.raycastTarget = false;
            return;
        }
    }

    private void QuitarOscurecidoJuego()
    {
        Scene escenaJuego = SceneManager.GetSceneByName(EscenaJuego);
        if (!escenaJuego.isLoaded) return;

        foreach (GameObject raiz in escenaJuego.GetRootGameObjects())
        {
            Transform oscurecido = raiz.transform.Find(NombreOscurecidoJuego);
            if (oscurecido != null)
            {
                oscurecido.gameObject.SetActive(false);
                return;
            }
        }
    }

    /// <summary>
    /// Oscurece el juego detrás del menú de pausa. Si ya pusiste tu propio
    /// objeto "FondoPausa" en el Canvas, no hacemos nada y se usa el tuyo.
    /// Importante: debe ser un Image de UI, no un SpriteRenderer, porque la
    /// cámara de la escena Pausa está desactivada al cargarla de forma aditiva.
    /// </summary>
    private void AsegurarFondoOscurecido(Transform canvas)
    {
        if (canvas.Find("FondoPausa") != null) return;

        GameObject fondo = new GameObject("FondoPausa", typeof(RectTransform), typeof(CanvasRenderer), typeof(UnityEngine.UI.Image));
        fondo.transform.SetParent(canvas, false);
        fondo.transform.SetAsFirstSibling();

        RectTransform rect = (RectTransform)fondo.transform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        UnityEngine.UI.Image imagen = fondo.GetComponent<UnityEngine.UI.Image>();
        imagen.color = new Color(0f, 0f, 0f, 0.8f);
        imagen.raycastTarget = false;
    }

    public void IrAGameOver()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaGameOver);
    }

    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(EscenaMenu);
    }

    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }

    private void IniciarPartidaNueva()
    {
        Time.timeScale = 1f;
        EstadoPartida.Reiniciar(PerfilJugador.Nombre);

        if (SceneManager.GetSceneByName(EscenaPausa).isLoaded)
        {
            SceneManager.UnloadSceneAsync(EscenaPausa);
        }

        SceneManager.LoadScene(EscenaJuego);
    }
}
