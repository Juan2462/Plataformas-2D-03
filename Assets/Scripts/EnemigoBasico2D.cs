using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D))]
public class EnemigoBasico2D : MonoBehaviour, IDamageable2D
{
    public enum Estado
    {
        Patrullar,
        Perseguir,
        Atacar,
        Aturdido,
        Muerto
    }

    [Header("Patrulla")]
    [SerializeField, Min(0.1f)] private float velocidad = 2f;
    [SerializeField, Min(0.1f)] private float distanciaPatrulla = 3f;
    [SerializeField] private Transform limiteIzquierdo;
    [SerializeField] private Transform limiteDerecho;

    [Header("Deteccion y ataque")]
    [SerializeField, Min(0.1f)] private float rangoDeteccion = 5f;
    [SerializeField, Min(0.1f)] private float rangoAtaque = 1.1f;
    [SerializeField, Min(0.1f)] private float tiempoEntreAtaques = 1f;
    [SerializeField, Min(1)] private int dano = 1;
    [SerializeField, Min(0f)] private float toleranciaAltura = 1.25f;

    [Header("Vida y retroceso")]
    [SerializeField, Min(1)] private int vidaMaxima = 2;
    [SerializeField, Min(0f)] private float tiempoAturdido = 0.25f;
    [SerializeField, Min(0f)] private float tiempoInvulnerableTrasGolpe = 0.12f;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Animator animator;

    private Rigidbody2D rb;
    private Jugador2D jugador;
    private FeedbackImpacto2D feedbackImpacto;
    private Estado estado;
    private Vector2 posicionInicial;
    private float limiteIzquierdoX;
    private float limiteDerechoX;
    private float direccionPatrulla = 1f;
    private float proximoAtaque;
    private float aturdidoHasta;
    private float puedeRecibirGolpeDesde;
    private int vidaActual;
    private bool mirandoDerecha = true;

    public Estado EstadoActual => estado;
    public int VidaActual => vidaActual;
    public bool EstaVivo => estado != Estado.Muerto;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>();
        if (animator == null)
            animator = GetComponent<Animator>();

        posicionInicial = rb.position;
        limiteIzquierdoX = limiteIzquierdo != null
            ? limiteIzquierdo.position.x
            : posicionInicial.x - distanciaPatrulla;
        limiteDerechoX = limiteDerecho != null
            ? limiteDerecho.position.x
            : posicionInicial.x + distanciaPatrulla;

        if (limiteIzquierdoX > limiteDerechoX)
        {
            float temporal = limiteIzquierdoX;
            limiteIzquierdoX = limiteDerechoX;
            limiteDerechoX = temporal;
        }

        vidaActual = Mathf.Max(1, vidaMaxima);
        estado = Estado.Patrullar;
    }

    private void Update()
    {
        if (estado == Estado.Muerto)
            return;

        if (Time.time < aturdidoHasta)
        {
            CambiarEstado(Estado.Aturdido);
            return;
        }

        if (estado == Estado.Aturdido)
            CambiarEstado(Estado.Patrullar);

        if (jugador == null || !jugador.isActiveAndEnabled)
            jugador = FindFirstObjectByType<Jugador2D>();

        if (jugador == null || !jugador.isActiveAndEnabled || !JugadorADistancia(rangoDeteccion))
        {
            CambiarEstado(Estado.Patrullar);
            return;
        }

        MirarHacia(jugador.transform.position.x - transform.position.x);
        CambiarEstado(JugadorADistancia(rangoAtaque) ? Estado.Atacar : Estado.Perseguir);

        if (estado == Estado.Atacar && Time.time >= proximoAtaque)
            AtacarJugador();
    }

    private void FixedUpdate()
    {
        if (estado == Estado.Muerto || estado == Estado.Aturdido)
            return;

        if (estado == Estado.Patrullar)
            ActualizarDireccionPatrulla();
        else if (estado == Estado.Perseguir && jugador != null)
            direccionPatrulla = Mathf.Sign(jugador.transform.position.x - transform.position.x);
        else if (estado == Estado.Atacar)
            direccionPatrulla = 0f;

        rb.linearVelocity = new Vector2(direccionPatrulla * velocidad, rb.linearVelocity.y);
        if (Mathf.Abs(direccionPatrulla) > 0.01f)
            MirarHacia(direccionPatrulla);

        ActualizarAnimacion();
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        if (estado == Estado.Muerto || cantidad <= 0)
            return false;
        if (Time.time < puedeRecibirGolpeDesde)
            return false;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        puedeRecibirGolpeDesde = Time.time + Mathf.Max(0f, tiempoInvulnerableTrasGolpe);
        ObtenerFeedbackImpacto()?.MostrarImpacto();
        if (vidaActual == 0)
        {
            Morir();
            return true;
        }

        aturdidoHasta = Time.time + tiempoAturdido;
        CambiarEstado(Estado.Aturdido);
        AplicarRetroceso(posicionAtacante, retroceso);
        ActivarTrigger("Hurt", "stun", "damage");
        return true;
    }

    private void AplicarRetroceso(Vector2 posicionAtacante, float fuerzaRetroceso)
    {
        if (rb == null || fuerzaRetroceso <= 0f)
            return;

        float direccion = Mathf.Sign(rb.position.x - posicionAtacante.x);
        if (Mathf.Approximately(direccion, 0f))
            direccion = transform.position.x >= posicionAtacante.x ? 1f : -1f;
        float fuerza = Mathf.Max(0f, fuerzaRetroceso);
        rb.linearVelocity = new Vector2(direccion * fuerza, Mathf.Max(1f, fuerza * 0.35f));
        MirarHacia(direccion);
    }

    private FeedbackImpacto2D ObtenerFeedbackImpacto()
    {
        if (feedbackImpacto == null)
            feedbackImpacto = GetComponent<FeedbackImpacto2D>();
        if (feedbackImpacto == null && GetComponentInChildren<SpriteRenderer>(true) != null)
            feedbackImpacto = gameObject.AddComponent<FeedbackImpacto2D>();

        return feedbackImpacto;
    }

    private void ActualizarDireccionPatrulla()
    {
        if (rb.position.x <= limiteIzquierdoX)
            direccionPatrulla = 1f;
        else if (rb.position.x >= limiteDerechoX)
            direccionPatrulla = -1f;
    }

    private bool JugadorADistancia(float distanciaMaxima)
    {
        Vector2 diferencia = (Vector2)jugador.transform.position - rb.position;
        return Mathf.Abs(diferencia.x) <= distanciaMaxima &&
               Mathf.Abs(diferencia.y) <= toleranciaAltura;
    }

    private void AtacarJugador()
    {
        proximoAtaque = Time.time + tiempoEntreAtaques;
        ActivarTrigger("Attack", "attack");

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            Debug.LogError("El jugador necesita un componente VidaJugador2D para recibir daño.", jugador);
            return;
        }

        vida.RecibirDanio(dano, transform.position);
    }

    private void MirarHacia(float direccion)
    {
        if (Mathf.Abs(direccion) < 0.01f)
            return;

        mirandoDerecha = direccion > 0f;
        if (sprite != null)
            sprite.flipX = !mirandoDerecha;
    }

    private void CambiarEstado(Estado nuevoEstado)
    {
        estado = nuevoEstado;
        ActualizarAnimacion();
    }

    private void ActualizarAnimacion()
    {
        if (animator == null)
            return;

        bool seMueve = estado == Estado.Patrullar || estado == Estado.Perseguir;
        EstablecerBool("move", seMueve && Mathf.Abs(direccionPatrulla) > 0.01f);
        EstablecerBool("idle", estado == Estado.Atacar || estado == Estado.Aturdido);
        EstablecerBool("playerDetected", estado == Estado.Perseguir || estado == Estado.Atacar);
        if (TieneParametro("Speed", AnimatorControllerParameterType.Float))
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        if (TieneParametro("yVelocity", AnimatorControllerParameterType.Float))
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
    }

    private void EstablecerBool(string nombre, bool valor)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Bool))
            animator.SetBool(nombre, valor);
    }

    private void ActivarTrigger(params string[] nombres)
    {
        if (animator == null)
            return;

        foreach (string nombre in nombres)
        {
            if (!TieneParametro(nombre, AnimatorControllerParameterType.Trigger))
                continue;

            animator.SetTrigger(nombre);
            return;
        }
    }

    private bool TieneParametro(string nombre, AnimatorControllerParameterType tipo)
    {
        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre && parametro.type == tipo)
                return true;
        }

        return false;
    }

    private void Morir()
    {
        CambiarEstado(Estado.Muerto);
        rb.linearVelocity = Vector2.zero;
        rb.simulated = false;
        foreach (Collider2D colisionador in GetComponentsInChildren<Collider2D>())
            colisionador.enabled = false;
        Destroy(gameObject, 0.2f);
    }

    private void OnDrawGizmosSelected()
    {
        float izquierda = limiteIzquierdo != null
            ? limiteIzquierdo.position.x
            : transform.position.x - distanciaPatrulla;
        float derecha = limiteDerecho != null
            ? limiteDerecho.position.x
            : transform.position.x + distanciaPatrulla;

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(izquierda, transform.position.y, 0f),
            new Vector3(derecha, transform.position.y, 0f));
        Gizmos.DrawWireSphere(new Vector3(izquierda, transform.position.y, 0f), 0.12f);
        Gizmos.DrawWireSphere(new Vector3(derecha, transform.position.y, 0f), 0.12f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
