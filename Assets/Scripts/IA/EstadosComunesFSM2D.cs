using UnityEngine;

// ================= ESTADO DE REPOSO / ESPERA =================
public class EstadoReposoEnemigo2D : IEstadoEnemigo2D
{
    private readonly MaquinaEstadosEnemigo2D fsm;
    private readonly Rigidbody2D rb;
    private readonly IEstadoEnemigo2D estadoSiguiente;
    private readonly float duracion;
    private float tiempoFin;

    public EstadoReposoEnemigo2D(MaquinaEstadosEnemigo2D fsm, Rigidbody2D rb, IEstadoEnemigo2D estadoSiguiente, float duracion = 1f)
    {
        this.fsm = fsm;
        this.rb = rb;
        this.estadoSiguiente = estadoSiguiente;
        this.duracion = duracion;
    }

    public void Entrar()
    {
        if (rb != null)
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

        tiempoFin = Time.time + duracion;
    }

    public void Actualizar()
    {
        if (Time.time >= tiempoFin && estadoSiguiente != null)
            fsm.CambiarEstado(estadoSiguiente);
    }

    public void ActualizarFisicas() { }
    public void Salir() { }
}

// ================= ESTADO DE ATURDIMIENTO (HITSTUN / POGO) =================
public class EstadoAturdido2D : IEstadoEnemigo2D
{
    private readonly MaquinaEstadosEnemigo2D fsm;
    private readonly Rigidbody2D rb;
    private readonly IEstadoEnemigo2D estadoSiguiente;
    private readonly float duracion;
    private float tiempoFin;

    public EstadoAturdido2D(MaquinaEstadosEnemigo2D fsm, Rigidbody2D rb, IEstadoEnemigo2D estadoSiguiente, float duracion = 0.25f)
    {
        this.fsm = fsm;
        this.rb = rb;
        this.estadoSiguiente = estadoSiguiente;
        this.duracion = duracion;
    }

    public void Entrar()
    {
        tiempoFin = Time.time + duracion;
    }

    public void Actualizar()
    {
        if (Time.time >= tiempoFin)
            fsm.CambiarEstado(estadoSiguiente);
    }

    public void ActualizarFisicas()
    {
        if (rb != null)
        {
            float velX = Mathf.Lerp(rb.linearVelocity.x, 0f, Time.fixedDeltaTime * 10f);
            rb.linearVelocity = new Vector2(velX, rb.linearVelocity.y);
        }
    }

    public void Salir() { }
}

// ================= ESTADO DE MUERTE =================
public class EstadoMuerteEnemigo2D : IEstadoEnemigo2D
{
    private readonly GameObject entidad;
    private readonly Rigidbody2D rb;
    private readonly Collider2D col;
    private readonly float retrasoDestruccion;

    public EstadoMuerteEnemigo2D(GameObject entidad, Rigidbody2D rb, Collider2D col, float retrasoDestruccion = 0.4f)
    {
        this.entidad = entidad;
        this.rb = rb;
        this.col = col;
        this.retrasoDestruccion = retrasoDestruccion;
    }

    public void Entrar()
    {
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        if (col != null)
            col.enabled = false;

        if (entidad != null)
            Object.Destroy(entidad, retrasoDestruccion);
    }

    public void Actualizar() { }
    public void ActualizarFisicas() { }
    public void Salir() { }
}
