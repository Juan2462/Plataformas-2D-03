using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public sealed class RecompensaGuardianDash2D : MonoBehaviour
{
    [SerializeField] private EnemigoPrototipo2D guardian;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;

    private bool guardianAsignado;
    private bool disponible;

    public EnemigoPrototipo2D Guardian => guardian;

    private void Awake()
    {
        guardianAsignado = guardian != null;
        if (!guardianAsignado)
        {
            Debug.LogError("La recompensa requiere una referencia al guardian.", this);
            enabled = false;
            return;
        }

        GetComponent<Collider2D>().isTrigger = true;
        GetComponent<Collider2D>().enabled = false;
    }

    private void Update()
    {
        if (disponible || !guardianAsignado)
            return;

        if (guardian != null && guardian.EstaVivo)
            return;

        disponible = true;
        GetComponent<Collider2D>().enabled = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (!disponible)
            return;

        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null)
            return;

        if (jugador.DesbloquearHabilidad(HabilidadExploracion2D.Dash))
            interfaz?.MostrarMensaje("Dash desbloqueado: Mayus + direccion horizontal.");

        disponible = false;
        GetComponent<Collider2D>().enabled = false;
        gameObject.SetActive(false);
    }
}
