using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class HUDJuego : MonoBehaviour
{
    private const string EscenaJuego = "JuegoPrincipal";

    public TextMeshProUGUI textoNombreJugador;

    private void OnEnable()
    {
        PerfilJugador.NombreCambiado += ActualizarNombre;
    }

    private void OnDisable()
    {
        PerfilJugador.NombreCambiado -= ActualizarNombre;
    }

    private void Start()
    {
        // Si no está asignado en el Inspector, lo buscamos por su nombre real.
        if (textoNombreJugador == null)
        {
            GameObject objTexto = GameObject.Find("nombreJugador");
            if (objTexto != null)
            {
                textoNombreJugador = objTexto.GetComponent<TextMeshProUGUI>();
            }
        }

        ActualizarNombre(PerfilJugador.Nombre);
    }

    private void ActualizarNombre(string nombre)
    {
        if (textoNombreJugador != null)
        {
            textoNombreJugador.text = "Jugador: " + nombre;
        }
    }

    private void Update()
    {
        // Este script está en todas las escenas. Solo debe reaccionar en
        // JuegoPrincipal y solo en la escena activa (Pausa se carga encima).
        if (SceneManager.GetActiveScene().name != EscenaJuego) return;
        if (gameObject.scene != SceneManager.GetActiveScene()) return;
        // Durante el 5-4-3-2-1 no se puede ni pausar.
        if (ControladorCuentaRegresiva.Instancia != null &&
            !ControladorCuentaRegresiva.Terminado) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            GestorEscenas gestor = FindAnyObjectByType<GestorEscenas>();
            if (gestor != null)
            {
                gestor.AlternarPausa();
            }
        }
    }
}
