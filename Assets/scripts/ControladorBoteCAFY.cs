using UnityEngine;

/// <summary>
/// Ajustes editables del bote de basura. Se coloca en el botón PedidoDesechar
/// para que puedas mover la tapa y cambiar los sprites desde el Inspector.
/// </summary>
public class ControladorBoteCAFY : MonoBehaviour
{
    [Header("Tapa abierta")]
    [Tooltip("Cuánto se desplaza la tapa respecto al bote cerrado.")]
    public Vector2 tapaAbiertaOffset = new Vector2(6f, 160f);
    [Tooltip("Rotación de la tapa en grados cuando está abierta.")]
    public float tapaAbiertaRotacion = -28f;
    [Tooltip("Segundos que tarda en subir o bajar la tapa.")]
    public float duracionTapa = 0.18f;

    [Header("Sprites (opcional)")]
    [Tooltip("Si se asigna, reemplaza el frame BoteBasuraS_0.")]
    public Sprite spriteCerrado;
    [Tooltip("Si se asigna, reemplaza el frame BoteBasuraS_1 (solo tapa).")]
    public Sprite spriteTapa;
    [Tooltip("Si se asigna, reemplaza el frame BoteBasuraS_2 (solo cuerpo).")]
    public Sprite spriteCuerpo;
}
