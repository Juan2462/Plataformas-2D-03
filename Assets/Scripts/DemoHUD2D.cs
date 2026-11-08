using UnityEngine;

public class DemoHUD2D : MonoBehaviour
{
    [SerializeField] private Jugador2D jugador;
    [SerializeField] private VidaJugador2D vida;

    private bool demoCompletada;
    private bool derrotado;
    private bool dashDesbloqueadoAntes;

    private void Awake()
    {
        if (jugador == null)
            jugador = FindFirstObjectByType<Jugador2D>();

        if (jugador != null)
        {
            dashDesbloqueadoAntes = jugador.DashDesbloqueado;

            if (vida == null)
                vida = jugador.GetComponent<VidaJugador2D>();
        }
    }

    private void Start()
    {
        if (vida == null && jugador != null)
            vida = jugador.GetComponent<VidaJugador2D>();

        if (vida != null)
        {
            vida.JugadorDerrotado += AlDerrotarJugador;
            vida.VidaCambiada += AlCambiarVida;
        }
    }

    private void OnDisable()
    {
        if (vida != null)
        {
            vida.JugadorDerrotado -= AlDerrotarJugador;
            vida.VidaCambiada -= AlCambiarVida;
        }
    }

    private void Update()
    {
        if (jugador != null && jugador.DashDesbloqueado && !dashDesbloqueadoAntes)
            InterfazJuego2D.MostrarMensaje("DASH DESBLOQUEADO");

        if (jugador != null)
            dashDesbloqueadoAntes = jugador.DashDesbloqueado;
    }

    public void TerminarDemo()
    {
        if (demoCompletada || derrotado)
            return;

        demoCompletada = true;
        DetenerJugador();
        InterfazJuego2D.MostrarVictoria();
    }

    private void AlDerrotarJugador()
    {
        derrotado = true;
    }

    private void AlCambiarVida(int vidaActual, int vidaMaxima)
    {
        if (vidaActual <= 0 || !derrotado)
            return;

        derrotado = false;
        InterfazJuego2D.MostrarMensaje("HAS VUELTO AL ULTIMO PUNTO DE CONTROL");
    }

    public void ActivarPuntoControl()
    {
        InterfazJuego2D.MostrarMensaje("PUNTO DE CONTROL ACTIVADO");
    }

    private void DetenerJugador()
    {
        if (jugador == null)
            return;

        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        jugador.enabled = false;
    }

}
