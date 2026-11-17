using UnityEngine;

[RequireComponent(typeof(MaquinaEstadosEnemigo2D), typeof(Rigidbody2D))]
public abstract class JefeBaseFSM2D : MonoBehaviour, IDamageable2D
{
    [Header("Configuración del Jefe")]
    [SerializeField] protected string nombreJefe = "Coloso del Acueducto";
    [SerializeField] protected int saludMaxima = 30;
    [SerializeField, Range(0.1f, 0.9f)] protected float umbralFase2 = 0.5f;

    public MaquinaEstadosEnemigo2D FSM { get; private set; }
    public Rigidbody2D RB { get; private set; }
    public Transform Jugador { get; private set; }
    public bool MirandoDerecha { get; protected set; } = false;

    protected int saludActual;
    protected bool enFase2 = false;

    protected IEstadoEnemigo2D estadoFase1;
    protected IEstadoEnemigo2D estadoTransicionFase2;
    protected IEstadoEnemigo2D estadoFase2;
    protected IEstadoEnemigo2D estadoDerrota;

    public float PorcentajeSalud => (float)saludActual / saludMaxima;

    protected virtual void Awake()
    {
        FSM = GetComponent<MaquinaEstadosEnemigo2D>();
        RB = GetComponent<Rigidbody2D>();
        saludActual = saludMaxima;

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
            Jugador = objJugador.transform;

        ConfigurarFasesJefe();
    }

    protected virtual void Start()
    {
        if (estadoFase1 != null)
            FSM.Inicializar(estadoFase1);
    }

    protected abstract void ConfigurarFasesJefe();

    public void MirarHaciaJugador()
    {
        if (Jugador == null) return;
        float diffX = Jugador.position.x - transform.position.x;
        if ((diffX > 0.1f && !MirandoDerecha) || (diffX < -0.1f && MirandoDerecha))
        {
            MirandoDerecha = !MirandoDerecha;
            Vector3 escala = transform.localScale;
            escala.x *= -1f;
            transform.localScale = escala;
        }
    }

    public virtual bool RecibirDanio(int cantidad, Vector2 origenAtaque, float fuerzaRetroceso)
    {
        if (saludActual <= 0) return false;

        saludActual = Mathf.Max(0, saludActual - cantidad);

        // Comprobación automática de cambio de fase
        if (!enFase2 && PorcentajeSalud <= umbralFase2 && saludActual > 0)
        {
            enFase2 = true;
            if (estadoTransicionFase2 != null)
                FSM.CambiarEstado(estadoTransicionFase2);
            else if (estadoFase2 != null)
                FSM.CambiarEstado(estadoFase2);
        }
        else if (saludActual <= 0 && estadoDerrota != null)
        {
            FSM.CambiarEstado(estadoDerrota);
        }

        return true;
    }
}

///Para los jefes de tus zonas (como el guardián de la Galería de Válvulas, 
/// el jefe del Canal de Campanas y el jefe del Acueducto de la Niebla), 
/// cada Fase de Combate se gestiona como un estado maestro que transiciona 
/// automáticamente según el porcentaje de vida