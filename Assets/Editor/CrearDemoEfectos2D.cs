using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public static class CrearDemoEfectos2D
{
    private const string ScenePath = "Assets/Scenes/EfectosDemo.unity";
    private const string PrefabFolder = "Assets/Demo/Prefabs/Efectos";
    private const string MaterialPath = "Assets/Demo/Art/MaterialParticulasDemo.mat";
    private const string SceneMaterialPath = "Assets/Demo/Art/MaterialEscenarioEfectosDemo.mat";
    private const string VolumeProfilePath = "Assets/Demo/Art/EfectosDemoVolumeProfile.asset";
    private const string JmoPrefabFolder = "Assets/JMO Assets/Cartoon FX Remaster/CFXR Prefabs";

    private static readonly string[] EffectNames =
    {
        "Tajo igneo", "Tajo glacial", "Tajo espectral", "Espiral de fuego", "Espiral glacial",
        "Espiral luminosa", "Explosion arcana", "Explosion de sombra", "Explosion de fuego", "Onda de humo",
        "Impacto rojo", "Impacto electrico", "Impacto de hielo", "Impacto de fuego", "Golpe al suelo",
        "Aura electrica", "Aliento de fuego", "Nube toxica", "Aura runica", "Rafaga magica",
        "Escape de almas", "Lluvia de estrellas", "Destello radial", "Reliquia luminosa", "Luz ambiental",
        "Escudo de hojas", "Impacto de hojas", "Ondas de agua", "Salpicadura", "Burbujas arcanas"
    };

    private static readonly string[] EffectSourcePaths =
    {
        "Sword Trails/Fire/CFXR4 Sword Hit FIRE (Cross).prefab",
        "Sword Trails/Ice/CFXR4 Sword Hit ICE (Cross).prefab",
        "Sword Trails/Plain/CFXR4 Sword Hit PLAIN (Cross).prefab",
        "Sword Trails/Fire/CFXR4 Sword Trail FIRE (360 Spiral).prefab",
        "Sword Trails/Ice/CFXR4 Sword Trail ICE (360 Spiral).prefab",
        "Sword Trails/Plain/CFXR4 Sword Trail PLAIN (360 Thin Spiral).prefab",
        "Explosions/CFXR Explosion 1.prefab",
        "Explosions/CFXR2 WW Explosion.prefab",
        "Explosions/CFXR3 Fire Explosion B.prefab",
        "Explosions/CFXR Explosion Smoke 2 Solo (HDR).prefab",
        "Impacts/CFXR Hit A (Red).prefab",
        "Electric/CFXR3 Hit Electric C (Air).prefab",
        "Ice/CFXR3 Hit Ice B (Air).prefab",
        "Fire/CFXR3 Hit Fire B (Air).prefab",
        "Impacts/CFXR2 Ground Hit.prefab",
        "Electric/CFXR Electrified 3.prefab",
        "Fire/CFXR Fire Breath.prefab",
        "Misc/CFXR2 Poison Cloud.prefab",
        "Magic Misc/CFXR3 Magic Aura A (Runic).prefab",
        "Misc/CFXR Magic Poof.prefab",
        "Eerie/CFXR2 Souls Escape.prefab",
        "Magic Misc/CFXR4 Falling Stars.prefab",
        "Misc/CFXR Flash.prefab",
        "Misc/CFXR2 Shiny Item (Loop).prefab",
        "Light/CFXR3 LightGlow A (Loop).prefab",
        "Nature/CFXR3 Shield Leaves A (Lit).prefab",
        "Nature/CFXR3 Hit Leaves A (Lit).prefab",
        "Liquids/CFXR Water Ripples.prefab",
        "Liquids/CFXR Water Splash (Smaller).prefab",
        "Liquids/CFXR4 Bubbles Breath Underwater Loop.prefab"
    };

    private static readonly float[] EffectScales =
    {
        0.62f, 0.62f, 0.62f, 0.48f, 0.48f, 0.48f, 0.55f, 0.55f, 0.55f, 0.58f,
        0.7f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.55f, 0.62f,
        0.62f, 0.58f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f, 0.62f
    };

    private static readonly int[] ContinuousEffectIndices = { 15, 16, 18, 21, 23, 24, 25, 29 };
    private static readonly string[] PreviousGeneratedEffectNames =
    {
        "Tajo de espada", "Impacto", "Aterrizaje de polvo", "Monedas recogidas", "Curacion",
        "Destello de dash", "Portal de energia", "Escudo protector", "Llama azul", "Estallido de hielo",
        "Nube de veneno", "Magia arcana", "Pasos de polvo", "Anillo de salto", "Desvanecimiento",
        "Aparicion enemiga", "Cristal de retorno", "Llave antigua", "Explosion arcana", "Chispas electricas",
        "Burbujas", "Hojas al viento", "Aura sombria", "Estrellas fugaces", "Cristal roto",
        "Humo espectral", "Carga de espada", "Parada perfecta", "Ondas curativas", "Runa giratoria"
    };

    [MenuItem("Demo/Generar galeria de efectos")]
    public static void Generar()
    {
        CrearCarpetaSiFalta("Assets/Demo/Prefabs");
        CrearCarpetaSiFalta(PrefabFolder);
        CrearCarpetaSiFalta("Assets/Demo/Art");
        CrearCarpetaSiFalta("Assets/Scenes");

        ValidarDefiniciones();
        HashSet<string> nombresActuales = new HashSet<string>(EffectNames.Select(NombreArchivo));
        foreach (string nombreAnterior in PreviousGeneratedEffectNames)
        {
            string nombreArchivo = NombreArchivo(nombreAnterior);
            if (!nombresActuales.Contains(nombreArchivo))
                AssetDatabase.DeleteAsset(PrefabFolder + "/" + nombreArchivo + ".prefab");
        }

        GameObject[] prefabs = new GameObject[EffectNames.Length];
        for (int i = 0; i < EffectNames.Length; i++)
            prefabs[i] = CrearPrefabEfectoJmo(i);

        CrearEscena(prefabs);
        RegistrarEscena();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Validar();
        Selection.activeObject = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        Debug.Log("Galeria creada: 30 efectos URP, prefabs reutilizables y escena incluida en Build Settings.");
    }

    [MenuItem("Demo/Validar galeria de efectos")]
    public static void Validar()
    {
        ValidarDefiniciones();
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) == null)
            throw new FileNotFoundException("Genera primero la galeria de efectos.", ScenePath);

        for (int i = 0; i < EffectNames.Length; i++)
        {
            string path = PrefabFolder + "/" + NombreArchivo(EffectNames[i]) + ".prefab";
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOriginal(i));
            if (prefab == null || original == null ||
                prefab.GetComponentsInChildren<ParticleSystem>(true).Length == 0)
                throw new InvalidOperationException("Falta un efecto JMO o un prefab de demostracion valido: " + path);

            if (prefab.transform.childCount == 0 ||
                AssetDatabase.GetAssetPath(PrefabUtility.GetCorrespondingObjectFromSource(
                    prefab.transform.GetChild(0).gameObject)) != RutaPrefabOriginal(i))
                throw new InvalidOperationException("El efecto no conserva su prefab original JMO: " + path);
        }

        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        DemoGaleriaParticulas2D galeria = UnityEngine.Object.FindFirstObjectByType<DemoGaleriaParticulas2D>();
        if (galeria == null)
            throw new InvalidOperationException("La escena no contiene el controlador de la galeria.");

        Camera camara = Camera.main;
        Volume volumen = UnityEngine.Object.FindFirstObjectByType<Volume>();
        if (camara == null || !camara.GetUniversalAdditionalCameraData().renderPostProcessing ||
            volumen == null || volumen.sharedProfile == null ||
            !volumen.sharedProfile.components.OfType<Bloom>().Any(componente => componente.active) ||
            !volumen.sharedProfile.components.OfType<Vignette>().Any(componente => componente.active))
            throw new InvalidOperationException("La escena necesita Bloom y viñeta activos en URP.");

        SerializedObject datos = new SerializedObject(galeria);
        SerializedProperty nombres = datos.FindProperty("nombresEfectos");
        SerializedProperty prefabs = datos.FindProperty("prefabsEfectos");
        SerializedProperty puntos = datos.FindProperty("puntosVista");
        if (nombres == null || prefabs == null || puntos == null ||
            nombres.arraySize != EffectNames.Length ||
            prefabs.arraySize != EffectNames.Length ||
            puntos.arraySize != EffectNames.Length)
            throw new InvalidOperationException("La galeria debe tener 30 nombres, prefabs y posiciones.");

        for (int i = 0; i < EffectNames.Length; i++)
        {
            if (nombres.GetArrayElementAtIndex(i).stringValue != EffectNames[i] ||
                prefabs.GetArrayElementAtIndex(i).objectReferenceValue == null ||
                puntos.GetArrayElementAtIndex(i).objectReferenceValue == null)
                throw new InvalidOperationException("La galeria tiene una muestra sin configurar: " + EffectNames[i]);
        }

        SerializedProperty continuos = datos.FindProperty("indicesEfectosContinuos");
        if (continuos == null || continuos.arraySize != ContinuousEffectIndices.Length)
            throw new InvalidOperationException("La galeria debe marcar sus ocho efectos continuos.");
        for (int i = 0; i < ContinuousEffectIndices.Length; i++)
        {
            if (continuos.GetArrayElementAtIndex(i).intValue != ContinuousEffectIndices[i])
                throw new InvalidOperationException("La galeria no tiene configuradas sus muestras continuas.");
        }

        if (!EditorBuildSettings.scenes.Any(escena => escena.path == ScenePath && escena.enabled))
            throw new InvalidOperationException("La escena de efectos no esta habilitada en Build Settings.");

        Debug.Log("Validacion correcta: 30 prefabs de particulas, referencias de escena y Build Settings.");
    }

    private static Material CrearMaterialParticulas()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("No se encontro un shader Unlit compatible con URP.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        else
        {
            material.shader = shader;
        }

        if (material.HasProperty("_Surface"))
            material.SetFloat("_Surface", 1f);
        material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
        EditorUtility.SetDirty(material);
        return material;
    }

    private static GameObject CrearPrefabEfecto(int indice, Material material)
    {
        string path = PrefabFolder + "/" + NombreArchivo(EffectNames[indice]) + ".prefab";
        GameObject raiz = new GameObject(EffectNames[indice]);
        ParticleSystem sistema = raiz.AddComponent<ParticleSystem>();
        ConfigurarParticulas(sistema, indice);

        ParticleSystemRenderer renderer = raiz.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = 5;
        if (indice == 0 || indice == 5)
        {
            renderer.trailMaterial = material;
            renderer.maxParticleSize = 0.5f;
        }

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, path);
        UnityEngine.Object.DestroyImmediate(raiz);
        if (prefab == null)
            throw new IOException("No se pudo crear el prefab de efecto: " + path);
        return prefab;
    }

    private static void ConfigurarParticulas(ParticleSystem sistema, int indice)
    {
        ParticleSystem.MainModule main = sistema.main;
        bool continuo = EsContinuo(indice);
        main.loop = continuo;
        main.duration = continuo ? 4f : 1f;
        main.startLifetime = new ParticleSystem.MinMaxCurve(indice == 4 || indice == 28 ? 1.25f : 0.55f + (indice % 4) * 0.12f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(VelocidadMinima(indice), VelocidadMaxima(indice));
        main.startSize = new ParticleSystem.MinMaxCurve(TamanoMinimo(indice), TamanoMaximo(indice));
        main.startColor = new ParticleSystem.MinMaxGradient(ColorPrimario(indice), ColorSecundario(indice));
        main.maxParticles = 220;
        main.simulationSpace = ParticleSystemSimulationSpace.Local;
        main.gravityModifier = indice == 2 || indice == 12 || indice == 21 || indice == 24
            ? 1.2f
            : indice == 3 || indice == 20 ? 0.25f : 0f;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;

        ParticleSystem.EmissionModule emision = sistema.emission;
        emision.enabled = true;
        if (continuo)
        {
            emision.rateOverTime = indice == 29 ? 15f : indice == 22 ? 24f : 18f;
        }
        else
        {
            emision.rateOverTime = 0f;
            emision.SetBursts(new[]
            {
                new ParticleSystem.Burst(0f, (short)Cantidad(indice))
            });
        }

        ParticleSystem.ShapeModule forma = sistema.shape;
        forma.enabled = true;
        forma.shapeType = TipoForma(indice);
        forma.radius = continuo ? 0.75f : indice == 4 ? 0.16f : indice == 13 || indice == 28 ? 0.5f : 0.08f;
        forma.angle = indice == 0 ? 48f : indice == 2 ? 48f : indice == 5 ? 34f : indice % 3 == 0 ? 18f : 28f;
        forma.rotation = indice == 0 || indice == 5 || indice == 26 ? new Vector3(0f, 0f, -90f) : Vector3.zero;
        forma.arc = indice == 28 ? 180f : 360f;
        forma.randomDirectionAmount = indice == 0 || indice == 5 || indice == 26 ? 0.12f : 0.65f;

        ParticleSystem.ColorOverLifetimeModule colorVida = sistema.colorOverLifetime;
        colorVida.enabled = true;
        Gradient gradiente = new Gradient();
        gradiente.SetKeys(
            new[]
            {
                new GradientColorKey(ColorPrimario(indice), 0f),
                new GradientColorKey(ColorSecundario(indice), 0.55f),
                new GradientColorKey(ColorSecundario(indice), 1f)
            },
            new[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(0.75f, 0.55f),
                new GradientAlphaKey(0f, 1f)
            });
        colorVida.color = new ParticleSystem.MinMaxGradient(gradiente);

        ParticleSystem.SizeOverLifetimeModule tamanoVida = sistema.sizeOverLifetime;
        tamanoVida.enabled = true;
        AnimationCurve curva = new AnimationCurve(
            new Keyframe(0f, 0.25f),
            new Keyframe(0.18f, 1f),
            new Keyframe(1f, 0f));
        tamanoVida.size = new ParticleSystem.MinMaxCurve(1f, curva);

        ParticleSystem.NoiseModule ruido = sistema.noise;
        ruido.enabled = indice == 3 || indice == 4 || indice == 6 || indice == 8 ||
            indice == 10 || indice == 11 || indice == 16 || indice == 20 ||
            indice == 21 || indice == 22 || indice == 25 || indice == 28 || indice == 29;
        ruido.strength = continuo ? 0.12f : 0.28f;
        ruido.frequency = indice == 20 || indice == 28 ? 1.8f : 0.65f;

        ParticleSystem.VelocityOverLifetimeModule velocidad = sistema.velocityOverLifetime;
        velocidad.enabled = continuo || indice == 13 || indice == 18 || indice == 27 || indice == 28;
        velocidad.orbitalZ = continuo ? (indice == 29 ? 2.8f : 1.4f) : indice == 13 || indice == 28 ? 2.5f : 0f;
        velocidad.radial = continuo ? 0.2f : indice == 18 || indice == 27 ? 0.5f : 0f;

        ParticleSystem.TrailModule estelas = sistema.trails;
        estelas.enabled = indice == 0 || indice == 5 || indice == 19 || indice == 23 || indice == 26;
        estelas.mode = ParticleSystemTrailMode.PerParticle;
        estelas.ratio = 0.6f;
        estelas.lifetime = 0.18f;
        estelas.widthOverTrail = new ParticleSystem.MinMaxCurve(
            1f, new AnimationCurve(new Keyframe(0f, 0.8f), new Keyframe(1f, 0f)));

        ParticleSystemRenderer renderer = sistema.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.alignment = ParticleSystemRenderSpace.View;
        renderer.minParticleSize = 0.01f;
        renderer.maxParticleSize = indice == 2 || indice == 12 || indice == 25 ? 0.48f : 0.35f;
    }

    private static void ValidarDefiniciones()
    {
        if (EffectNames.Length != 30 || EffectSourcePaths.Length != EffectNames.Length ||
            EffectScales.Length != EffectNames.Length ||
            EffectSourcePaths.Distinct().Count() != EffectSourcePaths.Length)
            throw new InvalidOperationException("La galeria requiere 30 definiciones unicas y completas.");

        for (int i = 0; i < EffectNames.Length; i++)
        {
            GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOriginal(i));
            if (original == null)
                throw new FileNotFoundException("No se encontro el prefab Cartoon FX requerido.", RutaPrefabOriginal(i));

            ParticleSystem[] sistemas = original.GetComponentsInChildren<ParticleSystem>(true);
            if (sistemas.Length == 0)
                throw new InvalidOperationException("El prefab Cartoon FX no contiene particulas: " + RutaPrefabOriginal(i));

            bool tieneBucle = sistemas.Any(sistema => sistema.main.loop);
            if (tieneBucle != EsContinuo(i))
                throw new InvalidOperationException("El estado de reproduccion continua no coincide con " + EffectNames[i]);
        }
    }

    private static string RutaPrefabOriginal(int indice)
    {
        return JmoPrefabFolder + "/" + EffectSourcePaths[indice];
    }

    private static GameObject CrearPrefabEfectoJmo(int indice)
    {
        string path = PrefabFolder + "/" + NombreArchivo(EffectNames[indice]) + ".prefab";
        GameObject original = AssetDatabase.LoadAssetAtPath<GameObject>(RutaPrefabOriginal(indice));
        if (original == null)
            throw new FileNotFoundException("No se encontro el prefab Cartoon FX requerido.", RutaPrefabOriginal(indice));

        GameObject raiz = new GameObject(EffectNames[indice]);
        GameObject instanciaOriginal = PrefabUtility.InstantiatePrefab(original, raiz.transform) as GameObject;
        if (instanciaOriginal == null)
        {
            UnityEngine.Object.DestroyImmediate(raiz);
            throw new InvalidOperationException("No se pudo anidar el efecto JMO: " + RutaPrefabOriginal(indice));
        }

        instanciaOriginal.transform.localPosition = Vector3.zero;
        raiz.transform.localScale = Vector3.one * EffectScales[indice];
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(raiz, path);
        UnityEngine.Object.DestroyImmediate(raiz);
        if (prefab == null)
            throw new IOException("No se pudo crear el prefab de demostracion: " + path);
        return prefab;
    }

    private static void CrearEscena(GameObject[] prefabs)
    {
        Scene escena = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Camera camara = new GameObject("Camara de efectos").AddComponent<Camera>();
        camara.tag = "MainCamera";
        camara.orthographic = true;
        camara.orthographicSize = 8.5f;
        camara.backgroundColor = new Color(0.025f, 0.035f, 0.075f);
        camara.clearFlags = CameraClearFlags.SolidColor;
        camara.transform.position = new Vector3(0f, 0f, -10f);
        UniversalAdditionalCameraData datosCamara = camara.GetUniversalAdditionalCameraData();
        datosCamara.renderPostProcessing = true;
        camara.gameObject.AddComponent<AudioListener>();
        CrearAcabadoVisual(camara);

        Material materialEscena = CrearMaterialEscena();
        CrearPlaca("Fondo", new Vector3(0f, 0f, 2f), new Vector3(40f, 20f, 1f),
            new Color(0.055f, 0.075f, 0.13f), materialEscena);
        CrearPlaca("Suelo del taller", new Vector3(0f, -7.3f, 1f), new Vector3(40f, 1.3f, 1f),
            new Color(0.12f, 0.16f, 0.23f), materialEscena);
        CrearPlaca("Franja superior", new Vector3(0f, 7.7f, 1f), new Vector3(40f, 0.08f, 1f),
            new Color(0.2f, 0.65f, 0.85f), materialEscena);

        Transform[] puntos = new Transform[EffectNames.Length];
        for (int i = 0; i < EffectNames.Length; i++)
        {
            int columna = i % 5;
            int fila = i / 5;
            float anchoVista = camara.orthographicSize * camara.aspect;
            Vector3 posicion = new Vector3(
                Mathf.Lerp(-anchoVista * 0.78f, anchoVista * 0.78f, columna / 4f),
                5.25f - fila * 2.1f,
                0f);
            GameObject punto = new GameObject("Muestra " + EffectNames[i]);
            punto.transform.position = posicion;
            SceneManager.MoveGameObjectToScene(punto, escena);
            puntos[i] = punto.transform;

            CrearPlaca("Panel " + EffectNames[i],
                new Vector3(posicion.x, posicion.y - 0.4f, 1.4f),
                new Vector3(anchoVista * 0.31f, 1.35f, 0.3f),
                new Color(0.07f, 0.1f, 0.17f), materialEscena);
            CrearPlaca("Pedestal " + EffectNames[i],
                new Vector3(posicion.x, posicion.y - 1.02f, 0.8f),
                new Vector3(anchoVista * 0.32f, 0.12f, 0.3f),
                ColorPrimario(i) * 0.45f, materialEscena);
        }

        DemoGaleriaParticulas2D galeria =
            new GameObject("Control de la galeria").AddComponent<DemoGaleriaParticulas2D>();
        ConfigurarArreglo(galeria, "nombresEfectos", EffectNames);
        ConfigurarArreglo(galeria, "prefabsEfectos", prefabs);
        ConfigurarArreglo(galeria, "puntosVista", puntos);
        ConfigurarArreglo(galeria, "indicesEfectosContinuos", ContinuousEffectIndices);

        if (!EditorSceneManager.SaveScene(escena, ScenePath))
            throw new IOException("No se pudo guardar la escena de efectos: " + ScenePath);
    }

    private static void CrearAcabadoVisual(Camera camara)
    {
        VolumeProfile perfil = AssetDatabase.LoadAssetAtPath<VolumeProfile>(VolumeProfilePath);
        if (perfil == null)
        {
            perfil = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(perfil, VolumeProfilePath);
        }

        for (int i = perfil.components.Count - 1; i >= 0; i--)
        {
            if (perfil.components[i] == null)
                perfil.components.RemoveAt(i);
        }

        Bloom bloom = perfil.components.OfType<Bloom>().FirstOrDefault();
        if (bloom == null)
        {
            bloom = perfil.Add<Bloom>(true);
            AssetDatabase.AddObjectToAsset(bloom, perfil);
        }
        bloom.active = true;
        bloom.threshold.Override(0.85f);
        bloom.intensity.Override(1.15f);
        bloom.scatter.Override(0.72f);

        Vignette vineta = perfil.components.OfType<Vignette>().FirstOrDefault();
        if (vineta == null)
        {
            vineta = perfil.Add<Vignette>(true);
            AssetDatabase.AddObjectToAsset(vineta, perfil);
        }
        vineta.active = true;
        vineta.intensity.Override(0.2f);
        vineta.smoothness.Override(0.78f);

        GameObject volumenObjeto = new GameObject("Bloom y acabado cinematografico");
        Volume volumen = volumenObjeto.AddComponent<Volume>();
        volumen.isGlobal = true;
        volumen.priority = 1f;
        volumen.sharedProfile = perfil;
        perfil.Reset();
        EditorUtility.SetDirty(perfil);
        EditorUtility.SetDirty(volumen);
        EditorUtility.SetDirty(camara);
    }

    private static Material CrearMaterialEscena()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Unlit");
        if (shader == null)
            throw new InvalidOperationException("No se encontro el shader Unlit de URP para el escenario.");

        Material material = AssetDatabase.LoadAssetAtPath<Material>(SceneMaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, SceneMaterialPath);
        }
        else
        {
            material.shader = shader;
        }
        return material;
    }

    private static void CrearPlaca(string nombre, Vector3 posicion, Vector3 escala, Color color, Material material)
    {
        GameObject placa = GameObject.CreatePrimitive(PrimitiveType.Quad);
        placa.name = nombre;
        placa.transform.position = posicion;
        placa.transform.localScale = escala;
        UnityEngine.Object.DestroyImmediate(placa.GetComponent<Collider>());
        MeshRenderer renderer = placa.GetComponent<MeshRenderer>();
        renderer.sharedMaterial = material;
        MaterialPropertyBlock bloque = new MaterialPropertyBlock();
        bloque.SetColor("_BaseColor", color);
        bloque.SetColor("_Color", color);
        renderer.SetPropertyBlock(bloque);
    }

    private static void RegistrarEscena()
    {
        EditorBuildSettingsScene[] escenasActuales = EditorBuildSettings.scenes;
        List<EditorBuildSettingsScene> escenas = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene("Assets/Scenes/RuinsDemo.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/InventarioDemo.unity", true),
            new EditorBuildSettingsScene("Assets/Scenes/TiendaDemo.unity", true),
            new EditorBuildSettingsScene(ScenePath, true)
        };
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path != "Assets/Scenes/RuinsDemo.unity" &&
            escena.path != "Assets/Scenes/InventarioDemo.unity" &&
            escena.path != "Assets/Scenes/TiendaDemo.unity" &&
            escena.path != ScenePath &&
            escena.path != "Assets/Scenes/SampleScene.unity"));
        escenas.AddRange(escenasActuales.Where(escena =>
            escena.path == "Assets/Scenes/SampleScene.unity"));
        EditorBuildSettings.scenes = escenas.ToArray();
    }

    private static void CrearCarpetaSiFalta(string ruta)
    {
        if (AssetDatabase.IsValidFolder(ruta))
            return;

        int separador = ruta.LastIndexOf('/');
        string padre = ruta.Substring(0, separador);
        string nombre = ruta.Substring(separador + 1);
        if (!AssetDatabase.IsValidFolder(padre))
            CrearCarpetaSiFalta(padre);
        AssetDatabase.CreateFolder(padre, nombre);
    }

    private static void ConfigurarArreglo<T>(UnityEngine.Object objetivo, string nombre, T[] valores)
        where T : UnityEngine.Object
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty arreglo = serializado.FindProperty(nombre);
        if (arreglo == null)
            throw new InvalidOperationException("No se encontro el campo serializado: " + nombre);

        arreglo.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            arreglo.GetArrayElementAtIndex(i).objectReferenceValue = valores[i];
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurarArreglo(UnityEngine.Object objetivo, string nombre, string[] valores)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty arreglo = serializado.FindProperty(nombre);
        if (arreglo == null)
            throw new InvalidOperationException("No se encontro el campo serializado: " + nombre);

        arreglo.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            arreglo.GetArrayElementAtIndex(i).stringValue = valores[i];
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void ConfigurarArreglo(UnityEngine.Object objetivo, string nombre, int[] valores)
    {
        SerializedObject serializado = new SerializedObject(objetivo);
        SerializedProperty arreglo = serializado.FindProperty(nombre);
        if (arreglo == null)
            throw new InvalidOperationException("No se encontro el campo serializado: " + nombre);

        arreglo.arraySize = valores.Length;
        for (int i = 0; i < valores.Length; i++)
            arreglo.GetArrayElementAtIndex(i).intValue = valores[i];
        serializado.ApplyModifiedPropertiesWithoutUndo();
    }

    private static string NombreArchivo(string nombre)
    {
        return string.Concat(nombre.Select(caracter => char.IsLetterOrDigit(caracter) ? caracter : '-')).Trim('-');
    }

    private static bool EsContinuo(int indice)
    {
        return ContinuousEffectIndices.Contains(indice);
    }

    private static ParticleSystemShapeType TipoForma(int indice)
    {
        if (indice == 6 || indice == 13 || indice == 16 || indice == 22 || indice == 28 || indice == 29)
            return ParticleSystemShapeType.Circle;
        if (indice == 0 || indice == 2 || indice == 5 || indice == 11 || indice == 14 ||
            indice == 15 || indice == 18 || indice == 19 || indice == 23 || indice == 26 || indice == 27)
            return ParticleSystemShapeType.Cone;
        return ParticleSystemShapeType.Sphere;
    }

    private static int Cantidad(int indice)
    {
        int[] cantidades =
        {
            26, 34, 22, 24, 28, 36, 0, 30, 42, 38,
            32, 34, 18, 30, 38, 42, 0, 26, 48, 44,
            24, 32, 0, 30, 40, 26, 35, 40, 28, 0
        };
        return cantidades[indice];
    }

    private static float VelocidadMinima(int indice)
    {
        float[] valores =
        {
            3.2f, 2.4f, 1.2f, 1.5f, 0.7f, 1.7f, 0.45f, 0.7f, 1.1f, 1.5f,
            0.3f, 1.4f, 0.6f, 1.7f, 1.2f, 0.8f, 0.4f, 0.6f, 2.2f, 2.4f,
            0.4f, 0.8f, 0.25f, 1.5f, 2.3f, 0.3f, 1.1f, 1.8f, 0.45f, 0.35f
        };
        return valores[indice];
    }

    private static float VelocidadMaxima(int indice)
    {
        float[] valores =
        {
            5.6f, 4.5f, 3.1f, 3.4f, 2f, 3.6f, 1.5f, 2.8f, 3.7f, 4.4f,
            1.4f, 3.3f, 1.4f, 3.4f, 3.2f, 2.6f, 1.5f, 2.3f, 5f, 5.5f,
            1.6f, 2.3f, 1.2f, 3.8f, 5.2f, 1.2f, 2.8f, 4.4f, 2.2f, 1.4f
        };
        return valores[indice];
    }

    private static float TamanoMinimo(int indice)
    {
        float[] valores =
        {
            0.1f, 0.08f, 0.1f, 0.09f, 0.08f, 0.09f, 0.07f, 0.08f, 0.12f, 0.08f,
            0.12f, 0.08f, 0.09f, 0.07f, 0.1f, 0.1f, 0.08f, 0.12f, 0.1f, 0.05f,
            0.08f, 0.09f, 0.09f, 0.08f, 0.08f, 0.16f, 0.1f, 0.07f, 0.1f, 0.09f
        };
        return valores[indice];
    }

    private static float TamanoMaximo(int indice)
    {
        float[] valores =
        {
            0.22f, 0.2f, 0.23f, 0.2f, 0.16f, 0.24f, 0.13f, 0.22f, 0.28f, 0.22f,
            0.28f, 0.2f, 0.2f, 0.2f, 0.26f, 0.3f, 0.18f, 0.22f, 0.35f, 0.14f,
            0.18f, 0.22f, 0.22f, 0.2f, 0.24f, 0.35f, 0.24f, 0.2f, 0.24f, 0.2f
        };
        return valores[indice];
    }

    private static Color ColorPrimario(int indice)
    {
        Color[] colores =
        {
            new Color(0.32f, 0.9f, 1f), new Color(1f, 0.55f, 0.23f),
            new Color(0.8f, 0.75f, 0.63f), new Color(1f, 0.82f, 0.24f),
            new Color(0.36f, 1f, 0.74f), new Color(0.68f, 0.45f, 1f),
            new Color(0.25f, 0.82f, 1f), new Color(0.35f, 0.68f, 1f),
            new Color(0.18f, 0.55f, 1f), new Color(0.55f, 0.88f, 1f),
            new Color(0.55f, 1f, 0.35f), new Color(0.72f, 0.35f, 1f),
            new Color(0.75f, 0.72f, 0.58f), new Color(0.42f, 0.86f, 1f),
            new Color(0.55f, 0.62f, 0.8f), new Color(0.72f, 0.35f, 1f),
            new Color(0.3f, 1f, 0.82f), new Color(1f, 0.86f, 0.36f),
            new Color(0.48f, 0.45f, 1f), new Color(0.34f, 0.84f, 1f),
            new Color(0.35f, 0.8f, 1f), new Color(0.52f, 0.82f, 0.4f),
            new Color(0.4f, 0.32f, 0.72f), new Color(0.9f, 0.78f, 1f),
            new Color(0.48f, 0.84f, 1f), new Color(0.56f, 0.64f, 0.78f),
            new Color(0.36f, 0.76f, 1f), new Color(1f, 0.88f, 0.4f),
            new Color(0.42f, 1f, 0.76f), new Color(0.45f, 0.62f, 1f)
        };
        return colores[indice];
    }

    private static Color ColorSecundario(int indice)
    {
        Color[] colores =
        {
            new Color(0.8f, 1f, 1f), new Color(1f, 0.2f, 0.12f),
            new Color(1f, 0.9f, 0.72f), new Color(1f, 0.48f, 0.12f),
            new Color(0.8f, 1f, 0.88f), new Color(0.3f, 0.75f, 1f),
            new Color(0.65f, 0.32f, 1f), new Color(0.52f, 0.9f, 1f),
            new Color(0.9f, 0.28f, 0.12f), new Color(0.75f, 1f, 1f),
            new Color(0.82f, 1f, 0.18f), new Color(0.3f, 0.95f, 1f),
            new Color(0.95f, 0.9f, 0.75f), new Color(0.72f, 0.5f, 1f),
            new Color(0.82f, 0.88f, 1f), new Color(0.4f, 0.8f, 1f),
            new Color(0.9f, 1f, 0.75f), new Color(1f, 0.98f, 0.7f),
            new Color(0.8f, 0.48f, 1f), new Color(0.9f, 0.98f, 1f),
            new Color(0.7f, 1f, 1f), new Color(0.88f, 1f, 0.52f),
            new Color(0.7f, 0.48f, 1f), new Color(1f, 0.9f, 0.72f),
            new Color(0.8f, 1f, 1f), new Color(0.75f, 0.84f, 1f),
            new Color(0.88f, 0.52f, 1f), new Color(0.82f, 1f, 1f),
            new Color(0.7f, 1f, 0.9f), new Color(1f, 0.98f, 0.72f)
        };
        return colores[indice];
    }
}
