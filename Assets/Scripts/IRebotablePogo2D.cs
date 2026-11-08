using UnityEngine;

/// <summary>
/// Interfaz para cualquier entidad u obstaculo que reaccione fisicamente
/// al ataque hacia abajo (pogo) del jugador, permitiendo el rebote vertical.
/// </summary>
public interface IRebotablePogo2D
{
    /// <summary>
    /// Se ejecuta cuando un ataque hacia abajo impacta sobre este objetivo.
    /// </summary>
    /// <param name="jugador">Referencia al jugador que ejecuta el ataque.</param>
    /// <param name="puntoContacto">Punto aproximado del impacto.</param>
    /// <returns>True si el rebote fue aceptado y ejecutado.</returns>
    bool AlRecibirPogo(Jugador2D jugador, Vector2 puntoContacto);
}
