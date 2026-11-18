using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
/// Minijuego de barra de precisión. Los tres pasos usan la misma mecánica:
/// un cursor rebota de lado a lado y hay que pulsar cuando esté en la zona
/// verde. Cada paso tiene la zona más estrecha y el cursor más rápido.
/// Al completar los tres se guarda el pedido y se vuelve al mostrador.
/// Sin vidas ni puntos: eso se conectará más adelante.
/// </summary>
public class ControladorMinijuego : MonoBehaviour
{
    [Header("UI de la barra")]
    public RectTransform barra;
    public RectTransform zonaVerde;
    public RectTransform cursor;
    public TextMeshProUGUI titulo;
    public TextMeshProUGUI instruccion;
    public TextMeshProUGUI feedback;
    public Button btnAccionar;
    public GameObject arteExito;
    public Animator animadorExito;

    [Header("Configuración de los 3 pasos")]
    public float[] velocidades = { 1.1f, 1.7f, 2.5f };
    public float[] anchosZona = { 0.24f, 0.16f, 0.10f };
    public RuntimeAnimatorController[] animacionesExito = new RuntimeAnimatorController[3];
    public float duracionExito = 1.4f;
    public float duracionMensaje = 0.9f;

    [Header("Referencias")]
    public ControladorPedido pedido;
    public ControladorInterfazPrincipal interfaz;

    private int paso;
    private float posicion;
    private int direccion = 1;
    private bool activo;
    private bool ocupado;
    private Coroutine rutina;

    private void Awake()
    {
        if (pedido == null) pedido = FindAnyObjectByType<ControladorPedido>();
        if (interfaz == null) interfaz = FindAnyObjectByType<ControladorInterfazPrincipal>();
        if (btnAccionar != null) btnAccionar.onClick.AddListener(Accionar);
        if (arteExito != null) arteExito.SetActive(false);
        Detener();
    }

    /// <summary>Llama el botón Preparar al abrir el panel del minijuego.</summary>
    public void Iniciar()
    {
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }

        paso = 0;
        posicion = 0f;
        direccion = 1;
        ocupado = false;
        activo = true;

        if (arteExito != null) arteExito.SetActive(false);
        ConfigurarPaso();
    }

    /// <summary>Llama el botón Volver del minijuego.</summary>
    public void Detener()
    {
        activo = false;
        ocupado = false;
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        if (arteExito != null) arteExito.SetActive(false);
    }

    private void ConfigurarPaso()
    {
        float ancho = Obtener(anchosZona, paso, 0.2f);

        if (zonaVerde != null)
        {
            zonaVerde.anchorMin = new Vector2(0.5f - ancho * 0.5f, 0f);
            zonaVerde.anchorMax = new Vector2(0.5f + ancho * 0.5f, 1f);
            zonaVerde.offsetMin = Vector2.zero;
            zonaVerde.offsetMax = Vector2.zero;
        }

        if (titulo != null) titulo.text = "Paso " + (paso + 1) + " de 3";
        if (instruccion != null) instruccion.text = "Pulsa cuando el cursor esté en la zona verde";
        if (feedback != null)
        {
            feedback.text = "";
            feedback.color = Color.white;
        }

        ActualizarCursor();
    }

    private void ActualizarCursor()
    {
        if (cursor == null) return;
        cursor.anchorMin = new Vector2(posicion, 0f);
        cursor.anchorMax = new Vector2(posicion, 1f);
        cursor.offsetMin = new Vector2(-6f, 0f);
        cursor.offsetMax = new Vector2(6f, 0f);
    }

    private void Update()
    {
        if (!activo || ocupado) return;

        posicion += Time.deltaTime * Obtener(velocidades, paso, 1f) * direccion;
        if (posicion >= 1f) { posicion = 1f; direccion = -1; }
        else if (posicion <= 0f) { posicion = 0f; direccion = 1; }
        ActualizarCursor();

        if (Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            Accionar();
        }
    }

    /// <summary>Botón Accionar o barra espaciadora.</summary>
    public void Accionar()
    {
        if (!activo || ocupado) return;

        float ancho = Obtener(anchosZona, paso, 0.2f);
        bool acierto = Mathf.Abs(posicion - 0.5f) <= ancho * 0.5f;

        if (acierto)
        {
            rutina = StartCoroutine(MostrarExito());
            return;
        }

        if (feedback != null)
        {
            feedback.text = "Fallaste, intenta de nuevo";
            feedback.color = new Color(1f, 0.45f, 0.35f);
        }
        posicion = 0f;
        direccion = 1;
        ActualizarCursor();
    }

    private IEnumerator MostrarExito()
    {
        ocupado = true;
        if (feedback != null)
        {
            feedback.text = "¡Perfecto!";
            feedback.color = new Color(0.4f, 1f, 0.5f);
        }

        RuntimeAnimatorController controller = animacionesExito != null && paso < animacionesExito.Length
            ? animacionesExito[paso]
            : null;

        if (controller != null && animadorExito != null && arteExito != null)
        {
            animadorExito.runtimeAnimatorController = controller;
            animadorExito.enabled = true;
            arteExito.SetActive(true);
            yield return new WaitForSeconds(duracionExito);
            arteExito.SetActive(false);
        }
        else
        {
            // Los pasos 2 y 3 todavía no tienen sprite: mostramos el mensaje.
            yield return new WaitForSeconds(duracionMensaje);
        }

        if (paso + 1 < 3)
        {
            paso++;
            posicion = 0f;
            direccion = 1;
            ocupado = false;
            ConfigurarPaso();
            yield break;
        }

        if (pedido != null) pedido.GuardarPedido();
        if (feedback != null)
        {
            feedback.text = "¡Pedido listo!";
            feedback.color = new Color(0.4f, 1f, 0.5f);
        }
        yield return new WaitForSeconds(duracionMensaje);

        activo = false;
        if (interfaz != null) interfaz.RegresarAlMostrador();
    }

    private static float Obtener(float[] valores, int indice, float porDefecto)
    {
        return valores != null && indice < valores.Length ? valores[indice] : porDefecto;
    }
}
