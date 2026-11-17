using UnityEngine;

public class CorrienteViento2D : MonoBehaviour
{
    [Header("Configuración del Viento")]
    [SerializeField] private Vector2 fuerzaViento = new Vector2(5f, 0f);

    private void OnTriggerStay2D(Collider2D otro)
    {
        if (otro.TryGetComponent<Rigidbody2D>(out var rb))
        {
            // Aplica una fuerza constante en la dirección del viento
            rb.AddForce(fuerzaViento, ForceMode2D.Force);
        }
    }
}

///Para simular ráfagas de aire en las zonas abiertas
///  del acueducto que empujen al jugador (hacia arriba, 
/// los lados, etc.), podemos crear un área de trigger con este script:
/// Ajustar la IA de los enemigos para que reaccionen cuando Sira utiliza magia