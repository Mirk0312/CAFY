using System;
using UnityEngine;

/// <summary>
/// Acceso único a los datos persistentes del jugador.
/// Antes GestorEscenas y HUDJuego escribían/leían PlayerPrefs por separado y
/// podían sobrescribir el nombre con "Cafetero" al volver del panel de preparación.
/// </summary>
public static class PerfilJugador
{
    private const string ClaveNombre = "NombreJugador";
    private const string NombrePorDefecto = "Cafetero";

    public static event Action<string> NombreCambiado;

    public static string Nombre
    {
        get
        {
            string nombre = PlayerPrefs.GetString(ClaveNombre, NombrePorDefecto);
            return string.IsNullOrWhiteSpace(nombre) ? NombrePorDefecto : nombre.Trim();
        }
        set
        {
            string nombre = string.IsNullOrWhiteSpace(value) ? NombrePorDefecto : value.Trim();
            PlayerPrefs.SetString(ClaveNombre, nombre);
            PlayerPrefs.Save();
            NombreCambiado?.Invoke(nombre);
        }
    }
}
