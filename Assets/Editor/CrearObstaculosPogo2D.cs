using System;
using System.IO;
using UnityEditor;
using UnityEngine;

public static class CrearObstaculosPogo2D
{
    private const string CarpetaArte = "Assets/Demo/CanalCampanas";
    private const string CarpetaPrefabs = "Assets/Demo/Prefabs/Obstaculos";

    [MenuItem("Demo/Generar prefabs de obstaculos rebotables")]
    public static void Generar()
    {
        Directory.CreateDirectory(CarpetaArte);
        Directory.CreateDirectory(CarpetaPrefabs);

        Sprite spritePinchos = CrearSpritePinchos("PinchosSal");
        Sprite spriteCampana = CrearSpriteCampanaElastica("CampanaElastica");

        GameObject prefabPinchos = GenerarPrefabPinchos(spritePinchos);
        GameObject prefabCampana = GenerarPrefabCampana(spriteCampana);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("Prefabs de obstaculos rebotables generados con exito en " + CarpetaPrefabs + " (" +
                  prefabPinchos.name + ", " + prefabCampana.name + ").");
    }

    public static GameObject GenerarPrefabPinchos(Sprite sprite)
    {
        string ruta = Path.Combine(CarpetaPrefabs, "PinchosSal.prefab").Replace("\\", "/");

        GameObject objeto = new GameObject("PinchosSal");
        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        renderer.color = Color.white;

        BoxCollider2D collider = objeto.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(1f, 0.6f);
        collider.offset = new Vector2(0f, -0.05f);

        PinchosSal2D pinchos = objeto.AddComponent<PinchosSal2D>();
        pinchos.Configurar(1, 0.25f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        UnityEngine.Object.DestroyImmediate(objeto);
        return prefab;
    }

    public static GameObject GenerarPrefabCampana(Sprite sprite)
    {
        string ruta = Path.Combine(CarpetaPrefabs, "CampanaElastica.prefab").Replace("\\", "/");

        GameObject objeto = new GameObject("CampanaElastica");
        SpriteRenderer renderer = objeto.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 2;
        renderer.color = Color.white;

        CircleCollider2D collider = objeto.AddComponent<CircleCollider2D>();
        collider.isTrigger = true;
        collider.radius = 0.45f;

        CampanaResonante2D campana = objeto.AddComponent<CampanaResonante2D>();
        campana.Configurar(14.5f);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(objeto, ruta);
        UnityEngine.Object.DestroyImmediate(objeto);
        return prefab;
    }

    public static Sprite CrearSpritePinchos(string nombre)
    {
        string ruta = CarpetaArte + "/" + nombre + ".png";
        Texture2D textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;

        Color transparente = new Color(0f, 0f, 0f, 0f);
        Color baseCristal = new Color(0.85f, 0.95f, 0.98f, 1f);
        Color sombraCristal = new Color(0.45f, 0.65f, 0.72f, 1f);
        Color borde = new Color(0.25f, 0.42f, 0.48f, 1f);

        for (int y = 0; y < textura.height; y++)
        {
            for (int x = 0; x < textura.width; x++)
            {
                // Dibuja 3 puas triangulares de sal/cristal
                int puaIndex = x / 10;
                int xRel = x % 10;
                int centroPua = 5;
                int distDesdeCentro = Mathf.Abs(xRel - centroPua);
                int alturaMax = 26 - distDesdeCentro * 5;

                if (y <= alturaMax && y >= 3 && puaIndex < 3)
                {
                    if (y == alturaMax || distDesdeCentro == 4 || y == 3)
                        textura.SetPixel(x, y, borde);
                    else if (xRel < centroPua)
                        textura.SetPixel(x, y, baseCristal);
                    else
                        textura.SetPixel(x, y, sombraCristal);
                }
                else
                {
                    textura.SetPixel(x, y, transparente);
                }
            }
        }

        textura.Apply();
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        return ConfigurarImportadorSprite(ruta);
    }

    public static Sprite CrearSpriteCampanaElastica(string nombre)
    {
        string ruta = CarpetaArte + "/" + nombre + ".png";
        Texture2D textura = new Texture2D(32, 32, TextureFormat.RGBA32, false);
        textura.filterMode = FilterMode.Point;
        textura.wrapMode = TextureWrapMode.Clamp;

        Color transparente = new Color(0f, 0f, 0f, 0f);
        Color oroClaro = new Color(0.98f, 0.88f, 0.68f, 1f);
        Color oroOscuro = new Color(0.82f, 0.62f, 0.35f, 1f);
        Color borde = new Color(0.48f, 0.32f, 0.15f, 1f);
        Color rebordeElastico = new Color(0.4f, 0.85f, 0.9f, 1f);

        for (int y = 0; y < textura.height; y++)
        {
            for (int x = 0; x < textura.width; x++)
            {
                int dx = x - 16;
                int dy = y - 16;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (dist <= 13f)
                {
                    if (dist >= 11.5f)
                        textura.SetPixel(x, y, borde);
                    else if (y <= 7)
                        textura.SetPixel(x, y, rebordeElastico);
                    else if (dx < 0)
                        textura.SetPixel(x, y, oroClaro);
                    else
                        textura.SetPixel(x, y, oroOscuro);
                }
                else
                {
                    textura.SetPixel(x, y, transparente);
                }
            }
        }

        textura.Apply();
        File.WriteAllBytes(ruta, textura.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(textura);
        AssetDatabase.ImportAsset(ruta, ImportAssetOptions.ForceUpdate);

        return ConfigurarImportadorSprite(ruta);
    }

    private static Sprite ConfigurarImportadorSprite(string ruta)
    {
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
}
