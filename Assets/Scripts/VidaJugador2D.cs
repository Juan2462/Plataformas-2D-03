using System;
using UnityEngine;

public class VidaJugador2D : MonoBehaviour
{
    [SerializeField, Min(1)] private int vidaMaxima = 5;
    [SerializeField] private float tiempoInvulnerable = 0.75f;
    [SerializeField, Min(0f)] private float fuerzaRetrocesoRecibido = 5f;
    [SerializeField, Min(0f)] private float intervaloParpadeoDanio = 0.08f;

    public int VidaActual { get; private set; }
    public int VidaMaxima => vidaMaxima;
    public bool EstaVivo => VidaActual > 0;

    public event Action<int, int> VidaCambiada;
    public event Action JugadorDerrotado;
    public event Action<Vector2> DanioPeligroAmbientalRecibido;

    public Vector2 UltimoSueloSeguro { get; private set; }

    private float puedeRecibirDanioDesde;
    private Jugador2D controlador;
    private Rigidbody2D rb;
    private FeedbackImpacto2D feedbackImpacto;

    private void Awake()
    {
        VidaActual = Mathf.Max(1, vidaMaxima);
        controlador = GetComponent<Jugador2D>();
        rb = GetComponent<Rigidbody2D>();
        UltimoSueloSeguro = transform.position;
    }

    private void Start()
    {
        UltimoSueloSeguro = transform.position;
    }

    private void Update()
    {
        ActualizarSueloSeguro();
    }

    public void ActualizarSueloSeguro()
    {
        if (controlador != null && controlador.EstaEnElSuelo() && rb != null && Mathf.Abs(rb.linearVelocity.y) < 0.2f)
        {
            UltimoSueloSeguro = transform.position;
        }
    }

    public bool RecibirDanioPeligroAmbiental(int cantidad = 1, Vector2? puntoSeguroForzado = null)
    {
        if (cantidad <= 0 || !EstaVivo || Time.time < puedeRecibirDanioDesde)
            return false;

        VidaActual = Mathf.Max(0, VidaActual - cantidad);
        puedeRecibirDanioDesde = Time.time + Mathf.Max(0.5f, tiempoInvulnerable);

        Vector2 destino = puntoSeguroForzado ?? UltimoSueloSeguro;
        if (rb != null)
        {
            rb.position = destino;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = destino;
        }

        ObtenerFeedbackImpacto()?.MostrarInvulnerabilidad(
            Mathf.Max(0.5f, tiempoInvulnerable),
            intervaloParpadeoDanio
        );
        VidaCambiada?.Invoke(VidaActual, vidaMaxima);
        DanioPeligroAmbientalRecibido?.Invoke(destino);

        if (VidaActual == 0)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (controlador != null)
                controlador.enabled = false;

            JugadorDerrotado?.Invoke();
        }

        return true;
    }

    public bool RecibirDanio(int cantidad)
    {
        return RecibirDanio(cantidad, Vector2.zero, false);
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante)
    {
        return RecibirDanio(cantidad, posicionAtacante, true);
    }

    private bool RecibirDanio(int cantidad, Vector2 posicionAtacante, bool aplicarRetroceso)
    {
        if (cantidad <= 0)
        {
            Debug.LogWarning("El daño debe ser mayor que cero.", this);
            return false;
        }

        if (!EstaVivo || Time.time < puedeRecibirDanioDesde)
            return false;

        if (controlador != null && (controlador.EstaDefendiendose || controlador.EstaEnDash))
            return false;

        VidaActual = Mathf.Max(0, VidaActual - cantidad);
        puedeRecibirDanioDesde = Time.time + Mathf.Max(0f, tiempoInvulnerable);
        if (aplicarRetroceso)
            AplicarRetroceso(posicionAtacante);
        ObtenerFeedbackImpacto()?.MostrarInvulnerabilidad(
            Mathf.Max(0.08f, tiempoInvulnerable),
            intervaloParpadeoDanio
        );
        VidaCambiada?.Invoke(VidaActual, vidaMaxima);

        if (VidaActual == 0)
        {
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            if (controlador != null)
                controlador.enabled = false;

            JugadorDerrotado?.Invoke();
        }

        return true;
    }

    private void AplicarRetroceso(Vector2 posicionAtacante)
    {
        float fuerza = Mathf.Max(0f, fuerzaRetrocesoRecibido);
        if (rb == null || fuerza <= 0f)
            return;

        float direccion = Mathf.Sign(rb.position.x - posicionAtacante.x);
        if (Mathf.Approximately(direccion, 0f))
            direccion = 1f;

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

    public bool Curar(int cantidad)
    {
        if (cantidad <= 0)
        {
            Debug.LogWarning("La curación debe ser mayor que cero.", this);
            return false;
        }

        if (!EstaVivo || VidaActual >= vidaMaxima)
            return false;

        VidaActual = Mathf.Min(vidaMaxima, VidaActual + cantidad);
        VidaCambiada?.Invoke(VidaActual, vidaMaxima);
        return true;
    }

    public bool Respawnear(Vector2 posicion)
    {
        if (EstaVivo)
            return false;

        return RespawnearEnPuntoControl(posicion);
    }

    public bool RespawnearEnPuntoControl(Vector2 posicion)
    {
        if (float.IsNaN(posicion.x) || float.IsNaN(posicion.y) ||
            float.IsInfinity(posicion.x) || float.IsInfinity(posicion.y))
        {
            Debug.LogError("La posicion de reaparicion no es valida.", this);
            return false;
        }

        VidaActual = Mathf.Max(1, vidaMaxima);
        puedeRecibirDanioDesde = Time.time + Mathf.Max(0f, tiempoInvulnerable);

        controlador?.RestablecerEstadoParaReaparicion();
        if (rb != null)
        {
            rb.position = posicion;
            rb.linearVelocity = Vector2.zero;
        }
        else
        {
            transform.position = posicion;
        }

        if (controlador != null)
            controlador.enabled = true;

        Physics2D.SyncTransforms();
        ObtenerFeedbackImpacto()?.MostrarInvulnerabilidad(
            Mathf.Max(0.08f, tiempoInvulnerable),
            intervaloParpadeoDanio
        );
        VidaCambiada?.Invoke(VidaActual, vidaMaxima);
        return true;
    }
}
