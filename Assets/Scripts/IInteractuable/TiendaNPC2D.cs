using UnityEngine;

public class TiendaNPC2D : MonoBehaviour
{
    [Header("Configuración de la Tienda")]
    [SerializeField] private string nombreTienda = "Mercader del Acueducto";
    [SerializeField] private ItemTienda2D[] itemsDisponibles;
    
    [Header("Texto de Interacción")]
    [SerializeField] private string textoIndicacion = "Presiona [E] para abrir la Tienda";

    public string TextoIndicacion => textoIndicacion;
    public bool EstaInteractuableDisponible => true;

    // Método que se ejecuta cuando el jugador interactúa con el NPC
    public void Interactuar(InventarioJugador2D inventario)
    {
        AbrirInterfazTienda(inventario);
    }

    private void AbrirInterfazTienda(InventarioJugador2D inventario)
    {
        Debug.Log($"--- Abriendo tienda de: {nombreTienda} ---");
        
        if (itemsDisponibles.Length > 0)
        {
            // Ejemplo: intenta comprar el primer objeto de prueba al interactuar
            IntentarComprarItem(0, inventario);
        }
    }

    public void IntentarComprarItem(int indiceItem, InventarioJugador2D inventario)
    {
        if (indiceItem < 0 || indiceItem >= itemsDisponibles.Length) return;

        ItemTienda2D item = itemsDisponibles[indiceItem];

        if (item.comprado && item.esUnico)
        {
            Debug.Log("Este objeto ya ha sido comprado.");
            return;
        }

        if (inventario != null)
        {
            // Usamos el método seguro de tu InventarioJugador2D para gastar monedas
            if (inventario.IntentarGastarMonedas(item.costoEcos))
            {
                if (item.esUnico) 
                    item.comprado = true;

                Debug.Log($"¡Compra exitosa! Has adquirido: {item.nombreItem}");
                AplicarEfectoItem(item, inventario);
            }
            else
            {
                Debug.Log("No tienes suficientes monedas (ecos).");
            }
        }
    }

    private void AplicarEfectoItem(ItemTienda2D item, InventarioJugador2D inventario)
    {
        // Dependiendo del tipo de objeto, lo añadimos al inventario o aplicamos su lógica
        if (item.nombreItem.Contains("Llave"))
        {
            inventario.AgregarLlave(item.name, 1);
        }
        else 
        {
            inventario.AgregarObjeto(item.name, 1);
        }
    }
}