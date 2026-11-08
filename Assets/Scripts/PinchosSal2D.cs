using UnityEngine;

/// <summary>
/// Obstaculo de peligro ambiental (pinchos de sal).
/// Dania al jugador si entra en contacto directo con el cuerpo, pero permite
/// el rebote fisico (pogo) si el jugador realiza un ataque hacia abajo sobre ellos.
/// Al ejecutar pogo exitoso, otorga una breve ventana de inmunidad para evitar
/// dano involuntario en el mismo frame del impacto.
/// Opcionalmente puede recolocar al jugador en un punto seguro si recibe dano.
/// </summary>
[DisallowMultipleComponent]
public class PinchosSal2D : MonoBehaviour, IRebotablePogo2D
{
    [Header("Dano y Peligro")]
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField, Min(0f)] private float fuerzaEmpuje = 6f;
    [SerializeField, Min(0.05f)] private float tiempoInmunidadPogo = 0.25f;

    [Header("Retorno Seguro (Opcional)")]
    [SerializeField] private Transform puntoRetornoSeguro;

    [Header("Feedback Visual")]
    [SerializeField] private Color colorImpactoPogo = new Color(0.9f, 0.95f, 1f, 1f);

    private float inmuneHasta;
    private SpriteRenderer spriteRenderer;
    private Color colorOriginal = Color.white;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
            colorOriginal = spriteRenderer.color;
    }

    public bool AlRecibirPogo(Jugador2D jugador, Vector2 puntoContacto)
    {
        if (jugador == null)
            return false;

        inmuneHasta = Time.time + tiempoInmunidadPogo;

        if (spriteRenderer != null)
        {
            spriteRenderer.color = colorImpactoPogo;
            CancelInvoke(nameof(RestaurarColor));
            Invoke(nameof(RestaurarColor), 0.12f);
        }

        return true;
    }

    private void RestaurarColor()
    {
        if (spriteRenderer != null)
            spriteRenderer.color = colorOriginal;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        ProcesarContacto(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        ProcesarContacto(otro);
    }

    private void OnCollisionEnter2D(Collision2D colision)
    {
        ProcesarContacto(colision.collider);
    }

    private void ProcesarContacto(Collider2D colisionador)
    {
        if (colisionador == null || Time.time < inmuneHasta)
            return;

        Jugador2D jugador = colisionador.GetComponentInParent<Jugador2D>();
        if (jugador == null)
            return;

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null || !vida.EstaVivo)
            return;

        Vector2? retorno = puntoRetornoSeguro != null ? (Vector2?)puntoRetornoSeguro.position : null;
        vida.RecibirDanioPeligroAmbiental(danio, retorno);
    }

    public void Configurar(int nuevoDanio, float nuevoTiempoInmunidad, Transform puntoSeguro = null)
    {
        danio = Mathf.Max(1, nuevoDanio);
        tiempoInmunidadPogo = Mathf.Max(0.05f, nuevoTiempoInmunidad);
        puntoRetornoSeguro = puntoSeguro;
    }
}
