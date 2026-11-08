using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasDialogos2D
{
    [MenuItem("Demo/Validar interaccion y dialogos")]
    public static void Ejecutar()
    {
        ProbarCatalogoDeHistoria();
        ProbarPersonajeInteractuable();
        ProbarControlesDeDialogo();
        ProbarCierreSeguroSinObjetivos();
        Debug.Log("Dialogos validados: Mara, Teren, Narel y avisos del Medidor; interaccion contextual y controles.");
    }

    private static void ProbarCatalogoDeHistoria()
    {
        PersonajeHistoria2D[] personajes =
        {
            PersonajeHistoria2D.Mara,
            PersonajeHistoria2D.Teren,
            PersonajeHistoria2D.Narel,
            PersonajeHistoria2D.ElMedidor
        };

        foreach (PersonajeHistoria2D personaje in personajes)
        {
            string[] lineas = DialogosHistoria2D.ObtenerLineas(personaje);
            Exigir(DialogosHistoria2D.TieneConversacion(personaje) && lineas.Length >= 3,
                "Faltan lineas para " + DialogosHistoria2D.ObtenerNombre(personaje) + ".");
            Exigir(lineas.All(linea => !string.IsNullOrWhiteSpace(linea)),
                "El dialogo contiene una linea vacia: " + DialogosHistoria2D.ObtenerNombre(personaje) + ".");
        }

        Exigir(!DialogosHistoria2D.TieneConversacion(PersonajeHistoria2D.SinAsignar) &&
               DialogosHistoria2D.ObtenerLineas(PersonajeHistoria2D.SinAsignar).Length == 0,
            "Un personaje sin asignar no debe ofrecer interaccion.");
    }

    private static void ProbarPersonajeInteractuable()
    {
        GameObject objeto = new GameObject("Prueba - personaje secundario");
        try
        {
            BoxCollider2D trigger = objeto.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            PersonajeSecundario2D personaje = objeto.AddComponent<PersonajeSecundario2D>();
            EstablecerPersonaje(personaje, PersonajeHistoria2D.Teren);

            Exigir(personaje.EstaInteractuableDisponible && trigger.isTrigger,
                "El personaje debe estar disponible dentro de su zona de interaccion.");
            Exigir(personaje.TextoIndicacion.Contains("Teren") &&
                   personaje.TextoIndicacion.Contains("E / Y"),
                "La indicacion debe mostrar el personaje y los controles de interaccion.");
            Exigir(personaje.ObtenerLineasDialogo().Length == 3 &&
                   personaje.VelocidadTexto > 0f,
                "El texto y su velocidad deben tener valores utilizables.");

            EstablecerPersonaje(personaje, PersonajeHistoria2D.SinAsignar);
            Exigir(!personaje.EstaInteractuableDisponible,
                "Un personaje sin asignar no debe entrar en la lista de interaccion.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(objeto);
        }
    }

    private static void ProbarControlesDeDialogo()
    {
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        Exigir(acciones != null, "Falta el InputActionAsset del proyecto.");

        InputAction avanzar = acciones.FindAction("UI/Submit", false);
        InputAction cerrar = acciones.FindAction("UI/Cancel", false);
        InputAction interactuar = acciones.FindAction("Player/Interact", false);
        Exigir(avanzar != null && cerrar != null && interactuar != null,
            "Los mapas Player y UI necesitan interaccion, aceptar y volver.");
        Exigir(avanzar.bindings.Any(binding => binding.path == "*/{Submit}") &&
               cerrar.bindings.Any(binding => binding.path == "*/{Cancel}"),
            "Avanzar y cerrar deben usar las acciones compartidas de la interfaz.");
        Exigir(interactuar.bindings.Any(binding => binding.path == "<Keyboard>/e") &&
               interactuar.bindings.Any(binding => binding.path == "<Gamepad>/buttonNorth"),
            "La interaccion debe estar disponible con teclado y mando.");
    }

    private static void ProbarCierreSeguroSinObjetivos()
    {
        Exigir(!InterfazJuego2D.MostrarDialogo(null, null) &&
               !InterfazJuego2D.DialogoAbierto,
            "La interfaz debe rechazar un dialogo sin personaje ni jugador.");
    }

    private static void EstablecerPersonaje(
        PersonajeSecundario2D personaje,
        PersonajeHistoria2D tipo)
    {
        SerializedObject serializado = new SerializedObject(personaje);
        SerializedProperty propiedad = serializado.FindProperty("personaje");
        Exigir(propiedad != null, "El personaje necesita un tipo editable.");
        propiedad.enumValueIndex = Convert.ToInt32(tipo);
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}
