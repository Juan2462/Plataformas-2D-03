using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class EnemigosPrototipoHUD2D : MonoBehaviour
{
    private readonly List<EnemigoPrototipo2D> enemigos = new List<EnemigoPrototipo2D>();
    private GUIStyle estilo;

    private void Start()
    {
        enemigos.AddRange(FindObjectsByType<EnemigoPrototipo2D>(FindObjectsSortMode.None));
        enemigos.Sort((a, b) => string.CompareOrdinal(a.name, b.name));
    }

    private void OnGUI()
    {
        if (estilo == null)
        {
            estilo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                normal = { textColor = Color.white },
                wordWrap = true
            };
        }

        StringBuilder texto = new StringBuilder(
            "DEMO DE ENEMIGOS  |  A/D mover  |  Espacio saltar  |  Intro atacar"
        );
        foreach (EnemigoPrototipo2D enemigo in enemigos)
        {
            if (enemigo != null)
                texto.Append("\n").Append(etiqueta(enemigo)).Append(": ")
                    .Append(nombreEstado(enemigo.EstadoActual))
                    .Append("  ").Append(enemigo.VidaActual).Append("/").Append(enemigo.VidaMaxima);
        }

        GUI.Box(new Rect(12f, 12f, Mathf.Min(620f, Screen.width - 24f), 118f), GUIContent.none);
        GUI.Label(new Rect(22f, 18f, Mathf.Min(600f, Screen.width - 44f), 108f), texto.ToString(), estilo);
    }

    private static string etiqueta(EnemigoPrototipo2D enemigo)
    {
        if (enemigo is EnemigoTerrestrePrototipo2D)
            return "Terrestre";
        if (enemigo is EnemigoTiradorPrototipo2D)
            return "Tirador";
        if (enemigo is EnemigoRondaPlataformas2D)
            return "Rondador";
        if (enemigo is EnemigoVoladorPrototipo2D)
            return "Volador";
        if (enemigo is EnemigoResonadorPrototipo2D)
            return "Resonador";
        return enemigo.name;
    }

    private static string nombreEstado(EnemigoPrototipo2D.Estado estado)
    {
        switch (estado)
        {
            case EnemigoPrototipo2D.Estado.Patrullar:
                return "patrullando";
            case EnemigoPrototipo2D.Estado.Detectando:
                return "detectando";
            case EnemigoPrototipo2D.Estado.AnticipandoAtaque:
                return "aviso de ataque";
            case EnemigoPrototipo2D.Estado.Atacando:
                return "atacando";
            case EnemigoPrototipo2D.Estado.Recuperandose:
                return "recuperandose";
            case EnemigoPrototipo2D.Estado.RecibiendoDanio:
                return "recibiendo dano";
            case EnemigoPrototipo2D.Estado.Muerto:
                return "derrotado";
            default:
                return estado.ToString();
        }
    }
}
