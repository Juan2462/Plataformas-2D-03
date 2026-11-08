using System.Collections;
using UnityEngine;

/// <summary>
/// Trampa ambiental de vapor intermitente.
/// Alterna en ciclos predecibles entre reposo, advertencia (silbido/fuga)
/// y erupcion a presion.
/// Durante la erupcion, su collider activo inflige dano a la integridad de Sira
/// y la empuja segun la direccion del escape.
/// </summary>
[DisallowMultipleComponent]
public class ObstaculoVapor2D : MonoBehaviour
{
    public enum EstadoVapor
    {
        Reposo,
        Aviso,
        Activo
    }

    [Header("Tiempos del Ciclo")]
    [SerializeField, Min(0.1f)] private float tiempoReposo = 2f;
    [SerializeField, Min(0.05f)] private float tiempoAviso = 0.6f;
    [SerializeField, Min(0.1f)] private float tiempoActivo = 1.2f;
    [SerializeField, Min(0f)] private float desfaseInicial = 0f;

    [Header("Dano y Empuje")]
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField] private Vector2 direccionEmpuje = Vector2.up;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 7f;

    [Header("Referencias")]
    [SerializeField] private Collider2D colisionadorVapor;
    [SerializeField] private SpriteRenderer visualVapor;
    [SerializeField] private ParticleSystem particulasVapor;

    [Header("Visual")]
    [SerializeField] private Color colorAviso = new Color(1f, 1f, 1f, 0.25f);
    [SerializeField] private Color colorActivo = new Color(0.9f, 0.98f, 1f, 0.85f);
    [SerializeField] private Color colorReposo = new Color(1f, 1f, 1f, 0f);

    public EstadoVapor EstadoActual { get; private set; } = EstadoVapor.Reposo;
    public bool EstaActivo => EstadoActual == EstadoVapor.Activo;

    private Coroutine cicloCoroutine;

    private void Awake()
    {
        if (colisionadorVapor == null)
            colisionadorVapor = GetComponent<Collider2D>();

        if (colisionadorVapor != null)
            colisionadorVapor.isTrigger = true;

        if (visualVapor == null)
            visualVapor = GetComponent<SpriteRenderer>();

        AplicarEstadoVisual(EstadoVapor.Reposo);
    }

    private void OnEnable()
    {
        cicloCoroutine = StartCoroutine(RutinaCiclo());
    }

    private void OnDisable()
    {
        if (cicloCoroutine != null)
        {
            StopCoroutine(cicloCoroutine);
            cicloCoroutine = null;
        }
        AplicarEstadoVisual(EstadoVapor.Reposo);
    }

    private IEnumerator RutinaCiclo()
    {
        if (desfaseInicial > 0f)
            yield return new WaitForSeconds(desfaseInicial);

        while (true)
        {
            // 1. Reposo
            EstadoActual = EstadoVapor.Reposo;
            AplicarEstadoVisual(EstadoVapor.Reposo);
            if (colisionadorVapor != null)
                colisionadorVapor.enabled = false;
            yield return new WaitForSeconds(tiempoReposo);

            // 2. Aviso
            EstadoActual = EstadoVapor.Aviso;
            AplicarEstadoVisual(EstadoVapor.Aviso);
            if (particulasVapor != null && !particulasVapor.isPlaying)
                particulasVapor.Play();
            yield return new WaitForSeconds(tiempoAviso);

            // 3. Activo (Erupcion)
            EstadoActual = EstadoVapor.Activo;
            AplicarEstadoVisual(EstadoVapor.Activo);
            if (colisionadorVapor != null)
                colisionadorVapor.enabled = true;
            yield return new WaitForSeconds(tiempoActivo);
        }
    }

    private void AplicarEstadoVisual(EstadoVapor estado)
    {
        if (visualVapor != null)
        {
            switch (estado)
            {
                case EstadoVapor.Reposo:
                    visualVapor.color = colorReposo;
                    break;
                case EstadoVapor.Aviso:
                    visualVapor.color = colorAviso;
                    break;
                case EstadoVapor.Activo:
                    visualVapor.color = colorActivo;
                    break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        ProcesarImpacto(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        ProcesarImpacto(otro);
    }

    private void ProcesarImpacto(Collider2D otro)
    {
        if (!EstaActivo || otro == null)
            return;

        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null)
            return;

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null || !vida.EstaVivo)
            return;

        Vector2 posicionOrigen = (Vector2)transform.position - direccionEmpuje.normalized * 0.5f;
        if (vida.RecibirDanio(danio, posicionOrigen))
        {
            Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
            if (rb != null && fuerzaEmpuje > 0f)
            {
                rb.linearVelocity = new Vector2(
                    direccionEmpuje.x != 0 ? Mathf.Sign(direccionEmpuje.x) * fuerzaEmpuje : rb.linearVelocity.x,
                    direccionEmpuje.y > 0 ? fuerzaEmpuje : rb.linearVelocity.y
                );
            }
        }
    }

    public void Configurar(float reposo, float aviso, float activo, float desfase = 0f, int nuevoDanio = 1)
    {
        tiempoReposo = Mathf.Max(0.1f, reposo);
        tiempoAviso = Mathf.Max(0.05f, aviso);
        tiempoActivo = Mathf.Max(0.1f, activo);
        desfaseInicial = Mathf.Max(0f, desfase);
        danio = Mathf.Max(1, nuevoDanio);
    }
}
