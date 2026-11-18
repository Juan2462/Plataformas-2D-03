using System;
using UnityEngine;

// ================= ESTADO DE COMBATE POR FASES (ANTICIPACIÓN -> ATAQUE -> RECUPERACIÓN) =================
public class EstadoCicloAtaque2D : IEstadoEnemigo2D
{
    private readonly MaquinaEstadosEnemigo2D fsm;
    private readonly Rigidbody2D rb;
    private readonly float duracionAnticipacion;
    private readonly float duracionAtaque;
    private readonly float duracionRecuperacion;
    private readonly Action alIniciarAnticipacion;
    private readonly Action alEjecutarGolpe;
    private readonly Action alActualizarAtaque;
    private readonly Action alTerminarCiclo;
    private readonly IEstadoEnemigo2D estadoSiguiente;

    private int subFase; // 0: Anticipando, 1: Atacando, 2: Recuperando
    private float tiempoSiguienteSubFase;

    public int SubFaseActual => subFase;

    public EstadoCicloAtaque2D(
        MaquinaEstadosEnemigo2D fsm,
        Rigidbody2D rb,
        float duracionAnticipacion,
        float duracionAtaque,
        float duracionRecuperacion,
        Action alIniciarAnticipacion,
        Action alEjecutarGolpe,
        Action alActualizarAtaque,
        Action alTerminarCiclo,
        IEstadoEnemigo2D estadoSiguiente)
    {
        this.fsm = fsm;
        this.rb = rb;
        this.duracionAnticipacion = duracionAnticipacion;
        this.duracionAtaque = duracionAtaque;
        this.duracionRecuperacion = duracionRecuperacion;
        this.alIniciarAnticipacion = alIniciarAnticipacion;
        this.alEjecutarGolpe = alEjecutarGolpe;
        this.alActualizarAtaque = alActualizarAtaque;
        this.alTerminarCiclo = alTerminarCiclo;
        this.estadoSiguiente = estadoSiguiente;
    }

    public void Entrar()
    {
        subFase = 0;
        tiempoSiguienteSubFase = Time.time + Mathf.Max(0f, duracionAnticipacion);
        if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
        alIniciarAnticipacion?.Invoke();
    }

    public void Actualizar()
    {
        if (subFase == 1)
            alActualizarAtaque?.Invoke();

        if (Time.time < tiempoSiguienteSubFase)
            return;

        if (subFase == 0)
        {
            subFase = 1;
            tiempoSiguienteSubFase = Time.time + Mathf.Max(0.01f, duracionAtaque);
            alEjecutarGolpe?.Invoke();
        }
        else if (subFase == 1)
        {
            subFase = 2;
            tiempoSiguienteSubFase = Time.time + Mathf.Max(0f, duracionRecuperacion);
            if (rb != null) rb.linearVelocity = Vector2.zero;
        }
        else
        {
            alTerminarCiclo?.Invoke();
            fsm.CambiarEstado(estadoSiguiente);
        }
    }

    public void ActualizarFisicas() { }
    public void Salir() { }
}
