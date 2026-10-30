using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Menu RED MET > Construire l'arène 2,5D (étude) : scène Assets/_Game/Scenes/Arena25D.unity, beat'em up 2,5D
// (docs/ETUDE_25D.md). Ne touche ni à Arena.unity ni à ArenaBuilder : seuls des composants nouveaux (Scripts/Brawler25D)
// et les données existantes (AttackData de Nathan, archétypes d'ennemis, contrôleurs d'animation) sont utilisés.
// Décor : tools/gen3d/street25d.py ; ciel du ch. 3 (Art/Backgrounds/Skies/ch03_sky.png), sans lune rouge.
public static class Arena25DBuilder
{
    private const string Root = "Assets/_Game";
    private const string ScenePath = Root + "/Scenes/Arena25D.unity";
    private const string ArtFolder = Root + "/Art/Backgrounds/Street25D";
    private const string AttackFolder = Root + "/Data/Attacks/Nathan25D";
    private const string NathanAttacks = Root + "/Data/Attacks/Nathan";
    private const string Archetypes = Root + "/Data/Enemies";
    private const string SoundFolder = Root + "/Audio/Voice";

    // Sol : bord avant et bord arrière (pied des façades). Caméra : 6,8 m de haut, centrée à 1,2 m.
    private const float Front = -2.1f;
    private const float Back = 0.1f;
    private const float CameraY = 1.2f;
    // Façades : 16 m peints (96 px/m), affichés à 0,8 (au fond de la rue) : 12,8 m de large.
    private const float FacadeScale = 0.8f;
    private const float FacadeWidth = 16f * FacadeScale;
    private static readonly float[] FacadeX = { -6.4f, 6.4f };
    private static readonly float[] ZoneX = { 0f, 13f };
    private const int PoolSize = 10;

    private const int SkyOrder = -3000;
    private const int FacadeOrder = -2000;
    private const int OpeningOrder = -1900;
    private const int FloorOrder = -1000;
    private const int ShadowOrder = 500;
    private const int ForegroundOrder = 5000;

    [MenuItem("RED MET/Construire l'arène 2,5D (étude)")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        ImportArt();

        var light = new GameObject("Global Light 2D").AddComponent<Light2D>();
        light.lightType = Light2D.LightType.Global;
        light.color = new Color(1f, 0.93f, 0.85f);

        var playfield = new GameObject("Playfield").AddComponent<Playfield25D>();
        SetFloat(playfield, "front", Front);
        SetFloat(playfield, "back", Back);

        Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Placeholder/Square.png");
        Sprite dot = Art("shadow");
        var flash = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/Shaders/SpriteFlash.mat");
        var lineMaterial = AssetDatabase.LoadAssetAtPath<Material>(Root + "/Art/VFX/TelegraphLine.mat");

        var shadows = new GameObject("Ombres").transform;
        var telegraphs = new GameObject("Annonces").transform;

        List<SpawnPoint25D>[] entrances = BuildDecor(square);
        Body25D player = BuildPlayer(flash, dot, shadows);
        Enemy25D[] pool = BuildEnemyPool(flash, dot, shadows, telegraphs, lineMaterial);
        BuildProps(dot, shadows);

        var anchor = new GameObject("CameraAnchor").transform;
        anchor.position = new Vector3(player.transform.position.x, CameraY, 0f);
        Camera camera = BuildCamera(anchor);

        HUD25D hud = BuildHud(player, out Image flashOverlay);

        var vfx = new GameObject("CombatVfx").AddComponent<CombatVfx>();
        SetObject(vfx, "particleMaterial", lineMaterial);
        SetObject(vfx, "square", square);
        SetObject(vfx, "flashOverlay", flashOverlay);
        // Pas de sol physique en 2,5D : le sang et les morceaux ne se posent pas (voir docs/ETUDE_25D.md).
        SetInt(vfx, "bloodCollisionLayers", 0);
        SetInt(vfx, "debrisLayer", Math.Max(0, LayerMask.NameToLayer("Debris")));
        SetObjects(vfx, "fleshChunks", new UnityEngine.Object[]
        {
            Gore("Generic", "chunk1"), Gore("Generic", "chunk2"), Gore("Generic", "chunk3"),
        });
        SetObject(vfx, "guts", Gore("Generic", "guts"));
        SetObject(vfx, "goreSound", Sound("Gore_Burst"));

        new GameObject("GameAudio").AddComponent<GameAudio>();

        var director = new GameObject("Director").AddComponent<ArenaDirector25D>();
        SetObject(director, "player", player);
        SetObject(director, "cameraFollow", camera.GetComponent<CameraFollow>());
        SetObject(director, "cameraAnchor", anchor);
        SetFloat(director, "cameraY", CameraY);
        SetObject(director, "hud", hud);
        SetObjects(director, "pool", pool.Cast<UnityEngine.Object>().ToArray());
        SetObjects(director, "dropShadows", Enumerable.Range(0, 3)
            .Select(i => (UnityEngine.Object)CreateShadow($"Annonce chute {i}", dot, shadows, false)).ToArray());
        SetObject(director, "runSound", Sound("SFX_Course"));
        // Pas de bruit de toit propre pour l'instant (limite du forfait ElevenLabs) : la course sert d'annonce.
        SetObject(director, "roofSound", Sound("SFX_Course"));
        SetObject(director, "landSound", Sound("SFX_Reception"));
        SetObject(director, "goSound", Sound("SFX_Go"));
        SetZones(director, entrances);
        SetObjects(director, "benchmarkArchetypes", new UnityEngine.Object[]
        {
            Archetype("01_Voyou_MainsNues"), Archetype("01_Voyou_Barre"), Archetype("03_FlicCorrompu"), Archetype("02_Dealer"),
        });
        SetObject(hud, "director", director);

        var bench = new GameObject("Benchmark25D").AddComponent<Benchmark25D>();
        SetObject(bench, "director", director);
        SetObject(bench, "player", player);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player.gameObject;
        Debug.Log("[RED MET] Arène 2,5D construite : " + ScenePath + ". Appuie sur Play pour tester.");
    }

    // ------------------------------------------------------------------ Décor

    private static void ImportArt()
    {
        ImportSprite(ArtFolder + "/facade_a.png", 96f, Vector2.zero);
        ImportSprite(ArtFolder + "/facade_b.png", 96f, Vector2.zero);
        ImportSprite(ArtFolder + "/floor.png", 96f, new Vector2(0.5f, 1f));
        ImportSprite(ArtFolder + "/foreground_rubble.png", 320f, new Vector2(0.5f, 0.02f));
        ImportSprite(ArtFolder + "/foreground_post.png", 244f, new Vector2(0.45f, 0f));
        ImportSprite(ArtFolder + "/crate.png", 710f, new Vector2(0.5f, 0.18f));
        ImportSprite(ArtFolder + "/bar.png", 860f, new Vector2(0.5f, 0.5f));
        ImportSprite(ArtFolder + "/food.png", 1700f, new Vector2(0.5f, 0.3f));
        // Ombre ovale (générée par script : disque plein, bord doux), 1 m de large.
        ImportSprite(ArtFolder + "/shadow.png", 128f, new Vector2(0.5f, 0.5f));
    }

    private static void ImportSprite(string path, float pixelsPerUnit, Vector2 pivot)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
        {
            AssetDatabase.ImportAsset(path);
            importer = AssetImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {
                Debug.LogError("[RED MET] Image introuvable : " + path + " (tools/gen3d/street25d.py).");
                return;
            }
        }

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerUnit;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;
        importer.maxTextureSize = 2048;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = pivot;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    private static Sprite Art(string name) => AssetDatabase.LoadAssetAtPath<Sprite>($"{ArtFolder}/{name}.png");

    // Ciel du ch. 3 au loin, façades au fond (avec leurs entrées), sol pavé, premier plan qui passe devant.
    private static List<SpawnPoint25D>[] BuildDecor(Sprite square)
    {
        var root = new GameObject("Décor").transform;

        Sprite sky = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Backgrounds/Skies/ch03_sky.png");
        GameObject skyGo = CreateSprite("Ciel ch3", root, sky, Color.white, new Vector3(6.5f, 3.4f), Vector3.one, SkyOrder);
        float skyScale = 26f / sky.bounds.size.x;
        skyGo.transform.localScale = new Vector3(skyScale, skyScale, 1f);
        SetVector2(skyGo.AddComponent<ParallaxLayer>(), "factor", new Vector2(0.85f, 0f));

        string[] facades = { "facade_a", "facade_b" };

        for (int i = 0; i < facades.Length; i++)
            CreateSprite("Façade " + (i + 1), root, Art(facades[i]), Color.white, new Vector3(FacadeX[i], Back - 0.05f),
                new Vector3(FacadeScale, FacadeScale), FacadeOrder);

        for (int i = 0; i < 3; i++)
        {
            Sprite floor = Art("floor");
            float height = Back - (Front - 0.25f);
            float scaleY = height / floor.bounds.size.y;
            // Sol assombri : les personnages doivent ressortir dessus. Une dalle sur deux en miroir : l'image n'est pas
            // répétable, ses bords se raccordent ainsi sans cassure.
            CreateSprite("Sol " + (i + 1), root, floor, new Color(0.55f, 0.5f, 0.52f), new Vector3(-8f + i * 16f + 8f, Back + 0.1f),
                new Vector3(i % 2 == 0 ? 1f : -1f, scaleY), FloorOrder);
        }

        CreateSprite("Premier plan gravats", root, Art("foreground_rubble"), Color.white, new Vector3(-4.6f, Front - 0.75f), Vector3.one, ForegroundOrder);
        CreateSprite("Premier plan lampadaire", root, Art("foreground_post"), Color.white, new Vector3(4.9f, Front - 0.7f), Vector3.one, ForegroundOrder);
        CreateSprite("Premier plan gravats 2", root, Art("foreground_rubble"), Color.white, new Vector3(18.2f, Front - 0.8f), new Vector3(-1f, 1f), ForegroundOrder);
        CreateSprite("Premier plan lampadaire 2", root, Art("foreground_post"), Color.white, new Vector3(15.6f, Front - 0.7f), new Vector3(-1f, 1f), ForegroundOrder);

        // Entrées peintes sur les façades (positions de tools/gen3d/street25d.py, en mètres peints × 0,8).
        var entrances = new List<SpawnPoint25D>[ZoneX.Length];

        for (int i = 0; i < FacadeX.Length; i++)
        {
            float x0 = FacadeX[i];
            entrances[i] = new List<SpawnPoint25D>
            {
                Entrance("Porte", root, square, ArenaDirector25D.Entry.Door, x0 + 9.1f * FacadeScale, 0f,
                    new Vector2(2.0f, 2.4f) * FacadeScale, new Color(0.02f, 0.01f, 0.01f, 0.95f), Sound("SFX_Porte")),
                Entrance("Fenêtre", root, square, ArenaDirector25D.Entry.Window, x0 + 3.6f * FacadeScale, 2.4f * FacadeScale,
                    new Vector2(1.2f, 1.2f) * FacadeScale, new Color(0.03f, 0.02f, 0.02f, 0.95f), Sound("SFX_Vitre")),
                Entrance("Ruelle", root, square, ArenaDirector25D.Entry.Alley, x0 + 5.8f * FacadeScale, 0f,
                    new Vector2(0.9f, 2.6f) * FacadeScale, new Color(0.35f, 0.02f, 0.02f, 0.55f), Sound("SFX_Course")),
            };
        }

        return entrances;
    }

    private static SpawnPoint25D Entrance(string name, Transform parent, Sprite square, ArenaDirector25D.Entry kind, float x,
        float sill, Vector2 size, Color color, AudioClip sound)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = new Vector3(x, Back, 0f);

        // Ouverture : rectangle de la porte, de la vitre ou de la ruelle, posé sur la façade.
        float bottom = Back - 0.05f + sill;
        GameObject opening = CreateSprite("Ouverture", go.transform, square, color, Vector3.zero, Vector3.one, OpeningOrder);
        opening.transform.position = new Vector3(x, bottom + size.y * 0.5f, 0f);
        Vector2 spriteSize = square.bounds.size;
        opening.transform.localScale = new Vector3(size.x / spriteSize.x, size.y / spriteSize.y, 1f);

        var point = go.AddComponent<SpawnPoint25D>();
        SetEnum(point, "kind", (int)kind);
        SetObject(point, "opening", opening.GetComponent<SpriteRenderer>());
        SetFloat(point, "height", sill);
        SetObject(point, "sound", sound);
        return point;
    }

    // ------------------------------------------------------------ Personnages

    private static Body25D BuildPlayer(Material flash, Sprite dot, Transform shadows)
    {
        var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(Root + "/Animations/Nathan_M0/Nathan_M0.controller");
        GameObject go = CreateFighter("Nathan", Body25D.Team.Player, new Vector2(-3f, -1f), controller, flash, dot, shadows,
            out SpriteRenderer body, out Body25D body25);

        var health = go.GetComponent<Health>();
        SetInt(health, "maxHealth", 100);

        var weapon = CreateSprite("Barre tenue", body.transform, Art("bar"), Color.white, new Vector3(0.3f, 1.05f), Vector3.one * 0.9f, 1);

        var brawler = go.AddComponent<Brawler25D>();
        SetObject(brawler, "spriteRenderer", body);

        var combat = go.AddComponent<BrawlerCombat25D>();
        SetObject(combat, "jab", NathanAttack("N01_Jab"));
        SetObject(combat, "cross", NathanAttack("N02_Direct"));
        SetObject(combat, "hook", NathanAttack("N03_Crochet"));
        SetObject(combat, "heavy", NathanAttack("N04_CoupLourd"));
        SetObject(combat, "launcher", NathanAttack("N05_Lanceur"));
        SetObject(combat, "airLight1", NathanAttack("N06_Air1"));
        SetObject(combat, "airLight2", NathanAttack("N07_Air2"));
        SetObject(combat, "airLight3", NathanAttack("N08_Air3"));
        SetObject(combat, "smash", NathanAttack("N09_Smash"));
        SetObject(combat, "shotgun", NathanAttack("N10_FusilAPompe"));
        SetObject(combat, "shellSound", Sound("Shotgun_Shell"));
        SetObject(combat, "weaponSprite", weapon.GetComponent<SpriteRenderer>());
        SetObjects(combat, "weaponCombo", new UnityEngine.Object[] { BarAttack("N20_Barre1", 1, 0f), BarAttack("N21_Barre2", 2, 0.1f) });

        go.AddComponent<CharacterAnimator25D>();
        SetObject(go.GetComponent<CharacterAnimator25D>(), "animator", body.GetComponent<Animator>());

        var gibs = go.AddComponent<GibSource>();
        SetObject(gibs, "body", body);
        SetObject(gibs, "set", AssetDatabase.LoadAssetAtPath<GibSet>(Root + "/Data/Gibs/NathanM0.asset"));
        return body25;
    }

    private static Enemy25D[] BuildEnemyPool(Material flash, Sprite dot, Transform shadows, Transform telegraphs, Material lineMaterial)
    {
        var root = new GameObject("Réserve d'ennemis").transform;
        var pool = new Enemy25D[PoolSize];

        for (int i = 0; i < PoolSize; i++)
        {
            GameObject go = CreateFighter($"Ennemi {i + 1:00}", Body25D.Team.Enemy, new Vector2(0f, -30f), null, flash, dot, shadows,
                out SpriteRenderer body, out _);
            go.transform.SetParent(root, true);

            var cone = new GameObject($"Cône {i + 1:00}").AddComponent<LineRenderer>();
            cone.transform.SetParent(telegraphs, false);
            cone.sharedMaterial = lineMaterial;
            cone.loop = true;
            cone.useWorldSpace = true;
            cone.widthMultiplier = 0.05f;
            cone.sortingOrder = ShadowOrder + 10;
            cone.enabled = false;

            var gibs = go.AddComponent<GibSource>();
            SetObject(gibs, "body", body);

            var enemy = go.AddComponent<Enemy25D>();
            SetObject(enemy, "body", body);
            SetObject(enemy, "animator", body.GetComponent<Animator>());
            SetObject(enemy, "coneLine", cone);
            SetObject(enemy, "gibs", gibs);

            go.AddComponent<CharacterAnimator25D>();
            SetObject(go.GetComponent<CharacterAnimator25D>(), "animator", body.GetComponent<Animator>());

            // En réserve : inactif, et son ombre aussi (elle n'est pas un enfant du personnage).
            go.GetComponent<Body25D>().Shadow.enabled = false;
            go.SetActive(false);
            pool[i] = enemy;
        }

        return pool;
    }

    // Combattant : racine au sol (Rigidbody2D cinématique, Body25D, Health, HitStop, réaction), visuel au pivot des pieds
    // (sprite, Animator, capsule de corps pour les effets), ombre à part. Ordre d'affichage par SortingGroup.
    private static GameObject CreateFighter(string name, Body25D.Team team, Vector2 position, RuntimeAnimatorController controller,
        Material flash, Sprite dot, Transform shadows, out SpriteRenderer body, out Body25D body25)
    {
        var go = new GameObject(name);
        go.transform.position = position;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;

        var group = go.AddComponent<SortingGroup>();

        var visual = new GameObject("Visual");
        visual.transform.SetParent(go.transform, false);
        body = visual.AddComponent<SpriteRenderer>();
        body.sharedMaterial = flash;

        if (controller != null)
        {
            visual.AddComponent<Animator>().runtimeAnimatorController = controller;
            body.sprite = AssetDatabase.LoadAllAssetsAtPath(Root + "/Art/Characters/Nathan_M0/Sprites/NathanM0_idle.png").OfType<Sprite>().FirstOrDefault();
        }
        else
        {
            visual.AddComponent<Animator>();
        }

        var capsule = visual.AddComponent<CapsuleCollider2D>();
        capsule.isTrigger = true;
        capsule.size = new Vector2(0.7f, 1.7f);
        capsule.offset = new Vector2(0f, 0.85f);

        go.AddComponent<HitStop>();
        go.AddComponent<Health>();
        go.AddComponent<DamageScale>();
        SpriteRenderer shadow = CreateShadow(name + " (ombre)", dot, shadows, true);

        body25 = go.AddComponent<Body25D>();
        SetEnum(body25, "team", (int)team);
        SetObject(body25, "visual", visual.transform);
        SetObject(body25, "shadow", shadow);
        SetObject(body25, "sortingGroup", group);

        var reaction = go.AddComponent<HitReaction25D>();
        SetObject(reaction, "body", body);
        return go;
    }

    private static SpriteRenderer CreateShadow(string name, Sprite dot, Transform parent, bool enabled)
    {
        GameObject go = CreateSprite(name, parent, dot, new Color(0f, 0f, 0f, 0.7f), Vector3.zero, new Vector3(0.9f, 0.3f), ShadowOrder);
        var renderer = go.GetComponent<SpriteRenderer>();
        renderer.enabled = enabled;
        return renderer;
    }

    // ------------------------------------------------------------------ Objets

    private static void BuildProps(Sprite dot, Transform shadows)
    {
        var root = new GameObject("Objets").transform;
        Pickup(root, "Barre de fer", Pickup25D.Kind.Weapon, 8, Art("bar"), new Vector2(-2.2f, -1.4f), true);
        Pickup(root, "Barre de fer 2", Pickup25D.Kind.Weapon, 8, Art("bar"), new Vector2(11.5f, -0.5f), true);
        Crate(root, "Caisse", new Vector2(3.6f, -0.4f), dot, shadows);
        Crate(root, "Caisse 2", new Vector2(16.4f, -1.6f), dot, shadows);
    }

    private static Pickup25D Pickup(Transform parent, string name, Pickup25D.Kind kind, int amount, Sprite sprite, Vector2 position, bool active)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;
        GameObject visual = CreateSprite("Visuel", go.transform, sprite, Color.white, new Vector3(0f, 0.08f), Vector3.one, 0);
        var pickup = go.AddComponent<Pickup25D>();
        SetEnum(pickup, "kind", (int)kind);
        SetInt(pickup, "amount", amount);
        SetObject(pickup, "sprite", visual.GetComponent<SpriteRenderer>());
        go.SetActive(active);
        return pickup;
    }

    private static void Crate(Transform parent, string name, Vector2 position, Sprite dot, Transform shadows)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.position = position;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        var group = go.AddComponent<SortingGroup>();
        GameObject visual = CreateSprite("Visual", go.transform, Art("crate"), Color.white, Vector3.zero, Vector3.one, 0);
        var capsule = visual.AddComponent<BoxCollider2D>();
        capsule.isTrigger = true;
        capsule.size = new Vector2(0.9f, 0.75f);
        capsule.offset = new Vector2(0f, 0.37f);

        go.AddComponent<HitStop>();
        var health = go.AddComponent<Health>();
        SetInt(health, "maxHealth", 20);

        var body = go.AddComponent<Body25D>();
        SetEnum(body, "team", (int)Body25D.Team.Neutral);
        SetObject(body, "visual", visual.transform);
        SetObject(body, "shadow", CreateShadow(name + " (ombre)", dot, shadows, true));
        SetObject(body, "sortingGroup", group);
        SetFloat(body, "bodyHeight", 0.75f);
        SetFloat(body, "bodyWidth", 0.9f);
        SetFloat(body, "shadowWidth", 1.1f);

        Pickup25D food = Pickup(parent, name + " (nourriture)", Pickup25D.Kind.Food, 30, Art("food"), position, false);
        var breakable = go.AddComponent<Breakable25D>();
        SetObject(breakable, "sprite", visual.GetComponent<SpriteRenderer>());
        SetObject(breakable, "content", food);
        SetObject(breakable, "breakSound", Sound("SFX_Caisse"));
    }

    // --------------------------------------------------------- Vagues et zones

    private static void SetZones(ArenaDirector25D director, List<SpawnPoint25D>[] entrances)
    {
        EnemyArchetype voyou = Archetype("01_Voyou_MainsNues");
        EnemyArchetype barre = Archetype("01_Voyou_Barre");
        EnemyArchetype couteau = Archetype("01_Voyou_Couteau");
        EnemyArchetype flic = Archetype("03_FlicCorrompu");
        EnemyArchetype dealer = Archetype("02_Dealer");

        var zones = new[]
        {
            new ArenaDirector25D.Zone
            {
                name = "ZONE 1", centerX = ZoneX[0], entrances = entrances[0].ToArray(),
                waves = new[]
                {
                    Wave(voyou, voyou, voyou),
                    Wave(voyou, barre, voyou, flic, voyou),
                },
            },
            new ArenaDirector25D.Zone
            {
                name = "ZONE 2", centerX = ZoneX[1], entrances = entrances[1].ToArray(),
                waves = new[]
                {
                    Wave(voyou, couteau, voyou, barre),
                    Wave(dealer, voyou, flic, barre, voyou, couteau, voyou, flic),
                },
            },
        };

        var so = new SerializedObject(director);
        SerializedProperty list = so.FindProperty("zones");
        list.arraySize = zones.Length;

        for (int z = 0; z < zones.Length; z++)
        {
            SerializedProperty zone = list.GetArrayElementAtIndex(z);
            zone.FindPropertyRelative("name").stringValue = zones[z].name;
            zone.FindPropertyRelative("centerX").floatValue = zones[z].centerX;
            SerializedProperty points = zone.FindPropertyRelative("entrances");
            points.arraySize = zones[z].entrances.Length;

            for (int i = 0; i < zones[z].entrances.Length; i++)
                points.GetArrayElementAtIndex(i).objectReferenceValue = zones[z].entrances[i];

            SerializedProperty waves = zone.FindPropertyRelative("waves");
            waves.arraySize = zones[z].waves.Length;

            for (int w = 0; w < zones[z].waves.Length; w++)
            {
                SerializedProperty enemies = waves.GetArrayElementAtIndex(w).FindPropertyRelative("enemies");
                EnemyArchetype[] wave = zones[z].waves[w].enemies;
                enemies.arraySize = wave.Length;

                for (int e = 0; e < wave.Length; e++)
                    enemies.GetArrayElementAtIndex(e).objectReferenceValue = wave[e];
            }
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static ArenaDirector25D.Wave Wave(params EnemyArchetype[] enemies) => new() { enemies = enemies };

    private static EnemyArchetype Archetype(string file) => AssetDatabase.LoadAssetAtPath<EnemyArchetype>($"{Archetypes}/{file}.asset");

    // ---------------------------------------------------------------- Attaques

    private static AttackData NathanAttack(string file)
    {
        var attack = AssetDatabase.LoadAssetAtPath<AttackData>($"{NathanAttacks}/{file}.asset");

        if (attack == null)
            Debug.LogError($"[RED MET] Attaque introuvable : {file} (construire d'abord l'arène latérale).");

        return attack;
    }

    // Barre de fer : plus d'allonge et de dégâts que les poings, mêmes clips que le direct et le crochet.
    private static AttackData BarAttack(string file, int animationId, float extraReach)
    {
        if (!AssetDatabase.IsValidFolder(AttackFolder))
            AssetDatabase.CreateFolder(Root + "/Data/Attacks", "Nathan25D");

        string path = $"{AttackFolder}/{file}.asset";
        var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

        if (attack != null)
            return attack;

        attack = ScriptableObject.CreateInstance<AttackData>();
        attack.displayName = animationId == 1 ? "Barre de fer (revers)" : "Barre de fer (abattu)";
        attack.animationId = animationId;
        attack.startup = 8;
        attack.active = 4;
        attack.recovery = 16;
        attack.cancelWindow = new Vector2Int(10, 26);
        attack.damage = animationId == 1 ? 18 : 24;
        attack.damageType = DamageType.Blunt;
        attack.hitboxOffset = new Vector2(0.85f + extraReach, 0.3f);
        attack.hitboxSize = new Vector2(1.3f, 1f);
        attack.depthTolerance = 0.45f;
        attack.step = 0.3f;
        attack.hitstop = animationId == 1 ? 6 : 9;
        attack.shake = animationId == 1 ? 0.2f : 0.4f;
        attack.knockback = animationId == 1 ? KnockbackType.Light : KnockbackType.Heavy;
        attack.knockbackVelocity = animationId == 1 ? new Vector2(5f, 0f) : new Vector2(9f, 4f);
        attack.hitstun = animationId == 1 ? 20 : 28;
        AssetDatabase.CreateAsset(attack, path);
        return attack;
    }

    // ------------------------------------------------------------- Caméra, HUD

    private static Camera BuildCamera(Transform anchor)
    {
        var go = new GameObject("Main Camera") { tag = "MainCamera" };
        // Départ sur la zone 1 : le ciel en parallaxe se cale sur cette position.
        go.transform.position = new Vector3(ZoneX[0], CameraY, -10f);
        var camera = go.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 3.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.08f, 0.1f);
        go.AddComponent<AudioListener>();

        var follow = go.AddComponent<CameraFollow>();
        SetObject(follow, "target", anchor);
        SetVector2(follow, "offset", Vector2.zero);
        SetFloat(follow, "minY", -100f);
        SetFloat(follow, "minX", ZoneX[0]);
        SetFloat(follow, "maxX", ZoneX[0]);
        return camera;
    }

    private static HUD25D BuildHud(Body25D player, out Image flashOverlay)
    {
        var canvasGo = new GameObject("HUD25D", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        Transform canvas = canvasGo.transform;

        RectTransform barBack = Rect("Vie", canvas, new Vector2(0f, 1f), new Vector2(40f, -40f), new Vector2(420f, 26f));
        barBack.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.65f);
        RectTransform fill = Rect("Remplissage", barBack, new Vector2(0f, 0.5f), new Vector2(3f, 0f), new Vector2(414f, 20f));
        fill.pivot = new Vector2(0f, 0.5f);
        fill.gameObject.AddComponent<Image>().color = new Color(0.8f, 0.08f, 0.08f);

        Text ammo = Label("Cartouches", canvas, font, 24, TextAnchor.UpperLeft, new Vector2(0f, 1f), new Vector2(40f, -76f), new Vector2(400f, 34f), Color.white);
        Text enemies = Label("Ennemis", canvas, font, 26, TextAnchor.UpperRight, new Vector2(1f, 1f), new Vector2(-40f, -40f), new Vector2(400f, 36f), Color.white);
        Text go = Label("GO", canvas, font, 72, TextAnchor.MiddleRight, new Vector2(1f, 0.5f), new Vector2(-50f, 120f), new Vector2(400f, 90f), new Color(1f, 0.15f, 0.1f));
        go.text = "GO ▶";
        go.fontStyle = FontStyle.Bold;
        Text message = Label("Message", canvas, font, 48, TextAnchor.MiddleCenter, new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(1400f, 70f), Color.white);
        message.fontStyle = FontStyle.Bold;
        Text fps = Label("FPS", canvas, font, 22, TextAnchor.UpperCenter, new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 30f), new Color(0.7f, 1f, 0.7f));
        Text help = Label("Aide", canvas, font, 18, TextAnchor.LowerLeft, new Vector2(0f, 0f), new Vector2(30f, 20f), new Vector2(1500f, 28f), new Color(1f, 1f, 1f, 0.6f));
        help.text = "Q/D + Z/S marcher · Espace sauter · J léger (et ramasser) · K lourd · arrière + K lanceur · Maj esquive 4 directions · U fusil · I recharger · 8 : 8 ennemis · F1 FPS · R recommencer";

        var markers = new Text[6];

        for (int i = 0; i < markers.Length; i++)
        {
            markers[i] = Label($"Repère {i}", canvas, font, 44, TextAnchor.MiddleCenter, new Vector2(0f, 0f), Vector2.zero, new Vector2(160f, 60f), new Color(1f, 0.2f, 0.15f));
            markers[i].fontStyle = FontStyle.Bold;
            markers[i].gameObject.AddComponent<Outline>().effectColor = Color.black;
        }

        RectTransform overlay = Rect("Flash", canvas, new Vector2(0.5f, 0.5f), Vector2.zero, Vector2.zero);
        overlay.anchorMin = Vector2.zero;
        overlay.anchorMax = Vector2.one;
        overlay.sizeDelta = Vector2.zero;
        flashOverlay = overlay.gameObject.AddComponent<Image>();
        flashOverlay.color = new Color(1f, 1f, 1f, 0f);
        flashOverlay.raycastTarget = false;

        var hud = canvasGo.AddComponent<HUD25D>();
        SetObject(hud, "player", player.GetComponent<Health>());
        SetObject(hud, "combat", player.GetComponent<BrawlerCombat25D>());
        SetObject(hud, "healthFill", fill);
        SetObject(hud, "ammo", ammo);
        SetObject(hud, "enemies", enemies);
        SetObject(hud, "go", go);
        SetObject(hud, "message", message);
        SetObject(hud, "fps", fps);
        SetObjects(hud, "markers", markers.Cast<UnityEngine.Object>().ToArray());
        return hud;
    }

    private static RectTransform Rect(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        var rect = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
        rect.SetParent(parent, false);
        rect.anchorMin = rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        return rect;
    }

    private static Text Label(string name, Transform parent, Font font, int size, TextAnchor alignment, Vector2 anchor, Vector2 position,
        Vector2 box, Color color)
    {
        RectTransform rect = Rect(name, parent, anchor, position, box);
        var text = rect.gameObject.AddComponent<Text>();
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = color;
        text.raycastTarget = false;
        return text;
    }

    // ---------------------------------------------------------------- Outils

    private static GameObject CreateSprite(string name, Transform parent, Sprite sprite, Color color, Vector3 position, Vector3 scale, int order)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = position;
        go.transform.localScale = new Vector3(scale.x, scale.y, 1f);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = order;
        return go;
    }

    private static Sprite Gore(string folder, string file) =>
        AssetDatabase.LoadAllAssetsAtPath($"{Root}/Art/Gore/{folder}/{file}.png").OfType<Sprite>().FirstOrDefault();

    private static AudioClip Sound(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{name}.mp3");

    private static void AddSceneToBuildSettings(string path)
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(s => s.path == path))
            return;

        scenes.Add(new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    private static void Apply(Component component, string field, Action<SerializedProperty> set)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = so.FindProperty(field);

        if (property == null)
        {
            Debug.LogError($"[RED MET] Champ introuvable : {component.GetType().Name}.{field}");
            return;
        }

        set(property);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObject(Component c, string field, UnityEngine.Object value) => Apply(c, field, p => p.objectReferenceValue = value);
    private static void SetInt(Component c, string field, int value) => Apply(c, field, p => p.intValue = value);
    private static void SetEnum(Component c, string field, int value) => Apply(c, field, p => p.enumValueIndex = value);
    private static void SetFloat(Component c, string field, float value) => Apply(c, field, p => p.floatValue = value);
    private static void SetVector2(Component c, string field, Vector2 value) => Apply(c, field, p => p.vector2Value = value);

    private static void SetObjects(Component c, string field, UnityEngine.Object[] values) => Apply(c, field, p =>
    {
        p.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
    });
}
