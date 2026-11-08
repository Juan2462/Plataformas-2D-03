using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class CrearDemo2D
{
    private const string DemoFolder = "Assets/Demo";
    private const string ScenePath = "Assets/Scenes/RuinsDemo.unity";
    private const string InventoryScenePath = "Assets/Scenes/InventarioDemo.unity";
    private const string ShopScenePath = "Assets/Scenes/TiendaDemo.unity";
    private const string GroundLayerName = "Ground";
    private const string EnemyLayerName = "Enemy";

    [MenuItem("Demo/Generar demo de las Ruinas del Eco")]
    public static void Generar()
    {
        CrearCarpetas();
        int groundLayer = CrearOCargarCapa(GroundLayerName);
        int enemyLayer = CrearOCargarCapa(EnemyLayerName);
        CrearOCargarTag("Ladder");
        CrearOCargarTag("EnemyAttack");

        Sprite suelo = CrearSprite("DemoSuelo", TipoSprite.Suelo);
        Sprite piedra = CrearSprite("DemoPiedra", TipoSprite.Piedra);
        Sprite cristal = CrearSprite("DemoCristal", TipoSprite.Cristal);
        Sprite llave = CrearSprite("DemoLlave", TipoSprite.Llave);
        Sprite moneda = CrearSprite("DemoEco", TipoSprite.Moneda);
        Sprite portal = CrearSprite("DemoPortal", TipoSprite.Portal);
        Sprite proyectil = CrearSprite("DemoProyectil", TipoSprite.Proyectil);
        Sprite jugadorSprite = PrimerSprite("Assets/Sprites/Player/Movement/PlayerMovement.png");
        Sprite enemigoUnoSprite = PrimerSprite("Assets/Sprites/Enemies/Enemy 1..png");
        Sprite enemigoDosSprite = PrimerSprite("Assets/Sprites/Enemies/Enemy2.png");

        if (jugadorSprite == null || enemigoUnoSprite == null || enemigoDosSprite == null)
            throw new InvalidOperationException("No se pudieron cargar los sprites del jugador o de los enemigos.");

        AssetDatabase.SaveAssets();
        RuntimeAnimatorController animatorJugador = CrearAnimadorJugador();
        RuntimeAnimatorController animatorEnemigoUno = CrearAnimadorEnemigo(
            "Assets/Animation/Animations/Enemies/Enemy1/Enemy1_Idle.anim",
            "Assets/Animation/Animations/Enemies/Enemy1/Enemy1_Walk.anim",
            "Assets/Animation/Animations/Enemies/Enemy1/Enemy1_MeleeAttack.anim",
            "Assets/Animation/Animations/Enemies/Enemy1/Enemy1_Hurt.anim",
            "DemoEnemigoUno.controller"
        );
        RuntimeAnimatorController animatorEnemigoDos = CrearAnimadorEnemigo(
            "Assets/Animation/Animations/Enemies/Enemy2/Enemy2_Idle.anim",
            "Assets/Animation/Animations/Enemies/Enemy2/Enemy2_Move.anim",
            "Assets/Animation/Animations/Enemies/Enemy2/Enemy2_MeleeAttack.anim",
            "Assets/Animation/Animations/Enemies/Enemy2/Enemy2_Stun.anim",
            "DemoEnemigoDos.controller"
        );
        GameObject prefabProyectil = CrearPrefabProyectil(proyectil);

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearEscena(suelo, piedra, cristal, llave, moneda, portal, jugadorSprite,
            enemigoUnoSprite, enemigoDosSprite, animatorJugador, animatorEnemigoUno,
            animatorEnemigoDos, prefabProyectil, groundLayer, enemyLayer);

        EditorBuildSettingsScene[] escenasActuales = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path != ScenePath && escena.path != "Assets/Scenes/SampleScene.unity"));
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path == "Assets/Scenes/SampleScene.unity"));
        EditorBuildSettings.scenes = escenas.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidarContenidoEscena();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("Demo creada en " + ScenePath + ". Está configurada como primera escena de la compilación.");
    }

    [MenuItem("Demo/Validar demo de las Ruinas del Eco")]
    public static void Validar()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new FileNotFoundException("Genera primero la escena de demo.", ScenePath);

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        ValidarContenidoEscena();
        Debug.Log("Validacion correcta: jugador, dash desbloqueable, barrera de dash, vida, enemigos, recogibles, puerta, punto de control y salida.");
    }

    [MenuItem("Demo/Generar escenas de inventario y tienda")]
    public static void GenerarEscenasInventarioYTienda()
    {
        CrearCarpetas();
        int groundLayer = CrearOCargarCapa(GroundLayerName);
        int enemyLayer = CrearOCargarCapa(EnemyLayerName);

        Sprite suelo = CrearSprite("DemoSuelo", TipoSprite.Suelo);
        Sprite cristal = CrearSprite("DemoCristal", TipoSprite.Cristal);
        Sprite jugadorSprite = PrimerSprite("Assets/Sprites/Player/Movement/PlayerMovement.png");
        if (jugadorSprite == null)
            throw new InvalidOperationException("No se pudo cargar el spritesheet del jugador.");

        AssetDatabase.SaveAssets();
        RuntimeAnimatorController controlador = CrearAnimadorJugador();
        CrearEscenaInventarioYTienda(InventoryScenePath, "Demo de inventario e intercambio",
            false, suelo, cristal, jugadorSprite, controlador, groundLayer, enemyLayer);
        CrearEscenaInventarioYTienda(ShopScenePath, "Demo de tienda",
            true, suelo, cristal, jugadorSprite, controlador, groundLayer, enemyLayer);

        EditorBuildSettingsScene[] escenasActuales = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(InventoryScenePath, true),
            new EditorBuildSettingsScene(ShopScenePath, true)
        };
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path != ScenePath &&
            escena.path != InventoryScenePath &&
            escena.path != ShopScenePath &&
            escena.path != "Assets/Scenes/SampleScene.unity"));
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path == "Assets/Scenes/SampleScene.unity"));
        EditorBuildSettings.scenes = escenas.ToArray();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(InventoryScenePath);
        Debug.Log("Escenas de inventario e intercambio y de tienda generadas y añadidas a Build Settings.");
    }

    [MenuItem("Demo/Validar escenas de inventario y tienda")]
    public static void ValidarEscenasInventarioYTienda()
    {
        ValidarArchivoEscena(InventoryScenePath, false);
        ValidarArchivoEscena(ShopScenePath, true);
        Debug.Log("Validacion correcta: intercambio, tienda y galeria de 35 animaciones.");
    }

    private static void ValidarArchivoEscena(string rutaEscena, bool esTienda)
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(rutaEscena) == null)
            throw new FileNotFoundException("Genera primero las escenas de inventario y tienda.", rutaEscena);

        EditorSceneManager.OpenScene(rutaEscena, OpenSceneMode.Single);
        GameObject jugador = GameObject.Find("Explorador");
        if (jugador == null)
            throw new InvalidOperationException("La escena no contiene al jugador: " + rutaEscena);

        ValidarEscenaInventarioYTienda(
            esTienda,
            jugador.GetComponent<Animator>()?.runtimeAnimatorController
        );
    }

    private static void CrearEscenaInventarioYTienda(
        string rutaEscena,
        string titulo,
        bool esTienda,
        Sprite suelo,
        Sprite cristal,
        Sprite jugadorSprite,
        RuntimeAnimatorController controlador,
        int groundLayer,
        int enemyLayer)
    {
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene escena = SceneManager.GetActiveScene();
        escena.name = titulo;

        Camera camara = new GameObject("Camara de demostracion").AddComponent<Camera>();
        camara.tag = "MainCamera";
        camara.orthographic = true;
        camara.orthographicSize = 5.2f;
        camara.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.transform.position = new Vector3(0f, 0f, -10f);
        camara.gameObject.AddComponent<AudioListener>();

        CrearFondo(cristal);
        CrearPlataforma("Suelo de la demostracion", suelo,
            new Vector2(0f, -3.25f), new Vector2(26f, 2f), groundLayer);
        CrearPlataforma("Plataforma de practica izquierda", suelo,
            new Vector2(-4f, -0.75f), new Vector2(3.2f, 0.4f), groundLayer);
        CrearPlataforma("Plataforma de practica derecha", suelo,
            new Vector2(4.5f, 0.35f), new Vector2(3.2f, 0.4f), groundLayer);

        GameObject jugador = CrearJugador(jugadorSprite, controlador, groundLayer, enemyLayer);
        jugador.transform.position = new Vector3(-6f, -1.48f, 0f);
        jugador.AddComponent<VidaJugador2D>();
        jugador.AddComponent<ReceptorEventosGaleria2D>();
        InventarioJugador2D inventario = jugador.AddComponent<InventarioJugador2D>();
        SetString(inventario, "idGuardado", "demo-inventario-tienda-jugador");

        InventarioJugador2D almacen = null;
        if (!esTienda)
        {
            GameObject almacenObjeto = new GameObject("Almacen de intercambio");
            almacen = almacenObjeto.AddComponent<InventarioJugador2D>();
            SetString(almacen, "idGuardado", "demo-almacen-intercambio");
            CrearDecoracion("Cristal del almacen", cristal, new Vector3(4.8f, -1.3f, 0f),
                new Vector2(1.2f, 1.8f), new Color(0.45f, 0.8f, 0.9f), 1);
        }
        else
        {
            CrearDecoracion("Emblema del mercado", cristal, new Vector3(4.8f, -1.3f, 0f),
                new Vector2(1.5f, 1.5f), new Color(0.9f, 0.7f, 0.35f), 1);
        }

        DemoInterfazInventario2D interfaz =
            new GameObject("Interfaz " + titulo).AddComponent<DemoInterfazInventario2D>();
        SetBoolean(interfaz, "esTienda", esTienda);
        SetReference(interfaz, "inventarioJugador", inventario);
        SetReference(interfaz, "inventarioAlmacen", almacen);
        SetReference(interfaz, "animadorJugador", jugador.GetComponent<Animator>());

        ValidarEscenaInventarioYTienda(esTienda, controlador);
        if (!EditorSceneManager.SaveScene(escena, rutaEscena))
            throw new IOException("No se pudo guardar la escena de demostracion: " + rutaEscena);
    }

    private static void ValidarEscenaInventarioYTienda(
        bool esTienda,
        RuntimeAnimatorController controlador)
    {
        GameObject jugador = GameObject.Find("Explorador");
        DemoInterfazInventario2D interfaz = UnityEngine.Object.FindFirstObjectByType<DemoInterfazInventario2D>();
        if (jugador == null ||
            jugador.GetComponent<Jugador2D>() == null ||
            jugador.GetComponent<VidaJugador2D>() == null ||
            jugador.GetComponent<InventarioJugador2D>() == null ||
            jugador.GetComponent<Animator>()?.runtimeAnimatorController != controlador ||
            interfaz == null)
            throw new InvalidOperationException("La escena debe incluir al jugador animado, su inventario y la interfaz.");

        if (!esTienda && GameObject.Find("Almacen de intercambio")?.GetComponent<InventarioJugador2D>() == null)
            throw new InvalidOperationException("La escena de inventario debe incluir un almacen intercambiable.");

        SerializedObject interfazSerializada = new SerializedObject(interfaz);
        SerializedProperty referenciaJugador = interfazSerializada.FindProperty("inventarioJugador");
        SerializedProperty referenciaAlmacen = interfazSerializada.FindProperty("inventarioAlmacen");
        SerializedProperty modoTienda = interfazSerializada.FindProperty("esTienda");
        if (referenciaJugador == null ||
            referenciaJugador.objectReferenceValue != jugador.GetComponent<InventarioJugador2D>() ||
            modoTienda == null ||
            modoTienda.boolValue != esTienda ||
            (!esTienda && (referenciaAlmacen == null || referenciaAlmacen.objectReferenceValue == null)))
            throw new InvalidOperationException("La interfaz de inventario o tienda no tiene sus referencias configuradas.");

        if (jugador.GetComponent<ReceptorEventosGaleria2D>() == null)
            throw new InvalidOperationException("El jugador necesita receptores para los eventos de sus animaciones.");

        AnimatorController animatorController = controlador as AnimatorController;
        if (animatorController == null)
            throw new InvalidOperationException("El controlador de animacion del jugador no es valido.");

        string[] animaciones =
        {
            "CrouchIdle", "CrouchMove", "Fall", "Idle", "JumpUp1", "JumpUp1X", "JumpUp2",
            "JumpUp2X", "JumpUp3", "JumpUp3X", "Land", "LedgeClimb", "LedgeClimbCrouch",
            "LedgeGrab", "LedgeHold", "Move", "Stun", "WallClimb", "WallGrab", "WallSlide",
            "bow_action", "bow_anticipation", "bow_idle", "Shield_Anticipation", "Shield_Break",
            "Shield_Cancel", "Shield_Idle", "Shield_Parry", "Sword_Attack_1", "Sword_Attack_2",
            "Sword_Attack_3", "Tome_Action", "Tome_Anticipation", "Tome_Cancel", "Tome_Idle"
        };
        foreach (string animacion in animaciones)
        {
            bool encontrado = animatorController.layers[0].stateMachine.states
                .Any(estado => estado.state.name == "Galeria_" + animacion);
            if (!encontrado)
                throw new InvalidOperationException("Falta la animacion de galeria: " + animacion);
        }
    }

    private static void ValidarContenidoEscena()
    {
        GameObject jugador = GameObject.Find("Explorador");
        if (jugador == null || jugador.GetComponent<Jugador2D>() == null ||
            jugador.GetComponent<Rigidbody2D>() == null ||
            jugador.GetComponent<VidaJugador2D>() == null ||
            jugador.GetComponent<InventarioJugador2D>() == null)
            throw new InvalidOperationException("La escena debe incluir un jugador con movimiento, Rigidbody2D, vida e inventario.");

        if (jugador.GetComponent<Jugador2D>().accionesEntrada == null)
            throw new InvalidOperationException("El jugador no tiene asignadas sus acciones de entrada.");

        if (jugador.GetComponent<Jugador2D>().DashDesbloqueado)
            throw new InvalidOperationException("El dash debe empezar bloqueado en la demo de progresion.");

        ObjetoDesbloqueoDash2D cristalDash =
            UnityEngine.Object.FindFirstObjectByType<ObjetoDesbloqueoDash2D>();
        PuertaDash2D barreraDash =
            UnityEngine.Object.FindFirstObjectByType<PuertaDash2D>();
        if (UnityEngine.Object.FindObjectsByType<ObjetoDesbloqueoDash2D>(FindObjectsSortMode.None).Length != 1 ||
            UnityEngine.Object.FindObjectsByType<PuertaDash2D>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La demo necesita un cristal de Dash y una barrera de Dash.");

        GameObject llaveEsencial = GameObject.Find("Llave antigua");
        Puerta2D puertaEsencial = UnityEngine.Object.FindFirstObjectByType<Puerta2D>();
        DemoSalida2D salida = UnityEngine.Object.FindFirstObjectByType<DemoSalida2D>();
        Jugador2D controladorJugador = jugador.GetComponent<Jugador2D>();
        if (llaveEsencial == null || puertaEsencial == null || salida == null)
            throw new InvalidOperationException("La progresion critica debe incluir llave, puerta y salida.");

        if (cristalDash.transform.position.x <= jugador.transform.position.x ||
            cristalDash.transform.position.x >= barreraDash.transform.position.x ||
            barreraDash.transform.position.x >= llaveEsencial.transform.position.x ||
            llaveEsencial.transform.position.x >= puertaEsencial.transform.position.x ||
            puertaEsencial.transform.position.x >= salida.transform.position.x)
            throw new InvalidOperationException(
                "La progresion critica debe ordenar jugador, cristal de Dash, barrera, llave, puerta y salida.");

        float alcanceDash = controladorJugador.velocidadDash * controladorJugador.duracionDash;
        if (llaveEsencial.transform.position.x - barreraDash.transform.position.x <= alcanceDash + 1f)
            throw new InvalidOperationException(
                "La llave esencial debe quedar fuera del alcance de un unico Dash a traves de la barrera.");

        if (UnityEngine.Object.FindObjectsByType<EnemigoIA2D>(FindObjectsSortMode.None).Length != 3)
            throw new InvalidOperationException("La demo debe incluir exactamente tres enemigos.");

        if (UnityEngine.Object.FindObjectsByType<ObjetoRecolectable2D>(FindObjectsSortMode.None).Length < 5)
            throw new InvalidOperationException("La demo debe incluir monedas y una llave.");

        if (UnityEngine.Object.FindObjectsByType<DemoPuntoControl2D>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La demo debe incluir un punto de control.");

        if (UnityEngine.Object.FindObjectsByType<Puerta2D>(FindObjectsSortMode.None).Length != 1 ||
            UnityEngine.Object.FindObjectsByType<DemoSalida2D>(FindObjectsSortMode.None).Length != 1 ||
            UnityEngine.Object.FindObjectsByType<DemoHUD2D>(FindObjectsSortMode.None).Length != 1)
            throw new InvalidOperationException("La escena necesita una puerta, una salida y una interfaz.");
    }

    private static void CrearEscena(
        Sprite suelo,
        Sprite piedra,
        Sprite cristal,
        Sprite llave,
        Sprite moneda,
        Sprite portal,
        Sprite jugadorSprite,
        Sprite enemigoUnoSprite,
        Sprite enemigoDosSprite,
        RuntimeAnimatorController animatorJugador,
        RuntimeAnimatorController animatorEnemigoUno,
        RuntimeAnimatorController animatorEnemigoDos,
        GameObject prefabProyectil,
        int groundLayer,
        int enemyLayer)
    {
        Scene escena = SceneManager.GetActiveScene();

        Camera camara = new GameObject("Camara de exploracion").AddComponent<Camera>();
        camara.tag = "MainCamera";
        camara.orthographic = true;
        camara.orthographicSize = 5.2f;
        camara.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.transform.position = new Vector3(0f, 0f, -10f);
        camara.gameObject.AddComponent<AudioListener>();
        camara.gameObject.AddComponent<DemoCamara2D>();

        CrearFondo(cristal);
        CrearPlataforma("Sendero inicial", suelo, new Vector2(13f, -3.25f), new Vector2(42f, 2f), groundLayer);
        CrearPlataforma("Terraza occidental", piedra, new Vector2(6f, -0.4f), new Vector2(3.2f, 0.45f), groundLayer);
        CrearPlataforma("Terraza central", piedra, new Vector2(14f, 0.65f), new Vector2(3.6f, 0.45f), groundLayer);
        CrearPlataforma("Terraza oriental", piedra, new Vector2(21f, -0.1f), new Vector2(3.4f, 0.45f), groundLayer);
        CrearPlataforma("Boveda alta", piedra, new Vector2(25.5f, 0.8f), new Vector2(3.6f, 0.45f), groundLayer);

        for (int i = 0; i < 16; i++)
        {
            float x = -5f + i * 2.45f;
            float y = -1.35f + (i % 3) * 0.55f;
            CrearDecoracion("Cristal de fondo " + i, cristal,
                new Vector3(x, y, 1f), new Vector2(0.35f + (i % 2) * 0.2f, 0.7f + (i % 3) * 0.15f),
                new Color(0.32f, 0.69f, 0.72f, 0.38f), -3);
        }

        GameObject jugador = CrearJugador(jugadorSprite, animatorJugador, groundLayer, enemyLayer);
        SetBoolean(jugador.GetComponent<Jugador2D>(), "dashDesbloqueado", false);
        camara.gameObject.GetComponent<DemoCamara2D>().FijarObjetivo(jugador.transform);
        jugador.AddComponent<VidaJugador2D>();
        InventarioJugador2D inventario = jugador.AddComponent<InventarioJugador2D>();
        SetString(inventario, "idGuardado", "ruinas-del-eco-demo");

        CrearCristalDash(cristal, new Vector2(0f, -1.7f));
        CrearMoneda(moneda, new Vector2(-2.2f, -1.75f));
        CrearMoneda(moneda, new Vector2(6f, 0.45f));
        CrearMoneda(moneda, new Vector2(14f, 1.5f));
        CrearMoneda(moneda, new Vector2(21f, 0.75f));
        CrearRecolectable("Llave antigua", llave, new Vector2(25.5f, -1.2f), 1, "eco_key");
        CrearPuertaDash(cristal, new Vector2(10.3f, -0.1f), groundLayer);

        CrearEnemigo("Centinela del sendero", enemigoUnoSprite, animatorEnemigoUno,
            new Vector2(3.7f, -1.45f), enemyLayer, prefabProyectil, false, 1.7f, 2.3f, 1.25f, 2);
        CrearEnemigo("Acechador de piedra", enemigoDosSprite, animatorEnemigoDos,
            new Vector2(16.5f, -1.45f), enemyLayer, prefabProyectil, false, 2.9f, 5f, 1.4f, 2);
        CrearEnemigo("Vigía del umbral", enemigoUnoSprite, animatorEnemigoUno,
            new Vector2(23f, -1.45f), enemyLayer, prefabProyectil, true, 1.5f, 7f, 5f, 3);

        CrearPuntoControl(cristal, new Vector2(19.2f, -1.45f));
        CrearPuerta(suelo, new Vector2(29f, -1.15f), groundLayer);
        CrearSalida(portal, new Vector2(33.1f, -1.15f));

        DemoHUD2D hud = new GameObject("Interfaz de demo").AddComponent<DemoHUD2D>();
        SetReference(hud, "jugador", jugador.GetComponent<Jugador2D>());
        SetReference(hud, "vida", jugador.GetComponent<VidaJugador2D>());

        EditorSceneManager.MarkSceneDirty(escena);
        if (!EditorSceneManager.SaveScene(escena, ScenePath))
            throw new IOException("No se pudo guardar la escena de demo.");
    }

    private static GameObject CrearJugador(
        Sprite sprite,
        RuntimeAnimatorController controlador,
        int groundLayer,
        int enemyLayer)
    {
        GameObject jugador = new GameObject("Explorador");
        jugador.transform.position = new Vector3(-5f, -1.48f, 0f);

        SpriteRenderer renderer = jugador.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;

        Animator animator = jugador.AddComponent<Animator>();
        animator.runtimeAnimatorController = controlador;

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

        Jugador2D controladorJugador = jugador.AddComponent<Jugador2D>();
        controladorJugador.accionesEntrada = acciones;
        controladorJugador.comprobadorSuelo = suelo;
        controladorJugador.capaSuelo = 1 << groundLayer;
        controladorJugador.capaEnemigos = 1 << enemyLayer;

        GameObject espada = new GameObject("Destello de espada");
        espada.transform.SetParent(jugador.transform, false);
        espada.transform.localPosition = new Vector3(0.72f, 0.05f, 0f);
        espada.transform.localScale = new Vector3(1.15f, 0.8f, 1f);
        SpriteRenderer spriteEspada = espada.AddComponent<SpriteRenderer>();
        spriteEspada.sprite = CrearSprite("DemoTajo", TipoSprite.Tajo);
        spriteEspada.sortingOrder = 4;
        espada.SetActive(false);
        controladorJugador.espadaVisual = espada;

        return jugador;
    }

    private static void CrearEnemigo(
        string nombre,
        Sprite sprite,
        RuntimeAnimatorController controlador,
        Vector2 posicion,
        int enemyLayer,
        GameObject prefabProyectil,
        bool ataqueDistancia,
        float velocidad,
        float rangoDeteccion,
        float rangoAtaque,
        int vida)
    {
        GameObject enemigo = new GameObject(nombre);
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
        SetReference(ia, "sprite", renderer);
        SetReference(ia, "animator", animator);
        SetReference(ia, "prefabProyectil", prefabProyectil);
        SetEnum(ia, "comportamiento", 0);
        SetEnum(ia, "tipoAtaque", ataqueDistancia ? 1 : 0);
        SetFloat(ia, "velocidad", velocidad);
        SetFloat(ia, "distanciaPatrulla", 2f);
        SetFloat(ia, "rangoDeteccion", rangoDeteccion);
        SetFloat(ia, "rangoAtaque", rangoAtaque);
        SetFloat(ia, "tiempoEntreAtaques", ataqueDistancia ? 1.7f : 1.15f);
        SetInteger(ia, "vidaMaxima", vida);

        if (ataqueDistancia)
        {
            Transform punto = new GameObject("Punto de disparo").transform;
            punto.SetParent(enemigo.transform, false);
            punto.localPosition = new Vector3(0f, 0.1f, 0f);
            SetReference(ia, "puntoDisparo", punto);
        }
    }

    private static GameObject CrearPrefabProyectil(Sprite sprite)
    {
        const string ruta = DemoFolder + "/Prefabs/ProyectilEco.prefab";
        GameObject proyectil = new GameObject("ProyectilEco");

        SpriteRenderer renderer = proyectil.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        Rigidbody2D rb = proyectil.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CircleCollider2D colisionador = proyectil.AddComponent<CircleCollider2D>();
        colisionador.isTrigger = true;
        colisionador.radius = 0.22f;
        proyectil.AddComponent<ProyectilEnemigo2D>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(proyectil, ruta);
        UnityEngine.Object.DestroyImmediate(proyectil);
        return prefab;
    }

    private static void CrearPlataforma(
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 tamano,
        int groundLayer)
    {
        GameObject plataforma = new GameObject(nombre);
        plataforma.layer = groundLayer;
        plataforma.transform.position = posicion;

        SpriteRenderer renderer = plataforma.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = tamano;

        BoxCollider2D colisionador = plataforma.AddComponent<BoxCollider2D>();
        colisionador.size = tamano;
    }

    private static void CrearMoneda(Sprite sprite, Vector2 posicion)
    {
        CrearRecolectable("Eco luminoso", sprite, posicion, 0, "moneda");
    }

    private static void CrearCristalDash(Sprite sprite, Vector2 posicion)
    {
        GameObject cristal = new GameObject("Cristal de habilidad: Dash");
        cristal.transform.position = posicion;

        SpriteRenderer renderer = cristal.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.35f, 1f, 0.95f);
        renderer.sortingOrder = 4;
        cristal.transform.localScale = new Vector3(0.75f, 0.9f, 1f);

        CircleCollider2D trigger = cristal.AddComponent<CircleCollider2D>();
        trigger.isTrigger = true;
        trigger.radius = 0.65f;
        cristal.AddComponent<ObjetoDesbloqueoDash2D>();
    }

    private static void CrearRecolectable(
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        int tipo,
        string id)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.position = posicion;

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        CircleCollider2D colisionador = objeto.AddComponent<CircleCollider2D>();
        colisionador.isTrigger = true;
        colisionador.radius = 0.45f;

        ObjetoRecolectable2D recolectable = objeto.AddComponent<ObjetoRecolectable2D>();
        SetEnum(recolectable, "tipo", tipo);
        SetString(recolectable, "id", id);
        SetInteger(recolectable, "cantidad", 1);
    }

    private static void CrearPuerta(Sprite sprite, Vector2 posicion, int groundLayer)
    {
        GameObject puerta = new GameObject("Umbral cerrado (requiere la llave antigua)");
        puerta.layer = groundLayer;
        puerta.transform.position = posicion;

        BoxCollider2D solido = puerta.AddComponent<BoxCollider2D>();
        solido.size = new Vector2(0.8f, 2.2f);

        GameObject visual = new GameObject("Reja de piedra");
        visual.transform.SetParent(puerta.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(0.8f, 2.2f);
        renderer.sortingOrder = 2;

        GameObject rango = new GameObject("Rango de interaccion");
        rango.transform.SetParent(puerta.transform, false);
        BoxCollider2D trigger = rango.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.8f, 2.6f);

        Puerta2D puertaConLlave = puerta.AddComponent<Puerta2D>();
        SetString(puertaConLlave, "idLlaveRequerida", "eco_key");
        SetBoolean(puertaConLlave, "consumirLlave", false);
        SetReference(puertaConLlave, "colisionadorPuerta", solido);
        SetReference(puertaConLlave, "visualPuerta", visual);
    }

    private static void CrearPuertaDash(Sprite sprite, Vector2 posicion, int groundLayer)
    {
        GameObject puerta = new GameObject("Barrera de prueba: requiere Dash horizontal");
        puerta.layer = groundLayer;
        puerta.transform.position = posicion;

        BoxCollider2D solido = puerta.AddComponent<BoxCollider2D>();
        solido.size = new Vector2(1f, 1f);

        SpriteRenderer visual = puerta.AddComponent<SpriteRenderer>();
        visual.sprite = sprite;
        visual.color = new Color(0.32f, 0.9f, 0.88f, 0.82f);
        visual.sortingOrder = 2;
        puerta.transform.localScale = new Vector3(0.8f, 7f, 1f);

        BoxCollider2D sensor = puerta.AddComponent<BoxCollider2D>();
        sensor.isTrigger = true;
        sensor.size = new Vector2(3.125f, 1f);

        PuertaDash2D control = puerta.AddComponent<PuertaDash2D>();
        SetReference(control, "colisionadorBloqueo", solido);
        SetReference(control, "sensor", sensor);
        SetReference(control, "visualBloqueo", visual);
    }

    private static void CrearSalida(Sprite sprite, Vector2 posicion)
    {
        GameObject salida = new GameObject("Salida de las ruinas");
        salida.transform.position = posicion;

        SpriteRenderer renderer = salida.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 1;
        renderer.transform.localScale = new Vector3(1.6f, 2.4f, 1f);

        BoxCollider2D trigger = salida.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.4f, 2.4f);

        salida.AddComponent<DemoSalida2D>();
    }

    private static void CrearPuntoControl(Sprite sprite, Vector2 posicion)
    {
        GameObject puntoControl = new GameObject("Punto de control de las ruinas");
        puntoControl.transform.position = posicion;

        BoxCollider2D trigger = puntoControl.AddComponent<BoxCollider2D>();
        trigger.isTrigger = true;
        trigger.size = new Vector2(1.2f, 1.8f);

        GameObject visual = new GameObject("Cristal de retorno");
        visual.transform.SetParent(puntoControl.transform, false);
        visual.transform.localScale = new Vector3(0.8f, 1.2f, 1f);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = new Color(0.35f, 0.75f, 0.8f, 0.8f);
        renderer.sortingOrder = 2;

        PuntoControlZona2D persistente = puntoControl.AddComponent<PuntoControlZona2D>();
        SetString(persistente, "idPuntoControl", "checkpoint-ruinas-demo");
        SetReference(persistente, "indicador", renderer);
        puntoControl.AddComponent<DemoPuntoControl2D>();
    }

    private static void CrearFondo(Sprite cristal)
    {
        GameObject fondo = new GameObject("Silueta lejana de las ruinas");
        fondo.transform.position = new Vector3(12f, 2.8f, 2f);
        fondo.transform.localScale = new Vector3(48f, 2.4f, 1f);
        SpriteRenderer renderer = fondo.AddComponent<SpriteRenderer>();
        renderer.sprite = cristal;
        renderer.color = new Color(0.08f, 0.16f, 0.2f, 0.45f);
        renderer.sortingOrder = -10;
    }

    private static void CrearDecoracion(
        string nombre,
        Sprite sprite,
        Vector3 posicion,
        Vector2 escala,
        Color color,
        int orden)
    {
        GameObject decoracion = new GameObject(nombre);
        decoracion.transform.position = posicion;
        decoracion.transform.localScale = new Vector3(escala.x, escala.y, 1f);
        SpriteRenderer renderer = decoracion.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = orden;
    }

    private static RuntimeAnimatorController CrearAnimadorJugador()
    {
        const string ruta = DemoFolder + "/Animations/DemoJugador.controller";
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(ruta);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Grounded", AnimatorControllerParameterType.Bool);
        controller.AddParameter("YVelocity", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState idle = AgregarEstado(maquina, "Quieto", "Assets/Animation/Animations/Player/Movement/Idle.anim");
        AnimatorState move = AgregarEstado(maquina, "Avanzar", "Assets/Animation/Animations/Player/Movement/Move.anim");
        AnimatorState jump = AgregarEstado(maquina, "Ascenso", "Assets/Animation/Animations/Player/Movement/JumpUp1.anim");
        AnimatorState fall = AgregarEstado(maquina, "Caida", "Assets/Animation/Animations/Player/Movement/Fall.anim");
        AnimatorState attack = AgregarEstado(maquina, "Tajo", "Assets/Animation/Animations/Player/Movement/Idle.anim");
        AgregarAnimacionesGaleria(maquina);
        maquina.defaultState = idle;

        Transicion(idle, move, AnimatorConditionMode.Greater, "Speed", 0.1f);
        Transicion(move, idle, AnimatorConditionMode.Less, "Speed", 0.1f);
        Transicion(maquina, jump, AnimatorConditionMode.IfNot, "Grounded", 0f,
            AnimatorConditionMode.Greater, "YVelocity", 0.1f);
        Transicion(maquina, fall, AnimatorConditionMode.IfNot, "Grounded", 0f,
            AnimatorConditionMode.Less, "YVelocity", 0f);
        Transicion(jump, fall, AnimatorConditionMode.Less, "YVelocity", 0f);
        Transicion(fall, idle, AnimatorConditionMode.If, "Grounded", 0f,
            AnimatorConditionMode.Less, "Speed", 0.1f);
        Transicion(fall, move, AnimatorConditionMode.If, "Grounded", 0f,
            AnimatorConditionMode.Greater, "Speed", 0.1f);
        Transicion(maquina, attack, AnimatorConditionMode.If, "Attack", 0f);
        SalirAlTerminar(attack, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static void AgregarAnimacionesGaleria(AnimatorStateMachine maquina)
    {
        string[] nombres =
        {
            "CrouchIdle", "CrouchMove", "Fall", "Idle", "JumpUp1", "JumpUp1X", "JumpUp2",
            "JumpUp2X", "JumpUp3", "JumpUp3X", "Land", "LedgeClimb", "LedgeClimbCrouch",
            "LedgeGrab", "LedgeHold", "Move", "Stun", "WallClimb", "WallGrab", "WallSlide",
            "bow_action", "bow_anticipation", "bow_idle", "Shield_Anticipation", "Shield_Break",
            "Shield_Cancel", "Shield_Idle", "Shield_Parry", "Sword_Attack_1", "Sword_Attack_2",
            "Sword_Attack_3", "Tome_Action", "Tome_Anticipation", "Tome_Cancel", "Tome_Idle"
        };

        foreach (string nombre in nombres)
        {
            string directorio = nombre.StartsWith("bow_", StringComparison.Ordinal)
                ? "Combat/Bow/"
                : nombre.StartsWith("Shield_", StringComparison.Ordinal)
                    ? "Combat/Shield/"
                    : nombre.StartsWith("Sword_", StringComparison.Ordinal)
                        ? "Combat/Sword/"
                        : nombre.StartsWith("Tome_", StringComparison.Ordinal)
                            ? "Combat/Tome/"
                            : "Movement/";
            string ruta = "Assets/Animation/Animations/Player/" + directorio + nombre + ".anim";
            AgregarEstado(maquina, "Galeria_" + nombre, ruta);
        }
    }

    private static RuntimeAnimatorController CrearAnimadorEnemigo(
        string rutaIdle,
        string rutaMover,
        string rutaAtaque,
        string rutaHerida,
        string nombre)
    {
        string rutaController = DemoFolder + "/Animations/" + nombre;
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(rutaController);
        controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
        controller.AddParameter("Attack", AnimatorControllerParameterType.Trigger);
        controller.AddParameter("Hurt", AnimatorControllerParameterType.Trigger);

        AnimatorStateMachine maquina = controller.layers[0].stateMachine;
        AnimatorState idle = AgregarEstado(maquina, "Quieto", rutaIdle);
        AnimatorState move = AgregarEstado(maquina, "Caminar", rutaMover);
        AnimatorState attack = AgregarEstado(maquina, "Atacar", rutaAtaque);
        AnimatorState hurt = AgregarEstado(maquina, "Recibir dano", rutaHerida);
        maquina.defaultState = idle;

        Transicion(idle, move, AnimatorConditionMode.Greater, "Speed", 0.1f);
        Transicion(move, idle, AnimatorConditionMode.Less, "Speed", 0.1f);
        Transicion(maquina, attack, AnimatorConditionMode.If, "Attack", 0f);
        Transicion(maquina, hurt, AnimatorConditionMode.If, "Hurt", 0f);
        SalirAlTerminar(attack, idle);
        SalirAlTerminar(hurt, idle);

        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static AnimatorState AgregarEstado(AnimatorStateMachine maquina, string nombre, string rutaClip)
    {
        AnimatorState estado = maquina.AddState(nombre);
        estado.motion = AssetDatabase.LoadAssetAtPath<AnimationClip>(rutaClip);
        if (estado.motion == null)
            throw new FileNotFoundException("No se encontro el clip de animacion.", rutaClip);

        return estado;
    }

    private static void Transicion(
        AnimatorState origen,
        AnimatorState destino,
        AnimatorConditionMode modo,
        string parametro,
        float valor)
    {
        AnimatorStateTransition transicion = origen.AddTransition(destino);
        transicion.hasExitTime = false;
        transicion.duration = 0.06f;
        transicion.AddCondition(modo, valor, parametro);
    }

    private static void Transicion(
        AnimatorState origen,
        AnimatorState destino,
        AnimatorConditionMode modoUno,
        string parametroUno,
        float valorUno,
        AnimatorConditionMode modoDos,
        string parametroDos,
        float valorDos)
    {
        AnimatorStateTransition transicion = origen.AddTransition(destino);
        transicion.hasExitTime = false;
        transicion.duration = 0.06f;
        transicion.AddCondition(modoUno, valorUno, parametroUno);
        transicion.AddCondition(modoDos, valorDos, parametroDos);
    }

    private static void Transicion(
        AnimatorStateMachine maquina,
        AnimatorState destino,
        AnimatorConditionMode modoUno,
        string parametroUno,
        float valorUno,
        AnimatorConditionMode modoDos,
        string parametroDos,
        float valorDos)
    {
        AnimatorStateTransition transicion = maquina.AddAnyStateTransition(destino);
        transicion.hasExitTime = false;
        transicion.duration = 0.06f;
        transicion.canTransitionToSelf = false;
        transicion.AddCondition(modoUno, valorUno, parametroUno);
        transicion.AddCondition(modoDos, valorDos, parametroDos);
    }

    private static void Transicion(
        AnimatorStateMachine maquina,
        AnimatorState destino,
        AnimatorConditionMode modo,
        string parametro,
        float valor)
    {
        AnimatorStateTransition transicion = maquina.AddAnyStateTransition(destino);
        transicion.hasExitTime = false;
        transicion.duration = 0.06f;
        transicion.canTransitionToSelf = false;
        transicion.AddCondition(modo, valor, parametro);
    }

    private static void SalirAlTerminar(AnimatorState origen, AnimatorState destino)
    {
        AnimatorStateTransition transicion = origen.AddTransition(destino);
        transicion.hasExitTime = true;
        transicion.exitTime = 0.9f;
        transicion.duration = 0.06f;
    }

    private enum TipoSprite
    {
        Suelo,
        Piedra,
        Cristal,
        Llave,
        Moneda,
        Portal,
        Proyectil,
        Tajo
    }

    private static Sprite CrearSprite(string nombre, TipoSprite tipo)
    {
        string texturaPath = DemoFolder + "/Art/" + nombre + "Texture.asset";
        string spritePath = DemoFolder + "/Art/" + nombre + ".asset";
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
        if (existente != null)
            return existente;

        const int dimension = 16;
        Color32[] pixeles = new Color32[dimension * dimension];
        System.Random random = new System.Random((int)tipo * 739 + 41);
        Color32 oscuro = new Color32(31, 48, 62, 255);
        Color32 medio = new Color32(48, 73, 82, 255);
        Color32 brillo = new Color32(81, 123, 123, 255);

        for (int y = 0; y < dimension; y++)
        {
            for (int x = 0; x < dimension; x++)
            {
                Color32 color = new Color32(0, 0, 0, 0);
                switch (tipo)
                {
                    case TipoSprite.Suelo:
                        color = y == 15 ? brillo : (random.Next(100) < 18 ? brillo : medio);
                        if (y < 3)
                            color = oscuro;
                        break;
                    case TipoSprite.Piedra:
                        color = random.Next(100) < 20 ? brillo : medio;
                        if ((x + y) % 7 == 0)
                            color = oscuro;
                        break;
                    case TipoSprite.Cristal:
                        if (x >= 4 && x <= 11 && y >= 2 && y <= 13 &&
                            Math.Abs(x - 7) + Math.Abs(y - 8) / 2 <= 6)
                            color = x == 6 ? new Color32(150, 227, 213, 255) : new Color32(54, 135, 147, 255);
                        break;
                    case TipoSprite.Llave:
                        if ((x >= 4 && x <= 11 && y >= 10 && y <= 12) ||
                            (x >= 4 && x <= 6 && y >= 7 && y <= 13) ||
                            (x >= 6 && x <= 12 && y >= 3 && y <= 5) ||
                            (x >= 10 && x <= 12 && y >= 3 && y <= 7) ||
                            (x >= 9 && x <= 11 && y >= 7 && y <= 10))
                            color = new Color32(238, 193, 109, 255);
                        break;
                    case TipoSprite.Moneda:
                        if ((x - 8) * (x - 8) + (y - 8) * (y - 8) <= 42)
                            color = new Color32(242, 206, 126, 255);
                        if ((x - 8) * (x - 8) + (y - 8) * (y - 8) <= 20)
                            color = new Color32(172, 119, 68, 255);
                        break;
                    case TipoSprite.Portal:
                        float distancia = Mathf.Sqrt((x - 8) * (x - 8) + (y - 8) * (y - 8));
                        if (distancia > 4f && distancia < 7f)
                            color = new Color32(91, 205, 197, 255);
                        else if (distancia <= 4f)
                            color = new Color32(16, 40, 57, 255);
                        break;
                    case TipoSprite.Proyectil:
                        if ((x - 8) * (x - 8) + (y - 8) * (y - 8) <= 42)
                            color = new Color32(148, 237, 215, 255);
                        break;
                    case TipoSprite.Tajo:
                        float radio = Mathf.Sqrt((x - 7) * (x - 7) + (y - 8) * (y - 8));
                        if (radio >= 5f && radio <= 7f && x >= 5 && y >= 2 && y <= 14)
                            color = new Color32(183, 244, 224, 230);
                        break;
                }

                pixeles[y * dimension + x] = color;
            }
        }

        Texture2D textura = new Texture2D(dimension, dimension, TextureFormat.RGBA32, false)
        {
            name = nombre + "Texture",
            filterMode = FilterMode.Point,
            wrapMode = tipo == TipoSprite.Suelo || tipo == TipoSprite.Piedra
                ? TextureWrapMode.Repeat
                : TextureWrapMode.Clamp
        };
        textura.SetPixels32(pixeles);
        textura.Apply(false, false);
        AssetDatabase.CreateAsset(textura, texturaPath);

        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, dimension, dimension),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect
        );
        sprite.name = nombre;
        AssetDatabase.CreateAsset(sprite, spritePath);
        return sprite;
    }

    private static Sprite PrimerSprite(string ruta)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(ruta)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static int CrearOCargarCapa(string nombre)
    {
        int existente = LayerMask.NameToLayer(nombre);
        if (existente >= 0)
            return existente;

        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0)
            throw new InvalidOperationException("No se pudo abrir la configuracion de capas de Unity.");

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty capas = tagManager.FindProperty("layers");
        for (int i = 8; i < capas.arraySize; i++)
        {
            SerializedProperty capa = capas.GetArrayElementAtIndex(i);
            if (string.IsNullOrEmpty(capa.stringValue))
            {
                capa.stringValue = nombre;
                tagManager.ApplyModifiedProperties();
                AssetDatabase.SaveAssets();
                return i;
            }
        }

        throw new InvalidOperationException("No quedan capas libres para '" + nombre + "'.");
    }

    private static void CrearOCargarTag(string nombre)
    {
        UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset");
        if (assets.Length == 0)
            throw new InvalidOperationException("No se pudo abrir la configuracion de tags de Unity.");

        SerializedObject tagManager = new SerializedObject(assets[0]);
        SerializedProperty tags = tagManager.FindProperty("tags");
        for (int i = 0; i < tags.arraySize; i++)
        {
            if (tags.GetArrayElementAtIndex(i).stringValue == nombre)
                return;
        }

        int nuevoIndice = tags.arraySize;
        tags.InsertArrayElementAtIndex(nuevoIndice);
        tags.GetArrayElementAtIndex(nuevoIndice).stringValue = nombre;
        tagManager.ApplyModifiedProperties();
        AssetDatabase.SaveAssets();
    }

    private static void CrearCarpetas()
    {
        CrearCarpetaSiFalta("Assets", "Demo");
        CrearCarpetaSiFalta(DemoFolder, "Art");
        CrearCarpetaSiFalta(DemoFolder, "Animations");
        CrearCarpetaSiFalta(DemoFolder, "Prefabs");
        CrearCarpetaSiFalta("Assets", "Editor");
    }

    private static void CrearCarpetaSiFalta(string padre, string nombre)
    {
        string ruta = padre + "/" + nombre;
        if (!AssetDatabase.IsValidFolder(ruta))
            AssetDatabase.CreateFolder(padre, nombre);
    }

    private static void SetReference(UnityEngine.Object objetivo, string nombre, UnityEngine.Object valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.objectReferenceValue = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetString(UnityEngine.Object objetivo, string nombre, string valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.stringValue = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBoolean(UnityEngine.Object objetivo, string nombre, bool valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.boolValue = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(UnityEngine.Object objetivo, string nombre, float valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.floatValue = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInteger(UnityEngine.Object objetivo, string nombre, int valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.intValue = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetEnum(UnityEngine.Object objetivo, string nombre, int valor)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty propiedad = serializado.FindProperty(nombre);
        if (propiedad == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);

        propiedad.enumValueIndex = valor;
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }
}
