using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public enum DireccionAtaque2D
{
    Horizontal,
    Arriba,
    Abajo
}

/// <summary>
/// Controlador principal del jugador (Sira).
/// Salto unificado (buffer + coyote), habilidades centralizadas,
/// wall jump mejorado, dash, combate direccional y descarga de presión.
/// Compatible con Unity 6 (Rigidbody2D.linearVelocity).
/// </summary>
public class Jugador2D : MonoBehaviour
{
    [Header("Entrada")]
    public InputActionAsset accionesEntrada;

    [Header("Movimiento")]
    [Min(0.1f)] public float velocidad = 7f;
    [Min(0f)] public float aceleracion = 55f;

    [Header("Salto")]
    [Min(0.1f)] public float fuerzaSalto = 13f;
    [Min(0.1f)] public float fuerzaDobleSalto = 10f;
    [Min(0f)] public float coyoteTime = 0.12f;
    [Min(0f)] public float bufferSalto = 0.14f;
    [Range(0.1f, 1f)] public float corteSalto = 0.5f;
    [Min(1f)] public float multiplicadorCaida = 2.2f;

    [Header("Suelo")]
    public Transform comprobadorSuelo;
    [Min(0.01f)] public float radioSuelo = 0.15f;
    public LayerMask capaSuelo;

    [Header("Dash")]
    [Min(0.1f)] public float velocidadDash = 16f;
    [Min(0.05f)] public float duracionDash = 0.16f;
    [Min(0f)] public float enfriamientoDash = 0.5f;

    [Header("Pared / Wall Jump")]
    [SerializeField] private LayerMask capaParedes;
    [SerializeField] private Transform comprobadorPared;
    [Min(0.1f)] public float distanciaPared = 0.55f;
    [Min(0f)] public float velocidadDeslizamientoPared = 2.2f;
    [Min(0.1f)] public float fuerzaSaltoParedX = 9f;
    [Min(0.1f)] public float fuerzaSaltoParedY = 12.5f;
    [Min(0f)] public float tiempoControlTrasWallJump = 0.12f;

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

    [Header("Pogo")]
    [Min(1f)] public float fuerzaRebotePogo = 12.5f;
    public bool recargarDashConPogo = true;

    [Header("Descarga de Presión")]
    [Min(10f)] public float velocidadDescargaPresion = 25f;
    [Min(0.5f)] public float radioImpactoDescarga = 2.4f;
    [Min(1)] public int danioDescarga = 2;
    [Min(0f)] public float fuerzaRetrocesoDescarga = 8f;

    [Header("Salud")]
    [SerializeField] private int saludMaxima = 5;
    private int saludActual;
    public int SaludActual => saludActual;

    [Header("Hechizos")]
    [SerializeField] private GameObject prefabProyectilMagico;
    [SerializeField] private int costeResonanciaHechizo = 33;

    // --- Estado público ---
    public bool EstaDefendiendose { get; private set; }
    public bool EstaEnDash => Time.time < dashTerminaEn;
    public bool TieneDialogoActivo => dialogoActivo;
    public Vector2 DireccionDash => direccionDash;
    public bool MirandoDerecha => mirandoDerecha;
    public DireccionAtaque2D UltimaDireccionAtaque { get; private set; } = DireccionAtaque2D.Horizontal;
    public bool EstaEnDescargaPresion { get; private set; }

    public bool DashDesbloqueado => TieneHabilidad(HabilidadExploracion2D.Dash);
    public bool DobleSaltoDesbloqueado => TieneHabilidad(HabilidadExploracion2D.DobleSalto);
    public bool EscaladaDesbloqueada => TieneHabilidad(HabilidadExploracion2D.EscaladaParedes);
    public bool DescargaPresionDesbloqueada => TieneHabilidad(HabilidadExploracion2D.DescargaPresion);

    // --- Componentes ---
    private Rigidbody2D rb;
    private SpriteRenderer sprite;
    private Animator animator;
    private InventarioJugador2D inventario;
    private HabilidadesExploracion2D habilidades;

    // --- Input (Sprint = Dash, Crouch = Defensa) ---
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
    private float controlMovimientoDisponibleEn;

    private bool mirandoDerecha = true;
    private bool dashSolicitado;
    private bool puedeSaltarCoyote;
    private bool puedeDobleSalto;
    private bool estaEnPared;
    private bool estaDeslizandoseEnPared;
    private bool dialogoActivo;
    private Vector3 posicionOriginalZonaDefensa;

    // =========================================================
    // Ciclo de vida
    // =========================================================

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sprite = GetComponent<SpriteRenderer>();
        animator = GetComponent<Animator>();
        inventario = GetComponent<InventarioJugador2D>();
        InicializarHabilidades();

        if (capaObstaculosRebotables.value == 0)
        {
            int mascara = 0;
            int groundLayer = LayerMask.NameToLayer("Ground");
            int defaultLayer = LayerMask.NameToLayer("Default");
            int enemyLayer = LayerMask.NameToLayer("Enemy");
            if (groundLayer >= 0) mascara |= 1 << groundLayer;
            if (defaultLayer >= 0) mascara |= 1 << defaultLayer;
            if (enemyLayer >= 0) mascara |= 1 << enemyLayer;
            capaObstaculosRebotables = mascara;
        }

        if (rb == null)
        {
            Debug.LogError("El jugador necesita Rigidbody2D.", this);
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

        ConfigurarInput();
    }

    private void Start()
    {
        saludActual = saludMaxima;
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

    private void ConfigurarInput()
    {
        if (accionesEntrada == null)
        {
            Debug.LogError("Asigna el InputActionAsset al jugador.", this);
            enabled = false;
            return;
        }

        InputActionMap mapaFuente = accionesEntrada.FindActionMap("Player", false);
        if (mapaFuente == null)
        {
            Debug.LogError("El InputActionAsset necesita un mapa 'Player'.", this);
            enabled = false;
            return;
        }

        mapaJugador = mapaFuente.Clone();
        movimiento = mapaJugador.FindAction("Move", false);
        salto = mapaJugador.FindAction("Jump", false);
        ataque = mapaJugador.FindAction("Attack", false);
        dash = mapaJugador.FindAction("Sprint", false);   // Dash
        defensa = mapaJugador.FindAction("Crouch", false); // Defensa
        interaccion = mapaJugador.FindAction("Interact", false);

        if (movimiento == null || salto == null || ataque == null || dash == null ||
            defensa == null || interaccion == null)
        {
            Debug.LogError("El mapa Player debe contener Move, Jump, Attack, Sprint, Crouch e Interact.", this);
            enabled = false;
        }
    }

    private void InicializarHabilidades()
    {
        if (habilidades == null)
        {
            habilidades = GetComponent<HabilidadesExploracion2D>();
            if (habilidades == null)
                habilidades = gameObject.AddComponent<HabilidadesExploracion2D>();
        }
    }

    // =========================================================
    // Update / FixedUpdate
    // =========================================================

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

        // Suelo → coyote + recarga doble salto
        if (EstaEnElSuelo() && rb.linearVelocity.y <= 0f)
        {
            ultimoContactoSuelo = Time.time;
            puedeSaltarCoyote = true;
            puedeDobleSalto = true;
        }

        // Único punto de entrada del salto: buffer
        if (salto.WasPressedThisFrame())
            saltoDisponibleHasta = Time.time + bufferSalto;

        // Altura variable
        if (salto.WasReleasedThisFrame() && rb.linearVelocity.y > 0f && !EstaEnDash && !EstaEnDescargaPresion)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, rb.linearVelocity.y * corteSalto);

        // Descarga de presión: ataque + defensa, o solo dash
        if (DescargaPresionDesbloqueada && !EstaEnElSuelo() &&
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

        // Escaleras
        if (escaleras.Count > 0 && Mathf.Abs(entradaMovimiento.y) > 0.01f)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = new Vector2(0f, entradaMovimiento.y * velocidad);
            return;
        }

        // Descarga de presión
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

        // Dash
        if (Time.time < dashTerminaEn)
        {
            rb.gravityScale = 0f;
            rb.linearVelocity = direccionDash * velocidadDash;
            return;
        }

        // Gravedad y caída
        if (!estaDeslizandoseEnPared)
        {
            rb.gravityScale = rb.linearVelocity.y < 0f
                ? gravedadOriginal * multiplicadorCaida
                : gravedadOriginal;
        }

        // Movimiento horizontal (respeta bloqueo post wall-jump)
        if (Time.time >= controlMovimientoDisponibleEn && !estaDeslizandoseEnPared)
        {
            float velocidadObjetivo = entradaMovimiento.x * velocidad;
            float velocidadX = Mathf.MoveTowards(
                rb.linearVelocity.x,
                velocidadObjetivo,
                aceleracion * Time.fixedDeltaTime);
            rb.linearVelocity = new Vector2(velocidadX, rb.linearVelocity.y);
        }

        if (dashSolicitado)
        {
            dashSolicitado = false;
            if (Time.time >= dashDisponibleEn)
                IniciarDash();
        }

        // Único sistema de salto
        IntentarSaltar();
    }

    // =========================================================
    // Salto unificado (buffer + coyote + pared + doble)
    // =========================================================

    private void IntentarSaltar()
    {
        if (Time.time > saltoDisponibleHasta)
            return;
        if (EstaEnDash || EstaEnDescargaPresion)
            return;

        bool enSuelo = EstaEnElSuelo() && rb.linearVelocity.y <= 0f;
        bool coyote = puedeSaltarCoyote && (Time.time - ultimoContactoSuelo <= coyoteTime);
        bool enPared = estaDeslizandoseEnPared && EscaladaDesbloqueada;

        if (enSuelo || coyote)
        {
            EjecutarSalto(fuerzaSalto);
            puedeDobleSalto = true;
            puedeSaltarCoyote = false;
            saltoDisponibleHasta = 0f;
        }
        else if (enPared)
        {
            EjecutarSaltoDePared();
            saltoDisponibleHasta = 0f;
            puedeDobleSalto = true;
        }
        else if (DobleSaltoDesbloqueado && puedeDobleSalto)
        {
            EjecutarSalto(fuerzaDobleSalto);
            puedeDobleSalto = false;
            saltoDisponibleHasta = 0f;
        }
    }

    private void EjecutarSalto(float fuerza)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);
    }

    private void EjecutarSaltoDePared()
    {
        float dir = mirandoDerecha ? -1f : 1f;
        rb.linearVelocity = new Vector2(dir * fuerzaSaltoParedX, fuerzaSaltoParedY);

        mirandoDerecha = dir > 0f;
        if (sprite != null)
            sprite.flipX = !mirandoDerecha;

        controlMovimientoDisponibleEn = Time.time + tiempoControlTrasWallJump;
        puedeSaltarCoyote = false;
    }

    // NO uses OnJump para saltar. Si está cableado en el Inspector, solo alimenta el buffer:
    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            saltoDisponibleHasta = Time.time + bufferSalto;
    }

    // =========================================================
    // Pared
    // =========================================================

    private void ComprobarParedes()
    {
        if (!EscaladaDesbloqueada)
        {
            estaEnPared = false;
            estaDeslizandoseEnPared = false;
            return;
        }

        Vector2 origen = comprobadorPared != null
            ? (Vector2)comprobadorPared.position
            : (Vector2)transform.position;

        Vector2 dirFrente = mirandoDerecha ? Vector2.right : Vector2.left;
        estaEnPared = Physics2D.Raycast(origen, dirFrente, distanciaPared, capaParedes)
                   || Physics2D.Raycast(origen, -dirFrente, distanciaPared, capaParedes);

        estaDeslizandoseEnPared = estaEnPared && !EstaEnElSuelo() && rb.linearVelocity.y < 0.15f;

        if (estaDeslizandoseEnPared)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -velocidadDeslizamientoPared);
    }

    // =========================================================
    // Dash
    // =========================================================

    private void IniciarDash()
    {
        if (!DashDesbloqueado || EstaEnDash)
            return;

        if (Mathf.Abs(entradaMovimiento.x) > 0.1f)
            direccionDash = new Vector2(Mathf.Sign(entradaMovimiento.x), 0f);
        else
            direccionDash = mirandoDerecha ? Vector2.right : Vector2.left;

        direccionDash.Normalize();
        dashTerminaEn = Time.time + duracionDash;
        dashDisponibleEn = dashTerminaEn + enfriamientoDash;
        saltoDisponibleHasta = 0f;
        rb.linearVelocity = direccionDash * velocidadDash;
    }

    // =========================================================
    // Habilidades (solo HabilidadesExploracion2D)
    // =========================================================

    public bool TieneHabilidad(HabilidadExploracion2D habilidad)
    {
        if (habilidades == null)
            InicializarHabilidades();
        return habilidades != null && habilidades.Tiene(habilidad);
    }

    public bool DesbloquearHabilidad(HabilidadExploracion2D habilidad)
    {
        if (habilidades == null)
            InicializarHabilidades();

        bool desbloqueada = habilidades != null && habilidades.Desbloquear(habilidad);
        if (desbloqueada && habilidad == HabilidadExploracion2D.Dash)
            Debug.Log("Habilidad desbloqueada: Dash.", this);
        return desbloqueada;
    }

    public bool DesbloquearDash()
    {
        return DesbloquearHabilidad(HabilidadExploracion2D.Dash);
    }

    // =========================================================
    // Combate
    // =========================================================

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
                if (rebotable != null && (!(rebotable is MonoBehaviour mb) || mb.isActiveAndEnabled))
                {
                    Vector2 puntoContacto = impacto.ClosestPoint(transform.position);
                    if (rebotable.AlRecibirPogo(this, puntoContacto))
                        ejecutoRebotePogo = true;
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
            EjecutarRebotePogo();
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
            default:
                espadaVisual.transform.localPosition = new Vector3(alcance * 0.65f * dirX, 0.05f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, mirandoDerecha ? 0f : 180f);
                break;
        }
    }

    private void ActualizarLadoDeLaEspada()
    {
        if (espadaVisual == null || Time.time >= ataqueTerminaEn)
            return;
        PosicionarEspadaVisual(UltimaDireccionAtaque);
    }

    // =========================================================
    // Hechizos
    // =========================================================

    public void IntentarLanzarHechizo()
    {
        ResonanciaJugador2D resonancia = GetComponent<ResonanciaJugador2D>();
        if (resonancia == null || resonancia.ResonanciaActual < costeResonanciaHechizo)
        {
            Debug.Log("Resonancia insuficiente para lanzar el hechizo.");
            return;
        }

        if (prefabProyectilMagico == null)
            return;

        // Gasta resonancia (API actual: EstablecerResonancia)
        resonancia.EstablecerResonancia(resonancia.ResonanciaActual - costeResonanciaHechizo);

        Vector3 posicionSpawn = transform.position + (mirandoDerecha ? Vector3.right : Vector3.left) * 0.8f;
        GameObject proyectil = Instantiate(prefabProyectilMagico, posicionSpawn, Quaternion.identity);

        if (proyectil.TryGetComponent(out HechizoProyectil2D scriptHechizo))
            scriptHechizo.Inicializar(mirandoDerecha);

        ReceptorMagiaEnemigo2D[] receptores = FindObjectsByType<ReceptorMagiaEnemigo2D>(FindObjectsSortMode.None);
        foreach (ReceptorMagiaEnemigo2D enemigo in receptores)
            enemigo.NotificarMagiaDetectada(transform.position);
    }

    // =========================================================
    // Salud / reaparición / diálogo
    // =========================================================

    public void RestaurarSaludYResonanciaCompleta()
    {
        saludActual = saludMaxima;
        ResonanciaJugador2D resonancia = GetComponent<ResonanciaJugador2D>();
        if (resonancia != null)
            resonancia.GanarResonancia(999);
        Debug.Log("Salud y Resonancia restauradas al 100%.");
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
        controlMovimientoDisponibleEn = 0f;
        dashSolicitado = false;
        puedeSaltarCoyote = false;
        puedeDobleSalto = false;
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

    // =========================================================
    // Interacción / triggers
    // =========================================================

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

    // =========================================================
    // Utilidades
    // =========================================================

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
            animator.SetFloat("Speed", Mathf.Abs(entradaMovimiento.x));
        if (TieneParametro("IsGrounded", AnimatorControllerParameterType.Bool))
            animator.SetBool("IsGrounded", enSuelo);
        if (TieneParametro("VerticalVelocity", AnimatorControllerParameterType.Float))
            animator.SetFloat("VerticalVelocity", rb != null ? rb.linearVelocity.y : 0f);
        if (TieneParametro("IsDashing", AnimatorControllerParameterType.Bool))
            animator.SetBool("IsDashing", EstaEnDash);
        if (TieneParametro("IsWallSliding", AnimatorControllerParameterType.Bool))
            animator.SetBool("IsWallSliding", estaDeslizandoseEnPared);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (comprobadorSuelo != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(comprobadorSuelo.position, radioSuelo);
        }

        Vector3 origenPared = comprobadorPared != null ? comprobadorPared.position : transform.position;
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(origenPared, origenPared + Vector3.right * distanciaPared);
        Gizmos.DrawLine(origenPared, origenPared + Vector3.left * distanciaPared);

        float alcance = Mathf.Max(0.1f, alcanceAtaque);
        float altura = Mathf.Max(0.1f, alturaAtaque);

        Gizmos.color = Color.red;
        Vector3 centroH = transform.position + Vector3.right * (mirandoDerecha ? alcance * 0.5f : -alcance * 0.5f);
        Gizmos.DrawWireCube(centroH, new Vector3(alcance, altura, 0f));

        Gizmos.color = new Color(0.2f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position + Vector3.up * (alcance * 0.5f), new Vector3(altura, alcance, 0f));

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.4f);
        Gizmos.DrawWireCube(transform.position + Vector3.down * (alcance * 0.5f), new Vector3(altura, alcance, 0f));
    }
#endif
}