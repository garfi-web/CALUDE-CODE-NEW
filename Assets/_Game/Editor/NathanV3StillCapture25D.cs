using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Nathan M0 refait (corps 3D, tête A et mains illustrées 2D, 2026-10-30) : une image fixe par pose (repos, garde,
// coup tendu) posée à la place du sprite de Nathan dans l'arène 2,5D, à côté d'un Voyou ; capture en jeu à
// 1920 × 1080 dans docs/captures/. Les images ont le cadrage des sprites (1024 px pour 2,6 m, pieds à 0,349 m du bas).
// La scène enregistrée n'est pas modifiée : elle est rechargée à la fin.
public static class NathanV3StillCapture25D
{
    private const string ScenePath = "Assets/_Game/Scenes/Arena25D.unity";
    private const string Characters = "Assets/_Game/Art/Characters";
    private const string VoyouStill = Characters + "/Voyou/Stills/Voyou_idle_tete3d.png";
    private const float SpritePixelsPerMeter = 1024f / 2.6f;
    private const float FeetFromBottom = 0.349f;
    private static readonly string[] Poses = { "repos", "garde", "coup" };

    [MenuItem("RED MET/Capturer Nathan M0 refait (repos, garde, coup)")]
    public static void Capture()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "captures"));
        Import(VoyouStill);

        foreach (string pose in Poses)
        {
            string still = $"{Characters}/Nathan_M0/Stills/Nathan_M0_v3_{pose}.png";
            Import(still);
            EditorSceneManager.OpenScene(ScenePath);
            Stage(AssetDatabase.LoadAssetAtPath<Sprite>(still));
            Render(Camera.main, Path.Combine(folder, $"2026-10-30_nathan_m0_v3_jeu_{pose}.png"));
        }

        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[RED MET] Nathan M0 refait : captures dans " + folder);
    }

    // Essai de direction artistique « roman graphique » (2026-10-30) : Nathan et le Voyou rendus en BD dans Blender
    // (tools/blender/redmet_bd.py), en garde, face à face. Les décors ne sont pas modifiés.
    [MenuItem("RED MET/Capturer l'essai BD (Nathan et Voyou en garde)")]
    public static void CaptureComic()
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "captures"));
        string nathan = $"{Characters}/Nathan_M0/Stills/Nathan_M0_BD_garde.png";
        string voyou = $"{Characters}/Voyou/Stills/Voyou_BD_garde.png";
        Import(nathan);
        Import(voyou);
        EditorSceneManager.OpenScene(ScenePath);
        Stage(AssetDatabase.LoadAssetAtPath<Sprite>(nathan), voyou);

        // L'éclairage et le contour sont dans l'image : matériau non éclairé, sans le liseré coloré des personnages.
        var unlit = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));

        foreach (string who in new[] { "Nathan" })
            GameObject.Find(who).transform.Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial = unlit;

        GameObject.Find("Réserve d'ennemis").transform.GetChild(0).Find("Visual").GetComponent<SpriteRenderer>().sharedMaterial = unlit;
        Render(Camera.main, Path.Combine(folder, "2026-10-30_essai_bd2_jeu.png"));
        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[RED MET] Essai BD : capture dans " + folder);
    }

    private static void Stage(Sprite nathanSprite, string voyouStill = VoyouStill)
    {
        GameObject nathan = GameObject.Find("Nathan");
        nathan.transform.position = new Vector3(-0.9f, -0.75f, 0f);
        SpriteRenderer body = nathan.transform.Find("Visual").GetComponent<SpriteRenderer>();
        body.GetComponent<Animator>().enabled = false;
        body.sprite = nathanSprite;
        body.color = Color.white;

        if (nathan.TryGetComponent(out FaceLayer25D oldFace))
            oldFace.enabled = false;

        foreach (string child in new[] { "Visage", "Barre tenue" })
        {
            Transform t = body.transform.Find(child);

            if (t != null)
                t.gameObject.SetActive(false);
        }

        // Voyou à côté, tourné vers Nathan.
        Transform pool = GameObject.Find("Réserve d'ennemis").transform;
        GameObject voyou = pool.GetChild(0).gameObject;
        voyou.SetActive(true);
        voyou.transform.position = new Vector3(0.8f, -0.85f, 0f);
        voyou.transform.localScale = new Vector3(-1f, 1f, 1f);
        SpriteRenderer voyouBody = voyou.transform.Find("Visual").GetComponent<SpriteRenderer>();
        voyouBody.GetComponent<Animator>().enabled = false;
        voyouBody.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(voyouStill);
        voyouBody.color = Color.white;

        GameObject props = GameObject.Find("Objets");

        if (props != null)
            props.SetActive(false);

        foreach (SpawnPoint25D point in Object.FindObjectsByType<SpawnPoint25D>())
        {
            foreach (SpriteRenderer opening in point.GetComponentsInChildren<SpriteRenderer>())
                opening.enabled = false;
        }

        foreach (SpriteRenderer shadow in GameObject.Find("Ombres").GetComponentsInChildren<SpriteRenderer>())
            shadow.enabled = false;

        foreach (GameObject character in new[] { nathan, voyou })
        {
            var b = character.GetComponent<Body25D>();

            if (b.Shadow != null)
                b.Shadow.enabled = true;

            b.UpdatePresentation();
        }

        Transform ring = GameObject.Find("Ombres").transform.Find("Nathan (repère)");

        if (ring != null)
            ring.GetComponent<SpriteRenderer>().enabled = true;
    }

    private static void Render(Camera cam, string path)
    {
        var rt = new RenderTexture(1920, 1080, 24, RenderTextureFormat.ARGB32);
        RenderTexture previous = cam.targetTexture;
        cam.targetTexture = rt;
        cam.Render();
        RenderTexture.active = rt;
        var tex = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
        tex.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
        tex.Apply();
        File.WriteAllBytes(path, tex.EncodeToPNG());
        cam.targetTexture = previous;
        RenderTexture.active = null;
        Object.DestroyImmediate(tex);
        rt.Release();
        Object.DestroyImmediate(rt);
    }

    private static void Import(string path)
    {
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = SpritePixelsPerMeter;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, FeetFromBottom / 2.6f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
