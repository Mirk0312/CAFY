using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el teléfono del mostrador: animación de subida/bajada, panel de
/// opciones, notificación grande (teléfono abajo) y mini badge sobre Pedidos
/// cuando el teléfono está arriba.
/// </summary>
public class ControladorTelefono : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animador;
    public GameObject panelOpciones;
    public GameObject notificacion;
    public TextMeshProUGUI textoOpciones;
    public ControladorRecetas panelRecetas;

    [Header("Tiempo de las animaciones")]
    [Tooltip("Duración de la subida; coincide con el clip TelefonoSube (0.6 s).")]
    public float duracionSubida = 0.6f;
    [Tooltip("Duración de la bajada; coincide con el clip TelefonoBaja (0.65 s).")]
    public float duracionBajada = 0.65f;

    private bool arriba;
    private bool animando;
    private Coroutine rutina;
    private Coroutine rutinaNoti;

    private GameObject badgePedidos;
    private TextMeshProUGUI badgeTexto;
    private int pedidosSinVer;

    public bool EstaArriba => arriba;

    private void Awake()
    {
        // La escena guardaba 1 s de la versión anterior; los clips ahora duran
        // 0.6 s y 0.65 s. Se fuerza aquí para que siempre coincidan.
        duracionSubida = 0.6f;
        duracionBajada = 0.65f;

        if (animador == null) animador = GetComponent<Animator>();
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (notificacion != null) notificacion.SetActive(false);
        OcultarTextoOpciones();
        CrearBadgePedidos();
    }

    /// <summary>El panel de opciones solo muestra botones: sin texto de estado.</summary>
    private void OcultarTextoOpciones()
    {
        if (textoOpciones != null) textoOpciones.gameObject.SetActive(false);

        // La escena tiene un TextoOpciones duplicado del generador; también se oculta.
        foreach (Transform t in FindObjectsByType<Transform>(FindObjectsInactive.Include))
        {
            if (t.name != "TextoOpciones") continue;
            if (t.gameObject.scene != gameObject.scene) continue;
            t.gameObject.SetActive(false);
        }
    }

    /// <summary>Se llama desde el Button del GameObject Telefono.</summary>
    public void AlternarTelefono()
    {
        if (animando || animador == null) return;

        if (arriba) rutina = StartCoroutine(BajarTelefono());
        else rutina = StartCoroutine(SubirTelefono());
    }

    private IEnumerator SubirTelefono()
    {
        animando = true;
        // La notificación grande queda abajo: se oculta para no quedarse flotando.
        if (notificacion != null) notificacion.SetActive(false);

        animador.SetTrigger("Subir");
        yield return new WaitForSeconds(duracionSubida);

        arriba = true;
        animando = false;
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    private IEnumerator BajarTelefono()
    {
        animando = true;
        if (panelOpciones != null) panelOpciones.SetActive(false);

        animador.SetTrigger("Bajar");
        yield return new WaitForSeconds(duracionBajada);

        arriba = false;
        animando = false;
        MostrarNotificacionPendienteAlBajar();
    }

    /// <summary>Cierra el teléfono sin esperar la animación (lo usa el panel de recetas).</summary>
    public void CerrarInmediatamente()
    {
        if (rutina != null) { StopCoroutine(rutina); rutina = null; }
        animando = false;
        arriba = false;

        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (animador != null) animador.SetTrigger("Bajar");
        MostrarNotificacionPendienteAlBajar();
    }

    private void MostrarNotificacionPendienteAlBajar()
    {
        if (pedidosSinVer <= 0 || notificacion == null) return;

        notificacion.SetActive(true);
        if (rutinaNoti != null) StopCoroutine(rutinaNoti);
        rutinaNoti = StartCoroutine(OcultarNotificacionAutomatica(5f));
    }

    public void MostrarPedidos()
    {
        // Primero se marca como visto y se baja el teléfono; así no aparece
        // la notificación grande encima de la ventana de pedidos.
        MarcarPedidosVistos();
        CerrarInmediatamente();
        if (rutinaNoti != null) { StopCoroutine(rutinaNoti); rutinaNoti = null; }
        if (notificacion != null) notificacion.SetActive(false);

        ControladorPedidos sistema = ControladorPedidos.Instancia;
        if (sistema != null) sistema.AbrirPanelPedidos();
    }

    public void MostrarRecetas()
    {
        if (panelRecetas != null) panelRecetas.Abrir();
    }

    /// <summary>
    /// Teléfono abajo: notificación grande. Teléfono arriba o en movimiento:
    /// mini badge sobre el botón Pedidos, que se mantiene hasta que el jugador lo vea.
    /// </summary>
    public void MostrarNotificacion(bool visible)
    {
        if (notificacion == null) return;

        if (visible)
        {
            // El aviso siempre queda registrado, esté el teléfono arriba o abajo.
            // Así el badge persiste aunque la notificación grande se oculte a los
            // 5 s o el jugador suba el teléfono, y al bajarlo vuelve a aparecer.
            int pendientes = ControladorPedidos.Instancia != null
                ? ControladorPedidos.Instancia.PedidosPendientes
                : 1;
            SenalarPedido(pendientes);
        }

        if (visible && (arriba || animando))
        {
            notificacion.SetActive(false);
            return;
        }

        if (visible)
        {
            notificacion.SetActive(true);
            if (rutinaNoti != null) StopCoroutine(rutinaNoti);
            rutinaNoti = StartCoroutine(OcultarNotificacionAutomatica(5f));
        }
        else
        {
            notificacion.SetActive(false);
        }
    }

    private IEnumerator OcultarNotificacionAutomatica(float espera)
    {
        yield return new WaitForSeconds(espera);
        if (notificacion != null) notificacion.SetActive(false);
        rutinaNoti = null;
    }

    /// <summary>Enciende el mini contador sobre btnPedidos.</summary>
    public void SenalarPedido(int cantidad)
    {
        pedidosSinVer = Mathf.Max(1, cantidad);
        if (badgePedidos == null) CrearBadgePedidos();
        if (badgePedidos != null) badgePedidos.SetActive(true);
        if (badgeTexto != null) badgeTexto.text = pedidosSinVer.ToString();
    }

    /// <summary>El jugador abrió Pedidos: apagamos el aviso.</summary>
    public void MarcarPedidosVistos()
    {
        pedidosSinVer = 0;
        if (badgePedidos != null) badgePedidos.SetActive(false);
    }

    private void CrearBadgePedidos()
    {
        Button[] botones = FindObjectsByType<Button>(FindObjectsInactive.Include);
        foreach (Button boton in botones)
        {
            if (boton.name != "btnPedidos") continue;
            if (boton.gameObject.scene != gameObject.scene) continue;
            if (panelOpciones != null && !boton.transform.IsChildOf(panelOpciones.transform)) continue;

            GameObject go = new GameObject("BadgePedidos",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(boton.transform, false);

            RectTransform rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.72f, 0.70f);
            rect.anchorMax = new Vector2(1.14f, 1.14f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            Image imagen = go.GetComponent<Image>();
            imagen.color = new Color(0.92f, 0.22f, 0.18f, 1f);
            imagen.raycastTarget = false;

            GameObject numeroGO = new GameObject("Numero",
                typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            numeroGO.transform.SetParent(go.transform, false);

            RectTransform numeroRect = numeroGO.GetComponent<RectTransform>();
            numeroRect.anchorMin = Vector2.zero;
            numeroRect.anchorMax = Vector2.one;
            numeroRect.offsetMin = Vector2.zero;
            numeroRect.offsetMax = Vector2.zero;

            badgeTexto = numeroGO.GetComponent<TextMeshProUGUI>();
            if (TMP_Settings.defaultFontAsset != null) badgeTexto.font = TMP_Settings.defaultFontAsset;
            badgeTexto.text = "1";
            badgeTexto.fontSize = 22f;
            badgeTexto.alignment = TextAlignmentOptions.Center;
            badgeTexto.color = Color.white;
            badgeTexto.raycastTarget = false;

            badgePedidos = go;
            badgePedidos.SetActive(false);
            return;
        }
    }
}
