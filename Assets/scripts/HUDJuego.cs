using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HUDJuego : MonoBehaviour
{
    public TextMeshProUGUI textoNombreJugador;

    void Start()
    {
        // Si no está asignado en el Inspector, lo buscamos automáticamente
        if (textoNombreJugador == null)
        {
            GameObject objTexto = GameObject.Find("nombreJugado");
            if (objTexto != null)
            {
                textoNombreJugador = objTexto.GetComponent<TextMeshProUGUI>();
            }
        }

        // Cargamos el nombre del jugador o usamos "Cafetero" por defecto
        string nombre = PlayerPrefs.GetString("NombreJugador", "Cafetero");

        if (string.IsNullOrEmpty(nombre) || nombre.Trim() == "")
        {
            nombre = "Cafetero";
        }

        if (textoNombreJugador != null)
        {
            textoNombreJugador.text = "Jugador: " + nombre;
        }
    }

    void Update()
    {
        // Detecta la tecla ESC para ir a la escena de Pausa
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            SceneManager.LoadScene("Pausa");
        }
    }
}