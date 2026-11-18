using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Combate con combos y finishers.
/// - Cadena de golpes con buffer y timeout
/// - Finisher en el último paso (daño, knockback, hitstop y efectos extra)
/// - Finishers opcionales: gastar resonancia, impulso, trigger dedicado
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(Rigidbody2D))]
public class CombateJugador2D : MonoBehaviour
{
    public enum FaseAtaque
    {
        Idle,
        Startup,
        Active,
        Recovery
    }

    [System.Serializable]
    public class PasoCombo
    {
        [Min(0.1f)] public float multiplicadorDano = 1f;
        [Min(0f)] public float multiplicadorRetroceso = 1f;
        [Min(0f)] public float hitstopExtra = 0f;
        [Tooltip("Impulso horizontal del jugador al iniciar el golpe.")]
        public float impulsoJugador = 0f;
        [Tooltip("Trigger de Animator (vacío = Attack / AttackUp / AttackDown).")]
        public string triggerAnimacion = "";
        [Tooltip("Marca este paso como finisher (último golpe de la cadena).")]
        public bool esFinisher = false;
        [Tooltip("Resonancia que gasta al ejecutar este finisher (0 = gratis).")]
        [Min(0)] public int costeResonancia = 0;
        [Tooltip("Resonancia extra que gana al conectar el finisher.")]
        [Min(0)] public int bonusResonanciaAlConectar = 0;
        [Tooltip("Multiplicador extra de hitstop solo si es finisher.")]
        [Min(1f)] public float multiplicadorHitstopFinisher = 1.5f;
        [Tooltip("Alcance extra del hitbox en este paso.")]
        [Min(0f)] public float alcanceExtra = 0f;
    }

    [Header("Referencias")]
    [SerializeField] private Jugador2D jugador;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Collider2D zonaDefensa;
    [SerializeField] private GameObject espadaVisual;

    [Header("Ataque base")]
    [SerializeField] private LayerMask capaEnemigos;
    [SerializeField] private LayerMask capaObstaculosRebotables;
    [Min(0.1f)] public float alcanceAtaque = 1.15f;
    [Min(0.1f)] public float alturaAtaque = 0.8f;
    [Min(0f)] public float fuerzaRetroceso = 5f;
    [Min(0f)] public float impulsoRetrocesoJugador = 1.5f;
    [Min(1)] public int danoAtaque = 1;

    [Header("Timing base (segundos)")]
    [Min(0f)] public float startupAtaque = 0.05f;
    [Min(0.01f)] public float activeAtaque = 0.12f;
    [Min(0f)] public float recoveryAtaque = 0.22f;

    [Header("Timing del finisher (opcional, -1 = usar base)")]
    public float startupFinisher = 0.08f;
    public float activeFinisher = 0.16f;
    public float recoveryFinisher = 0.32f;

    [Header("Combos")]
    public PasoCombo[] comboHorizontal = new PasoCombo[]
    {
        new PasoCombo { multiplicadorDano = 1f, multiplicadorRetroceso = 1f, impulsoJugador = 0.8f },
        new PasoCombo { multiplicadorDano = 1.1f, multiplicadorRetroceso = 1.15f, impulsoJugador = 1.2f, hitstopExtra = 0.01f },
        new PasoCombo
        {
            multiplicadorDano = 1.6f,
            multiplicadorRetroceso = 1.8f,
            impulsoJugador = 2.2f,
            hitstopExtra = 0.04f,
            esFinisher = true,
            triggerAnimacion = "AttackFinisher",
            bonusResonanciaAlConectar = 5,
            multiplicadorHitstopFinisher = 1.6f,
            alcanceExtra = 0.15f,
            costeResonancia = 0
        }
    };

    public PasoCombo[] comboArriba = new PasoCombo[]
    {
        new PasoCombo { multiplicadorDano = 1f, multiplicadorRetroceso = 1.1f },
        new PasoCombo
        {
            multiplicadorDano = 1.4f,
            multiplicadorRetroceso = 1.5f,
            esFinisher = true,
            triggerAnimacion = "AttackUpFinisher",
            hitstopExtra = 0.03f,
            multiplicadorHitstopFinisher = 1.4f,
            bonusResonanciaAlConectar = 3
        }
    };

    public PasoCombo[] comboAbajo = new PasoCombo[]
    {
        new PasoCombo { multiplicadorDano = 1f, multiplicadorRetroceso = 0.3f }
    };

    [Min(0.05f)] public float tiempoMaximoEntreGolpes = 0.55f;
    [Min(0f)] public float ventanaBufferCombo = 0.18f;
    public bool reiniciarComboAlCambiarDireccion = true;

    [Header("Finisher")]
    [Tooltip("Si el finisher tiene coste y no hay resonancia, usa el último golpe normal en su lugar.")]
    public bool degradarFinisherSinResonancia = true;
    [Tooltip("Tras un finisher, el combo siempre se reinicia.")]
    public bool reiniciarComboTrasFinisher = true;
    [Min(0f)] public float impulsoVerticalFinisherArriba = 3.5f;

    [Header("Pogo")]
    [Min(1f)] public float fuerzaRebotePogo = 12.5f;
    public bool recargarDashConPogo = true;

    [Header("Hitstop")]
    [Min(0f)] public float hitstopNormal = 0.055f;
    [Min(0f)] public float hitstopPogo = 0.09f;
    [Min(0f)] public float hitstopParry = 0.12f;
    [Min(0f)] public float hitstopFinisherBase = 0.1f;
    [Range(0.01f, 1f)] public float escalaHitstop = 0.05f;

    [Header("Defensa / Parry")]
    [Min(0.05f)] public float ventanaParry = 0.15f;
    public bool destruirProyectilesAlDefender = true;

    [Header("Descarga de presión")]
    [Min(0.5f)] public float radioImpactoDescarga = 2.4f;
    [Min(1)] public int danioDescarga = 2;
    [Min(0f)] public float fuerzaRetrocesoDescarga = 8f;
    [Min(0f)] public float hitstopDescarga = 0.1f;

    public FaseAtaque FaseActual { get; private set; } = FaseAtaque.Idle;
    public DireccionAtaque2D UltimaDireccionAtaque { get; private set; } = DireccionAtaque2D.Horizontal;
    public int IndiceComboActual { get; private set; }
    public bool UltimoGolpeFueFinisher { get; private set; }
    public bool EstaAtacando => FaseActual != FaseAtaque.Idle;
    public bool EstaDefendiendose { get; private set; }
    public bool PuedeAtacar => !EstaDefendiendose && (FaseActual == FaseAtaque.Idle || PuedeEncadenarDesdeRecovery());

    private Rigidbody2D rb;
    private float ataqueFaseTerminaEn;
    private float defensaIniciadaEn = float.NegativeInfinity;
    private float ultimoGolpeComboEn = float.NegativeInfinity;
    private bool hitboxYaAplicado;
    private bool ataqueEncolado;
    private DireccionAtaque2D direccionEncolada;
    private bool hitstopActivo;
    private bool pasoActualEsFinisher;
    private Coroutine hitstopCoroutine;
    private Vector3 posicionOriginalZonaDefensa;

    // Caché (evita GetComponent / allocs en el hot path)
    private ResonanciaJugador2D resonanciaCache;
    private static readonly Collider2D[] BufferImpactos = new Collider2D[24];
    private ContactFilter2D filtroAtaque;
    private bool filtroAtaqueListo;

    private void Awake()
    {
        if (jugador == null) jugador = GetComponent<Jugador2D>();
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (sprite == null) sprite = GetComponent<SpriteRenderer>();

        if (zonaDefensa != null)
        {
            posicionOriginalZonaDefensa = zonaDefensa.transform.localPosition;
            zonaDefensa.enabled = false;
        }
        if (espadaVisual != null)
            espadaVisual.SetActive(false);

        if (capaObstaculosRebotables.value == 0)
        {
            int mascara = 0;
            int ground = LayerMask.NameToLayer("Ground");
            int def = LayerMask.NameToLayer("Default");
            int enemy = LayerMask.NameToLayer("Enemy");
            if (ground >= 0) mascara |= 1 << ground;
            if (def >= 0) mascara |= 1 << def;
            if (enemy >= 0) mascara |= 1 << enemy;
            capaObstaculosRebotables = mascara;
        }

        resonanciaCache = GetComponent<ResonanciaJugador2D>();
        PrepararFiltroAtaque();
        AsegurarCombosPorDefecto();
    }

    private void PrepararFiltroAtaque()
    {
        LayerMask mascara = capaEnemigos;
        if (capaObstaculosRebotables.value != 0)
            mascara |= capaObstaculosRebotables;
        filtroAtaque = new ContactFilter2D
        {
            useTriggers = true,
            useLayerMask = true,
            layerMask = mascara
        };
        filtroAtaqueListo = true;
    }

    private void AsegurarCombosPorDefecto()
    {
        if (comboHorizontal == null || comboHorizontal.Length == 0)
            comboHorizontal = new[] { new PasoCombo() };
        if (comboArriba == null || comboArriba.Length == 0)
            comboArriba = new[] { new PasoCombo() };
        if (comboAbajo == null || comboAbajo.Length == 0)
            comboAbajo = new[] { new PasoCombo() };
    }

    private void Update()
    {
        if (jugador != null && jugador.TieneDialogoActivo)
        {
            if (FaseActual != FaseAtaque.Idle || EstaDefendiendose || IndiceComboActual != 0)
            {
                CancelarAtaque();
                ReiniciarCombo();
                EstaDefendiendose = false;
                if (zonaDefensa != null) zonaDefensa.enabled = false;
            }
            return;
        }

        // Timeout de combo solo si hay índice activo y estamos en Idle
        if (FaseActual == FaseAtaque.Idle && IndiceComboActual > 0)
        {
            if (Time.time - ultimoGolpeComboEn > tiempoMaximoEntreGolpes)
                ReiniciarCombo();
        }

        // Fases: solo avanza si hay ataque en curso (Idle + sin buffer → casi no-op)
        if (FaseActual != FaseAtaque.Idle)
            ActualizarFaseAtaque();
        else if (ataqueEncolado)
            ConsumirBufferEncolado();

        // Visuales solo mientras ataca
        if (FaseActual != FaseAtaque.Idle)
            ActualizarVisuales();
        else if (espadaVisual != null && espadaVisual.activeSelf)
            espadaVisual.SetActive(false);
    }

    /// <summary>
    /// Consume el ataque encolado por el buffer de recovery.
    /// Se llama al terminar recovery o si quedó pendiente al pasar a Idle.
    /// </summary>
    private void ConsumirBufferEncolado()
    {
        if (!ataqueEncolado)
            return;

        DireccionAtaque2D dir = direccionEncolada;
        ataqueEncolado = false;

        PrepararIndiceCombo(dir);
        if (!PuedePagarFinisherSiCorresponde(dir) && degradarFinisherSinResonancia)
        {
            PasoCombo[] cadena = ObtenerCadena(dir);
            if (IndiceComboActual > 0 && cadena[IndiceComboActual].esFinisher)
                IndiceComboActual = Mathf.Max(0, IndiceComboActual - 1);
        }
        IniciarAtaque(dir);
    }

    // -------------------- API --------------------

    public void SolicitarAtaque() => SolicitarAtaqueHacia(DireccionAtaque2D.Horizontal);

    public void SolicitarAtaqueHacia(DireccionAtaque2D direccion)
    {
        if (jugador != null && jugador.EstaEnDash) return;
        if (EstaDefendiendose) return;

        if (FaseActual == FaseAtaque.Idle)
        {
            PrepararIndiceCombo(direccion);
            if (!PuedePagarFinisherSiCorresponde(direccion))
            {
                // Sin resonancia: usar el penúltimo paso (golpe normal) en lugar del finisher
                if (degradarFinisherSinResonancia)
                {
                    PasoCombo[] cadena = ObtenerCadena(direccion);
                    if (IndiceComboActual > 0 && cadena[IndiceComboActual].esFinisher)
                        IndiceComboActual = Mathf.Max(0, IndiceComboActual - 1);
                }
                else
                {
                    return; // no ataca
                }
            }
            IniciarAtaque(direccion);
            return;
        }

        // Buffer de combo: un solo slot durante recovery (último input gana).
        // No hace trabajo en Startup/Active: evita spam y es O(1).
        if (FaseActual == FaseAtaque.Recovery && PuedeEncadenarDesdeRecovery())
            EncolarAtaque(direccion);
    }

    private void EncolarAtaque(DireccionAtaque2D direccion)
    {
        ataqueEncolado = true;
        direccionEncolada = direccion;
    }

    public void EstablecerDefensa(bool defendiendo)
    {
        if (EstaAtacando && defendiendo) return;
        if (defendiendo && !EstaDefendiendose)
            defensaIniciadaEn = Time.time;
        EstaDefendiendose = defendiendo;
        if (zonaDefensa != null) zonaDefensa.enabled = defendiendo;
    }

    public bool IntentarBloquearOParry(Component fuenteAtaque, Vector2 puntoImpacto)
    {
        if (!EstaDefendiendose) return false;
        bool esParry = (Time.time - defensaIniciadaEn) <= ventanaParry;
        if (esParry)
        {
            AplicarHitstop(hitstopParry);
            if (fuenteAtaque != null)
            {
                Rigidbody2D rbEnemigo = fuenteAtaque.GetComponentInParent<Rigidbody2D>();
                if (rbEnemigo != null)
                {
                    Vector2 dir = ((Vector2)rbEnemigo.position - (Vector2)transform.position).normalized;
                    if (dir.sqrMagnitude < 0.01f)
                        dir = jugador != null && jugador.MirandoDerecha ? Vector2.right : Vector2.left;
                    rbEnemigo.linearVelocity = dir * 4f + Vector2.up * 2f;
                }
            }
            return true;
        }
        return true;
    }

    public void ResolverImpactoDescargaPresion()
    {
        Vector2 centro = transform.position;
        Collider2D[] impactos = Physics2D.OverlapCircleAll(centro, radioImpactoDescarga);
        HashSet<Component> afectados = new HashSet<Component>();
        bool conecto = false;

        foreach (Collider2D col in impactos)
        {
            if (col == null) continue;
            SelloSalFragil2D sello = col.GetComponentInParent<SelloSalFragil2D>();
            if (sello != null && afectados.Add(sello))
            {
                sello.AlRecibirDescargaPresion(jugador != null ? jugador : GetComponent<Jugador2D>());
                conecto = true;
                continue;
            }
            foreach (MonoBehaviour comp in col.GetComponentsInParent<MonoBehaviour>())
            {
                if (comp is IDamageable2D objetivo && afectados.Add(comp))
                {
                    if (objetivo.RecibirDanio(danioDescarga, centro, fuerzaRetrocesoDescarga))
                        conecto = true;
                    break;
                }
            }
        }
        if (conecto) AplicarHitstop(hitstopDescarga);
        ReiniciarCombo();
    }

    public void CancelarAtaque()
    {
        FaseActual = FaseAtaque.Idle;
        hitboxYaAplicado = false;
        ataqueEncolado = false;
        pasoActualEsFinisher = false;
        ataqueFaseTerminaEn = 0f;
        if (espadaVisual != null) espadaVisual.SetActive(false);
    }

    public void ReiniciarCombo()
    {
        IndiceComboActual = 0;
        ataqueEncolado = false;
        UltimoGolpeFueFinisher = false;
    }

    public void EjecutarRebotePogo(float fuerzaPersonalizada = -1f)
    {
        if (rb == null) return;
        float fuerza = fuerzaPersonalizada > 0f ? fuerzaPersonalizada : fuerzaRebotePogo;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerza);
    }

    public void AplicarHitstop(float duracion)
    {
        if (duracion <= 0f || hitstopActivo) return;
        if (hitstopCoroutine != null) StopCoroutine(hitstopCoroutine);
        hitstopCoroutine = StartCoroutine(HitstopCoroutine(duracion));
    }

    // -------------------- Combo / Finisher --------------------

    private PasoCombo[] ObtenerCadena(DireccionAtaque2D direccion)
    {
        switch (direccion)
        {
            case DireccionAtaque2D.Arriba: return comboArriba;
            case DireccionAtaque2D.Abajo: return comboAbajo;
            default: return comboHorizontal;
        }
    }

    private PasoCombo ObtenerPasoActual(DireccionAtaque2D direccion)
    {
        PasoCombo[] cadena = ObtenerCadena(direccion);
        int i = Mathf.Clamp(IndiceComboActual, 0, cadena.Length - 1);
        return cadena[i];
    }

    private void PrepararIndiceCombo(DireccionAtaque2D direccion)
    {
        bool mismoCombo = !reiniciarComboAlCambiarDireccion || direccion == UltimaDireccionAtaque;
        bool enTiempo = Time.time - ultimoGolpeComboEn <= tiempoMaximoEntreGolpes;

        if (mismoCombo && enTiempo && FaseActual == FaseAtaque.Idle)
        {
            PasoCombo[] cadena = ObtenerCadena(direccion);
            // Si el último fue finisher y se reinició, empezamos de 0
            if (UltimoGolpeFueFinisher && reiniciarComboTrasFinisher)
                IndiceComboActual = 0;
            else
                IndiceComboActual = Mathf.Min(IndiceComboActual + 1, cadena.Length - 1);
        }
        else
        {
            IndiceComboActual = 0;
        }
    }

    private bool PuedePagarFinisherSiCorresponde(DireccionAtaque2D direccion)
    {
        PasoCombo paso = ObtenerPasoActual(direccion);
        if (!paso.esFinisher || paso.costeResonancia <= 0)
            return true;

        if (resonanciaCache == null)
            resonanciaCache = GetComponent<ResonanciaJugador2D>();
        if (resonanciaCache == null)
            return degradarFinisherSinResonancia;

        return resonanciaCache.ResonanciaActual >= paso.costeResonancia;
    }

    private void GastarResonanciaFinisher(PasoCombo paso)
    {
        if (!paso.esFinisher || paso.costeResonancia <= 0) return;
        if (resonanciaCache == null)
            resonanciaCache = GetComponent<ResonanciaJugador2D>();
        if (resonanciaCache == null) return;
        resonanciaCache.EstablecerResonancia(resonanciaCache.ResonanciaActual - paso.costeResonancia);
    }

    private float ObtenerStartup(PasoCombo paso) =>
        paso.esFinisher && startupFinisher >= 0f ? startupFinisher : startupAtaque;

    private float ObtenerActive(PasoCombo paso) =>
        paso.esFinisher && activeFinisher >= 0f ? activeFinisher : activeAtaque;

    private float ObtenerRecovery(PasoCombo paso) =>
        paso.esFinisher && recoveryFinisher >= 0f ? recoveryFinisher : recoveryAtaque;

    // -------------------- Flujo --------------------

    private void IniciarAtaque(DireccionAtaque2D direccion)
    {
        UltimaDireccionAtaque = direccion;
        PasoCombo paso = ObtenerPasoActual(direccion);
        pasoActualEsFinisher = paso.esFinisher;
        UltimoGolpeFueFinisher = false;

        if (paso.esFinisher)
            GastarResonanciaFinisher(paso);

        FaseActual = FaseAtaque.Startup;
        ataqueFaseTerminaEn = Time.time + ObtenerStartup(paso);
        hitboxYaAplicado = false;
        ataqueEncolado = false;

        DispararTriggerAnimacion(direccion, paso);
        PosicionarEspadaVisual(direccion, paso);
        if (espadaVisual != null) espadaVisual.SetActive(true);

        if (rb != null)
        {
            bool mirandoDerecha = jugador == null || jugador.MirandoDerecha;
            float dir = mirandoDerecha ? 1f : -1f;

            if (direccion == DireccionAtaque2D.Horizontal && Mathf.Abs(paso.impulsoJugador) > 0.01f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x + dir * paso.impulsoJugador,
                    rb.linearVelocity.y);
            }

            // Finisher hacia arriba: pequeño impulso vertical
            if (paso.esFinisher && direccion == DireccionAtaque2D.Arriba && impulsoVerticalFinisherArriba > 0f)
            {
                rb.linearVelocity = new Vector2(
                    rb.linearVelocity.x,
                    Mathf.Max(rb.linearVelocity.y, impulsoVerticalFinisherArriba));
            }
        }

        if (animator != null)
        {
            if (TieneParametroBool("IsFinisher"))
                animator.SetBool("IsFinisher", paso.esFinisher);
            if (TieneParametroTrigger("Finisher") && paso.esFinisher)
                animator.SetTrigger("Finisher");
        }
    }

    private void ActualizarFaseAtaque()
    {
        if (FaseActual == FaseAtaque.Idle) return;
        if (Time.time < ataqueFaseTerminaEn) return;

        PasoCombo paso = ObtenerPasoActual(UltimaDireccionAtaque);

        switch (FaseActual)
        {
            case FaseAtaque.Startup:
                FaseActual = FaseAtaque.Active;
                ataqueFaseTerminaEn = Time.time + ObtenerActive(paso);
                ResolverHitboxAtaque(UltimaDireccionAtaque);
                hitboxYaAplicado = true;
                ultimoGolpeComboEn = Time.time;
                break;

            case FaseAtaque.Active:
                FaseActual = FaseAtaque.Recovery;
                ataqueFaseTerminaEn = Time.time + ObtenerRecovery(paso);
                break;

            case FaseAtaque.Recovery:
                FaseActual = FaseAtaque.Idle;
                if (espadaVisual != null) espadaVisual.SetActive(false);

                if (pasoActualEsFinisher && reiniciarComboTrasFinisher)
                    ReiniciarCombo();

                ConsumirBufferEncolado();
                break;
        }
    }

    private void ResolverHitboxAtaque(DireccionAtaque2D direccion)
    {
        PasoCombo paso = ObtenerPasoActual(direccion);
        int dano = Mathf.Max(1, Mathf.RoundToInt(danoAtaque * paso.multiplicadorDano));
        float retroceso = fuerzaRetroceso * paso.multiplicadorRetroceso;
        float alcance = Mathf.Max(0.1f, alcanceAtaque + paso.alcanceExtra);
        float altura = Mathf.Max(0.1f, alturaAtaque);
        bool mirandoDerecha = jugador == null || jugador.MirandoDerecha;

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

        if (!filtroAtaqueListo)
            PrepararFiltroAtaque();
        else
        {
            LayerMask mascara = capaEnemigos;
            if (capaObstaculosRebotables.value != 0)
                mascara |= capaObstaculosRebotables;
            if (filtroAtaque.layerMask != mascara)
                filtroAtaque.layerMask = mascara;
        }

        // Overlap sin alloc de array
        int numImpactos = Physics2D.OverlapBox(centro, tamano, 0f, filtroAtaque, BufferImpactos);

        // Hasta 8 IDamageable únicos sin HashSet
        const int maxUnicos = 8;
        IDamageable2D u0 = null, u1 = null, u2 = null, u3 = null;
        IDamageable2D u4 = null, u5 = null, u6 = null, u7 = null;
        int numUnicos = 0;

        bool aplicoDano = false;
        bool ejecutoPogo = false;
        Vector2 posBase = transform.position;

        for (int i = 0; i < numImpactos; i++)
        {
            Collider2D impacto = BufferImpactos[i];
            if (impacto == null)
                continue;

            if (direccion == DireccionAtaque2D.Abajo && !ejecutoPogo)
            {
                IRebotablePogo2D rebotable = impacto.GetComponentInParent<IRebotablePogo2D>();
                if (rebotable != null && (!(rebotable is MonoBehaviour mb) || mb.isActiveAndEnabled))
                {
                    Vector2 punto = impacto.ClosestPoint(posBase);
                    Jugador2D refJugador = jugador != null ? jugador : GetComponent<Jugador2D>();
                    if (rebotable.AlRecibirPogo(refJugador, punto))
                        ejecutoPogo = true;
                }
            }

            // Preferir un solo IDamageable por jerarquía (el de más arriba que implemente la interfaz)
            IDamageable2D objetivo = impacto.GetComponentInParent<IDamageable2D>();
            if (objetivo == null)
                continue;
            if (objetivo is MonoBehaviour mbObj && !mbObj.isActiveAndEnabled)
                continue;

            // Dedup O(1) con lista fija
            bool ya = false;
            switch (numUnicos)
            {
                case 8: ya = true; break;
                case 7: ya = ReferenceEquals(objetivo, u6); if (ya) break; goto case 6;
                case 6: ya = ReferenceEquals(objetivo, u5); if (ya) break; goto case 5;
                case 5: ya = ReferenceEquals(objetivo, u4); if (ya) break; goto case 4;
                case 4: ya = ReferenceEquals(objetivo, u3); if (ya) break; goto case 3;
                case 3: ya = ReferenceEquals(objetivo, u2); if (ya) break; goto case 2;
                case 2: ya = ReferenceEquals(objetivo, u1); if (ya) break; goto case 1;
                case 1: ya = ReferenceEquals(objetivo, u0); break;
            }
            if (ya)
                continue;

            switch (numUnicos++)
            {
                case 0: u0 = objetivo; break;
                case 1: u1 = objetivo; break;
                case 2: u2 = objetivo; break;
                case 3: u3 = objetivo; break;
                case 4: u4 = objetivo; break;
                case 5: u5 = objetivo; break;
                case 6: u6 = objetivo; break;
                case 7: u7 = objetivo; break;
            }

            Vector2 posAtaque = posBase;
            if (direccion == DireccionAtaque2D.Abajo)
                posAtaque = posBase + Vector2.up * 0.5f;
            else if (direccion == DireccionAtaque2D.Arriba)
                posAtaque = posBase + Vector2.down * 0.5f;

            if (objetivo.RecibirDanio(dano, posAtaque, retroceso))
            {
                aplicoDano = true;
                if (direccion == DireccionAtaque2D.Abajo)
                    ejecutoPogo = true;

                if (resonanciaCache == null)
                    resonanciaCache = GetComponent<ResonanciaJugador2D>();
                if (resonanciaCache != null)
                {
                    int ganancia = resonanciaCache.GananciaPorImpacto;
                    if (paso.esFinisher)
                        ganancia += paso.bonusResonanciaAlConectar;
                    resonanciaCache.GanarResonancia(ganancia);
                }
            }
        }

        if (direccion == DireccionAtaque2D.Abajo && ejecutoPogo)
        {
            EjecutarRebotePogo();
            AplicarHitstop(hitstopPogo + paso.hitstopExtra);
            ReiniciarCombo();
        }
        else if (aplicoDano)
        {
            float hitstop = paso.esFinisher
                ? (hitstopFinisherBase + paso.hitstopExtra) * paso.multiplicadorHitstopFinisher
                : hitstopNormal + paso.hitstopExtra;
            AplicarHitstop(hitstop);

            if (direccion == DireccionAtaque2D.Horizontal && rb != null)
            {
                Vector2 dirRetro = mirandoDerecha ? Vector2.left : Vector2.right;
                float impulso = impulsoRetrocesoJugador * (0.6f + 0.25f * IndiceComboActual);
                if (paso.esFinisher) impulso *= 1.35f;
                rb.AddForce(dirRetro * Mathf.Max(0f, impulso), ForceMode2D.Impulse);
            }

            if (paso.esFinisher)
            {
                UltimoGolpeFueFinisher = true;
                OnFinisherConectado(direccion, paso);
            }
        }
        else if (paso.esFinisher)
        {
            UltimoGolpeFueFinisher = true;
        }
    }

    /// <summary>Gancho para efectos extra al conectar un finisher (partículas, shake, sonido).</summary>
    private void OnFinisherConectado(DireccionAtaque2D direccion, PasoCombo paso)
    {
        if (animator != null && TieneParametroTrigger("FinisherHit"))
            animator.SetTrigger("FinisherHit");

        // Hook: CameraShake2D.Instance?.Shake(0.15f, 0.2f);
    }

    private bool PuedeEncadenarDesdeRecovery()
    {
        if (FaseActual != FaseAtaque.Recovery)
            return false;
        // Tras finisher no se encadena
        if (pasoActualEsFinisher && reiniciarComboTrasFinisher)
            return false;
        // ventanaBufferCombo <= 0 → buffer durante todo el recovery
        if (ventanaBufferCombo <= 0f)
            return true;
        return (ataqueFaseTerminaEn - Time.time) <= ventanaBufferCombo;
    }

    private IEnumerator HitstopCoroutine(float duracion)
    {
        hitstopActivo = true;
        float escalaAnterior = Time.timeScale;
        if (escalaAnterior > 0.01f)
            Time.timeScale = Mathf.Min(escalaAnterior, escalaHitstop);
        yield return new WaitForSecondsRealtime(duracion);
        if (Time.timeScale <= escalaHitstop + 0.001f)
            Time.timeScale = escalaAnterior > 0.01f ? escalaAnterior : 1f;
        hitstopActivo = false;
        hitstopCoroutine = null;
    }

    // -------------------- Visuales --------------------

    private void ActualizarVisuales()
    {
        if (espadaVisual != null)
            espadaVisual.SetActive(FaseActual == FaseAtaque.Startup || FaseActual == FaseAtaque.Active);
        if (FaseActual != FaseAtaque.Idle)
            PosicionarEspadaVisual(UltimaDireccionAtaque, ObtenerPasoActual(UltimaDireccionAtaque));
        if (animator != null && TieneParametroFloat("ComboIndex"))
            animator.SetFloat("ComboIndex", IndiceComboActual);
    }

    private void DispararTriggerAnimacion(DireccionAtaque2D direccion, PasoCombo paso)
    {
        if (animator == null) return;

        if (!string.IsNullOrEmpty(paso.triggerAnimacion) && TieneParametroTrigger(paso.triggerAnimacion))
        {
            animator.SetTrigger(paso.triggerAnimacion);
            return;
        }

        if (direccion == DireccionAtaque2D.Arriba && TieneParametroTrigger("AttackUp"))
            animator.SetTrigger("AttackUp");
        else if (direccion == DireccionAtaque2D.Abajo && TieneParametroTrigger("AttackDown"))
            animator.SetTrigger("AttackDown");
        else if (TieneParametroTrigger("Attack"))
            animator.SetTrigger("Attack");
        else if (TieneParametroTrigger("attack"))
            animator.SetTrigger("attack");
    }

    private bool TieneParametroTrigger(string nombre)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Trigger) return true;
        return false;
    }

    private bool TieneParametroFloat(string nombre)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Float) return true;
        return false;
    }

    private bool TieneParametroBool(string nombre)
    {
        if (animator == null) return false;
        foreach (var p in animator.parameters)
            if (p.name == nombre && p.type == AnimatorControllerParameterType.Bool) return true;
        return false;
    }

    private void PosicionarEspadaVisual(DireccionAtaque2D direccion, PasoCombo paso)
    {
        if (espadaVisual == null) return;
        float alcance = Mathf.Max(0.1f, alcanceAtaque + (paso != null ? paso.alcanceExtra : 0f));
        bool mirandoDerecha = jugador == null || jugador.MirandoDerecha;
        float dirX = mirandoDerecha ? 1f : -1f;
        float extra = 1f + IndiceComboActual * 0.05f;
        if (paso != null && paso.esFinisher) extra += 0.1f;

        switch (direccion)
        {
            case DireccionAtaque2D.Arriba:
                espadaVisual.transform.localPosition = new Vector3(0f, alcance * 0.65f * extra, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                break;
            case DireccionAtaque2D.Abajo:
                espadaVisual.transform.localPosition = new Vector3(0f, -alcance * 0.65f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, -90f);
                break;
            default:
                espadaVisual.transform.localPosition = new Vector3(alcance * 0.65f * dirX * extra, 0.05f, 0f);
                espadaVisual.transform.localRotation = Quaternion.Euler(0f, 0f, mirandoDerecha ? 0f : 180f);
                break;
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (!destruirProyectilesAlDefender || !EstaDefendiendose) return;
        if (otro.CompareTag("EnemyAttack"))
        {
            IntentarBloquearOParry(otro, otro.ClosestPoint(transform.position));
            Destroy(otro.gameObject);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        float alcance = Mathf.Max(0.1f, alcanceAtaque);
        float altura = Mathf.Max(0.1f, alturaAtaque);
        bool mirandoDerecha = Application.isPlaying && jugador != null && jugador.MirandoDerecha;
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(transform.position + Vector3.right * (mirandoDerecha ? alcance * 0.5f : -alcance * 0.5f),
            new Vector3(alcance, altura, 0f));
        Gizmos.color = new Color(1f, 0.4f, 0f, 0.35f);
        Gizmos.DrawWireSphere(transform.position, radioImpactoDescarga);
    }
#endif
}
