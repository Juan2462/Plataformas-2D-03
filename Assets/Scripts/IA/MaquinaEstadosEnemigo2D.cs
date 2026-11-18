using UnityEngine;

public class MaquinaEstadosEnemigo2D : MonoBehaviour
{
    public IEstadoEnemigo2D EstadoActual { get; private set; }

    public void Inicializar(IEstadoEnemigo2D estadoInicial)
    {
        EstadoActual = estadoInicial;
        EstadoActual?.Entrar();
    }

    public void CambiarEstado(IEstadoEnemigo2D nuevoEstado)
    {
        if (nuevoEstado == null || nuevoEstado == EstadoActual)
            return;

        EstadoActual?.Salir();
        EstadoActual = nuevoEstado;
        EstadoActual.Entrar();
    }

    private void Update()
    {
        EstadoActual?.Actualizar();
    }

    private void FixedUpdate()
    {
        EstadoActual?.ActualizarFisicas();
    }
}

///El Controlador de la Máquina de Estados (MaquinaEstadosEnemigo2D.cs)

///Este componente se añade al GameObject del enemigo y gestiona
///  la transición limpia entre un estado y otro.