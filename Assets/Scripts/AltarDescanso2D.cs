using UnityEngine;

public class AltarDescanso2D : MonoBehaviour
{
    [Header("Configuración del Altar")]
    [SerializeField] private string idAltar = "Altar_Acueducto_Niebla";
    [SerializeField] private string textoIndicacion = "Presiona [E] para descansar en el Altar";
    [SerializeField] private GameObject efectoVisualDescanso;

    public string TextoIndicacion => textoIndicacion;
    public bool EstaInteractuableDisponible => true;

    // Método que se ejecuta al interactuar (conectado con tu sistema de input/interacción)
    public void Interactuar(Jugador2D jugador)
    {
        if (jugador == null) return;

        // 1. Curar al jugador y rellenar su resonancia al máximo
        jugador.RestaurarSaludYResonanciaCompleta();

        // 2. Guardar la posición de reaparición (CheckPoint) y el inventario
        GuardarPuntoControl(jugador);

        // 3. Efectos visuales y de sonido opcionales
        if (efectoVisualDescanso != null)
        {
            Instantiate(efectoVisualDescanso, transform.position, Quaternion.identity);
        }

        Debug.Log($"[Altar] Sira ha descansado en: {idAltar}. Partida guardada y estado restaurado.");
    }

    private void GuardarPuntoControl(Jugador2D jugador)
    {
        // Guarda la posición actual como el último punto de control activo
        PlayerPrefs.SetFloat("CheckPoint_X", transform.position.x);
        PlayerPrefs.SetFloat("CheckPoint_Y", transform.position.y);
        PlayerPrefs.SetString("UltimoAltar", idAltar);
        PlayerPrefs.Save();

        // Si tienes un gestor de inventario o datos globales, se invoca aquí su guardado
        InventarioJugador2D inventario = jugador.GetComponent<InventarioJugador2D>();
        if (inventario != null)
        {
            inventario.Guardar();
        }
    }
}

///Crea un objeto en la escena (por ejemplo, el sprite de una estatua o un banco antiguo
///  del Acueducto de la Niebla).

///Añade un BoxCollider2D marcado como Is Trigger para detectar cuando el jugador 
/// se acerque.

///Asigna el script AltarDescanso2D.cs a ese objeto.

///Cuando Sira se acerque y pulse la tecla de interacción,
///  el script rellenará su salud, recargará su resonancia y guardará
///  las coordenadas en los PlayerPrefs para que, si el jugador muere, 
/// pueda reaparecer directamente en este altar.