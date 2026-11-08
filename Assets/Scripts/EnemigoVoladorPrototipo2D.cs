using UnityEngine;

public class EnemigoVoladorPrototipo2D : EnemigoPrototipo2D
{
    [SerializeField, Min(0.1f)] private float amplitudFlotacion = 0.8f;
    [SerializeField, Min(0.1f)] private float velocidadFlotacion = 1.2f;
    [SerializeField, Min(0.1f)] private float velocidadPicado = 8f;
    [SerializeField, Min(0.1f)] private float radioImpacto = 0.8f;

    private Vector2 destinoPicado;
    private bool golpeAplicado;
    private float faseFlotacion;

    protected override void Awake()
    {
        base.Awake();
        rb.gravityScale = 0f;
        faseFlotacion = Random.Range(0f, 10f);
    }

    protected override void ActualizarMovimiento()
    {
        if (estado == Estado.Atacando)
        {
            Vector2 diferencia = destinoPicado - rb.position;
            rb.linearVelocity = diferencia.sqrMagnitude > 0.04f
                ? diferencia.normalized * velocidadPicado
                : Vector2.zero;

            if (!golpeAplicado && jugador != null &&
                ((Vector2)jugador.transform.position - rb.position).sqrMagnitude <= radioImpacto * radioImpacto)
            {
                DanarJugadorEnAlcance(radioImpacto, radioImpacto);
                golpeAplicado = true;
            }
            return;
        }

        if (estado == Estado.Detectando && jugador != null)
        {
            Vector2 diferencia = (Vector2)jugador.transform.position - rb.position;
            if (diferencia.sqrMagnitude > rangoAtaque * rangoAtaque)
            {
                rb.linearVelocity = Vector2.MoveTowards(
                    rb.linearVelocity,
                    diferencia.normalized * velocidad,
                    velocidad * Time.fixedDeltaTime
                );
            }
            else
            {
                rb.linearVelocity = Vector2.zero;
            }

            if (Mathf.Abs(diferencia.x) > 0.05f)
                direccionMirada = Mathf.Sign(diferencia.x);
            return;
        }

        Vector2 destino = posicionInicial + new Vector2(
            Mathf.Sin((Time.time + faseFlotacion) * velocidadFlotacion) * amplitudFlotacion,
            Mathf.Sin((Time.time + faseFlotacion) * velocidadFlotacion * 1.7f) * amplitudFlotacion * 0.45f
        );
        rb.linearVelocity = (destino - rb.position) * 2f;
    }

    protected override void EjecutarAtaque()
    {
        if (jugador == null || !jugador.isActiveAndEnabled)
            return;

        destinoPicado = jugador.transform.position;
        golpeAplicado = false;
    }

    protected override void AlCambiarEstado(Estado estadoAnterior, Estado estadoNuevo)
    {
        if (estadoNuevo == Estado.Atacando)
            golpeAplicado = false;
        else if (estadoAnterior == Estado.Atacando)
            rb.linearVelocity = Vector2.zero;
    }
}
