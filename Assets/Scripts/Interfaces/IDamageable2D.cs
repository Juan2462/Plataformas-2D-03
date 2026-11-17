using UnityEngine;

public interface IDamageable2D
{
    bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso);
}
