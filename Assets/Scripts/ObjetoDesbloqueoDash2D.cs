using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ObjetoDesbloqueoDash2D : MonoBehaviour
{
    [SerializeField] private string mensaje = "Cristal obtenido: Dash desbloqueado.";

    private void Awake()
    {
        Collider2D colisionador = GetComponent<Collider2D>();
        colisionador.isTrigger = true;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null)
            return;

        if (jugador.DesbloquearHabilidad(HabilidadExploracion2D.Dash))
            Debug.Log(mensaje, this);

        Destroy(gameObject);
    }
}
