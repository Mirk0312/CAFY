using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GestorEscenas : MonoBehaviour
{
    public TMP_InputField inputNombreJugador;

    // Método para el botón INICIAR en el Menú Principal
    public void IrAJuego()
    {
        if (inputNombreJugador != null && !string.IsNullOrEmpty(inputNombreJugador.text))
        {
            // Guardamos el nombre ingresado por el usuario
            PlayerPrefs.SetString("NombreJugador", inputNombreJugador.text);
        }
        else
        {
            PlayerPrefs.SetString("NombreJugador", "Cafetero");
        }

        SceneManager.LoadScene("JuegoPrincipal"); 
    }

    // Método para el botón REANUDAR (en Pausa o Game Over)
    public void ReanudarJuego()
    {
        Time.timeScale = 1f; // Restauramos el tiempo por si estaba pausado
        SceneManager.LoadScene("JuegoPrincipal");
    }

    // Método para ir a la escena de Pausa mediante tecla ESC
    public void IrAPausa()
    {
        SceneManager.LoadScene("Pausa");
    }

    // Método para ir a Game Over (cuando pierdas las vidas o algo por el estilo)
    public void IrAGameOver()
    {
        SceneManager.LoadScene("GameOver");
    }

    // Método para volver al Menú Principal
    public void VolverAlMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MenuPrincipal");
    }

    // Método para el botón SALIR
    public void SalirDelJuego()
    {
        Debug.Log("Saliendo del juego...");
        Application.Quit();
    }
}
