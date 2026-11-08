using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class ProyectilPrototipoEnemigo2D : MonoBehaviour
{
    private Rigidbody2D rb;
    private Vector2 direccion;
    private Transform propietario;
    private float velocidad;
    private int dano;
    private bool inicializado;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (Application.isPlaying)
            Destroy(gameObject, 4f);
    }

    public void Inicializar(
        Vector2 direccion,
        float velocidad,
        int dano,
        float duracion,
        Transform propietario,
        bool soloHorizontal)
    {
        if (direccion.sqrMagnitude < 0.001f || velocidad <= 0f || dano <= 0 || propietario == null)
        {
            Debug.LogError("El proyectil requiere direccion, velocidad, dano y propietario validos.", this);
            Destroy(gameObject);
            return;
        }

        this.direccion = soloHorizontal
            ? new Vector2(Mathf.Sign(direccion.x), 0f)
            : direccion.normalized;
        this.velocidad = velocidad;
        this.dano = dano;
        this.propietario = propietario;
        inicializado = true;
        if (Application.isPlaying)
            Destroy(gameObject, Mathf.Max(0.2f, duracion));
    }

    private void FixedUpdate()
    {
        if (inicializado && rb != null)
            rb.linearVelocity = direccion * velocidad;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (!inicializado || (propietario != null && otro.transform.root == propietario.root))
            return;

        Jugador2D objetivo = otro.GetComponentInParent<Jugador2D>();
        if (objetivo == null)
        {
            if (((1 << otro.gameObject.layer) & LayerMask.GetMask("Ground")) != 0)
                Destroy(gameObject);
            return;
        }

        VidaJugador2D vidaJugador = objetivo.GetComponent<VidaJugador2D>();
        if (vidaJugador == null)
            Debug.LogError("El jugador necesita VidaJugador2D para recibir dano.", objetivo);
        else
            vidaJugador.RecibirDanio(dano, transform.position);

        Destroy(gameObject);
    }
}
