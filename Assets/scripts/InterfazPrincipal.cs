using UnityEngine;

public class ControladorInterfazPrincipal : MonoBehaviour
{
    [Header("Paneles Principales")]
    public GameObject PanelJuegoPrincipal;
    public GameObject PanelPreparacion;
    public GameObject PanelMinijuego;
    public ControladorMinijuego minijuego;

    void Start()
    {
        // Solo el mostrador principal empieza visible.
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(true);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
    }

    // Este método se llama al hacer clic en la cafetera.
    public void AbrirEstacionPreparacion()
    {
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(true);
    }

    // Se ejecuta al hacer clic en el botón "btnPreparar" de los ingredientes.
    public void IniciarMinijuego()
    {
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelMinijuego != null) PanelMinijuego.SetActive(true);
        if (minijuego != null) minijuego.Iniciar();
    }

    // Se ejecuta cuando termines el minijuego con éxito para volver al mostrador.
    public void RegresarAPreparacion()
    {
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(true);
        if (minijuego != null) minijuego.Detener();
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
