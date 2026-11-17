using UnityEngine;

public class HechizoProyectil2D : MonoBehaviour
{
    [SerializeField] private float velocidadProyectil = 14f;
    [SerializeField] private int danioHechizo = 2;
    private Rigidbody2D rb;
    private bool direccionDerecha = true;

    public void Inicializar(bool mirandoDerecha)
    {
        direccionDerecha = mirandoDerecha;
        if (!mirandoDerecha)
        {
            Vector3 escala = transform.localScale;
            escala.x *= -1f;
            transform.localScale = escala;
        }
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        float dirX = direccionDerecha ? 1f : -1f;
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(dirX * velocidadProyectil, 0f);
        }

        Destroy(gameObject, 3f);
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.TryGetComponent<IDamageable2D>(out var objetivo))
        {
            objetivo.RecibirDanio(danioHechizo, transform.position, 4f);
            Destroy(gameObject);
        }
        else if (otro.gameObject.layer == LayerMask.NameToLayer("Ground"))
        {
            Destroy(gameObject);
        }
    }
}