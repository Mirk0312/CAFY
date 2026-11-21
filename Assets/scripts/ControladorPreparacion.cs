using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
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

    [Header("Vista previa al pasar el mouse")]
    public GameObject areaPreview;
    public RuntimeAnimatorController animacionJarabe;

    // Cantidad de cada ingrediente: varios clics suman unidades (×2, ×3...).
    private readonly Dictionary<Ingrediente, int> cantidades = new Dictionary<Ingrediente, int>();

    private int TotalUnidades
    {
        get
        {
            int total = 0;
            foreach (int cantidad in cantidades.Values) total += cantidad;
            return total;
        }
    }

    private Image previewImagen;
    private Animator previewAnimator;
    private TextMeshProUGUI previewTexto;

    private void Awake()
    {
        DetenerAnimacionJarabeBoton();
    }

    private void Start()
    {
        // Sin vista previa al pasar el mouse: los ingredientes solo reaccionan al clic.
        // La animación del botón Jarabe sigue desactivada en Awake().
    }

    public void AgregarIngrediente(int valor)
    {
        Ingrediente ingrediente = (Ingrediente)valor;
        if (!cantidades.ContainsKey(ingrediente)) cantidades[ingrediente] = 0;
        cantidades[ingrediente]++;
        Mensaje("", Color.white);
        ActualizarFila();
    }

    /// <summary>La X de una ficha reduce de uno en uno; al llegar a 0 desaparece.</summary>
    public void QuitarIngrediente(int valor)
    {
        Ingrediente ingrediente = (Ingrediente)valor;
        if (!cantidades.ContainsKey(ingrediente)) return;

        cantidades[ingrediente]--;
        if (cantidades[ingrediente] <= 0) cantidades.Remove(ingrediente);

        Mensaje("", Color.white);
        ActualizarFila();
    }

    public void Preparar()
    {
        if (TotalUnidades == 0)
        {
            Mensaje("Selecciona al menos un ingrediente", new Color(1f, 0.45f, 0.35f));
            return;
        }

        if (minijuego == null) minijuego = FindAnyObjectByType<ControladorMinijuego>();
        if (interfaz == null) interfaz = FindAnyObjectByType<ControladorInterfazPrincipal>();
        if (minijuego == null || interfaz == null) return;

        List<Ingrediente> unidades = new List<Ingrediente>();
        foreach (Ingrediente ingrediente in System.Enum.GetValues(typeof(Ingrediente)))
        {
            int cantidad;
            if (!cantidades.TryGetValue(ingrediente, out cantidad)) continue;
            for (int i = 0; i < cantidad; i++) unidades.Add(ingrediente);
        }

        minijuego.EstablecerIngredientes(unidades.ToArray());
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

        if (textoVacio != null) textoVacio.gameObject.SetActive(TotalUnidades == 0);

        foreach (Ingrediente ingrediente in System.Enum.GetValues(typeof(Ingrediente)))
        {
            int cantidad;
            if (cantidades.TryGetValue(ingrediente, out cantidad) && cantidad > 0)
                CrearFicha(ingrediente, cantidad);
        }
    }

    private void CrearFicha(Ingrediente ingrediente, int cantidad)
    {
        GameObject ficha = new GameObject("Ficha_" + ingrediente,
            typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(LayoutElement));
        ficha.transform.SetParent(contenedorFila, false);

        Image fondo = ficha.GetComponent<Image>();
        fondo.color = new Color(0.22f, 0.16f, 0.12f, 0.95f);

        Button boton = ficha.GetComponent<Button>();
        boton.targetGraphic = fondo;

        LayoutElement layout = ficha.GetComponent<LayoutElement>();
        layout.preferredWidth = 220f;
        layout.minWidth = 220f;
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
        tmp.text = ControladorRecetas.Nombre(ingrediente) + "  ×" + cantidad;
        tmp.fontSize = 24f;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.color = new Color(1f, 0.92f, 0.82f);
        tmp.raycastTarget = false;
    }

    /// <summary>Limpia la fila por completo (lo usa el sistema de pedidos).</summary>
    public void Limpiar()
    {
        cantidades.Clear();
        ActualizarFila();
        Mensaje("", Color.white);
        OcultarPreview();
    }

    private void AsegurarPreview()
    {
        GameObject panel = GameObject.Find("PanelPreparacion");
        if (panel == null) return;

        Transform encontrado = panel.transform.Find("VistaPreviaIngrediente");
        if (encontrado != null)
        {
            areaPreview = encontrado.gameObject;
        }
        else
        {
            areaPreview = new GameObject("VistaPreviaIngrediente",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Animator));
            areaPreview.transform.SetParent(panel.transform, false);
            RectTransform rect = areaPreview.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.68f, 0.52f);
            rect.anchorMax = new Vector2(0.95f, 0.86f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        previewImagen = areaPreview.GetComponent<Image>();
        if (previewImagen != null)
        {
            previewImagen.color = new Color(0.18f, 0.13f, 0.10f, 0.95f);
            previewImagen.preserveAspect = true;
            previewImagen.raycastTarget = false;
        }
        previewAnimator = areaPreview.GetComponent<Animator>();

        Transform texto = areaPreview.transform.Find("Nombre");
        if (texto == null)
        {
            GameObject go = new GameObject("Nombre", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(areaPreview.transform, false);
            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(1f, 0.22f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            previewTexto = go.GetComponent<TextMeshProUGUI>();
        }
        else
        {
            previewTexto = texto.GetComponent<TextMeshProUGUI>();
        }

        if (previewTexto != null)
        {
            if (TMP_Settings.defaultFontAsset != null) previewTexto.font = TMP_Settings.defaultFontAsset;
            previewTexto.fontSize = 22f;
            previewTexto.alignment = TextAlignmentOptions.Center;
            previewTexto.color = new Color(1f, 0.92f, 0.82f);
            previewTexto.raycastTarget = false;
        }

        areaPreview.SetActive(false);
    }

    /// <summary>
    /// El botón Jarabe traía un Animator en loop asignado desde el editor.
    /// Lo apagamos: el jarabe solo debe moverse en la vista previa al hacer hover.
    /// </summary>
    private void DetenerAnimacionJarabeBoton()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button boton in botones)
        {
            if (boton.name != "Jarabe") continue;
            if (boton.gameObject.scene != gameObject.scene) continue;

            Animator animador = boton.GetComponent<Animator>();
            if (animador == null) continue;
            animador.enabled = false;
            animador.runtimeAnimatorController = null;
        }
    }

    private void CablearHover()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        for (int i = 0; i <= (int)Ingrediente.Jarabe; i++)
        {
            Ingrediente ingrediente = (Ingrediente)i;
            foreach (Button boton in botones)
            {
                if (boton.gameObject.scene != gameObject.scene) continue;
                if (boton.name != ingrediente.ToString()) continue;

                EventTrigger trigger = boton.GetComponent<EventTrigger>();
                if (trigger == null) trigger = boton.gameObject.AddComponent<EventTrigger>();

                EventTrigger.Entry entrar = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
                entrar.callback.AddListener(_ => MostrarPreview(ingrediente));
                trigger.triggers.Add(entrar);

                EventTrigger.Entry salir = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
                salir.callback.AddListener(_ => OcultarPreview());
                trigger.triggers.Add(salir);
            }
        }
    }

    private void MostrarPreview(Ingrediente ingrediente)
    {
        if (areaPreview == null) return;
        areaPreview.SetActive(true);

        bool animar = ingrediente == Ingrediente.Jarabe && animacionJarabe != null;
        if (previewAnimator != null)
        {
            previewAnimator.enabled = animar;
            previewAnimator.runtimeAnimatorController = animar ? animacionJarabe : null;
        }
        if (previewImagen != null)
        {
            previewImagen.sprite = null;
            previewImagen.color = ColorDe(ingrediente);
        }
        if (previewTexto != null) previewTexto.text = ControladorRecetas.Nombre(ingrediente);
    }

    private void OcultarPreview()
    {
        if (areaPreview == null) return;
        if (previewAnimator != null) previewAnimator.runtimeAnimatorController = null;
        areaPreview.SetActive(false);
    }

    private static Color ColorDe(Ingrediente ingrediente)
    {
        switch (ingrediente)
        {
            case Ingrediente.Cafe: return new Color(0.45f, 0.26f, 0.14f, 0.95f);
            case Ingrediente.Azucar: return new Color(0.95f, 0.82f, 0.30f, 0.95f);
            case Ingrediente.Leche: return new Color(0.95f, 0.88f, 0.72f, 0.95f);
            case Ingrediente.Chocolate: return new Color(0.55f, 0.30f, 0.16f, 0.95f);
            case Ingrediente.Jarabe: return new Color(0.78f, 0.45f, 0.18f, 0.95f);
            case Ingrediente.Taza: return new Color(0.86f, 0.86f, 0.90f, 0.95f);
            default: return new Color(0.18f, 0.13f, 0.10f, 0.95f);
        }
    }

    /// <summary>Muestra un aviso en el panel de preparación (lo usa el minijuego al agotar el tiempo).</summary>
    public void MostrarMensaje(string texto, Color color)
    {
        Mensaje(texto, color);
    }

    private void Mensaje(string texto, Color color)
    {
        if (feedback == null) return;
        feedback.text = texto;
        feedback.color = color;
    }
}
