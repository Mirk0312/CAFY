using System.Collections;
using TMPro;
using UnityEngine;

/// <summary>
/// Controla el teléfono del mostrador: al hacer clic reproduce la animación
/// de subida, muestra el sprite base con los botones Pedidos/Recetas y al
/// volver a hacer clic reproduce la de bajada. La lógica de pedidos real
/// vendrá después; aquí solo está la parte visual.
/// </summary>
public class ControladorTelefono : MonoBehaviour
{
    [Header("Referencias")]
    public Animator animador;
    public GameObject panelOpciones;
    public GameObject notificacion;
    public TextMeshProUGUI textoOpciones;

    [Header("Tiempo de las animaciones")]
    public float duracionSubida = 1f;
    public float duracionBajada = 1f;

    private bool arriba;
    private bool animando;

    private void Awake()
    {
        if (animador == null) animador = GetComponent<Animator>();
        if (panelOpciones != null) panelOpciones.SetActive(false);
        if (notificacion != null) notificacion.SetActive(false);
        if (textoOpciones != null) textoOpciones.text = "Elige una opción";
    }

    /// <summary>Se llama desde el Button del GameObject Telefono.</summary>
    public void AlternarTelefono()
    {
        if (animando || animador == null) return;

        if (arriba) StartCoroutine(BajarTelefono());
        else StartCoroutine(SubirTelefono());
    }

    private IEnumerator SubirTelefono()
    {
        animando = true;
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
        if (textoOpciones != null) textoOpciones.text = "Elige una opción";

        animador.SetTrigger("Bajar");
        yield return new WaitForSeconds(duracionBajada);

        arriba = false;
        animando = false;
    }

    public void MostrarPedidos()
    {
        if (textoOpciones != null) textoOpciones.text = "Pedidos\n(próximamente)";
    }

    public void MostrarRecetas()
    {
        if (textoOpciones != null) textoOpciones.text = "Recetas\n(próximamente)";
    }

    /// <summary>La usará la lógica de pedidos cuando llegue un encargo.</summary>
    public void MostrarNotificacion(bool visible)
    {
        if (notificacion != null) notificacion.SetActive(visible);
    }
}
