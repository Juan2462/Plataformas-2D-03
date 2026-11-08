using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasAtaquesDireccionalesYPogo2D
{
    private const int CapaPrueba = 8;

    [MenuItem("Demo/Validar ataques direccionales y pogo")]
    public static void Ejecutar()
    {
        int pruebas = 0;
        ProbarAtaqueHaciaArriba();
        pruebas++;
        ProbarPogoSobreEnemigoYRecargaDash();
        pruebas++;
        ProbarPogoSobrePinchosDeSal();
        pruebas++;
        ProbarPogoSobreCampanaElastica();
        pruebas++;
        ProbarBloqueoPogoEnElSuelo();
        pruebas++;

        Debug.Log("Pruebas de ataques direccionales y pogo correctas: " + pruebas +
                  " grupos (ataque arriba, pogo enemigo + dash, pinchos de sal, campana elastica, seguridad en suelo).");
    }

    private static void ProbarAtaqueHaciaArriba()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoArriba = CrearEnemigo(new Vector2(0f, 0.75f), 3);
        GameObject enemigoLateral = CrearEnemigo(new Vector2(1.2f, 0f), 3);
        GameObject enemigoAbajo = CrearEnemigo(new Vector2(0f, -0.75f), 3);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            EnemigoBasico2D enemigoSup = enemigoArriba.GetComponent<EnemigoBasico2D>();
            EnemigoBasico2D enemigoLat = enemigoLateral.GetComponent<EnemigoBasico2D>();
            EnemigoBasico2D enemigoInf = enemigoAbajo.GetComponent<EnemigoBasico2D>();

            EstablecerCampoPrivado(jugador, "entradaMovimiento", Vector2.up);
            Physics2D.SyncTransforms();

            jugador.AtacarHacia(DireccionAtaque2D.Arriba);

            Exigir(jugador.UltimaDireccionAtaque == DireccionAtaque2D.Arriba,
                "La direccion de ataque registrada debe ser Arriba.");
            Exigir(enemigoSup.VidaActual == 2,
                "El enemigo situado arriba debe recibir un punto de dano con el ataque vertical ascendente.");
            Exigir(enemigoLat.VidaActual == 3,
                "El enemigo lateral fuera del rango vertical no debe recibir dano.");
            Exigir(enemigoInf.VidaActual == 3,
                "El enemigo inferior no debe recibir dano con ataque hacia arriba.");
            Exigir(Mathf.Approximately(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity.x, 0f),
                "El ataque hacia arriba no debe aplicar retroceso horizontal al jugador.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoArriba, enemigoLateral, enemigoAbajo, acciones);
        }
    }

    private static void ProbarPogoSobreEnemigoYRecargaDash()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject enemigoAbajo = CrearEnemigo(new Vector2(0f, -0.7f), 3);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Rigidbody2D rbJugador = jugadorObjeto.GetComponent<Rigidbody2D>();
            EnemigoBasico2D enemigo = enemigoAbajo.GetComponent<EnemigoBasico2D>();

            // Simular caída en el aire y cooldown de dash en el futuro
            rbJugador.linearVelocity = new Vector2(0f, -6f);
            float cooldownFuturo = Time.time + 10f;
            EstablecerCampoPrivado(jugador, "dashDisponibleEn", cooldownFuturo);
            EstablecerCampoPrivado(jugador, "entradaMovimiento", Vector2.down);
            Physics2D.SyncTransforms();

            jugador.AtacarHacia(DireccionAtaque2D.Abajo);

            Exigir(enemigo.VidaActual == 2,
                "El ataque hacia abajo en el aire debe aplicar dano al enemigo inferior.");
            Exigir(rbJugador.linearVelocity.y > 0f,
                "El impacto pogo contra un enemigo debe aplicar un rebote vertical positivo.");
            Exigir(Mathf.Approximately(rbJugador.linearVelocity.y, jugador.fuerzaRebotePogo),
                "La velocidad vertical de rebote debe coincidir con fuerzaRebotePogo.");

            float nuevoCooldown = (float)LeerCampoPrivado(jugador, "dashDisponibleEn");
            Exigir(nuevoCooldown <= Time.time + 0.05f,
                "Un pogo exitoso debe reiniciar el enfriamiento del Dash.");
        }
        finally
        {
            Limpiar(jugadorObjeto, enemigoAbajo, acciones);
        }
    }

    private static void ProbarPogoSobrePinchosDeSal()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        VidaJugador2D vidaJugador = jugadorObjeto.AddComponent<VidaJugador2D>();
        Invocar(vidaJugador, "Awake");

        GameObject pinchosObjeto = new GameObject("PinchosSal_Prueba");
        pinchosObjeto.layer = CapaPrueba;
        pinchosObjeto.transform.position = new Vector2(0f, -0.65f);
        BoxCollider2D colliderPinchos = pinchosObjeto.AddComponent<BoxCollider2D>();
        colliderPinchos.isTrigger = true;
        colliderPinchos.size = new Vector2(1.2f, 0.6f);
        PinchosSal2D pinchos = pinchosObjeto.AddComponent<PinchosSal2D>();
        pinchos.Configurar(1, 0.3f);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Rigidbody2D rbJugador = jugadorObjeto.GetComponent<Rigidbody2D>();
            rbJugador.linearVelocity = new Vector2(0f, -4f);

            Physics2D.SyncTransforms();

            // 1. Ejecutar ataque pogo sobre pinchos
            jugador.AtacarHacia(DireccionAtaque2D.Abajo);

            Exigir(rbJugador.linearVelocity.y > 0f,
                "El pogo sobre pinchos de sal debe impulsar al jugador hacia arriba.");
            Exigir(Mathf.Approximately(rbJugador.linearVelocity.y, jugador.fuerzaRebotePogo),
                "El rebote sobre pinchos debe usar fuerzaRebotePogo.");

            // Disparar trigger de contacto en el mismo frame
            InvocarMetodo(pinchos, "OnTriggerEnter2D", colliderPinchos);
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima,
                "La ventana de inmunidad post-pogo debe impedir que los pinchos dañen a Sira tras el rebote.");

            // 2. Probar que si expira la inmunidad y Sira cae sobre pinchos SIN pogo, sí recibe daño
            EstablecerCampoPrivado(pinchos, "inmuneHasta", Time.time - 1f);
            InvocarMetodo(pinchos, "OnTriggerEnter2D", jugadorObjeto.GetComponent<Collider2D>());
            Exigir(vidaJugador.VidaActual == vidaJugador.VidaMaxima - 1,
                "Tocar los pinchos sin pogo activo debe infligir dano al jugador.");
        }
        finally
        {
            Limpiar(jugadorObjeto, pinchosObjeto, acciones);
        }
    }

    private static void ProbarPogoSobreCampanaElastica()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);

        GameObject campanaObjeto = new GameObject("CampanaElastica_Prueba");
        campanaObjeto.layer = CapaPrueba;
        campanaObjeto.transform.position = new Vector2(0f, -0.65f);
        CircleCollider2D colliderCampana = campanaObjeto.AddComponent<CircleCollider2D>();
        colliderCampana.isTrigger = true;
        colliderCampana.radius = 0.5f;
        CampanaResonante2D campana = campanaObjeto.AddComponent<CampanaResonante2D>();
        campana.Configurar(15f);

        bool resono = false;
        campana.ResonanciaActivada += () => resono = true;

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Rigidbody2D rbJugador = jugadorObjeto.GetComponent<Rigidbody2D>();
            rbJugador.linearVelocity = new Vector2(0f, -3f);

            Physics2D.SyncTransforms();

            jugador.AtacarHacia(DireccionAtaque2D.Abajo);

            Exigir(resono, "El pogo debe activar la resonancia acustica de la campana.");
            Exigir(Mathf.Approximately(rbJugador.linearVelocity.y, 15f),
                "La campana elastica debe aplicar su fuerza de rebote personalizada (15f).");

            // Probar golpe horizontal
            resono = false;
            campanaObjeto.transform.position = new Vector2(0.6f, 0f);
            Physics2D.SyncTransforms();
            jugador.AtacarHacia(DireccionAtaque2D.Horizontal);

            Exigir(resono, "Un tajo lateral contra la campana tambien debe activar la resonancia.");
            Exigir(rbJugador.linearVelocity.x < 0f,
                "Un tajo lateral contra la campana debe producir retroceso normal en el jugador.");
        }
        finally
        {
            Limpiar(jugadorObjeto, campanaObjeto, acciones);
        }
    }

    private static void ProbarBloqueoPogoEnElSuelo()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);

        // Crear suelo bajo el comprobador de suelo
        GameObject sueloObjeto = new GameObject("Suelo");
        sueloObjeto.layer = CapaPrueba;
        sueloObjeto.transform.position = new Vector2(0f, -0.6f);
        BoxCollider2D colliderSuelo = sueloObjeto.AddComponent<BoxCollider2D>();
        colliderSuelo.size = new Vector2(4f, 0.4f);

        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Transform comprobador = new GameObject("ComprobadorSuelo").transform;
            comprobador.SetParent(jugadorObjeto.transform, false);
            comprobador.localPosition = new Vector2(0f, -0.5f);
            jugador.comprobadorSuelo = comprobador;
            jugador.capaSuelo = (LayerMask)(1 << CapaPrueba);
            EstablecerCampoPrivado(jugador, "radioSuelo", 0.2f);

            EstablecerCampoPrivado(jugador, "entradaMovimiento", Vector2.down);
            Physics2D.SyncTransforms();

            DireccionAtaque2D direccion = jugador.DeterminarDireccionAtaque();
            Exigir(direccion == DireccionAtaque2D.Horizontal,
                "Estando en el suelo, presionar abajo no debe iniciar pogo hacia el suelo, sino ataque horizontal.");
        }
        finally
        {
            Limpiar(jugadorObjeto, sueloObjeto, acciones);
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
        GameObject objeto = new GameObject("Prueba pogo - jugador");
        objeto.SetActive(false);
        objeto.layer = CapaPrueba;
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.constraints = RigidbodyConstraints2D.FreezeRotation;
        objeto.AddComponent<BoxCollider2D>().size = new Vector2(0.5f, 1f);

        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        jugador.fuerzaRebotePogo = 12.5f;
        jugador.recargarDashConPogo = true;
        EstablecerCampoPrivado(jugador, "capaEnemigos", (LayerMask)(1 << CapaPrueba));
        EstablecerCampoPrivado(jugador, "capaObstaculosRebotables", (LayerMask)(1 << CapaPrueba));
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

    private static GameObject CrearEnemigo(Vector2 posicion, int vida)
    {
        GameObject objeto = new GameObject("Prueba pogo - enemigo");
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

    private static void InvocarMetodo(object objetivo, string nombre, params object[] parametros)
    {
        Type[] tipos = new Type[parametros.Length];
        for (int i = 0; i < parametros.Length; i++)
            tipos[i] = parametros[i].GetType();

        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public,
            null,
            tipos,
            null
        );

        if (metodo == null)
        {
            // Fallback sin tipos estrictos
            MethodInfo[] metodos = objetivo.GetType().GetMethods(
                BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public
            );
            foreach (MethodInfo m in metodos)
            {
                if (m.Name == nombre && m.GetParameters().Length == parametros.Length)
                {
                    metodo = m;
                    break;
                }
            }
        }

        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);

        metodo.Invoke(objetivo, parametros);
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
