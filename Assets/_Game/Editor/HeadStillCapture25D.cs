using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Têtes illustrées, palier 2 (validation de l'auteur, 2026-10-30) : une seule image fixe de Nathan au repos, sa tête
// illustrée posée par la piste de l'os de la tête (heads.json, comme le fera le jeu), à côté d'un Voyou (encore avec
// sa tête 3D : ses têtes illustrées viendront au palier 5), dans l'arène 2,5D. Capture en jeu à 1920 × 1080 et gros
// plan dans docs/captures/. La scène enregistrée n'est pas modifiée : elle est rechargée à la fin.
public static class HeadStillCapture25D
{
    private const string ScenePath = "Assets/_Game/Scenes/Arena25D.unity";
    private const string Characters = "Assets/_Game/Art/Characters";
    private const string HeadPath = Characters + "/Nathan_M0/Heads/Nathan_M0_head_neutral.png";
    private const string VoyouStill = Characters + "/Voyou/Stills/Voyou_idle_tete3d.png";
    // Cadre du rendu de référence de la tête (tools/blender/redmet_quality.py) : 1024 px pour 0,48 m.
    private const float HeadPixelsPerMeter = 1024f / 0.48f;
    // Sprites 3/4 : 1024 px pour 2,6 m, pieds à 0,349 m du bas (sprites.json).
    private const float SpritePixelsPerMeter = 1024f / 2.6f;
    private const float FeetFromBottom = 0.349f;

    [Serializable]
    private class Track
    {
        public int values_per_frame = 6;
        public Clip[] clips;
    }

    [Serializable]
    private class Clip
    {
        public string name;
        public float[] data;
    }

    [MenuItem("RED MET/Capturer l'essai de tête (palier 2)")]
    public static void Capture()
    {
        EditorSceneManager.OpenScene(ScenePath);
        Import(HeadPath, HeadPixelsPerMeter, new Vector2(0.5f, 0.5f));
        Import(VoyouStill, SpritePixelsPerMeter, new Vector2(0.5f, FeetFromBottom / 2.6f));

        // Nathan : image 0 du repos (corps sans tête), tête illustrée derrière le corps.
        GameObject nathan = GameObject.Find("Nathan");
        nathan.transform.position = new Vector3(-0.9f, -0.75f, 0f);
        SpriteRenderer body = nathan.transform.Find("Visual").GetComponent<SpriteRenderer>();
        body.GetComponent<Animator>().enabled = false;
        body.sprite = AssetDatabase.LoadAllAssetsAtPath(Characters + "/Nathan_M0/Sprites25D/NathanM0_idle.png")
            .OfType<Sprite>().First(s => s.name == "NathanM0_idle_00");

        if (nathan.TryGetComponent(out FaceLayer25D oldFace))
            oldFace.enabled = false;

        Transform oldFaceSprite = body.transform.Find("Visage");

        if (oldFaceSprite != null)
            oldFaceSprite.gameObject.SetActive(false);

        var track = JsonUtility.FromJson<Track>(File.ReadAllText(Characters + "/Nathan_M0/Sprites25D/heads.json"));
        float[] idle = track.clips.First(c => c.name == "idle").data;
        var headGo = new GameObject("Tête illustrée");
        headGo.transform.SetParent(body.transform, false);
        var head = headGo.AddComponent<SpriteRenderer>();
        head.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(HeadPath);
        head.sharedMaterial = body.sharedMaterial;
        head.sortingOrder = -1;
        Rect rect = body.sprite.rect;
        Vector2 pixel = new(idle[0] * rect.width, idle[1] * rect.height);
        headGo.transform.localPosition = (pixel - body.sprite.pivot) / body.sprite.pixelsPerUnit;
        // Inclinaison de l'image par rapport à la pose de repos du rendu de référence (angle 0).
        headGo.transform.localRotation = Quaternion.Euler(0f, 0f, idle[2]);

        // Voyou à côté, tourné vers Nathan.
        Transform pool = GameObject.Find("Réserve d'ennemis").transform;
        GameObject voyou = pool.GetChild(0).gameObject;
        voyou.SetActive(true);
        voyou.transform.position = new Vector3(0.8f, -0.85f, 0f);
        voyou.transform.localScale = new Vector3(-1f, 1f, 1f);
        SpriteRenderer voyouBody = voyou.transform.Find("Visual").GetComponent<SpriteRenderer>();
        voyouBody.GetComponent<Animator>().enabled = false;
        voyouBody.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(VoyouStill);
        voyouBody.color = Color.white;

        // Les objets au sol (barre de fer, caisses) gêneraient la lecture de l'essai.
        GameObject props = GameObject.Find("Objets");

        if (props != null)
            props.SetActive(false);

        // Ce que le jeu cache au lancement : barre tenue, ouvertures des entrées (transparentes tant qu'aucun ennemi
        // n'arrive), ombres des corps absents.
        Transform bar = body.transform.Find("Barre tenue");

        if (bar != null)
            bar.gameObject.SetActive(false);

        foreach (SpawnPoint25D point in UnityEngine.Object.FindObjectsByType<SpawnPoint25D>(FindObjectsSortMode.None))
        {
            foreach (SpriteRenderer opening in point.GetComponentsInChildren<SpriteRenderer>())
                opening.enabled = false;
        }

        foreach (SpriteRenderer shadow in GameObject.Find("Ombres").GetComponentsInChildren<SpriteRenderer>())
            shadow.enabled = false;

        // Ombres, repère et ordre d'affichage (LateUpdate ne tourne pas hors du jeu).
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

        Camera cam = Camera.main;
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "..", "docs", "captures"));
        Render(cam, Path.Combine(folder, "2026-10-30_tete_nathan_palier2_jeu.png"));

        // Gros plan : même rendu, cadré sur les deux têtes.
        Vector3 start = cam.transform.position;
        float size = cam.orthographicSize;
        cam.orthographicSize = 0.62f;
        cam.transform.position = new Vector3(-0.05f, headGo.transform.position.y - 0.05f, start.z);
        Render(cam, Path.Combine(folder, "2026-10-30_tete_nathan_palier2_grosplan.png"));
        cam.orthographicSize = size;
        cam.transform.position = start;

        EditorSceneManager.OpenScene(ScenePath);
        Debug.Log("[RED MET] Palier 2 : captures dans " + folder);
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
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);
    }

    private static void Import(string path, float pixelsPerUnit, Vector2 pivot)
    {
        AssetDatabase.ImportAsset(path);
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = true;
        importer.filterMode = FilterMode.Trilinear;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }
}
