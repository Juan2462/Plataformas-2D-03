using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasPuntosControl2D
{
    [MenuItem("Demo/Validar puntos de control y reaparicion")]
    public static void Ejecutar()
    {
        string idInventario = "test-checkpoint-" + Guid.NewGuid().ToString("N");
        FieldInfo campoClave = typeof(ProgresoPartida2D).GetField(
            "ClaveGuardado",
            BindingFlags.Static | BindingFlags.NonPublic
        );
        if (campoClave == null)
            throw new InvalidOperationException("No se pudo localizar la clave de progreso para aislarla.");

        string claveProgreso = (string)campoClave.GetRawConstantValue();
        bool habiaProgresoPrevio = PlayerPrefs.HasKey(claveProgreso);
        string progresoPrevio = habiaProgresoPrevio
            ? PlayerPrefs.GetString(claveProgreso)
            : null;
        try
        {
            ProgresoPartida2D.LimpiarProgresoDePrueba();
            ProbarGuardadoDeCheckpointYProgreso();
            ProbarReaparicionTrasMuerte(idInventario);
            ProbarSinCheckpoint();
            ProbarDestinoEntreZonas();
            ProbarEscenasIncluidasEnBuildSettings();
            ProbarCheckpointObsoleto();
            Debug.Log("Pruebas de checkpoints correctas: persistencia, muerte, reaparicion, fallback y destino entre zonas.");
        }
        finally
        {
            if (habiaProgresoPrevio)
                PlayerPrefs.SetString(claveProgreso, progresoPrevio);
            else
                PlayerPrefs.DeleteKey(claveProgreso);
            PlayerPrefs.DeleteKey("InventarioJugador2D_" + idInventario);
            PlayerPrefs.Save();
            ProgresoPartida2D.RecargarProgresoDePrueba();
        }
    }

    private static void ProbarGuardadoDeCheckpointYProgreso()
    {
        Vector2 posicion = new Vector2(18.5f, -2.25f);
        ProgresoPartida2D.GuardarPuntoControl(
            "Assets/Scenes/CanalCampanas.unity",
            "canal-campanas",
            "estacion-canal-01",
            posicion
        );
        ProgresoPartida2D.RegistrarEstacion("canal-campanas", "estacion-canal-01");
        ProgresoPartida2D.RegistrarHabilidad(HabilidadExploracion2D.Dash);
        ProgresoPartida2D.RecargarProgresoDePrueba();

        RegistroPuntoControl2D guardado = ProgresoPartida2D.UltimoPuntoControl;
        Exigir(guardado != null &&
               guardado.RutaEscena == "Assets/Scenes/CanalCampanas.unity" &&
               guardado.IdZona == "canal-campanas" &&
               guardado.IdPunto == "estacion-canal-01" &&
               guardado.Posicion == posicion,
            "El checkpoint debe conservar escena, zona, identificador y posicion tras recargar PlayerPrefs.");
        Exigir(ProgresoPartida2D.EstacionActivada("canal-campanas", "estacion-canal-01"),
            "El estado de una estacion activada debe persistir.");
        Exigir(ProgresoPartida2D.TieneHabilidad(HabilidadExploracion2D.Dash),
            "Las habilidades desbloqueadas deben persistir.");

        GameObject objetoHabilidades = new GameObject("Prueba - habilidad persistente");
        try
        {
            HabilidadesExploracion2D habilidades =
                objetoHabilidades.AddComponent<HabilidadesExploracion2D>();
            InvocarNoPublico(habilidades, "Awake");
            Exigir(habilidades.Tiene(HabilidadExploracion2D.Dash),
                "La zona siguiente debe recuperar la habilidad guardada.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objetoHabilidades);
        }
    }

    private static void ProbarReaparicionTrasMuerte(string idInventario)
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject objetoJugador = new GameObject("Prueba - jugador con checkpoint");
        objetoJugador.SetActive(false);
        Rigidbody2D rb = objetoJugador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        objetoJugador.AddComponent<BoxCollider2D>();
        Jugador2D jugador = objetoJugador.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        Set(jugador, "dashDesbloqueado", false);
        VidaJugador2D vida = objetoJugador.AddComponent<VidaJugador2D>();
        InventarioJugador2D inventario = objetoJugador.AddComponent<InventarioJugador2D>();
        Set(inventario, "idGuardado", idInventario);
        Set(vida, "vidaMaxima", 4);
        Set(vida, "tiempoInvulnerable", 0f);
        InvocarNoPublico(jugador, "Awake");
        InvocarNoPublico(vida, "Awake");
        InvocarNoPublico(inventario, "Awake");

        try
        {
            objetoJugador.SetActive(true);
            inventario.AgregarMonedas(7);
            inventario.AgregarLlave("llave-test", 1);

            Exigir(vida.RecibirDanio(vida.VidaMaxima), "El golpe letal debe derrotar al jugador.");
            Exigir(!vida.EstaVivo && !jugador.enabled, "La muerte debe detener el controlador.");

            Set(jugador, "dashTerminaEn", Time.time + 4f);
            Set(jugador, "ataqueTerminaEn", Time.time + 4f);
            Vector2 destino = new Vector2(27f, -1.5f);
            Exigir(vida.RespawnearEnPuntoControl(destino),
                "El jugador debe reaparecer en el checkpoint aunque su controlador estuviera desactivado.");
            Exigir(vida.EstaVivo && vida.VidaActual == vida.VidaMaxima,
                "La reaparicion debe restaurar toda la salud.");
            Exigir(Vector2.Distance(rb.position, destino) < 0.001f && rb.linearVelocity == Vector2.zero,
                "La reaparicion debe colocar al jugador en el punto y anular su velocidad.");
            Exigir(jugador.enabled && !jugador.EstaEnDash &&
                   (float)Leer(jugador, "ataqueTerminaEn") == 0f &&
                   (float)Leer(jugador, "dashTerminaEn") == 0f,
                "La reaparicion debe limpiar ataques y dash temporales.");
            Exigir(inventario.Monedas == 7 && inventario.TieneLlave("llave-test"),
                "La muerte no debe quitar monedas ni llaves.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objetoJugador);
            UnityEngine.Object.DestroyImmediate(acciones);
        }
    }

    private static void ProbarSinCheckpoint()
    {
        ProgresoPartida2D.EliminarPuntoControl();
        Vector2 inicio = new Vector2(3.5f, -1f);
        bool hayCheckpoint = GestorReaparicion2D.IntentarObtenerDestinoGuardado(
            "Assets/Scenes/CanalCampanas.unity",
            inicio,
            out string escena,
            out Vector2 destino
        );

        Exigir(!hayCheckpoint &&
               escena == "Assets/Scenes/CanalCampanas.unity" &&
               destino == inicio,
            "Sin checkpoint, el destino debe ser el inicio de la zona actual.");
    }

    private static void ProbarDestinoEntreZonas()
    {
        Vector2 descansoRefugio = new Vector2(12f, -3f);
        ProgresoPartida2D.GuardarPuntoControl(
            "Assets/Scenes/RuinsDemo.unity",
            "refugio",
            "descanso-refugio",
            descansoRefugio
        );

        bool hayCheckpoint = GestorReaparicion2D.IntentarObtenerDestinoGuardado(
            "Assets/Scenes/CanalCampanas.unity",
            Vector2.zero,
            out string escenaDestino,
            out Vector2 posicionDestino
        );
        Exigir(hayCheckpoint &&
               escenaDestino == "Assets/Scenes/RuinsDemo.unity" &&
               posicionDestino == descansoRefugio,
            "Una muerte en otra zona debe resolver la escena y posicion del ultimo descanso.");

        GameObject puntoEntrada = new GameObject("Entrada de prueba");
        try
        {
            PuntoEntradaZona2D entrada = puntoEntrada.AddComponent<PuntoEntradaZona2D>();
            Set(entrada, "idEntrada", "entrada-principal");
            Exigir(entrada.IdEntrada == "entrada-principal",
                "Las transiciones deben poder seleccionar una entrada explicita de destino.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(puntoEntrada);
        }
    }

    private static void ProbarEscenasIncluidasEnBuildSettings()
    {
        Exigir(GestorReaparicion2D.EscenaDisponibleEnBuildSettings(
                   "Assets/Scenes/CanalCampanas.unity"),
            "El Canal debe estar incluido en Build Settings para guardar sus checkpoints.");
        Exigir(!GestorReaparicion2D.EscenaDisponibleEnBuildSettings(
                   "Assets/Scenes/ZonaEliminada.unity") &&
               !GestorReaparicion2D.EscenaDisponibleEnBuildSettings(string.Empty),
            "Una ruta inexistente o vacia no debe considerarse un destino de reaparicion.");
    }

    private static void ProbarCheckpointObsoleto()
    {
        const string escenaActual = "Assets/Scenes/CanalCampanas.unity";
        Vector2 inicio = new Vector2(2f, 4f);
        ProgresoPartida2D.GuardarPuntoControl(
            "Assets/Scenes/ZonaEliminada.unity",
            "zona-antigua",
            "checkpoint-antiguo",
            Vector2.one
        );

        bool hayCheckpoint = GestorReaparicion2D.IntentarObtenerDestinoGuardado(
            escenaActual,
            inicio,
            out string escenaDestino,
            out Vector2 posicionDestino
        );

        Exigir(!hayCheckpoint &&
              escenaDestino == escenaActual &&
              posicionDestino == inicio &&
              !ProgresoPartida2D.TienePuntoControl,
            "Un checkpoint de una escena ausente debe borrarse y usar el inicio de la zona actual.");
    }

    private static InputActionAsset CrearAcciones()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap jugador = asset.AddActionMap("Player");
        jugador.AddAction("Move");
        jugador.AddAction("Jump", InputActionType.Button);
        jugador.AddAction("Attack", InputActionType.Button);
        jugador.AddAction("Sprint", InputActionType.Button);
        jugador.AddAction("Crouch", InputActionType.Button);
        jugador.AddAction("Interact", InputActionType.Button);
        return asset;
    }

    private static void Set(object objetivo, string nombre, object valor)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        campo.SetValue(objetivo, valor);
    }

    private static object Leer(object objetivo, string nombre)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        return campo.GetValue(objetivo);
    }

    private static void InvocarNoPublico(object objetivo, string nombre)
    {
        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);
        metodo.Invoke(objetivo, null);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}
