using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Décor de l'arène de la lune rouge (tranche verticale) : plaine de cendres sous une lune rouge géante,
// palette noir et rouge du monde de la Red Met (docs/CHARACTERS.md).
// Les couches principales sont peintes avec ComfyUI et Animagine XL (tools/gen3d/background.py, dans
// Art/Backgrounds/RedMoonPainted) : ciel, lune, nuages, montagnes, ruines gothiques, arbres morts, champ de
// bataille, premier plan flou, sol. Les petits éléments sont calculés ici, à l'identique d'une fois sur
// l'autre (graines fixes) : halo de la lune, brume, crête de cendre du sol, fissures de Red Met, dalles,
// corbeaux, particules.
// ArenaBuilder appelle Build (couches en parallaxe, sol, lumières 2D, post-traitement, cendres, braises,
// corbeaux) ; les images calculées ne sont refaites que si elles manquent, ou par le menu RED MET > Régénérer le décor.
public static class RedMoonDecor
{
    private const string ArtFolder = "Assets/_Game/Art/Backgrounds/RedMoon";
    private const string PaintedFolder = "Assets/_Game/Art/Backgrounds/RedMoonPainted";
    private const string ProfilePath = "Assets/_Game/Settings/RedMoon_PostFX.asset";
    private const string RendererPath = "Assets/_Game/Settings/Renderer2D.asset";
    private const string PostProcessDataPath = "Packages/com.unity.render-pipelines.universal/Runtime/Data/PostProcessData.asset";
    private const float Ppu = 64f;

    // Halo, Haze, Mist et CloudSoft sont blancs : le ciel du chapitre les teinte (SkyController).
    private static readonly string[] SpriteNames =
        { "Halo", "Haze", "Mist", "Ground", "Cracks", "Slab", "Dot", "CrowUp", "CrowDown", "Quad", "Bolt1", "Bolt2", "CloudSoft", "Sun", "PaleMoon" };

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
        Material glow = EnsureMaterial("SkyGlow", glowShader, null, 1f);
        Vector2 c = CameraStart;
        var bottom = new Vector2(0.5f, 0f);

        // Ciel collé à la caméra, astres presque immobiles, nuages qui glissent devant. Le ciel du chapitre
        // (SkyController, Data/Sky) remplace les images et les couleurs ; la construction pose le ch. 1.
        Transform sky = Layer(root, "Sky", Painted("sky", 17f, new Vector2(0.5f, 0.5f), false), unlit, new Vector3(c.x, c.y + 0.4f), Vector2.one, -100);
        Transform aurora = Layer(root, "Aurora", art["Quad"], EnsureMaterial("SkyAurora", Shader.Find("RED MET/Sky Aurora"), null, 1f),
            new Vector3(c.x, c.y + 2.2f), new Vector2(0.97f, 0.97f), -98, scale: new Vector2(26f, 5.5f));
        aurora.GetComponent<SpriteRenderer>().enabled = false;

        var celestials = new SkyController.CelestialSlot[3];

        for (int i = 0; i < celestials.Length; i++)
        {
            Transform body = Layer(root, i == 0 ? "Moon" : $"Celestial {i + 1}", Painted("moon", 4.4f, new Vector2(0.5f, 0.5f), false),
                i == 0 ? EnsureMaterial("MoonGlow", glowShader, null, 1.1f) : glow, new Vector3(c.x + 3.6f, c.y + 1.9f), new Vector2(0.95f, 0.95f), -94);
            Transform halo = Layer(body, "Halo", art["Halo"], EnsureMaterial("HaloGlow", glowShader, null, 0.9f), body.position, Vector2.zero, -95);
            halo.gameObject.AddComponent<SkyPulse>();

            var light = new GameObject("Light 2D").AddComponent<Light2D>();
            light.transform.SetParent(body, false);
            light.lightType = Light2D.LightType.Point;
            light.pointLightInnerRadius = 2f;
            light.pointLightOuterRadius = 16f;

            celestials[i] = new SkyController.CelestialSlot
            {
                body = body.GetComponent<SpriteRenderer>(), halo = halo.GetComponent<SpriteRenderer>(), light = light,
            };
            body.gameObject.SetActive(i == 0);
        }

        SpriteRenderer clouds = Tiled(root, "Clouds", Painted("clouds", 10f, new Vector2(0.5f, 0.5f), true), unlit, new Vector3(c.x, c.y + 2.6f), new Vector2(64f, 4.17f),
            new Vector2(0.95f, 0.95f), -93, drift: 0.22f, wrap: 10f, tint: new Color(1f, 1f, 1f, 0.8f));

        // Éclairs : deux traits possibles dans le ciel, et un voile qui illumine le ciel derrière le paysage.
        Transform boltRoot = Layer(root, "Lightning", null, null, new Vector3(c.x, c.y + 0.4f), new Vector2(0.97f, 0.97f), -92);
        var bolts = new SpriteRenderer[2];

        for (int i = 0; i < bolts.Length; i++)
        {
            Transform bolt = Layer(boltRoot, $"Bolt {i + 1}", art[$"Bolt{i + 1}"], EnsureMaterial("BoltGlow", glowShader, null, 2.5f),
                boltRoot.position + new Vector3(0f, 2.2f), Vector2.zero, -92);
            bolts[i] = bolt.GetComponent<SpriteRenderer>();
            bolts[i].enabled = false;
        }

        Transform flash = Layer(root, "Lightning Flash", art["Quad"], unlit, new Vector3(c.x, c.y + 0.4f), Vector2.one, -91, scale: new Vector2(40f, 20f));
        flash.GetComponent<SpriteRenderer>().enabled = false;

        var crows = new GameObject("Crows").AddComponent<CrowFlock>();
        crows.transform.SetParent(root, false);
        var so = new SerializedObject(crows);
        so.FindProperty("wingsUp").objectReferenceValue = art["CrowUp"];
        so.FindProperty("wingsDown").objectReferenceValue = art["CrowDown"];
        so.ApplyModifiedPropertiesWithoutUndo();

        // Montagnes, brume d'horizon, ruines gothiques, brume qui dérive, arbres morts, champ de bataille.
        // Les plans lointains sont petits et posés sur l'horizon ; plus un plan est proche, plus il est sombre,
        // pour que les personnages ressortent toujours (largeur d'une répétition : 2,4 fois sa hauteur).
        // Chaque couche a une version neutre (tools/gen3d/skies.py), colorée par le ciel des ch. 2 à 9.
        var landscape = new List<SkyController.LandscapeLayer>
        {
            Landscape("mountains", Tiled(root, "Mountains", Painted("mountains", 10f, bottom, true), null, new Vector3(c.x, -1.6f), new Vector2(64f, 4.17f),
                new Vector2(0.85f, 0.7f), -80, tint: new Color(0.85f, 0.85f, 0.85f)), new Color(0.85f, 0.85f, 0.85f)),
        };
        SpriteRenderer horizon = Layer(root, "Horizon Haze", art["Haze"], unlit, new Vector3(c.x, -0.7f), new Vector2(0.75f, 0.6f), -76, scale: new Vector2(400f, 0.75f))
            .GetComponent<SpriteRenderer>();
        horizon.color = HorizonRed;
        landscape.Add(Landscape("ruins", Tiled(root, "Ruins", Painted("ruins", 10f, bottom, true), null, new Vector3(c.x, -1.7f), new Vector2(64f, 4.17f),
            new Vector2(0.6f, 0.45f), -70, tint: new Color(0.8f, 0.8f, 0.8f)), new Color(0.8f, 0.8f, 0.8f)));
        SpriteRenderer mist = Tiled(root, "Mist", art["Mist"], unlit, new Vector3(c.x, -1.2f), new Vector2(64f, 2f), new Vector2(0.45f, 0.35f), -65, drift: 0.35f, wrap: 8f,
            tint: MistRed);
        landscape.Add(Landscape("trees", Tiled(root, "Dead Trees", Painted("trees", 11f, bottom, true), null, new Vector3(c.x, -2f), new Vector2(64f, 4.58f),
            new Vector2(0.3f, 0.2f), -60, tint: new Color(0.6f, 0.6f, 0.6f)), new Color(0.6f, 0.6f, 0.6f)));
        landscape.Add(Landscape("battlefield", Tiled(root, "Battlefield", Painted("battlefield", 8f, bottom, true), null, new Vector3(c.x, -2.4f), new Vector2(64f, 3.33f),
            new Vector2(0.12f, 0.08f), -50, tint: new Color(0.55f, 0.55f, 0.55f)), new Color(0.55f, 0.55f, 0.55f)));

        // Sol du plan de jeu (le collider reste celui d'ArenaBuilder) : crête de cendre calculée, terre peinte
        // juste dessous, fissures de Red Met par-dessus, identiques dans tous les chapitres.
        Tiled(root, "Ground Crest", art["Ground"], null, new Vector3(0f, -1.5f), new Vector2(44f, 5f), Vector2.zero, -11);
        Tiled(root, "Ground", Painted("ground", 5f, new Vector2(0.5f, 1f), true), null, new Vector3(0f, -1.66f), new Vector2(44f, 2.5f), Vector2.zero, -10);
        Tiled(root, "Ground Cracks", art["Cracks"], EnsureMaterial("CrackGlow", glowShader, null, 3f), new Vector3(0f, -1.5f), new Vector2(44f, 5f), Vector2.zero, -9);

        // Brouillard qui roule devant le plan de jeu (aube du ch. 7), éteint ailleurs.
        SpriteRenderer frontFog = Tiled(root, "Front Fog", art["Mist"], unlit, new Vector3(-30f, -0.6f), new Vector2(90f, 3.5f), Vector2.zero, 30);
        frontFog.enabled = false;

        // Premier plan sombre et flou, qui passe devant les personnages et défile plus vite que la caméra.
        landscape.Add(Landscape("foreground", Tiled(root, "Foreground", Painted("foreground", 6f, bottom, true), unlit, new Vector3(c.x, -3.6f), new Vector2(90f, 2.5f),
            new Vector2(-0.35f, 0.3f), 40, tint: new Color(1f, 1f, 1f, 0.85f)), new Color(1f, 1f, 1f, 0.85f)));

        Material ash = EnsureMaterial("AshParticle", Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"), art["Dot"].texture, 0f);
        Material ember = EnsureMaterial("EmberGlow", glowShader, art["Dot"].texture, 3f);
        ParticleSystem ashSystem = CreateAsh(camera.transform, ash);
        ParticleSystem emberSystem = CreateEmbers(camera.transform, ember);
        ParticleSystem rainSystem = CreateRain(camera.transform, ash);
        Volume volume = SetupPostProcessing(camera, root);

        // Lune du ch. 1 telle que construite avant les ciels (halo et lumière rouges) : le halo, désormais blanc,
        // prend sa teinte rouge du matériau dans l'éditeur ; en jeu, SkyController la règle par chapitre.
        celestials[0].halo.sharedMaterial.SetColor("_Tint", new Color(1f, 0.25f, 0.15f));
        celestials[0].light.color = new Color(1f, 0.32f, 0.22f);
        celestials[0].light.intensity = 0.8f;

        BuildSkyController(root, SkyProfiles.EnsureAll(art), sky.GetComponent<SpriteRenderer>(), clouds, celestials, landscape, mist, horizon, frontFog,
            aurora.GetComponent<SpriteRenderer>(), flash.GetComponent<SpriteRenderer>(), bolts, ashSystem, emberSystem, rainSystem, volume);
    }

    private static readonly Color MistRed = new(0.42f, 0.1f, 0.1f);
    private static readonly Color HorizonRed = new(0.55f, 0.08f, 0.06f);

    // Couche de paysage et sa version neutre, importée à la même taille et au même pivot que l'originale.
    private static SkyController.LandscapeLayer Landscape(string name, SpriteRenderer renderer, Color baseTint)
    {
        Sprite original = renderer.sprite;
        Sprite neutral = ImportPainted($"{SkyProfiles.Folder}/Neutral/{name}.png", original.bounds.size.x, original.pivot / original.rect.size, true);
        return new SkyController.LandscapeLayer { renderer = renderer, original = original, neutral = neutral, baseTint = baseTint };
    }

    private static void BuildSkyController(Transform root, SkyProfile[] profiles, SpriteRenderer sky, SpriteRenderer clouds,
        SkyController.CelestialSlot[] celestials, List<SkyController.LandscapeLayer> landscape, SpriteRenderer mist, SpriteRenderer horizon,
        SpriteRenderer frontFog, SpriteRenderer aurora, SpriteRenderer flash, SpriteRenderer[] bolts, ParticleSystem ash, ParticleSystem embers,
        ParticleSystem rain, Volume volume)
    {
        var controller = new GameObject("Sky").AddComponent<SkyController>();
        controller.transform.SetParent(root, false);
        var so = new SerializedObject(controller);

        SetArray(so.FindProperty("chapters"), profiles.Length, (p, i) => p.objectReferenceValue = profiles[i]);
        so.FindProperty("cameraStart").vector2Value = CameraStart;
        so.FindProperty("sky").objectReferenceValue = sky;
        so.FindProperty("clouds").objectReferenceValue = clouds;
        SetArray(so.FindProperty("celestials"), celestials.Length, (p, i) =>
        {
            p.FindPropertyRelative("body").objectReferenceValue = celestials[i].body;
            p.FindPropertyRelative("halo").objectReferenceValue = celestials[i].halo;
            p.FindPropertyRelative("light").objectReferenceValue = celestials[i].light;
        });
        SetArray(so.FindProperty("landscape"), landscape.Count, (p, i) =>
        {
            p.FindPropertyRelative("renderer").objectReferenceValue = landscape[i].renderer;
            p.FindPropertyRelative("original").objectReferenceValue = landscape[i].original;
            p.FindPropertyRelative("neutral").objectReferenceValue = landscape[i].neutral;
            p.FindPropertyRelative("baseTint").colorValue = landscape[i].baseTint;
        });
        so.FindProperty("mist").objectReferenceValue = mist;
        so.FindProperty("horizon").objectReferenceValue = horizon;
        so.FindProperty("frontFog").objectReferenceValue = frontFog;
        so.FindProperty("aurora").objectReferenceValue = aurora;
        so.FindProperty("flash").objectReferenceValue = flash;
        SetArray(so.FindProperty("bolts"), bolts.Length, (p, i) => p.objectReferenceValue = bolts[i]);
        so.FindProperty("ash").objectReferenceValue = ash;
        so.FindProperty("embers").objectReferenceValue = embers;
        so.FindProperty("rain").objectReferenceValue = rain;
        so.FindProperty("globalLight").objectReferenceValue = FindGlobalLight();
        so.FindProperty("volume").objectReferenceValue = volume;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetArray(SerializedProperty array, int size, Action<SerializedProperty, int> set)
    {
        array.arraySize = size;

        for (int i = 0; i < size; i++)
            set(array.GetArrayElementAtIndex(i), i);
    }

    private static Light2D FindGlobalLight()
    {
        foreach (Light2D light in UnityEngine.Object.FindObjectsByType<Light2D>(FindObjectsInactive.Include))
            if (light.lightType == Light2D.LightType.Global)
                return light;

        return null;
    }

    // Couche peinte (tools/gen3d/background.py) : l'échelle est réglée pour que l'image fasse widthMeters de large.
    private static Sprite Painted(string name, float widthMeters, Vector2 pivot, bool repeat) =>
        ImportPainted($"{PaintedFolder}/{name}.png", widthMeters, pivot, repeat);

    // Importe une image peinte en sprite de widthMeters de large (aussi pour les ciels, SkyProfiles).
    public static Sprite ImportPainted(string path, float widthMeters, Vector2 pivot, bool repeat)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
        {
            Debug.LogError($"[RED MET] Couche peinte manquante : {path} (lancez tools/gen3d/background.py ou skies.py).");
            return null;
        }

        importer.GetSourceTextureWidthAndHeight(out int width, out int _);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = width / widthMeters;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapModeU = repeat ? TextureWrapMode.Repeat : TextureWrapMode.Clamp;
        importer.wrapModeV = TextureWrapMode.Clamp;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
        int order, Vector2? scale = null, float drift = 0f, float wrap = 0f)
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
            SetFactor(go.AddComponent<ParallaxLayer>(), factor, drift, wrap);

        return go.transform;
    }

    private static SpriteRenderer Tiled(Transform parent, string name, Sprite sprite, Material material, Vector3 position, Vector2 size, Vector2 factor,
        int order, float drift = 0f, float wrap = 0f, Color? tint = null)
    {
        Transform layer = Layer(parent, name, sprite, material, position, factor, order, drift: drift, wrap: wrap);
        var renderer = layer.GetComponent<SpriteRenderer>();
        renderer.color = tint ?? Color.white;
        renderer.drawMode = SpriteDrawMode.Tiled;
        renderer.tileMode = SpriteTileMode.Continuous;
        // Jamais plus haut que l'image : sinon le mode Tiled ajoute en haut une tranche de la répétition
        // suivante (la dernière ligne de l'image), visible comme une ligne nette sur les ciels clairs.
        renderer.size = sprite != null ? new Vector2(size.x, Mathf.Min(size.y, sprite.bounds.size.y)) : size;
        return renderer;
    }

    private static void SetFactor(ParallaxLayer layer, Vector2 factor, float drift, float wrap)
    {
        var so = new SerializedObject(layer);
        so.FindProperty("factor").vector2Value = factor;
        so.FindProperty("drift").floatValue = drift;
        so.FindProperty("wrapWidth").floatValue = wrap;
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
    private static ParticleSystem CreateAsh(Transform camera, Material material)
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
        return ps;
    }

    private static ParticleSystem CreateEmbers(Transform camera, Material material)
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
        return ps;
    }

    // Pluie ou bruine (ch. 2 et 6) : gouttes étirées selon leur vitesse, éteinte dans les autres chapitres.
    private static ParticleSystem CreateRain(Transform camera, Material material)
    {
        ParticleSystem ps = Particles(camera, "Rain", new Vector3(1.5f, 5f, 10f), new Vector3(22f, 0.5f, 0.1f), material, 22);
        ParticleSystem.MainModule main = ps.main;
        main.startLifetime = 0.9f;
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.035f);
        main.startRotation = 0f;
        main.maxParticles = 800;
        main.prewarm = false;
        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        Velocity(ps, new Vector2(-2f, -14f), new Vector2(-2f, -14f));

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.velocityScale = 0.05f;
        renderer.lengthScale = 1f;
        return ps;
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
    private static Volume SetupPostProcessing(Camera camera, Transform parent)
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
        return volume;
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
        Save("Halo", Radial(256, Color.white, 2.2f, 0.9f), new Vector2(0.5f, 0.5f), 32f, false);
        Save("Haze", Haze(), new Vector2(0.5f, 0.5f), 32f, false);
        Save("Mist", Mist(), new Vector2(0.5f, 0.5f), Ppu, true);
        Save("Ground", Ground(), new Vector2(0.5f, 1f), Ppu, true);
        Save("Cracks", Cracks(), new Vector2(0.5f, 1f), Ppu, true);
        Save("Slab", Slab(), new Vector2(0.5f, 1f), Ppu, false, new Vector4(24f, 0f, 24f, 0f));
        Save("Dot", Radial(32, Color.white, 1.5f, 1f), new Vector2(0.5f, 0.5f), 32f, false);
        Save("CrowUp", Crow(true), new Vector2(0.5f, 0.5f), 100f, false);
        Save("CrowDown", Crow(false), new Vector2(0.5f, 0.5f), 100f, false);
        Save("Quad", Solid(4, 4), new Vector2(0.5f, 0.5f), 4f, false);
        Save("Bolt1", Bolt(61), new Vector2(0.5f, 1f), 64f, false);
        Save("Bolt2", Bolt(62), new Vector2(0.5f, 1f), 64f, false);
        Save("CloudSoft", CloudSoft(), new Vector2(0.5f, 0.5f), 51.2f, true);
        Save("Sun", Sun(), new Vector2(0.5f, 0.5f), 64f, false);
        Save("PaleMoon", PaleMoon(), new Vector2(0.5f, 0.5f), 64f, false);
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

    // Brume qui dérive : bande de nappes douces, répétable en largeur (512 px pour 8 m).
    private static Pixels Mist()
    {
        const int w = 512, h = 128;
        var image = new Pixels(w, h);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = (y + 0.5f) / h;
            float n = Noise((x + 0.5f) / w, 91, 4, 4, v * 2f, 2);
            float bell = Mathf.Sin(Mathf.PI * v);
            image.Set(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(n * 1.6f - 0.45f) * bell * bell * 0.4f));
        }

        return image;
    }

    // Corbeau en silhouette, ailes hautes ou basses (64 × 32 px, teinté par le SpriteRenderer).
    private static Pixels Crow(bool wingsUp)
    {
        const int w = 64, h = 32;
        var mask = new float[w * h];
        var body = new Vector2(32f, 15f);
        Stroke(mask, w, h, body + new Vector2(-9f, 0f), body + new Vector2(9f, 1f), 3.2f, 2f);
        Stroke(mask, w, h, body + new Vector2(9f, 1f), body + new Vector2(14f, 1.5f), 2.2f, 0.8f);
        Stroke(mask, w, h, body + new Vector2(-9f, 0f), body + new Vector2(-15f, -1f), 2f, 2.8f);
        float tip = wingsUp ? 13f : -12f;

        foreach (float side in new[] { -1f, 1f })
        {
            Vector2 elbow = body + new Vector2(side * 2f - 3f, tip * 0.6f);
            Stroke(mask, w, h, body + new Vector2(-1f, 1f), elbow, 2.6f, 1.8f);
            Stroke(mask, w, h, elbow, elbow + new Vector2(side * 4f - 6f, tip * 0.45f), 1.8f, 0.6f);
        }

        var image = new Pixels(w, h);

        for (int i = 0; i < mask.Length; i++)
            image.P[i] = new Color(1f, 1f, 1f, mask[i]);

        return image;
    }

    private static Pixels Solid(int w, int h)
    {
        var image = new Pixels(w, h);

        for (int i = 0; i < image.P.Length; i++)
            image.P[i] = Color.white;

        return image;
    }

    // Éclair : trait brisé qui descend en zigzag avec deux ramifications, cœur net et halo (64 × 512 px, 1 × 8 m).
    private static Pixels Bolt(int seed)
    {
        const int w = 64, h = 512;
        var core = new float[w * h];
        var halo = new float[w * h];
        var p = new Vector2(w / 2f, h - 4f);

        for (int s = 0; p.y > 8f; s++)
        {
            Vector2 next = p + new Vector2((Hash(s, seed) - 0.5f) * 14f, -10f - 10f * Hash(s, seed + 1));
            next.x = Mathf.Clamp(next.x, 10f, w - 10f);
            float width = Mathf.Lerp(1.8f, 0.8f, 1f - p.y / h);
            Stroke(core, w, h, p, next, width, width);
            Stroke(halo, w, h, p, next, width * 4f, width * 4f);

            if (s == 8 || s == 17)
            {
                Vector2 q = next;
                float side = Hash(s, seed + 2) < 0.5f ? -1f : 1f;

                for (int b = 0; b < 6; b++)
                {
                    Vector2 r = q + new Vector2(side * (3f + 4f * Hash(b, seed + 3)), -9f);
                    r.x = Mathf.Clamp(r.x, 4f, w - 4f);
                    Stroke(core, w, h, q, r, 0.8f, 0.5f);
                    Stroke(halo, w, h, q, r, 2.5f, 2f);
                    q = r;
                }
            }

            p = next;
        }

        var image = new Pixels(w, h);

        for (int i = 0; i < core.Length; i++)
            image.P[i] = new Color(1f, 1f, 1f, Mathf.Max(core[i], halo[i] * 0.3f));

        return image;
    }

    // Nuages doux en bande, répétables en largeur (512 px pour 10 m) : la couleur vient du ciel du chapitre.
    private static Pixels CloudSoft()
    {
        const int w = 512, h = 214;
        var image = new Pixels(w, h);

        for (int y = 0; y < h; y++)
        for (int x = 0; x < w; x++)
        {
            float v = (y + 0.5f) / h;
            float n = Noise((x + 0.5f) / w, 71, 5, 5, v * 2.5f, 2);
            float bell = Mathf.Sin(Mathf.PI * v);
            image.Set(x, y, new Color(1f, 1f, 1f, Mathf.Clamp01(n * 2.2f - 0.95f) * bell));
        }

        return image;
    }

    // Soleil : disque net et blanc, couronne douce (256 px ; le disque occupe la moitié centrale).
    // Les soleils peints par Animagine sortaient en disques mécaniques : celui-ci est calculé.
    private static Pixels Sun()
    {
        const int size = 256;
        var image = new Pixels(size, size);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(size / 2f, size / 2f)) / (size / 2f);
            float disc = Mathf.Clamp01((0.5f - d) * 40f);
            float corona = Mathf.Pow(Mathf.Clamp01(1f - d), 3f) * 0.6f;
            image.Set(x, y, new Color(1f, 1f, 1f, Mathf.Max(disc, corona)));
        }

        return image;
    }

    // Lune pâle : disque gris clair, mers plus sombres, bord assombri (256 px, disque plein).
    private static Pixels PaleMoon()
    {
        const int size = 256;
        var image = new Pixels(size, size);

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            var p = new Vector2(x + 0.5f, y + 0.5f) / (size / 2f) - Vector2.one;
            float d = p.magnitude;
            float alpha = Mathf.Clamp01((0.96f - d) * 60f);

            if (alpha <= 0f)
                continue;

            float maria = Noise(p.x * 0.5f + 0.5f, 33, 3, 3, p.y * 1.5f + 1.5f, 3);
            float shade = Mathf.Lerp(1f, 0.72f, Mathf.Clamp01((maria - 0.45f) * 3f)) * Mathf.Lerp(1f, 0.8f, d * d);
            image.Set(x, y, new Color(shade, shade, shade * 1.04f, alpha));
        }

        return image;
    }

    private static Pixels Haze()
    {
        var image = new Pixels(8, 128);

        for (int y = 0; y < image.H; y++)
        {
            float s = Mathf.Sin(Mathf.PI * (y + 0.5f) / image.H);
            var c = new Color(1f, 1f, 1f, s * s * 0.45f);

            for (int x = 0; x < image.W; x++)
                image.Set(x, y, c);
        }

        return image;
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

        // Fissures nombreuses qui partent juste sous la crête (la partie du sol visible à l'écran), s'affinent
        // en descendant et se ramifient, avec un halo large pour le bloom.
        for (int k = 0; k < 11; k++)
        {
            var p = new Vector2((k + Hash(k, 51)) * w / 11f, h - 12f - 22f * Hash(k, 52));
            float heading = -90f + 80f * (Hash(k, 53) - 0.5f);
            int steps = 45 + Mathf.FloorToInt(40f * Hash(k, 54));

            for (int s = 0; s < steps && p.y > 8f; s++)
            {
                heading = Mathf.Clamp(heading + 50f * (Hash(s, 55 + k) - 0.5f), -165f, -15f);
                Vector2 next = p + new Vector2(Mathf.Cos(heading * Mathf.Deg2Rad), Mathf.Sin(heading * Mathf.Deg2Rad)) * 3f;
                float width = Mathf.Lerp(2f, 0.9f, s / (float)steps);
                Stroke(core, w, h, p, next, width, width);
                Stroke(halo, w, h, p, next, width * 3.5f, width * 3.5f);

                if (s == steps / 3)
                {
                    float branch = heading + (Hash(k, 56) < 0.5f ? 50f : -50f);
                    Vector2 q = p;

                    for (int b = 0; b < 14; b++)
                    {
                        Vector2 r = q + new Vector2(Mathf.Cos(branch * Mathf.Deg2Rad), Mathf.Sin(branch * Mathf.Deg2Rad)) * 3f;
                        Stroke(core, w, h, q, r, 1f, 0.7f);
                        Stroke(halo, w, h, q, r, 3f, 2.5f);
                        q = r;
                    }
                }

                p = next;
            }
        }

        var image = new Pixels(w, h);

        for (int i = 0; i < core.Length; i++)
            image.P[i] = new Color(1f, 0.2f, 0.08f, Mathf.Max(core[i], halo[i] * 0.35f));

        return image;
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
