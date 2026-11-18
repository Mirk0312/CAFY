using UnityEngine;
using UnityEngine.SceneManagement;

public class ControladorInterfazPrincipal : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject PanelJuegoPrincipal;
    public GameObject PanelPreparacion;
    public GameObject PanelMinijuego;

    void Start()
    {
        // solo el mostrador principal empiece visible
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(true);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
    }

    // Este m�todo se llamar� al hacer clic en la cafetera
    public void AbrirEstacionPreparacion()
    {
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(true);
    }

    // Se ejecuta al hacer clic en el bot�n "btnPreparar" de los ingredientes
    public void IniciarMinijuego()
    {
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelMinijuego != null) PanelMinijuego.SetActive(true);
    }

    // Se ejecuta cuando termines el minijuego con �xito para volver al mostrador
    public void RegresarAPreparacion()
    {
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(true);

    }

    // Botón "volver" del panel de preparación: vuelve al mostrador sin recargar
    // la escena (antes llamaba a IrAJuego, que reiniciaba la partida y el nombre).
    public void RegresarAlMostrador()
    {
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(true);
    }

}