using UnityEngine;

public class ProyectilEnemigo2D : MonoBehaviour
{
    [SerializeField, Min(0.1f)] private float velocidad = 6f;
    [SerializeField, Min(0.1f)] private float tiempoDeVida = 4f;

    private Rigidbody2D rb;
    private Vector2 direccion;
    private Transform propietario;
    private int dano;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("El proyectil necesita un componente Rigidbody2D.", this);
            enabled = false;
            return;
        }

        Destroy(gameObject, tiempoDeVida);
    }

    public void Inicializar(Vector2 direccion, int dano, Transform propietario)
    {
        if (direccion.sqrMagnitude < 0.001f || dano <= 0 || propietario == null)
        {
            Debug.LogError("La dirección, el daño y el propietario del proyectil deben ser válidos.", this);
            Destroy(gameObject);
            return;
        }

        this.direccion = direccion.normalized;
        this.dano = dano;
        this.propietario = propietario;
    }

    private void FixedUpdate()
    {
        if (rb != null && direccion.sqrMagnitude > 0f)
            rb.linearVelocity = direccion * velocidad;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (propietario != null && otro.transform.root == propietario.root)
            return;

        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador != null)
        {
            VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
            if (vida == null)
                Debug.LogError("El jugador necesita VidaJugador2D para recibir daño.", jugador);
            else
                vida.RecibirDanio(dano, transform.position);
        }

        Destroy(gameObject);
    }
}
