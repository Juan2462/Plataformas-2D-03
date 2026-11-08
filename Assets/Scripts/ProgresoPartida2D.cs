using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class RegistroPuntoControl2D
{
    [SerializeField] private string rutaEscena;
    [SerializeField] private string idZona;
    [SerializeField] private string idPunto;
    [SerializeField] private float posicionX;
    [SerializeField] private float posicionY;

    public string RutaEscena => rutaEscena;
    public string IdZona => idZona;
    public string IdPunto => idPunto;
    public Vector2 Posicion => new Vector2(posicionX, posicionY);

    public RegistroPuntoControl2D(
        string rutaEscena,
        string idZona,
        string idPunto,
        Vector2 posicion)
    {
        this.rutaEscena = rutaEscena;
        this.idZona = idZona;
        this.idPunto = idPunto;
        posicionX = posicion.x;
        posicionY = posicion.y;
    }
}

public static class ProgresoPartida2D
{
    private const string ClaveGuardado = "LasRuinasDelEco_ProgresoPartida_v1";

    [Serializable]
    private sealed class DatosPartida
    {
        public int version = 1;
        public RegistroPuntoControl2D puntoControl;
        public List<string> habilidades = new List<string>();
        public List<string> estaciones = new List<string>();
        public List<string> zonasDescubiertas = new List<string>();
        public List<CeldaExplorada> celdasExploradas = new List<CeldaExplorada>();
    }

    [Serializable]
    private sealed class CeldaExplorada
    {
        public string idZona;
        public int x;
        public int y;
    }

    private static DatosPartida datos;

    public static bool TienePuntoControl => ObtenerDatos().puntoControl != null;

    public static RegistroPuntoControl2D UltimoPuntoControl => ObtenerDatos().puntoControl;

    public static void GuardarPuntoControl(
        string rutaEscena,
        string idZona,
        string idPunto,
        Vector2 posicion)
    {
        if (string.IsNullOrWhiteSpace(rutaEscena) ||
            string.IsNullOrWhiteSpace(idZona) ||
            string.IsNullOrWhiteSpace(idPunto) ||
            float.IsNaN(posicion.x) || float.IsNaN(posicion.y) ||
            float.IsInfinity(posicion.x) || float.IsInfinity(posicion.y))
        {
            Debug.LogError("El punto de control necesita escena, zona, identificador y posicion validos.");
            return;
        }

        ObtenerDatos().puntoControl =
            new RegistroPuntoControl2D(rutaEscena, idZona, idPunto, posicion);
        Guardar();
    }

    public static bool TieneHabilidad(HabilidadExploracion2D habilidad)
    {
        return ObtenerDatos().habilidades.Contains(habilidad.ToString());
    }

    public static void RegistrarHabilidad(HabilidadExploracion2D habilidad)
    {
        if (!Enum.IsDefined(typeof(HabilidadExploracion2D), habilidad))
        {
            Debug.LogError("No se puede guardar una habilidad que no pertenece al catalogo.");
            return;
        }

        DatosPartida partida = ObtenerDatos();
        if (partida.habilidades.Contains(habilidad.ToString()))
            return;

        partida.habilidades.Add(habilidad.ToString());
        Guardar();
    }

    public static void RegistrarEstacion(string idZona, string idEstacion)
    {
        if (string.IsNullOrWhiteSpace(idZona) || string.IsNullOrWhiteSpace(idEstacion))
        {
            Debug.LogError("La estacion necesita identificadores de zona y estacion.");
            return;
        }

        string id = idZona + ":" + idEstacion;
        DatosPartida partida = ObtenerDatos();
        if (partida.estaciones.Contains(id))
            return;

        partida.estaciones.Add(id);
        Guardar();
    }

    public static bool EstacionActivada(string idZona, string idEstacion)
    {
        if (string.IsNullOrWhiteSpace(idZona) || string.IsNullOrWhiteSpace(idEstacion))
            return false;

        return ObtenerDatos().estaciones.Contains(idZona + ":" + idEstacion);
    }

    public static bool RegistrarZonaDescubierta(string idZona)
    {
        if (string.IsNullOrWhiteSpace(idZona))
        {
            Debug.LogError("La zona descubierta necesita un identificador.");
            return false;
        }

        DatosPartida partida = ObtenerDatos();
        if (partida.zonasDescubiertas.Contains(idZona))
            return false;

        partida.zonasDescubiertas.Add(idZona);
        Guardar();
        return true;
    }

    public static bool ZonaDescubierta(string idZona)
    {
        return !string.IsNullOrWhiteSpace(idZona) &&
               ObtenerDatos().zonasDescubiertas.Contains(idZona);
    }

    public static bool RegistrarCeldaExplorada(string idZona, int x, int y)
    {
        if (string.IsNullOrWhiteSpace(idZona))
        {
            Debug.LogError("La celda explorada necesita un identificador de zona.");
            return false;
        }

        DatosPartida partida = ObtenerDatos();
        if (partida.celdasExploradas.Exists(celda =>
                celda != null && celda.idZona == idZona && celda.x == x && celda.y == y))
            return false;

        partida.celdasExploradas.Add(new CeldaExplorada { idZona = idZona, x = x, y = y });
        Guardar();
        return true;
    }

    public static List<Vector2Int> ObtenerCeldasExploradas(string idZona)
    {
        List<Vector2Int> celdas = new List<Vector2Int>();
        if (string.IsNullOrWhiteSpace(idZona))
            return celdas;

        foreach (CeldaExplorada celda in ObtenerDatos().celdasExploradas)
        {
            if (celda != null && celda.idZona == idZona)
                celdas.Add(new Vector2Int(celda.x, celda.y));
        }

        return celdas;
    }

    public static void EliminarPuntoControl()
    {
        ObtenerDatos().puntoControl = null;
        Guardar();
    }

#if UNITY_EDITOR
    public static void LimpiarProgresoDePrueba()
    {
        PlayerPrefs.DeleteKey(ClaveGuardado);
        datos = null;
    }

    public static void RecargarProgresoDePrueba()
    {
        datos = null;
    }

    public static void LimpiarDescubrimientosDePrueba(string idZona)
    {
        DatosPartida partida = ObtenerDatos();
        partida.zonasDescubiertas.Remove(idZona);
        partida.celdasExploradas.RemoveAll(celda => celda != null && celda.idZona == idZona);
        Guardar();
    }
#endif

    private static DatosPartida ObtenerDatos()
    {
        if (datos != null)
            return datos;

        if (!PlayerPrefs.HasKey(ClaveGuardado))
        {
            datos = new DatosPartida();
            return datos;
        }

        string json = PlayerPrefs.GetString(ClaveGuardado);
        datos = JsonUtility.FromJson<DatosPartida>(json);
        if (datos == null || datos.version != 1)
        {
            Debug.LogWarning("El guardado de progreso no es compatible; se iniciara un registro nuevo.");
            datos = new DatosPartida();
        }

        if (datos.habilidades == null)
            datos.habilidades = new List<string>();
        if (datos.estaciones == null)
            datos.estaciones = new List<string>();
        if (datos.zonasDescubiertas == null)
            datos.zonasDescubiertas = new List<string>();
        if (datos.celdasExploradas == null)
            datos.celdasExploradas = new List<CeldaExplorada>();

        return datos;
    }

    private static void Guardar()
    {
        PlayerPrefs.SetString(ClaveGuardado, JsonUtility.ToJson(ObtenerDatos()));
        PlayerPrefs.Save();
    }
}
