using UnityEngine;

public sealed class ConexionZona2D : Interactuable2D
{
    [SerializeField] private ZonaMetroidvania2D zona;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;
    [SerializeField] private string rutaEscenaDestino;
    [SerializeField] private string idEntradaDestino = "entrada-principal";

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (zona == null)
        {
            Debug.LogError("La conexion necesita una ZonaMetroidvania2D.", this);
            return;
        }

        if (inventario == null || inventario.GetComponent<Jugador2D>() == null)
        {
            Debug.LogError("La conexion espera el inventario del jugador.", this);
            return;
        }

        Jugador2D jugador = inventario.GetComponent<Jugador2D>();
        if (!jugador.DashDesbloqueado)
        {
            interfaz?.MostrarMensaje("El paso a " + zona.SiguienteZona + " requiere Dash.");
            return;
        }

        if (string.IsNullOrWhiteSpace(rutaEscenaDestino))
        {
            interfaz?.MostrarMensaje("La conexion a " + zona.SiguienteZona + " esta marcada; la zona aun no esta construida.");
            return;
        }

        if (!GestorReaparicion2D.SolicitarTransicion(rutaEscenaDestino, idEntradaDestino))
            interfaz?.MostrarMensaje("No se pudo abrir la conexion a " + zona.SiguienteZona + ".");
    }
}
