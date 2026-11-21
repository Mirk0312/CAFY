using System;
using UnityEngine;

/// <summary>
/// Estado de la partida en memoria. Es estático para sobrevivir a cargas de
/// escena: así pausar/reanudar o ir a GameOver no reinicia vidas, nivel ni puntos.
/// </summary>
public static class EstadoPartida
{
    public const int VidasIniciales = 3;

    public static int Vidas { get; private set; } = VidasIniciales;
    public static int Nivel { get; private set; } = 1;
    public static int Puntos { get; private set; }
    public static string NombreJugador { get; private set; } = "Cafetero";

    public static bool SinVidas => Vidas <= 0;

    public static event Action<int> VidasCambiadas;
    public static event Action<int> PuntosCambiados;
    public static event Action<int> NivelCambiado;
    /// <summary>Solo se dispara al subir de nivel (no al reiniciar la partida).</summary>
    public static event Action<int> NivelSubido;
    public static event Action PartidaPerdida;
    public static event Action PartidaReiniciada;

    public static void Reiniciar(string nombreJugador = null)
    {
        Vidas = VidasIniciales;
        Nivel = 1;
        Puntos = 0;
        NombreJugador = string.IsNullOrWhiteSpace(nombreJugador)
            ? PerfilJugador.Nombre
            : nombreJugador.Trim();

        PartidaReiniciada?.Invoke();
        VidasCambiadas?.Invoke(Vidas);
        PuntosCambiados?.Invoke(Puntos);
        NivelCambiado?.Invoke(Nivel);
    }

    public static void PerderVida()
    {
        if (Vidas <= 0) return;

        Vidas--;
        VidasCambiadas?.Invoke(Vidas);

        if (SinVidas) PartidaPerdida?.Invoke();
    }

    /// <summary>Fuerza el fin de la partida (por ejemplo, al juntar 5 pedidos).</summary>
    public static void PerderPartida()
    {
        Vidas = 0;
        VidasCambiadas?.Invoke(Vidas);
        PartidaPerdida?.Invoke();
    }

    public static void SumarPuntos(int cantidad)
    {
        if (cantidad <= 0) return;

        Puntos += cantidad;
        PuntosCambiados?.Invoke(Puntos);
    }

    public static void SubirNivel()
    {
        Nivel++;
        // Cada nivel nuevo empieza con las 3 vidas llenas.
        Vidas = VidasIniciales;
        VidasCambiadas?.Invoke(Vidas);
        NivelCambiado?.Invoke(Nivel);
        NivelSubido?.Invoke(Nivel);
    }
}
