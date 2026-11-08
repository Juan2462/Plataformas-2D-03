using UnityEngine;

public class Puerta2D : Interactuable2D
{
    [SerializeField] private string idLlaveRequerida;
    [SerializeField] private bool consumirLlave = true;
    [SerializeField] private Collider2D colisionadorPuerta;
    [SerializeField] private GameObject visualPuerta;
    [SerializeField] private Animator animator;
    [SerializeField] private string parametroAbrir = "Open";

    private bool abierta;

    private void Awake()
    {
        if (colisionadorPuerta == null)
            colisionadorPuerta = GetComponent<Collider2D>();

        if (visualPuerta == null)
            visualPuerta = gameObject;

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (abierta)
            return;

        if (inventario == null)
        {
            Debug.LogError("No se puede abrir la puerta sin un inventario.", this);
            return;
        }

        if (string.IsNullOrWhiteSpace(idLlaveRequerida))
        {
            Debug.LogError("Asigna el ID de la llave requerida en la puerta.", this);
            return;
        }

        if (!inventario.TieneLlave(idLlaveRequerida))
        {
            Debug.Log("La puerta está cerrada. Necesitas la llave: " + idLlaveRequerida, this);
            return;
        }

        if (consumirLlave && !inventario.IntentarUsarLlave(idLlaveRequerida))
            return;

        abierta = true;

        if (colisionadorPuerta != null)
            colisionadorPuerta.enabled = false;

        if (animator != null && !string.IsNullOrWhiteSpace(parametroAbrir))
            animator.SetTrigger(parametroAbrir);
        else if (visualPuerta != null)
            visualPuerta.SetActive(false);
    }
}
