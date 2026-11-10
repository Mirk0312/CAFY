using UnityEngine;

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

    // Este método se llamará al hacer clic en la cafetera
    public void AbrirEstacionPreparacion()
    {
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(false);
        if (PanelPreparacion != null) PanelPreparacion.SetActive(true);
    }

    // Se ejecuta al hacer clic en el botón "btnPreparar" de los ingredientes
    public void IniciarMinijuego()
    {
        if (PanelPreparacion != null) PanelPreparacion.SetActive(false);
        if (PanelMinijuego != null) PanelMinijuego.SetActive(true);
    }

    // Se ejecuta cuando termines el minijuego con éxito para volver al mostrador
    public void RegresarAlMostrador()
    {
        if (PanelMinijuego != null) PanelMinijuego.SetActive(false);
        if (PanelJuegoPrincipal != null) PanelJuegoPrincipal.SetActive(true);
    }
}