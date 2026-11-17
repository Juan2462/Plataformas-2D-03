using UnityEngine;

[RequireComponent(typeof(MaquinaEstadosEnemigo2D), typeof(Rigidbody2D))]
public class EnemigoAcueductoFSM2D : MonoBehaviour, IDamageable2D
{
    [Header("Estadísticas")]
    [SerializeField] private int saludMaxima = 4;
    [SerializeField] private float velocidadPatrulla = 2.5f;
    [SerializeField] private float velocidadPersecucion = 5f;
    [SerializeField] private float rangoDeteccion = 7f;
    [SerializeField] private float rangoAtaque = 1.4f;
    [SerializeField] private LayerMask capaSuelo;
    [SerializeField] private Transform sensorBorde;

    public MaquinaEstadosEnemigo2D FSM { get; private set; }
    public Rigidbody2D RB { get; private set; }
    public Transform Jugador { get; private set; }
    public bool MirandoDerecha { get; private set; } = true;

    // Instancias de los estados reutilizables
    public EstadoPatrulla2D EstadoPatrulla { get; private set; }
    public EstadoPersecucion2D EstadoPersecucion { get; private set; }
    public EstadoAtaque2D EstadoAtaque { get; private set; }
    public EstadoAlertaMagia2D EstadoAlertaMagia { get; private set; }

    private int saludActual;

    private void Awake()
    {
        FSM = GetComponent<MaquinaEstadosEnemigo2D>();
        RB = GetComponent<Rigidbody2D>();
        saludActual = saludMaxima;

        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
            Jugador = objJugador.transform;

        // Creamos los estados pasándoles referencia a este controlador
        EstadoPatrulla = new EstadoPatrulla2D(this, velocidadPatrulla, sensorBorde, capaSuelo);
        EstadoPersecucion = new EstadoPersecucion2D(this, velocidadPersecucion, rangoDeteccion, rangoAtaque);
        EstadoAtaque = new EstadoAtaque2D(this, rangoAtaque);
        EstadoAlertaMagia = new EstadoAlertaMagia2D(this);
    }

    private void Start()
    {
        FSM.Inicializar(EstadoPatrulla);
    }

    public void Girar()
    {
        MirandoDerecha = !MirandoDerecha;
        Vector3 escala = transform.localScale;
        escala.x *= -1f;
        transform.localScale = escala;
    }

    public float DistanciaAlJugador()
    {
        if (Jugador == null) return float.PositiveInfinity;
        return Vector2.Distance(transform.position, Jugador.position);
    }

    public bool JugadorEnRangoDeteccion() => DistanciaAlJugador() <= rangoDeteccion;

    // Llamado cuando Sira lanza un hechizo cerca
    public void AlDetectarMagiaCercana()
    {
        if (FSM.EstadoActual != EstadoAtaque)
            FSM.CambiarEstado(EstadoAlertaMagia);
    }

    public bool RecibirDanio(int cantidad, Vector2 origenAtaque, float fuerzaRetroceso)
    {
        saludActual -= cantidad;
        Vector2 dirRetroceso = ((Vector2)transform.position - origenAtaque).normalized;
        RB.linearVelocity = new Vector2(dirRetroceso.x * fuerzaRetroceso, RB.linearVelocity.y);

        if (saludActual <= 0)
            Destroy(gameObject);

        return true;
    }
}

// ================= ESTADOS CONCRETOS =================

public class EstadoPatrulla2D : IEstadoEnemigo2D
{
    private readonly EnemigoAcueductoFSM2D enemigo;
    private readonly float velocidad;
    private readonly Transform sensorBorde;
    private readonly LayerMask capaSuelo;

    public EstadoPatrulla2D(EnemigoAcueductoFSM2D enemigo, float velocidad, Transform sensorBorde, LayerMask capaSuelo)
    {
        this.enemigo = enemigo;
        this.velocidad = velocidad;
        this.sensorBorde = sensorBorde;
        this.capaSuelo = capaSuelo;
    }

    public void Entrar() { }

    public void Actualizar()
    {
        if (enemigo.JugadorEnRangoDeteccion())
        {
            enemigo.FSM.CambiarEstado(enemigo.EstadoPersecucion);
            return;
        }

        if (sensorBorde != null)
        {
            bool haySueloDelante = Physics2D.Raycast(sensorBorde.position, Vector2.down, 0.5f, capaSuelo);
            if (!haySueloDelante)
                enemigo.Girar();
        }
    }

    public void ActualizarFisicas()
    {
        float dir = enemigo.MirandoDerecha ? 1f : -1f;
        enemigo.RB.linearVelocity = new Vector2(dir * velocidad, enemigo.RB.linearVelocity.y);
    }

    public void Salir() { }
}

public class EstadoPersecucion2D : IEstadoEnemigo2D
{
    private readonly EnemigoAcueductoFSM2D enemigo;
    private readonly float velocidad;
    private readonly float rangoDeteccion;
    private readonly float rangoAtaque;

    public EstadoPersecucion2D(EnemigoAcueductoFSM2D enemigo, float velocidad, float rangoDeteccion, float rangoAtaque)
    {
        this.enemigo = enemigo;
        this.velocidad = velocidad;
        this.rangoDeteccion = rangoDeteccion;
        this.rangoAtaque = rangoAtaque;
    }

    public void Entrar() { }

    public void Actualizar()
    {
        float dist = enemigo.DistanciaAlJugador();

        if (dist > rangoDeteccion * 1.3f)
        {
            enemigo.FSM.CambiarEstado(enemigo.EstadoPatrulla);
            return;
        }

        if (dist <= rangoAtaque)
        {
            enemigo.FSM.CambiarEstado(enemigo.EstadoAtaque);
            return;
        }

        float diffX = enemigo.Jugador.position.x - enemigo.transform.position.x;
        if ((diffX > 0.1f && !enemigo.MirandoDerecha) || (diffX < -0.1f && enemigo.MirandoDerecha))
            enemigo.Girar();
    }

    public void ActualizarFisicas()
    {
        float dir = enemigo.MirandoDerecha ? 1f : -1f;
        enemigo.RB.linearVelocity = new Vector2(dir * velocidad, enemigo.RB.linearVelocity.y);
    }

    public void Salir() { }
}

public class EstadoAtaque2D : IEstadoEnemigo2D
{
    private readonly EnemigoAcueductoFSM2D enemigo;
    private readonly float rangoAtaque;
    private float tiempoFinAtaque;

    public EstadoAtaque2D(EnemigoAcueductoFSM2D enemigo, float rangoAtaque)
    {
        this.enemigo = enemigo;
        this.rangoAtaque = rangoAtaque;
    }

    public void Entrar()
    {
        enemigo.RB.linearVelocity = new Vector2(0f, enemigo.RB.linearVelocity.y);
        tiempoFinAtaque = Time.time + 0.6f; // Duración del golpe/recuperación
        // Aquí disparas el trigger de animación de ataque y el hitbox
    }

    public void Actualizar()
    {
        if (Time.time >= tiempoFinAtaque)
        {
            if (enemigo.DistanciaAlJugador() <= rangoAtaque)
                Entrar(); // Vuelve a atacar si sigue pegado
            else
                enemigo.FSM.CambiarEstado(enemigo.EstadoPersecucion);
        }
    }

    public void ActualizarFisicas() { }
    public void Salir() { }
}

public class EstadoAlertaMagia2D : IEstadoEnemigo2D
{
    private readonly EnemigoAcueductoFSM2D enemigo;
    private float tiempoReaccion;

    public EstadoAlertaMagia2D(EnemigoAcueductoFSM2D enemigo)
    {
        this.enemigo = enemigo;
    }

    public void Entrar()
    {
        // Se frena un instante al detectar el hechizo y mira hacia Sira
        enemigo.RB.linearVelocity = new Vector2(0f, enemigo.RB.linearVelocity.y);
        tiempoReaccion = Time.time + 0.25f;

        if (enemigo.Jugador != null)
        {
            float diffX = enemigo.Jugador.position.x - enemigo.transform.position.x;
            if ((diffX > 0f && !enemigo.MirandoDerecha) || (diffX < 0f && enemigo.MirandoDerecha))
                enemigo.Girar();
        }
    }

    public void Actualizar()
    {
        if (Time.time >= tiempoReaccion)
        {
            // Tras detectar la magia, pasa directamente a persecución agresiva
            enemigo.FSM.CambiarEstado(enemigo.EstadoPersecucion);
        }
    }

    public void ActualizarFisicas() { }
    public void Salir() { }
}

///Aquí tienes un controlador completo que usa la FSM para alternar 
/// entre Patrullar, Perseguir, Atacar y Reaccionar a Magia
///Haciendo que todos tus enemigos (guardias de válvulas, 
/// centinelas de campanas, enemigos del acueducto) hereden de esta clase base,
///  todos soportarán automáticamente:

/// Detección de hechizos de Sira.

///Efectos alterados (como la lentitud de RutinaLentitud).

///  Retroceso e interfaz IDamageable2D.