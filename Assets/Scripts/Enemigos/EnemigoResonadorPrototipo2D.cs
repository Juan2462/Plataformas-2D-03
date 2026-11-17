using UnityEngine;

public class EnemigoResonadorPrototipo2D : EnemigoPrototipo2D
{
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField, Min(0.1f)] private float fuerzaSalto = 5f;
    [SerializeField, Min(0.1f)] private float intervaloSaltos = 1.1f;
    [SerializeField, Min(0.5f)] private float alcanceOnda = 5f;
    [SerializeField] private Color colorOnda = new Color(0.35f, 0.95f, 1f);

    private float proximoSalto;

    protected override void ActualizarMovimiento()
    {
        if (estado == Estado.AnticipandoAtaque || estado == Estado.Atacando ||
            estado == Estado.Recuperandose || estado == Estado.RecibiendoDanio)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float direccion = estado == Estado.Detectando && jugador != null
            ? Mathf.Sign(jugador.transform.position.x - transform.position.x)
            : direccionMirada;
        float velocidadHorizontal = estado == Estado.Detectando ? velocidad * 0.7f : velocidad;
        rb.linearVelocity = new Vector2(direccion * velocidadHorizontal, rb.linearVelocity.y);

        bool enSuelo = Physics2D.Raycast(
            rb.position,
            Vector2.down,
            GetComponent<Collider2D>().bounds.extents.y + 0.08f,
            capaSuelo
        );
        if (enSuelo && Time.time >= proximoSalto)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, fuerzaSalto);
            proximoSalto = Time.time + intervaloSaltos;
        }

        if (estado == Estado.Patrullar &&
            Mathf.Abs(rb.position.x - posicionInicial.x) >= rangoDeteccion * 0.45f)
            direccionMirada *= -1f;
    }

    protected override void EjecutarAtaque()
    {
        LanzarProyectil(Vector2.right * direccionMirada, colorOnda, true, alcanceOnda);
    }
}
