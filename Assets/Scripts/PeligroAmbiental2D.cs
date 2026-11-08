using UnityEngine;

/// <summary>
/// Peligro ambiental con retorno seguro (estilo Hollow Knight).
/// Al entrar en contacto con el jugador, inflige dano y lo reubica
/// inmediatamente en su ultima posicion de suelo seguro registrada.
/// </summary>
[DisallowMultipleComponent]
public class PeligroAmbiental2D : MonoBehaviour
{
    [Header("Configuracion de Dano")]
    [SerializeField, Min(1)] private int danio = 1;
    [SerializeField] private Transform puntoRetornoManual;

    private void OnTriggerEnter2D(Collider2D otro)
    {
        ProcesarContacto(otro);
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        ProcesarContacto(otro);
    }

    private void OnCollisionEnter2D(Collision2D colision)
    {
        ProcesarContacto(colision.collider);
    }

    private void ProcesarContacto(Collider2D colisionador)
    {
        if (colisionador == null)
            return;

        VidaJugador2D vida = colisionador.GetComponentInParent<VidaJugador2D>();
        if (vida == null || !vida.EstaVivo)
            return;

        Vector2? retorno = puntoRetornoManual != null ? (Vector2?)puntoRetornoManual.position : null;
        vida.RecibirDanioPeligroAmbiental(danio, retorno);
    }

    public void Configurar(int nuevoDanio, Transform nuevoPuntoRetorno = null)
    {
        danio = Mathf.Max(1, nuevoDanio);
        puntoRetornoManual = nuevoPuntoRetorno;
    }
}
