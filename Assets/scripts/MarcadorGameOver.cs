using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// En GameOver muestra la puntuación obtenida y el historial de los mejores 3
/// puntajes (con nombre de jugador) guardado en PlayerPrefs.
/// Se crea en runtime para no editar la escena ajustada a mano.
/// </summary>
public class MarcadorGameOver : MonoBehaviour
{
    private const string EscenaGameOver = "GameOver";
    private const string ClaveHistorial = "CAFY_MejoresPuntajes";

    private struct Entrada
    {
        public string nombre;
        public int puntos;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Instalar()
    {
        SceneManager.sceneLoaded -= AlCargarEscena;
        SceneManager.sceneLoaded += AlCargarEscena;
        CrearSiCorresponde();
    }

    private static void AlCargarEscena(Scene escena, LoadSceneMode modoCarga)
    {
        CrearSiCorresponde();
    }

    private static void CrearSiCorresponde()
    {
        if (SceneManager.GetActiveScene().name != EscenaGameOver) return;
        if (FindAnyObjectByType<MarcadorGameOver>() != null) return;

        GameObject go = new GameObject("MarcadorGameOverCAFY");
        go.AddComponent<MarcadorGameOver>();
    }

    private void Awake()
    {
        int puntos = EstadoPartida.Puntos;
        string nombre = string.IsNullOrWhiteSpace(PerfilJugador.Nombre)
            ? "Cafetero"
            : PerfilJugador.Nombre.Trim();

        List<Entrada> mejores = CargarHistorial();
        mejores.Add(new Entrada { nombre = nombre, puntos = puntos });
        mejores.Sort((a, b) => b.puntos.CompareTo(a.puntos));
        if (mejores.Count > 3) mejores.RemoveRange(3, mejores.Count - 3);
        GuardarHistorial(mejores);

        string historial = FormatearHistorial(mejores);
        StartCoroutine(Mostrar(puntos, historial));
    }

    private static string FormatearHistorial(List<Entrada> mejores)
    {
        string resultado = "";
        for (int i = 0; i < 3; i++)
        {
            resultado += (i + 1) + ". ";
            if (i < mejores.Count)
                resultado += mejores[i].nombre + " - " + mejores[i].puntos;
            else
                resultado += "-";
            if (i < 2) resultado += "\n";
        }
        return resultado;
    }

    private IEnumerator Mostrar(int puntos, string historial)
    {
        // Un frame para que la escena termine de construir su Canvas.
        yield return null;

        // 1) Refuerzo: se escribe dentro del texto que ya trae la escena.
        TextMeshProUGUI[] textos = FindObjectsByType<TextMeshProUGUI>(FindObjectsInactive.Include);
        foreach (TextMeshProUGUI texto in textos)
        {
            if (texto == null || string.IsNullOrEmpty(texto.text)) continue;
            if (!texto.text.Contains("Te quedaste sin vidas")) continue;

            texto.text = "Te quedaste sin vidas.....\n\n"
                + "PUNTOS OBTENIDOS: " + puntos + "\n\n"
                + "MEJORES 3 PUNTAJES\n" + historial;
            texto.fontSize = 30f;
            texto.color = new Color(1f, 0.9f, 0.6f);
            break;
        }

        // 2) Panel propio, más visible.
        ConstruirPanel(puntos, historial);
        Canvas.ForceUpdateCanvases();
    }

    private static List<Entrada> CargarHistorial()
    {
        List<Entrada> lista = new List<Entrada>();
        string guardado = PlayerPrefs.GetString(ClaveHistorial, "");
        if (string.IsNullOrWhiteSpace(guardado)) return lista;

        string[] partes = guardado.Split(',');
        foreach (string parte in partes)
        {
            string[] campos = parte.Split('|');
            int valor;
            if (campos.Length >= 2 && int.TryParse(campos[1].Trim(), out valor))
            {
                lista.Add(new Entrada { nombre = campos[0].Trim(), puntos = valor });
            }
            else if (int.TryParse(campos[0].Trim(), out valor))
            {
                // Formato anterior (solo números) compatible.
                lista.Add(new Entrada { nombre = "Cafetero", puntos = valor });
            }
        }
        return lista;
    }

    private static void GuardarHistorial(List<Entrada> lista)
    {
        List<string> partes = new List<string>();
        foreach (Entrada entrada in lista)
            partes.Add(entrada.nombre + "|" + entrada.puntos);

        PlayerPrefs.SetString(ClaveHistorial, string.Join(",", partes));
        PlayerPrefs.Save();
    }

    private void ConstruirPanel(int puntos, string historial)
    {
        string contenido = "<size=32><b>PUNTOS OBTENIDOS</b></size>\n"
            + "<size=64><color=#FFD24A>" + puntos + "</color></size>\n\n"
            + "<size=28><b>MEJORES 3 PUNTAJES</b></size>\n"
            + "<size=30>" + historial + "</size>";

        GameObject panel = GameObject.Find("PanelPuntajeCAFY");
        if (panel != null)
        {
            // El objeto ya está en la escena: el usuario puede moverlo en el Hierarchy.
            TextMeshProUGUI textoExistente = panel.GetComponentInChildren<TextMeshProUGUI>(true);
            if (textoExistente != null) textoExistente.text = contenido;
            panel.transform.SetAsLastSibling();
            return;
        }

        Canvas canvas = FindAnyObjectByType<Canvas>();

        panel = new GameObject("PanelPuntajeCAFY",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        if (canvas != null) panel.transform.SetParent(canvas.transform, false);

        RectTransform rect = panel.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(250f, 150f);
        rect.sizeDelta = new Vector2(560f, 320f);

        Image fondo = panel.GetComponent<Image>();
        fondo.color = new Color(0.08f, 0.06f, 0.05f, 0.92f);
        fondo.raycastTarget = false;

        GameObject textoGO = new GameObject("TextoPuntaje",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        textoGO.transform.SetParent(panel.transform, false);

        RectTransform textoRect = textoGO.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(12f, 12f);
        textoRect.offsetMax = new Vector2(-12f, -12f);

        TextMeshProUGUI texto = textoGO.GetComponent<TextMeshProUGUI>();
        if (TMP_Settings.defaultFontAsset != null) texto.font = TMP_Settings.defaultFontAsset;
        texto.text = contenido;
        texto.fontSize = 30f;
        texto.alignment = TextAlignmentOptions.Center;
        texto.color = Color.white;
        texto.raycastTarget = false;

        panel.transform.SetAsLastSibling();
    }
}
