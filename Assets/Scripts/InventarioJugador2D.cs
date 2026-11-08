using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class EntradaInventario2D
{
    [SerializeField] private string id;
    [SerializeField, Min(1)] private int cantidad = 1;

    public string Id => id;
    public int Cantidad => cantidad;

    public EntradaInventario2D(string id, int cantidad)
    {
        this.id = id;
        this.cantidad = cantidad;
    }

    public void Agregar(int cantidad)
    {
        this.cantidad += cantidad;
    }

    public void Quitar(int cantidad)
    {
        this.cantidad -= cantidad;
    }
}

public enum TipoEntradaInventario2D
{
    Llave,
    Arma,
    Objeto
}

public class InventarioJugador2D : MonoBehaviour
{
    [Header("Guardado")]
    [SerializeField] private string idGuardado = "Jugador";

    [Header("Monedas")]
    [SerializeField, Min(0)] private int monedas;

    [Header("Llaves (ID y cantidad)")]
    [SerializeField] private List<EntradaInventario2D> llaves = new List<EntradaInventario2D>();

    [Header("Armas (ID y cantidad)")]
    [SerializeField] private List<EntradaInventario2D> armas = new List<EntradaInventario2D>();

    [Header("Objetos (ID y cantidad)")]
    [SerializeField] private List<EntradaInventario2D> objetos = new List<EntradaInventario2D>();

    private const int VersionDatos = 2;
    private const string PrefijoClaveGuardado = "InventarioJugador2D_";

    [Serializable]
    private class DatosGuardado
    {
        public int version = VersionDatos;
        public int monedas;
        public List<EntradaInventario2D> llaves;
        public List<EntradaInventario2D> armas;
        public List<EntradaInventario2D> objetos;
    }

    public int Monedas => monedas;
    public IReadOnlyList<EntradaInventario2D> Llaves => llaves;
    public IReadOnlyList<EntradaInventario2D> Armas => armas;
    public IReadOnlyList<EntradaInventario2D> Objetos => objetos;
    public bool TieneDatosGuardados => !string.IsNullOrWhiteSpace(idGuardado) &&
                                       PlayerPrefs.HasKey(ClaveGuardado);

    private string ClaveGuardado => PrefijoClaveGuardado + idGuardado;

    private void Awake()
    {
        Cargar();
    }

    public void AgregarMonedas(int cantidad)
    {
        if (cantidad <= 0)
        {
            Debug.LogWarning("La cantidad de monedas debe ser mayor que cero.", this);
            return;
        }

        monedas += cantidad;
        Guardar();
    }

    public bool IntentarGastarMonedas(int cantidad)
    {
        if (cantidad < 0)
        {
            Debug.LogError("No se puede gastar una cantidad negativa de monedas.", this);
            return false;
        }

        if (monedas < cantidad)
            return false;

        monedas -= cantidad;
        Guardar();
        return true;
    }

    public void AgregarLlave(string id, int cantidad = 1)
    {
        AgregarALista(llaves, id, cantidad);
    }

    public bool TieneLlave(string id)
    {
        return ObtenerCantidad(llaves, id) > 0;
    }

    public bool IntentarUsarLlave(string id)
    {
        bool usada = IntentarQuitarDeLista(llaves, id, 1);
        if (usada)
            Guardar();

        return usada;
    }

    public void AgregarArma(string id, int cantidad = 1)
    {
        AgregarALista(armas, id, cantidad);
    }

    public void AgregarObjeto(string id, int cantidad = 1)
    {
        AgregarALista(objetos, id, cantidad);
    }

    public bool TieneArma(string id)
    {
        return ObtenerCantidad(armas, id) > 0;
    }

    public bool TieneObjeto(string id)
    {
        return ObtenerCantidad(objetos, id) > 0;
    }

    public bool IntentarQuitarArma(string id, int cantidad = 1)
    {
        return IntentarQuitarYGuardar(armas, id, cantidad);
    }

    public bool IntentarQuitarObjeto(string id, int cantidad = 1)
    {
        return IntentarQuitarYGuardar(objetos, id, cantidad);
    }

    public bool IntentarTransferir(
        InventarioJugador2D destino,
        TipoEntradaInventario2D tipo,
        string id,
        int cantidad = 1)
    {
        if (destino == null || destino == this || string.IsNullOrWhiteSpace(id) || cantidad <= 0)
            return false;

        List<EntradaInventario2D> origen = ObtenerLista(tipo);
        List<EntradaInventario2D> receptor = destino.ObtenerLista(tipo);
        if (!IntentarQuitarDeLista(origen, id, cantidad))
            return false;

        AgregarAListaSinGuardar(receptor, id, cantidad);
        Guardar();
        destino.Guardar();
        return true;
    }

    public bool IntentarComprar(
        TipoEntradaInventario2D tipo,
        string id,
        int precio,
        int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(id) || precio < 0 || cantidad <= 0 ||
            monedas < precio || monedas > int.MaxValue - precio)
            return false;

        monedas -= precio;
        AgregarAListaSinGuardar(ObtenerLista(tipo), id, cantidad);
        Guardar();
        return true;
    }

    public bool IntentarVender(
        TipoEntradaInventario2D tipo,
        string id,
        int precio,
        int cantidad = 1)
    {
        if (string.IsNullOrWhiteSpace(id) || precio < 0 || cantidad <= 0 ||
            monedas > int.MaxValue - precio ||
            !IntentarQuitarDeLista(ObtenerLista(tipo), id, cantidad))
            return false;

        monedas += precio;
        Guardar();
        return true;
    }

    private void AgregarALista(List<EntradaInventario2D> lista, string id, int cantidad)
    {
        if (string.IsNullOrWhiteSpace(id) || cantidad <= 0)
        {
            Debug.LogWarning("El ID debe tener un valor y la cantidad debe ser mayor que cero.");
            return;
        }

        AgregarAListaSinGuardar(lista, id, cantidad);
        Guardar();
    }

    private static void AgregarAListaSinGuardar(List<EntradaInventario2D> lista, string id, int cantidad)
    {
        EntradaInventario2D entrada = lista.Find(item => item != null && item.Id == id);
        if (entrada != null)
            entrada.Agregar(cantidad);
        else
            lista.Add(new EntradaInventario2D(id, cantidad));
    }

    private bool IntentarQuitarYGuardar(List<EntradaInventario2D> lista, string id, int cantidad)
    {
        if (string.IsNullOrWhiteSpace(id) || cantidad <= 0 || !IntentarQuitarDeLista(lista, id, cantidad))
            return false;

        Guardar();
        return true;
    }

    private List<EntradaInventario2D> ObtenerLista(TipoEntradaInventario2D tipo)
    {
        switch (tipo)
        {
            case TipoEntradaInventario2D.Llave:
                return llaves;
            case TipoEntradaInventario2D.Arma:
                return armas;
            case TipoEntradaInventario2D.Objeto:
                return objetos;
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo), tipo, "Tipo de inventario no válido.");
        }
    }

    private static int ObtenerCantidad(List<EntradaInventario2D> lista, string id)
    {
        EntradaInventario2D entrada = lista.Find(item => item != null && item.Id == id);
        return entrada != null ? entrada.Cantidad : 0;
    }

    private static bool IntentarQuitarDeLista(List<EntradaInventario2D> lista, string id, int cantidad)
    {
        EntradaInventario2D entrada = lista.Find(item => item != null && item.Id == id);
        if (entrada == null || entrada.Cantidad < cantidad)
            return false;

        entrada.Quitar(cantidad);
        if (entrada.Cantidad == 0)
            lista.Remove(entrada);

        return true;
    }

    public void Guardar()
    {
        if (string.IsNullOrWhiteSpace(idGuardado))
        {
            Debug.LogError("Asigna un ID de guardado al inventario para poder guardar los datos.", this);
            return;
        }

        DatosGuardado datos = new DatosGuardado
        {
            monedas = monedas,
            llaves = llaves,
            armas = armas,
            objetos = objetos
        };

        PlayerPrefs.SetString(ClaveGuardado, JsonUtility.ToJson(datos));
        PlayerPrefs.Save();
    }

    public void Cargar()
    {
        if (string.IsNullOrWhiteSpace(idGuardado))
        {
            Debug.LogError("Asigna un ID de guardado al inventario para poder cargar los datos.", this);
            return;
        }

        if (!PlayerPrefs.HasKey(ClaveGuardado))
            return;

        try
        {
            DatosGuardado datos = JsonUtility.FromJson<DatosGuardado>(PlayerPrefs.GetString(ClaveGuardado));
            if (datos == null || (datos.version != 1 && datos.version != VersionDatos))
            {
                Debug.LogError("El guardado del inventario está vacío o usa una versión no compatible.", this);
                return;
            }

            monedas = Mathf.Max(0, datos.monedas);
            llaves = datos.llaves ?? new List<EntradaInventario2D>();
            armas = datos.armas ?? new List<EntradaInventario2D>();
            objetos = datos.objetos ?? new List<EntradaInventario2D>();
        }
        catch (ArgumentException excepcion)
        {
            Debug.LogError("No se pudo leer el guardado del inventario: " + excepcion.Message, this);
        }
    }

    private void OnApplicationPause(bool pausado)
    {
        if (pausado)
            Guardar();
    }

    private void OnApplicationQuit()
    {
        Guardar();
    }
}
