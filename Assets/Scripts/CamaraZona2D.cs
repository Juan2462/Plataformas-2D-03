using UnityEngine;

[RequireComponent(typeof(Camera))]
public sealed class CamaraZona2D : MonoBehaviour
{
    [SerializeField] private ZonaMetroidvania2D zona;
    [SerializeField] private Transform objetivo;
    [SerializeField, Min(0.01f)] private float suavizado = 0.16f;
    [SerializeField] private Vector2 desplazamiento = new Vector2(1.3f, 1.1f);

    private Camera camara;
    private Vector3 velocidadSuavizado;

    public void Configurar(ZonaMetroidvania2D zonaObjetivo, Transform nuevoObjetivo)
    {
        zona = zonaObjetivo;
        objetivo = nuevoObjetivo;
    }

    private void Awake()
    {
        camara = GetComponent<Camera>();
    }

    private void LateUpdate()
    {
        if (zona == null || objetivo == null || camara == null)
            return;

        float semialtura = camara.orthographicSize;
        float semiancho = semialtura * camara.aspect;
        Vector2 limitesX = zona.LimitesX;
        Vector2 limitesY = zona.LimitesY;

        float minX = limitesX.x + semiancho;
        float maxX = limitesX.y - semiancho;
        float minY = limitesY.x + semialtura;
        float maxY = limitesY.y - semialtura;
        Vector3 destino = objetivo.position + (Vector3)desplazamiento;
        destino.x = minX > maxX ? (limitesX.x + limitesX.y) * 0.5f :
            Mathf.Clamp(destino.x, minX, maxX);
        destino.y = minY > maxY ? (limitesY.x + limitesY.y) * 0.5f :
            Mathf.Clamp(destino.y, minY, maxY);
        destino.z = transform.position.z;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            destino,
            ref velocidadSuavizado,
            suavizado
        );
    }
}
