using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1000)]
public sealed class GestorReaparicion2D : MonoBehaviour
{
    private static GestorReaparicion2D instancia;
    private static string escenaTransicionPendiente;
    private static string entradaTransicionPendiente;

    [SerializeField, Min(0f)] private float demoraReaparicion = 1f;

    private VidaJugador2D vidaActual;
    private Vector2 puntoInicioZona;
    private bool reaparicionEnTransicion;
    private Coroutine prepararEscena;
    private Coroutine reaparicionLocal;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void CrearGestorAlIniciar()
    {
        if (instancia != null)
            return;

        GameObject objeto = new GameObject("Gestor de reaparicion");
        objeto.AddComponent<GestorReaparicion2D>();
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
        SceneManager.sceneLoaded += AlCargarEscena;
    }

    private void OnDestroy()
    {
        if (instancia != this)
            return;

        SceneManager.sceneLoaded -= AlCargarEscena;
        if (vidaActual != null)
            vidaActual.JugadorDerrotado -= AlDerrotarJugador;
        instancia = null;
    }

    public static bool SolicitarTransicion(string rutaEscena, string idEntrada)
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Las transiciones de zona solo se pueden iniciar durante el juego.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(rutaEscena) || string.IsNullOrWhiteSpace(idEntrada))
        {
            Debug.LogError("La transicion necesita una escena y un identificador de entrada.");
            return false;
        }

        int indiceEscena = SceneUtility.GetBuildIndexByScenePath(rutaEscena);
        if (indiceEscena < 0)
        {
            Debug.LogError("La escena de destino no esta incluida en Build Settings: " + rutaEscena);
            return false;
        }

        escenaTransicionPendiente = rutaEscena;
        entradaTransicionPendiente = idEntrada;
        AsyncOperation carga = SceneManager.LoadSceneAsync(indiceEscena, LoadSceneMode.Single);
        if (carga == null)
        {
            escenaTransicionPendiente = null;
            entradaTransicionPendiente = null;
            Debug.LogError("Unity no pudo iniciar la carga de la zona: " + rutaEscena);
            return false;
        }

        return true;
    }

    public static bool IntentarObtenerDestinoGuardado(
        string escenaActual,
        Vector2 inicioZona,
        out string escenaDestino,
        out Vector2 posicionDestino)
    {
        if (IntentarObtenerPuntoControlDisponible(out RegistroPuntoControl2D punto, out _))
        {
            escenaDestino = punto.RutaEscena;
            posicionDestino = punto.Posicion;
            return true;
        }

        escenaDestino = escenaActual;
        posicionDestino = inicioZona;
        return false;
    }

    public static bool EscenaDisponibleEnBuildSettings(string rutaEscena)
    {
        return !string.IsNullOrWhiteSpace(rutaEscena) &&
               SceneUtility.GetBuildIndexByScenePath(rutaEscena) >= 0;
    }

    private static bool IntentarObtenerPuntoControlDisponible(
        out RegistroPuntoControl2D punto,
        out int indiceEscena)
    {
        punto = ProgresoPartida2D.UltimoPuntoControl;
        indiceEscena = punto != null && EscenaDisponibleEnBuildSettings(punto.RutaEscena)
            ? SceneUtility.GetBuildIndexByScenePath(punto.RutaEscena)
            : -1;

        if (punto == null)
            return false;

        if (indiceEscena >= 0)
            return true;

        Debug.LogWarning("El punto de control guardado ya no esta disponible en Build Settings: " +
                         punto.RutaEscena +
                         ". Se eliminara el destino obsoleto y reapareceras al inicio de esta zona.");
        ProgresoPartida2D.EliminarPuntoControl();
        punto = null;
        indiceEscena = -1;
        return false;
    }

    private void AlCargarEscena(Scene escena, LoadSceneMode modo)
    {
        if (prepararEscena != null)
            StopCoroutine(prepararEscena);
        prepararEscena = StartCoroutine(PrepararJugadorEnEscena(escena));
    }

    private IEnumerator PrepararJugadorEnEscena(Scene escena)
    {
        yield return null;

        Jugador2D jugador = FindFirstObjectByType<Jugador2D>();
        if (jugador == null)
        {
            vidaActual = null;
            prepararEscena = null;
            yield break;
        }

        if (vidaActual != null)
            vidaActual.JugadorDerrotado -= AlDerrotarJugador;
        vidaActual = jugador.GetComponent<VidaJugador2D>();
        if (vidaActual == null)
        {
            Debug.LogError("El jugador de la zona necesita VidaJugador2D.", jugador);
            prepararEscena = null;
            yield break;
        }

        vidaActual.JugadorDerrotado -= AlDerrotarJugador;
        vidaActual.JugadorDerrotado += AlDerrotarJugador;
        puntoInicioZona = jugador.transform.position;

        if (!string.IsNullOrEmpty(escenaTransicionPendiente))
        {
            if (escena.path == escenaTransicionPendiente)
            {
                PuntoEntradaZona2D entrada = BuscarEntrada(escena, entradaTransicionPendiente);
                if (entrada != null)
                    ColocarJugador(jugador, entrada.transform.position);
                else
                    Debug.LogError("La zona no contiene el punto de entrada solicitado: " +
                                   entradaTransicionPendiente, jugador);
            }

            escenaTransicionPendiente = null;
            entradaTransicionPendiente = null;
            reaparicionEnTransicion = false;
            prepararEscena = null;
            yield break;
        }

        RegistroPuntoControl2D punto = ProgresoPartida2D.UltimoPuntoControl;
        if (reaparicionEnTransicion)
        {
            if (punto == null || punto.RutaEscena != escena.path)
            {
                Debug.LogError("La zona cargada no coincide con el punto de control de reaparicion.");
                reaparicionEnTransicion = false;
                prepararEscena = null;
                yield break;
            }

            yield return new WaitForSecondsRealtime(demoraReaparicion);
            RestaurarJugador(jugador, punto.Posicion);
            reaparicionEnTransicion = false;
        }
        else if (punto != null && punto.RutaEscena == escena.path)
        {
            RestaurarJugador(jugador, punto.Posicion);
        }

        prepararEscena = null;
    }

    private void AlDerrotarJugador()
    {
        if (reaparicionLocal != null || reaparicionEnTransicion)
            return;

        bool hayPuntoControl = IntentarObtenerPuntoControlDisponible(
            out RegistroPuntoControl2D punto,
            out int indiceEscena);
        Scene escenaActual = SceneManager.GetActiveScene();

        if (hayPuntoControl && punto.RutaEscena != escenaActual.path)
        {
            reaparicionEnTransicion = true;
            AsyncOperation carga = SceneManager.LoadSceneAsync(indiceEscena, LoadSceneMode.Single);
            if (carga == null)
            {
                reaparicionEnTransicion = false;
                Debug.LogError("No se pudo volver a cargar la escena del punto de control: " +
                               punto.RutaEscena);
            }
            return;
        }

        Vector2 destino = hayPuntoControl ? punto.Posicion : puntoInicioZona;
        reaparicionLocal = StartCoroutine(ReaparecerEnEscenaActual(destino));
    }

    private IEnumerator ReaparecerEnEscenaActual(Vector2 destino)
    {
        yield return new WaitForSecondsRealtime(demoraReaparicion);
        if (vidaActual != null && !vidaActual.EstaVivo &&
            vidaActual.RespawnearEnPuntoControl(destino))
        {
            Jugador2D jugador = vidaActual.GetComponent<Jugador2D>();
            if (jugador != null)
                puntoInicioZona = destino;
        }

        reaparicionLocal = null;
    }

    private void RestaurarJugador(Jugador2D jugador, Vector2 posicion)
    {
        VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            Debug.LogError("No se puede restaurar un jugador sin VidaJugador2D.", jugador);
            return;
        }

        vida.RespawnearEnPuntoControl(posicion);
        puntoInicioZona = posicion;
    }

    private static void ColocarJugador(Jugador2D jugador, Vector2 posicion)
    {
        Rigidbody2D rb = jugador.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            rb.position = posicion;
            rb.linearVelocity = Vector2.zero;
            Physics2D.SyncTransforms();
        }
        else
        {
            jugador.transform.position = posicion;
        }
    }

    private static PuntoEntradaZona2D BuscarEntrada(Scene escena, string idEntrada)
    {
        foreach (GameObject objetoRaiz in escena.GetRootGameObjects())
        {
            PuntoEntradaZona2D[] entradas = objetoRaiz.GetComponentsInChildren<PuntoEntradaZona2D>(true);
            foreach (PuntoEntradaZona2D entrada in entradas)
            {
                if (entrada.IdEntrada == idEntrada)
                    return entrada;
            }
        }

        return null;
    }
}
