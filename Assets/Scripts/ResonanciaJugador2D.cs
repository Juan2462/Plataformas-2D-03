using System;
using UnityEngine;

/// <summary>
/// Mecanica de Frecuencia / Resonancia y Curacion Activa (Focus estilo Hollow Knight).
/// Golpear enemigos acumula Resonancia en el instrumento de Sira.
/// Al mantener pulsada la tecla de concentracion (F), Sira canaliza energia
/// para restaurar 1 punto de integridad a cambio de 33 puntos de resonancia.
/// Si recibe dano durante la concentracion, el proceso se interrumpe inmediatamente.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(VidaJugador2D))]
public class ResonanciaJugador2D : MonoBehaviour
{
    [Header("Resonancia Acustica")]
    [SerializeField, Range(0, 100)] private int resonanciaActual = 0;
    [SerializeField, Min(10)] private int resonanciaMaxima = 100;
    [SerializeField, Min(10)] private int costeCuracion = 33;
    [SerializeField, Min(1)] private int gananciaPorImpacto = 11;

    [Header("Concentracion (Focus)")]
    [SerializeField, Min(0.2f)] private float tiempoCanalizacionRequerido = 0.85f;
    [SerializeField] private KeyCode teclaConcentracion = KeyCode.F;

    public int ResonanciaActual => resonanciaActual;
    public int ResonanciaMaxima => resonanciaMaxima;
    public int CosteCuracion => costeCuracion;
    public int GananciaPorImpacto => gananciaPorImpacto;
    public bool EstaConcentrandose { get; private set; }
    public float ProgresoConcentracion { get; private set; }

    public event Action<int, int> ResonanciaCambiada;
    public event Action ConcentracionIniciada;
    public event Action ConcentracionCancelada;
    public event Action ConcentracionExitosa;

    private VidaJugador2D vida;
    private Jugador2D controlador;
    private Rigidbody2D rb;
    private float tiempoCanalizado;
    private int ultimaVidaRegistrada;

    private void Awake()
    {
        vida = GetComponent<VidaJugador2D>();
        controlador = GetComponent<Jugador2D>();
        rb = GetComponent<Rigidbody2D>();
    }

    private void Start()
    {
        if (vida != null)
        {
            ultimaVidaRegistrada = vida.VidaActual;
            vida.VidaCambiada += AlCambiarVida;
        }
        ResonanciaCambiada?.Invoke(resonanciaActual, resonanciaMaxima);
    }

    private void OnDestroy()
    {
        if (vida != null)
            vida.VidaCambiada -= AlCambiarVida;
    }

    private void Update()
    {
        if (controlador != null && controlador.TieneDialogoActivo)
        {
            if (EstaConcentrandose)
                CancelarConcentracion();
            return;
        }

        if (Input.GetKeyDown(teclaConcentracion))
        {
            if (PuedeConcentrarse())
                IniciarConcentracion();
        }

        if (EstaConcentrandose)
        {
            if (!Input.GetKey(teclaConcentracion) || !PuedeConcentrarse())
            {
                CancelarConcentracion();
                return;
            }

            if (rb != null)
                rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            tiempoCanalizado += Time.deltaTime;
            ProgresoConcentracion = Mathf.Clamp01(tiempoCanalizado / tiempoCanalizacionRequerido);

            if (tiempoCanalizado >= tiempoCanalizacionRequerido)
            {
                CompletarCuracion();
            }
        }
        else
        {
            ProgresoConcentracion = 0f;
        }
    }

    public bool PuedeConcentrarse()
    {
        if (vida == null || !vida.EstaVivo || vida.VidaActual >= vida.VidaMaxima)
            return false;

        if (resonanciaActual < costeCuracion)
            return false;

        if (controlador != null && (!controlador.EstaEnElSuelo() || controlador.EstaEnDash))
            return false;

        return true;
    }

    public void IniciarConcentracion()
    {
        EstaConcentrandose = true;
        tiempoCanalizado = 0f;
        ProgresoConcentracion = 0f;
        ConcentracionIniciada?.Invoke();
    }

    public void CancelarConcentracion()
    {
        if (!EstaConcentrandose)
            return;

        EstaConcentrandose = false;
        tiempoCanalizado = 0f;
        ProgresoConcentracion = 0f;
        ConcentracionCancelada?.Invoke();
    }

    private void CompletarCuracion()
    {
        resonanciaActual = Mathf.Max(0, resonanciaActual - costeCuracion);
        vida.Curar(1);
        ultimaVidaRegistrada = vida.VidaActual;

        tiempoCanalizado = 0f;
        ProgresoConcentracion = 0f;

        ResonanciaCambiada?.Invoke(resonanciaActual, resonanciaMaxima);
        ConcentracionExitosa?.Invoke();

        if (vida.VidaActual >= vida.VidaMaxima || resonanciaActual < costeCuracion)
        {
            EstaConcentrandose = false;
        }
    }

    public void GanarResonancia(int cantidad)
    {
        if (cantidad <= 0)
            return;

        int previa = resonanciaActual;
        resonanciaActual = Mathf.Clamp(resonanciaActual + cantidad, 0, resonanciaMaxima);

        if (resonanciaActual != previa)
            ResonanciaCambiada?.Invoke(resonanciaActual, resonanciaMaxima);
    }

    public void EstablecerResonancia(int valor)
    {
        resonanciaActual = Mathf.Clamp(valor, 0, resonanciaMaxima);
        ResonanciaCambiada?.Invoke(resonanciaActual, resonanciaMaxima);
    }

    private void AlCambiarVida(int actual, int maxima)
    {
        if (actual < ultimaVidaRegistrada && EstaConcentrandose)
        {
            CancelarConcentracion();
        }
        ultimaVidaRegistrada = actual;
    }

    public void Configurar(int inicial, int maximo, int coste, int ganancia, float tiempoReq)
    {
        resonanciaMaxima = Mathf.Max(10, maximo);
        resonanciaActual = Mathf.Clamp(inicial, 0, resonanciaMaxima);
        costeCuracion = Mathf.Max(5, coste);
        gananciaPorImpacto = Mathf.Max(1, ganancia);
        tiempoCanalizacionRequerido = Mathf.Max(0.1f, tiempoReq);
        ResonanciaCambiada?.Invoke(resonanciaActual, resonanciaMaxima);
    }
}
