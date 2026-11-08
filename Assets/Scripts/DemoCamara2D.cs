using UnityEngine;

public class DemoCamara2D : MonoBehaviour
{
    [SerializeField] private Transform objetivo;
    [SerializeField, Min(0.01f)] private float suavizado = 0.18f;
    [SerializeField] private Vector2 desplazamiento = new Vector2(1.2f, 0.6f);
    [SerializeField] private Vector2 limiteX = new Vector2(-4f, 33f);

    private Vector3 velocidadSuavizado;

    public void FijarObjetivo(Transform nuevoObjetivo)
    {
        objetivo = nuevoObjetivo;
    }

    private void LateUpdate()
    {
        if (objetivo == null)
            return;

        Vector3 destino = objetivo.position + (Vector3)desplazamiento;
        destino.x = Mathf.Clamp(destino.x, limiteX.x, limiteX.y);
        destino.z = transform.position.z;
        transform.position = Vector3.SmoothDamp(
            transform.position,
            destino,
            ref velocidadSuavizado,
            suavizado
        );
    }
}
