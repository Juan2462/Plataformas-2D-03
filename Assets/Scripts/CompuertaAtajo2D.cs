using System.Collections;
using UnityEngine;

/// <summary>
/// Compuerta hidraulica de atajo en la Galeria de Valvulas.
/// Bloquea el paso fisico entre el corredor inferior y la entrada hasta que Sira
/// acciona la valvula de derivacion desde el interior de la zona.
/// Al abrirse, guarda su estado en el progreso de la partida para mantenerse
/// permanentemente abierta tras descansos o transiciones.
/// </summary>
public sealed class CompuertaAtajo2D : Interactuable2D
{
    [Header("Identificadores")]
    [SerializeField] private string idZona = "galeria-valvulas";
    [SerializeField] private string idMecanismo = "compuerta-atajo-canal";

    [Header("Componentes de la Compuerta")]
    [SerializeField] private Collider2D colisionadorSolido;
    [SerializeField] private Transform panelCompuerta;
    [SerializeField] private InterfazZonaMetroidvania2D interfaz;

    [Header("Movimiento")]
    [SerializeField] private Vector2 desplazamientoApertura = new Vector2(0f, 3.2f);
    [SerializeField, Min(0.1f)] private float duracionApertura = 0.9f;

    [Header("Restriccion de Lado")]
    [SerializeField] private bool exigirLadoApertura = true;
    [SerializeField] private float coordenadaXMinimaApertura = 55f;

    public bool EstaAbierta { get; private set; }

    private Vector3 posicionInicialPanel;
    private Coroutine animacionCoroutine;

    private void Awake()
    {
        if (panelCompuerta != null)
            posicionInicialPanel = panelCompuerta.position;

        // Comprobar persistencia
        if (ProgresoPartida2D.EstacionActivada(idZona, idMecanismo))
        {
            AbrirInmediato();
        }
    }

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (inventario == null)
            return;

        Jugador2D jugador = inventario.GetComponent<Jugador2D>();
        if (jugador == null)
            return;

        if (EstaAbierta)
        {
            interfaz?.MostrarMensaje("La compuerta hidraulica ya esta abierta.");
            return;
        }

        if (exigirLadoApertura && jugador.transform.position.x < coordenadaXMinimaApertura)
        {
            interfaz?.MostrarMensaje("Compuerta trabada desde el interior por un cerrojo hidraulico.");
            return;
        }

        AbrirCompuerta();
    }

    public void AbrirCompuerta()
    {
        if (EstaAbierta)
            return;

        EstaAbierta = true;
        ProgresoPartida2D.RegistrarEstacion(idZona, idMecanismo);

        if (animacionCoroutine != null)
            StopCoroutine(animacionCoroutine);

        if (gameObject.activeInHierarchy)
            animacionCoroutine = StartCoroutine(AnimarApertura());
        else
            AbrirInmediato();

        interfaz?.MostrarMensaje("Valvula de derivacion accionada: atajo de compuerta abierto.");
    }

    private void AbrirInmediato()
    {
        EstaAbierta = true;
        if (colisionadorSolido != null)
            colisionadorSolido.enabled = false;

        if (panelCompuerta != null)
            panelCompuerta.position = posicionInicialPanel + (Vector3)desplazamientoApertura;
    }

    private IEnumerator AnimarApertura()
    {
        if (colisionadorSolido != null)
            colisionadorSolido.enabled = false;

        Vector3 origen = panelCompuerta != null ? panelCompuerta.position : Vector3.zero;
        Vector3 destino = posicionInicialPanel + (Vector3)desplazamientoApertura;
        float tiempo = 0f;

        while (tiempo < duracionApertura)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionApertura);
            if (panelCompuerta != null)
                panelCompuerta.position = Vector3.Lerp(origen, destino, t);
            yield return null;
        }

        if (panelCompuerta != null)
            panelCompuerta.position = destino;
        animacionCoroutine = null;
    }
}
