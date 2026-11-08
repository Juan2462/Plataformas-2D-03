using UnityEngine;

public class EnemigoIA2D : MonoBehaviour, IDamageable2D
{
    public enum Comportamiento
    {
        Patrullar,
        Perseguir
    }

    public enum TipoAtaque
    {
        CuerpoACuerpo,
        Proyectil
    }

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

    private Rigidbody2D rb;
    private Jugador2D jugador;
    private FeedbackImpacto2D feedbackImpacto;
    private Vector2 posicionInicial;
    private float direccionPatrulla = 1f;
    private float puedeAtacarDesde;
    private float puedeRecibirDanioDesde;
    private float aturdidoHasta;
    private int vidaActual;
    private bool ataquePendiente;
    private bool muerto;

    public bool EstaVivo => !muerto;
    public int VidaActual => vidaActual;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb == null)
        {
            Debug.LogError("El enemigo necesita un componente Rigidbody2D.", this);
            enabled = false;
            return;
        }

        if (sprite == null)
            sprite = GetComponent<SpriteRenderer>();

        if (animator == null)
            animator = GetComponent<Animator>();

        posicionInicial = rb.position;
        vidaActual = Mathf.Max(1, vidaMaxima);
    }

    private void Update()
    {
        if (muerto)
            return;

        if (Time.time < aturdidoHasta)
        {
            MirarAlJugador();
            return;
        }

        if (jugador == null)
            jugador = FindFirstObjectByType<Jugador2D>();

        if (jugador == null || !jugador.isActiveAndEnabled)
        {
            Patrullar();
            return;
        }

        float distanciaJugador = Vector2.Distance(rb.position, jugador.transform.position);
        bool jugadorDetectado = distanciaJugador <= rangoDeteccion;

        if (jugadorDetectado && distanciaJugador <= rangoAtaque)
        {
            MoverHorizontal(0f);
            MirarHacia(jugador.transform.position.x - transform.position.x);
            ActivarParametroAnimador("idle", true);
            ActivarParametroAnimador("move", false);
            Atacar();
            return;
        }

        if (jugadorDetectado)
        {
            ActivarParametroAnimador("playerDetected", true);
            PerseguirJugador();
            return;
        }

        ActivarParametroAnimador("playerDetected", false);
        if (comportamiento == Comportamiento.Patrullar)
            Patrullar();
        else
            VolverAlPuntoInicial();

        bool seMueve = Mathf.Abs(rb.linearVelocity.x) > 0.1f;
        ActivarParametroAnimador("move", seMueve);
        ActivarParametroAnimador("idle", !seMueve);
        EstablecerParametroFloat("yVelocity", rb.linearVelocity.y);
        EstablecerParametroFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
    }

    private void Patrullar()
    {
        float limiteIzquierdo = posicionInicial.x - distanciaPatrulla;
        float limiteDerecho = posicionInicial.x + distanciaPatrulla;

        if (rb.position.x <= limiteIzquierdo)
            direccionPatrulla = 1f;
        else if (rb.position.x >= limiteDerecho)
            direccionPatrulla = -1f;

        MoverHorizontal(direccionPatrulla);
    }

    private void PerseguirJugador()
    {
        float diferenciaX = jugador.transform.position.x - transform.position.x;
        MoverHorizontal(Mathf.Sign(diferenciaX));
        MirarHacia(diferenciaX);
    }

    private void VolverAlPuntoInicial()
    {
        float diferenciaX = posicionInicial.x - rb.position.x;

        if (Mathf.Abs(diferenciaX) <= 0.1f)
            MoverHorizontal(0f);
        else
        {
            MoverHorizontal(Mathf.Sign(diferenciaX));
            MirarHacia(diferenciaX);
        }
    }

    private void MoverHorizontal(float direccion)
    {
        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);

        if (Mathf.Abs(direccion) > 0.01f)
            MirarHacia(direccion);
    }

    private void MirarHacia(float direccion)
    {
        if (sprite != null && Mathf.Abs(direccion) > 0.01f)
            sprite.flipX = direccion < 0f;
    }

    private void Atacar()
    {
        if (Time.time < puedeAtacarDesde)
            return;

        puedeAtacarDesde = Time.time + Mathf.Max(0.1f, tiempoEntreAtaques);
        ataquePendiente = true;

        if (!string.IsNullOrWhiteSpace(parametroAtaque) &&
            TieneParametro(parametroAtaque, AnimatorControllerParameterType.Trigger))
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

    public void TriggerAttack()
    {
        EjecutarAtaquePendiente();
    }

    public void FinishAttack()
    {
        EjecutarAtaquePendiente();
    }

    private void EjecutarAtaquePendiente()
    {
        if (!ataquePendiente || muerto)
            return;

        ataquePendiente = false;
        if (tipoAtaque == TipoAtaque.CuerpoACuerpo)
        {
            if (jugador != null &&
                jugador.isActiveAndEnabled &&
                Vector2.Distance(rb.position, jugador.transform.position) <= rangoAtaque)
                AplicarDanoAlJugador();
            return;
        }

        DispararProyectil();
    }

    private void AplicarDanoAlJugador()
    {
        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            Debug.LogError("El jugador necesita VidaJugador2D para recibir daño.", jugador);
            return;
        }

        vida.RecibirDanio(dano, transform.position);
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        if (muerto || cantidad <= 0)
            return false;
        if (Time.time < puedeRecibirDanioDesde)
            return false;

        vidaActual = Mathf.Max(0, vidaActual - cantidad);
        puedeRecibirDanioDesde = Time.time + Mathf.Max(0f, tiempoInvulnerableTrasGolpe);
        aturdidoHasta = Time.time + tiempoAturdido;
        AplicarRetroceso(posicionAtacante, retroceso);
        ObtenerFeedbackImpacto()?.MostrarImpacto();

        if (TieneParametro("Hurt", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("Hurt");
        else if (TieneParametro("stun", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("stun");
        else if (TieneParametro("damage", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("damage");

        if (vidaActual == 0)
        {
            muerto = true;
            rb.linearVelocity = Vector2.zero;
            foreach (Collider2D colisionador in GetComponents<Collider2D>())
                colisionador.enabled = false;
            Destroy(gameObject, 0.2f);
        }

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
    }

    private FeedbackImpacto2D ObtenerFeedbackImpacto()
    {
        if (feedbackImpacto == null)
            feedbackImpacto = GetComponent<FeedbackImpacto2D>();
        if (feedbackImpacto == null && GetComponentInChildren<SpriteRenderer>(true) != null)
            feedbackImpacto = gameObject.AddComponent<FeedbackImpacto2D>();

        return feedbackImpacto;
    }

    private void MirarAlJugador()
    {
        if (jugador != null)
            MirarHacia(jugador.transform.position.x - transform.position.x);
    }

    private void ActivarParametroAnimador(string nombre, bool valor)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Bool))
            animator.SetBool(nombre, valor);
    }

    private void EstablecerParametroFloat(string nombre, float valor)
    {
        if (TieneParametro(nombre, AnimatorControllerParameterType.Float))
            animator.SetFloat(nombre, valor);
    }

    private bool TieneParametro(string nombre, AnimatorControllerParameterType tipo)
    {
        if (animator == null)
            return false;

        foreach (AnimatorControllerParameter parametro in animator.parameters)
        {
            if (parametro.name == nombre && parametro.type == tipo)
                return true;
        }

        return false;
    }

    private void DispararProyectil()
    {
        if (prefabProyectil == null)
        {
            Debug.LogError("Asigna un prefab de ProyectilEnemigo2D al enemigo.", this);
            return;
        }

        Vector3 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 direccion = (jugador.transform.position - origen).normalized;
        GameObject proyectil = Instantiate(prefabProyectil, origen, Quaternion.identity);
        ProyectilEnemigo2D comportamientoProyectil = proyectil.GetComponent<ProyectilEnemigo2D>();

        if (comportamientoProyectil == null)
        {
            Debug.LogError("El prefab del proyectil necesita el componente ProyectilEnemigo2D.", proyectil);
            Destroy(proyectil);
            return;
        }

        comportamientoProyectil.Inicializar(direccion, dano, transform);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, rangoDeteccion);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, rangoAtaque);
    }
}
