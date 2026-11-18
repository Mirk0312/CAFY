using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Selección de ingredientes en el panel de preparación. Los ingredientes se
/// van apilando en una fila; si haces clic en la ficha de la fila, se quita.
/// Al pulsar Preparar se pasan los ingredientes al minijuego.
/// </summary>
public class ControladorPreparacion : MonoBehaviour
{
    public RectTransform contenedorFila;
    public TextMeshProUGUI textoVacio;
    public TextMeshProUGUI feedback;
    public ControladorMinijuego minijuego;
    public ControladorInterfazPrincipal interfaz;

    private readonly List<Ingrediente> seleccionados = new List<Ingrediente>();

    public void AgregarIngrediente(int valor)
    {
        Ingrediente ingrediente = (Ingrediente)valor;
        if (seleccionados.Contains(ingrediente))
        {
            Mensaje(ControladorRecetas.Nombre(ingrediente) + " ya está en la fila", new Color(1f, 0.75f, 0.3f));
            return;
        }

        seleccionados.Add(ingrediente);
        Mensaje("", Color.white);
        ActualizarFila();
    }

    public void QuitarIngrediente(int valor)
    {
        Ingrediente ingrediente = (Ingrediente)valor;
        if (!seleccionados.Remove(ingrediente)) return;

        Mensaje("", Color.white);
        ActualizarFila();
    }

    public void Preparar()
    {
        if (seleccionados.Count == 0)
        {
            Mensaje("Selecciona al menos un ingrediente", new Color(1f, 0.45f, 0.35f));
            return;
        }

        if (minijuego == null) minijuego = FindAnyObjectByType<ControladorMinijuego>();
        if (interfaz == null) interfaz = FindAnyObjectByType<ControladorInterfazPrincipal>();
        if (minijuego == null || interfaz == null) return;

        minijuego.EstablecerIngredientes(seleccionados.ToArray());
        interfaz.IniciarMinijuego();
    }

    private void ActualizarFila()
    {
        if (contenedorFila == null) return;

        foreach (RectTransform hijo in contenedorFila.GetComponentsInChildren<RectTransform>(true))
        {
            if (hijo != contenedorFila && hijo.name.StartsWith("Ficha_"))
            {
                Destroy(hijo.gameObject);
            }
        }

        if (textoVacio != null) textoVacio.gameObject.SetActive(seleccionados.Count == 0);

        for (int i = 0; i < seleccionados.Count; i++)
        {
            CrearFicha(seleccionados[i]);
        }
    }

    private void CrearFicha(Ingrediente ingrediente)
    {
        GameObject ficha = new GameObject("Ficha_" + ingrediente,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        ficha.transform.SetParent(contenedorFila, false);

        Image fondo = ficha.GetComponent<Image>();
        fondo.color = new Color(0.22f, 0.16f, 0.12f, 0.95f);

        Button boton = ficha.GetComponent<Button>();
        boton.targetGraphic = fondo;

        LayoutElement layout = ficha.GetComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.minWidth = 180f;
        layout.preferredHeight = 64f;
        layout.minHeight = 64f;

        int valor = (int)ingrediente;
        boton.onClick.AddListener(() => QuitarIngrediente(valor));

        GameObject texto = new GameObject("Texto",
            typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
        texto.transform.SetParent(ficha.transform, false);
        RectTransform textoRect = texto.GetComponent<RectTransform>();
        textoRect.anchorMin = Vector2.zero;
        textoRect.anchorMax = Vector2.one;
        textoRect.offsetMin = Vector2.zero;
        textoRect.offsetMax = Vector2.zero;

        TextMeshProUGUI tmp = texto.GetComponent<TextMeshProUGUI>();
        tmp.font = TMP_Settings.defaultFontAsset != null
            ? TMP_Settings.defaultFontAsset
            : Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF");
        tmp.text = ControladorRecetas.Nombre(ingrediente) + "  ×";
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.82f);
        tmp.raycastTarget = false;
    }

    private void Mensaje(string texto, Color color)
    {
        if (feedback == null) return;
        feedback.text = texto;
        feedback.color = color;
    }
}
