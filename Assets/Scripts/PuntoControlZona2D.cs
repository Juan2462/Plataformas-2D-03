using UnityEngine;
using UnityEngine.SceneManagement;

[RequireComponent(typeof(Collider2D))]
public sealed class PuntoControlZona2D : MonoBehaviour
{
    [SerializeField] private Transform puntoReaparicion;
    [SerializeField] private ZonaMetroidvania2D zona;
    [SerializeField] private string idPuntoControl;
    [SerializeField] private SpriteRenderer indicador;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;

    private Jugador2D ultimoJugadorActivado;
    private bool activado;

    private void Awake()
    {
        GetComponent<Collider2D>().isTrigger = true;
        if (zona == null)
            zona = GetComponentInParent<ZonaMetroidvania2D>();
        if (puntoReaparicion == null)
            puntoReaparicion = transform;
        if (indicador == null)
            indicador = GetComponentInChildren<SpriteRenderer>();
        ActualizarIndicador();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null || jugador == ultimoJugadorActivado)
            return;

        ultimoJugadorActivado = jugador;
        Activar(jugador);
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == ultimoJugadorActivado)
            ultimoJugadorActivado = null;
    }

    private void Activar(Jugador2D jugador)
    {
        if (puntoReaparicion == null)
        {
            Debug.LogError("Asigna el punto de reaparicion del checkpoint.", this);
            return;
        }

        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            Debug.LogError("El checkpoint necesita VidaJugador2D en el jugador.", jugador);
            return;
        }

        Scene escena = gameObject.scene;
        if (!GestorReaparicion2D.EscenaDisponibleEnBuildSettings(escena.path))
        {
            Debug.LogWarning("No se activo el checkpoint porque su escena no esta incluida en Build Settings: " +
                             escena.path, this);
            interfaz?.MostrarMensaje("Checkpoint no disponible: incluye esta escena en Build Settings.");
            return;
        }

        string idZona = zona != null ? zona.IdZona : escena.name;
        string idPunto = string.IsNullOrWhiteSpace(idPuntoControl)
            ? gameObject.name
            : idPuntoControl;

        ProgresoPartida2D.GuardarPuntoControl(
            escena.path,
            idZona,
            idPunto,
            puntoReaparicion.position
        );
        ProgresoPartida2D.RegistrarEstacion(idZona, idPunto);
        ActualizarIndicador();
        vida.RespawnearEnPuntoControl(puntoReaparicion.position);

        interfaz?.MostrarMensaje("Punto de retorno activado.");
    }

    private void ActualizarIndicador()
    {
        if (indicador == null)
            return;

        Scene escena = gameObject.scene;
        string idZona = zona != null ? zona.IdZona : escena.name;
        string idPunto = string.IsNullOrWhiteSpace(idPuntoControl)
            ? gameObject.name
            : idPuntoControl;
        activado = ProgresoPartida2D.EstacionActivada(idZona, idPunto);
        if (activado)
            indicador.color = new Color(0.47f, 1f, 0.82f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = activado ? Color.green : Color.cyan;
        if (puntoReaparicion != null)
            Gizmos.DrawWireSphere(puntoReaparicion.position, 0.35f);
    }
}
