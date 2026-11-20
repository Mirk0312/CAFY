using System;
using UnityEngine;

/// <summary>
/// Pedido que llega al teléfono. El cliente se identifica solo por su número
/// (no le ponemos nombre) y guarda la receta que pide, con sus ingredientes
/// en el orden correcto.
/// </summary>
[Serializable]
public class PedidoCliente
{
    public int id;
    public Receta receta;
    public Ingrediente[] ingredientes;
    public Color color;
    /// <summary>Momento (Time.time) en que llegó el pedido; sirve para calcular puntos.</summary>
    public float creadoEn;
    /// <summary>Extra del nivel 2+, por ejemplo "+2 de azúcar". No enumera la receta completa.</summary>
    public string extra;

    public string Titulo => "Pedido #" + id;

    public string Descripcion
    {
        get
        {
            string nombre = receta != null && !string.IsNullOrEmpty(receta.nombre)
                ? receta.nombre
                : "Bebida";
            return string.IsNullOrEmpty(extra) ? nombre : nombre + "  " + extra;
        }
    }

    private static string Juntar(Ingrediente[] lista)
    {
        if (lista == null || lista.Length == 0) return "-";
        string[] nombres = new string[lista.Length];
        for (int i = 0; i < lista.Length; i++) nombres[i] = ControladorRecetas.Nombre(lista[i]);
        return string.Join(" + ", nombres);
    }
}

/// <summary>
/// Bebida ya preparada que se guarda en el inventario. Copiamos los datos de
/// la receta temporal del minijuego porque esa receta se destruye al preparar
/// otra; así el inventario nunca apunta a un objeto borrado.
/// </summary>
[Serializable]
public class BebidaPreparada
{
    public int id;
    public string nombre;
    public Sprite icono;
    public Ingrediente[] ingredientes;

    public string Titulo => "Bebida #" + id + " - " + (string.IsNullOrEmpty(nombre) ? "Sin nombre" : nombre);

    public string Descripcion => Juntar(ingredientes);

    /// <summary>Compara cantidades por ingrediente; el orden ya no importa.</summary>
    public bool CoincideCon(PedidoCliente pedido)
    {
        if (pedido == null || ingredientes == null || pedido.ingredientes == null) return false;

        int total = System.Enum.GetValues(typeof(Ingrediente)).Length;
        int[] requerido = new int[total];
        int[] preparado = new int[total];

        foreach (Ingrediente ingrediente in pedido.ingredientes)
            requerido[(int)ingrediente]++;

        foreach (Ingrediente ingrediente in ingredientes)
            preparado[(int)ingrediente]++;

        for (int i = 0; i < total; i++)
        {
            if (preparado[i] != requerido[i]) return false;
        }
        return true;
    }

    private static string Juntar(Ingrediente[] lista)
    {
        if (lista == null || lista.Length == 0) return "-";
        string[] nombres = new string[lista.Length];
        for (int i = 0; i < lista.Length; i++) nombres[i] = ControladorRecetas.Nombre(lista[i]);
        return string.Join(" + ", nombres);
    }
}
