using UnityEngine;

public sealed class SenalZona2D : MonoBehaviour
{
    [SerializeField] private string texto;
    [SerializeField] private Color color = new Color(0.76f, 0.94f, 0.88f);
    [SerializeField, Min(0.1f)] private float ancho = 210f;

    private void OnGUI()
    {
        Camera camara = Camera.main;
        if (camara == null)
            return;

        Vector3 punto = camara.WorldToScreenPoint(transform.position);
        if (punto.z <= 0f)
            return;

        float alto = 28f;
        Rect rect = new Rect(punto.x - ancho * 0.5f, Screen.height - punto.y - alto, ancho, alto);
        Color colorAnterior = GUI.color;
        GUI.color = color;
        GUI.Box(rect, texto);
        GUI.color = colorAnterior;
    }
}
