using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class CrearGaleriaValvulas2D
{
    public const string RutaEscena = "Assets/Scenes/GaleriaValvulas.unity";
    private const string CarpetaArte = "Assets/Demo/GaleriaValvulas";
    private const string RutaJugador = "Assets/Sprites/Player/Movement/PlayerMovement.png";
    private const string RutaEnemigoUno = "Assets/Sprites/Enemies/Enemy 1..png";
    private const string RutaEnemigoDos = "Assets/Sprites/Enemies/Enemy2.png";
    private const string RutaControladorJugador =
        "Assets/Animation/AnimatorControllers/Player/Player AC.controller";
    private const string RutaControladorEnemigoUno =
        "Assets/Animation/AnimatorControllers/Enemies/Enemy1 AC.controller";
    private const string RutaControladorEnemigoDos =
        "Assets/Animation/AnimatorControllers/Enemies/Enemy2 AC.controller";

    [MenuItem("Demo/Generar zona: Galeria de Valvulas")]
    public static void Generar()
    {
        ValidarRecursos();
        Directory.CreateDirectory(CarpetaArte);

        Sprite suelo = CrearSprite("ValvulaSuelo", new Color32(45, 52, 54, 255), new Color32(85, 107, 104, 255));
        Sprite tuberia = CrearSprite("TuberiaCobre", new Color32(140, 85, 55, 255), new Color32(205, 138, 90, 255));
        Sprite pasarela = CrearSprite("PasarelaMetalica", new Color32(65, 78, 75, 255), new Color32(110, 135, 125, 255));
        Sprite vapor = CrearSprite("VaporChorro", new Color32(200, 230, 245, 180), new Color32(255, 255, 255, 230));
        Sprite compuerta = CrearSprite("CompuertaMetalica", new Color32(50, 65, 70, 255), new Color32(150, 175, 180, 255));
        Sprite campana = CrearSprite("CampanaValvula", new Color32(215, 160, 95, 255), new Color32(245, 215, 155, 255));
        Sprite barreraSprite = CrearSprite("BarreraResonancia", new Color32(40, 160, 180, 220), new Color32(140, 245, 235, 255));

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene escena = SceneManager.GetActiveScene();
        escena.name = "Galeria de Valvulas";

        int groundLayer = LayerMask.NameToLayer("Ground");
        int enemyLayer = LayerMask.NameToLayer("Enemy");
        if (groundLayer < 0 || enemyLayer < 0)
            throw new InvalidOperationException("La escena necesita las capas Ground y Enemy.");

        GameObject raiz = new GameObject("Zona - Galeria de Valvulas");
        ZonaMetroidvania2D zona = raiz.AddComponent<ZonaMetroidvania2D>();
        Set(zona, "idZona", "galeria-valvulas");
        Set(zona, "nombreZona", "Galeria de Valvulas");
        Set(zona, "siguienteZona", "Talleres de Aire");
        Set(zona, "limitesX", new Vector2(0f, 92f));
        Set(zona, "limitesY", new Vector2(-8f, 15f));

        GameObject geometria = CrearGrupo(raiz.transform, "Geometria y rutas");
        GameObject puntos = CrearGrupo(raiz.transform, "Puntos de interes");
        GameObject obstaculos = CrearGrupo(raiz.transform, "Obstaculos y Mecanismos");
        GameObject encuentros = CrearGrupo(raiz.transform, "Encuentros");

        // Fondo de tuberias industriales
        CrearFondo(puntos.transform, tuberia);

        // --- Seccion 1: Entrada y calibracion de Dash ---
        CrearPlataforma(geometria.transform, "Suelo entrada 01", suelo,
            new Vector2(8f, -4f), new Vector2(16f, 1.3f), groundLayer);
        CrearPlataforma(geometria.transform, "Plataforma previa foso", pasarela,
            new Vector2(18f, -2.8f), new Vector2(4f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Plataforma de barrera", pasarela,
            new Vector2(23f, -2.5f), new Vector2(4f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Plataforma recepcion post-barrera", pasarela,
            new Vector2(28.5f, -2.8f), new Vector2(4f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Fondo foso seguro", suelo,
            new Vector2(23.5f, -7.5f), new Vector2(15f, 0.8f), groundLayer);

        // --- Seccion 2: Corredor de valvulas y vapor ---
        CrearPlataforma(geometria.transform, "Suelo valvulas 01", suelo,
            new Vector2(35f, -4f), new Vector2(9f, 1.3f), groundLayer);
        CrearPlataforma(geometria.transform, "Pasarela elevada vapor", pasarela,
            new Vector2(44f, -1.5f), new Vector2(7f, 0.45f), groundLayer);
        CrearPlataforma(geometria.transform, "Plataforma secreta pogo", pasarela,
            new Vector2(44f, 3.8f), new Vector2(4f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Suelo valvulas 02", suelo,
            new Vector2(53f, -4f), new Vector2(9f, 1.3f), groundLayer);

        // --- Tunel inferior del atajo ---
        CrearPlataforma(geometria.transform, "Suelo tunel atajo", suelo,
            new Vector2(43f, -6.5f), new Vector2(30f, 0.8f), groundLayer);
        CrearPlataforma(geometria.transform, "Techo tunel atajo", suelo,
            new Vector2(43f, -4.9f), new Vector2(28f, 0.6f), groundLayer);

        // --- Seccion 3: Estacion de descanso y valvula de atajo ---
        CrearPlataforma(geometria.transform, "Suelo estacion y mecanismo", suelo,
            new Vector2(65f, -4f), new Vector2(15f, 1.3f), groundLayer);

        // --- Seccion 4: Subida hacia Talleres de Aire ---
        CrearPlataforma(geometria.transform, "Escalon hacia Talleres 01", pasarela,
            new Vector2(76f, -2.5f), new Vector2(3.5f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Escalon hacia Talleres 02", pasarela,
            new Vector2(81f, -0.5f), new Vector2(3.5f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Pasarela salida Talleres", pasarela,
            new Vector2(87f, 1.8f), new Vector2(9f, 0.45f), groundLayer);

        // Limites de la zona
        CrearLimite(geometria.transform, "Limite izquierdo", new Vector2(0f, 3f),
            new Vector2(0.7f, 20f), groundLayer);
        CrearLimite(geometria.transform, "Limite derecho", new Vector2(92f, 3f),
            new Vector2(0.7f, 20f), groundLayer);
        CrearLimite(geometria.transform, "Fondo seguro general", new Vector2(46f, -9f),
            new Vector2(92f, 0.6f), groundLayer);

        // Marcadores de entrada y salida
        Transform entradaOeste = CrearMarcador(geometria.transform, "Entrada desde Canal de las Campanas",
            new Vector2(2.5f, -2.68f));
        PuntoEntradaZona2D compEntrada = entradaOeste.gameObject.AddComponent<PuntoEntradaZona2D>();
        Set(compEntrada, "idEntrada", "entrada-desde-canal");
        Set(zona, "puntoEntrada", entradaOeste);

        Transform salidaTalleres = CrearMarcador(geometria.transform, "Conexion a Talleres de Aire",
            new Vector2(89.5f, 2.7f));
        Set(zona, "conexionSiguiente", salidaTalleres);

        // Interfaz y HUD
        InterfazZonaMetroidvania2D interfaz = CrearInterfaz(raiz.transform);

        // Senales y textos de orientacion
        CrearSenal(puntos.transform, "Senal - entrada Canal", campana,
            new Vector2(2.2f, -1.2f), "CANAL DE LAS CAMPANAS  ←", new Color(0.85f, 0.96f, 0.87f));
        CrearSenal(puntos.transform, "Senal - barrera dash", tuberia,
            new Vector2(17.5f, -1.4f), "PRESIÓN CRÍTICA: ATRAVESAR CON DASH", new Color(0.6f, 0.95f, 0.95f));
        CrearSenal(puntos.transform, "Senal - estacion valvulas", campana,
            new Vector2(61.5f, -1.2f), "ESTACIÓN DE VÁLVULAS - DESCANSO", new Color(0.7f, 0.95f, 0.85f));
        CrearSenal(puntos.transform, "Senal - salida talleres", tuberia,
            new Vector2(88f, 3.8f), "TALLERES DE AIRE  →", new Color(1f, 0.88f, 0.6f));

        CrearPuntoDeInteres(puntos.transform, "Registro de Cuadrilla 14",
            new Vector2(34f, -1.8f), "«Válvulas cerradas por acuerdo. La presión remanente abastece a los Talleres de Aire.»");

        // Monedas / Ecos
        CrearMoneda(puntos.transform, campana, new Vector2(35f, -2.6f), 2);
        CrearMoneda(puntos.transform, campana, new Vector2(44f, 4.8f), 3); // Recompensa pogo
        CrearMoneda(puntos.transform, campana, new Vector2(76f, -1.2f), 2);

        // --- Obstaculos y Mecanismos ---

        // 1. Barrera de Dash sobre el vacio
        CrearBarreraDash(obstaculos.transform, barreraSprite, new Vector2(23f, -1.8f));

        // 2. Chorro de vapor 1 (suelo inferior corredor)
        CrearChorroVapor(obstaculos.transform, vapor, new Vector2(38f, -3.2f),
            reposo: 2.2f, aviso: 0.6f, activo: 1.2f, desfase: 0f);

        // 3. Chorro de vapor 2 (sobre pasarela elevada)
        CrearChorroVapor(obstaculos.transform, vapor, new Vector2(44f, -0.6f),
            reposo: 2.2f, aviso: 0.6f, activo: 1.2f, desfase: 1.1f);

        // 4. Chorro de vapor 3 (salida del corredor)
        CrearChorroVapor(obstaculos.transform, vapor, new Vector2(51f, -3.2f),
            reposo: 2.2f, aviso: 0.6f, activo: 1.2f, desfase: 2.2f);

        // 5. Campana elastica rebotable para pogo hacia plataforma secreta
        CrearCampanaPogo(obstaculos.transform, campana, new Vector2(44f, 0.9f));

        // 6. Compuerta de Atajo y Valvula de Apertura
        CrearCompuertaYValvulaAtajo(obstaculos.transform, compuerta, tuberia, interfaz);

        // --- Conexiones de Zona ---
        // Retorno hacia Canal de las Campanas
        CrearConexionRetornoCanal(geometria.transform, campana, new Vector2(1.5f, -2.68f), zona, interfaz);
        // Salida hacia Talleres de Aire
        CrearConexionTalleres(geometria.transform, tuberia, salidaTalleres.position, zona, interfaz);

        // Estacion de retorno / checkpoint
        CrearEstacionDescanso(puntos.transform, campana, new Vector2(62f, -2.68f), zona);

        // --- Encuentros con Enemigos ---
        CrearEnemigo(encuentros.transform, "Escarabajo de junta 01",
            RutaEnemigoUno, RutaControladorEnemigoUno, new Vector2(36f, -2.68f), 3, enemyLayer, 32f, 40f);
        CrearEnemigo(encuentros.transform, "Automata de inspeccion 01",
            RutaEnemigoDos, RutaControladorEnemigoDos, new Vector2(45f, -0.2f), 2, enemyLayer, 42f, 47f);
        CrearEnemigo(encuentros.transform, "Escarabajo de junta 02",
            RutaEnemigoUno, RutaControladorEnemigoUno, new Vector2(78f, -1.2f), 3, enemyLayer, 75f, 82f);

        // --- Jugador y Camara ---
        GameObject jugador = CrearJugador(groundLayer, enemyLayer);
        jugador.transform.position = entradaOeste.position;
        Camera camara = CrearCamara(zona, jugador.transform);

        AnadirEscenaABuildSettings();

        EditorSceneManager.MarkSceneDirty(escena);
        if (!EditorSceneManager.SaveScene(escena, RutaEscena))
            throw new InvalidOperationException("No se pudo guardar la escena " + RutaEscena);

        Debug.Log("Escena 'Galeria de Valvulas' generada y guardada con exito en " + RutaEscena);
    }

    [MenuItem("Demo/Validar zona: Galeria de Valvulas")]
    public static void Validar()
    {
        Scene escena = EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        GameObject raiz = GameObject.Find("Zona - Galeria de Valvulas");
        if (raiz == null)
            throw new InvalidOperationException("No se encontro el objeto raiz 'Zona - Galeria de Valvulas'.");

        ZonaMetroidvania2D zona = raiz.GetComponent<ZonaMetroidvania2D>();
        if (zona == null)
            throw new InvalidOperationException("La raiz debe contener ZonaMetroidvania2D.");

        Camera camara = Camera.main;
        if (camara == null || camara.GetComponent<CamaraZona2D>() == null)
            throw new InvalidOperationException("La camara debe tener CamaraZona2D.");

        Jugador2D jugador = raiz.GetComponentInChildren<Jugador2D>(true);
        if (jugador == null || !jugador.DashDesbloqueado)
            throw new InvalidOperationException("El jugador debe estar presente con Dash desbloqueado.");

        PuertaDash2D barrera = raiz.GetComponentInChildren<PuertaDash2D>(true);
        if (barrera == null)
            throw new InvalidOperationException("La zona debe contener la barrera de Dash.");

        ObstaculoVapor2D[] vapores = raiz.GetComponentsInChildren<ObstaculoVapor2D>(true);
        if (vapores.Length < 3)
            throw new InvalidOperationException("La zona debe incluir al menos tres obstaculos de vapor.");

        CompuertaAtajo2D compuerta = raiz.GetComponentInChildren<CompuertaAtajo2D>(true);
        if (compuerta == null)
            throw new InvalidOperationException("La zona debe incluir la CompuertaAtajo2D.");

        CampanaResonante2D campanaPogo = raiz.GetComponentInChildren<CampanaResonante2D>(true);
        if (campanaPogo == null)
            throw new InvalidOperationException("La zona debe incluir la campana de rebote pogo.");

        PuntoControlZona2D checkpoint = raiz.GetComponentInChildren<PuntoControlZona2D>(true);
        if (checkpoint == null || !checkpoint.GetComponent<Collider2D>().isTrigger)
            throw new InvalidOperationException("La estacion de descanso debe ser un trigger valido.");

        PuntoEntradaZona2D[] entradas = raiz.GetComponentsInChildren<PuntoEntradaZona2D>(true);
        if (!entradas.Any(e => e.IdEntrada == "entrada-desde-canal"))
            throw new InvalidOperationException("Debe existir el punto de entrada 'entrada-desde-canal'.");

        ConexionZona2D[] conexiones = raiz.GetComponentsInChildren<ConexionZona2D>(true);
        if (conexiones.Length < 2)
            throw new InvalidOperationException("Debe haber conexiones de retorno al Canal y de avance hacia Talleres.");

        EditorBuildSettingsScene[] escenas = EditorBuildSettings.scenes;
        if (!escenas.Any(e => e.enabled && e.path == RutaEscena))
            throw new InvalidOperationException("GaleriaValvulas.unity debe estar habilitada en Build Settings.");

        Debug.Log("Validacion de 'Galeria de Valvulas' completada correctamente: " +
                  "geometria, barrera dash, 3 trampas de vapor, compuerta de atajo, campana pogo, descanso y transiciones.");
    }

    private static void CrearBarreraDash(Transform padre, Sprite sprite, Vector2 posicion)
    {
        GameObject objeto = new GameObject("Barrera de Dash (Presion)");
        objeto.transform.SetParent(padre, false);
        objeto.transform.position = posicion;

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;
        renderer.color = new Color(0.2f, 0.85f, 0.95f, 0.8f);

        BoxCollider2D colliderSolido = objeto.AddComponent<BoxCollider2D>();
        colliderSolido.size = new Vector2(0.5f, 2.8f);
        colliderSolido.isTrigger = false;

        BoxCollider2D sensor = objeto.AddComponent<BoxCollider2D>();
        sensor.size = new Vector2(1.8f, 2.8f);
        sensor.isTrigger = true;

        PuertaDash2D puerta = objeto.AddComponent<PuertaDash2D>();
        Set(puerta, "colisionadorBloqueo", colliderSolido);
        Set(puerta, "sensor", sensor);
        Set(puerta, "visualBloqueo", renderer);
    }

    private static void CrearChorroVapor(
        Transform padre,
        Sprite sprite,
        Vector2 posicion,
        float reposo,
        float aviso,
        float activo,
        float desfase)
    {
        GameObject objeto = new GameObject("Chorro de Vapor");
        objeto.transform.SetParent(padre, false);
        objeto.transform.position = posicion;

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        BoxCollider2D trigger = objeto.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(0.8f, 2.4f);
        trigger.offset = new Vector2(0f, 1.2f);
        trigger.isTrigger = true;

        ObstaculoVapor2D vapor = objeto.AddComponent<ObstaculoVapor2D>();
        vapor.Configurar(reposo, aviso, activo, desfase, nuevoDanio: 1);
        Set(vapor, "colisionadorVapor", trigger);
        Set(vapor, "visualVapor", renderer);
        Set(vapor, "direccionEmpuje", Vector2.up);
        Set(vapor, "fuerzaEmpuje", 8f);
    }

    private static void CrearCampanaPogo(Transform padre, Sprite sprite, Vector2 posicion)
    {
        GameObject objeto = new GameObject("Campana Elastica Pogo");
        objeto.transform.SetParent(padre, false);
        objeto.transform.position = posicion;

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;

        CircleCollider2D collider = objeto.AddComponent<CircleCollider2D>();
        collider.radius = 0.5f;
        collider.isTrigger = true;

        CampanaResonante2D campana = objeto.AddComponent<CampanaResonante2D>();
        campana.Configurar(14.5f);
    }

    private static void CrearCompuertaYValvulaAtajo(
        Transform padre,
        Sprite spriteCompuerta,
        Sprite spriteValvula,
        InterfazZonaMetroidvania2D interfaz)
    {
        GameObject raizCompuerta = new GameObject("Atajo de Compuerta Hidraulica");
        raizCompuerta.transform.SetParent(padre, false);
        raizCompuerta.transform.position = new Vector2(57f, -5.5f);

        // Panel fisico de la compuerta
        GameObject panel = new GameObject("Panel Compuerta");
        panel.transform.SetParent(raizCompuerta.transform, false);
        panel.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = panel.AddComponent<SpriteRenderer>();
        renderer.sprite = spriteCompuerta;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(0.6f, 2.8f);
        renderer.sortingOrder = 2;

        BoxCollider2D colliderSolido = panel.AddComponent<BoxCollider2D>();
        colliderSolido.size = new Vector2(0.6f, 2.8f);
        colliderSolido.isTrigger = false;

        // Valvula interactuable (lado interior)
        GameObject valvula = new GameObject("Valvula de Derivacion");
        valvula.transform.SetParent(raizCompuerta.transform, false);
        valvula.transform.position = new Vector2(65f, -3.2f);

        SpriteRenderer rendererValvula = valvula.AddComponent<SpriteRenderer>();
        rendererValvula.sprite = spriteValvula;
        rendererValvula.sortingOrder = 2;

        BoxCollider2D triggerValvula = valvula.AddComponent<BoxCollider2D>();
        triggerValvula.size = new Vector2(1.8f, 2f);
        triggerValvula.isTrigger = true;

        CompuertaAtajo2D compuerta = valvula.AddComponent<CompuertaAtajo2D>();
        Set(compuerta, "colisionadorSolido", colliderSolido);
        Set(compuerta, "panelCompuerta", panel.transform);
        Set(compuerta, "interfaz", interfaz);
        Set(compuerta, "desplazamientoApertura", new Vector2(0f, 3f));
        Set(compuerta, "exigirLadoApertura", true);
        Set(compuerta, "coordenadaXMinimaApertura", 55f);
        Set(compuerta, "textoIndicacion", "Presiona E para activar la valvula de atajo.");

        SenalZona2D senal = valvula.AddComponent<SenalZona2D>();
        Set(senal, "texto", "VALVULA DE DERIVACION  [E]");
        Set(senal, "color", new Color(0.9f, 0.8f, 0.5f));
        Set(senal, "ancho", 220f);
    }

    private static void CrearConexionRetornoCanal(
        Transform padre,
        Sprite sprite,
        Vector2 posicion,
        ZonaMetroidvania2D zona,
        InterfazZonaMetroidvania2D interfaz)
    {
        GameObject conexion = CrearDecoracion(padre, "Conexion de regreso al Canal", sprite, posicion,
            new Vector2(1.2f, 1.6f), 2);
        BoxCollider2D trigger = conexion.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2.5f, 2.6f);
        trigger.isTrigger = true;

        ConexionZona2D componente = conexion.AddComponent<ConexionZona2D>();
        Set(componente, "zona", zona);
        Set(componente, "interfaz", interfaz);
        Set(componente, "rutaEscenaDestino", "Assets/Scenes/CanalCampanas.unity");
        Set(componente, "idEntradaDestino", "retorno-desde-galeria");
        Set(componente, "textoIndicacion", "Presiona E para volver al Canal de las Campanas.");

        SenalZona2D senal = conexion.AddComponent<SenalZona2D>();
        Set(senal, "texto", "CANAL DE LAS CAMPANAS  ←");
        Set(senal, "color", new Color(0.78f, 0.94f, 0.86f));
        Set(senal, "ancho", 220f);
    }

    private static void CrearConexionTalleres(
        Transform padre,
        Sprite sprite,
        Vector2 posicion,
        ZonaMetroidvania2D zona,
        InterfazZonaMetroidvania2D interfaz)
    {
        GameObject conexion = CrearDecoracion(padre, "Conexion a Talleres de Aire", sprite, posicion,
            new Vector2(1.2f, 1.6f), 2);
        BoxCollider2D trigger = conexion.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2.5f, 2.6f);
        trigger.isTrigger = true;

        ConexionZona2D componente = conexion.AddComponent<ConexionZona2D>();
        Set(componente, "zona", zona);
        Set(componente, "interfaz", interfaz);
        Set(componente, "rutaEscenaDestino", ""); // Siguiente zona aun no construida
        Set(componente, "idEntradaDestino", "entrada-desde-galeria");
        Set(componente, "textoIndicacion", "Presiona E para ascender a los Talleres de Aire.");

        SenalZona2D senal = conexion.AddComponent<SenalZona2D>();
        Set(senal, "texto", "TALLERES DE AIRE  →");
        Set(senal, "color", new Color(1f, 0.85f, 0.6f));
        Set(senal, "ancho", 200f);
    }

    private static void CrearEstacionDescanso(
        Transform padre,
        Sprite sprite,
        Vector2 posicion,
        ZonaMetroidvania2D zona)
    {
        GameObject estacion = CrearDecoracion(padre, "Estacion de retorno - Galeria de Valvulas", sprite, posicion,
            new Vector2(1.3f, 1.4f), 1);
        BoxCollider2D trigger = estacion.AddComponent<BoxCollider2D>();
        trigger.size = new Vector2(2.2f, 2.2f);
        trigger.isTrigger = true;

        Transform puntoReaparicion = new GameObject("PuntoReaparicion").transform;
        puntoReaparicion.SetParent(estacion.transform, false);
        puntoReaparicion.localPosition = new Vector3(0f, 0.2f, 0f);

        PuntoControlZona2D checkpoint = estacion.AddComponent<PuntoControlZona2D>();
        Set(checkpoint, "idZona", "galeria-valvulas");
        Set(checkpoint, "idPunto", "estacion-valvulas");
        Set(checkpoint, "nombreEstacion", "Estacion de Valvulas");
        Set(checkpoint, "zona", zona);
        Set(checkpoint, "puntoReaparicion", puntoReaparicion);
    }

    private static void CrearPlataforma(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 tamano,
        int layer)
    {
        GameObject plataforma = CrearDecoracion(padre, nombre, sprite, posicion, Vector2.one, 0);
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
        limite.layer = layer;
        BoxCollider2D colisionador = limite.AddComponent<BoxCollider2D>();
        colisionador.size = tamano;
    }

    private static GameObject CrearFondo(Transform padre, Sprite sprite)
    {
        GameObject fondo = CrearDecoracion(padre, "Fondo - Tuberias industriales", sprite,
            new Vector2(46f, 3.5f), Vector2.one, -5);
        SpriteRenderer renderer = fondo.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = new Vector2(96f, 16f);
        renderer.color = new Color(0.22f, 0.32f, 0.32f, 0.45f);
        return fondo;
    }

    private static Transform CrearMarcador(Transform padre, string nombre, Vector2 posicion)
    {
        GameObject marcador = new GameObject(nombre);
        marcador.transform.SetParent(padre, false);
        marcador.transform.position = posicion;
        return marcador.transform;
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
        Set(etiqueta, "ancho", Mathf.Clamp(texto.Length * 10f, 160f, 280f));
    }

    private static void CrearPuntoDeInteres(
        Transform padre,
        string nombre,
        Vector2 posicion,
        string texto)
    {
        Sprite tuberia = AssetDatabase.LoadAssetAtPath<Sprite>(CarpetaArte + "/TuberiaCobre.png");
        CrearSenal(padre, nombre, tuberia, posicion, texto, new Color(0.88f, 0.93f, 0.78f));
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
        Set(objeto, "cantidad", cantidad);
    }

    private static GameObject CrearEnemigo(
        Transform padre,
        string nombre,
        string rutaSprite,
        string rutaControlador,
        Vector2 posicion,
        int vida,
        int layer,
        float limiteIzq,
        float limiteDer)
    {
        GameObject enemigo = new GameObject(nombre);
        enemigo.transform.SetParent(padre, false);
        enemigo.transform.position = posicion;
        enemigo.layer = layer;

        SpriteRenderer renderer = enemigo.AddComponent<SpriteRenderer>();
        renderer.sprite = CargarSprite(rutaSprite);
        renderer.sortingOrder = 2;

        Animator animator = enemigo.AddComponent<Animator>();
        RuntimeAnimatorController controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(rutaControlador);
        if (controlador != null)
            animator.runtimeAnimatorController = controlador;

        Rigidbody2D rb = enemigo.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;

        BoxCollider2D colisionador = enemigo.AddComponent<BoxCollider2D>();
        colisionador.size = new Vector2(0.9f, 1.1f);

        EnemigoIA2D ia = enemigo.AddComponent<EnemigoIA2D>();
        Set(ia, "vidaMaxima", vida);
        Set(ia, "velocidadPatrulla", 2f);
        Set(ia, "velocidadPersecucion", 3.6f);
        Set(ia, "limiteIzquierdoX", limiteIzq);
        Set(ia, "limiteDerechoX", limiteDer);

        return enemigo;
    }

    private static GameObject CrearJugador(int groundLayer, int enemyLayer)
    {
        GameObject jugador = new GameObject("Explorador");
        SpriteRenderer renderer = jugador.AddComponent<SpriteRenderer>();
        renderer.sprite = CargarSprite(RutaJugador);
        renderer.sortingOrder = 2;

        Animator animator = jugador.AddComponent<Animator>();
        RuntimeAnimatorController controlador = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(RutaControladorJugador);
        if (controlador != null)
            animator.runtimeAnimatorController = controlador;

        Rigidbody2D rb = jugador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.2f;
        rb.freezeRotation = true;

        BoxCollider2D colisionador = jugador.AddComponent<BoxCollider2D>();
        colisionador.size = new Vector2(0.6f, 1.25f);

        Transform comprobador = new GameObject("ComprobadorSuelo").transform;
        comprobador.SetParent(jugador.transform, false);
        comprobador.localPosition = new Vector3(0f, -0.65f, 0f);

        Jugador2D compJugador = jugador.AddComponent<Jugador2D>();
        compJugador.accionesEntrada = AssetDatabase.LoadAssetAtPath<InputActionAsset>(
            "Assets/InputSystem_Actions.inputactions");
        compJugador.comprobadorSuelo = comprobador;
        compJugador.capaSuelo = 1 << groundLayer;
        compJugador.capaEnemigos = 1 << enemyLayer;
        Set(compJugador, "dashDesbloqueado", true); // En Galeria de Valvulas el Dash ya fue obtenido

        jugador.AddComponent<VidaJugador2D>();
        jugador.AddComponent<InventarioJugador2D>();

        return jugador;
    }

    private static Camera CrearCamara(ZonaMetroidvania2D zona, Transform objetivo)
    {
        GameObject objetoCamara = new GameObject("Camara Principal");
        Camera camara = objetoCamara.AddComponent<Camera>();
        camara.orthographic = true;
        camara.orthographicSize = 5f;
        objetoCamara.tag = "MainCamera";

        CamaraZona2D seguidor = objetoCamara.AddComponent<CamaraZona2D>();
        seguidor.Configurar(zona,objetivo);
       // seguidor.AsignarObjetivo(objetivo);
        return camara;
    }

    private static InterfazZonaMetroidvania2D CrearInterfaz(Transform padre)
    {
        GameObject objetoUI = new GameObject("Interfaz Zona");
        objetoUI.transform.SetParent(padre, false);
        Canvas canvas = objetoUI.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        objetoUI.AddComponent<CanvasScaler>();
        objetoUI.AddComponent<GraphicRaycaster>();
        return objetoUI.AddComponent<InterfazZonaMetroidvania2D>();
    }

    private static GameObject CrearDecoracion(
        Transform padre,
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 escala,
        int sortingOrder)
    {
        GameObject objeto = new GameObject(nombre);
        objeto.transform.SetParent(padre, false);
        objeto.transform.position = posicion;
        objeto.transform.localScale = new Vector3(escala.x, escala.y, 1f);

        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = sortingOrder;
        return objeto;
    }

    private static GameObject CrearGrupo(Transform padre, string nombre)
    {
        GameObject grupo = new GameObject(nombre);
        grupo.transform.SetParent(padre, false);
        return grupo;
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
                if (nombre.Contains("Vapor"))
                {
                    float dx = (x - 16) / 12f;
                    float dy = y / 28f;
                    float dist = dx * dx + (dy - 0.5f) * (dy - 0.5f);
                    pixel = dist < 0.25f ? Color.Lerp(marca, fondo, dist * 3f) : transparente;
                }
                else if (nombre.Contains("Compuerta"))
                {
                    bool borde = x <= 2 || x >= 29 || y <= 2 || y >= 29;
                    bool remaches = (x == 6 || x == 25) && (y % 6 == 0);
                    pixel = borde ? marca : (remaches ? marca : fondo);
                }
                else if (nombre.Contains("Tuberia"))
                {
                    bool brillo = y >= 20 && y <= 23;
                    bool junta = x <= 2 || x >= 29;
                    pixel = junta ? marca : (brillo ? marca : fondo);
                }
                else
                {
                    bool borde = y > 24 || x % 8 == 0 || y % 8 == 0;
                    pixel = borde ? marca : fondo;
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
            throw new InvalidOperationException("No se pudo configurar el importador para " + ruta);

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

    private static Sprite CargarSprite(string ruta)
    {
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
        if (sprite != null)
            return sprite;

        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(ruta).OfType<Sprite>().ToArray();
        if (sprites.Length > 0)
            return sprites[0];

        throw new InvalidOperationException("No se encontro un Sprite valido en " + ruta);
    }

    private static void AnadirEscenaABuildSettings()
    {
        List<EditorBuildSettingsScene> escenas = EditorBuildSettings.scenes.ToList();
        int indice = escenas.FindIndex(e => e.path == RutaEscena);
        if (indice >= 0)
            escenas[indice] = new EditorBuildSettingsScene(RutaEscena, true);
        else
            escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));

        EditorBuildSettings.scenes = escenas.ToArray();
    }

    private static void ValidarRecursos()
    {
        if (AssetDatabase.LoadAssetAtPath<InputActionAsset>("Assets/InputSystem_Actions.inputactions") == null)
            throw new InvalidOperationException("Falta Assets/InputSystem_Actions.inputactions.");
    }

    private static void Set(object objetivo, string campo, object valor)
    {
        var field = objetivo.GetType().GetField(campo,
            System.Reflection.BindingFlags.Instance |
            System.Reflection.BindingFlags.Public |
            System.Reflection.BindingFlags.NonPublic);
        if (field != null)
            field.SetValue(objetivo, valor);
    }
}
