using UnityEngine;

[DisallowMultipleComponent]
public sealed class ZonaMetroidvania2D : MonoBehaviour
{
    [SerializeField] private string idZona = "canal-campanas";
    [SerializeField] private string nombreZona = "Canal de las Campanas";
    [SerializeField] private string siguienteZona = "Galeria de Valvulas";
    [SerializeField] private Vector2 limitesX = new Vector2(0f, 84f);
    [SerializeField] private Vector2 limitesY = new Vector2(-6f, 10f);
    [SerializeField] private Transform puntoEntrada;
    [SerializeField] private Transform conexionSiguiente;

    public string IdZona => idZona;
    public string NombreZona => nombreZona;
    public string SiguienteZona => siguienteZona;
    public Transform PuntoEntrada => puntoEntrada;
    public Transform ConexionSiguiente => conexionSiguiente;

    public Vector2 LimitesX => limitesX;
    public Vector2 LimitesY => limitesY;

    public bool Contiene(Vector2 posicion)
    {
        return posicion.x >= limitesX.x && posicion.x <= limitesX.y &&
               posicion.y >= limitesY.x && posicion.y <= limitesY.y;
    }

    private void OnValidate()
    {
        limitesX = new Vector2(Mathf.Min(limitesX.x, limitesX.y), Mathf.Max(limitesX.x, limitesX.y));
        limitesY = new Vector2(Mathf.Min(limitesY.x, limitesY.y), Mathf.Max(limitesY.x, limitesY.y));
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.2f, 0.9f, 0.8f, 0.75f);
        Vector3 centro = new Vector3(
            (limitesX.x + limitesX.y) * 0.5f,
            (limitesY.x + limitesY.y) * 0.5f,
            0f
        );
        Gizmos.DrawWireCube(centro, new Vector3(
            limitesX.y - limitesX.x,
            limitesY.y - limitesY.x,
            0.1f
        ));
    }
}
