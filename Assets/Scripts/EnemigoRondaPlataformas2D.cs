using UnityEngine;

public class EnemigoRondaPlataformas2D : EnemigoPrototipo2D
{
    [SerializeField] private Transform[] puntosRuta;
    [SerializeField, Min(0.05f)] private float distanciaLlegada = 0.12f;
    [SerializeField, Min(0.1f)] private float toleranciaAltura = 1.1f;

    private int indiceRuta;

    protected override void Awake()
    {
        base.Awake();
        if (puntosRuta == null || puntosRuta.Length == 0)
            Debug.LogWarning("Asigna puntos de ruta para patrullar alrededor de la plataforma.", this);
    }

    protected override void ActualizarMovimiento()
    {
        if (estado == Estado.AnticipandoAtaque || estado == Estado.Atacando ||
            estado == Estado.Recuperandose || estado == Estado.RecibiendoDanio ||
            puntosRuta == null || puntosRuta.Length == 0)
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        Transform punto = puntosRuta[indiceRuta];
        if (punto == null)
        {
            indiceRuta = (indiceRuta + 1) % puntosRuta.Length;
            return;
        }

        Vector2 diferencia = (Vector2)punto.position - rb.position;
        if (diferencia.magnitude <= distanciaLlegada)
        {
            indiceRuta = (indiceRuta + 1) % puntosRuta.Length;
            punto = puntosRuta[indiceRuta];
            if (punto == null)
                return;
            diferencia = (Vector2)punto.position - rb.position;
        }

        if (Mathf.Abs(diferencia.x) > 0.05f)
            direccionMirada = Mathf.Sign(diferencia.x);
        rb.linearVelocity = diferencia.normalized * velocidad;
    }

    protected override void EjecutarAtaque()
    {
        DanarJugadorEnAlcance(rangoAtaque, toleranciaAltura);
    }
}
