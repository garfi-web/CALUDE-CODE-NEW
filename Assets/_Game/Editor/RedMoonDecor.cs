using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Décor de l'arène de la lune rouge (tranche verticale) : plaine de cendres sous une lune rouge géante,
// palette noir et rouge du monde de la Red Met (docs/CHARACTERS.md).
// Les images sont calculées ici, sans IA et à l'identique d'une fois sur l'autre (graines fixes), en aplats
// francs avec un liseré clair sur les crêtes, pour s'accorder au rendu toon des personnages.
// ArenaBuilder appelle Build (couches en parallaxe, sol, lumières 2D, post-traitement, cendres et braises) ;
// les images ne sont recalculées que si elles manquent, ou par le menu RED MET > Régénérer le décor.
public static class RedMoonDecor
{
    private const string ArtFolder = "Assets/_Game/Art/Backgrounds/RedMoon";
    private const string ProfilePath = "Assets/_Game/Settings/RedMoon_PostFX.asset";
    private const string RendererPath = "Assets/_Game/Settings/Renderer2D.asset";
    private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
    private const float Ppu = 64f;

    private static readonly string[] SpriteNames = { "Sky", "Moon", "Halo", "Haze", "Far", "Mid", "Near", "Ground", "Cracks", "Foreground", "Slab", "Dot" };

    // Position de départ de la caméra (ArenaBuilder.BuildCamera) : le décor est composé autour.
    private static readonly Vector2 CameraStart = new(-4f, 0.9f);

    [MenuItem("RED MET/Régénérer le décor de la lune rouge")]
    public static void Regenerate()
    {
        EnsureArt(true);
        Debug.Log("[RED MET] Décor de la lune rouge régénéré : relancez Construire l'arène prototype.");
    }

    // ------------------------------------------------------------------ Scène

    public static void Build(Camera camera)
    {
        Dictionary<string, Sprite> art = EnsureArt(false);
        var root = new GameObject("Decor").transform;
        Material unlit = EnsureMaterial("DecorUnlit", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"), null, 0f);
        Shader glowShader = Shader.Find("RED MET/Sprite Glow");
        Vector2 c = CameraStart;

        // Ciel collé à la caméra, puis lune et halo presque immobiles.
        Layer(root, "Sky", art["Sky"], unlit, new Vector3(c.x, c.y + 1f), new Vector2(1f, 1f), -100, scale: new Vector2(120f, 1f));
        Vector3 moonPosition = new(c.x + 3.8f, c.y + 1.9f);
        Layer(root, "Moon Halo", art["Halo"], EnsureMaterial("HaloGlow", glowShader, null, 0.9f), moonPosition, new Vector2(0.95f, 0.95f), -95);
        Transform moon = Layer(root, "Moon", art["Moon"], EnsureMaterial("MoonGlow", glowShader, null, 1.25f), moonPosition, new Vector2(0.95f, 0.95f), -94);

        var moonLight = new GameObject("Moon Light 2D").AddComponent<Light2D>();
        moonLight.transform.SetParent(moon, false);
        moonLight.lightType = Light2D.LightType.Point;
        moonLight.color = new Color(1f, 0.32f, 0.22f);
        moonLight.intensity = 0.8f;
        moonLight.pointLightInnerRadius = 2f;
        moonLight.pointLightOuterRadius = 16f;

        // Montagnes lointaines, brume d'horizon, ruines, dunes de cendres.
        Tiled(root, "Far Mountains", art["Far"], null, new Vector3(c.x, -2f), new Vector2(70f, 8f), new Vector2(0.85f, 0.7f), -80);
        Layer(root, "Horizon Haze", art["Haze"], unlit, new Vector3(c.x, -0.7f), new Vector2(0.75f, 0.6f), -76, scale: new Vector2(400f, 0.75f));
        Tiled(root, "Ruins", art["Mid"], null, new Vector3(c.x, -2.2f), new Vector2(70f, 8f), new Vector2(0.6f, 0.45f), -70);
        Tiled(root, "Ash Dunes", art["Near"], null, new Vector3(c.x, -2.4f), new Vector2(70f, 6f), new Vector2(0.3f, 0.2f), -60);

        // Sol du plan de jeu (le collider reste celui d'ArenaBuilder), avec ses fissures de Red Met.
        Tiled(root, "Ground", art["Ground"], null, new Vector3(0f, -1.5f), new Vector2(44f, 5f), Vector2.zero, -10);
        Tiled(root, "Ground Cracks", art["Cracks"], EnsureMaterial("CrackGlow", glowShader, null, 2.2f), new Vector3(0f, -1.5f), new Vector2(44f, 5f), Vector2.zero, -9);

        // Rochers du premier plan, qui défilent plus vite que la caméra, en bas de l'écran.
        Tiled(root, "Foreground Rocks", art["Foreground"], null, new Vector3(c.x, -3f), new Vector2(90f, 4f), new Vector2(-0.35f, 0.3f), 40);

        Material ash = EnsureMaterial("AshParticle", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"), art["Dot"].texture, 0f);
        Material ember = EnsureMaterial("EmberGlow", glowShader, art["Dot"].texture, 3f);
        CreateAsh(camera.transform, ash);
        CreateEmbers(camera.transform, ember);
        SetupPostProcessing(camera, root);
    }

    // Plateforme d'ArenaBuilder : son carré provisoire est masqué, une dalle de pierre noire le remplace.
    public static void DressPlatform(GameObject platform)
    {
        Dictionary<string, Sprite> art = EnsureArt(false);
        var placeholder = platform.GetComponent<SpriteRenderer>();
        Bounds bounds = placeholder.bounds;
        placeholder.enabled = false;

        var slab = new GameObject(platform.name + " Slab");
        slab.transform.SetParent(platform.transform.parent, false);
        slab.transform.position = new Vector3(bounds.center.x, bounds.max.y);

        var renderer = slab.AddComponent<SpriteRenderer>();
        renderer.sprite = art["Slab"];
        renderer.drawMode = SpriteDrawMode.Sliced;
        renderer.size = new Vector2(bounds.size.x, art["Slab"].bounds.size.y);
        renderer.sortingOrder = -4;
    }

    private static Transform Layer(Transform parent, string name, Sprite sprite, Material material, Vector3 position, Vector2 factor,
        int order, Vector2? scale = null)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        if (scale.HasValue)
            go.transform.localScale = new Vector3(scale.Value.x, scale.Value.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = order;

        if (material != null)
            renderer.sharedMaterial = material;

        if (factor != Vector2.zero)
            SetFactor(go.AddComponent<ParallaxLayer>(), factor);

        return go.transform;
    }

    private static void Tiled(Transform parent, string name, Sprite sprite, Material material, Vector3 position, Vector2 size, Vector2 factor, int order)
    {
        Transform layer = Layer(parent, name, sprite, material, position, factor, order);
        var renderer = layer.GetComponent<SpriteRenderer>();
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        renderer.size = size;
    }

    private static void SetFactor(ParallaxLayer layer, Vector2 factor)
    {
        var so = new SerializedObject(layer);
        so.FindProperty("factor").vector2Value = factor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Material EnsureMaterial(string name, Shader shader, Texture texture, float intensity)
    {
        string path = $"{ArtFolder}/{name}.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material == null)
        {
            material = new Material(shader) { name = name };
            AssetDatabase.CreateAsset(material, path);
        }

        if (texture != null)
            material.mainTexture = texture;

        if (material.HasProperty("_Intensity"))
            material.SetFloat("_Intensity", intensity);

        EditorUtility.SetDirty(material);
        return material;
    }

    // Cendres qui tombent en biais et braises qui montent du sol. Systèmes posés une fois pour toutes :
    // rien n'est créé ni détruit pendant le combat.
    private static void CreateAsh(Transform camera, Material material)
    {
        ParticleSystem ps = Particles(camera, "Ash", new Vector3(0f, 4.4f, 10f), new Vector3(18f, 0.5f, 0.1f), material, 20);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(9f, 12f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(0.6f, 0.55f, 0.56f, 0.85f), new Color(0.32f, 0.28f, 0.3f, 0.7f));
        main.maxParticles = 500;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 40f;
        Velocity(ps, new Vector2(-0.45f, -1f), new Vector2(-0.1f, -0.6f));
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.25f;
        noise.frequency = 0.4f;
        noise.scrollSpeed = 0.2f;
    }

    private static void CreateEmbers(Transform camera, Material material)
    {
        ParticleSystem ps = Particles(camera, "Embers", new Vector3(0f, -3.2f, 10f), new Vector3(18f, 0.3f, 0.1f), material, 21);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(4f, 7f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.07f);
        main.startColor = new ParticleSystem.MinMaxGradient(new Color(1f, 0.38f, 0.12f), new Color(1f, 0.12f, 0.05f));
        main.maxParticles = 120;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 9f;
        Velocity(ps, new Vector2(-0.25f, 0.3f), new Vector2(0.15f, 0.9f));
        ParticleSystem.NoiseModule noise = ps.noise;
        noise.enabled = true;
        noise.strength = 0.6f;
        noise.frequency = 0.6f;
    }

    private static ParticleSystem Particles(Transform camera, string name, Vector3 localPosition, Vector3 box, Material material, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(camera, false);
        go.transform.localPosition = localPosition;

        var ps = go.AddComponent<ParticleSystem>();
        ParticleSystem.MainModule main = ps.main;
        main.loop = true;
        main.prewarm = true;
        main.startSpeed = 0f;
        main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = box;

        // Apparition et disparition en fondu.
        ParticleSystem.ColorOverLifetimeModule color = ps.colorOverLifetime;
        color.enabled = true;
        var fade = new Gradient();
        fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(1f, 0.75f), new GradientAlphaKey(0f, 1f) });
        color.color = fade;

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = material;
        renderer.sortingOrder = order;
        return ps;
    }

    private static void Velocity(ParticleSystem ps, Vector2 min, Vector2 max)
    {
        ParticleSystem.VelocityOverLifetimeModule velocity = ps.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(min.x, max.x);
        velocity.y = new ParticleSystem.MinMaxCurve(min.y, max.y);
        velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
    }

    // Bloom (lune, fissures, braises), vignette, contraste et grain léger.
    private static void SetupPostProcessing(Camera camera, Transform parent)
    {
        camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

        // Le Renderer 2D du projet n'avait pas de données de post-traitement : sans elles, aucun effet ne s'affiche.
        var rendererData = AssetDatabase.LoadAssetAtPath<ScriptableObject>(RendererPath);
        var so = new SerializedObject(rendererData);
        SerializedProperty data = so.FindProperty("m_PostProcessData");

        if (data != null && data.objectReferenceValue == null)
        {
            data.objectReferenceValue = AssetDatabase.LoadAssetAtPath<UnityEngine.Object>(PostProcessDataPath);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        var volume = new GameObject("PostFX Volume").AddComponent<Volume>();
        volume.transform.SetParent(parent, false);
        volume.isGlobal = true;
        volume.sharedProfile = EnsureProfile();
    }

    private static VolumeProfile EnsureProfile()
    {
        var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(ProfilePath);

        if (profile != null)
            return profile;

        profile = ScriptableObject.CreateInstance<VolumeProfile>();
        AssetDatabase.CreateAsset(profile, ProfilePath);

        var bloom = profile.Add<Bloom>(true);
        bloom.threshold.Override(0.95f);
        bloom.intensity.Override(1.1f);
        bloom.scatter.Override(0.6f);
        bloom.tint.Override(new Color(1f, 0.6f, 0.55f));

        var vignette = profile.Add<Vignette>(true);
        vignette.intensity.Override(0.38f);
        vignette.smoothness.Override(0.5f);

        var color = profile.Add<ColorAdjustments>(true);
        color.contrast.Override(12f);
        color.saturation.Override(6f);

        var grain = profile.Add<FilmGrain>(true);
        grain.type.Override(FilmGrainLookup.Thin1);
        grain.intensity.Override(0.15f);

        foreach (VolumeComponent component in profile.components)
            AssetDatabase.AddObjectToAsset(component, profile);

        EditorUtility.SetDirty(profile);
        AssetDatabase.SaveAssets();
        return profile;
    }

    // ----------------------------------------------------------------- Images

    private static Dictionary<string, Sprite> EnsureArt(bool force)
    {
        if (!AssetDatabase.IsValidFolder(ArtFolder))
            AssetDatabase.CreateFolder(Path.GetDirectoryName(ArtFolder).Replace('\\', '/'), Path.GetFileName(ArtFolder));

        bool missing = Array.Exists(SpriteNames, n => !File.Exists($"{ArtFolder}/{n}.png"));

        if (force || missing)
            Generate();

        var art = new Dictionary<string, Sprite>();

        foreach (string name in SpriteNames)
            art[name] = AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

        return art;
    }

    private static void Generate()
    {
        Save("Sky", Sky(), new Vector2(0.5f, 0.5f), 32f, false);
        Save("Moon", Moon(), new Vector2(0.5f, 0.5f), 128f, false);
        Save("Halo", Radial(256, new Color(1f, 0.25f, 0.15f), 2.2f, 0.9f), new Vector2(0.5f, 0.5f), 32f, false);
        Save("Haze", Haze(), new Vector2(0.5f, 0.5f), 32f, false);
        Save("Far", FarMountains(), new Vector2(0.5f, 0f), Ppu, true);
        Save("Mid", Ruins(), new Vector2(0.5f, 0f), Ppu, true);
        Save("Near", Dunes(), new Vector2(0.5f, 0f), Ppu, true);
        Save("Ground", Ground(), new Vector2(0.5f, 1f), Ppu, true);
        Save("Cracks", Cracks(), new Vector2(0.5f, 1f), Ppu, true);
        Save("Foreground", Rocks(), new Vector2(0.5f, 0f), Ppu, true);
        Save("Slab", Slab(), new Vector2(0.5f, 1f), Ppu, false, new Vector4(24f, 0f, 24f, 0f));
        Save("Dot", Radial(32, Color.white, 1.5f, 1f), new Vector2(0.5f, 0.5f), 32f, false);
    }

    private static void Save(string name, Pixels image, Vector2 pivot, float ppu, bool repeat, Vector4 border = default)
    {
        string path = $"{ArtFolder}/{name}.png";
        var texture = new Texture2D(image.W, image.H, TextureFormat.RGBA32, false);
        texture.SetPixels(image.P);
        texture.Apply();
        File.WriteAllBytes(path, texture.EncodeToPNG());
        UnityEngine.Object.DestroyImmediate(texture);
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);

        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = ppu;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapModeU = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spriteBorder = border;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // Image RGBA, ligne 0 en bas comme Texture2D ; x boucle pour les couches qui se répètent.
    private sealed class Pixels
    {
        public readonly int W, H;
        public readonly Color[] P;

        public Pixels(int w, int h)
        {
            W = w;
            H = h;
            P = new Color[w * h];
        }

        public void Set(int x, int y, Color c) => P[y * W + Wrap(x, W)] = c;
    }

    private static int Wrap(int x, int n) => ((x % n) + n) % n;

    private static float Hash(int i, int seed)
    {
        unchecked
        {
            uint h = (uint)(i * 374761393 + seed * 668265263);
            h = (h ^ (h >> 13)) * 1274126177u;
            h ^= h >> 16;
            return (h & 0xFFFFFF) / 16777216f;
        }
    }

    // Bruit de valeur entre 0 et 1, périodique en u (de 0 à 1) : la couche se répète sans couture.
    private static float Noise(float u, int seed, int frequency, int octaves = 1, float v = 0f, int frequencyV = 0)
    {
        float sum = 0f, amplitude = 1f, total = 0f;

        for (int o = 0; o < octaves; o++)
        {
            int n = frequency << o;
            float x = (u - Mathf.Floor(u)) * n;
            float y = v * (frequencyV << o);
            int i = Mathf.FloorToInt(x), j = Mathf.FloorToInt(y);
            float tx = Smooth(x - i), ty = Smooth(y - j);
            int s = seed + o * 101;
            float a = Mathf.Lerp(Hash(Wrap(i, n) + j * 7919, s), Hash(Wrap(i + 1, n) + j * 7919, s), tx);
            float b = Mathf.Lerp(Hash(Wrap(i, n) + (j + 1) * 7919, s), Hash(Wrap(i + 1, n) + (j + 1) * 7919, s), tx);
            sum += amplitude * Mathf.Lerp(a, b, ty);
            total += amplitude;
            amplitude *= 0.5f;
        }

        return sum / total;
    }

    private static float Smooth(float t) => t * t * (3f - 2f * t);

    private static float Ridge(float u, int seed, int frequency, int octaves) => 1f - Mathf.Abs(2f * Noise(u, seed, frequency, octaves) - 1f);

    // Masque de couverture (0 à 1) : silhouettes remplies sous une hauteur, et traits en capsule.
    private static float[] HeightMask(int w, int h, Func<float, float> height)
    {
        var mask = new float[w * h];

        for (int x = 0; x < w; x++)
        {
            float top = height((x + 0.5f) / w);

            for (int y = 0; y < h; y++)
                mask[y * w + x] = Mathf.Clamp01(top - y);
        }

        return mask;
    }

    private static void Stroke(float[] mask, int w, int h, Vector2 a, Vector2 b, float r0, float r1)
    {
        float r = Mathf.Max(r0, r1) + 1f;
        int minX = Mathf.FloorToInt(Mathf.Min(a.x, b.x) - r), maxX = Mathf.CeilToInt(Mathf.Max(a.x, b.x) + r);
        int minY = Mathf.Max(0, Mathf.FloorToInt(Mathf.Min(a.y, b.y) - r)), maxY = Mathf.Min(h - 1, Mathf.CeilToInt(Mathf.Max(a.y, b.y) + r));
        Vector2 ab = b - a;
        float length = Mathf.Max(ab.sqrMagnitude, 1e-4f);

        for (int y = minY; y <= maxY; y++)
        for (int x = minX; x <= maxX; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / length);
            float d = Vector2.Distance(p, a + ab * t);
            float coverage = Mathf.Clamp01(Mathf.Lerp(r0, r1, t) - d + 0.5f);
            int i = y * w + Wrap(x, w);
            mask[i] = Mathf.Max(mask[i], coverage);
        }
    }

    // Aplat + liseré clair sur les bords supérieurs (lumière de la lune), comme un décor d'anime.
    private static Pixels Colorize(float[] mask, int w, int h, Color body, Color rim, int rimPixels, Color? bottom = null)
    {
        var image = new Pixels(w, h);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float a = mask[y * w + x];

            if (a <= 0f)
                continue;

            bool edge = y + rimPixels >= h || mask[(y + rimPixels) * w + x] < 0.5f;
            Color c = edge ? rim : bottom.HasValue ? Color.Lerp(bottom.Value, body, Mathf.Clamp01(y / (h * 0.45f))) : body;
            c.a = a;
            image.Set(x, y, c);
        }

        return image;
    }

    private static Pixels Sky()
    {
        // Bandes franches du noir (haut) au rouge sombre (horizon, au tiers inférieur).
        Color[] bands =
        {
            new(0.40f, 0.07f, 0.06f), new(0.30f, 0.05f, 0.05f), new(0.21f, 0.035f, 0.04f), new(0.14f, 0.025f, 0.035f),
            new(0.09f, 0.015f, 0.025f), new(0.055f, 0.01f, 0.02f), new(0.03f, 0.006f, 0.014f),
        };
        float[] limits = { 0.12f, 0.22f, 0.32f, 0.42f, 0.55f, 0.7f, 1.01f };
        var image = new Pixels(8, 512);

        for (int y = 0; y < image.H; y++)
        {
            float v = (y + 0.5f) / image.H;
            Color c = bands[Array.FindIndex(limits, l => v < l)];

            for (int x = 0; x < image.W; x++)
                image.Set(x, y, c);
        }

        return image;
    }

    private static Pixels Moon()
    {
        const int size = 512;
        const float radius = 235f;
        var center = new Vector2(size / 2f, size / 2f);
        var shifted = center + new Vector2(16f, -10f);
        var image = new Pixels(size, size);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f);
            float alpha = Mathf.Clamp01(radius - Vector2.Distance(p, center) + 0.5f);

            if (alpha <= 0f)
                continue;

            // Mers en deux aplats plus sombres, croissant éclairé en haut à gauche.
            float n = Noise(p.x / size, 71, 4, 3, p.y / size, 4);
            Color c = n > 0.66f ? new Color(0.5f, 0.04f, 0.04f) : n > 0.56f ? new Color(0.64f, 0.07f, 0.05f) : new Color(0.86f, 0.12f, 0.08f);

            if (Vector2.Distance(p, shifted) > radius - 3f)
                c = new Color(1f, 0.42f, 0.28f);

            c.a = alpha;
            image.Set(x, y, c);
        }

        return image;
    }

    private static Pixels Radial(int size, Color color, float power, float strength)
    {
        var image = new Pixels(size, size);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
            Color c = color;
            c.a = Mathf.Pow(Mathf.Clamp01(1f - d), power) * strength;
            image.Set(x, y, c);
        }

        return image;
    }

    private static Pixels Haze()
    {
        var image = new Pixels(8, 128);

        for (int y = 0; y < image.H; y++)
        {
            float s = Mathf.Sin(Mathf.PI * (y + 0.5f) / image.H);
            var c = new Color(0.55f, 0.08f, 0.06f, s * s * 0.45f);

            for (int x = 0; x < image.W; x++)
                image.Set(x, y, c);
        }

        return image;
    }

    private static Pixels FarMountains()
    {
        const int w = 2048, h = 512;
        float[] mask = HeightMask(w, h, u => 90f + 130f * Noise(u, 11, 2, 4) + 90f * Mathf.Pow(Ridge(u, 12, 5, 3), 1.5f));
        return Colorize(mask, w, h, new Color(0.17f, 0.03f, 0.045f), new Color(0.5f, 0.08f, 0.07f), 3, new Color(0.3f, 0.05f, 0.055f));
    }

    private static Pixels Ruins()
    {
        const int w = 2048, h = 512;
        float[] mask = HeightMask(w, h, u => 70f + 50f * Noise(u, 21, 4, 3));

        // Piliers brisés au sommet dentelé.
        for (int k = 0; k < 7; k++)
        {
            float x = (k + Hash(k, 22)) * w / 7f;
            float width = 34f + 36f * Hash(k, 23);
            float height = 140f + 190f * Hash(k, 24);

            for (int px = Mathf.FloorToInt(x); px < x + width; px++)
            {
                float top = 60f + height - 45f * Hash(px / 6, 25 + k);

                for (int y = 0; y < Mathf.Min(h, top + 1f); y++)
                {
                    int i = y * w + Wrap(px, w);
                    mask[i] = Mathf.Max(mask[i], Mathf.Clamp01(top - y));
                }
            }
        }

        // Arbres morts : branches qui se divisent en s'affinant.
        for (int k = 0; k < 5; k++)
        {
            float x = (k + 0.5f + 0.3f * (Hash(k, 26) - 0.5f)) * w / 5f;
            Branch(mask, w, h, new Vector2(x, 80f), 90f + 16f * (Hash(k, 27) - 0.5f), 80f + 40f * Hash(k, 28), 18f, 3, 30 + k * 13);
        }

        return Colorize(mask, w, h, new Color(0.075f, 0.015f, 0.022f), new Color(0.38f, 0.06f, 0.05f), 2);
    }

    private static void Branch(float[] mask, int w, int h, Vector2 start, float angle, float length, float width, int depth, int seed)
    {
        float radians = angle * Mathf.Deg2Rad;
        Vector2 end = start + new Vector2(Mathf.Cos(radians), Mathf.Sin(radians)) * length;
        Stroke(mask, w, h, start, end, width, width * 0.65f);

        if (depth == 0)
            return;

        for (int side = -1; side <= 1; side += 2)
        {
            float spread = 20f + 25f * Hash(depth * 3 + side, seed);
            Branch(mask, w, h, end, angle + side * spread, length * (0.6f + 0.15f * Hash(depth + side, seed + 1)), width * 0.65f, depth - 1, seed + 7 * (side + 2));
        }
    }

    private static Pixels Dunes()
    {
        const int w = 2048, h = 384;
        Func<float, float> height = u => 80f + 70f * Noise(u, 31, 3, 3);
        float[] mask = HeightMask(w, h, height);

        // Épées et lances plantées dans la cendre.
        for (int k = 0; k < 9; k++)
        {
            float x = (k + Hash(k, 32)) * w / 9f;
            float angle = (90f + 40f * (Hash(k, 33) - 0.5f)) * Mathf.Deg2Rad;
            float length = 60f + 50f * Hash(k, 34);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            var foot = new Vector2(x, height(x / w) - 10f);
            Stroke(mask, w, h, foot, foot + dir * length, 3f, 1.5f);
            Vector2 guard = foot + dir * (length * 0.25f);
            var across = new Vector2(dir.y, -dir.x) * 9f;
            Stroke(mask, w, h, guard - across, guard + across, 2f, 2f);
        }

        return Colorize(mask, w, h, new Color(0.06f, 0.04f, 0.045f), new Color(0.32f, 0.27f, 0.28f), 4);
    }

    private static Pixels Ground()
    {
        const int w = 512, h = 320;
        var image = new Pixels(w, h);

        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w;
            float ash = 8f + 6f * Noise(u, 41, 8, 2);

            for (int d = 0; d < h; d++)
            {
                Color c;

                if (d < 2)
                    c = new Color(0.46f, 0.4f, 0.41f);
                else if (d < ash)
                    c = new Color(0.31f, 0.27f, 0.28f);
                else
                {
                    // Terre en trois aplats, plus sombre en profondeur.
                    float n = Noise(u, 42, 8, 3, d / 64f, 1);
                    c = n < 0.42f ? new Color(0.075f, 0.05f, 0.06f) : n < 0.6f ? new Color(0.105f, 0.07f, 0.08f) : new Color(0.14f, 0.095f, 0.105f);
                    c = Color.Lerp(c, Color.black, 0.5f * d / h);
                }

                c.a = 1f;
                image.Set(x, h - 1 - d, c);
            }
        }

        return image;
    }

    private static Pixels Cracks()
    {
        const int w = 512, h = 320;
        var core = new float[w * h];
        var halo = new float[w * h];

        for (int k = 0; k < 6; k++)
        {
            var p = new Vector2((k + Hash(k, 51)) * w / 6f, h - 18f - 40f * Hash(k, 52));
            float heading = -90f + 70f * (Hash(k, 53) - 0.5f);
            int steps = 40 + Mathf.FloorToInt(50f * Hash(k, 54));

            for (int s = 0; s < steps && p.y > 8f; s++)
            {
                heading = Mathf.Clamp(heading + 50f * (Hash(s, 55 + k) - 0.5f), -160f, -20f);
                Vector2 next = p + new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad)) * 3f;
                Stroke(core, w, h, p, next, 1.1f, 1.1f);
                Stroke(halo, w, h, p, next, 4f, 4f);
                p = next;
            }
        }

        var image = new Pixels(w, h);

        for (int i = 0; i < core.Length; i++)
            image.P[i] = new Color(1f, 0.18f, 0.08f, Mathf.Max(core[i], halo[i] * 0.25f));

        return image;
    }

    private static Pixels Rocks()
    {
        const int w = 1024, h = 256;
        var mask = new float[w * h];

        for (int k = 0; k < 3; k++)
        {
            float cx = (k + Hash(k, 61)) * w / 3f;
            float half = (120f + 100f * Hash(k, 62)) / 2f;
            float height = 40f + 18f * Hash(k, 63);

            for (int x = Mathf.FloorToInt(cx - half); x <= cx + half; x++)
            {
                float t = (x - cx) / half;
                float top = height * Mathf.Sqrt(Mathf.Max(0f, 1f - t * t)) * (0.85f + 0.15f * Noise((float)x / w, 64, 32));

                for (int y = 0; y < Mathf.Min(h, top + 1f); y++)
                {
                    int i = y * w + Wrap(x, w);
                    mask[i] = Mathf.Max(mask[i], Mathf.Clamp01(top - y));
                }
            }
        }

        return Colorize(mask, w, h, new Color(0.02f, 0.008f, 0.012f), new Color(0.28f, 0.05f, 0.05f), 2);
    }

    // Dalle de pierre noire : 0,4 m de dessus (le collider), roche pendante dessous.
    private static Pixels Slab()
    {
        const int w = 256, h = 48;
        var image = new Pixels(w, h);

        for (int x = 0; x < w; x++)
        {
            float u = (x + 0.5f) / w;
            float bottom = 26f + 20f * Noise(u, 81, 16) * Mathf.Sin(Mathf.PI * u);

            for (int d = 0; d < h; d++)
            {
                float a = Mathf.Clamp01(bottom - d);

                if (a <= 0f)
                    continue;

                Color c = d < 3 ? new Color(0.45f, 0.12f, 0.11f)
                    : d < 26 ? (Noise(u, 82, 8, 2, d / 16f, 1) > 0.55f ? new Color(0.16f, 0.05f, 0.06f) : new Color(0.13f, 0.04f, 0.05f))
                    : new Color(0.07f, 0.02f, 0.03f);
                c.a = a;
                image.Set(x, h - 1 - d, c);
            }
        }

        return image;
    }
}
