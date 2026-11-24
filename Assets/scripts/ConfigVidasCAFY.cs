using UnityEngine;

/// <summary>
/// Referencias de los sprites del HUD de vidas. Se guarda como asset en Resources
/// para que el controlador (que se crea en tiempo de ejecución) pueda usar los
/// sprites de Assets/Imagenes sin moverlos de carpeta.
/// </summary>
[CreateAssetMenu(menuName = "CAFY/Configuración de vidas", fileName = "ConfigVidasCAFY")]
public class ConfigVidasCAFY : ScriptableObject
{
    public Sprite vidaBuena;
    public Sprite vidaPerdida;
    /// <summary>Primer frame de la bolsa para que no aparezca un cuadro blanco.</summary>
    public Sprite bolsaEntrega;
}
