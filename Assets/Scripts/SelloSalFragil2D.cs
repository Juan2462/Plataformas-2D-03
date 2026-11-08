using UnityEngine;

/// <summary>
/// Bloque o plataforma de sal fragil.
/// Soporta el peso del jugador y los saltos normales, pero se quiebra
/// y destruye al recibir el impacto de una Descarga de Presion (Ground Pound).
/// </summary>
[DisallowMultipleComponent]
public class SelloSalFragil2D : MonoBehaviour
{
    [SerializeField] private Collider2D colisionadorSolido;
    [SerializeField] private SpriteRenderer visual;
    [SerializeField] private string idSello;
    [SerializeField] private string idZona;

    public bool EstaRoto { get; private set; }

    private void Awake()
    {
        if (colisionadorSolido == null)
            colisionadorSolido = GetComponent<Collider2D>();
        if (visual == null)
            visual = GetComponent<SpriteRenderer>();

        if (!string.IsNullOrWhiteSpace(idZona) && !string.IsNullOrWhiteSpace(idSello))
        {
            if (ProgresoPartida2D.EstacionActivada(idZona, idSello))
                RomperInmediato();
        }
    }

    public bool AlRecibirDescargaPresion(Jugador2D jugador)
    {
        if (EstaRoto)
            return false;

        Romper();
        return true;
    }

    public void Romper()
    {
        EstaRoto = true;
        if (!string.IsNullOrWhiteSpace(idZona) && !string.IsNullOrWhiteSpace(idSello))
            ProgresoPartida2D.RegistrarEstacion(idZona, idSello);

        if (colisionadorSolido != null)
            colisionadorSolido.enabled = false;

        if (visual != null)
            visual.enabled = false;

        Destroy(gameObject, 0.1f);
    }

    private void RomperInmediato()
    {
        EstaRoto = true;
        if (colisionadorSolido != null)
            colisionadorSolido.enabled = false;
        if (visual != null)
            visual.enabled = false;
    }

    public void Configurar(string zona, string sello)
    {
        idZona = zona;
        idSello = sello;
    }
}
