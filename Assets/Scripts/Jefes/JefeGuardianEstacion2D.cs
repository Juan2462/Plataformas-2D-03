using UnityEngine;

public sealed class JefeGuardianEstacion2D : EnemigoPrototipo2D
{
    private enum Ataque
    {
        Golpe,
        Onda
    }

    [Header("Fases")]
    [SerializeField, Range(0.1f, 0.9f)] private float umbralSobrecarga = 0.5f;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;

    [Header("Ataques")]
    [SerializeField, Min(0.5f)] private float alcanceGolpe = 2.2f;
    [SerializeField, Min(0.2f)] private float toleranciaAlturaGolpe = 0.75f;
    [SerializeField, Min(1f)] private float alcanceOnda = 7f;
    [SerializeField] private Color colorAvisoGolpe = new Color(1f, 0.54f, 0.16f);
    [SerializeField] private Color colorAvisoOnda = new Color(0.24f, 0.91f, 1f);
    [SerializeField] private Color colorSobrecarga = new Color(0.63f, 0.88f, 0.77f);

    private Ataque ataquePreparado;
    private bool faseSobrecarga;
    private bool siguienteAtaqueEsOnda = true;
    private Vector3 escalaInicial;

    public bool EnSobrecarga => faseSobrecarga;
    public InterfazZonaMetroidvania2D Interfaz => interfaz;

    protected override void Awake()
    {
        base.Awake();
        escalaInicial = transform.localScale;
    }

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

        float multiplicadorVelocidad = faseSobrecarga ? 1.1f : 1f;
        rb.linearVelocity = new Vector2(direccion * velocidad * multiplicadorVelocidad, rb.linearVelocity.y);
    }

    protected override void EjecutarAtaque()
    {
        if (ataquePreparado == Ataque.Onda)
            LanzarProyectil(Vector2.right * direccionMirada, colorAvisoOnda, true, alcanceOnda);
        else
            DanarJugadorEnAlcance(alcanceGolpe, toleranciaAlturaGolpe);
    }

    public override bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        bool recibido = base.RecibirDanio(cantidad, posicionAtacante, retroceso);
        if (!recibido || !EstaVivo || faseSobrecarga ||
            VidaActual > Mathf.CeilToInt(vidaMaxima * umbralSobrecarga))
            return recibido;

        faseSobrecarga = true;
        anticipacionAtaque = Mathf.Max(0.35f, anticipacionAtaque * 0.8f);
        recuperacionAtaque = Mathf.Max(0.45f, recuperacionAtaque * 0.8f);
        tiempoEntreAtaques = Mathf.Max(1f, tiempoEntreAtaques * 0.85f);
        if (visual != null)
            visual.color = colorSobrecarga;

        interfaz?.MostrarMensaje(
            "El Guardian de la Estacion entra en sobrecarga. Salta sobre la onda de resonancia.",
            4f
        );
        return true;
    }

    protected override void AlCambiarEstado(Estado estadoAnterior, Estado estadoNuevo)
    {
        if (estadoNuevo == Estado.AnticipandoAtaque)
        {
            ataquePreparado = faseSobrecarga && siguienteAtaqueEsOnda
                ? Ataque.Onda
                : Ataque.Golpe;
            if (faseSobrecarga)
                siguienteAtaqueEsOnda = !siguienteAtaqueEsOnda;

            if (visual != null)
            {
                visual.color = ataquePreparado == Ataque.Onda
                    ? colorAvisoOnda
                    : colorAvisoGolpe;
                transform.localScale = escalaInicial * (ataquePreparado == Ataque.Onda ? 1.28f : 1.2f);
            }

            return;
        }

        if (faseSobrecarga && visual != null &&
            estadoNuevo != Estado.RecibiendoDanio &&
            estadoNuevo != Estado.Muerto)
            visual.color = colorSobrecarga;
    }
}
