using UnityEngine;

public abstract class Interactuable2D : MonoBehaviour
{
    public virtual bool EstaInteractuableDisponible => isActiveAndEnabled;

    public virtual string TextoIndicacion => "E / Y · Interactuar";

    public abstract void Interactuar(InventarioJugador2D inventario);
}
