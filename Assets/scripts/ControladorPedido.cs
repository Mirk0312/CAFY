using TMPro;
using UnityEngine;

/// <summary>
/// Estado del pedido guardado: se crea al terminar los tres pasos del
/// minijuego, se puede desechar para hacer otro y se entrega pulsando el
/// botón invisible que hay sobre el cuadro gris del mostrador.
/// Todavía no suma puntos ni quita vidas.
/// </summary>
public class ControladorPedido : MonoBehaviour
{
    public TextMeshProUGUI textoPedido;
    public TextMeshProUGUI feedback;

    public bool HayPedido { get; private set; }

    private void Awake()
    {
        Actualizar();
    }

    public void GuardarPedido()
    {
        HayPedido = true;
        Mensaje("Pedido guardado", new Color(0.4f, 1f, 0.5f));
        Actualizar();
    }

    public void DescartarPedido()
    {
        HayPedido = false;
        Mensaje("Pedido descartado", new Color(1f, 0.75f, 0.3f));
        Actualizar();
    }

    public void EntregarPedido()
    {
        if (!HayPedido)
        {
            Mensaje("No hay pedido listo", new Color(1f, 0.45f, 0.35f));
            return;
        }

        HayPedido = false;
        Mensaje("¡Pedido entregado!", new Color(0.4f, 1f, 0.5f));
        Actualizar();
    }

    private void Mensaje(string texto, Color color)
    {
        if (feedback == null) return;
        feedback.text = texto;
        feedback.color = color;
    }

    private void Actualizar()
    {
        if (textoPedido != null)
        {
            textoPedido.text = HayPedido ? "Pedido listo" : "Sin pedido";
        }
    }
}
