using UnityEngine;

public class EnemigoTerrestrePrototipo2D : EnemigoPrototipo2D
{
    [SerializeField, Min(0.1f)] private float distanciaPatrulla = 2.5f;
    [SerializeField, Min(0.1f)] private float toleranciaAltura = 0.9f;

    protected override void ActualizarMovimiento()
    {
        if (estado == Estado.AnticipandoAtaque || estado == Estado.Atacando ||
            estado == Estado.Recuperandose || estado == Estado.RecibiendoDanio)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float direccion = 0f;
        if (estado == Estado.Detectando && jugador != null)
        {
            float diferencia = jugador.transform.position.x - transform.position.x;
            if (Mathf.Abs(diferencia) > rangoAtaque * 0.8f)
                direccion = Mathf.Sign(diferencia);
        }
        else
        {
            float desplazamiento = rb.position.x - posicionInicial.x;
            if (desplazamiento >= distanciaPatrulla)
                direccionMirada = -1f;
            else if (desplazamiento <= -distanciaPatrulla)
                direccionMirada = 1f;
            direccion = direccionMirada;
        }

        rb.linearVelocity = new Vector2(direccion * velocidad, rb.linearVelocity.y);
    }

    protected override void EjecutarAtaque()
    {
        DanarJugadorEnAlcance(rangoAtaque, toleranciaAltura);
    }
}
