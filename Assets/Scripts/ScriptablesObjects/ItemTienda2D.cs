using UnityEngine;

[CreateAssetMenu(fileName = "NuevoItemTienda", menuName = "Juego2D/Item de Tienda")]
public class ItemTienda2D : ScriptableObject
{
    public string nombreItem = "Fragmento de Máscara";
    [TextArea(2, 4)] public string descripcion = "Aumenta la salud máxima del jugador.";
    public int costoEcos = 150;
    public Sprite iconoItem;
    public bool esUnico = true; // Si ya se compró, no se puede volver a comprar
    [HideInInspector] public bool comprado = false;
}

///Este ScriptableObject te permitirá crear diferentes objetos 
/// (como mejoras de vida, llaves o reliquias) directamente desde
///  el menú de Unity haciendo clic derecho en la ventana 
/// Project -> Create -> Juego2D -> Item de Tienda