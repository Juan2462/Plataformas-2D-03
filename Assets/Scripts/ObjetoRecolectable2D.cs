using UnityEngine;

public class ObjetoRecolectable2D : MonoBehaviour
{
    public enum TipoObjeto
    {
        Moneda,
        Llave,
        Arma
    }

    [SerializeField] private TipoObjeto tipo;
    [SerializeField] private string id;
    [SerializeField, Min(1)] private int cantidad = 1;

    public bool IntentarRecoger(InventarioJugador2D inventario)
    {
        if (inventario == null)
        {
            Debug.LogError("No se puede recoger el objeto sin un inventario.", this);
            return false;
        }

        if (cantidad <= 0)
        {
            Debug.LogError("La cantidad del objeto recolectable debe ser mayor que cero.", this);
            return false;
        }

        switch (tipo)
        {
            case TipoObjeto.Moneda:
                inventario.AgregarMonedas(cantidad);
                break;
            case TipoObjeto.Llave:
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError("Asigna un ID a esta llave.", this);
                    return false;
                }
                inventario.AgregarLlave(id, cantidad);
                break;
            case TipoObjeto.Arma:
                if (string.IsNullOrWhiteSpace(id))
                {
                    Debug.LogError("Asigna un ID a esta arma.", this);
                    return false;
                }
                inventario.AgregarArma(id, cantidad);
                break;
            default:
                Debug.LogError("El tipo de objeto recolectable no es válido.", this);
                return false;
        }

        Destroy(gameObject);
        return true;
    }
}
