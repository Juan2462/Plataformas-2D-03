using UnityEngine;

public sealed class InterfazZonaMetroidvania2D : MonoBehaviour
{
    public void MostrarMensaje(string texto, float duracion = 3f)
    {
        InterfazJuego2D.MostrarMensaje(texto, duracion);
    }
}
