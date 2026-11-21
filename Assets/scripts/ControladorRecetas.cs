using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Apartado informativo de recetas. Las tarjetas NO son clicables: solo
/// muestran qué ingredientes lleva cada bebida. La selección real se hace en
/// el panel de preparación.
/// </summary>
public class ControladorRecetas : MonoBehaviour
{
    public GameObject panel;
    public RectTransform contenedor;
    public Receta[] recetas;
    public Button botonCerrar;

    private bool construido;

    private void Awake()
    {
        if (panel != null) panel.SetActive(false);
        if (botonCerrar != null) botonCerrar.onClick.AddListener(Cerrar);
    }

    public void Abrir()
    {
        if (panel == null) return;
        ConstruirTarjetas();
        panel.SetActive(true);
    }

    public void Cerrar()
    {
        if (panel != null) panel.SetActive(false);
    }

    private void ConstruirTarjetas()
    {
        if (construido || contenedor == null) return;
        construido = true;

        foreach (RectTransform hijo in contenedor.GetComponentsInChildren<RectTransform>(true))
        {
            if (hijo != contenedor && hijo.name.StartsWith("Tarjeta_"))
            {
                Destroy(hijo.gameObject);
            }
        }

        if (recetas == null) return;

        foreach (Receta receta in recetas)
        {
            if (receta == null) continue;
            CrearTarjeta(receta);
        }
    }

    private void CrearTarjeta(Receta receta)
    {
        GameObject tarjeta = new GameObject("Tarjeta_" + receta.name,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        tarjeta.transform.SetParent(contenedor, false);

        Image fondo = tarjeta.GetComponent<Image>();
        fondo.color = new Color(0.16f, 0.12f, 0.10f, 0.96f);
        fondo.raycastTarget = false;

        LayoutElement layout = tarjeta.GetComponent<LayoutElement>();
        layout.preferredHeight = 110f;
        layout.minHeight = 110f;

        if (receta.icono != null)
        {
            GameObject icono = new GameObject("Icono",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            icono.transform.SetParent(tarjeta.transform, false);
            RectTransform iconoRect = icono.GetComponent<RectTransform>();
            iconoRect.anchorMin = new Vector2(0f, 0.5f);
            iconoRect.anchorMax = new Vector2(0f, 0.5f);
            iconoRect.pivot = new Vector2(0f, 0.5f);
            iconoRect.anchoredPosition = new Vector2(14f, 0f);
            iconoRect.sizeDelta = new Vector2(72f, 72f);
            Image imagen = icono.GetComponent<Image>();
            imagen.sprite = receta.icono;
            imagen.preserveAspect = true;
            imagen.raycastTarget = false;
        }

        float margenIzquierdo = receta.icono != null ? 100f : 20f;

        GameObject texto = new GameObject("Texto",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        texto.transform.SetParent(tarjeta.transform, false);
        RectTransform textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = new Vector2(margenIzquierdo, 8f);
        textoRect.offsetMax = new Vector2(-16f, -8f);

        TextMeshProUGUI tmp = texto.GetComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        tmp.text = receta.nombre + "\n" + IngredientesComoTexto(receta);
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.MidlineLeft;
        tmp.color = new Color(1f, 0.94f, 0.86f);
        tmp.raycastTarget = false;
    }

    private static string IngredientesComoTexto(Receta receta)
    {
        if (receta.ingredientes == null || receta.ingredientes.Length == 0) return "";

        List<string> nombres = new List<string>();
        foreach (Ingrediente ingrediente in receta.ingredientes)
        {
            nombres.Add(Nombre(ingrediente));
        }
        return string.Join(" + ", nombres);
    }

    public static string Nombre(Ingrediente ingrediente)
    {
        switch (ingrediente)
        {
            case Ingrediente.Cafe: return "Café";
            case Ingrediente.Azucar: return "Azúcar";
            case Ingrediente.Leche: return "Leche";
            case Ingrediente.Chocolate: return "Chocolate";
            case Ingrediente.Jarabe: return "Jarabe";
            case Ingrediente.Taza: return "Taza";
            default: return ingrediente.ToString();
        }
    }
}
