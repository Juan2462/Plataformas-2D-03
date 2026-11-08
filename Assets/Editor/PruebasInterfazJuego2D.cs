using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public static class PruebasInterfazJuego2D
{
    [MenuItem("Demo/Validar interfaz de juego")]
    public static void Ejecutar()
    {
        string idZona = "prueba-interfaz-" + Guid.NewGuid().ToString("N");
        try
        {
            ProbarDescubrimientoPersistente(idZona);
            ProbarControlesDeMenu();
            ProbarAislamientoMapasAcciones();
            ProbarEscaladoAdaptativo();
            Debug.Log("Interfaz validada: mapa, controles de menu y aislamiento de acciones del jugador e interfaz.");
        }
        finally
        {
            ProgresoPartida2D.LimpiarDescubrimientosDePrueba(idZona);
        }
    }

    private static void ProbarDescubrimientoPersistente(string idZona)
    {
        ProgresoPartida2D.RegistrarZonaDescubierta(idZona);
        ProgresoPartida2D.RegistrarCeldaExplorada(idZona, 2, 1);
        ProgresoPartida2D.RegistrarCeldaExplorada(idZona, 3, 1);
        ProgresoPartida2D.RecargarProgresoDePrueba();

        Exigir(ProgresoPartida2D.ZonaDescubierta(idZona),
            "La zona visitada debe conservarse al recargar el progreso.");
        Exigir(ProgresoPartida2D.ObtenerCeldasExploradas(idZona).SequenceEqual(
                new[] { new Vector2Int(2, 1), new Vector2Int(3, 1) }),
            "El mapa debe guardar solo las celdas que el jugador ya recorrio.");
        Exigir(ProgresoPartida2D.ObtenerCeldasExploradas(idZona + "-oculta").Count == 0,
            "Una zona no visitada no debe mostrar celdas descubiertas.");
    }

    private static void ProbarControlesDeMenu()
    {
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        Exigir(acciones != null, "Falta el InputActionAsset del proyecto.");

        InputAction navegar = acciones.FindAction("UI/Navigate", false);
        InputAction aceptar = acciones.FindAction("UI/Submit", false);
        InputAction cancelar = acciones.FindAction("UI/Cancel", false);
        InputAction mapa = acciones.FindAction("UI/Map", false);
        Exigir(navegar != null && aceptar != null && cancelar != null && mapa != null,
            "El mapa UI necesita navegacion, aceptar, volver y abrir mapa.");
        Exigir(navegar.bindings.Any(binding => binding.path == "<Keyboard>/upArrow") &&
               navegar.bindings.Any(binding => binding.path == "<Gamepad>/leftStick/up"),
            "La navegacion debe estar enlazada a teclado y mando.");
        Exigir(aceptar.bindings.Any(binding => binding.path == "*/{Submit}") &&
               cancelar.bindings.Any(binding => binding.path == "*/{Cancel}") &&
               cancelar.bindings.Any(binding => binding.path == "<Gamepad>/start"),
            "Aceptar, volver y pausa deben poder usarse desde los dispositivos compatibles.");
        Exigir(mapa.bindings.Any(binding => binding.path == "<Keyboard>/tab") &&
               mapa.bindings.Any(binding => binding.path == "<Gamepad>/select"),
            "El mapa debe abrirse desde teclado o mando.");
    }

    private static void ProbarEscaladoAdaptativo()
    {
        GameObject objeto = new GameObject("Prueba - escalado de interfaz");
        try
        {
            CanvasScaler escalador = objeto.AddComponent<CanvasScaler>();
            InterfazJuego2D.ConfigurarEscaladoCanvas(escalador);
            Exigir(escalador.uiScaleMode == CanvasScaler.ScaleMode.ScaleWithScreenSize &&
                   escalador.screenMatchMode == CanvasScaler.ScreenMatchMode.MatchWidthOrHeight &&
                   escalador.referenceResolution == new Vector2(1600f, 900f) &&
                   Mathf.Approximately(escalador.matchWidthOrHeight, 0.5f),
                "La interfaz debe adaptar su escala a la resolucion disponible.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objeto);
        }
    }

    private static void ProbarAislamientoMapasAcciones()
    {
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        Exigir(acciones != null, "Falta el InputActionAsset del proyecto.");
        InputActionMap mapaJugadorOriginal = acciones.FindActionMap("Player", false);
        InputActionMap mapaInterfaz = acciones.FindActionMap("UI", false);
        Exigir(mapaJugadorOriginal != null && mapaInterfaz != null,
            "El InputActionAsset necesita los mapas Player y UI.");
        InputActionMap mapaJugadorClonado = mapaJugadorOriginal.Clone();

        try
        {
            for (int intento = 0; intento < 3; intento++)
            {
                mapaInterfaz.Enable();
                mapaJugadorClonado.Enable();
                Exigir(mapaInterfaz.enabled && mapaJugadorClonado.enabled,
                    "Los mapas de interfaz y jugador deben poder activarse de forma independiente.");

                mapaJugadorClonado.Disable();
                Exigir(mapaInterfaz.enabled && !mapaJugadorClonado.enabled,
                    "Desactivar el mapa del jugador no debe desactivar la interfaz.");
                mapaInterfaz.Disable();
            }
        }
        finally
        {
            mapaJugadorClonado.Disable();
            mapaJugadorClonado.Dispose();
            mapaInterfaz.Disable();
        }
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}
