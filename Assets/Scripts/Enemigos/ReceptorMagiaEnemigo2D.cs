using UnityEngine;

public class ReceptorMagiaEnemigo2D : MonoBehaviour
{
    [Header("Configuración de Reacción")]
    [SerializeField] private float radioDeteccionMagia = 10f;
    private Transform jugador;
    private bool enModoAlerta = false;

    private void Start()
    {
        GameObject objJugador = GameObject.FindGameObjectWithTag("Player");
        if (objJugador != null)
            jugador = objJugador.transform;
    }

    // Método que puedes invocar desde el proyectil o desde el script del jugador al lanzar magia
    public void NotificarMagiaDetectada(Vector2 posicionMagia)
    {
        if (jugador == null) return;

        float distanciaAlEnemigo = Vector2.Distance(transform.position, posicionMagia);
        
        if (distanciaAlEnemigo <= radioDeteccionMagia)
        {
            ReaccionarAleaMagia();
        }
    }

    private void ReaccionarAleaMagia()
    {
        if (enModoAlerta) return;
        enModoAlerta = true;

        Debug.Log($"{gameObject.name} ha sentido la energía del hechizo y se pone en guardia.", this);

        // Aquí puedes cambiar el estado de la IA del enemigo:
        // Ej: Cambiar velocidad de persecución, activar un escudo temporal, o reproducir una animación de sorpresa
        
        // Ejemplo de cambio temporal de comportamiento:
        StopAllCoroutines();
        StartCoroutine(RutinaReaccionCombate());
    }

    private System.Collections.IEnumerator RutinaReaccionCombate()
    {
        // El enemigo se detiene un instante por la sorpresa y luego embiste con furia
        yield return new WaitForSeconds(0.4f);
        
        // Restablece la alerta tras unos segundos si pierde de vista al jugador
        yield return new WaitForSeconds(4f);
        enModoAlerta = false;
    }
}