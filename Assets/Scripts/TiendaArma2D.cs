using UnityEngine;

public class TiendaArma2D : Interactuable2D
{
    [SerializeField] private string idArma;
    [SerializeField, Min(0)] private int precio;
    [SerializeField, Min(1)] private int cantidad = 1;

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (inventario == null)
        {
            Debug.LogError("No se puede comprar sin un inventario.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(idArma))
        {
            Debug.LogError("Asigna un ID al arma que vende esta tienda.", this);
            return;
        }

        if (cantidad <= 0 || precio < 0)
        {
            Debug.LogError("El precio no puede ser negativo y la cantidad debe ser mayor que cero.", this);
            return;
        }

        if (inventario.Monedas < precio)
        {
            Debug.Log("No tienes suficientes monedas. Precio: " + precio, this);
            return;
        }

        if (inventario.IntentarGastarMonedas(precio))
            inventario.AgregarArma(idArma, cantidad);
    }
}
