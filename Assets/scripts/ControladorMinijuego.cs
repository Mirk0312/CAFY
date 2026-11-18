using System.Collections;
using System.Collections.Generic;
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
    public Receta receta;

    [Header("Ingredientes elegidos en Preparación")]
    public Ingrediente[] ingredientes;
    public RuntimeAnimatorController animacionCafetera;
    public RuntimeAnimatorController animacionJarabe;

    private Receta recetaTemporal;

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

    /// <summary>Recibe los ingredientes marcados en el panel de preparación.</summary>
    public void EstablecerIngredientes(Ingrediente[] seleccionados)
    {
        ingredientes = seleccionados;
        if (recetaTemporal != null)
        {
            Destroy(recetaTemporal);
            recetaTemporal = null;
        }
        receta = null;
    }

    /// <summary>Llama el botón Preparar al abrir el panel del minijuego.</summary>
    public void Iniciar()
    {
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }

        if (receta == null && ingredientes != null && ingredientes.Length > 0)
        {
            recetaTemporal = ConstruirRecetaDesdeIngredientes(ingredientes);
            receta = recetaTemporal;
        }

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
        float ancho = AnchoActual();

        if (zonaVerde != null)
        {
            zonaVerde.anchorMin = new Vector2(0.5f - ancho * 0.5f, 0f);
            zonaVerde.anchorMax = new Vector2(0.5f + ancho * 0.5f, 1f);
            zonaVerde.offsetMin = Vector2.zero;
            zonaVerde.offsetMax = Vector2.zero;

            Image imagenZona = zonaVerde.GetComponent<Image>();
            if (imagenZona != null)
            {
                imagenZona.color = receta != null
                    ? receta.ColorZona(paso, new Color(0.25f, 0.75f, 0.35f, 0.9f))
                    : new Color(0.25f, 0.75f, 0.35f, 0.9f);
            }
        }

        string nombrePaso = receta != null ? receta.NombrePaso(paso) : "Paso " + (paso + 1);
        if (titulo != null) titulo.text = nombrePaso + "  (" + (paso + 1) + "/" + NumeroPasos() + ")";
        if (instruccion != null)
        {
            instruccion.text = receta != null && !string.IsNullOrEmpty(receta.descripcion)
                ? receta.descripcion
                : "Pulsa cuando el cursor esté en la zona marcada";
        }
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

        posicion += Time.deltaTime * VelocidadActual() * direccion;
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

        float ancho = AnchoActual();
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

        RuntimeAnimatorController controller = receta != null
            ? receta.Animacion(paso)
            : (animacionesExito != null && paso < animacionesExito.Length ? animacionesExito[paso] : null);

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

        if (paso + 1 < NumeroPasos())
        {
            paso++;
            posicion = 0f;
            direccion = 1;
            ocupado = false;
            ConfigurarPaso();
            yield break;
        }

        if (pedido != null) pedido.GuardarPedido(receta);
        if (feedback != null)
        {
            feedback.text = "¡Pedido listo!";
            feedback.color = new Color(0.4f, 1f, 0.5f);
        }
        yield return new WaitForSeconds(duracionMensaje);

        activo = false;
        if (interfaz != null) interfaz.RegresarAlMostrador();
    }

    private Receta ConstruirRecetaDesdeIngredientes(Ingrediente[] seleccionados)
    {
        List<string> pasos = new List<string>();
        List<RuntimeAnimatorController> animaciones = new List<RuntimeAnimatorController>();
        List<float> velocidades = new List<float>();
        List<float> anchos = new List<float>();
        List<Color> colores = new List<Color>();
        List<string> nombres = new List<string>();

        foreach (Ingrediente ingrediente in seleccionados)
        {
            nombres.Add(ControladorRecetas.Nombre(ingrediente));
            switch (ingrediente)
            {
                case Ingrediente.Cafe:
                    pasos.Add("Extracción");
                    animaciones.Add(animacionCafetera);
                    velocidades.Add(1.1f);
                    anchos.Add(0.24f);
                    colores.Add(new Color(0.95f, 0.62f, 0.22f, 0.95f));
                    break;
                case Ingrediente.Azucar:
                    pasos.Add("Endulzado");
                    animaciones.Add(null);
                    velocidades.Add(1.4f);
                    anchos.Add(0.18f);
                    colores.Add(new Color(0.95f, 0.82f, 0.30f, 0.95f));
                    break;
                case Ingrediente.Leche:
                    pasos.Add("Vaporizado de leche");
                    animaciones.Add(null);
                    velocidades.Add(1.7f);
                    anchos.Add(0.16f);
                    colores.Add(new Color(0.95f, 0.88f, 0.72f, 0.95f));
                    break;
                case Ingrediente.Chocolate:
                    pasos.Add("Latte art");
                    animaciones.Add(null);
                    velocidades.Add(2.5f);
                    anchos.Add(0.10f);
                    colores.Add(new Color(0.55f, 0.30f, 0.16f, 0.95f));
                    break;
                case Ingrediente.Jarabe:
                    pasos.Add("Toque de jarabe");
                    animaciones.Add(animacionJarabe);
                    velocidades.Add(1.5f);
                    anchos.Add(0.18f);
                    colores.Add(new Color(0.78f, 0.45f, 0.18f, 0.95f));
                    break;
            }
        }

        Receta receta = ScriptableObject.CreateInstance<Receta>();
        receta.nombre = string.Join(" + ", nombres);
        receta.descripcion = "Prepara " + receta.nombre;
        receta.ingredientes = seleccionados;
        receta.nombresPaso = pasos.ToArray();
        receta.animacionesExito = animaciones.ToArray();
        receta.velocidades = velocidades.ToArray();
        receta.anchosZona = anchos.ToArray();
        receta.coloresZona = colores.ToArray();
        return receta;
    }

    private int NumeroPasos()
    {
        return receta != null ? receta.NumeroPasos : 3;
    }

    private float VelocidadActual()
    {
        return receta != null ? receta.Velocidad(paso) : Obtener(velocidades, paso, 1f);
    }

    private float AnchoActual()
    {
        return receta != null ? receta.AnchoZona(paso) : Obtener(anchosZona, paso, 0.2f);
    }

    private static float Obtener(float[] valores, int indice, float porDefecto)
    {
        return valores != null && indice < valores.Length ? valores[indice] : porDefecto;
    }
}
