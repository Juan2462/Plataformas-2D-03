using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public static class CrearAcueductoNiebla2D
{
    public const string RutaEscena = "Assets/Scenes/AcueductoNiebla.unity";
    private const string CarpetaArte = "Assets/Demo/AcueductoNiebla";
    private const string RutaJugador = "Assets/Sprites/Player/Movement/PlayerMovement.png";
    private const string RutaControladorJugador = "Assets/Animation/AnimatorControllers/Player/Player AC.controller";

    [MenuItem("Demo/Generar zona: Acueducto de la Niebla")]
    public static void Generar()
    {
        Directory.CreateDirectory(CarpetaArte);

        Sprite suelo = CrearSprite("AcueductoPiedra", new Color32(40, 48, 56, 255), new Color32(75, 90, 105, 255));
        Sprite pasarela = CrearSprite("PasarelaNiebla", new Color32(60, 75, 85, 255), new Color32(110, 130, 145, 255));

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Scene escena = SceneManager.GetActiveScene();
        escena.name = "Acueducto de la Niebla";

        int groundLayer = LayerMask.NameToLayer("Ground");
        int enemyLayer = LayerMask.NameToLayer("Enemy");

        GameObject raiz = new GameObject("Zona - Acueducto de la Niebla");
        ZonaMetroidvania2D zona = raiz.AddComponent<ZonaMetroidvania2D>();
        Set(zona, "idZona", "acueducto-niebla");
        Set(zona, "nombreZona", "Acueducto de la Niebla");
        Set(zona, "limitesX", new Vector2(0f, 60f));
        Set(zona, "limitesY", new Vector2(-6f, 12f));

        GameObject geometria = new GameObject("Geometria");
        geometria.transform.SetParent(raiz.transform);
        GameObject puntos = new GameObject("Puntos de Interes");
        puntos.transform.SetParent(raiz.transform);

        // Geometría del Acueducto
        CrearPlataforma(geometria.transform, "Entrada Oeste (Galeria)", suelo, new Vector2(6f, -3f), new Vector2(12f, 1.2f), groundLayer);
        CrearPlataforma(geometria.transform, "Puente Central 01", pasarela, new Vector2(20f, -1.5f), new Vector2(8f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Puente Central 02 (Elevado)", pasarela, new Vector2(34f, 1.0f), new Vector2(8f, 0.4f), groundLayer);
        CrearPlataforma(geometria.transform, "Salida Este (Canal)", suelo, new Vector2(50f, -3f), new Vector2(14f, 1.2f), groundLayer);

        // Marcadores de transición
        Transform entradaGaleria = CrearMarcador(geometria.transform, "Entrada desde Galeria", new Vector2(2f, -1.8f));
        PuntoEntradaZona2D compEntrada = entradaGaleria.gameObject.AddComponent<PuntoEntradaZona2D>();
        Set(compEntrada, "idEntrada", "entrada-desde-galeria");
        Set(zona, "puntoEntrada", entradaGaleria);

        Transform salidaCanal = CrearMarcador(geometria.transform, "Conexion a Canal", new Vector2(55f, -1.8f));
        Set(zona, "conexionSiguiente", salidaCanal);

        // Conexiones de zona interactivas
        CrearConexion(puntos.transform, "Regreso a Galeria", entradaGaleria.position, "Assets/Scenes/GaleriaValvulas.unity", "retorno-acueducto");
        CrearConexion(puntos.transform, "Avance a Canal", salidaCanal.position, "Assets/Scenes/CanalCampanas.unity", "entrada-acueducto");

        // Jugador y Cámara
        GameObject jugador = CrearJugador(groundLayer, enemyLayer);
        jugador.transform.position = entradaGaleria.position;

        Camera camara = new GameObject("Main Camera").AddComponent<Camera>();
        camara.tag = "MainCamera";
        camara.orthographic = true;
        camara.orthographicSize = 5.5f;
        camara.gameObject.AddComponent<CamaraZona2D>().Configurar(zona, jugador.transform);

        AnadirEscenaABuildSettings();

        EditorSceneManager.MarkSceneDirty(escena);
        EditorSceneManager.SaveScene(escena, RutaEscena);
        Debug.Log("¡Acueducto de la Niebla generado y conectado con éxito!");
    }

    private static Transform CrearMarcador(Transform padre, string nombre, Vector2 pos)
    {
        GameObject go = new GameObject(nombre);
        go.transform.SetParent(padre);
        go.transform.position = pos;
        return go.transform;
    }

    private static void CrearConexion(Transform padre, string nombre, Vector2 pos, string escenaDestino, string idEntrada)
    {
        GameObject con = new GameObject(nombre);
        con.transform.SetParent(padre);
        con.transform.position = pos;
        BoxCollider2D col = con.AddComponent<BoxCollider2D>();
        col.isTrigger = true;
        col.size = new Vector2(2f, 3f);

        ConexionZona2D comp = con.AddComponent<ConexionZona2D>();
        Set(comp, "rutaEscenaDestino", escenaDestino);
        Set(comp, "idEntradaDestino", idEntrada);
    }

    private static void CrearPlataforma(Transform padre, string nombre, Sprite sprite, Vector2 pos, Vector2 tamano, int layer)
    {
        GameObject plat = new GameObject(nombre);
        plat.transform.SetParent(padre);
        plat.transform.position = pos;
        plat.layer = layer;

        SpriteRenderer sr = plat.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.drawMode = SpriteDrawMode.Tiled;
        sr.size = tamano;

        BoxCollider2D col = plat.AddComponent<BoxCollider2D>();
        col.size = tamano;
    }

    private static GameObject CrearJugador(int groundLayer, int enemyLayer)
    {
        GameObject jugador = new GameObject("Explorador");
        SpriteRenderer sr = jugador.AddComponent<SpriteRenderer>();
        sr.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(RutaJugador);
        Rigidbody2D rb = jugador.AddComponent<Rigidbody2D>();
        rb.gravityScale = 2.5f;
        rb.freezeRotation = true;
        jugador.AddComponent<BoxCollider2D>().size = new Vector2(0.6f, 1.2f);
        jugador.AddComponent<Jugador2D>();
        jugador.AddComponent<VidaJugador2D>();
        jugador.AddComponent<InventarioJugador2D>();
        return jugador;
    }

    private static Sprite CrearSprite(string nombre, Color fondo, Color marca)
    {
        string ruta = CarpetaArte + "/" + nombre + ".png";
        Texture2D tex = new Texture2D(32, 32);
        for (int y = 0; y < 32; y++)
            for (int x = 0; x < 32; x++)
                tex.SetPixel(x, y, (x % 8 == 0 || y % 8 == 0) ? marca : fondo);
        tex.Apply();
        File.WriteAllBytes(ruta, tex.EncodeToPNG());
        AssetDatabase.ImportAsset(ruta);
        return AssetDatabase.LoadAssetAtPath<Sprite>(ruta);
    }

    private static void AnadirEscenaABuildSettings()
    {
        List<EditorBuildSettingsScene> escenas = EditorBuildSettings.scenes.ToList();
        if (!escenas.Any(e => e.path == RutaEscena))
        {
            escenas.Add(new EditorBuildSettingsScene(RutaEscena, true));
            EditorBuildSettings.scenes = escenas.ToArray();
        }
    }

    private static void Set(object obj, string campo, object val)
    {
        var f = obj.GetType().GetField(campo, System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        if (f != null) f.SetValue(obj, val);
    }
}