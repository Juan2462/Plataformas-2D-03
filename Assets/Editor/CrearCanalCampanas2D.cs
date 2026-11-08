using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class CrearCanalCampanas2D
{
    public const string RutaEscena = "Assets/Scenes/CanalCampanas.unity";
    private const string CarpetaArte = "Assets/Demo/CanalCampanas";
    private const string RutaJugador = "Assets/Sprites/Player/Movement/PlayerMovement.png";
    private const string RutaEnemigoUno = "Assets/Sprites/Enemies/Enemy 1..png";
    private const string RutaEnemigoDos = "Assets/Sprites/Enemies/Enemy2.png";
    private const string RutaControladorJugador =
        "Assets/Animation/AnimatorControllers/Player/Player AC.controller";
    private const string RutaControladorEnemigoUno =
        "Assets/Animation/AnimatorControllers/Enemies/Enemy1 AC.controller";
    private const string RutaControladorEnemigoDos =
        "Assets/Animation/AnimatorControllers/Enemies/Enemy2 AC.controller";

    [MenuItem("Demo/Generar zona: Canal de las Campanas")]
    public static void Generar()
    {
        ValidarRecursos();
        Directory.CreateDirectory(CarpetaArte);

        Sprite suelo = CrearSprite("CanalSuelo", new Color32(38, 72, 76, 255), new Color32(74, 124, 120, 255));
        Sprite piedra = CrearSprite("CanalPiedra", new Color32(48, 77, 78, 255), new Color32(91, 131, 117, 255));
        Sprite campana = CrearSprite("CampanaCeramica", new Color32(226, 181, 120, 255), new Color32(250, 225, 175, 255));
        Sprite cristal = CrearSprite("CristalResonador", new Color32(39, 143, 151, 255), new Color32(138, 243, 224, 255));
        Sprite plataforma = CrearSprite("PasarelaCeramica", new Color32(74, 104, 92, 255), new Color32(136, 155, 110, 255));

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene escena = SceneManager.GetActiveScene();
        escena.name = "Canal de las Campanas";

        int groundLayer = LayerMask.NameToLayer("Ground");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (groundLayer < 0 || enemyLayer < 0)
            throw new InvalidOperationException("La escena necesita las capas Ground y Enemy.");

        GameObject raiz = new GameObject("Zona - Canal de las Campanas");
        ZonaMetroidvania2D zona = raiz.AddComponent<ZonaMetroidvania2D>();
        Set(zona, "idZona", "canal-campanas");
        Set(zona, "nombreZona", "Canal de las Campanas");
        Set(zona, "siguienteZona", "Galeria de Valvulas");
        Set(zona, "limitesX", new Vector2(0f, 84f));
        Set(zona, "limitesY", new Vector2(-6f, 10f));

        GameObject geometria = CrearGrupo(raiz.transform, "Geometria y rutas");
        GameObject puntos = CrearGrupo(raiz.transform, "Puntos de interes");
        GameObject personajes = CrearGrupo(raiz.transform, "Personajes secundarios");
        GameObject encuentros = CrearGrupo(raiz.transform, "Encuentros");

        CrearFondo(puntos.transform, piedra);
        CrearPlataforma(geometria.transform, "Suelo ruta principal 01", suelo,
            new Vector2(9f, -4f), new Vector2(18f, 1.3f), groundLayer);
        CrearPlataforma(geometria.transform, "Suelo ruta principal 02", suelo,
            new Vector2(30f, -4f), new Vector2(24f, 1.3f), groundLayer);
        CrearPlataforma(geometria.transform, "Suelo ruta principal 03", suelo,
            new Vector2(52f, -4f), new Vector2(20f, 1.3f), groundLayer);
        CrearPlataforma(geometria.transform, "Suelo ruta principal 04", suelo,
            new Vector2(73f, -4f), new Vector2(22f, 1.3f), groundLayer);

        CrearPlataforma(geometria.transform, "Ramal superior - escalon 1", plataforma,
            new Vector2(12f, -2.72f), new Vector2(3.2f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Ramal superior - escalon 2", plataforma,
            new Vector2(16f, -1.78f), new Vector2(3.2f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Ramal de campanas", plataforma,
            new Vector2(21f, -1.56f), new Vector2(8f, 0.45f), groundLayer);
        CrearPlataforma(geometria.transform, "Ramal superior - bajada", plataforma,
            new Vector2(26f, -2.68f), new Vector2(3.2f, 0.4f), groundLayer);

        CrearPlataforma(geometria.transform, "Ramal del filtrado - escalon 1", plataforma,
            new Vector2(36f, -2.72f), new Vector2(3.2f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Ramal del filtrado - escalon 2", plataforma,
            new Vector2(40f, -1.78f), new Vector2(3.2f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Pasarela del filtrado", plataforma,
            new Vector2(46f, -1.56f), new Vector2(8f, 0.45f), groundLayer);
        CrearPlataforma(geometria.transform, "Ramal del filtrado - bajada", plataforma,
            new Vector2(51f, -2.68f), new Vector2(3.2f, 0.4f), groundLayer);

        CrearLimite(geometria.transform, "Limite izquierdo", new Vector2(0f, 0f),
            new Vector2(0.7f, 15f), groundLayer);
        CrearLimite(geometria.transform, "Limite derecho", new Vector2(84f, 0f),
            new Vector2(0.7f, 15f), groundLayer);
        CrearLimite(geometria.transform, "Fondo seguro", new Vector2(42f, -8f),
            new Vector2(84f, 0.5f), groundLayer);

        Transform entrada = CrearMarcador(geometria.transform, "Entrada desde el Refugio",
            new Vector2(2.5f, -2.6f));
        PuntoEntradaZona2D componenteEntrada = entrada.gameObject.AddComponent<PuntoEntradaZona2D>();
        Set(componenteEntrada, "idEntrada", "entrada-principal");
        Transform salida = CrearMarcador(geometria.transform, "Conexion a Galeria de Valvulas",
            new Vector2(81.5f, -2.68f));
        Transform entradaRetorno = CrearMarcador(geometria.transform, "Entrada desde Galeria de Valvulas",
            new Vector2(79.5f, -2.68f));
        PuntoEntradaZona2D compEntradaRetorno = entradaRetorno.gameObject.AddComponent<PuntoEntradaZona2D>();
        Set(compEntradaRetorno, "idEntrada", "retorno-desde-galeria");
        Set(zona, "puntoEntrada", entrada);
        Set(zona, "conexionSiguiente", salida);

        CrearSenal(puntos.transform, "Senal - entrada del Refugio", campana,
            new Vector2(2.2f, -1.1f), "REFUGIO  ←   CANAL", new Color(0.85f, 0.96f, 0.87f));
        CrearSenal(puntos.transform, "Senal - ramal de campanas", campana,
            new Vector2(18f, 0.3f), "DESVIO  ↑  CAMPANAS", new Color(1f, 0.83f, 0.52f));
        CrearSenal(puntos.transform, "Senal - ramal de filtrado", campana,
            new Vector2(43f, 0.3f), "DESVIO  ↑  FILTRADO", new Color(0.71f, 0.91f, 0.8f));
        CrearSenal(puntos.transform, "Senal - guardian", cristal,
            new Vector2(63f, -1.2f), "ESTACION DE RESONANCIA", new Color(0.61f, 0.95f, 0.91f));

        CrearMoneda(puntos.transform, campana, new Vector2(21f, -0.75f), 3);
        CrearMoneda(puntos.transform, campana, new Vector2(46f, -0.75f), 2);
        CrearPuntoDeInteres(puntos.transform, "Registro de ceramica",
            new Vector2(25.5f, -1.6f), "Las campanas registran el nivel del agua.");
        CrearPuntoDeInteres(puntos.transform, "Marca de cuadrilla",
            new Vector2(49.5f, -1.6f), "La compuerta se detuvo siguiendo un protocolo.");

        GameObject jugador = CrearJugador(groundLayer, enemyLayer);
        jugador.transform.position = entrada.position;
        jugador.AddComponent<VidaJugador2D>();
        InventarioJugador2D inventario = jugador.AddComponent<InventarioJugador2D>();
        Set(inventario, "idGuardado", "ruinas-del-eco-jugador");
        Set(inventario, "monedas", 0);

        CrearPersonajeSecundario(personajes.transform, "Teren (arte provisional)",
            CargarSprite(RutaJugador), new Vector2(4.5f, -2.72f),
            PersonajeHistoria2D.Teren);

        CrearEnemigo(encuentros.transform, "Recolector de barro A",
            CargarSprite(RutaEnemigoUno), CargarControlador(RutaControladorEnemigoUno),
            new Vector2(10f, -2.72f), enemyLayer, 2, 1.5f, 5f, 1.1f, 2);
        CrearEnemigo(encuentros.transform, "Recolector de barro B",
            CargarSprite(RutaEnemigoDos), CargarControlador(RutaControladorEnemigoDos),
            new Vector2(31f, -2.72f), enemyLayer, 2, 1.8f, 5.5f, 1.25f, 2);
        CrearEnemigo(encuentros.transform, "Campana defensiva del ramal",
            CargarSprite(RutaEnemigoUno), CargarControlador(RutaControladorEnemigoUno),
            new Vector2(20f, -0.65f), enemyLayer, 1, 1.2f, 4.5f, 1.4f, 2);
        CrearEnemigo(encuentros.transform, "Recolector del filtrado",
            CargarSprite(RutaEnemigoDos), CargarControlador(RutaControladorEnemigoDos),
            new Vector2(46f, -0.65f), enemyLayer, 2, 1.4f, 5f, 1.2f, 2);
        JefeGuardianEstacion2D guardian = CrearJefeGuardian(encuentros.transform,
            "Guardian de la estacion", CargarSprite(RutaEnemigoUno),
            new Vector2(63f, -2.72f), enemyLayer);

        Transform puntoReaparicion = CrearMarcador(geometria.transform,
            "Punto de reaparicion del Guardian", new Vector2(55f, -2.6f));
        GameObject puntoControl = CrearDecoracion(puntos.transform, "Estacion de retorno",
            campana, new Vector2(55f, -2.4f), new Vector2(0.8f, 1.1f), 3);
        BoxCollider2D triggerControl = puntoControl.AddComponent<BoxCollider2D>();
        triggerControl.size = new Vector2(1.7f, 2.5f);
        triggerControl.isTrigger = true;
        SenalZona2D senalCheckpoint = puntoControl.AddComponent<SenalZona2D>();
        Set(senalCheckpoint, "texto", "ESTACION DE RETORNO");
        Set(senalCheckpoint, "color", new Color(0.7f, 0.96f, 0.88f));
        Set(senalCheckpoint, "ancho", 180f);
        PuntoControlZona2D checkpoint = puntoControl.AddComponent<PuntoControlZona2D>();
        Set(checkpoint, "puntoReaparicion", puntoReaparicion);
        Set(checkpoint, "zona", zona);
        Set(checkpoint, "idPuntoControl", "estacion-canal-01");

        GameObject cristalDash = CrearDecoracion(puntos.transform, "Cristal de Dash",
            cristal, new Vector2(66.2f, -2.2f), new Vector2(0.9f, 1.1f), 4);
        BoxCollider2D triggerCristal = cristalDash.AddComponent<BoxCollider2D>();
        triggerCristal.size = new Vector2(1.3f, 1.4f);
        triggerCristal.isTrigger = true;
        triggerCristal.enabled = false;
        RecompensaGuardianDash2D recompensa = cristalDash.AddComponent<RecompensaGuardianDash2D>();
        Set(recompensa, "guardian", guardian);

        GameObject interfazObjeto = new GameObject("Interfaz del Canal");
        InterfazZonaMetroidvania2D interfaz = interfazObjeto.AddComponent<InterfazZonaMetroidvania2D>();
        Set(guardian, "interfaz", interfaz);
        Set(recompensa, "interfaz", interfaz);
        Set(checkpoint, "interfaz", interfaz);

        CrearAtajo(puntos.transform, "Atajo de regreso al inicio", campana,
            new Vector2(72f, -2.5f), entrada, guardian, interfaz);

        GameObject barrera = CrearDecoracion(geometria.transform, "Barrera de resonancia - requiere Dash",
            cristal, new Vector2(78f, -0.6f), new Vector2(0.7f, 5.6f), 3);
        BoxCollider2D bloqueo = barrera.AddComponent<BoxCollider2D>();
        bloqueo.size = new Vector2(1f, 5.6f);
        BoxCollider2D sensor = barrera.AddComponent<BoxCollider2D>();
        sensor.size = new Vector2(1.4f, 6.4f);
        sensor.isTrigger = true;
        PuertaDash2D puerta = barrera.AddComponent<PuertaDash2D>();
        Set(puerta, "colisionadorBloqueo", bloqueo);
        Set(puerta, "sensor", sensor);
        Set(puerta, "visualBloqueo", barrera.GetComponent<SpriteRenderer>());

        CrearConexion(puntos.transform, "Conexion a Galeria de Valvulas", campana,
            salida.position, zona, interfaz);

        Camera camara = CrearCamara(zona, jugador.transform);
        EditorSceneManager.MarkSceneDirty(escena);
        if (!EditorSceneManager.SaveScene(escena, RutaEscena))
            throw new IOException("No se pudo guardar la escena " + RutaEscena + ".");

        AnadirEscenaABuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidarContenidoEscena(escena, camara);
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena);
        Debug.Log("Canal de las Campanas generado en " + RutaEscena + ".");
    }

    [MenuItem("Demo/Validar zona: Canal de las Campanas")]
    public static void Validar()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null)
            throw new FileNotFoundException("Genera primero la escena CanalCampanas.", RutaEscena);

        Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        ValidarContenidoEscena(escena, Camera.main);
        Debug.Log("Canal validado: ruta continua sin Dash, dos ramales opcionales, enemigos, guardian, atajo y salida protegida.");
    }

    private static void ValidarRecursos()
    {
        foreach (string ruta in new[]
        {
            RutaJugador, RutaEnemigoUno, RutaEnemigoDos, RutaControladorJugador,
            RutaControladorEnemigoUno, RutaControladorEnemigoDos, "Assets/InputSystem_Actions.inputactions"
        })
        {
            if (AssetDatabase.LoadMainAssetAtPath(ruta) == null)
                throw new FileNotFoundException("Falta un recurso necesario para la escena.", ruta);
        }
    }

    private static void ValidarContenidoEscena(Scene escena, Camera camara)
    {
        GameObject raiz = GameObject.Find("Zona - Canal de las Campanas");
        GameObject jugadorObjeto = GameObject.Find("Explorador");
        if (raiz == null || jugadorObjeto == null)
            throw new InvalidOperationException("Falta la zona o el jugador en CanalCampanas.");

        ZonaMetroidvania2D zona = raiz.GetComponent<ZonaMetroidvania2D>();
        Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
        if (zona == null || jugador == null || jugador.GetComponent<VidaJugador2D>() == null ||
            jugador.GetComponent<InventarioJugador2D>() == null)
            throw new InvalidOperationException("La zona necesita jugador, vida, inventario y metadatos.");
        if (jugador.DashDesbloqueado)
            throw new InvalidOperationException("El Dash debe permanecer bloqueado al entrar en la zona.");
        PersonajeSecundario2D teren = raiz.GetComponentsInChildren<PersonajeSecundario2D>(true)
            .FirstOrDefault(personaje => personaje.Personaje == PersonajeHistoria2D.Teren);
        if (teren == null || !teren.GetComponent<Collider2D>().isTrigger ||
            DialogosHistoria2D.ObtenerLineas(PersonajeHistoria2D.Teren).Length == 0)
            throw new InvalidOperationException("La zona necesita la interaccion de Teren junto a la entrada.");
        if (zona.PuntoEntrada == null ||
            zona.PuntoEntrada.GetComponent<PuntoEntradaZona2D>() == null ||
            zona.ConexionSiguiente == null ||
            zona.SiguienteZona != "Galeria de Valvulas")
            throw new InvalidOperationException("Configura la entrada y la conexion a la zona siguiente.");

        BoxCollider2D[] suelos = raiz.GetComponentsInChildren<BoxCollider2D>(true)
            .Where(colision => colision.gameObject.name.StartsWith("Suelo ruta principal", StringComparison.Ordinal))
            .ToArray();
        Array.Sort(suelos, (a, b) => a.bounds.min.x.CompareTo(b.bounds.min.x));
        if (suelos.Length < 4)
            throw new InvalidOperationException("La ruta principal debe tener cuatro tramos de suelo.");
        for (int i = 1; i < suelos.Length; i++)
        {
            if (suelos[i].bounds.min.x > suelos[i - 1].bounds.max.x + 0.15f)
                throw new InvalidOperationException("La ruta principal tiene un hueco que puede exigir una habilidad.");
        }

        float alturaMaximaSalto = jugador.fuerzaSalto * jugador.fuerzaSalto /
            (2f * Physics2D.gravity.magnitude * jugadorObjeto.GetComponent<Rigidbody2D>().gravityScale);
        foreach (string ramal in new[] { "Ramal superior", "Ramal del filtrado" })
        {
            BoxCollider2D[] plataformas = raiz.GetComponentsInChildren<BoxCollider2D>(true)
                .Where(colision => colision.gameObject.name.Contains(ramal))
                .ToArray();
            if (plataformas.Length < 3)
                throw new InvalidOperationException("El ramal " + ramal + " no tiene una ruta de plataformas.");

            foreach (BoxCollider2D plataforma in plataformas)
            {
                if (plataforma.bounds.max.y - (-3.35f) > alturaMaximaSalto + 0.25f)
                    throw new InvalidOperationException("El ramal " + ramal + " supera el salto normal.");
            }
        }

        AtajoCanal2D atajo = raiz.GetComponentInChildren<AtajoCanal2D>(true);
        RecompensaGuardianDash2D recompensa = raiz.GetComponentInChildren<RecompensaGuardianDash2D>(true);
        PuntoControlZona2D checkpoint = raiz.GetComponentInChildren<PuntoControlZona2D>(true);
        PuertaDash2D puerta = raiz.GetComponentInChildren<PuertaDash2D>(true);
        if (atajo == null || recompensa == null || checkpoint == null || puerta == null ||
            raiz.GetComponentInChildren<ConexionZona2D>(true) == null)
            throw new InvalidOperationException("La escena debe incluir atajo, recompensa, barrera Dash y conexion.");
        if (atajo.EstaDisponible || recompensa.GetComponent<Collider2D>().enabled ||
            !checkpoint.GetComponent<Collider2D>().isTrigger ||
            puerta.PuedeAtravesar(jugador))
            throw new InvalidOperationException("El atajo, el cristal o la barrera no respetan la progresion inicial.");
        JefeGuardianEstacion2D[] guardianes = raiz.GetComponentsInChildren<JefeGuardianEstacion2D>(true);
        if (raiz.GetComponentsInChildren<EnemigoIA2D>(true).Length < 4 || guardianes.Length != 1)
            throw new InvalidOperationException("La zona debe contener cuatro enemigos y el Guardian de la Estacion.");
        JefeGuardianEstacion2D guardian = guardianes[0];
        if (guardian.Interfaz == null || recompensa.Guardian != guardian || atajo.Guardian != guardian)
            throw new InvalidOperationException("El jefe, el cristal y el atajo deben compartir sus referencias.");
        if (raiz.GetComponentsInChildren<SenalZona2D>(true).Length < 4)
            throw new InvalidOperationException("La zona necesita señales para entrada, ramales y estacion.");
        if (camara == null || camara.GetComponent<CamaraZona2D>() == null)
            throw new InvalidOperationException("La camara debe usar los limites de esta zona.");

        EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
        if (!escenas.Any(escenaConfigurada => escenaConfigurada.enabled &&
                                              escenaConfigurada.path == RutaEscena))
            throw new InvalidOperationException("CanalCampanas debe estar habilitada en Build Settings.");
    }

    private static GameObject CrearJugador(int groundLayer, int enemyLayer)
    {
        GameObject jugador = new GameObject("Explorador");
        SpriteRenderer renderer = jugador.AddComponent<SpriteRenderer>();
        renderer.sprite = CargarSprite(RutaJugador);
        renderer.sortingOrder = 2;
        Animator animator = jugador.AddComponent<Animator>();
        animator.runtimeAnimatorController = CargarControlador(RutaControladorJugador);

        Rigidbody2D rb = jugador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D colisionador = jugador.AddComponent<CapsuleCollider2D>();
        colisionador.size = new Vector2(0.72f, 1.45f);
        Transform suelo = new GameObject("Comprobador de suelo").transform;
        suelo.SetParent(jugador.transform, false);
        suelo.localPosition = new Vector3(0f, -0.76f, 0f);

        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        Jugador2D controlador = jugador.AddComponent<Jugador2D>();
        controlador.accionesEntrada = acciones;
        controlador.comprobadorSuelo = suelo;
        controlador.capaSuelo = 1 << groundLayer;
        controlador.capaEnemigos = 1 << enemyLayer;
        Set(controlador, "dashDesbloqueado", false);

        GameObject espada = CrearDecoracion(jugador.transform, "Destello de espada",
            CrearSprite("CanalTajo", new Color32(245, 214, 138, 255), new Color32(255, 247, 215, 255)),
            new Vector2(0.72f, 0.05f), new Vector2(0.9f, 0.35f), 4);
        espada.SetActive(false);
        controlador.espadaVisual = espada;
        return jugador;
    }

    private static void CrearPersonajeSecundario(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        PersonajeHistoria2D personaje)
    {
        GameObject actor = new GameObject(nombre);
        actor.transform.SetParent(padre, false);
        actor.transform.position = posicion;
        SpriteRenderer renderer = actor.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.65f, 0.88f, 0.79f, 1f);
        renderer.sortingOrder = 3;

        BoxCollider2D zonaInteraccion = actor.AddComponent<BoxCollider2D>();
        zonaInteraccion.size = new Vector2(1.7f, 1.9f);
        zonaInteraccion.isTrigger = true;

        PersonajeSecundario2D dialogo = actor.AddComponent<PersonajeSecundario2D>();
        Set(dialogo, "personaje", personaje);
        Set(dialogo, "nombreVisible", DialogosHistoria2D.ObtenerNombre(personaje));

        GameObject etiquetaObjeto = new GameObject("Nombre provisional");
        etiquetaObjeto.transform.SetParent(actor.transform, false);
        etiquetaObjeto.transform.localPosition = new Vector3(0f, 1.15f, 0f);
        TextMesh etiqueta = etiquetaObjeto.AddComponent<TextMesh>();
        etiqueta.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        etiqueta.text = DialogosHistoria2D.ObtenerNombre(personaje).ToUpperInvariant();
        etiqueta.anchor = TextAnchor.MiddleCenter;
        etiqueta.alignment = TextAlignment.Center;
        etiqueta.fontSize = 32;
        etiqueta.characterSize = 0.07f;
        etiqueta.color = new Color(0.88f, 0.96f, 0.83f);
        MeshRenderer etiquetaRenderer = etiquetaObjeto.GetComponent<MeshRenderer>();
        etiquetaRenderer.sortingOrder = 10;
    }

    private static EnemigoIA2D CrearEnemigo(
        Transform padre,
        string nombre,
        Sprite sprite,
        RuntimeAnimatorController controlador,
        Vector2 posicion,
        int enemyLayer,
        int vida,
        float velocidad,
        float rangoDeteccion,
        float rangoAtaque,
        float distanciaPatrulla)
    {
        GameObject enemigo = new GameObject(nombre);
        enemigo.transform.SetParent(padre, false);
        enemigo.transform.position = posicion;
        enemigo.layer = enemyLayer;
        SpriteRenderer renderer = enemigo.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        Animator animator = enemigo.AddComponent<Animator>();
        animator.runtimeAnimatorController = controlador;
        Rigidbody2D rb = enemigo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D colisionador = enemigo.AddComponent<CapsuleCollider2D>();
        colisionador.size = new Vector2(0.8f, 1.3f);

        EnemigoIA2D ia = enemigo.AddComponent<EnemigoIA2D>();
        Set(ia, "velocidad", velocidad);
        Set(ia, "distanciaPatrulla", distanciaPatrulla);
        Set(ia, "rangoDeteccion", rangoDeteccion);
        Set(ia, "rangoAtaque", rangoAtaque);
        Set(ia, "vidaMaxima", vida);
        Set(ia, "tipoAtaque", EnemigoIA2D.TipoAtaque.CuerpoACuerpo);
        Set(ia, "sprite", renderer);
        Set(ia, "animator", animator);
        return ia;
    }

    private static JefeGuardianEstacion2D CrearJefeGuardian(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        int enemyLayer)
    {
        GameObject enemigo = new GameObject(nombre);
        enemigo.transform.SetParent(padre, false);
        enemigo.transform.position = posicion;
        enemigo.layer = enemyLayer;

        SpriteRenderer renderer = enemigo.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        Rigidbody2D rb = enemigo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        CapsuleCollider2D colisionador = enemigo.AddComponent<CapsuleCollider2D>();
        colisionador.size = new Vector2(0.8f, 1.3f);

        JefeGuardianEstacion2D jefe = enemigo.AddComponent<JefeGuardianEstacion2D>();
        Set(jefe, "vidaMaxima", 12);
        Set(jefe, "velocidad", 1.15f);
        Set(jefe, "rangoDeteccion", 11f);
        Set(jefe, "rangoAtaque", 3f);
        Set(jefe, "dano", 1);
        Set(jefe, "anticipacionAtaque", 0.8f);
        Set(jefe, "duracionAtaque", 0.22f);
        Set(jefe, "recuperacionAtaque", 1.1f);
        Set(jefe, "tiempoEntreAtaques", 2f);
        Set(jefe, "tiempoAturdido", 0.25f);
        Set(jefe, "tiempoInvulnerableTrasGolpe", 0.16f);
        Set(jefe, "fuerzaRetroceso", 2.5f);
        Set(jefe, "alcanceGolpe", 2.2f);
        Set(jefe, "toleranciaAlturaGolpe", 0.75f);
        Set(jefe, "alcanceOnda", 7f);
        return jefe;
    }

    private static void CrearAtajo(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Transform destino,
        EnemigoPrototipo2D guardian,
        InterfazZonaMetroidvania2D interfaz)
    {
        GameObject atajo = CrearDecoracion(padre, nombre, sprite, posicion,
            new Vector2(1.1f, 1.4f), 2);
        BoxCollider2D zonaInteraccion = atajo.AddComponent<BoxCollider2D>();
        zonaInteraccion.size = new Vector2(2.2f, 2.5f);
        zonaInteraccion.isTrigger = true;
        AtajoCanal2D componente = atajo.AddComponent<AtajoCanal2D>();
        Set(componente, "guardian", guardian);
        Set(componente, "destino", destino);
        Set(componente, "interfaz", interfaz);
        SenalZona2D senal = atajo.AddComponent<SenalZona2D>();
        Set(senal, "texto", "E: atajo de regreso");
        Set(senal, "color", new Color(0.7f, 0.96f, 0.88f));
        Set(senal, "ancho", 180f);
    }

    private static void CrearConexion(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        ZonaMetroidvania2D zona,
        InterfazZonaMetroidvania2D interfaz)
    {
        GameObject conexion = CrearDecoracion(padre, nombre, sprite, posicion,
            new Vector2(1.2f, 1.6f), 2);
        BoxCollider2D trigger = conexion.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2.5f, 2.6f);
        trigger.isTrigger = true;
        ConexionZona2D componente = conexion.AddComponent<ConexionZona2D>();
        Set(componente, "zona", zona);
        Set(componente, "interfaz", interfaz);
        Set(componente, "rutaEscenaDestino", "Assets/Scenes/GaleriaValvulas.unity");
        Set(componente, "idEntradaDestino", "entrada-desde-canal");
        SenalZona2D senal = conexion.AddComponent<SenalZona2D>();
        Set(senal, "texto", "GALERIA DE VALVULAS  →");
        Set(senal, "color", new Color(0.78f, 0.94f, 0.86f));
        Set(senal, "ancho", 220f);
    }

    private static void CrearMoneda(Transform padre, Sprite sprite, Vector2 posicion, int cantidad)
    {
        GameObject moneda = CrearDecoracion(padre, "Eco recolectable", sprite, posicion,
            new Vector2(0.45f, 0.45f), 3);
        CircleCollider2D trigger = moneda.AddComponent<CircleCollider2D>();
        trigger.radius = 0.6f;
        trigger.isTrigger = true;
        ObjetoRecolectable2D objeto = moneda.AddComponent<ObjetoRecolectable2D>();
        Set(objeto, "tipo", ObjetoRecolectable2D.TipoObjeto.Moneda);
        Set(objeto, "id", "eco");
        Set(objeto, "cantidad", cantidad);
    }

    private static void CrearPuntoDeInteres(Transform padre, string nombre, Vector2 posicion, string texto)
    {
        Sprite piedra = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaArte + "/CanalPiedra.png");
        CrearSenal(padre, nombre, piedra, posicion, texto, new Color(0.88f, 0.93f, 0.78f));
    }

    private static void CrearSenal(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        string texto,
        Color color)
    {
        GameObject senal = CrearDecoracion(padre, nombre, sprite, posicion, new Vector2(0.55f, 0.75f), 2);
        SenalZona2D etiqueta = senal.AddComponent<SenalZona2D>();
        Set(etiqueta, "texto", texto);
        Set(etiqueta, "color", color);
        Set(etiqueta, "ancho", Mathf.Clamp(texto.Length * 10f, 160f, 260f));
    }

    private static GameObject CrearFondo(Transform padre, Sprite sprite)
    {
        GameObject fondo = CrearDecoracion(padre, "Fondo - canales de ceramica", sprite,
            new Vector2(42f, 3.2f), Vector2.one, -5);
        SpriteRenderer renderer = fondo.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(90f, 15f);
        renderer.color = new Color(0.27f, 0.41f, 0.4f, 0.45f);
        return fondo;
    }

    private static void CrearPlataforma(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 tamano,
        int layer)
    {
        GameObject plataforma = CrearDecoracion(padre, nombre, sprite, posicion,
            Vector2.one, 0);
        plataforma.layer = layer;
        SpriteRenderer renderer = plataforma.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = tamano;
        BoxCollider2D colisionador = plataforma.AddComponent<BoxCollider2D>();
        colisionador.size = tamano;
    }

    private static void CrearLimite(Transform padre, string nombre, Vector2 posicion, Vector2 tamano, int layer)
    {
        GameObject limite = new GameObject(nombre);
        limite.transform.SetParent(padre, false);
        limite.transform.position = posicion;
        limite.transform.localScale = new Vector3(tamano.x, tamano.y, 1f);
        limite.layer = layer;
        limite.AddComponent<BoxCollider2D>();
    }

    private static GameObject CrearDecoracion(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 escala,
        int orden)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(padre, false);
        objeto.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        objeto.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = orden;
        return objeto;
    }

    private static Camera CrearCamara(ZonaMetroidvania2D zona, Transform objetivo)
    {
        GameObject objeto = new GameObject("Camara del Canal");
        objeto.tag = "MainCamera";
        objeto.transform.position = new Vector3(10f, 0f, -10f);
        Camera camara = objeto.AddComponent<Camera>();
        camara.orthographic = true;
        camara.orthographicSize = 5.4f;
        camara.backgroundColor = new Color(0.055f, 0.09f, 0.105f);
        camara.clearFlags = CameraClearFlags.SolidColor;
        objeto.AddComponent<AudioListener>();
        objeto.AddComponent<CamaraZona2D>().Configurar(zona, objetivo);
        return camara;
    }

    private static Transform CrearMarcador(Transform padre, string nombre, Vector2 posicion)
    {
        GameObject marcador = new GameObject(nombre);
        marcador.transform.SetParent(padre, false);
        marcador.transform.position = new Vector3(posicion.x, posicion.y, 0f);
        return marcador.transform;
    }

    private static GameObject CrearGrupo(Transform padre, string nombre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        return grupo;
    }

    private static Sprite CargarSprite(string ruta)
    {
        Sprite sprite = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().FirstOrDefault();
        if (sprite == null)
            throw new InvalidOperationException("No se encontro el sprite en " + ruta + ".");
        return sprite;
    }

    private static RuntimeAnimatorController CargarControlador(string ruta)
    {
        RuntimeAnimatorController controlador =
            AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ruta);
        if (controlador == null)
            throw new InvalidOperationException("No se encontro el controlador " + ruta + ".");
        return controlador;
    }

    private static Sprite CrearSprite(string nombre, Color fondo, Color marca)
    {
        string ruta = CarpetaArte + "/" + nombre + ".png";
        Texture2D textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;
        Color transparente = new Color(0f, 0f, 0f, 0f);

        for (int y = 0; y < textura.height; y++)
        {
            for (int x = 0; x < textura.width; x++)
            {
                Color pixel = fondo;
                if (nombre.Contains("Campana"))
                {
                    bool cuerpo = y >= 9 && y <= 24 && x >= 7 && x <= 24 &&
                                  (y > 12 || x >= 10 && x <= 21);
                    bool borde = cuerpo && (x == 7 || x == 24 || y == 24 || y == 9);
                    pixel = cuerpo ? (borde ? marca : fondo) : transparente;
                    if (y >= 5 && y <= 8 && x >= 12 && x <= 19)
                        pixel = marca;
                }
                else if (nombre.Contains("Cristal"))
                {
                    int distancia = Mathf.Abs(x - 16) + Mathf.Abs(y - 16);
                    pixel = distancia <= 15 ? (distancia > 11 ? marca : fondo) : transparente;
                }
                else if (nombre.Contains("Tajo"))
                {
                    pixel = Mathf.Abs(y - (31 - x)) < 3 && x > 2 && x < 30 ? marca : transparente;
                }
                else
                {
                    bool borde = y > 24 || x % 8 == 0 || y % 8 == 0;
                    pixel = borde ? marca : fondo;
                    if (nombre.Contains("Piedra") && (x * 3 + y * 5) % 29 == 0)
                        pixel = new Color(0.2f, 0.34f, 0.33f, 1f);
                }

                textura.SetPixel(x, y, pixel);
            }
        }

        textura.Apply();
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);
        TextureImporter importador = AssetImporter.GetAtPath(ruta) as TextureImporter;
        if (importador == null)
            throw new InvalidOperationException("No se pudo configurar el sprite " + ruta + ".");

        importador.textureType = TextureImporterType.Sprite;
        importador.spriteImportMode = SpriteImportMode.Single;
        importador.spritePixelsPerUnit = 32f;
        importador.filterMode = FilterMode.Point;
        importador.textureCompression = TextureImporterCompression.Uncompressed;
        importador.alphaIsTransparency = true;
        TextureImporterSettings opciones = new TextureImporterSettings();
        importador.ReadTextureSettings(opciones);
        opciones.spriteMeshType = SpriteMeshType.FullRect;
        importador.SetTextureSettings(opciones);
        importador.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void AnadirEscenaABuildSettings()
    {
        List<EditorBuildSettingsScene> escenas = EditorBuildSettings.scenes.ToList();
        int indice = escenas.FindIndex(escena => escena.path == RutaEscena);
        if (indice >= 0)
            escenas[indice] = new EditorBuildSettingsScene(RutaEscena, true);
        else
        {
            int indiceDemo = escenas.FindIndex(escena => escena.path == "Assets/Scenes/RuinsDemo.unity");
            escenas.Insert(indiceDemo >= 0 ? indiceDemo + 1 : 0,
                new EditorBuildSettingsScene(RutaEscena, true));
        }

        EditorBuildSettings.scenes = escenas.ToArray();
    }

    private static void Set(UnityEngine.Object objetivo, string nombre, object valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        switch (valor)
        {
            case string texto:
                propiedad.stringValue = texto;
                break;
            case bool booleano:
                propiedad.boolValue = booleano;
                break;
            case int entero:
                propiedad.intValue = entero;
                break;
            case float real:
                propiedad.floatValue = real;
                break;
            case Vector2 vector:
                propiedad.vector2Value = vector;
                break;
            case Color color:
                propiedad.colorValue = color;
                break;
            case UnityEngine.Object referencia:
                propiedad.objectReferenceValue = referencia;
                break;
            case Enum enumeracion:
                propiedad.enumValueIndex = Convert.ToInt32(enumeracion);
                break;
            default:
                throw new ArgumentException("Tipo de campo no soportado: " + valor.GetType().Name);
        }

        serializado.ApplyModifiedPropertiesWithoutUndo();
    }
}
