using UnityEngine;
using TMPro;
using UnityEngine.InputSystem; // <-- Añade esta librería

public class HUDJuego : MonoBehaviour
{
    public TextMeshProUGUI textoNombreJugador;

    void Start()
    {
        string nombre = PlayerPrefs.GetString("NombreJugador", "Cafetero");

        if (textoNombreJugador != null)
        {
            textoNombreJugador.text = "Jugador: " + nombre;
        }
    }

    void Update()
    {
        // Forma compatible con Unity 6 para detectar la tecla ESC
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene("Pausa");
        }
    }
}