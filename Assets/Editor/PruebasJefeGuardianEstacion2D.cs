using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class PruebasJefeGuardianEstacion2D
{
    [MenuItem("Demo/Validar jefe: Guardian de la Estacion")]
    public static void Ejecutar()
    {
        ProbarGolpeAnticipadoYVentana();
        ProbarSobrecargaYOndaEsquivable();
        ProbarDerrotaRecompensaYAtajo();
        ProbarIntegracionEscena();

        Debug.Log("Pruebas del Guardian correctas: anticipacion, fases, ataques, ventana de dano, progresion y escena.");
    }

    private static void ProbarGolpeAnticipadoYVentana()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones, Vector2.right);
        GameObject jefeObjeto = CrearJefe(new Vector2(0f, 0f), 8);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            VidaJugador2D vidaJugador = jugadorObjeto.GetComponent<VidaJugador2D>();
            JefeGuardianEstacion2D jefe = jefeObjeto.GetComponent<JefeGuardianEstacion2D>();
            Establecer(jefe, "jugador", jugador);
            Establecer(jefe, "rangoAtaque", 3f);
            Physics2D.SyncTransforms();

            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.AnticipandoAtaque,
                "El primer golpe debe mostrar su anticipacion antes de ejecutarse.");
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima,
                "El aviso del golpe no debe danar al jugador.");
            Exigir(jefeObjeto.GetComponent<SpriteRenderer>().color == new Color(1f, 0.54f, 0.16f),
                "El golpe debe tener una señal visual propia.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.Atacando,
                "El golpe debe ejecutarse al terminar la anticipacion.");
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima - 1,
                "El golpe cercano debe aplicar un punto de daño.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.Recuperandose,
                "El jefe debe abrir una ventana de recuperacion tras el golpe.");
            Exigir(jefe.RecibirDanio(1, jugador.transform.position, 0f),
                "El jefe debe aceptar daño durante su ventana de recuperacion.");
        }
        finally
        {
            Limpiar(jefeObjeto, jugadorObjeto, acciones);
        }
    }

    private static void ProbarSobrecargaYOndaEsquivable()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones, new Vector2(1f, 1.5f));
        GameObject jefeObjeto = CrearJefe(Vector2.zero, 8);
        HashSet<GameObject> proyectilesPrevios = EncontrarProyectiles();

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            VidaJugador2D vidaJugador = jugadorObjeto.GetComponent<VidaJugador2D>();
            JefeGuardianEstacion2D jefe = jefeObjeto.GetComponent<JefeGuardianEstacion2D>();
            Establecer(jefe, "jugador", jugador);
            Exigir(jefe.RecibirDanio(4, jugador.transform.position, 0f),
                "El golpe que cruza el umbral debe ser daño válido.");
            Exigir(jefe.EnSobrecarga,
                "La mitad de vida debe activar la segunda fase.");

            Establecer(jefe, "rangoAtaque", 3f);
            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Physics2D.SyncTransforms();
            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.AnticipandoAtaque,
                "El jefe debe anticipar la primera onda de sobrecarga.");
            Exigir(jefeObjeto.GetComponent<SpriteRenderer>().color == new Color(0.24f, 0.91f, 1f),
                "La onda debe tener una señal visual distinta del golpe.");
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima,
                "La onda no debe dañar antes de ejecutarse.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Invocar(jefe, "Update");
            GameObject proyectil = EncontrarProyectiles()
                .FirstOrDefault(objeto => !proyectilesPrevios.Contains(objeto));
            Exigir(proyectil != null, "La onda debe crear un proyectil visible.");
            Invocar(proyectil.GetComponent<ProyectilPrototipoEnemigo2D>(), "FixedUpdate");
            Rigidbody2D rbProyectil = proyectil.GetComponent<Rigidbody2D>();
            Exigir(Mathf.Abs(rbProyectil.linearVelocity.y) < 0.001f,
                "La onda debe viajar horizontalmente y poder esquivarse saltando.");
            Physics2D.SyncTransforms();
            Exigir(!proyectil.GetComponent<Collider2D>().bounds.Intersects(
                    jugadorObjeto.GetComponent<Collider2D>().bounds),
                "El salto debe sacar al jugador de la trayectoria de la onda.");
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima,
                "La onda que pasa por debajo del jugador no debe dañarlo.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.Recuperandose,
                "La onda también debe abrir una ventana de recuperación.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Establecer(jefe, "ataqueDisponibleDesde", Time.time - 1f);
            Invocar(jefe, "Update");
            Exigir(jefe.EstadoActual == EnemigoPrototipo2D.Estado.AnticipandoAtaque &&
                   jefeObjeto.GetComponent<SpriteRenderer>().color == new Color(1f, 0.54f, 0.16f),
                "La fase dos debe alternar la onda con un golpe claramente anunciado.");

            Establecer(jefe, "estadoTerminaEn", Time.time - 1f);
            Invocar(jefe, "Update");
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima,
                "El golpe debe poder esquivarse saltando fuera de su tolerancia vertical.");
        }
        finally
        {
            Limpiar(EncontrarProyectiles().Where(objeto => !proyectilesPrevios.Contains(objeto)));
            Limpiar(jefeObjeto, jugadorObjeto, acciones);
        }
    }

    private static void ProbarDerrotaRecompensaYAtajo()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones, Vector2.right);
        GameObject jefeObjeto = CrearJefe(Vector2.zero, 4);
        GameObject recompensaObjeto = new GameObject("Prueba recompensa del Guardian");
        recompensaObjeto.SetActive(false);
        BoxCollider2D colliderRecompensa = recompensaObjeto.AddComponent<BoxCollider2D>();
        RecompensaGuardianDash2D recompensa = recompensaObjeto.AddComponent<RecompensaGuardianDash2D>();
        Establecer(recompensa, "guardian", jefeObjeto.GetComponent<JefeGuardianEstacion2D>());
        recompensaObjeto.SetActive(true);
        Invocar(recompensa, "Awake");

        GameObject atajoObjeto = new GameObject("Prueba atajo del Canal");
        atajoObjeto.SetActive(false);
        atajoObjeto.AddComponent<BoxCollider2D>();
        AtajoCanal2D atajo = atajoObjeto.AddComponent<AtajoCanal2D>();
        Establecer(atajo, "guardian", jefeObjeto.GetComponent<JefeGuardianEstacion2D>());
        atajoObjeto.SetActive(true);

        try
        {
            JefeGuardianEstacion2D jefe = jefeObjeto.GetComponent<JefeGuardianEstacion2D>();
            Exigir(!colliderRecompensa.enabled,
                "El cristal debe permanecer desactivado antes de derrotar al jefe.");
            Invocar(atajo, "Update");
            Exigir(!atajo.EstaDisponible,
                "El atajo debe permanecer cerrado mientras el jefe vive.");

            Exigir(jefe.RecibirDanio(jefe.VidaActual, Vector2.left, 0f),
                "El golpe letal debe derrotar al jefe.");
            Exigir(!jefe.EstaVivo &&
                   !jefe.GetComponent<Collider2D>().enabled,
                "La derrota debe detener al jefe y desactivar su collider.");

            Invocar(recompensa, "Update");
            Invocar(atajo, "Update");
            Exigir(colliderRecompensa.enabled,
                "El cristal debe activarse tras la derrota.");
            Exigir(atajo.EstaDisponible,
                "El atajo debe abrirse tras la derrota.");

            Invocar(recompensa, "OnTriggerEnter2D", jugadorObjeto.GetComponent<Collider2D>());
            Exigir(jugadorObjeto.GetComponent<Jugador2D>().DashDesbloqueado,
                "Recoger el cristal posterior a la victoria debe desbloquear el Dash.");
            Exigir(!recompensaObjeto.activeSelf,
                "El cristal debe desaparecer después de recogerse.");
        }
        finally
        {
            Limpiar(atajoObjeto, recompensaObjeto, jefeObjeto, jugadorObjeto, acciones);
        }
    }

    private static void ProbarIntegracionEscena()
    {
        Scene escena = EditorSceneManager.OpenScene(
            "Assets/Scenes/CanalCampanas.unity",
            OpenSceneMode.Single
        );
        GameObject raiz = escena.GetRootGameObjects()
            .FirstOrDefault(objeto => objeto.name == "Zona - Canal de las Campanas");
        Exigir(raiz != null, "La escena del Canal debe contener su raiz de zona.");

        JefeGuardianEstacion2D[] jefes = raiz.GetComponentsInChildren<JefeGuardianEstacion2D>(true);
        RecompensaGuardianDash2D recompensa = raiz.GetComponentInChildren<RecompensaGuardianDash2D>(true);
        AtajoCanal2D atajo = raiz.GetComponentInChildren<AtajoCanal2D>(true);
        Exigir(jefes.Length == 1 && recompensa != null && atajo != null,
            "La escena debe incluir el jefe, el cristal y el atajo.");
        JefeGuardianEstacion2D jefe = jefes[0];
        Exigir(jefe.VidaMaxima == 12 && jefe.Interfaz != null,
            "El jefe de escena debe tener su resistencia y la interfaz de zona.");
        Exigir(recompensa.Guardian == jefe && atajo.Guardian == jefe,
            "El cristal y el atajo deben esperar a la derrota del mismo jefe.");
        Exigir(!recompensa.GetComponent<Collider2D>().enabled && !atajo.EstaDisponible,
            "La progresion debe empezar bloqueada hasta derrotar al jefe.");
    }

    private static GameObject CrearJefe(Vector2 posicion, int vida)
    {
        GameObject objeto = new GameObject("Prueba - Guardian de la Estacion");
        objeto.SetActive(false);
        objeto.transform.position = posicion;
        objeto.AddComponent<Rigidbody2D>().gravityScale = 0f;
        objeto.AddComponent<BoxCollider2D>();
        objeto.AddComponent<SpriteRenderer>();

        JefeGuardianEstacion2D jefe = objeto.AddComponent<JefeGuardianEstacion2D>();
        Establecer(jefe, "vidaMaxima", vida);
        Establecer(jefe, "rangoDeteccion", 10f);
        Establecer(jefe, "rangoAtaque", 3f);
        Establecer(jefe, "anticipacionAtaque", 0.6f);
        Establecer(jefe, "duracionAtaque", 0.2f);
        Establecer(jefe, "recuperacionAtaque", 0.8f);
        Establecer(jefe, "tiempoEntreAtaques", 1f);
        Establecer(jefe, "tiempoAturdido", 0.1f);
        Establecer(jefe, "tiempoInvulnerableTrasGolpe", 0f);
        Establecer(jefe, "alcanceGolpe", 2f);
        Establecer(jefe, "toleranciaAlturaGolpe", 0.6f);
        Establecer(jefe, "alcanceOnda", 5f);
        objeto.SetActive(true);
        Invocar(jefe, "Awake");
        return objeto;
    }

    private static GameObject CrearJugador(InputActionAsset acciones, Vector2 posicion)
    {
        GameObject objeto = new GameObject("Prueba - jugador");
        objeto.SetActive(false);
        objeto.transform.position = posicion;
        objeto.AddComponent<Rigidbody2D>().gravityScale = 0f;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.72f, 1.45f);
        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        VidaJugador2D vida = objeto.AddComponent<VidaJugador2D>();
        objeto.SetActive(true);
        Invocar(jugador, "Awake");
        Invocar(vida, "Awake");
        return objeto;
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

    private static HashSet<GameObject> EncontrarProyectiles()
    {
        return UnityEngine.Object.FindObjectsByType<ProyectilPrototipoEnemigo2D>(
                FindObjectsSortMode.None)
            .Select(proyectil => proyectil.gameObject)
            .ToHashSet();
    }

    private static void Invocar(object objetivo, string nombre, params object[] argumentos)
    {
        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);
        metodo.Invoke(objetivo, argumentos);
    }

    private static void Establecer(object objetivo, string nombre, object valor)
    {
        FieldInfo campo = null;
        Type tipo = objetivo.GetType();
        while (tipo != null && campo == null)
        {
            campo = tipo.GetField(nombre,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            tipo = tipo.BaseType;
        }

        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        campo.SetValue(objetivo, valor);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void Limpiar(params UnityEngine.Object[] objetos)
    {
        foreach (UnityEngine.Object objeto in objetos)
        {
            if (objeto != null)
                UnityEngine.Object.DestroyImmediate(objeto);
        }
    }

    private static void Limpiar(IEnumerable<GameObject> objetos)
    {
        foreach (GameObject objeto in objetos)
        {
            if (objeto != null)
                UnityEngine.Object.DestroyImmediate(objeto);
        }
    }
}
