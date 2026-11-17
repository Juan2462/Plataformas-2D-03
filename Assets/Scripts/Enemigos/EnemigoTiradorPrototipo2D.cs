using UnityEngine;

public class EnemigoTiradorPrototipo2D : EnemigoPrototipo2D
{
    [SerializeField, Min(0.5f)] private float distanciaPreferida = 4f;
    [SerializeField, Min(0.1f)] private float toleranciaAltura = 1.5f;
    [SerializeField] private Color colorProyectil = new Color(1f, 0.75f, 0.24f);

    protected override void ActualizarMovimiento()
    {
        if (estado == Estado.Patrullar)
        {
            float direccion = Mathf.Sign(Mathf.Sin(Time.time * 0.7f));
            rb.linearVelocity = new Vector2(direccion * velocidad * 0.5f, rb.linearVelocity.y);
            return;
        }

        if (estado != Estado.Detectando || jugador == null)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float diferencia = jugador.transform.position.x - transform.position.x;
        direccionMirada = Mathf.Sign(diferencia);
        float distancia = Mathf.Abs(diferencia);
        float movimiento = distancia < distanciaPreferida * 0.7f
            ? -direccionMirada
            : distancia > distanciaPreferida * 1.25f ? direccionMirada : 0f;
        rb.linearVelocity = new Vector2(movimiento * velocidad, rb.linearVelocity.y);
    }

    protected override void EjecutarAtaque()
    {
        if (jugador == null || !jugador.isActiveAndEnabled)
            return;

        Vector2 origen = puntoDisparo != null ? puntoDisparo.position : transform.position;
        Vector2 direccion = (Vector2)jugador.transform.position - origen;
        if (Mathf.Abs(direccion.y) > toleranciaAltura)
            return;

        LanzarProyectil(direccion, colorProyectil);
    }
}
