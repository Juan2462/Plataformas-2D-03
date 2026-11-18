using UnityEngine;

[RequireComponent(typeof(MaquinaEstadosEnemigo2D), typeof(Rigidbody2D), typeof(Collider2D))]
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

    public MaquinaEstadosEnemigo2D FSM { get; private set; }
    private Rigidbody2D rb;
    private Jugador2D jugador;
    private FeedbackImpacto2D feedbackImpacto;
    private Estado estado;
    private Vector2 posicionInicial;
    private float limiteIzquierdoX;
    private float limiteDerechoX;
    private float direccionPatrulla = 1f;
    private float proximoAtaque;
    private float puedeRecibirGolpeDesde;
    private int vidaActual;
    private bool mirandoDerecha = true;

    private EstadoPatrullaBasico estadoPatrulla;
    private EstadoPerseguirBasico estadoPerseguir;
    private EstadoAtacarBasico estadoAtacar;

    public Estado EstadoActual => estado;
    public int VidaActual => vidaActual;
    public bool EstaVivo => estado != Estado.Muerto;

    private void Awake()
    {
        FSM = GetComponent<MaquinaEstadosEnemigo2D>();
        if (FSM == null) FSM = gameObject.AddComponent<MaquinaEstadosEnemigo2D>();

        rb = GetComponent<Rigidbody2D>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        posicionInicial = rb.position;
        limiteIzquierdoX = limiteIzquierdo != null ? limiteIzquierdo.position.x : posicionInicial.x - distanciaPatrulla;
        limiteDerechoX = limiteDerecho != null ? limiteDerecho.position.x : posicionInicial.x + distanciaPatrulla;

        if (limiteIzquierdoX > limiteDerechoX)
        {
            float temporal = limiteIzquierdoX;
            limiteIzquierdoX = limiteDerechoX;
            limiteDerechoX = temporal;
        }

        vidaActual = Mathf.Max(1, vidaMaxima);

        estadoPatrulla = new EstadoPatrullaBasico(this);
        estadoPerseguir = new EstadoPerseguirBasico(this);
        estadoAtacar = new EstadoAtacarBasico(this);
    }

    private void Start()
    {
        CambiarEstado(Estado.Patrullar);
        FSM.Inicializar(estadoPatrulla);
    }

    private void Update()
    {
        if (estado == Estado.Muerto) return;

        if (jugador == null || !jugador.isActiveAndEnabled)
            jugador = FindFirstObjectByType<Jugador2D>();
    }

    public void AlDetectarMagiaCercana()
    {
        if (estado == Estado.Patrullar && jugador != null && jugador.isActiveAndEnabled)
        {
            MirarHacia(jugador.transform.position.x - transform.position.x);
            FSM.CambiarEstado(estadoPerseguir);
        }
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        if (estado == Estado.Muerto || cantidad <= 0 || Time.time < puedeRecibirGolpeDesde)
            return false;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        puedeRecibirGolpeDesde = Time.time + Mathf.Max(0f, tiempoInvulnerableTrasGolpe);
        ObtenerFeedbackImpacto()?.MostrarImpacto();

        if (vidaActual == 0)
        {
            CambiarEstado(Estado.Muerto);
            rb.simulated = false;
            FSM.CambiarEstado(new EstadoMuerteEnemigo2D(gameObject, rb, GetComponent<Collider2D>(), 0.25f));
            return true;
        }

        CambiarEstado(Estado.Aturdido);
        AplicarRetroceso(posicionAtacante, retroceso);
        ActivarTrigger("Hurt", "stun", "damage");
        FSM.CambiarEstado(new EstadoAturdido2D(FSM, rb, estadoPatrulla, tiempoAturdido));
        return true;
    }

    private void AplicarRetroceso(Vector2 posicionAtacante, float fuerzaRetroceso)
    {
        if (rb == null || fuerzaRetroceso <= 0f) return;

        float direccion = Mathf.Sign(rb.position.x - posicionAtacante.x);
        if (Mathf.Approximately(direccion, 0f))
            direccion = transform.position.x >= posicionAtacante.x ? 1f : -1f;
        float fuerza = Mathf.Max(0f, fuerzaRetroceso);
        rb.linearVelocity = new Vector2(direccion * fuerza, Mathf.Max(1f, fuerza * 0.35f));
        MirarHacia(direccion);
    }

    private FeedbackImpacto2D ObtenerFeedbackImpacto()
    {
        if (feedbackImpacto == null) feedbackImpacto = GetComponent<FeedbackImpacto2D>();
        if (feedbackImpacto == null && GetComponentInChildren<SpriteRenderer>(true) != null)
            feedbackImpacto = gameObject.AddComponent<FeedbackImpacto2D>();
        return feedbackImpacto;
    }

    private bool JugadorADistancia(float distanciaMaxima)
    {
        if (jugador == null || !jugador.isActiveAndEnabled) return false;
        Vector2 diferencia = (Vector2)jugador.transform.position - rb.position;
        return Mathf.Abs(diferencia.x) <= distanciaMaxima && Mathf.Abs(diferencia.y) <= toleranciaAltura;
    }

    private void AtacarJugador()
    {
        proximoAtaque = Time.time + tiempoEntreAtaques;
        ActivarTrigger("Attack", "attack");

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida != null)
            vida.RecibirDanio(dano, transform.position);
    }

    private void MirarHacia(float direccion)
    {
        if (Mathf.Abs(direccion) < 0.01f) return;
        mirandoDerecha = direccion > 0f;
        if (sprite != null) sprite.flipX = !mirandoDerecha;
    }

    private void CambiarEstado(Estado nuevoEstado)
    {
        estado = nuevoEstado;
        ActualizarAnimacion();
    }

    private void ActualizarAnimacion()
    {
        if (animator == null) return;

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
        if (animator == null) return;
        foreach (string nombre in nombres)
        {
            if (!TieneParametro(nombre, AnimatorControllerParameterType.Trigger)) continue;
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

    private class EstadoPatrullaBasico : IEstadoEnemigo2D
    {
        private readonly EnemigoBasico2D e;
        public EstadoPatrullaBasico(EnemigoBasico2D e) => this.e = e;

        public void Entrar() => e.CambiarEstado(Estado.Patrullar);
        public void Actualizar()
        {
            if (e.JugadorADistancia(e.rangoDeteccion))
                e.FSM.CambiarEstado(e.estadoPerseguir);
        }
        public void ActualizarFisicas()
        {
            if (e.rb.position.x <= e.limiteIzquierdoX) e.direccionPatrulla = 1f;
            else if (e.rb.position.x >= e.limiteDerechoX) e.direccionPatrulla = -1f;

            e.rb.linearVelocity = new Vector2(e.direccionPatrulla * e.velocidad, e.rb.linearVelocity.y);
            e.MirarHacia(e.direccionPatrulla);
            e.ActualizarAnimacion();
        }
        public void Salir() { }
    }

    private class EstadoPerseguirBasico : IEstadoEnemigo2D
    {
        private readonly EnemigoBasico2D e;
        public EstadoPerseguirBasico(EnemigoBasico2D e) => this.e = e;

        public void Entrar() => e.CambiarEstado(Estado.Perseguir);
        public void Actualizar()
        {
            if (!e.JugadorADistancia(e.rangoDeteccion))
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }
            if (e.JugadorADistancia(e.rangoAtaque))
                e.FSM.CambiarEstado(e.estadoAtacar);
        }
        public void ActualizarFisicas()
        {
            if (e.jugador == null) return;
            e.direccionPatrulla = Mathf.Sign(e.jugador.transform.position.x - e.transform.position.x);
            e.rb.linearVelocity = new Vector2(e.direccionPatrulla * e.velocidad, e.rb.linearVelocity.y);
            e.MirarHacia(e.direccionPatrulla);
            e.ActualizarAnimacion();
        }
        public void Salir() { }
    }

    private class EstadoAtacarBasico : IEstadoEnemigo2D
    {
        private readonly EnemigoBasico2D e;
        public EstadoAtacarBasico(EnemigoBasico2D e) => this.e = e;

        public void Entrar()
        {
            e.direccionPatrulla = 0f;
            e.rb.linearVelocity = new Vector2(0f, e.rb.linearVelocity.y);
            e.CambiarEstado(Estado.Atacar);
        }
        public void Actualizar()
        {
            if (!e.JugadorADistancia(e.rangoAtaque))
            {
                e.FSM.CambiarEstado(e.JugadorADistancia(e.rangoDeteccion) ? e.estadoPerseguir : e.estadoPatrulla);
                return;
            }
            e.MirarHacia(e.jugador.transform.position.x - e.transform.position.x);
            if (Time.time >= e.proximoAtaque)
                e.AtacarJugador();
        }
        public void ActualizarFisicas() => e.ActualizarAnimacion();
        public void Salir() { }
    }
}
