using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasCombate2D
{
    private const int CapaPrueba = 8;

    [MenuItem("Demo/Validar combate cuerpo a cuerpo")]
    public static void Ejecutar()
    {
        int pruebas = 0;
        ProbarGolpeUnicoYRecuperacion();
        pruebas++;
        ProbarLimitesYDireccion();
        pruebas++;
        ProbarInvulnerabilidadYRetroceso();
        pruebas++;

        Debug.Log("Pruebas de combate correctas: " + pruebas +
                  " grupos (impacto unico, alcance/direccion, invulnerabilidad/retroceso).");
    }

    private static void ProbarGolpeUnicoYRecuperacion()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoObjeto = CrearEnemigo(new Vector2(0.5f, 0f), 3, true);
        GameObject enemigoIAObjeto = CrearEnemigoIA(new Vector2(0.85f, 0f), 3);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            EnemigoBasico2D enemigo = enemigoObjeto.GetComponent<EnemigoBasico2D>();
            EnemigoIA2D enemigoIA = enemigoIAObjeto.GetComponent<EnemigoIA2D>();
            Invocar(jugador, "Atacar");

            Exigir(enemigo.VidaActual == 2, "Un ataque debe quitar un punto de vida aunque alcance varios colliders.");
            Exigir(enemigoIA.VidaActual == 2, "El mismo ataque debe ser compatible con el enemigo con IA.");
            Exigir(enemigoObjeto.GetComponent<Rigidbody2D>().linearVelocity.x > 0f,
                "El enemigo debe retroceder alejandose del jugador.");
            Exigir(enemigoIAObjeto.GetComponent<Rigidbody2D>().linearVelocity.x > 0f,
                "El enemigo con IA tambien debe retroceder al recibir dano.");
            Exigir(Mathf.Approximately(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity.x, -2f),
                "Un ataque que alcanza varios objetivos solo debe aplicar un impulso al jugador.");

            float recuperacionHasta = (float)LeerCampoPrivado(jugador, "ataqueDisponibleDesde");
            Invocar(jugador, "Atacar");
            Exigir(enemigo.VidaActual == 2, "La recuperacion debe impedir repetir el ataque inmediatamente.");
            Exigir(enemigoIA.VidaActual == 2, "La recuperacion debe cubrir todos los tipos de enemigo.");
            Exigir(Mathf.Approximately(recuperacionHasta,
                    (float)LeerCampoPrivado(jugador, "ataqueDisponibleDesde")),
                "Un ataque durante la recuperacion no debe iniciar otro tajo.");

            enemigo.RecibirDanio(1, jugador.transform.position, 2f);
            Exigir(enemigo.VidaActual == 2, "La invulnerabilidad del enemigo debe rechazar un golpe inmediato.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoObjeto, enemigoIAObjeto, acciones);
        }

        acciones = CrearAcciones();
        jugadorObjeto = CrearJugador(acciones);
        enemigoObjeto = CrearEnemigo(new Vector2(0.5f, 0f), 3, false);
        try
        {
            EstablecerCampoPrivado(enemigoObjeto.GetComponent<EnemigoBasico2D>(),
                "puedeRecibirGolpeDesde", Time.time + 1f);
            Physics2D.SyncTransforms();
            Invocar(jugadorObjeto.GetComponent<Jugador2D>(), "Atacar");
            Exigir(Mathf.Approximately(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity.x, 0f),
                "Un impacto rechazado por invulnerabilidad no debe causar retroceso al jugador.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoObjeto, acciones);
        }
    }

    private static void ProbarLimitesYDireccion()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoFueraDeAlcance = CrearEnemigo(new Vector2(1.5f, 0f), 3, false);
        GameObject enemigoDetras = CrearEnemigo(new Vector2(-0.5f, 0f), 3, false);
        GameObject enemigoElevado = CrearEnemigo(new Vector2(0.5f, 1.5f), 3, false);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Physics2D.SyncTransforms();
            Invocar(jugador, "Atacar");
            Exigir(enemigoFueraDeAlcance.GetComponent<EnemigoBasico2D>().VidaActual == 3,
                "Un objetivo fuera del alcance no debe recibir dano.");
            Exigir(enemigoDetras.GetComponent<EnemigoBasico2D>().VidaActual == 3,
                "Un objetivo a la espalda no debe recibir dano.");
            Exigir(enemigoElevado.GetComponent<EnemigoBasico2D>().VidaActual == 3,
                "Un objetivo fuera de la altura del tajo no debe recibir dano.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoFueraDeAlcance, enemigoDetras, enemigoElevado, acciones);
        }

        acciones = CrearAcciones();
        jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoIzquierdo = CrearEnemigo(new Vector2(-0.5f, 0f), 3, false);
        try
        {
            EstablecerCampoPrivado(jugadorObjeto.GetComponent<Jugador2D>(), "mirandoDerecha", false);
            Physics2D.SyncTransforms();
            Invocar(jugadorObjeto.GetComponent<Jugador2D>(), "Atacar");
            Exigir(enemigoIzquierdo.GetComponent<EnemigoBasico2D>().VidaActual == 2,
                "El ataque debe alcanzar el lado al que mira el jugador.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoIzquierdo, acciones);
        }
    }

    private static void ProbarInvulnerabilidadYRetroceso()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        try
        {
            VidaJugador2D vida = jugadorObjeto.AddComponent<VidaJugador2D>();
            Invocar(vida, "Awake");

            Exigir(vida.RecibirDanio(1, new Vector2(1f, 0f)), "El primer impacto valido debe aplicar dano.");
            Exigir(vida.VidaActual == 4, "El primer impacto debe reducir la vida exactamente una vez.");
            Exigir(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity.x < 0f,
                "El retroceso del jugador debe alejarlo del atacante.");
            Exigir(!vida.RecibirDanio(1, new Vector2(1f, 0f)),
                "La invulnerabilidad temporal debe rechazar impactos consecutivos.");
            Exigir(vida.VidaActual == 4, "Los impactos durante la invulnerabilidad no deben reducir vida.");
            Exigir(!vida.RecibirDanio(0), "El dano cero debe rechazarse.");
            Exigir(vida.VidaActual == 4, "El dano invalido no debe cambiar la vida.");

            EstablecerCampoPrivado(vida, "puedeRecibirDanioDesde", Time.time - 1f);
            Exigir(vida.RecibirDanio(1), "El dano debe volver a recibirse tras expirar la invulnerabilidad.");

            EstablecerCampoPrivado(vida, "puedeRecibirDanioDesde", Time.time - 1f);
            EstablecerCampoPrivado(jugadorObjeto.GetComponent<Jugador2D>(),
                "<EstaDefendiendose>k__BackingField", true);
            Exigir(!vida.RecibirDanio(1), "La defensa activa debe seguir bloqueando dano.");
            EstablecerCampoPrivado(jugadorObjeto.GetComponent<Jugador2D>(),
                "<EstaDefendiendose>k__BackingField", false);
        }
        finally
        {
            Limpiar(jugadorObjeto, acciones);
        }

        acciones = CrearAcciones();
        jugadorObjeto = CrearJugador(acciones);
        try
        {
            VidaJugador2D vida = jugadorObjeto.AddComponent<VidaJugador2D>();
            Invocar(vida, "Awake");
            EstablecerCampoPrivado(jugadorObjeto.GetComponent<Jugador2D>(), "dashTerminaEn", Time.time + 1f);
            Exigir(!vida.RecibirDanio(1, Vector2.left), "El dash activo debe seguir bloqueando dano.");
            Exigir(vida.VidaActual == vida.VidaMaxima, "El dash no debe quitar vida.");
        }
        finally
        {
            Limpiar(jugadorObjeto, acciones);
        }
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

    private static GameObject CrearJugador(InputActionAsset acciones)
    {
        GameObject objeto = new GameObject("Prueba combate - jugador");
        objeto.SetActive(false);
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;

        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        EstablecerCampoPrivado(jugador, "capaEnemigos", (LayerMask)(1 << CapaPrueba));
        EstablecerCampoPrivado(jugador, "alcanceAtaque", 1f);
        EstablecerCampoPrivado(jugador, "alturaAtaque", 1f);
        EstablecerCampoPrivado(jugador, "tiempoRecuperacionAtaque", 0.35f);
        EstablecerCampoPrivado(jugador, "fuerzaRetroceso", 4f);
        EstablecerCampoPrivado(jugador, "impulsoRetrocesoJugador", 2f);
        objeto.SetActive(true);
        Invocar(jugador, "Awake");
        Physics2D.SyncTransforms();
        return objeto;
    }

    private static GameObject CrearEnemigo(Vector2 posicion, int vida, bool colliderHijo)
    {
        GameObject objeto = new GameObject("Prueba combate - enemigo");
        objeto.SetActive(false);
        objeto.layer = CapaPrueba;
        objeto.transform.position = posicion;
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 0.5f);
        EnemigoBasico2D enemigo = objeto.AddComponent<EnemigoBasico2D>();
        EstablecerCampoPrivado(enemigo, "vidaMaxima", vida);
        EstablecerCampoPrivado(enemigo, "tiempoInvulnerableTrasGolpe", 0.5f);

        if (colliderHijo)
        {
            GameObject hijo = new GameObject("Collider secundario");
            hijo.layer = CapaPrueba;
            hijo.transform.SetParent(objeto.transform, false);
            hijo.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 0.4f);
        }

        objeto.SetActive(true);
        Invocar(enemigo, "Awake");
        return objeto;
    }

    private static GameObject CrearEnemigoIA(Vector2 posicion, int vida)
    {
        GameObject objeto = new GameObject("Prueba combate - enemigo IA");
        objeto.SetActive(false);
        objeto.layer = CapaPrueba;
        objeto.transform.position = posicion;
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.4f, 0.5f);
        EnemigoIA2D enemigo = objeto.AddComponent<EnemigoIA2D>();
        EstablecerCampoPrivado(enemigo, "vidaMaxima", vida);
        EstablecerCampoPrivado(enemigo, "tiempoInvulnerableTrasGolpe", 0.5f);
        objeto.SetActive(true);
        Invocar(enemigo, "Awake");
        return objeto;
    }

    private static void Invocar(object objetivo, string nombre)
    {
        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);

        metodo.Invoke(objetivo, null);
    }

    private static void EstablecerCampoPrivado(object objetivo, string nombre, object valor)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        campo.SetValue(objetivo, valor);
    }

    private static object LeerCampoPrivado(object objetivo, string nombre)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        return campo.GetValue(objetivo);
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
}
