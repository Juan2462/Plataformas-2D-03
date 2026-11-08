using UnityEngine;

public class DemoPuntoControl2D : MonoBehaviour
{
    [SerializeField] private DemoHUD2D interfaz;
    [SerializeField] private SpriteRenderer indicador;
    [SerializeField] private Color colorActivado = new Color(0.45f, 1f, 0.92f);

    private bool activado;

    private void Awake()
    {
        if (GetComponent<PuntoControlZona2D>() == null)
            gameObject.AddComponent<PuntoControlZona2D>();

        if (interfaz == null)
            interfaz = FindFirstObjectByType<DemoHUD2D>();

        if (indicador == null)
            indicador = GetComponentInChildren<SpriteRenderer>();
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        if (activado || otro.GetComponentInParent<Jugador2D>() == null)
            return;

        if (interfaz == null)
        {
            Debug.LogError("El punto de control necesita una interfaz DemoHUD2D.", this);
            return;
        }

        activado = true;
        if (indicador != null)
            indicador.color = colorActivado;

        interfaz.ActivarPuntoControl();
    }
}
