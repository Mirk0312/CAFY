using UnityEngine;

public enum Ingrediente
{
    Cafe,
    Azucar,
    Leche,
    Chocolate,
    Jarabe
}

/// <summary>
/// Receta editable en el Inspector. Define cuántos pasos tiene el minijuego,
/// qué animación se muestra en cada uno y con qué dificultad.
/// </summary>
[CreateAssetMenu(menuName = "CAFY/Receta", fileName = "Receta")]
public class Receta : ScriptableObject
{
    [Header("Datos")]
    public string nombre = "Receta";
    [TextArea(2, 4)] public string descripcion;
    public Sprite icono;
    public Ingrediente[] ingredientes;

    [Header("Pasos del minijuego")]
    public string[] nombresPaso = { "Extracción" };
    public RuntimeAnimatorController[] animacionesExito;
    public float[] velocidades = { 1.1f };
    public float[] anchosZona = { 0.24f };
    public Color[] coloresZona;

    public int NumeroPasos => nombresPaso != null && nombresPaso.Length > 0
        ? nombresPaso.Length
        : 1;

    public string NombrePaso(int indice)
    {
        return nombresPaso != null && indice < nombresPaso.Length
            ? nombresPaso[indice]
            : "Paso " + (indice + 1);
    }

    public RuntimeAnimatorController Animacion(int indice)
    {
        return animacionesExito != null && indice < animacionesExito.Length
            ? animacionesExito[indice]
            : null;
    }

    public float Velocidad(int indice)
    {
        return velocidades != null && indice < velocidades.Length
            ? velocidades[indice]
            : 1.1f;
    }

    public float AnchoZona(int indice)
    {
        return anchosZona != null && indice < anchosZona.Length
            ? anchosZona[indice]
            : 0.24f;
    }

    public Color ColorZona(int indice, Color porDefecto)
    {
        return coloresZona != null && indice < coloresZona.Length
            ? coloresZona[indice]
            : porDefecto;
    }
}
