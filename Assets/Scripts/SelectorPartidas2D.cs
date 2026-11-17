using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class SelectorPartidas2D : MonoBehaviour
{
    [Header("Configuración de Escena")]
    [SerializeField] private string escenaInicialJuego = "AcueductoNiebla";

    [Header("Textos de Ranuras (Slots)")]
    [SerializeField] private TextMeshProUGUI textoInfoRanura1;
    [SerializeField] private TextMeshProUGUI textoInfoRanura2;
    [SerializeField] private TextMeshProUGUI textoInfoRanura3;

    private void Start()
    {
        Time.timeScale = 1f;
        ActualizarTextosRanuras();
    }

    public void ActualizarTextosRanuras()
    {
        MostrarEstadoRanura(1, textoInfoRanura1);
        MostrarEstadoRanura(2, textoInfoRanura2);
        MostrarEstadoRanura(3, textoInfoRanura3);
    }

    private void MostrarEstadoRanura(int numeroRanura, TextMeshProUGUI textoUI)
    {
        if (textoUI == null) return;

        string claveAltar = $"Ranura_{numeroRanura}_UltimoAltar";
        if (PlayerPrefs.HasKey(claveAltar))
        {
            string zona = PlayerPrefs.GetString(claveAltar);
            textoUI.text = $"Partida {numeroRanura}\n<size=75%>Último descanso: {zona}</size>";
        }
        else
        {
            textoUI.text = $"Partida {numeroRanura}\n<size=75%>[ Ranura Vacía - Nueva Partida ]</size>";
        }
    }

    public void SeleccionarYIniciarRanura(int numeroRanura)
    {
        PlayerPrefs.SetInt("RanuraActiva", numeroRanura);
        PlayerPrefs.Save();
        SceneManager.LoadScene(escenaInicialJuego);
    }

    public void BorrarRanura(int numeroRanura)
    {
        PlayerPrefs.DeleteKey($"Ranura_{numeroRanura}_UltimoAltar");
        PlayerPrefs.DeleteKey($"Ranura_{numeroRanura}_CheckPoint_X");
        PlayerPrefs.DeleteKey($"Ranura_{numeroRanura}_CheckPoint_Y");
        PlayerPrefs.DeleteKey($"InventarioJugador2D_Ranura_{numeroRanura}");
        PlayerPrefs.Save();

        ActualizarTextosRanuras();
    }
}

///Configuración rápida en el Editor de UnityElementoConfiguración 
/// en InspectorBotón ReanudarEvento OnClick() $\rightarrow$ MenuPausa2D.
/// Reanudar()Botón InventarioEvento OnClick() $\rightarrow$ MenuPausa2D.
/// AbrirPestanaInventario()Botón Audio / OpcionesEvento OnClick() $\rightarrow$ 
/// MenuPausa2D.AbrirPestanaOpciones()Botón Salir al MenúEvento OnClick() $\rightarrow$
///  MenuPausa2D.SalirAlMenuPrincipal()Botón Ranura 1 / 2 / 3Evento OnClick() $\rightarrow$
///  SelectorPartidas2D.SeleccionarYIniciarRanura(1) (o 2 / 3)