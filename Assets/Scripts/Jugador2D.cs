using System.Collections.Generic;
using System.Collections; // <--- Necesario para las corrutinas
using UnityEngine;
using UnityEngine.InputSystem;



public enum DireccionAtaque2D
{
    Horizontal,
    Arriba,
    Abajo
}

public class Jugador2D : MonoBehaviour
{
    [Header("Entrada")]
    public InputActionAsset accionesEntrada;

    [Header("Movimiento")]
    [Min(0.1f)] public float velocidad = 7f;
    [Min(0.1f)] public float fuerzaSalto = 13f;
    [Min(0f)] public float aceleracion = 55f;
    [Min(0f)] public float coyoteTime = 0.12f;
    [Min(0f)] public float bufferSalto = 0.14f;
    [Min(1f)] public float multiplicadorCaida = 2.2f;
    [Range(0.1f, 1f)] public float corteSalto = 0.5f;

    [Header("Suelo")]
    public Transform comprobadorSuelo;
    [Min(0.01f)] public float radioSuelo = 0.15f;
    public LayerMask capaSuelo;

    [Header("Dash")]
    [Min(0.1f)] public float velocidadDash = 16f;
    [Min(0.05f)] public float duracionDash = 0.16f;
    [Min(0f)] public float enfriamientoDash = 0.5f;
    [SerializeField] private bool dashDesbloqueado = true;

    [Header("Salto y Doble Salto")]
    [SerializeField] private bool dobleSaltoDesbloqueado = false;
    [SerializeField] private float fuerzaDobleSalto = 10f;
    private bool puedeDobleSalto = false;
    private bool estaEnSuelo; 

    [Header("Escalada y Deslizamiento en Pared")]
    [SerializeField] private bool escaladaDesbloqueada = false;
    [SerializeField] private LayerMask capaParedes;
    [SerializeField] private Transform comprobadorPared;
    [SerializeField] private float velocidadDeslizamientoPared = 2f;
    private bool estaEnPared = false;
    private bool estaDeslizandoseEnPared = false;

    [Header("Combate")]
    public Collider2D zonaDefensa;
    public GameObject espadaVisual;
    public LayerMask capaEnemigos;
    [Min(0.1f)] public float alcanceAtaque = 1.15f;
    [Min(0.1f)] public float alturaAtaque = 0.8f;
    [Min(0.01f)] public float duracionAtaque = 0.18f;
    [Min(0f)] public float tiempoRecuperacionAtaque = 0.35f;
    [Min(0f)] public float fuerzaRetroceso = 5f;
    [Min(0f)] public float impulsoRetrocesoJugador = 1.5f;
    public LayerMask capaObstaculosRebotables;

    [Header("Pogo y Ataques Direccionales")]
    [Min(1f)] public float fuerzaRebotePogo = 12.5f;
    public bool recargarDashConPogo = true;

    [Header("Descarga de Presion (Ground Slam)")]
    [Min(10f)] public float velocidadDescargaPresion = 25f;
    [Min(0.5f)] public float radioImpactoDescarga = 2.4f;
    [Min(1)] public int danioDescarga = 2;
    [Min(0f)] public float fuerzaRetrocesoDescarga = 8f;

    [Header("Salud y Estadísticas")]
    [SerializeField] private int saludMaxima = 5;
    private int saludActual;

    public int SaludActual => saludActual;

    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    private InventarioJugador2D inventario;
    private HabilidadesExploracion2D habilidades;
    private InputActionMap mapaJugador;
    private InputAction movimiento;
    private InputAction salto;
    private InputAction ataque;
    private InputAction dash;
    private InputAction defensa;
    private InputAction interaccion;
    private readonly HashSet<Collider2D> escaleras = new HashSet<Collider2D>();
    private readonly HashSet<Interactuable2D> interactuables = new HashSet<Interactuable2D>();

    private Vector2 entradaMovimiento;
    private Vector2 direccionDash;
    private float gravedadOriginal;
    private float ultimoContactoSuelo = float.NegativeInfinity;
    private float saltoDisponibleHasta;
    private float dashTerminaEn;
    private float dashDisponibleEn;
    private float ataqueTerminaEn;
    private float ataqueDisponibleDesde;
    private bool mirandoDerecha = true;
    private bool dashSolicitado;
    private bool puedeSaltarCoyote;
    private bool dialogoActivo;
    private Vector3 posicionOriginalZonaDefensa;

    public bool EstaDefendiendose { get; private set; }
    public bool EstaEnDash => Time.time < dashTerminaEn;
    public bool TieneDialogoActivo => dialogoActivo;
    public Vector2 DireccionDash => direccionDash;
    public bool MirandoDerecha => mirandoDerecha;
    public bool DashDesbloqueado => TieneHabilidad(HabilidadExploracion2D.Dash);
    public DireccionAtaque2D UltimaDireccionAtaque { get; private set; } = DireccionAtaque2D.Horizontal;
    public bool EstaEnDescargaPresion { get; private set; }
    public bool DescargaPresionDesbloqueada => TieneHabilidad(HabilidadExploracion2D.DescargaPresion);

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            estaEnSuelo = EstaEnElSuelo();

            if (estaEnSuelo)
            {
                EjecutarSalto(fuerzaSalto);
                puedeDobleSalto = true;
            }
            else if (estaDeslizandoseEnPared)
            {
                EjecutarSaltoDePared();
            }
            else if (dobleSaltoDesbloqueado && puedeDobleSalto)
            {
                EjecutarSalto(fuerzaDobleSalto);
                puedeDobleSalto = false;
            }
        }
    }

      private void Start()
    {
        // Inicializa la salud al máximo al arrancar
        saludActual = saludMaxima;
    }

    // Método que llama el altar al descansar
  public void RestaurarSaludYResonanciaCompleta()
{
    saludActual = saludMaxima;

    // Rellena la barra de resonancia (alma) al máximo usando un entero (int)
    ResonanciaJugador2D resonancia = GetComponent<ResonanciaJugador2D>();
    if (resonancia != null)
    {
        resonancia.GanarResonancia(999); // Cambiado de 999f a 999 (int)
    }

    Debug.Log("Salud y Resonancia restauradas al 100%.");
}
    private void EjecutarSalto(float fuerza)
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);
    }

    private void ComprobarParedes()
    {
        if (!escaladaDesbloqueada) return;

        Vector2 direccionPared = mirandoDerecha ? Vector2.right : Vector2.left;
        estaEnPared = Physics2D.Raycast(transform.position, direccionPared, 0.6f, capaParedes);

        if (estaEnPared && !EstaEnElSuelo() && rb.linearVelocity.y < 0f)
        {
            estaDeslizandoseEnPared = true;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -velocidadDeslizamientoPared);
        }
        else
        {
            estaDeslizandoseEnPared = false;
        }
    }

    private void EjecutarSaltoDePared()
    {
        float direccionSaltoX = mirandoDerecha ? -8f : 8f;
        rb.linearVelocity = new Vector2(direccionSaltoX, fuerzaSalto);
        mirandoDerecha = !mirandoDerecha;
        transform.localScale = new Vector3(-transform.localScale.x, transform.localScale.y, transform.localScale.z);
        puedeDobleSalto = true;
    }

    public bool TieneHabilidad(HabilidadExploracion2D habilidad)
    {
        if (habilidades != null)
            return habilidades.Tiene(habilidad);

        return habilidad == HabilidadExploracion2D.Dash && dashDesbloqueado;
    }

    public bool DesbloquearHabilidad(HabilidadExploracion2D habilidad)
    {
        if (habilidades == null)
            InicializarHabilidades();

        bool desbloqueada = habilidades.Desbloquear(habilidad);
        if (desbloqueada && habilidad == HabilidadExploracion2D.Dash)
            dashDesbloqueado = true;

        return desbloqueada;
    }

    public bool DesbloquearDash()
    {
        if (!DesbloquearHabilidad(HabilidadExploracion2D.Dash))
            return false;

        Debug.Log("Habilidad desbloqueada: Dash.", this);
        return true;
    }

    public void RestablecerEstadoParaReaparicion()
    {
        InterfazJuego2D.CerrarDialogoDe(this);
        entradaMovimiento = Vector2.zero;
        direccionDash = Vector2.zero;
        ultimoContactoSuelo = float.NegativeInfinity;
        saltoDisponibleHasta = 0f;
        dashTerminaEn = 0f;
        dashDisponibleEn = 0f;
        ataqueTerminaEn = 0f;
        ataqueDisponibleDesde = 0f;
        dashSolicitado = false;
        puedeSaltarCoyote = false;
        dialogoActivo = false;
        EstaDefendiendose = false;

        EstaEnDescargaPresion = false;
        InterfazJuego2D.MostrarIndicacionInteraccion(null);

        escaleras.Clear();
        interactuables.Clear();

        if (rb != null)
        {
            rb.gravityScale = gravedadOriginal;
            rb.linearVelocity = Vector2.zero;
        }

        if (zonaDefensa != null)
            zonaDefensa.enabled = false;
        if (espadaVisual != null)
            espadaVisual.SetActive(false);
    }

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        inventario = GetComponent<InventarioJugador2D>();
        InicializarHabilidades();

        if (capaObstaculosRebotables.value == 0)
        {
            int groundLayer = LayerMask.NameToLayer("Ground");
            int defaultLayer = LayerMask.NameToLayer("Default");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            int mascara = 0;
            if (groundLayer >= 0) mascara |= (1 << groundLayer);
            if (defaultLayer >= 0) mascara |= (1 << defaultLayer);
            if (enemyLayer >= 0) mascara |= (1 << enemyLayer);
            capaObstaculosRebotables = mascara;
        }

        if (rb == null)
        {
            Debug.LogError("El jugador necesita un componente Rigidbody2D.", this);
            enabled = false;
            return;
        }

        gravedadOriginal = rb.gravityScale;

        if (zonaDefensa != null)
        {
            posicionOriginalZonaDefensa = zonaDefensa.transform.localPosition;
            zonaDefensa.enabled = false;
        }

        if (espadaVisual != null)
            espadaVisual.SetActive(false);

        if (accionesEntrada == null)
        {
            Debug.LogError("Asigna el InputActionAsset del proyecto al jugador.", this);
            enabled = false;
            return;
        }

        InputActionMap mapaJugadorFuente = accionesEntrada.FindActionMap("Player", false);
        if (mapaJugadorFuente == null)
        {
            Debug.LogError("El InputActionAsset necesita un mapa llamado 'Player'.", this);
            enabled = false;
            return;
        }

        mapaJugador = mapaJugadorFuente.Clone();
        movimiento = mapaJugador.FindAction("Move", false);
        salto = mapaJugador.FindAction("Jump", false);
        ataque = mapaJugador.FindAction("Attack", false);
        dash = mapaJugador.FindAction("Sprint", false);
        defensa = mapaJugador.FindAction("Crouch", false);
        interaccion = mapaJugador.FindAction("Interact", false);

        if (movimiento == null || salto == null || ataque == null || dash == null ||
            defensa == null || interaccion == null)
        {
            Debug.LogError("El mapa Player debe contener Move, Jump, Attack, Sprint, Crouch e Interact.", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        mapaJugador?.Enable();
    }

    private void OnDisable()
    {
        InterfazJuego2D.CerrarDialogoDe(this);
        mapaJugador?.Disable();
    }

    private void OnDestroy()
    {
        mapaJugador?.Dispose();
        mapaJugador = null;
    }

    private void Update()
    {
        if (movimiento == null)
            return;

        if (dialogoActivo)
        {
            entradaMovimiento = Vector2.zero;
            EstaDefendiendose = false;
            EstaEnDescargaPresion = false;
            InterfazJuego2D.MostrarIndicacionInteraccion(null);
            return;
        }

        ResonanciaJugador2D compResonancia = GetComponent<ResonanciaJugador2D>();
        if (compResonancia != null && compResonancia.EstaConcentrandose)
        {
            entradaMovimiento = Vector2.zero;
            EstaDefendiendose = false;
            EstaEnDescargaPresion = false;
            return;
        }

        entradaMovimiento = movimiento.ReadValue<Vector2>();
        EstaDefendiendose = defensa != null && defensa.IsPressed();
        ActualizarIndicacionInteraccion();
        ComprobarParedes();

        if (Mathf.Abs(entradaMovimiento.x) > 0.01f)
        {
            mirandoDerecha = entradaMovimiento.x > 0f;
            if (sprite != null)
                sprite.flipX = !mirandoDerecha;
            ActualizarLadoDeLaEspada();
        }

        estaEnSuelo = EstaEnElSuelo();
        if (estaEnSuelo && rb.linearVelocity.y <= 0f)
        {
            ultimoContactoSuelo = Time.time;
            puedeSaltarCoyote = true;
            puedeDobleSalto = true; // Recarga el doble salto al tocar suelo
        }

        if (salto.WasPressedThisFrame())
            saltoDisponibleHasta = Time.time + bufferSalto;

        if (salto.WasReleasedThisFrame() && rb.linearVelocity.y > 0f)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * corteSalto);

        if (DescargaPresionDesbloqueada && !estaEnSuelo && entradaMovimiento.y < -0.5f &&
            (dash.WasPressedThisFrame() || (ataque.WasPressedThisFrame() && defensa != null && defensa.IsPressed())))
        {
            IniciarDescargaPresion();
        }
        else if (DashDesbloqueado && dash.WasPressedThisFrame() && !EstaEnDescargaPresion)
        {
            dashSolicitado = true;
        }

        if (ataque.WasPressedThisFrame())
            Atacar();

        if (interaccion.WasPressedThisFrame())
            InteractuarConElMasCercano();

        if (zonaDefensa != null)
            zonaDefensa.enabled = EstaDefendiendose;

        if (espadaVisual != null)
            espadaVisual.SetActive(Time.time < ataqueTerminaEn);

        ActualizarAnimacion();
    }

    private void FixedUpdate()
    {
        if (rb == null)
            return;

        if (dialogoActivo)
        {
            rb.gravityScale = gravedadOriginal;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        if (escaleras.Count > 0 && Mathf.Abs(entradaMovimiento.y) > 0.01f)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, entradaMovimiento.y * velocidad);
            return;
        }

        if (EstaEnDescargaPresion)
        {
            if (EstaEnElSuelo())
            {
                FinalizarDescargaPresion();
                return;
            }

            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.down * velocidadDescargaPresion;
            return;
        }

        if (Time.time < dashTerminaEn)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direccionDash * velocidadDash;
            return;
        }

        if (estaDeslizandoseEnPared)
            return; // Evita sobreescribir la velocidad si se está deslizando

        rb.gravityScale = gravedadOriginal;

        float velocidadObjetivo = entradaMovimiento.x * velocidad;
        float velocidadX = Mathf.MoveTowards(
            rb.linearVelocity.x,
            velocidadObjetivo,
            aceleracion * Time.fixedDeltaTime
        );
        rb.linearVelocity = new Vector2(velocidadX, rb.linearVelocity.y);

        if (dashSolicitado)
        {
            dashSolicitado = false;
            if (Time.time >= dashDisponibleEn)
                IniciarDash();
        }
        else if (Time.time <= saltoDisponibleHasta &&
                 ((EstaEnElSuelo() && rb.linearVelocity.y <= 0f) ||
                  (puedeSaltarCoyote && Time.time - ultimoContactoSuelo <= coyoteTime)))
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            saltoDisponibleHasta = 0f;
            puedeSaltarCoyote = false;

            if (!salto.IsPressed())
                rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto * corteSalto);
        }

        if (rb.linearVelocity.y < 0f)
            rb.gravityScale = gravedadOriginal * multiplicadorCaida;
    }

    private void IniciarDash()
    {
        direccionDash = new Vector2(entradaMovimiento.x, entradaMovimiento.y);
        if (direccionDash.sqrMagnitude < 0.01f)
            direccionDash = mirandoDerecha ? Vector2.right : Vector2.left;

        direccionDash.Normalize();
        dashTerminaEn = Time.time + duracionDash;
        dashDisponibleEn = dashTerminaEn + enfriamientoDash;
        saltoDisponibleHasta = 0f;
        rb.linearVelocity = direccionDash * velocidadDash;
    }

    private void InicializarHabilidades()
    {
        if (habilidades == null)
        {
            habilidades = GetComponent<HabilidadesExploracion2D>();
            if (habilidades == null)
                habilidades = gameObject.AddComponent<HabilidadesExploracion2D>();
        }

        if (dashDesbloqueado)
            habilidades.Desbloquear(HabilidadExploracion2D.Dash);
    }

    public DireccionAtaque2D DeterminarDireccionAtaque()
    {
        if (entradaMovimiento.y > 0.5f)
            return DireccionAtaque2D.Arriba;

        if (!EstaEnElSuelo() && entradaMovimiento.y < -0.5f)
            return DireccionAtaque2D.Abajo;

        return DireccionAtaque2D.Horizontal;
    }

    private void Atacar()
    {
        AtacarHacia(DeterminarDireccionAtaque());
    }

    public void AtacarHacia(DireccionAtaque2D direccion)
    {
        if (EstaDefendiendose || EstaEnDash || Time.time < ataqueDisponibleDesde)
            return;

        UltimaDireccionAtaque = direccion;
        float alcance = Mathf.Max(0.1f, alcanceAtaque);
        float altura = Mathf.Max(0.1f, alturaAtaque);
        float duracion = Mathf.Max(0.01f, duracionAtaque);
        ataqueTerminaEn = Time.time + duracion;
        ataqueDisponibleDesde = Time.time + Mathf.Max(duracion, tiempoRecuperacionAtaque);

        if (direccion == DireccionAtaque2D.Arriba && TieneParametro("AttackUp", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("AttackUp");
        else if (direccion == DireccionAtaque2D.Abajo && TieneParametro("AttackDown", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("AttackDown");
        else if (TieneParametro("Attack", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("Attack");
        else if (TieneParametro("attack", AnimatorControllerParameterType.Trigger))
            animator.SetTrigger("attack");

        PosicionarEspadaVisual(direccion);

        Vector2 centro;
        Vector2 tamano;
        switch (direccion)
        {
            case DireccionAtaque2D.Arriba:
                centro = (Vector2)transform.position + Vector2.up * (alcance * 0.5f);
                tamano = new Vector2(altura, alcance);
                break;
            case DireccionAtaque2D.Abajo:
                centro = (Vector2)transform.position + Vector2.down * (alcance * 0.5f);
                tamano = new Vector2(altura, alcance);
                break;
            case DireccionAtaque2D.Horizontal:
            default:
                centro = (Vector2)transform.position +
                         Vector2.right * (mirandoDerecha ? alcance * 0.5f : -alcance * 0.5f);
                tamano = new Vector2(alcance, altura);
                break;
        }

        LayerMask mascaraAtaque = capaEnemigos;
        if (capaObstaculosRebotables.value != 0)
            mascaraAtaque |= capaObstaculosRebotables;

        Collider2D[] impactos = Physics2D.OverlapBoxAll(centro, tamano, 0f, mascaraAtaque);
        HashSet<Component> objetivosGolpeados = new HashSet<Component>();
        bool aplicoDano = false;
        bool ejecutoRebotePogo = false;

        foreach (Collider2D impacto in impactos)
        {
            if (impacto == null)
                continue;

            if (direccion == DireccionAtaque2D.Abajo && !ejecutoRebotePogo)
            {
                IRebotablePogo2D rebotable = impacto.GetComponentInParent<IRebotablePogo2D>();
                if (rebotable != null && (!(rebotable is MonoBehaviour mbRebotable) || mbRebotable.isActiveAndEnabled))
                {
                    Vector2 puntoContacto = impacto.ClosestPoint(transform.position);
                    if (rebotable.AlRecibirPogo(this, puntoContacto))
                    {
                        ejecutoRebotePogo = true;
                    }
                }
            }

            MonoBehaviour[] componentes = impacto.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour componente in componentes)
            {
                if (!componente.isActiveAndEnabled ||
                    !(componente is IDamageable2D objetivo) ||
                    !objetivosGolpeados.Add(componente))
                    continue;

                Vector2 posicionAtaque = transform.position;
                if (direccion == DireccionAtaque2D.Abajo)
                    posicionAtaque = (Vector2)transform.position + Vector2.up * 0.5f;
                else if (direccion == DireccionAtaque2D.Arriba)
                    posicionAtaque = (Vector2)transform.position + Vector2.down * 0.5f;

                if (objetivo.RecibirDanio(1, posicionAtaque, fuerzaRetroceso))
                {
                    aplicoDano = true;
                    if (direccion == DireccionAtaque2D.Abajo)
                        ejecutoRebotePogo = true;

                    ResonanciaJugador2D res = GetComponent<ResonanciaJugador2D>();
                    if (res != null)
                        res.GanarResonancia(res.GananciaPorImpacto);
                }
                break;
            }
        }

        if (direccion == DireccionAtaque2D.Abajo && ejecutoRebotePogo)
        {
            EjecutarRebotePogo();
        }
        else if (direccion == DireccionAtaque2D.Horizontal && aplicoDano && rb != null)
        {
            Vector2 direccionRetroceso = mirandoDerecha ? Vector2.left : Vector2.right;
            rb.AddForce(direccionRetroceso * Mathf.Max(0f, impulsoRetrocesoJugador), ForceMode2D.Impulse);
        }
    }

    public void EjecutarRebotePogo(float fuerzaPersonalizada = -1f)
    {
        if (rb == null)
            return;

        float fuerza = fuerzaPersonalizada > 0f ? fuerzaPersonalizada : fuerzaRebotePogo;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);

        if (recargarDashConPogo)
            dashDisponibleEn = Time.time;

        saltoDisponibleHasta = 0f;
        puedeSaltarCoyote = false;
    }

    public bool IniciarDescargaPresion()
    {
        if (!DescargaPresionDesbloqueada || EstaEnElSuelo() || EstaEnDash || EstaEnDescargaPresion || dialogoActivo)
            return false;

        EstaEnDescargaPresion = true;
        saltoDisponibleHasta = 0f;
        puedeSaltarCoyote = false;
        dashTerminaEn = 0f;

        if (rb != null)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = Vector2.down * velocidadDescargaPresion;
        }

        return true;
    }

    public void FinalizarDescargaPresion()
    {
        if (!EstaEnDescargaPresion)
            return;

        EstaEnDescargaPresion = false;
        if (rb != null)
        {
            rb.gravityScale = gravedadOriginal;
            rb.linearVelocity = Vector2.zero;
        }

        ImpactarDescargaPresion();
    }

    private void ImpactarDescargaPresion()
    {
        Vector2 centro = transform.position;
        Collider2D[] impactos = Physics2D.OverlapCircleAll(centro, radioImpactoDescarga);
        HashSet<Component> afectados = new HashSet<Component>();

        foreach (Collider2D col in impactos)
        {
            if (col == null)
                continue;

            SelloSalFragil2D sello = col.GetComponentInParent<SelloSalFragil2D>();
            if (sello != null && afectados.Add(sello))
            {
                sello.AlRecibirDescargaPresion(this);
                continue;
            }

            MonoBehaviour[] componentes = col.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour comp in componentes)
            {
                if (comp is IDamageable2D objetivo && afectados.Add(comp))
                {
                    objetivo.RecibirDanio(danioDescarga, centro, fuerzaRetrocesoDescarga);
                    break;
                }
            }
        }
    }

    private void PosicionarEspadaVisual(DireccionAtaque2D direccion)
    {
        if (espadaVisual == null)
            return;

        float alcance = Mathf.Max(0.1f, alcanceAtaque);
        float dirX = mirandoDerecha ? 1f : -1f;

        switch (direccion)
        {
            case DireccionAtaque2D.Arriba:
                espadaVisual.transform.localPosition = new Vector3(0f, alcance * 0.65f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case DireccionAtaque2D.Abajo:
                espadaVisual.transform.localPosition = new Vector3(0f, -alcance * 0.65f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                break;
            case DireccionAtaque2D.Horizontal:
            default:
                espadaVisual.transform.localPosition = new Vector3(alcance * 0.65f * dirX, 0.05f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, mirandoDerecha ? 0f : 180f);
                break;
        }
    }

    public bool EstaEnElSuelo()
    {
        if (comprobadorSuelo == null)
            return false;

        return Physics2D.OverlapCircle(comprobadorSuelo.position, radioSuelo, capaSuelo) != null;
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

    private void ActualizarAnimacion()
    {
        if (animator == null)
            return;

        bool enSuelo = EstaEnElSuelo();
        if (TieneParametro("Speed", AnimatorControllerParameterType.Float))
            animator.SetFloat("Speed", Mathf.Abs(rb.linearVelocity.x));
        if (TieneParametro("Grounded", AnimatorControllerParameterType.Bool))
            animator.SetBool("Grounded", enSuelo);
        if (TieneParametro("YVelocity", AnimatorControllerParameterType.Float))
            animator.SetFloat("YVelocity", rb.linearVelocity.y);
        if (TieneParametro("xVelocity", AnimatorControllerParameterType.Float))
            animator.SetFloat("xVelocity", Mathf.Abs(rb.linearVelocity.x));
        if (TieneParametro("yVelocity", AnimatorControllerParameterType.Float))
            animator.SetFloat("yVelocity", rb.linearVelocity.y);
        if (TieneParametro("move", AnimatorControllerParameterType.Bool))
            animator.SetBool("move", Mathf.Abs(entradaMovimiento.x) > 0.01f);
        if (TieneParametro("idle", AnimatorControllerParameterType.Bool))
            animator.SetBool("idle", enSuelo && Mathf.Abs(entradaMovimiento.x) < 0.01f);
        if (TieneParametro("inAir", AnimatorControllerParameterType.Bool))
            animator.SetBool("inAir", !enSuelo);
    }

    private void ActualizarLadoDeLaEspada()
    {
        float direccion = mirandoDerecha ? 1f : -1f;

        if (zonaDefensa != null)
        {
            zonaDefensa.transform.localPosition = new Vector3(
                Mathf.Abs(posicionOriginalZonaDefensa.x) * direccion,
                posicionOriginalZonaDefensa.y,
                posicionOriginalZonaDefensa.z
            );
        }

        if (espadaVisual != null && Time.time >= ataqueTerminaEn)
        {
            float alcance = Mathf.Max(0.1f, alcanceAtaque);
            espadaVisual.transform.localPosition = new Vector3(alcance * 0.65f * direccion, 0.05f, 0f);
            espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, mirandoDerecha ? 0f : 180f);
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (otro.CompareTag("Ladder"))
            escaleras.Add(otro);

        ObjetoRecolectable2D objeto = otro.GetComponentInParent<ObjetoRecolectable2D>();
        if (objeto != null && inventario != null)
            objeto.IntentarRecoger(inventario);

        Interactuable2D interactuable = otro.GetComponentInParent<Interactuable2D>();
        if (interactuable != null)
            interactuables.Add(interactuable);

        if (EstaDefendiendose && otro.CompareTag("EnemyAttack"))
            Destroy(otro.gameObject);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        if (otro.CompareTag("Ladder"))
            escaleras.Remove(otro);

        Interactuable2D interactuable = otro.GetComponentInParent<Interactuable2D>();
        if (interactuable != null)
            interactuables.Remove(interactuable);
    }

    private void InteractuarConElMasCercano()
    {
        if (inventario == null)
            return;

        ObtenerInteractuableCercano()?.Interactuar(inventario);
    }

    public void EstablecerDialogoActivo(bool activo)
    {
        dialogoActivo = activo;
        entradaMovimiento = Vector2.zero;
        dashSolicitado = false;
        EstaDefendiendose = false;
        EstaEnDescargaPresion = false;

        if (activo)
        {
            saltoDisponibleHasta = 0f;
            dashTerminaEn = 0f;
            ataqueTerminaEn = 0f;
        }

        if (rb != null)
        {
            rb.gravityScale = gravedadOriginal;
            rb.linearVelocity = Vector2.zero;
        }
        if (zonaDefensa != null)
            zonaDefensa.enabled = false;
        if (espadaVisual != null)
            espadaVisual.SetActive(false);
    }

    private void ActualizarIndicacionInteraccion()
    {
        Interactuable2D cercano = ObtenerInteractuableCercano();
        InterfazJuego2D.MostrarIndicacionInteraccion(
            cercano != null ? cercano.TextoIndicacion : null);
    }

    private Interactuable2D ObtenerInteractuableCercano()
    {
        Interactuable2D masCercano = null;
        float distanciaMasCercana = float.PositiveInfinity;

        foreach (Interactuable2D interactuable in interactuables)
        {
            if (interactuable == null || !interactuable.EstaInteractuableDisponible)
                continue;

            float distancia = (interactuable.transform.position - transform.position).sqrMagnitude;
            if (distancia < distanciaMasCercana)
            {
                distanciaMasCercana = distancia;
                masCercano = interactuable;
            }
        }

        return masCercano;
    }
    
    private bool estaLento = false;

public void AplicarEstadoLentitud(float factorReduccion, float duracion)
{
    if (!estaLento)
    {
        StartCoroutine(RutinaLentitud(factorReduccion, duracion));
    }
}

private IEnumerator RutinaLentitud(float factor, float duracion)
{
    estaLento = true;
    // Modifica temporalmente la velocidad en Jugador2D
     velocidad *= factor; 
    
    yield return new WaitForSeconds(duracion);
    
    // Restaura la velocidad original
    velocidad /= factor;
    estaLento = false;
}
    private void OnDrawGizmosSelected()
    {
        if (comprobadorSuelo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(comprobadorSuelo.position, radioSuelo);
        }

        float alcance = Mathf.Max(0.1f, alcanceAtaque);
        float altura = Mathf.Max(0.1f, alturaAtaque);

        Gizmos.color = Color.cyan;
        Vector3 centroH = transform.position + Vector3.right * (mirandoDerecha ? alcance * 0.5f : -alcance * 0.5f);
        Gizmos.DrawWireCube(centroH, new Vector3(alcance, altura, 0f));

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Vector3 centroArriba = transform.position + Vector3.up * (alcance * 0.5f);
        Gizmos.DrawWireCube(centroArriba, new Vector3(altura, alcance, 0f));

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Vector3 centroAbajo = transform.position + Vector3.down * (alcance * 0.5f);
        Gizmos.DrawWireCube(centroAbajo, new Vector3(altura, alcance, 0f));
    }
    [Header("Hechizos / Magia")]
    [SerializeField] private GameObject prefabProyectilMagico;
    [SerializeField] private int costeResonanciaHechizo = 33;

    public void IntentarLanzarHechizo()
{
    ResonanciaJugador2D resonancia = GetComponent<ResonanciaJugador2D>();
    
    if (resonancia != null && resonancia.ResonanciaActual >= costeResonanciaHechizo)
    {
        if (prefabProyectilMagico != null)
        {
            Vector3 posicionSpawn = transform.position + (mirandoDerecha ? Vector3.right : Vector3.left) * 0.8f;
            GameObject proyectil = Instantiate(prefabProyectilMagico, posicionSpawn, Quaternion.identity);
            
            if (proyectil.TryGetComponent<HechizoProyectil2D>(out var scriptHechizo))
            {
                scriptHechizo.Inicializar(mirandoDerecha);
            }

            // --- NUEVO: Notificar a los enemigos cercanos sobre el uso de magia ---
            ReceptorMagiaEnemigo2D[] enemigosEnEscena = FindObjectsByType<ReceptorMagiaEnemigo2D>(FindObjectsSortMode.None);
            foreach (var enemigo in enemigosEnEscena)
            {
                enemigo.NotificarMagiaDetectada(transform.position);
            }
        }
    }
    else
    {
        Debug.Log("Resonancia insuficiente para lanzar el hechizo.");
    }
}
///Cuando Sira use su magia en los pasillos del Acueducto de la Niebla, 
/// los enemigos que estén dentro del radio de detección dejarán de patrullar
///  pasivamente.

///Podrás programar que se abalancen sobre Sira de inmediato, 
/// obligándote a usar el doble salto o las corrientes de viento para 
/// esquivar sus embestidas rápidas.
}