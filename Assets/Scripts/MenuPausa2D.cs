using System.Text;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class MenuPausa2D : MonoBehaviour
{
    [Header("Paneles UI")]
    [SerializeField] private GameObject panelPausa;
    [SerializeField] private GameObject panelInventario;
    [SerializeField] private GameObject panelOpciones;

    [Header("Audio")]
    [SerializeField] private Slider sliderVolumenGeneral;

    [Header("Visualización de Inventario")]
    [SerializeField] private TextMeshProUGUI textoMonedas;
    [SerializeField] private TextMeshProUGUI textoListaObjetos;

    [Header("Escenas")]
    [SerializeField] private string nombreEscenaMenuPrincipal = "MenuPrincipal";

    private Jugador2D jugador;
    private InventarioJugador2D inventario;
    private bool estaPausado = false;

    private void Start()
    {
        jugador = FindFirstObjectByType<Jugador2D>();
        if (jugador != null)
            inventario = jugador.GetComponent<InventarioJugador2D>();

        if (panelPausa != null)
            panelPausa.SetActive(false);

        // Cargar volumen guardado
        float volumenGuardado = PlayerPrefs.GetFloat("VolumenGeneral", 1f);
        AudioListener.volume = volumenGuardado;
        if (sliderVolumenGeneral != null)
        {
            sliderVolumenGeneral.value = volumenGuardado;
            sliderVolumenGeneral.onValueChanged.AddListener(CambiarVolumen);
        }
    }

    private void Update()
    {
        // Detecta tecla Escape o botón Start del mando con el nuevo Input System
        bool teclaPausa = (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) ||
                          (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame);

        if (teclaPausa)
        {
            if (estaPausado) Reanudar();
            else Pausar();
        }
    }

    public void Pausar()
    {
        estaPausado = true;
        Time.timeScale = 0f;

        if (panelPausa != null) panelPausa.SetActive(true);
        AbrirPestanaInventario();
    }

    public void Reanudar()
    {
        estaPausado = false;
        Time.timeScale = 1f;

        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelInventario != null) panelInventario.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
    }

    public void AbrirPestanaInventario()
    {
        if (panelInventario != null) panelInventario.SetActive(true);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        ActualizarVistaInventario();
    }

    public void AbrirPestanaOpciones()
    {
        if (panelInventario != null) panelInventario.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(true);
    }

    private void ActualizarVistaInventario()
    {
        if (inventario == null) return;

        if (textoMonedas != null)
            textoMonedas.text = $"Ecos: {inventario.Monedas}";

        if (textoListaObjetos != null)
        {
            StringBuilder sb = new StringBuilder();

            sb.AppendLine("<b>--- OBJETOS Y RELIQUIAS ---</b>");
            if (inventario.Objetos.Count == 0) sb.AppendLine("<i>Ningún objeto adquirido</i>");
            foreach (var obj in inventario.Objetos)
                sb.AppendLine($"• {obj.Id} (x{obj.Cantidad})");

            sb.AppendLine("\n<b>--- LLAVES ---</b>");
            if (inventario.Llaves.Count == 0) sb.AppendLine("<i>Ninguna llave</i>");
            foreach (var llave in inventario.Llaves)
                sb.AppendLine($"• {llave.Id} (x{llave.Cantidad})");

            sb.AppendLine("\n<b>--- ARMAS ---</b>");
            foreach (var arma in inventario.Armas)
                sb.AppendLine($"• {arma.Id}");

            textoListaObjetos.text = sb.ToString();
        }
    }

    public void CambiarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat("VolumenGeneral", valor);
        PlayerPrefs.Save();
    }

    public void SalirAlMenuPrincipal()
    {
        Time.timeScale = 1f; // Fundamental restaurar el tiempo antes de cambiar de escena
        if (inventario != null)
            inventario.Guardar();

        SceneManager.LoadScene(nombreEscenaMenuPrincipal);
    }
}