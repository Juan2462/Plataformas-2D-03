using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(Collider2D), typeof(SpriteRenderer))]
public abstract class EnemigoPrototipo2D : MonoBehaviour, IDamageable2D
{
    public enum Estado
    {
        Patrullar,
        Detectando,
        AnticipandoAtaque,
        Atacando,
        Recuperandose,
        RecibiendoDanio,
        Muerto
    }

    [Header("Deteccion y combate")]
    [SerializeField, Min(0.5f)] protected float rangoDeteccion = 7f;
    [SerializeField, Min(0.5f)] protected float rangoAtaque = 1.25f;
    [SerializeField, Min(0.1f)] protected float velocidad = 2f;
    [SerializeField, Min(1)] protected int dano = 1;
    [SerializeField, Min(1)] protected int vidaMaxima = 2;
    [SerializeField, Min(0.1f)] protected float anticipacionAtaque = 0.5f;
    [SerializeField, Min(0.05f)] protected float duracionAtaque = 0.2f;
    [SerializeField, Min(0.1f)] protected float recuperacionAtaque = 0.8f;
    [SerializeField, Min(0.1f)] protected float tiempoEntreAtaques = 1.5f;
    [SerializeField, Min(0.05f)] protected float tiempoAturdido = 0.3f;
    [SerializeField, Min(0f)] protected float tiempoInvulnerableTrasGolpe = 0.15f;
    [SerializeField, Min(0f)] protected float fuerzaRetroceso = 4f;
    [SerializeField, Min(0f)] protected float toleranciaAlturaAtaque = 1.2f;

    [Header("Ataque a distancia")]
    [SerializeField, Min(0.5f)] protected float velocidadProyectil = 4f;
    [SerializeField, Min(0.2f)] protected float vidaProyectil = 3f;
    [SerializeField] protected Transform puntoDisparo;

    protected Rigidbody2D rb;
    protected Jugador2D jugador;
    protected SpriteRenderer visual;
    protected Estado estado;
    protected Vector2 posicionInicial;
    protected float direccionMirada = 1f;
    private float estadoTerminaEn;
    private float ataqueDisponibleDesde;
    private float invulnerableHasta;
    private int vidaActual;
    private Vector3 escalaInicial;
    private Color colorInicial;

    public Estado EstadoActual => estado;
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;
    public bool EstaVivo => estado != Estado.Muerto;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        posicionInicial = rb.position;
        escalaInicial = transform.localScale;
        colorInicial = visual.color;
        vidaActual = Mathf.Max(1, vidaMaxima);
        estado = Estado.Patrullar;
    }

    protected virtual void Update()
    {
        if (estado == Estado.Muerto)
            return;

        if (estado == Estado.RecibiendoDanio)
        {
            if (Time.time < estadoTerminaEn)
                return;
            CambiarEstado(jugador != null && jugador.isActiveAndEnabled
                ? Estado.Detectando
                : Estado.Patrullar);
        }

        if (jugador == null || !jugador.isActiveAndEnabled)
            jugador = FindFirstObjectByType<Jugador2D>();

        if (estado == Estado.AnticipandoAtaque)
        {
            if (Time.time >= estadoTerminaEn)
            {
                EjecutarAtaque();
                CambiarEstado(Estado.Atacando, duracionAtaque);
            }
            return;
        }

        if (estado == Estado.Atacando)
        {
            if (Time.time >= estadoTerminaEn)
                CambiarEstado(Estado.Recuperandose, recuperacionAtaque);
            return;
        }

        if (estado == Estado.Recuperandose)
        {
            if (Time.time < estadoTerminaEn)
                return;
            CambiarEstado(Estado.Detectando);
        }

        if (jugador == null || !jugador.isActiveAndEnabled)
        {
            CambiarEstado(Estado.Patrullar);
            return;
        }

        Vector2 diferencia = (Vector2)jugador.transform.position - rb.position;
        float distancia = diferencia.magnitude;
        if (distancia > rangoDeteccion)
        {
            CambiarEstado(Estado.Patrullar);
            return;
        }

        if (Mathf.Abs(diferencia.x) > 0.05f)
            direccionMirada = Mathf.Sign(diferencia.x);

        if (distancia <= rangoAtaque && Time.time >= ataqueDisponibleDesde)
        {
            ataqueDisponibleDesde = Time.time + Mathf.Max(
                anticipacionAtaque + duracionAtaque + recuperacionAtaque,
                tiempoEntreAtaques
            );
            CambiarEstado(Estado.AnticipandoAtaque, anticipacionAtaque);
        }
        else
        {
            CambiarEstado(Estado.Detectando);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (estado != Estado.Muerto && estado != Estado.RecibiendoDanio)
            ActualizarMovimiento();

        if (visual != null)
            visual.flipX = direccionMirada < 0f;
    }

    protected abstract void ActualizarMovimiento();
    protected abstract void EjecutarAtaque();

    protected virtual void AlCambiarEstado(Estado estadoAnterior, Estado estadoNuevo)
    {
    }

    public virtual bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        if (estado == Estado.Muerto || cantidad <= 0 || Time.time < invulnerableHasta)
            return false;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        invulnerableHasta = Time.time + Mathf.Max(0f, tiempoInvulnerableTrasGolpe);
        if (Application.isPlaying)
            ObtenerFeedbackImpacto()?.MostrarImpacto();

        if (vidaActual == 0)
        {
            CambiarEstado(Estado.Muerto);
            rb.linearVelocity = Vector2.zero;
            foreach (Collider2D colisionador in GetComponents<Collider2D>())
                colisionador.enabled = false;
            return true;
        }

        float direccion = Mathf.Sign(rb.position.x - posicionAtacante.x);
        if (Mathf.Approximately(direccion, 0f))
            direccion = transform.position.x >= posicionAtacante.x ? 1f : -1f;
        float fuerza = Mathf.Max(0f, retroceso);
        if (fuerza > 0f)
            rb.linearVelocity = new Vector2(direccion * fuerza, Mathf.Max(1f, fuerza * 0.35f));

        CambiarEstado(Estado.RecibiendoDanio, tiempoAturdido);
        return true;
    }

    protected bool JugadorEnAlcanceHorizontal(float alcance, float toleranciaAltura)
    {
        if (jugador == null || !jugador.isActiveAndEnabled)
            return false;

        Vector2 diferencia = (Vector2)jugador.transform.position - rb.position;
        return Mathf.Abs(diferencia.x) <= alcance &&
               Mathf.Abs(diferencia.y) <= toleranciaAltura;
    }

    protected void DanarJugadorEnAlcance(float alcance, float toleranciaAltura)
    {
        if (!JugadorEnAlcanceHorizontal(alcance, toleranciaAltura))
            return;

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            Debug.LogError("El jugador necesita VidaJugador2D para recibir daño.", jugador);
            return;
        }

        vida.RecibirDanio(dano, transform.position);
    }

    protected void LanzarProyectil(
        Vector2 direccion,
        Color color,
        bool soloHorizontal = false,
        float alcanceMaximo = 0f)
    {
        if (direccion.sqrMagnitude < 0.001f)
            return;

        Vector2 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        GameObject proyectil = new GameObject("Proyectil provisional de enemigo");
        proyectil.transform.position = origen;
        proyectil.transform.localScale = Vector3.one * 0.32f;

        SpriteRenderer spriteProyectil = proyectil.AddComponent<SpriteRenderer>();
        spriteProyectil.sprite = visual.sprite;
        spriteProyectil.color = color;
        spriteProyectil.sortingOrder = visual.sortingOrder + 1;

        Rigidbody2D cuerpoProyectil = proyectil.AddComponent<Rigidbody2D>();
        cuerpoProyectil.bodyType = RigidbodyType2D.Kinematic;
        cuerpoProyectil.gravityScale = 0f;
        cuerpoProyectil.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D colisionador = proyectil.AddComponent<CircleCollider2D>();
        colisionador.isTrigger = true;
        colisionador.radius = 0.4f;

        ProyectilPrototipoEnemigo2D controlador = proyectil.AddComponent<ProyectilPrototipoEnemigo2D>();
        controlador.Inicializar(
            direccion.normalized,
            velocidadProyectil,
            dano,
            alcanceMaximo > 0f
                ? alcanceMaximo / velocidadProyectil
                : vidaProyectil,
            transform,
            soloHorizontal
        );
    }

    private void CambiarEstado(Estado nuevoEstado, float duracion = 0f)
    {
        if (estado == nuevoEstado)
            return;

        Estado anterior = estado;
        estado = nuevoEstado;
        estadoTerminaEn = Time.time + Mathf.Max(0f, duracion);
        ActualizarVisualEstado();
        AlCambiarEstado(anterior, nuevoEstado);
    }

    private void ActualizarVisualEstado()
    {
        if (visual == null)
            return;

        visual.color = estado switch
        {
            Estado.Detectando => new Color(0.55f, 0.95f, 1f, 1f),
            Estado.AnticipandoAtaque => new Color(1f, 0.62f, 0.18f, 1f),
            Estado.Atacando => new Color(1f, 0.9f, 0.65f, 1f),
            Estado.RecibiendoDanio => new Color(1f, 0.4f, 0.38f, 1f),
            Estado.Muerto => new Color(0.35f, 0.35f, 0.35f, 0.7f),
            _ => colorInicial
        };

        float escala = estado == Estado.AnticipandoAtaque ? 1.15f : 1f;
        transform.localScale = escalaInicial * escala;
    }

    private FeedbackImpacto2D ObtenerFeedbackImpacto()
    {
        FeedbackImpacto2D feedback = GetComponent<FeedbackImpacto2D>();
        if (feedback == null)
            feedback = gameObject.AddComponent<FeedbackImpacto2D>();
        return feedback;
    }

    protected virtual void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0.25f, 0.85f, 1f, 0.8f);
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = new Color(1f, 0.35f, 0.2f, 0.9f);
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
