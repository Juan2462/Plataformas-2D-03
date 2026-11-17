using UnityEngine;

public class PlataformaMovil2D : MonoBehaviour
{
    [SerializeField] private Transform[] puntosRuta;
    [SerializeField] private float velocidad = 3f;
    private int indiceActual = 0;

    private void Update()
    {
        if (puntosRuta == null || puntosRuta.Length == 0)
            return;

        Transform destino = puntosRuta[indiceActual];
        transform.position = Vector3.MoveTowards(transform.position, destino.position, velocidad * Time.deltaTime);

        if (Vector3.Distance(transform.position, destino.position) < 0.05f)
        {
            indiceActual = (indiceActual + 1) % puntosRuta.Length;
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(transform);
        }
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            collision.transform.SetParent(null);
        }
    }
}

///Este script hace que una plataforma se 
/// mueva cíclicamente entre varios puntos de ruta (waypoints).
///  Además, incluye la lógica para que el jugador se mueva junto 
/// con la plataforma al subirse a ella, evitando que resbale o se quede atrás.
/// Cómo usarlo en Unity:

   /// Crea un GameObject vacío en tu escena, ponle hijos vacíos
   ///  (Waypoint1, Waypoint2) para marcar el recorrido y asígnalos en el array
   ///  Puntos Ruta.

   /// Asegúrate de que tu prefab del jugador tiene la etiqueta Player.