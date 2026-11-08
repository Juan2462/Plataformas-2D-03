using System;
using System.Collections.Generic;
using UnityEngine;

public enum PersonajeHistoria2D
{
    SinAsignar,
    Mara,
    Teren,
    Narel,
    ElMedidor
}

public sealed class PersonajeSecundario2D : Interactuable2D
{
    [SerializeField] private PersonajeHistoria2D personaje;
    [SerializeField] private string nombreVisible;
    [SerializeField, Min(1f)] private float velocidadTexto = 36f;
    [SerializeField, TextArea(2, 4)] private string[] lineasPersonalizadas;

    private bool avisoInteraccionEmitido;

    public PersonajeHistoria2D Personaje => personaje;
    public string NombreVisible => string.IsNullOrWhiteSpace(nombreVisible)
        ? DialogosHistoria2D.ObtenerNombre(personaje)
        : nombreVisible.Trim();
    public float VelocidadTexto => Mathf.Max(1f, velocidadTexto);

    public override bool EstaInteractuableDisponible =>
        base.EstaInteractuableDisponible &&
        personaje != PersonajeHistoria2D.SinAsignar &&
        TieneLineasDialogo();

    public override string TextoIndicacion =>
        "E / Y - Hablar con " + NombreVisible;

    public override void Interactuar(InventarioJugador2D inventario)
    {
        if (!EstaInteractuableDisponible)
            return;

        Jugador2D jugador = inventario != null
            ? inventario.GetComponent<Jugador2D>()
            : FindFirstObjectByType<Jugador2D>();

        if (jugador == null || !jugador.isActiveAndEnabled)
        {
            AvisarUnaVez("No se puede iniciar el dialogo: el jugador no esta disponible.");
            return;
        }

        if (!InterfazJuego2D.MostrarDialogo(this, jugador))
            AvisarUnaVez("No se puede iniciar el dialogo porque la interfaz no esta disponible.");
    }

    public string[] ObtenerLineasDialogo()
    {
        if (!TieneLineasPersonalizadas())
            return DialogosHistoria2D.ObtenerLineas(personaje);

        List<string> lineas = new List<string>();
        foreach (string linea in lineasPersonalizadas)
        {
            if (!string.IsNullOrWhiteSpace(linea))
                lineas.Add(linea.Trim());
        }

        return lineas.ToArray();
    }

    private bool TieneLineasDialogo()
    {
        return TieneLineasPersonalizadas() || DialogosHistoria2D.TieneConversacion(personaje);
    }

    private bool TieneLineasPersonalizadas()
    {
        if (lineasPersonalizadas == null)
            return false;

        foreach (string linea in lineasPersonalizadas)
        {
            if (!string.IsNullOrWhiteSpace(linea))
                return true;
        }

        return false;
    }

    private void AvisarUnaVez(string mensaje)
    {
        if (avisoInteraccionEmitido)
            return;

        avisoInteraccionEmitido = true;
        Debug.LogWarning(mensaje, this);
    }
}

public static class DialogosHistoria2D
{
    public static bool TieneConversacion(PersonajeHistoria2D personaje)
    {
        switch (personaje)
        {
            case PersonajeHistoria2D.Mara:
            case PersonajeHistoria2D.Teren:
            case PersonajeHistoria2D.Narel:
            case PersonajeHistoria2D.ElMedidor:
                return true;
            default:
                return false;
        }
    }

    public static string ObtenerNombre(PersonajeHistoria2D personaje)
    {
        switch (personaje)
        {
            case PersonajeHistoria2D.Mara:
                return "Mara";
            case PersonajeHistoria2D.Teren:
                return "Teren";
            case PersonajeHistoria2D.Narel:
                return "Narel";
            case PersonajeHistoria2D.ElMedidor:
                return "El Medidor";
            default:
                return "Personaje sin asignar";
        }
    }

    public static string[] ObtenerLineas(PersonajeHistoria2D personaje)
    {
        switch (personaje)
        {
            case PersonajeHistoria2D.Mara:
                return new[]
                {
                    "La cisterna pierde agua mas rapido de lo que puedo recuperarla.",
                    "No vuelvas con una solucion que solo funcione para un barrio.",
                    "El resonador era de tu madre. Escucha antes de forzar una compuerta."
                };
            case PersonajeHistoria2D.Teren:
                return new[]
                {
                    "Las campanas de ceramica registran la presion. Fijate en sus marcas.",
                    "Creia que el diapasón se apago por accidente.",
                    "Busquemos las señales de mantenimiento antes de sacar conclusiones."
                };
            case PersonajeHistoria2D.Narel:
                return new[]
                {
                    "Las cisternas bajas sobrevivieron con reservas pequeñas.",
                    "No abrire el acceso central si el sistema vuelve a dar prioridad a las terrazas.",
                    "Las cuatro estaciones pueden repartir el flujo. Comprobemos que siguen sincronizadas."
                };
            case PersonajeHistoria2D.ElMedidor:
                return new[]
                {
                    "PROTOCOLO: resonancia no reconocida.",
                    "CONFIGURACION ORIGINAL: prioridad de terrazas.",
                    "AVISO: modificacion de compuertas bloqueada."
                };
            default:
                return Array.Empty<string>();
        }
    }
}
