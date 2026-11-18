using UnityEngine;

[RequireComponent(typeof(MaquinaEstadosEnemigo2D))]
[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(SpriteRenderer))]



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

    public MaquinaEstadosEnemigo2D FSM { get; private set; }
    protected Rigidbody2D rb;
    protected Jugador2D jugador;
    protected SpriteRenderer visual;
    protected Estado estado;
    protected Vector2 posicionInicial;
    protected float direccionMirada = 1f;

    private float ataqueDisponibleDesde;
    private float invulnerableHasta;
    private int vidaActual;
    private Vector3 escalaInicial;
    private Color colorInicial;

    private EstadoPatrullaPrototipo estadoPatrulla;
    private EstadoDeteccionPrototipo estadoDeteccion;
    private EstadoCicloAtaque2D estadoCombate;

    public Estado EstadoActual => estado;
    public int VidaActual => vidaActual;
    public int VidaMaxima => vidaMaxima;
    public bool EstaVivo => estado != Estado.Muerto;

    protected virtual void Awake()
    {
        FSM = GetComponent<MaquinaEstadosEnemigo2D>();
        if (FSM == null) FSM = gameObject.AddComponent<MaquinaEstadosEnemigo2D>();

        rb = GetComponent<Rigidbody2D>();
        visual = GetComponent<SpriteRenderer>();
        posicionInicial = rb.position;
        escalaInicial = transform.localScale;
        colorInicial = visual.color;
        vidaActual = Mathf.Max(1, vidaMaxima);

        estadoPatrulla = new EstadoPatrullaPrototipo(this);
        estadoDeteccion = new EstadoDeteccionPrototipo(this);
        estadoCombate = new EstadoCicloAtaque2D(
            FSM,
            rb,
            anticipacionAtaque,
            duracionAtaque,
            recuperacionAtaque,
            alIniciarAnticipacion: () => SincronizarEstadoVisual(Estado.AnticipandoAtaque),
            alEjecutarGolpe: () =>
            {
                SincronizarEstadoVisual(Estado.Atacando);
                EjecutarAtaque();
            },
            alActualizarAtaque: () =>
            {
                if (estadoCombate.SubFaseActual == 2 && estado != Estado.Recuperandose)
                    SincronizarEstadoVisual(Estado.Recuperandose);
            },
            alTerminarCiclo: () => SincronizarEstadoVisual(Estado.Detectando),
            estadoSiguiente: estadoDeteccion
        );
    }

    protected virtual void Start()
    {
        SincronizarEstadoVisual(Estado.Patrullar);
        FSM.Inicializar(estadoPatrulla);
    }

    protected virtual void Update()
    {
        if (estado == Estado.Muerto) return;

        if (jugador == null || !jugador.isActiveAndEnabled)
            jugador = FindFirstObjectByType<Jugador2D>();
    }

    protected virtual void FixedUpdate()
    {
        if (visual != null)
            visual.flipX = direccionMirada < 0f;
    }

    protected abstract void ActualizarMovimiento();
    protected abstract void EjecutarAtaque();

    protected virtual void AlCambiarEstado(Estado estadoAnterior, Estado estadoNuevo) { }

    public void AlDetectarMagiaCercana()
    {
        if (estado == Estado.Patrullar && jugador != null)
        {
            direccionMirada = Mathf.Sign(jugador.transform.position.x - transform.position.x);
            FSM.CambiarEstado(estadoDeteccion);
        }
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
            SincronizarEstadoVisual(Estado.Muerto);
            FSM.CambiarEstado(new EstadoMuerteEnemigo2D(gameObject, rb, GetComponent<Collider2D>(), 0.35f));
            return true;
        }

        float direccion = Mathf.Sign(rb.position.x - posicionAtacante.x);
        if (Mathf.Approximately(direccion, 0f))
            direccion = transform.position.x >= posicionAtacante.x ? 1f : -1f;
        float fuerza = Mathf.Max(0f, retroceso);
        if (fuerza > 0f)
            rb.linearVelocity = new Vector2(direccion * fuerza, Mathf.Max(1f, fuerza * 0.35f));

        SincronizarEstadoVisual(Estado.RecibiendoDanio);
        FSM.CambiarEstado(new EstadoAturdido2D(FSM, rb, estadoDeteccion, tiempoAturdido));
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
        if (vida != null)
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
            alcanceMaximo > 0f ? alcanceMaximo / velocidadProyectil : vidaProyectil,
            transform,
            soloHorizontal
        );
    }

    private void SincronizarEstadoVisual(Estado nuevoEstado)
    {
        if (estado == nuevoEstado)
            return;

        Estado anterior = estado;
        estado = nuevoEstado;
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

    private class EstadoPatrullaPrototipo : IEstadoEnemigo2D
    {
        private readonly EnemigoPrototipo2D e;
        public EstadoPatrullaPrototipo(EnemigoPrototipo2D e) => this.e = e;

        public void Entrar() => e.SincronizarEstadoVisual(Estado.Patrullar);
        public void Actualizar()
        {
            if (e.jugador != null && e.jugador.isActiveAndEnabled)
            {
                float dist = Vector2.Distance(e.rb.position, e.jugador.transform.position);
                if (dist <= e.rangoDeteccion)
                    e.FSM.CambiarEstado(e.estadoDeteccion);
            }
        }
        public void ActualizarFisicas() => e.ActualizarMovimiento();
        public void Salir() { }
    }

    private class EstadoDeteccionPrototipo : IEstadoEnemigo2D
    {
        private readonly EnemigoPrototipo2D e;
        public EstadoDeteccionPrototipo(EnemigoPrototipo2D e) => this.e = e;

        public void Entrar() => e.SincronizarEstadoVisual(Estado.Detectando);
        public void Actualizar()
        {
            if (e.jugador == null || !e.jugador.isActiveAndEnabled)
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }

            Vector2 diff = (Vector2)e.jugador.transform.position - e.rb.position;
            float dist = diff.magnitude;
            if (dist > e.rangoDeteccion)
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }

            if (Mathf.Abs(diff.x) > 0.05f)
                e.direccionMirada = Mathf.Sign(diff.x);

            if (dist <= e.rangoAtaque && Time.time >= e.ataqueDisponibleDesde)
            {
                e.ataqueDisponibleDesde = Time.time + Mathf.Max(
                    e.anticipacionAtaque + e.duracionAtaque + e.recuperacionAtaque,
                    e.tiempoEntreAtaques
                );
                e.FSM.CambiarEstado(e.estadoCombate);
            }
        }
        public void ActualizarFisicas() => e.ActualizarMovimiento();
        public void Salir() { }
    }
}
