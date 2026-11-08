using UnityEngine;

public sealed class AtajoCanal2D : Interactuable2D
{
    [SerializeField] private EnemigoPrototipo2D guardian;
    [SerializeField] private Transform destino;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;

    private bool guardianDerrotado;

    public bool EstaDisponible => guardianDerrotado;
    public EnemigoPrototipo2D Guardian => guardian;

    private void Update()
    {
        if (guardian != null && !guardian.EstaVivo)
            guardianDerrotado = true;
    }

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (inventario == null)
        {
            Debug.LogError("El atajo necesita el inventario del jugador para identificarlo.", this);
            return;
        }

        Jugador2D jugador = inventario.GetComponent<Jugador2D>();
        if (jugador == null)
        {
            Debug.LogError("El inventario del atajo debe pertenecer al jugador.", this);
            return;
        }

        if (!EstaDisponible)
        {
            interfaz?.MostrarMensaje("El atajo se libera cuando cese el protocolo del guardian.");
            return;
        }

        if (destino == null)
        {
            Debug.LogError("Asigna el punto de llegada del atajo.", this);
            return;
        }

        Rigidbody2D cuerpo = jugador.GetComponent<Rigidbody2D>();
        if (cuerpo != null)
        {
            cuerpo.position = destino.position;
            cuerpo.linearVelocity = Vector2.zero;
        }
        else
        {
            jugador.transform.position = destino.position;
        }

        interfaz?.MostrarMensaje("Atajo abierto: regreso al inicio del Canal.");
    }
}
