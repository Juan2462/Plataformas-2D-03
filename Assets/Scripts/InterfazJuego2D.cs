using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class InterfazJuego2D : MonoBehaviour
{
    private const float TamanoCeldaMapa = 4f;
    private const string ClaveVolumen = "RuinasEco_Opciones_Volumen";
    private const string ClavePantallaCompleta = "RuinasEco_Opciones_PantallaCompleta";
    private const string ClaveReducirDestellos = "RuinasEco_Opciones_ReducirDestellos";
    private const string ClaveResolucionAncho = "RuinasEco_Opciones_ResolucionAncho";
    private const string ClaveResolucionAlto = "RuinasEco_Opciones_ResolucionAlto";

    private static InterfazJuego2D instancia;
    private static string mensajePendiente;
    private static float duracionMensajePendiente;

    private readonly List<InputActionReference> referenciasAcciones =
        new List<InputActionReference>();
    private readonly List<RectTransform> celdasMapa = new List<RectTransform>();
    private readonly List<Resolution> resoluciones = new List<Resolution>();

    private Canvas canvas;
    private GameObject hud;
    private GameObject panelPausa;
    private GameObject panelOpciones;
    private GameObject panelMapa;
    private GameObject panelResultado;
    private GameObject panelDialogo;
    private RectTransform areaMapa;
    private Text tituloZona;
    private Text textoVida;
    private Text textoRecursos;
    private Text textoHabilidad;
    private Text textoMensaje;
    private Text textoAyuda;
    private Text textoResultado;
    private Text textoResolucion;
    private Text textoIndicacionInteraccion;
    private Text textoNombreDialogo;
    private Text textoContenidoDialogo;
    private Text textoAyudaDialogo;
    private Slider barraVida;
    private Slider controlVolumen;
    private Toggle controlPantallaCompleta;
    private Toggle controlReducirDestellos;
    private Button botonMapa;
    private Button botonReintentar;
    private InputSystemUIInputModule moduloUI;
    private EventSystem sistemaEventos;
    private InputActionAsset accionesEntrada;
    private InputAction accionMapa;
    private InputAction accionCancelar;
    private InputAction accionSubmit;
    private Jugador2D jugador;
    private Jugador2D jugadorDialogo;
    private PersonajeSecundario2D personajeDialogo;
    private string[] lineasDialogo;
    private VidaJugador2D vida;
    private InventarioJugador2D inventario;
    private ZonaMetroidvania2D zona;
    private Coroutine vincularEscena;
    private string mensajeActual;
    private float mensajeHasta;
    private float siguienteActualizacionHUD;
    private float escalaAntesDePausa = 1f;
    private float escalaAntesDeDialogo = 1f;
    private float caracteresMostrados;
    private int indiceResolucion;
    private int indiceLineaDialogo;
    private Vector2Int ultimaCeldaMapa = new Vector2Int(int.MinValue, int.MinValue);
    private bool pausaAbierta;
    private bool dialogoAbierto;
    private bool resultadoVictoria;
    private bool resultadoDerrota;

    public static bool ReducirDestellos =>
        PlayerPrefs.GetInt(ClaveReducirDestellos, 0) != 0;

    public static bool DialogoAbierto => instancia != null && instancia.dialogoAbierto;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearInterfaz()
    {
        if (instancia != null)
            return;

        new GameObject("Interfaz de juego").AddComponent<InterfazJuego2D>();
    }

    public static void MostrarMensaje(string mensaje, float duracion = 3f)
    {
        if (instancia == null)
        {
            mensajePendiente = mensaje;
            duracionMensajePendiente = duracion;
            return;
        }

        instancia.MostrarMensajeLocal(mensaje, duracion);
    }

    public static void MostrarVictoria()
    {
        instancia?.MostrarResultado(true);
    }

    public static bool MostrarDialogo(PersonajeSecundario2D personaje, Jugador2D jugador)
    {
        return instancia != null && instancia.AbrirDialogo(personaje, jugador);
    }

    public static void CerrarDialogoDe(Jugador2D jugador)
    {
        if (instancia != null && instancia.jugadorDialogo == jugador)
            instancia.CerrarDialogo();
    }

    public static void MostrarIndicacionInteraccion(string texto)
    {
        if (instancia == null || instancia.textoIndicacionInteraccion == null)
            return;

        bool visible = !string.IsNullOrWhiteSpace(texto) &&
                       !instancia.dialogoAbierto &&
                       !instancia.pausaAbierta &&
                       !instancia.resultadoVictoria &&
                       !instancia.resultadoDerrota;
        instancia.textoIndicacionInteraccion.gameObject.SetActive(visible);
        if (visible)
            instancia.textoIndicacionInteraccion.text = texto;
    }

    public static void ConfigurarEscaladoCanvas(CanvasScaler escalador)
    {
        if (escalador == null)
            throw new ArgumentNullException(nameof(escalador));

        escalador.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        escalador.referenceResolution = new Vector2(1600f, 900f);
        escalador.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        escalador.matchWidthOrHeight = 0.5f;
    }

    private void Awake()
    {
        if (instancia != null && instancia != this)
        {
            Destroy(gameObject);
            return;
        }

        instancia = this;
        DontDestroyOnLoad(gameObject);
        AplicarOpcionesGuardadas();
        CrearCanvas();
        CrearEventSystem();
        CrearHUD();
        CrearPanelPausa();
        CrearPanelOpciones();
        CrearPanelMapa();
        CrearPanelResultado();
        CrearPanelDialogo();
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void Start()
    {
        PrepararVinculacion();
        if (!string.IsNullOrEmpty(mensajePendiente))
        {
            MostrarMensajeLocal(mensajePendiente, duracionMensajePendiente);
            mensajePendiente = null;
        }
    }

    private void OnDestroy()
    {
        if (instancia != this)
            return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        DesvincularJugador();
        DesvincularAcciones();
        CerrarDialogo();
        instancia = null;
    }

    private void Update()
    {
        ActualizarDialogo();
        if (jugador == null)
            return;

        if (Time.unscaledTime >= siguienteActualizacionHUD)
        {
            siguienteActualizacionHUD = Time.unscaledTime + 0.15f;
            ActualizarHUD();
            RegistrarExploracion();
        }

        if (textoMensaje != null)
        {
            bool mostrar = Time.unscaledTime < mensajeHasta;
            textoMensaje.gameObject.SetActive(mostrar);
            if (mostrar)
                textoMensaje.text = mensajeActual;
        }
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        CerrarDialogo();
        Time.timeScale = 1f;
        pausaAbierta = false;
        resultadoVictoria = false;
        resultadoDerrota = false;
        if (panelPausa != null)
            panelPausa.SetActive(false);
        if (panelOpciones != null)
            panelOpciones.SetActive(false);
        if (panelMapa != null)
            panelMapa.SetActive(false);
        if (panelResultado != null)
            panelResultado.SetActive(false);
        if (sistemaEventos == null)
            CrearEventSystem();

        PrepararVinculacion();
    }

    private void PrepararVinculacion()
    {
        if (vincularEscena != null)
            StopCoroutine(vincularEscena);
        vincularEscena = StartCoroutine(VincularEscena());
    }

    private IEnumerator VincularEscena()
    {
        yield return null;
        DesvincularJugador();

        jugador = FindFirstObjectByType<Jugador2D>();
        zona = FindFirstObjectByType<ZonaMetroidvania2D>();
        bool escenaJugable = jugador != null &&
            (zona != null || FindFirstObjectByType<DemoHUD2D>() != null);
        hud.SetActive(escenaJugable);
        if (jugador == null)
        {
            vincularEscena = null;
            yield break;
        }
        botonMapa.gameObject.SetActive(escenaJugable && zona != null);
        textoAyuda.text = zona != null
            ? "TECLADO: A/D mover · ESPACIO saltar · ENTER atacar · E interactuar · TAB mapa · ESC pausa\nMANDO: stick izquierdo mover · START pausa · SELECT mapa · Aceptar/Atras navegan menus"
            : "TECLADO: A/D mover · ESPACIO saltar · ENTER atacar · E interactuar · ESC pausa\nMANDO: stick izquierdo mover · START pausa · Aceptar/Atras navegan menus";

        vida = jugador.GetComponent<VidaJugador2D>();
        inventario = jugador.GetComponent<InventarioJugador2D>();
        if (vida == null)
            Debug.LogError("La interfaz de juego necesita VidaJugador2D en el jugador.", jugador);
        else
        {
            vida.VidaCambiada += AlCambiarVida;
            vida.JugadorDerrotado += AlDerrotarJugador;
        }

        if (jugador.accionesEntrada != null)
            ConfigurarAcciones(jugador.accionesEntrada);
        else
            Debug.LogError("La interfaz necesita el InputActionAsset del jugador.", jugador);

        if (!escenaJugable)
        {
            vincularEscena = null;
            yield break;
        }

        ultimaCeldaMapa = new Vector2Int(int.MinValue, int.MinValue);
        ActualizarHUD();
        RegistrarExploracion();
        vincularEscena = null;
    }

    private void DesvincularJugador()
    {
        if (jugadorDialogo == jugador)
            CerrarDialogo();

        if (vida != null)
        {
            vida.VidaCambiada -= AlCambiarVida;
            vida.JugadorDerrotado -= AlDerrotarJugador;
        }

        jugador = null;
        vida = null;
        inventario = null;
        zona = null;
    }

    private void ConfigurarAcciones(InputActionAsset acciones)
    {
        if (accionesEntrada == acciones)
            return;

        DesvincularAcciones();
        accionesEntrada = acciones;

        accionMapa = acciones.FindAction("UI/Map", false);
        accionCancelar = acciones.FindAction("UI/Cancel", false);
        accionSubmit = acciones.FindAction("UI/Submit", false);
        if (accionMapa == null || accionCancelar == null || accionSubmit == null)
        {
            Debug.LogError("El mapa UI necesita las acciones Map, Cancel y Submit.", this);
            return;
        }

        accionMapa.performed += AlPulsarMapa;
        accionCancelar.performed += AlPulsarCancelar;
        accionSubmit.performed += AlPulsarSubmit;

        moduloUI.actionsAsset = acciones;
        moduloUI.point = ReferenciaAccion(acciones, "UI/Point");
        moduloUI.leftClick = ReferenciaAccion(acciones, "UI/Click");
        moduloUI.rightClick = ReferenciaAccion(acciones, "UI/RightClick");
        moduloUI.middleClick = ReferenciaAccion(acciones, "UI/MiddleClick");
        moduloUI.scrollWheel = ReferenciaAccion(acciones, "UI/ScrollWheel");
        moduloUI.move = ReferenciaAccion(acciones, "UI/Navigate");
        moduloUI.submit = ReferenciaAccion(acciones, "UI/Submit");
        moduloUI.cancel = ReferenciaAccion(acciones, "UI/Cancel");
        acciones.FindActionMap("UI", false).Enable();
    }

    private InputActionReference ReferenciaAccion(InputActionAsset acciones, string ruta)
    {
        InputAction accion = acciones.FindAction(ruta, false);
        if (accion == null)
        {
            Debug.LogError("Falta la accion de interfaz " + ruta + ".", this);
            return null;
        }

        InputActionReference referencia = InputActionReference.Create(accion);
        referenciasAcciones.Add(referencia);
        return referencia;
    }

    private void DesvincularAcciones()
    {
        if (accionesEntrada != null)
            accionesEntrada.FindActionMap("UI", false)?.Disable();

        if (accionMapa != null)
            accionMapa.performed -= AlPulsarMapa;
        if (accionCancelar != null)
            accionCancelar.performed -= AlPulsarCancelar;
        if (accionSubmit != null)
            accionSubmit.performed -= AlPulsarSubmit;

        accionMapa = null;
        accionCancelar = null;
        accionSubmit = null;
        accionesEntrada = null;
        if (moduloUI != null)
            moduloUI.actionsAsset = null;
        foreach (InputActionReference referencia in referenciasAcciones)
        {
            if (referencia != null)
                Destroy(referencia);
        }
        referenciasAcciones.Clear();
    }

    private void AlPulsarMapa(InputAction.CallbackContext contexto)
    {
        if (jugador == null || zona == null || dialogoAbierto ||
            resultadoVictoria || resultadoDerrota)
            return;

        if (!pausaAbierta)
            AbrirPausa();
        MostrarMapa();
    }

    private void AlPulsarCancelar(InputAction.CallbackContext contexto)
    {
        if (resultadoVictoria || resultadoDerrota)
            return;

        if (dialogoAbierto)
        {
            CerrarDialogo();
            return;
        }

        if (jugador == null)
            return;

        if (!pausaAbierta)
            AbrirPausa();
        else if (panelOpciones.activeSelf || panelMapa.activeSelf)
            MostrarMenuPausa();
        else
            CerrarPausa();
    }

    private void AlPulsarSubmit(InputAction.CallbackContext contexto)
    {
        if (dialogoAbierto)
            AvanzarDialogo();
    }

    private void CrearCanvas()
    {
        GameObject objetoCanvas = new GameObject("Canvas de juego",
            typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        objetoCanvas.transform.SetParent(transform, false);
        canvas = objetoCanvas.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        ConfigurarEscaladoCanvas(objetoCanvas.GetComponent<CanvasScaler>());
    }

    private void CrearEventSystem()
    {
        sistemaEventos = EventSystem.current;
        if (sistemaEventos == null)
        {
            GameObject objetoEventos = new GameObject("EventSystem");
            objetoEventos.transform.SetParent(transform, false);
            sistemaEventos = objetoEventos.AddComponent<EventSystem>();
        }
        else if (sistemaEventos.transform.parent != transform)
        {
            sistemaEventos.transform.SetParent(transform, false);
        }

        BaseInputModule[] modulos = sistemaEventos.GetComponents<BaseInputModule>();
        foreach (BaseInputModule modulo in modulos)
        {
            if (!(modulo is InputSystemUIInputModule))
                modulo.enabled = false;
        }

        moduloUI = sistemaEventos.GetComponent<InputSystemUIInputModule>();
        if (moduloUI == null)
            moduloUI = sistemaEventos.gameObject.AddComponent<InputSystemUIInputModule>();
    }

    private void CrearHUD()
    {
        hud = CrearPanel(canvas.transform, "HUD",
            new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
            new Vector2(440f, 164f), new Vector2(22f, -22f),
            new Color(0.035f, 0.085f, 0.095f, 0.94f)).gameObject;

        tituloZona = CrearTexto(hud.transform, "Zona", "LAS RUINAS DEL ECO",
            19, FontStyle.Bold, TextAnchor.MiddleLeft);
        AjustarRect(tituloZona.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(-30f, 34f), new Vector2(16f, -12f));

        textoVida = CrearTexto(hud.transform, "Vida", "INTEGRIDAD  0 / 0",
            16, FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(textoVida.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(-34f, 24f), new Vector2(18f, -51f));
        barraVida = CrearBarra(hud.transform, "Barra de integridad",
            new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0f, 1f),
            new Vector2(-36f, 14f), new Vector2(18f, -78f));

        textoRecursos = CrearTexto(hud.transform, "Recursos", "ECOS  0   LLAVES  0",
            15, FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(textoRecursos.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(-34f, 22f), new Vector2(18f, -99f));

        textoHabilidad = CrearTexto(hud.transform, "Habilidad", "RESONADOR",
            14, FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(textoHabilidad.rectTransform, new Vector2(0f, 1f), new Vector2(1f, 1f),
            new Vector2(0f, 1f), new Vector2(-34f, 20f), new Vector2(18f, -124f));

        Button botonPausa = CrearBoton(canvas.transform, "Pausa", "II",
            new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
            new Vector2(54f, 48f), new Vector2(-24f, -22f), AbrirPausa);
        Navigation sinNavegacion = new Navigation { mode = Navigation.Mode.None };
        botonPausa.navigation = sinNavegacion;

        textoMensaje = CrearTexto(canvas.transform, "Mensaje", string.Empty,
            18, FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(textoMensaje.rectTransform, new Vector2(0.12f, 0f), new Vector2(0.88f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 52f), new Vector2(0f, 26f));
        textoMensaje.gameObject.SetActive(false);

        textoAyuda = CrearTexto(canvas.transform, "Ayuda de controles",
            "TECLADO: A/D mover · ESPACIO saltar · ENTER atacar · E interactuar · TAB mapa · ESC pausa\nMANDO: stick izquierdo mover · START pausa · SELECT mapa · Aceptar/Atras navegan menus",
            12, FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(textoAyuda.rectTransform, new Vector2(0f, 0f), new Vector2(0.88f, 0f),
            new Vector2(0f, 0f), new Vector2(0f, 48f), new Vector2(22f, 12f));
        textoAyuda.color = new Color(0.76f, 0.85f, 0.81f, 0.9f);

        textoIndicacionInteraccion = CrearTexto(canvas.transform, "Indicacion de interaccion",
            string.Empty, 18, FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(textoIndicacionInteraccion.rectTransform,
            new Vector2(0.2f, 0f), new Vector2(0.8f, 0f),
            new Vector2(0.5f, 0f), new Vector2(0f, 54f), new Vector2(0f, 82f));
        textoIndicacionInteraccion.gameObject.SetActive(false);
    }

    private void CrearPanelPausa()
    {
        panelPausa = CrearPanel(canvas.transform, "Menu de pausa",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(500f, 430f), Vector2.zero,
            new Color(0.035f, 0.085f, 0.095f, 0.98f)).gameObject;
        CrearTitulo(panelPausa.transform, "PAUSA", 0f, 24);
        CrearBotonPanel(panelPausa.transform, "Reanudar", -100f, CerrarPausa);
        botonMapa = CrearBotonPanel(panelPausa.transform, "Mapa descubierto", -164f, MostrarMapa);
        CrearBotonPanel(panelPausa.transform, "Opciones", -228f, MostrarOpciones);
        CrearTextoPanel(panelPausa.transform, "CONTROLES: TAB MAPA  ·  ESC / B VOLVER", -314f, 14);
        panelPausa.SetActive(false);
    }

    private void CrearPanelOpciones()
    {
        panelOpciones = CrearPanel(canvas.transform, "Opciones",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(660f, 560f), Vector2.zero,
            new Color(0.035f, 0.085f, 0.095f, 0.98f)).gameObject;
        CrearTitulo(panelOpciones.transform, "OPCIONES", 0f, 24);

        CrearTextoPanel(panelOpciones.transform, "VOLUMEN GENERAL", -96f, 16);
        controlVolumen = CrearBarra(panelOpciones.transform, "Volumen",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(340f, 22f), new Vector2(0f, -128f));
        controlVolumen.interactable = true;
        controlVolumen.minValue = 0f;
        controlVolumen.maxValue = 1f;
        controlVolumen.value = PlayerPrefs.GetFloat(ClaveVolumen, 1f);
        controlVolumen.onValueChanged.AddListener(GuardarVolumen);

        CrearTextoPanel(panelOpciones.transform, "RESOLUCION", -175f, 16);
        textoResolucion = CrearTexto(panelOpciones.transform, "Resolucion actual", "",
            16, FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(textoResolucion.rectTransform,
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(210f, 40f), new Vector2(0f, -212f));
        CrearBoton(panelOpciones.transform, "Resolucion anterior", "<",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(50f, 42f), new Vector2(-150f, -211f), ResolucionAnterior);
        CrearBoton(panelOpciones.transform, "Resolucion siguiente", ">",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(50f, 42f), new Vector2(150f, -211f), ResolucionSiguiente);
        PrepararResoluciones();

        controlPantallaCompleta = CrearToggle(panelOpciones.transform, "Pantalla completa",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(430f, 48f), new Vector2(0f, -286f),
            PlayerPrefs.GetInt(ClavePantallaCompleta, Screen.fullScreen ? 1 : 0) != 0);
        controlPantallaCompleta.onValueChanged.AddListener(EstablecerPantallaCompleta);

        controlReducirDestellos = CrearToggle(panelOpciones.transform, "Reducir destellos",
            new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(430f, 48f), new Vector2(0f, -342f), ReducirDestellos);
        controlReducirDestellos.onValueChanged.AddListener(EstablecerReduccionDestellos);

        CrearTextoPanel(panelOpciones.transform, "FLECHAS / WASD Y ACEPTAR PARA NAVEGAR", -406f, 13);
        CrearBotonPanel(panelOpciones.transform, "Volver", -470f, MostrarMenuPausa);
        panelOpciones.SetActive(false);
    }

    private void CrearPanelMapa()
    {
        panelMapa = CrearPanel(canvas.transform, "Mapa descubierto",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(790f, 560f), Vector2.zero,
            new Color(0.035f, 0.085f, 0.095f, 0.98f)).gameObject;
        CrearTitulo(panelMapa.transform, "CARTOGRAFIA DE ECOS", 0f, 24);
        Text instruccion = CrearTexto(panelMapa.transform, "Informacion descubierta",
            "Solo se muestran los tramos que ya recorriste.", 16, FontStyle.Normal,
            TextAnchor.MiddleCenter);
        AjustarRect(instruccion.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(650f, 32f), new Vector2(0f, -64f));

        Image fondoMapa = CrearPanel(panelMapa.transform, "Superficie de mapa",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(690f, 330f), new Vector2(0f, 4f),
            new Color(0.06f, 0.13f, 0.14f, 0.7f));
        areaMapa = fondoMapa.rectTransform;

        CrearTextoPanel(panelMapa.transform, "CADA MARCA REPRESENTA UN TRAMO EXPLORADO", -462f, 13);
        CrearBotonPanel(panelMapa.transform, "Volver", -512f, MostrarMenuPausa);
        panelMapa.SetActive(false);
    }

    private void CrearPanelResultado()
    {
        panelResultado = CrearPanel(canvas.transform, "Resultado",
            new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
            new Vector2(570f, 300f), Vector2.zero,
            new Color(0.035f, 0.085f, 0.095f, 0.98f)).gameObject;
        textoResultado = CrearTexto(panelResultado.transform, "Resultado", "",
            23, FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(textoResultado.rectTransform, new Vector2(0.08f, 0.58f),
            new Vector2(0.92f, 0.9f), new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        botonReintentar = CrearBoton(panelResultado.transform, "Volver a intentar",
            "Volver a intentar", new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
            new Vector2(0.5f, 0f), new Vector2(370f, 48f), new Vector2(0f, 24f),
            ReiniciarZona);
        panelResultado.SetActive(false);
    }

    private void CrearPanelDialogo()
    {
        panelDialogo = CrearPanel(canvas.transform, "Dialogo",
            new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.31f),
            new Vector2(0.5f, 0f), Vector2.zero, Vector2.zero,
            new Color(0.035f, 0.085f, 0.095f, 0.98f)).gameObject;

        textoNombreDialogo = CrearTexto(panelDialogo.transform, "Nombre del personaje",
            string.Empty, 20, FontStyle.Bold, TextAnchor.MiddleLeft);
        AjustarRect(textoNombreDialogo.rectTransform,
            new Vector2(0.04f, 0.68f), new Vector2(0.96f, 0.95f),
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        textoNombreDialogo.color = new Color(0.98f, 0.78f, 0.43f);

        textoContenidoDialogo = CrearTexto(panelDialogo.transform, "Texto del dialogo",
            string.Empty, 20, FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(textoContenidoDialogo.rectTransform,
            new Vector2(0.04f, 0.22f), new Vector2(0.96f, 0.7f),
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);

        textoAyudaDialogo = CrearTexto(panelDialogo.transform, "Controles del dialogo",
            "ACEPTAR: revelar / continuar     ·     ESC / ATRAS: cerrar",
            13, FontStyle.Normal, TextAnchor.MiddleRight);
        AjustarRect(textoAyudaDialogo.rectTransform,
            new Vector2(0.04f, 0.03f), new Vector2(0.96f, 0.2f),
            new Vector2(0f, 0.5f), Vector2.zero, Vector2.zero);
        textoAyudaDialogo.color = new Color(0.72f, 0.84f, 0.79f, 1f);
        panelDialogo.SetActive(false);
    }

    private bool AbrirDialogo(PersonajeSecundario2D personaje, Jugador2D jugadorActual)
    {
        if (personaje == null || jugadorActual == null ||
            !personaje.EstaInteractuableDisponible || !jugadorActual.isActiveAndEnabled ||
            dialogoAbierto || pausaAbierta || resultadoVictoria || resultadoDerrota ||
            Time.timeScale <= 0f || accionSubmit == null || accionCancelar == null)
        {
            return false;
        }

        string[] lineas = personaje.ObtenerLineasDialogo();
        if (lineas.Length == 0)
            return false;

        personajeDialogo = personaje;
        jugadorDialogo = jugadorActual;
        lineasDialogo = lineas;
        indiceLineaDialogo = 0;
        caracteresMostrados = 0f;
        dialogoAbierto = true;
        escalaAntesDeDialogo = Time.timeScale;
        Time.timeScale = 0f;
        jugadorDialogo.EstablecerDialogoActivo(true);
        textoNombreDialogo.text = personaje.NombreVisible.ToUpperInvariant();
        panelDialogo.SetActive(true);
        textoIndicacionInteraccion.gameObject.SetActive(false);
        MostrarLineaDialogo();
        Deseleccionar();
        return true;
    }

    private void ActualizarDialogo()
    {
        if (!dialogoAbierto)
            return;

        if (personajeDialogo == null || !personajeDialogo.EstaInteractuableDisponible ||
            jugadorDialogo == null || !jugadorDialogo.isActiveAndEnabled)
        {
            CerrarDialogo();
            return;
        }

        string linea = lineasDialogo[indiceLineaDialogo];
        if (caracteresMostrados >= linea.Length)
            return;

        caracteresMostrados = Mathf.Min(
            linea.Length,
            caracteresMostrados + Time.unscaledDeltaTime * personajeDialogo.VelocidadTexto);
        textoContenidoDialogo.text = linea.Substring(0, Mathf.FloorToInt(caracteresMostrados));
    }

    private void AvanzarDialogo()
    {
        if (!dialogoAbierto)
            return;

        string linea = lineasDialogo[indiceLineaDialogo];
        if (caracteresMostrados < linea.Length)
        {
            caracteresMostrados = linea.Length;
            textoContenidoDialogo.text = linea;
            return;
        }

        indiceLineaDialogo++;
        if (indiceLineaDialogo >= lineasDialogo.Length)
        {
            CerrarDialogo();
            return;
        }

        caracteresMostrados = 0f;
        MostrarLineaDialogo();
    }

    private void MostrarLineaDialogo()
    {
        textoContenidoDialogo.text = string.Empty;
    }

    private void CerrarDialogo()
    {
        if (!dialogoAbierto)
            return;

        dialogoAbierto = false;
        if (panelDialogo != null)
            panelDialogo.SetActive(false);
        if (jugadorDialogo != null)
            jugadorDialogo.EstablecerDialogoActivo(false);

        jugadorDialogo = null;
        personajeDialogo = null;
        lineasDialogo = null;
        indiceLineaDialogo = 0;
        caracteresMostrados = 0f;
        Time.timeScale = escalaAntesDeDialogo > 0f ? escalaAntesDeDialogo : 1f;
    }

    private void AbrirPausa()
    {
        if (pausaAbierta || dialogoAbierto || resultadoVictoria || resultadoDerrota)
            return;

        escalaAntesDePausa = Time.timeScale;
        Time.timeScale = 0f;
        pausaAbierta = true;
        panelOpciones.SetActive(false);
        panelMapa.SetActive(false);
        panelPausa.SetActive(true);
        SeleccionarPrimerBoton(panelPausa);
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    private void CerrarPausa()
    {
        panelPausa.SetActive(false);
        panelOpciones.SetActive(false);
        panelMapa.SetActive(false);
        pausaAbierta = false;
        Time.timeScale = escalaAntesDePausa > 0f ? escalaAntesDePausa : 1f;
        Deseleccionar();
        Cursor.visible = false;
    }

    private void MostrarMenuPausa()
    {
        panelOpciones.SetActive(false);
        panelMapa.SetActive(false);
        panelPausa.SetActive(true);
        SeleccionarPrimerBoton(panelPausa);
    }

    private void MostrarOpciones()
    {
        panelPausa.SetActive(false);
        panelMapa.SetActive(false);
        panelOpciones.SetActive(true);
        SeleccionarPrimerBoton(panelOpciones);
    }

    private void MostrarMapa()
    {
        if (zona == null)
        {
            MostrarMensajeLocal("NO HAY UN MAPA CONOCIDO EN ESTA ZONA", 2f);
            return;
        }

        panelPausa.SetActive(false);
        panelOpciones.SetActive(false);
        panelMapa.SetActive(true);
        DibujarMapaDescubierto();
        SeleccionarPrimerBoton(panelMapa);
    }

    private void MostrarResultado(bool victoria)
    {
        if (jugador == null || resultadoVictoria || resultadoDerrota)
            return;

        CerrarDialogo();
        resultadoVictoria = victoria;
        resultadoDerrota = !victoria;
        if (victoria)
        {
            escalaAntesDePausa = Time.timeScale;
            Time.timeScale = 0f;
            textoResultado.text = "ZONA COMPLETADA\n\nEl flujo vuelve a circular. Puedes recorrerla de nuevo cuando quieras.";
            botonReintentar.gameObject.SetActive(true);
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            textoResultado.text = "SIRA HA CAIDO\n\nRegresando al ultimo punto de descanso...";
            botonReintentar.gameObject.SetActive(false);
        }

        hud.SetActive(false);
        panelPausa.SetActive(false);
        panelOpciones.SetActive(false);
        panelMapa.SetActive(false);
        panelResultado.SetActive(true);

        if (victoria)
            SeleccionarPrimerBoton(panelResultado);
    }

    private void AlDerrotarJugador()
    {
        MostrarResultado(false);
    }

    private void AlCambiarVida(int vidaActual, int vidaMaxima)
    {
        if (!resultadoDerrota || vidaActual <= 0)
            return;

        resultadoDerrota = false;
        panelResultado.SetActive(false);
        hud.SetActive(true);
        MostrarMensajeLocal("HAS VUELTO AL ULTIMO PUNTO DE DESCANSO", 2.5f);
    }

    private void ReiniciarZona()
    {
        CerrarDialogo();
        Time.timeScale = 1f;
        Scene escena = SceneManager.GetActiveScene();
        if (escena.buildIndex < 0)
        {
            Debug.LogError("La escena actual no esta incluida en Build Settings.");
            return;
        }

        SceneManager.LoadScene(escena.buildIndex);
    }

    private void RegistrarExploracion()
    {
        if (zona == null || jugador == null)
            return;

        ProgresoPartida2D.RegistrarZonaDescubierta(zona.IdZona);
        Vector2 limitesX = zona.LimitesX;
        Vector2 limitesY = zona.LimitesY;
        int columnas = Mathf.Max(1, Mathf.CeilToInt((limitesX.y - limitesX.x) / TamanoCeldaMapa));
        int filas = Mathf.Max(1, Mathf.CeilToInt((limitesY.y - limitesY.x) / TamanoCeldaMapa));
        Vector2 posicion = jugador.transform.position;
        int x = Mathf.Clamp(Mathf.FloorToInt((posicion.x - limitesX.x) / TamanoCeldaMapa), 0, columnas - 1);
        int y = Mathf.Clamp(Mathf.FloorToInt((posicion.y - limitesY.x) / TamanoCeldaMapa), 0, filas - 1);
        Vector2Int celda = new Vector2Int(x, y);
        if (celda == ultimaCeldaMapa)
            return;

        ultimaCeldaMapa = celda;
        ProgresoPartida2D.RegistrarCeldaExplorada(zona.IdZona, x, y);
        if (panelMapa.activeSelf)
            DibujarMapaDescubierto();
    }

    private void DibujarMapaDescubierto()
    {
        foreach (RectTransform celda in celdasMapa)
        {
            if (celda != null)
                Destroy(celda.gameObject);
        }
        celdasMapa.Clear();

        if (zona == null)
            return;

        Vector2 limitesX = zona.LimitesX;
        Vector2 limitesY = zona.LimitesY;
        int columnas = Mathf.Max(1, Mathf.CeilToInt((limitesX.y - limitesX.x) / TamanoCeldaMapa));
        int filas = Mathf.Max(1, Mathf.CeilToInt((limitesY.y - limitesY.x) / TamanoCeldaMapa));
        Vector2Int celdaJugador = ObtenerCeldaJugador(columnas, filas, limitesX, limitesY);
        List<Vector2Int> descubiertas = ProgresoPartida2D.ObtenerCeldasExploradas(zona.IdZona);
        Rect rectMapa = areaMapa.rect;
        float anchoCelda = rectMapa.width / columnas;
        float altoCelda = rectMapa.height / filas;

        foreach (Vector2Int celda in descubiertas.Distinct().OrderBy(c => c.y).ThenBy(c => c.x))
        {
            GameObject objeto = new GameObject("Tramo descubierto", typeof(RectTransform), typeof(Image));
            objeto.transform.SetParent(areaMapa, false);
            RectTransform rect = objeto.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(Mathf.Max(8f, anchoCelda * 0.62f),
                Mathf.Max(8f, altoCelda * 0.42f));
            rect.anchoredPosition = new Vector2(
                ((celda.x + 0.5f) / columnas - 0.5f) * rectMapa.width,
                ((celda.y + 0.5f) / filas - 0.5f) * rectMapa.height);
            objeto.GetComponent<Image>().color = celda == celdaJugador
                ? new Color(0.98f, 0.77f, 0.39f, 1f)
                : new Color(0.35f, 0.78f, 0.68f, 0.9f);
            celdasMapa.Add(rect);
        }
    }

    private Vector2Int ObtenerCeldaJugador(int columnas, int filas, Vector2 limitesX, Vector2 limitesY)
    {
        if (jugador == null)
            return Vector2Int.zero;

        Vector2 posicion = jugador.transform.position;
        return new Vector2Int(
            Mathf.Clamp(Mathf.FloorToInt((posicion.x - limitesX.x) / TamanoCeldaMapa), 0, columnas - 1),
            Mathf.Clamp(Mathf.FloorToInt((posicion.y - limitesY.x) / TamanoCeldaMapa), 0, filas - 1));
    }

    private void ActualizarHUD()
    {
        if (jugador == null)
            return;

        tituloZona.text = zona != null ? zona.NombreZona.ToUpperInvariant() : "LAS RUINAS DEL ECO";
        if (vida != null)
        {
            textoVida.text = "INTEGRIDAD  " + vida.VidaActual + " / " + vida.VidaMaxima;
            barraVida.maxValue = Mathf.Max(1, vida.VidaMaxima);
            barraVida.value = vida.VidaActual;
        }

        if (inventario != null)
        {
            int llaves = inventario.Llaves.Where(entrada => entrada != null).Sum(entrada => entrada.Cantidad);
            int armas = inventario.Armas.Where(entrada => entrada != null).Sum(entrada => entrada.Cantidad);
            int objetos = inventario.Objetos.Where(entrada => entrada != null).Sum(entrada => entrada.Cantidad);
            textoRecursos.text = "ECOS " + inventario.Monedas + "  LLAVES " + llaves +
                "  ARMAS " + armas + "  OBJ. " + objetos;
        }

        textoHabilidad.text = jugador.DashDesbloqueado
            ? "DASH DISPONIBLE"
            : "DASH BLOQUEADO  ·  BUSCA SU CRISTAL";
    }

    private void MostrarMensajeLocal(string mensaje, float duracion)
    {
        if (string.IsNullOrWhiteSpace(mensaje))
            return;

        mensajeActual = mensaje;
        mensajeHasta = Time.unscaledTime + Mathf.Max(0.1f, duracion);
        textoMensaje.gameObject.SetActive(true);
        textoMensaje.text = mensaje;
    }

    private void GuardarVolumen(float valor)
    {
        AudioListener.volume = valor;
        PlayerPrefs.SetFloat(ClaveVolumen, valor);
        PlayerPrefs.Save();
    }

    private void EstablecerPantallaCompleta(bool pantallaCompleta)
    {
        PlayerPrefs.SetInt(ClavePantallaCompleta, pantallaCompleta ? 1 : 0);
        PlayerPrefs.Save();
        Screen.fullScreenMode = pantallaCompleta
            ? FullScreenMode.FullScreenWindow
            : FullScreenMode.Windowed;
    }

    private void EstablecerReduccionDestellos(bool reducir)
    {
        PlayerPrefs.SetInt(ClaveReducirDestellos, reducir ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void AplicarOpcionesGuardadas()
    {
        AudioListener.volume = Mathf.Clamp01(PlayerPrefs.GetFloat(ClaveVolumen, 1f));
        if (PlayerPrefs.HasKey(ClavePantallaCompleta))
        {
            Screen.fullScreenMode = PlayerPrefs.GetInt(ClavePantallaCompleta) != 0
                ? FullScreenMode.FullScreenWindow
                : FullScreenMode.Windowed;
        }

        int ancho = PlayerPrefs.GetInt(ClaveResolucionAncho, 0);
        int alto = PlayerPrefs.GetInt(ClaveResolucionAlto, 0);
        if (ancho > 0 && alto > 0)
            Screen.SetResolution(ancho, alto, Screen.fullScreenMode);
    }

    private void PrepararResoluciones()
    {
        resoluciones.Clear();
        foreach (Resolution resolucion in Screen.resolutions)
        {
            if (!resoluciones.Any(r => r.width == resolucion.width && r.height == resolucion.height))
                resoluciones.Add(resolucion);
        }

        if (resoluciones.Count == 0)
            resoluciones.Add(Screen.currentResolution);

        indiceResolucion = resoluciones.FindIndex(r =>
            r.width == Screen.width && r.height == Screen.height);
        if (indiceResolucion < 0)
        {
            resoluciones.Add(Screen.currentResolution);
            indiceResolucion = resoluciones.Count - 1;
        }
        ActualizarTextoResolucion();
    }

    private void ResolucionAnterior()
    {
        CambiarResolucion(-1);
    }

    private void ResolucionSiguiente()
    {
        CambiarResolucion(1);
    }

    private void CambiarResolucion(int delta)
    {
        indiceResolucion = (indiceResolucion + delta + resoluciones.Count) % resoluciones.Count;
        Resolution resolucion = resoluciones[indiceResolucion];
        Screen.SetResolution(resolucion.width, resolucion.height, Screen.fullScreenMode);
        PlayerPrefs.SetInt(ClaveResolucionAncho, resolucion.width);
        PlayerPrefs.SetInt(ClaveResolucionAlto, resolucion.height);
        PlayerPrefs.Save();
        ActualizarTextoResolucion();
    }

    private void ActualizarTextoResolucion()
    {
        if (textoResolucion == null || resoluciones.Count == 0)
            return;

        Resolution resolucion = resoluciones[indiceResolucion];
        textoResolucion.text = resolucion.width + " x " + resolucion.height;
    }

    private void SeleccionarPrimerBoton(GameObject panel)
    {
        Button boton = panel.GetComponentInChildren<Button>();
        if (boton != null && boton.gameObject.activeInHierarchy && boton.interactable)
            sistemaEventos.SetSelectedGameObject(boton.gameObject);
    }

    private void Deseleccionar()
    {
        if (sistemaEventos != null)
            sistemaEventos.SetSelectedGameObject(null);
    }

    private Image CrearPanel(Transform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax,
        Vector2 pivote, Vector2 tamano, Vector2 posicion, Color color)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Image));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        AjustarRect(rect, anclaMin, anclaMax, pivote, tamano, posicion);
        Image imagen = objeto.GetComponent<Image>();
        imagen.color = color;
        return imagen;
    }

    private Text CrearTexto(Transform padre, string nombre, string texto, int tamano,
        FontStyle estilo, TextAnchor alineacion)
    {
        GameObject objeto = new GameObject(nombre, typeof(RectTransform), typeof(Text));
        objeto.transform.SetParent(padre, false);
        Text etiqueta = objeto.GetComponent<Text>();
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.text = texto;
        etiqueta.fontSize = tamano;
        etiqueta.fontStyle = estilo;
        etiqueta.alignment = alineacion;
        etiqueta.color = new Color(0.9f, 0.94f, 0.86f);
        etiqueta.horizontalOverflow = HorizontalWrapMode.Wrap;
        etiqueta.verticalOverflow = VerticalWrapMode.Overflow;
        etiqueta.raycastTarget = false;
        return etiqueta;
    }

    private Slider CrearBarra(Transform padre, string nombre, Vector2 anclaMin, Vector2 anclaMax,
        Vector2 pivote, Vector2 tamano, Vector2 posicion)
    {
        Image fondo = CrearPanel(padre, nombre, anclaMin, anclaMax, pivote,
            tamano, posicion, new Color(0.1f, 0.2f, 0.2f, 1f));
        Slider barra = fondo.gameObject.AddComponent<Slider>();
        barra.minValue = 0f;
        barra.maxValue = 1f;
        barra.value = 1f;
        barra.interactable = false;
        barra.transition = Selectable.Transition.None;
        GameObject rellenoObjeto = new GameObject("Relleno", typeof(RectTransform), typeof(Image));
        rellenoObjeto.transform.SetParent(fondo.transform, false);
        RectTransform rellenoRect = rellenoObjeto.GetComponent<RectTransform>();
        rellenoRect.anchorMin = Vector2.zero;
        rellenoRect.anchorMax = Vector2.one;
        rellenoRect.offsetMin = new Vector2(2f, 2f);
        rellenoRect.offsetMax = new Vector2(-2f, -2f);
        rellenoObjeto.GetComponent<Image>().color = new Color(0.28f, 0.82f, 0.67f, 1f);
        barra.fillRect = rellenoRect;
        barra.handleRect = null;
        return barra;
    }

    private Button CrearBoton(Transform padre, string nombre, string etiqueta,
        Vector2 anclaMin, Vector2 anclaMax, Vector2 pivote, Vector2 tamano,
        Vector2 posicion, UnityEngine.Events.UnityAction accion)
    {
        Image fondo = CrearPanel(padre, nombre, anclaMin, anclaMax, pivote, tamano, posicion,
            new Color(0.11f, 0.25f, 0.25f, 1f));
        Button boton = fondo.gameObject.AddComponent<Button>();
        boton.targetGraphic = fondo;
        ColorBlock colores = boton.colors;
        colores.normalColor = new Color(0.11f, 0.25f, 0.25f, 1f);
        colores.highlightedColor = new Color(0.22f, 0.47f, 0.42f, 1f);
        colores.selectedColor = new Color(0.22f, 0.47f, 0.42f, 1f);
        colores.pressedColor = new Color(0.79f, 0.59f, 0.31f, 1f);
        colores.disabledColor = new Color(0.18f, 0.22f, 0.21f, 0.65f);
        boton.colors = colores;
        boton.onClick.AddListener(accion);

        Text texto = CrearTexto(fondo.transform, "Etiqueta", etiqueta, 17,
            FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(texto.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        return boton;
    }

    private Toggle CrearToggle(Transform padre, string etiqueta, Vector2 anclaMin,
        Vector2 anclaMax, Vector2 pivote, Vector2 tamano, Vector2 posicion, bool valor)
    {
        GameObject objeto = new GameObject(etiqueta, typeof(RectTransform), typeof(Toggle));
        objeto.transform.SetParent(padre, false);
        RectTransform rect = objeto.GetComponent<RectTransform>();
        AjustarRect(rect, anclaMin, anclaMax, pivote, tamano, posicion);
        Toggle toggle = objeto.GetComponent<Toggle>();
        toggle.isOn = valor;

        Image fondo = CrearPanel(objeto.transform, "Caja", new Vector2(1f, 0.5f),
            new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(30f, 30f),
            new Vector2(-8f, 0f), new Color(0.12f, 0.23f, 0.23f, 1f));
        Image marca = CrearPanel(fondo.transform, "Marca", Vector2.zero, Vector2.one,
            new Vector2(0.5f, 0.5f), new Vector2(-10f, -10f), Vector2.zero,
            new Color(0.43f, 0.85f, 0.68f, 1f));
        toggle.targetGraphic = fondo;
        toggle.graphic = marca;
        toggle.isOn = !valor;
        toggle.isOn = valor;
        Text texto = CrearTexto(objeto.transform, "Etiqueta", etiqueta, 16,
            FontStyle.Normal, TextAnchor.MiddleLeft);
        AjustarRect(texto.rectTransform, Vector2.zero, Vector2.one,
            new Vector2(0f, 0.5f), new Vector2(-62f, 0f), Vector2.zero);
        return toggle;
    }

    private void CrearTitulo(Transform padre, string texto, float posicionY, int tamano)
    {
        Text titulo = CrearTexto(padre, "Titulo", texto, tamano,
            FontStyle.Bold, TextAnchor.MiddleCenter);
        AjustarRect(titulo.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, 54f), new Vector2(0f, posicionY - 24f));
    }

    private void CrearTextoPanel(Transform padre, string texto, float posicionY, int tamano)
    {
        Text etiqueta = CrearTexto(padre, texto, texto, tamano,
            FontStyle.Normal, TextAnchor.MiddleCenter);
        AjustarRect(etiqueta.rectTransform, new Vector2(0.08f, 1f), new Vector2(0.92f, 1f),
            new Vector2(0.5f, 1f), new Vector2(0f, 28f), new Vector2(0f, posicionY));
        etiqueta.color = new Color(0.72f, 0.84f, 0.79f, 1f);
    }

    private Button CrearBotonPanel(Transform padre, string texto, float posicionY,
        UnityEngine.Events.UnityAction accion)
    {
        return CrearBoton(padre, texto, texto, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
            new Vector2(0.5f, 1f), new Vector2(370f, 48f), new Vector2(0f, posicionY), accion);
    }

    private static void AjustarRect(RectTransform rect, Vector2 anclaMin, Vector2 anclaMax,
        Vector2 pivote, Vector2 tamano, Vector2 posicion)
    {
        rect.anchorMin = anclaMin;
        rect.anchorMax = anclaMax;
        rect.pivot = pivote;
        rect.sizeDelta = tamano;
        rect.anchoredPosition = posicion;
    }
}
