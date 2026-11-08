using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class CrearDemoEnemigosPrototipo2D
{
    private const string RutaEscena = "Assets/Scenes/EnemigosPrototipoDemo.unity";
    private const string CarpetaPrefabs = "Assets/Demo/Prefabs/Enemigos";
    private const string RutaTexturaSuelo = "Assets/Demo/Art/EnemigosDemoSueloTexture.asset";
    private const string RutaSpriteSuelo = "Assets/Demo/Art/EnemigosDemoSuelo.asset";
    private const string RutaAcciones = "Assets/InputSystem_Actions.inputactions";
    private const string CapaSuelo = "Ground";
    private const string CapaEnemigo = "Enemy";

    private enum TipoEnemigo
    {
        Terrestre,
        Tirador,
        Rondador,
        Volador,
        Resonador
    }

    [MenuItem("Demo/Generar demo de cinco enemigos")]
    public static void Generar()
    {
        CrearCarpetas();
        int capaSuelo = ObtenerCapa(CapaSuelo);
        int capaEnemigo = ObtenerCapa(CapaEnemigo);
        Sprite spriteSuelo = CrearSpriteSuelo();
        Sprite spriteEnemigo1 = PrimerSprite("Assets/Sprites/Enemies/Enemy 1..png");
        Sprite spriteEnemigo2 = PrimerSprite("Assets/Sprites/Enemies/Enemy2.png");
        Sprite spriteJugador = PrimerSprite("Assets/Sprites/Player/Movement/PlayerMovement.png");
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaAcciones);

        if (spriteEnemigo1 == null || spriteEnemigo2 == null || spriteJugador == null || acciones == null)
            throw new InvalidOperationException("Faltan sprites provisionales o el InputActionAsset del proyecto.");

        GameObject[] prefabs = CrearPrefabs(capaEnemigo, spriteEnemigo1, spriteEnemigo2);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        CrearEscena(spriteSuelo, spriteJugador, prefabs, acciones, capaSuelo, capaEnemigo);
        AgregarABuildSettings();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        ValidarContenidoEscena();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena);
        Debug.Log("Demo de los cinco arquetipos creada en " + RutaEscena);
    }

    [MenuItem("Demo/Validar demo de cinco enemigos")]
    public static void Validar()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RutaEscena) == null)
            throw new FileNotFoundException("Genera primero la escena de demostracion.", RutaEscena);

        EditorSceneManager.OpenScene(RutaEscena, OpenSceneMode.Single);
        ValidarContenidoEscena();
        Debug.Log("Validacion correcta: jugador y cinco arquetipos con sus componentes y rutas.");
    }

    private static GameObject[] CrearPrefabs(int capaEnemigo, Sprite spriteUno, Sprite spriteDos)
    {
        return new[]
        {
            CrearPrefab(TipoEnemigo.Terrestre, "Terrestre - Perseguidor", spriteUno,
                new Color(0.94f, 0.52f, 0.34f), capaEnemigo),
            CrearPrefab(TipoEnemigo.Tirador, "Tirador - Vigia", spriteDos,
                new Color(1f, 0.78f, 0.3f), capaEnemigo),
            CrearPrefab(TipoEnemigo.Rondador, "Rondador - Guardaplataforma", spriteUno,
                new Color(0.46f, 0.83f, 0.67f), capaEnemigo),
            CrearPrefab(TipoEnemigo.Volador, "Volador - Picador", spriteDos,
                new Color(0.53f, 0.7f, 1f), capaEnemigo),
            CrearPrefab(TipoEnemigo.Resonador, "Resonador - Saltador", spriteUno,
                new Color(0.8f, 0.59f, 0.98f), capaEnemigo)
        };
    }

    private static GameObject CrearPrefab(
        TipoEnemigo tipo,
        string nombre,
        Sprite sprite,
        Color tinte,
        int capaEnemigo)
    {
        string ruta = CarpetaPrefabs + "/" + nombre.Replace(" ", "") + ".prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ruta);
        if (prefab != null)
            AssetDatabase.DeleteAsset(ruta);

        GameObject enemigo = new GameObject(nombre);
        enemigo.SetActive(false);
        enemigo.layer = capaEnemigo;

        SpriteRenderer renderer = enemigo.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = tinte;
        renderer.sortingOrder = 2;

        Rigidbody2D rb = enemigo.AddComponent<Rigidbody2D>();
        rb.gravityScale = tipo == TipoEnemigo.Volador || tipo == TipoEnemigo.Rondador ? 0f : 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        CapsuleCollider2D colisionador = enemigo.AddComponent<CapsuleCollider2D>();
        colisionador.size = new Vector2(0.8f, 0.85f);
        colisionador.isTrigger = tipo == TipoEnemigo.Rondador || tipo == TipoEnemigo.Volador;

        EnemigoPrototipo2D controlador = (EnemigoPrototipo2D)enemigo.AddComponent(TipoComponente(tipo));
        AjustarParametros(controlador, tipo);
        if (tipo == TipoEnemigo.Rondador)
            CrearRuta(enemigo, controlador);

        enemigo.SetActive(true);
        GameObject resultado = PrefabUtility.SaveAsPrefabAsset(enemigo, ruta);
        UnityEngine.Object.DestroyImmediate(enemigo);
        if (resultado == null)
            throw new IOException("No se pudo guardar el prefab " + ruta);

        return resultado;
    }

    private static Type TipoComponente(TipoEnemigo tipo)
    {
        switch (tipo)
        {
            case TipoEnemigo.Terrestre:
                return typeof(EnemigoTerrestrePrototipo2D);
            case TipoEnemigo.Tirador:
                return typeof(EnemigoTiradorPrototipo2D);
            case TipoEnemigo.Rondador:
                return typeof(EnemigoRondaPlataformas2D);
            case TipoEnemigo.Volador:
                return typeof(EnemigoVoladorPrototipo2D);
            case TipoEnemigo.Resonador:
                return typeof(EnemigoResonadorPrototipo2D);
            default:
                throw new ArgumentOutOfRangeException(nameof(tipo), tipo, null);
        }
    }

    private static void AjustarParametros(EnemigoPrototipo2D enemigo, TipoEnemigo tipo)
    {
        SerializedObject datos = new SerializedObject(enemigo);
        Establecer(datos, "rangoDeteccion", 8f);
        Establecer(datos, "velocidad", tipo == TipoEnemigo.Rondador ? 2.4f : 2.1f);
        Establecer(datos, "vidaMaxima", tipo == TipoEnemigo.Resonador ? 3 : 2);
        Establecer(datos, "anticipacionAtaque", tipo == TipoEnemigo.Volador ? 0.9f : 0.65f);
        Establecer(datos, "duracionAtaque", 0.24f);
        Establecer(datos, "recuperacionAtaque", 0.85f);
        Establecer(datos, "tiempoEntreAtaques", 1.8f);
        Establecer(datos, "velocidadProyectil", 3.2f);

        switch (tipo)
        {
            case TipoEnemigo.Terrestre:
                Establecer(datos, "rangoAtaque", 1.35f);
                break;
            case TipoEnemigo.Tirador:
                Establecer(datos, "rangoAtaque", 6f);
                Establecer(datos, "distanciaPreferida", 4f);
                Establecer(datos, "velocidadProyectil", 3.2f);
                break;
            case TipoEnemigo.Rondador:
                Establecer(datos, "rangoAtaque", 1.5f);
                Establecer(datos, "distanciaLlegada", 0.16f);
                break;
            case TipoEnemigo.Volador:
                Establecer(datos, "rangoAtaque", 6.5f);
                Establecer(datos, "duracionAtaque", 0.9f);
                Establecer(datos, "velocidadPicado", 8.5f);
                Establecer(datos, "radioImpacto", 0.8f);
                break;
            case TipoEnemigo.Resonador:
                Establecer(datos, "rangoAtaque", 1.5f);
                Establecer(datos, "alcanceOnda", 5f);
                Establecer(datos, "capaSuelo", (LayerMask)(1 << LayerMask.NameToLayer(CapaSuelo)));
                break;
        }

        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CrearRuta(GameObject enemigo, EnemigoPrototipo2D controlador)
    {
        Vector2[] posiciones =
        {
            new Vector2(-2.4f, 1.15f),
            new Vector2(2.4f, 1.15f),
            new Vector2(2.4f, -1.15f),
            new Vector2(-2.4f, -1.15f)
        };

        Transform[] ruta = new Transform[posiciones.Length];
        for (int i = 0; i < posiciones.Length; i++)
        {
            Transform punto = new GameObject("Punto de ruta " + (i + 1)).transform;
            punto.SetParent(enemigo.transform, false);
            punto.localPosition = posiciones[i];
            ruta[i] = punto;
        }

        SerializedObject datos = new SerializedObject(controlador);
        SerializedProperty puntos = datos.FindProperty("puntosRuta");
        puntos.arraySize = ruta.Length;
        for (int i = 0; i < ruta.Length; i++)
            puntos.GetArrayElementAtIndex(i).objectReferenceValue = ruta[i];
        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CrearEscena(
        Sprite suelo,
        Sprite spriteJugador,
        GameObject[] prefabs,
        InputActionAsset acciones,
        int capaSuelo,
        int capaEnemigo)
    {
        Scene escena = SceneManager.GetActiveScene();
        Camera camara = new GameObject("Camara de la arena").AddComponent<Camera>();
        camara.tag = "MainCamera";
        camara.orthographic = true;
        camara.orthographicSize = 5.5f;
        camara.backgroundColor = new Color(0.035f, 0.055f, 0.09f);
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.transform.position = new Vector3(0f, 0f, -10f);
        camara.gameObject.AddComponent<AudioListener>();
        DemoCamara2D seguimiento = camara.gameObject.AddComponent<DemoCamara2D>();
        Configurar(seguimiento, "limiteX", new Vector2(-5f, 34f));

        CrearPlataforma("Suelo de la arena", suelo, new Vector2(14f, -3.25f),
            new Vector2(42f, 2f), capaSuelo);
        CrearPlataforma("Plataforma del rondador", suelo, new Vector2(14f, -0.6f),
            new Vector2(4f, 0.45f), capaSuelo);

        GameObject jugador = CrearJugador(spriteJugador, acciones, capaSuelo, capaEnemigo);
        seguimiento.FijarObjetivo(jugador.transform);
        CrearInstancia(prefabs[0], new Vector3(0f, -1.45f));
        CrearInstancia(prefabs[1], new Vector3(6f, -1.45f));
        CrearInstancia(prefabs[2], new Vector3(14f, -0.6f));
        CrearInstancia(prefabs[3], new Vector3(20f, 0.5f));
        CrearInstancia(prefabs[4], new Vector3(27f, -1.45f));
        new GameObject("Panel de estados").AddComponent<EnemigosPrototipoHUD2D>();

        EditorSceneManager.MarkSceneDirty(escena);
        if (!EditorSceneManager.SaveScene(escena, RutaEscena))
            throw new IOException("No se pudo guardar " + RutaEscena);
    }

    private static GameObject CrearJugador(
        Sprite sprite,
        InputActionAsset acciones,
        int capaSuelo,
        int capaEnemigo)
    {
        GameObject jugador = new GameObject("Sira - jugadora de prueba");
        jugador.transform.position = new Vector3(-5f, -1.48f, 0f);
        SpriteRenderer renderer = jugador.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 3;

        Rigidbody2D rb = jugador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        jugador.AddComponent<CapsuleCollider2D>().size = new Vector2(0.72f, 1.45f);

        Transform comprobador = new GameObject("Comprobador de suelo").transform;
        comprobador.SetParent(jugador.transform, false);
        comprobador.localPosition = new Vector3(0f, -0.76f, 0f);

        Jugador2D controlador = jugador.AddComponent<Jugador2D>();
        controlador.accionesEntrada = acciones;
        controlador.comprobadorSuelo = comprobador;
        controlador.capaSuelo = 1 << capaSuelo;
        controlador.capaEnemigos = 1 << capaEnemigo;
        jugador.AddComponent<VidaJugador2D>();
        return jugador;
    }

    private static void CrearInstancia(GameObject prefab, Vector3 posicion)
    {
        GameObject instancia = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
        if (instancia == null)
            throw new InvalidOperationException("No se pudo instanciar el prefab " + prefab.name);

        instancia.transform.position = posicion;
    }

    private static void CrearPlataforma(
        string nombre,
        Sprite sprite,
        Vector2 posicion,
        Vector2 tamano,
        int capa)
    {
        GameObject plataforma = new GameObject(nombre);
        plataforma.layer = capa;
        plataforma.transform.position = posicion;
        SpriteRenderer renderer = plataforma.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.size = tamano;
        renderer.sortingOrder = 0;
        plataforma.AddComponent<BoxCollider2D>().size = tamano;
    }

    private static Sprite CrearSpriteSuelo()
    {
        Sprite existente = AssetDatabase.LoadAssetAtPath<Sprite>(RutaSpriteSuelo);
        if (existente != null)
            return existente;

        const int dimension = 16;
        Texture2D textura = new Texture2D(dimension, dimension, TextureFormat.RGBA32, false)
        {
            name = "EnemigosDemoSueloTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat
        };

        Color32[] pixeles = new Color32[dimension * dimension];
        for (int y = 0; y < dimension; y++)
        {
            for (int x = 0; x < dimension; x++)
            {
                bool borde = y == dimension - 1;
                bool detalle = (x * 7 + y * 11) % 17 == 0;
                pixeles[y * dimension + x] = borde
                    ? new Color32(105, 163, 145, 255)
                    : detalle ? new Color32(38, 63, 72, 255) : new Color32(49, 79, 84, 255);
            }
        }

        textura.SetPixels32(pixeles);
        textura.Apply(false, false);
        AssetDatabase.CreateAsset(textura, RutaTexturaSuelo);
        Sprite sprite = Sprite.Create(
            textura,
            new Rect(0f, 0f, dimension, dimension),
            new Vector2(0.5f, 0.5f),
            16f,
            0,
            SpriteMeshType.FullRect
        );
        sprite.name = "EnemigosDemoSuelo";
        AssetDatabase.CreateAsset(sprite, RutaSpriteSuelo);
        return sprite;
    }

    private static Sprite PrimerSprite(string ruta)
    {
        return AssetDatabase.LoadAllAssetRepresentationsAtPath(ruta)
            .OfType<Sprite>()
            .OrderBy(sprite => sprite.name, StringComparer.Ordinal)
            .FirstOrDefault();
    }

    private static int ObtenerCapa(string nombre)
    {
        int capa = LayerMask.NameToLayer(nombre);
        if (capa < 0)
            throw new InvalidOperationException("Falta la capa '" + nombre + "'. Genera primero la demo principal.");
        return capa;
    }

    private static void Establecer(SerializedObject datos, string nombre, float valor)
    {
        SerializedProperty propiedad = Requerir(datos, nombre);
        propiedad.floatValue = valor;
    }

    private static void Establecer(SerializedObject datos, string nombre, int valor)
    {
        SerializedProperty propiedad = Requerir(datos, nombre);
        propiedad.intValue = valor;
    }

    private static void Establecer(SerializedObject datos, string nombre, LayerMask valor)
    {
        SerializedProperty propiedad = Requerir(datos, nombre);
        propiedad.intValue = valor.value;
    }

    private static SerializedProperty Requerir(SerializedObject datos, string nombre)
    {
        SerializedProperty propiedad = datos.FindProperty(nombre);
        if (propiedad == null)
            throw new InvalidOperationException("No se encontro el parametro serializado '" + nombre + "'.");
        return propiedad;
    }

    private static void Configurar(UnityEngine.Object objetivo, string nombre, Vector2 valor)
    {
        SerializedObject datos = new SerializedObject(objetivo);
        Requerir(datos, nombre).vector2Value = valor;
        datos.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ValidarContenidoEscena()
    {
        Exigir(Unidad<Jugador2D>() != null, "Falta el controlador del jugador.");
        Exigir(Unidad<VidaJugador2D>() != null, "Falta VidaJugador2D.");
        Exigir(Unidad<EnemigoTerrestrePrototipo2D>() != null, "Falta el terrestre.");
        Exigir(Unidad<EnemigoTiradorPrototipo2D>() != null, "Falta el tirador.");

        EnemigoRondaPlataformas2D rondador = Unidad<EnemigoRondaPlataformas2D>();
        Exigir(rondador != null, "Falta el rondador de plataformas.");
        Exigir(rondador.GetComponent<Rigidbody2D>() != null &&
               rondador.GetComponent<Collider2D>() != null, "El rondador necesita fisica y collider.");
        SerializedObject datosRondador = new SerializedObject(rondador);
        SerializedProperty ruta = datosRondador.FindProperty("puntosRuta");
        Exigir(ruta != null && ruta.arraySize >= 4, "El rondador necesita cuatro puntos de ruta.");

        Exigir(Unidad<EnemigoVoladorPrototipo2D>() != null, "Falta el volador.");
        Exigir(Unidad<EnemigoResonadorPrototipo2D>() != null, "Falta el resonador.");
        Exigir(UnityEngine.Object.FindObjectsByType<EnemigoPrototipo2D>(FindObjectsSortMode.None).Length == 5,
            "La escena debe incluir exactamente cinco arquetipos.");
    }

    private static T Unidad<T>() where T : Component
    {
        return UnityEngine.Object.FindFirstObjectByType<T>();
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void AgregarABuildSettings()
    {
        List<EditorBuildSettingsScene> escenas = EditorBuildSettings.scenes.ToList();
        EditorBuildSettingsScene demo = escenas.FirstOrDefault(escena => escena.path == RutaEscena);
        if (demo == null)
            escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
        else
            demo.enabled = true;
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    private static void CrearCarpetas()
    {
        CrearCarpeta("Assets", "Demo");
        CrearCarpeta("Assets/Demo", "Art");
        CrearCarpeta("Assets/Demo", "Prefabs");
        CrearCarpeta("Assets/Demo/Prefabs", "Enemigos");
        CrearCarpeta("Assets", "Scenes");
    }

    private static void CrearCarpeta(string padre, string nombre)
    {
        string ruta = padre + "/" + nombre;
        if (!AssetDatabase.IsValidFolder(ruta))
            AssetDatabase.CreateFolder(padre, nombre);
    }
}
