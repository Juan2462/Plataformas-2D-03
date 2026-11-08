using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasSistemasHollowKnight2D
{
    private const int CapaPrueba = 8;

    [MenuItem("Demo/Validar sistemas Hollow Knight (Peligros, Focus y Descarga)")]
    public static void Ejecutar()
    {
        int pruebas = 0;
        ProbarRespawnSeguroEnPeligros();
        pruebas++;
        ProbarResonanciaYCuracionConcentrada();
        pruebas++;
        ProbarDescargaPresionYRoturaSellos();
        pruebas++;

        Debug.Log("Pruebas completas de sistemas Hollow Knight superadas con exito: " + pruebas +
                  " grupos (respawn seguro en peligros, concentracion de resonancia, descarga de presion y sellos de sal).");
    }

    private static void ProbarRespawnSeguroEnPeligros()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            VidaJugador2D vida = jugadorObjeto.AddComponent<VidaJugador2D>();
            Invocar(vida, "Awake");

            // 1. Simular suelo seguro en (5, 0)
            jugadorObjeto.transform.position = new Vector2(5f, 0f);
            EstablecerCampoPrivado(jugador, "capaSuelo", (LayerMask)(1 << CapaPrueba));
            vida.ActualizarSueloSeguro();

            Exigir(vida.UltimoSueloSeguro == new Vector2(5f, 0f),
                "El suelo seguro inicial debe registrar la posicion firme del jugador.");

            // 2. Jugador salta y cae sobre un foso o pincho en (12, -4)
            jugadorObjeto.transform.position = new Vector2(12f, -4f);
            jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity = new Vector2(2f, -10f);

            bool danado = vida.RecibirDanioPeligroAmbiental(1);

            Exigir(danado, "El peligro ambiental debe procesar el dano recibido.");
            Exigir(vida.VidaActual == vida.VidaMaxima - 1,
                "El peligro ambiental debe restar exactamente 1 punto de integridad.");
            Exigir(Mathf.Approximately(jugadorObjeto.transform.position.x, 5f) &&
                   Mathf.Approximately(jugadorObjeto.transform.position.y, 0f),
                "El jugador debe ser teletransportado inmediatamente a su ultimo suelo seguro.");
            Exigir(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity == Vector2.zero,
                "La velocidad al reaparecer tras un peligro ambiental debe ser nula.");
        }
        finally
        {
            Limpiar(jugadorObjeto, acciones);
        }
    }

    private static void ProbarResonanciaYCuracionConcentrada()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);

        try
        {
            VidaJugador2D vida = jugadorObjeto.AddComponent<VidaJugador2D>();
            Invocar(vida, "Awake");

            ResonanciaJugador2D resonancia = jugadorObjeto.AddComponent<ResonanciaJugador2D>();
            resonancia.Configurar(inicial: 0, maximo: 100, coste: 33, ganancia: 11, tiempoReq: 0.1f);
            Invocar(resonancia, "Awake");
            Invocar(resonancia, "Start");

            Exigir(resonancia.ResonanciaActual == 0, "La resonancia inicial debe ser 0.");

            // Simular daño recibido: 5 -> 3
            vida.RecibirDanio(2);
            Exigir(vida.VidaActual == 3, "La vida tras el dano inicial debe ser 3.");

            // No puede concentrarse sin resonancia
            Exigir(!resonancia.PuedeConcentrarse(), "No debe poder concentrarse con resonancia insuficiente.");

            // Simular 3 impactos exitosos con el resonador
            resonancia.GanarResonancia(11);
            resonancia.GanarResonancia(11);
            resonancia.GanarResonancia(11);
            Exigir(resonancia.ResonanciaActual == 33, "Tres impactos deben acumular exactamente 33 de resonancia.");

            // Ahora debe poder concentrarse
            Exigir(resonancia.PuedeConcentrarse(), "Con 33 de resonancia y dano previo, debe poder concentrarse.");

            // Iniciar concentracion
            resonancia.IniciarConcentracion();
            Exigir(resonancia.EstaConcentrandose, "La concentracion debe estar activa.");

            // Simular completar la curacion
            Invocar(resonancia, "CompletarCuracion");
            Exigir(vida.VidaActual == 4, "La concentracion exitosa debe curar 1 punto de integridad.");
            Exigir(resonancia.ResonanciaActual == 0, "El coste de 33 debe consumirse de la reserva de resonancia.");

            // Probar interrupcion por dano: acumular 33 e iniciar
            resonancia.GanarResonancia(33);
            resonancia.IniciarConcentracion();
            Exigir(resonancia.EstaConcentrandose, "Debe reiniciar la concentracion.");

            // Recibir dano debe interrumpir la concentracion
            vida.RecibirDanio(1);
            Exigir(!resonancia.EstaConcentrandose, "Recibir dano debe interrumpir inmediatamente la concentracion.");
        }
        finally
        {
            Limpiar(jugadorObjeto, acciones);
        }
    }

    private static void ProbarDescargaPresionYRoturaSellos()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoObjeto = CrearEnemigo(new Vector2(0.5f, 0f), 4);

        GameObject selloObjeto = new GameObject("SelloSalFragil_Prueba");
        selloObjeto.layer = CapaPrueba;
        selloObjeto.transform.position = new Vector2(0f, -0.8f);
        BoxCollider2D colliderSello = selloObjeto.AddComponent<BoxCollider2D>();
        colliderSello.size = new Vector2(2f, 0.5f);
        SelloSalFragil2D sello = selloObjeto.AddComponent<SelloSalFragil2D>();

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Rigidbody2D rb = jugadorObjeto.GetComponent<Rigidbody2D>();

            // 1. Sin la habilidad, no puede iniciar la descarga
            Exigir(!jugador.DescargaPresionDesbloqueada, "La descarga de presion debe comenzar bloqueada.");
            Exigir(!jugador.IniciarDescargaPresion(), "No debe iniciar descarga de presion sin la habilidad.");

            // 2. Desbloquear habilidad
            jugador.DesbloquearHabilidad(HabilidadExploracion2D.DescargaPresion);
            Exigir(jugador.DescargaPresionDesbloqueada, "La habilidad debe figurar como desbloqueada.");

            // 3. En el aire, activar la descarga
            rb.linearVelocity = new Vector2(0f, 2f);
            Physics2D.SyncTransforms();

            bool iniciada = jugador.IniciarDescargaPresion();
            Exigir(iniciada && jugador.EstaEnDescargaPresion,
                "En el aire, la descarga de presion debe activarse.");
            Exigir(rb.linearVelocity.y < -20f,
                "La descarga de presion debe impulsar a Sira en picado vertical hacia abajo.");

            // 4. Al impactar en el suelo, rompe el sello y dana al enemigo
            Invocar(jugador, "FinalizarDescargaPresion");

            Exigir(!jugador.EstaEnDescargaPresion, "Al aterrizar debe finalizar el estado de descarga.");
            Exigir(sello.EstaRoto, "La onda expansiva de la descarga debe quebrar el sello de sal fragil.");
            Exigir(enemigoObjeto.GetComponent<EnemigoBasico2D>().VidaActual == 2,
                "El impacto en picado debe aplicar 2 puntos de dano sismico a los enemigos en el area.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoObjeto, selloObjeto, acciones);
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
        GameObject objeto = new GameObject("Prueba HK - jugador");
        objeto.SetActive(false);
        objeto.layer = CapaPrueba;
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 1f);

        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        jugador.velocidadDescargaPresion = 25f;
        jugador.radioImpactoDescarga = 2.5f;
        jugador.danioDescarga = 2;

        objeto.SetActive(true);
        Invocar(jugador, "Awake");
        Physics2D.SyncTransforms();
        return objeto;
    }

    private static GameObject CrearEnemigo(Vector2 posicion, int vida)
    {
        GameObject objeto = new GameObject("Prueba HK - enemigo");
        objeto.SetActive(false);
        objeto.layer = CapaPrueba;
        objeto.transform.position = posicion;
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 0.5f);
        EnemigoBasico2D enemigo = objeto.AddComponent<EnemigoBasico2D>();
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
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
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
