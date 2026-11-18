using UnityEngine;

[RequireComponent(typeof(MaquinaEstadosEnemigo2D))]
public class EnemigoIA2D : MonoBehaviour, IDamageable2D
{
    public enum Comportamiento { Patrullar, Perseguir }
    public enum TipoAtaque { CuerpoACuerpo, Proyectil }

    [Header("IA")]
    [SerializeField] private Comportamiento comportamiento = Comportamiento.Patrullar;
    [SerializeField, Min(0.1f)] private float velocidad = 2f;
    [SerializeField, Min(0f)] private float distanciaPatrulla = 3f;
    [SerializeField, Min(0.1f)] private float rangoDeteccion = 6f;

    [Header("Ataque")]
    [SerializeField] private TipoAtaque tipoAtaque = TipoAtaque.CuerpoACuerpo;
    [SerializeField, Min(0.1f)] private float rangoAtaque = 1.2f;
    [SerializeField, Min(0.1f)] private float tiempoEntreAtaques = 1.2f;
    [SerializeField, Min(1)] private int dano = 1;
    [SerializeField, Min(1)] private int vidaMaxima = 2;
    [SerializeField, Min(0.05f)] private float tiempoAturdido = 0.2f;
    [SerializeField, Min(0f)] private float tiempoInvulnerableTrasGolpe = 0.12f;
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform puntoDisparo;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroAtaque = "Attack";

    public MaquinaEstadosEnemigo2D FSM { get; private set; }
    private Rigidbody2D rb;
    private Jugador2D jugador;
    private FeedbackImpacto2D feedbackImpacto;
    private Vector2 posicionInicial;
    private float direccionPatrulla = 1f;
    private float puedeAtacarDesde;
    private float puedeRecibirDanioDesde;
    private int vidaActual;
    private bool ataquePendiente;
    private bool muerto;

    private EstadoPatrullaIA estadoPatrulla;
    private EstadoPerseguirIA estadoPerseguir;
    private EstadoAtacarIA estadoAtacar;

    public bool EstaVivo => !muerto;
    public int VidaActual => vidaActual;

    private void Awake()
    {
        FSM = GetComponent<MaquinaEstadosEnemigo2D>();
        if (FSM == null) FSM = gameObject.AddComponent<MaquinaEstadosEnemigo2D>();

        rb = GetComponent<Rigidbody2D>();
        if (rb == null) { enabled = false; return; }

        if (sprite == null) sprite = GetComponent<SpriteRenderer>();
        if (animator == null) animator = GetComponent<Animator>();

        posicionInicial = rb.position;
        vidaActual = Mathf.Max(1, vidaMaxima);

        estadoPatrulla = new EstadoPatrullaIA(this);
        estadoPerseguir = new EstadoPerseguirIA(this);
        estadoAtacar = new EstadoAtacarIA(this);
    }

    private void Start()
    {
        FSM.Inicializar(estadoPatrulla);
    }

    private void Update()
    {
        if (muerto) return;

        if (jugador == null)
            jugador = FindFirstObjectByType<Jugador2D>();

        bool seMueve = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        ActivarParametroAnimador("move", seMueve);
        ActivarParametroAnimador("idle", !seMueve);
        EstablecerParametroFloat("yVelocity", rb.linearVelocity.y);
        EstablecerParametroFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    public void AlDetectarMagiaCercana()
    {
        if (!muerto && jugador != null && jugador.isActiveAndEnabled)
        {
            MirarAlJugador();
            FSM.CambiarEstado(estadoPerseguir);
        }
    }

    private void MoverHorizontal(float direccion)
    {
        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);
        if (Mathf.Abs(direccion) > 0.01f) MirarHacia(direccion);
    }

    private void MirarHacia(float direccion)
    {
        if (sprite != null && Mathf.Abs(direccion) > 0.01f)
            sprite.flipX = direccion < 0f;
    }

    private void Atacar()
    {
        if (Time.time < puedeAtacarDesde) return;

        puedeAtacarDesde = Time.time + Mathf.Max(0.1f, tiempoEntreAtaques);
        ataquePendiente = true;

        if (!string.IsNullOrWhiteSpace(parametroAtaque) && TieneParametro(parametroAtaque, AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger(parametroAtaque);
            return;
        }
        else if (TieneParametro("Attack", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Attack");
            return;
        }

        EjecutarAtaquePendiente();
    }

    public void TriggerAttack() => EjecutarAtaquePendiente();
    public void FinishAttack() => EjecutarAtaquePendiente();

    private void EjecutarAtaquePendiente()
    {
        if (!ataquePendiente || muerto) return;

        ataquePendiente = false;
        if (tipoAtaque == TipoAtaque.CuerpoACuerpo)
        {
            if (jugador != null && jugador.isActiveAndEnabled && Vector2.Distance(rb.position, jugador.transform.position) <= rangoAtaque)
            {
                VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
                if (vida != null) vida.RecibirDanio(dano, transform.position);
            }
            return;
        }

        DispararProyectil();
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        if (muerto || cantidad <= 0 || Time.time < puedeRecibirDanioDesde)
            return false;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        puedeRecibirDanioDesde = Time.time + Mathf.Max(0f, tiempoInvulnerableTrasGolpe);
        AplicarRetroceso(posicionAtacante, retroceso);
        ObtenerFeedbackImpacto()?.MostrarImpacto();

        if (TieneParametro("Hurt", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("Hurt");
        else if (TieneParametro("stun", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("stun");
        else if (TieneParametro("damage", AnimatorControllerParameterType.Trigger)) animator.SetTrigger("damage");

        if (vidaActual == 0)
        {
            muerto = true;
            FSM.CambiarEstado(new EstadoMuerteEnemigo2D(gameObject, rb, GetComponent<Collider2D>(), 0.25f));
            return true;
        }

        FSM.CambiarEstado(new EstadoAturdido2D(FSM, rb, estadoPerseguir, tiempoAturdido));
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
    }

    private FeedbackImpacto2D ObtenerFeedbackImpacto()
    {
        if (feedbackImpacto == null) feedbackImpacto = GetComponent<FeedbackImpacto2D>();
        if (feedbackImpacto == null && GetComponentInChildren<SpriteRenderer>(true) != null)
            feedbackImpacto = gameObject.AddComponent<FeedbackImpacto2D>();
        return feedbackImpacto;
    }

    private void MirarAlJugador()
    {
        if (jugador != null) MirarHacia(jugador.transform.position.x - transform.position.x);
    }

    private void ActivarParametroAnimador(string nombre, bool valor)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Bool)) animator.SetBool(nombre, valor);
    }

    private void EstablecerParametroFloat(string nombre, float valor)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Float)) animator.SetFloat(nombre, valor);
    }

    private bool TieneParametro(string nombre, AnimatorControllerParameterType tipo)
    {
        if (animator == null) return false;
        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre && parametro.type == tipo) return true;
        }
        return false;
    }

    private void DispararProyectil()
    {
        if (prefabProyectil == null || jugador == null) return;

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 direccion = (jugador.transform.position - origen).normalized;
        GameObject proyectil = Instantiate(prefabProyectil, origen, Quaternion.identity);
        ProyectilEnemigo2D comportamientoProyectil = proyectil.GetComponent<ProyectilEnemigo2D>();

        if (comportamientoProyectil == null)
        {
            Destroy(proyectil);
            return;
        }

        comportamientoProyectil.Inicializar(direccion, dano, transform);
    }

    private class EstadoPatrullaIA : IEstadoEnemigo2D
    {
        private readonly EnemigoIA2D e;
        public EstadoPatrullaIA(EnemigoIA2D e) => this.e = e;

        public void Entrar() => e.ActivarParametroAnimador("playerDetected", false);
        public void Actualizar()
        {
            if (e.jugador != null && e.jugador.isActiveAndEnabled)
            {
                float dist = Vector2.Distance(e.rb.position, e.jugador.transform.position);
                if (dist <= e.rangoDeteccion)
                {
                    e.FSM.CambiarEstado(e.estadoPerseguir);
                    return;
                }
            }

            if (e.comportamiento == Comportamiento.Patrullar)
            {
                float limiteIzq = e.posicionInicial.x - e.distanciaPatrulla;
                float limiteDer = e.posicionInicial.x + e.distanciaPatrulla;
                if (e.rb.position.x <= limiteIzq) e.direccionPatrulla = 1f;
                else if (e.rb.position.x >= limiteDer) e.direccionPatrulla = -1f;
                e.MoverHorizontal(e.direccionPatrulla);
            }
            else
            {
                float diffX = e.posicionInicial.x - e.rb.position.x;
                if (Mathf.Abs(diffX) <= 0.1f) e.MoverHorizontal(0f);
                else { e.MoverHorizontal(Mathf.Sign(diffX)); e.MirarHacia(diffX); }
            }
        }
        public void ActualizarFisicas() { }
        public void Salir() { }
    }

    private class EstadoPerseguirIA : IEstadoEnemigo2D
    {
        private readonly EnemigoIA2D e;
        public EstadoPerseguirIA(EnemigoIA2D e) => this.e = e;

        public void Entrar() => e.ActivarParametroAnimador("playerDetected", true);
        public void Actualizar()
        {
            if (e.jugador == null || !e.jugador.isActiveAndEnabled)
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }

            float dist = Vector2.Distance(e.rb.position, e.jugador.transform.position);
            if (dist > e.rangoDeteccion * 1.25f)
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }

            if (dist <= e.rangoAtaque)
            {
                e.FSM.CambiarEstado(e.estadoAtacar);
                return;
            }

            float diffX = e.jugador.transform.position.x - e.transform.position.x;
            e.MoverHorizontal(Mathf.Sign(diffX));
            e.MirarHacia(diffX);
        }
        public void ActualizarFisicas() { }
        public void Salir() { }
    }

    private class EstadoAtacarIA : IEstadoEnemigo2D
    {
        private readonly EnemigoIA2D e;
        public EstadoAtacarIA(EnemigoIA2D e) => this.e = e;

        public void Entrar() => e.MoverHorizontal(0f);
        public void Actualizar()
        {
            if (e.jugador == null || !e.jugador.isActiveAndEnabled)
            {
                e.FSM.CambiarEstado(e.estadoPatrulla);
                return;
            }

            float dist = Vector2.Distance(e.rb.position, e.jugador.transform.position);
            if (dist > e.rangoAtaque)
            {
                e.FSM.CambiarEstado(e.estadoPerseguir);
                return;
            }

            e.MirarHacia(e.jugador.transform.position.x - e.transform.position.x);
            e.Atacar();
        }
        public void ActualizarFisicas() { }
        public void Salir() { }
    }
}
