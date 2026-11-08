using UnityEngine;

public class DemoSalida2D : MonoBehaviour
{
    [SerializeField] private DemoHUD2D interfaz;

    private void Awake()
    {
        if (interfaz == null)
            interfaz = FindFirstObjectByType<DemoHUD2D>();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null || interfaz == null)
            return;

        interfaz.TerminarDemo();
    }
}
