using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu RED MET > Construire l'arène prototype.
// Crée les dossiers, les layers Ground, Player, Enemy et Debris, des sprites provisoires,
// les attaques (AttackData), les prefabs des ennemis (Enemy, Projectile, Grenade, Cover),
// les archétypes du bestiaire et la scène Assets/_Game/Scenes/Arena.unity avec joueur,
// vagues de test, mannequin, effets de combat, caméra, interface, options et debug.
// Règle aussi le pas physique à 1/60 s : une image de combat = un pas physique.
// Relancer la commande reconstruit la scène et les prefabs de zéro. Les attaques et les
// archétypes existants sont conservés, pour garder les réglages faits dans l'inspecteur :
// supprimez-en un pour le recréer avec les valeurs de référence (docs/ENEMIES.md).
public static class ArenaBuilder
{
    private const string Root = "Assets/_Game";
    private const string ScenePath = Root + "/Scenes/Arena.unity";
    private const string SpritePath = Root + "/Art/Placeholder/Square.png";
    private const string NoFrictionPath = Root + "/Settings/NoFriction.physicsMaterial2D";
    private const string LineMaterialPath = Root + "/Art/VFX/TelegraphLine.mat";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string ArchetypeFolder = Root + "/Data/Enemies";
    private const string NathanAttackFolder = Root + "/Data/Attacks/Nathan";
    private const string EnemyAttackFolder = Root + "/Data/Attacks/Enemies";
    private const string GroundLayerName = "Ground";
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName = "Enemy";
    private const string DebrisLayerName = "Debris";

    private static readonly string[] Folders =
    {
        Root + "/Art/Characters",
        Root + "/Art/Backgrounds",
        Root + "/Art/VFX",
        Root + "/Art/Placeholder",
        Root + "/Animations",
        Root + "/Prefabs",
        Root + "/Scenes",
        Root + "/Settings",
        Root + "/Audio/Voice",
        ArchetypeFolder,
        NathanAttackFolder,
        EnemyAttackFolder,
    };

    [MenuItem("RED MET/Construire l'arène prototype")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        foreach (string folder in Folders)
            EnsureFolder(folder);

        int groundLayer = EnsureLayer(GroundLayerName);
        int playerLayer = EnsureLayer(PlayerLayerName);
        int enemyLayer = EnsureLayer(EnemyLayerName);
        int debrisLayer = EnsureLayer(DebrisLayerName);

        if (groundLayer < 0 || playerLayer < 0 || enemyLayer < 0 || debrisLayer < 0)
        {
            Debug.LogError("[RED MET] Pas assez d'emplacements libres pour créer les layers Ground, Player, Enemy et Debris.");
            return;
        }

        // Les ennemis se croisent sans se bloquer (groupes, renforts, Dealer derrière ses gardes).
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);

        // Les membres arrachés ne touchent que le décor.
        Physics2D.IgnoreLayerCollision(debrisLayer, debrisLayer, true);
        Physics2D.IgnoreLayerCollision(debrisLayer, playerLayer, true);
        Physics2D.IgnoreLayerCollision(debrisLayer, enemyLayer, true);

        // Une image de combat (60 FPS) = un pas physique, écrit dans les Project Settings.
        SetFixedTimestep(Mathf.RoundToInt(AttackData.FrameRate));

        Sprite square = EnsureSquareSprite();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
        Material lineMaterial = EnsureLineMaterial();
        Material flashMaterial = EnsureFlashMaterial();

        // Sprites animés rendus dans Blender (tranche verticale), s'ils existent.
        SpriteAnimationBuilder.Result nathanSprites = SpriteAnimationBuilder.Build("Nathan_M0", false);
        SpriteAnimationBuilder.Result voyouSprites = SpriteAnimationBuilder.Build("Voyou", true);
        SpriteAnimationBuilder.Result copSprites = SpriteAnimationBuilder.Build("Flic", true);
        SpriteAnimationBuilder.Result dealerSprites = SpriteAnimationBuilder.Build("Dealer", true);
        SpriteAnimationBuilder.Result nathanM4Sprites = SpriteAnimationBuilder.Build("Nathan_M4", false);

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGlobalLight();
        BuildLevel(square, groundLayer);

        GameObject player = BuildPlayer(square, noFriction, groundLayer, playerLayer, enemyLayer, flashMaterial, nathanSprites);
        AddShotgunAndForms(player, enemyLayer, nathanSprites, nathanM4Sprites);

        Projectile projectilePrefab = BuildProjectilePrefab(square);
        Grenade grenadePrefab = BuildGrenadePrefab(square, lineMaterial);
        Cover coverPrefab = BuildCoverPrefab(square, groundLayer);
        Enemy enemyPrefab = BuildEnemyPrefab(square, noFriction, lineMaterial, flashMaterial, enemyLayer,
            (1 << playerLayer) | (1 << enemyLayer), projectilePrefab, grenadePrefab);

        Camera camera = BuildCamera(player.transform);
        RedMoonDecor.Build(camera);

        Health playerHealth = player.GetComponent<Health>();
        CombatHUD hud = BuildHud(playerHealth, player.GetComponent<PlayerShotgun>(), out Image flashOverlay);

        var vfx = new GameObject("CombatVfx").AddComponent<CombatVfx>();
        SetObject(vfx, "particleMaterial", lineMaterial);
        SetObject(vfx, "square", square);
        SetObject(vfx, "flashOverlay", flashOverlay);
        SetInt(vfx, "bloodCollisionLayers", (1 << groundLayer) | 1);
        SetInt(vfx, "debrisLayer", debrisLayer);
        SetObjects(vfx, "fleshChunks", new UnityEngine.Object[]
        {
            GoreSprite("Generic", "chunk1"), GoreSprite("Generic", "chunk2"), GoreSprite("Generic", "chunk3"),
        });
        SetObject(vfx, "guts", GoreSprite("Generic", "guts"));
        SetObject(vfx, "goreSound", Sound("Gore_Burst"));

        new GameObject("GameAudio").AddComponent<GameAudio>();

        var waves = new GameObject("Waves").AddComponent<EnemyWaveManager>();
        SetObject(waves, "enemyPrefab", enemyPrefab);
        SetObject(waves, "coverPrefab", coverPrefab);
        SetObject(waves, "target", player.transform);
        EnemyWave[] waveList = BuildWaves(voyouSprites, copSprites, dealerSprites);
        SetWaves(waves, waveList);
        SetInt(waves, "startWave", MannequinWave);

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        SetObject(manager, "player", playerHealth);
        SetObject(manager, "waves", waves);
        SetObject(manager, "hud", hud);
        SetObject(manager, "vfx", vfx);

        var debug = new GameObject("CombatDebug");
        var demo = debug.AddComponent<ComboDemo>();
        SetObject(demo, "player", player.GetComponent<PlayerCombat>());
        var overlay = debug.AddComponent<CombatDebugOverlay>();
        SetObject(overlay, "player", player.GetComponent<PlayerCombat>());
        SetObject(overlay, "demo", demo);
        debug.AddComponent<BenchmarkRunner>();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
        AssetDatabase.SaveAssets();
        Selection.activeGameObject = player;

        Debug.Log("[RED MET] Arène construite : " + ScenePath + ". Appuie sur Play pour tester.");
    }

    // ---------------------------------------------------------------- Assets

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
    }

    private static int EnsureLayer(string layerName)
    {
        int existing = LayerMask.NameToLayer(layerName);

        if (existing >= 0)
            return existing;

        var tagManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TagManager.asset")[0]);
        SerializedProperty layers = tagManager.FindProperty("layers");

        for (int i = 8; i < layers.arraySize; i++)
        {
            SerializedProperty layer = layers.GetArrayElementAtIndex(i);

            if (!string.IsNullOrEmpty(layer.stringValue))
                continue;

            layer.stringValue = layerName;
            tagManager.ApplyModifiedProperties();
            return i;
        }

        return -1;
    }

    // Unity 6 enregistre le pas fixe en temps rationnel : 1 tick à framesPerSecond ticks par seconde.
    private static void SetFixedTimestep(int framesPerSecond)
    {
        var timeManager = new SerializedObject(
            AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/TimeManager.asset")[0]);
        SerializedProperty count = Find(timeManager, "Fixed Timestep.m_Count");
        SerializedProperty numerator = Find(timeManager, "Fixed Timestep.m_Rate.m_Numerator");
        SerializedProperty denominator = Find(timeManager, "Fixed Timestep.m_Rate.m_Denominator");

        if (count == null || numerator == null || denominator == null)
            return;

        count.longValue = 1;
        numerator.uintValue = (uint)framesPerSecond;
        denominator.uintValue = 1;
        timeManager.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssets();
    }

    private static Sprite EnsureSquareSprite()
    {
        if (!File.Exists(SpritePath))
        {
            var texture = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            texture.SetPixels(Enumerable.Repeat(Color.white, 16).ToArray());
            texture.Apply();
            File.WriteAllBytes(SpritePath, texture.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(SpritePath);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(SpritePath);

        if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, 4f))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 4f;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);
    }

    private static PhysicsMaterial2D EnsureNoFrictionMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<PhysicsMaterial2D>(NoFrictionPath);

        if (material != null)
            return material;

        material = new PhysicsMaterial2D("NoFriction") { friction = 0f, bounciness = 0f };
        AssetDatabase.CreateAsset(material, NoFrictionPath);
        return material;
    }

    // Matériau des lignes d'annonce (visée, zone de grenade) : couleur par sommet, sans lumière.
    private static Material EnsureLineMaterial()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>(LineMaterialPath);

        if (material != null)
            return material;

        Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");

        if (shader == null)
            shader = Shader.Find("Sprites/Default");

        material = new Material(shader) { name = "TelegraphLine" };
        AssetDatabase.CreateAsset(material, LineMaterialPath);
        return material;
    }

    // Matériau des personnages : sprite non éclairé avec flash d'impact (RED MET/Sprite Flash).
    private static Material EnsureFlashMaterial()
    {
        const string path = Root + "/Art/Shaders/SpriteFlash.mat";
        var material = AssetDatabase.LoadAssetAtPath<Material>(path);

        if (material != null)
            return material;

        material = new Material(Shader.Find("RED MET/Sprite Flash")) { name = "SpriteFlash" };
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    // ----------------------------------------------------------------- Scene

    // Ajoute une lumière globale 2D si l'URP est présent, sans en dépendre à la compilation.
    private static void CreateGlobalLight()
    {
        // Light2D a changé d'assembly selon les versions de l'URP (Universal.Runtime puis Universal.2D.Runtime).
        Type lightType = AppDomain.CurrentDomain.GetAssemblies()
            .Select(assembly => assembly.GetType("UnityEngine.Rendering.Universal.Light2D"))
            .FirstOrDefault(type => type != null);

        if (lightType == null)
        {
            Debug.LogWarning("[RED MET] Light2D introuvable : la scène n'aura pas de lumière globale 2D.");
            return;
        }

        try
        {
            var go = new GameObject("Global Light 2D");
            Component light = go.AddComponent(lightType);
            var property = lightType.GetProperty("lightType");

            if (property != null && property.CanWrite)
                property.SetValue(light, Enum.Parse(property.PropertyType, "Global"));
        }
        catch (Exception exception)
        {
            Debug.LogWarning("[RED MET] Lumière globale 2D non créée : " + exception.Message);
        }
    }

    private static void BuildLevel(Sprite square, int groundLayer)
    {
        var level = new GameObject("Level").transform;

        // Le fond et le sol visibles viennent de RedMoonDecor ; le carré du sol ne garde que son collider.
        GameObject ground = CreateSprite("Ground", level, square, new Color(0.24f, 0.2f, 0.22f), new Vector3(0f, -2f), new Vector3(40f, 1f), 0);
        ground.AddComponent<BoxCollider2D>();
        ground.layer = groundLayer;
        ground.GetComponent<SpriteRenderer>().enabled = false;

        RedMoonDecor.DressPlatform(CreatePlatform("Platform Left", level, square, new Vector3(-6f, 0.6f), groundLayer));
        RedMoonDecor.DressPlatform(CreatePlatform("Platform Right", level, square, new Vector3(5f, 1.8f), groundLayer));

        // Les murs restent sur le layer Default pour ne pas compter comme du sol.
        CreateSprite("Wall Left", level, square, new Color(0.03f, 0.01f, 0.015f), new Vector3(-20.5f, 4f), new Vector3(1f, 12f), 0)
            .AddComponent<BoxCollider2D>();
        CreateSprite("Wall Right", level, square, new Color(0.03f, 0.01f, 0.015f), new Vector3(20.5f, 4f), new Vector3(1f, 12f), 0)
            .AddComponent<BoxCollider2D>();
    }

    // Plateforme traversable par le dessous.
    private static GameObject CreatePlatform(string name, Transform parent, Sprite square, Vector3 position, int groundLayer)
    {
        GameObject platform = CreateSprite(name, parent, square, new Color(0.35f, 0.18f, 0.2f), position, new Vector3(4f, 0.4f), 0);
        platform.layer = groundLayer;
        platform.AddComponent<BoxCollider2D>().usedByEffector = true;
        platform.AddComponent<PlatformEffector2D>().useOneWay = true;
        return platform;
    }

    private static GameObject BuildPlayer(Sprite square, PhysicsMaterial2D noFriction, int groundLayer, int playerLayer, int enemyLayer,
        Material flashMaterial, SpriteAnimationBuilder.Result sprites)
    {
        var player = new GameObject("Player") { tag = "Player", layer = playerLayer };
        player.transform.position = new Vector3(-4f, -0.6f, 0f);

        GameObject visual = CreateSprite("Visual", player.transform, square, new Color(0.2f, 0.35f, 0.9f), Vector3.zero, new Vector3(0.8f, 1.6f), 5);
        var visualRenderer = visual.GetComponent<SpriteRenderer>();
        visualRenderer.sharedMaterial = flashMaterial;

        if (sprites != null)
        {
            // Nathan M0 : sprite animé, pivot aux pieds (bas de la capsule de 1,6).
            visual.transform.localScale = Vector3.one;
            visual.transform.localPosition = new Vector3(0f, -0.8f, 0f);
            visualRenderer.sprite = sprites.IdleSprite;
            visualRenderer.color = Color.white;
            visual.AddComponent<Animator>().runtimeAnimatorController = sprites.Controller;
        }
        else
        {
            CreateSprite("Eye", player.transform, square, Color.white, new Vector3(0.22f, 0.45f), new Vector3(0.15f, 0.15f), 6);
        }

        Rigidbody2D rb = AddBody(player, new Vector2(0.8f, 1.6f), noFriction);
        rb.mass = 1f;

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, -0.8f, 0f);

        // Zone de frappe affichée par le debug (F1), placée et dimensionnée par PlayerCombat.
        GameObject hitboxDebug = CreateSprite("HitboxDebug", player.transform, square, new Color(1f, 0.3f, 0.2f, 0.35f), Vector3.zero, Vector3.one, 7);
        hitboxDebug.GetComponent<SpriteRenderer>().enabled = false;

        var health = player.AddComponent<Health>();
        SetInt(health, "maxHealth", 100);

        player.AddComponent<HitStop>();
        var reaction = player.AddComponent<HitReaction>();
        SetObject(reaction, "visual", visual.transform);
        SetObject(reaction, "body", visual.GetComponent<SpriteRenderer>());
        SetBool(reaction, "placeholderPoses", sprites == null);

        var controller = player.AddComponent<PlayerController>();
        SetObject(controller, "groundCheck", groundCheck);
        SetInt(controller, "groundLayer", 1 << groundLayer);

        var combat = player.AddComponent<PlayerCombat>();
        SetObject(combat, "controller", controller);
        SetInt(combat, "targetLayers", 1 << enemyLayer);
        SetObject(combat, "hitboxDebug", hitboxDebug.GetComponent<SpriteRenderer>());
        AssignNathanAttacks(combat);

        var dodge = player.AddComponent<PlayerDodge>();
        SetObject(dodge, "controller", controller);
        SetObject(dodge, "spriteRenderer", visual.GetComponent<SpriteRenderer>());
        SetFloat(dodge, "dodgeInvulnerability", 0.4f);

        var dash = player.AddComponent<PlayerDash>();
        SetObject(dash, "controller", controller);

        player.AddComponent<CharacterAnimator>();
        return player;
    }

    // ------------------------------------------------- Fusil à pompe et gore

    private const string GoreFolder = Root + "/Art/Gore";
    private const string GibsFolder = Root + "/Data/Gibs";
    private const string SoundFolder = Root + "/Audio/Voice";

    // Nathan M4 et son fusil (touche U), stades M0 / M4 (F4 ou vague 8), morceaux de corps.
    private static void AddShotgunAndForms(GameObject player, int enemyLayer, SpriteAnimationBuilder.Result m0, SpriteAnimationBuilder.Result m4)
    {
        var gibs = player.AddComponent<GibSource>();
        SetObject(gibs, "body", player.transform.Find("Visual").GetComponent<SpriteRenderer>());

        var shotgun = player.AddComponent<PlayerShotgun>();
        SetObject(shotgun, "attack", NathanShotgun());
        SetInt(shotgun, "targetLayers", 1 << enemyLayer);
        SetObject(shotgun, "shellSound", Sound("Shotgun_Shell"));
        // Bout du canon sur le sprite de tir de M4 (pose épaulée), depuis le centre du corps.
        SetVector2(shotgun, "muzzleOffset", new Vector2(1.25f, 0.45f));

        var form = player.AddComponent<NathanForm>();
        SetObject(form, "animator", player.transform.Find("Visual").GetComponent<Animator>());
        SetObject(form, "m0Controller", m0?.Controller);
        SetObject(form, "m4Controller", m4?.Controller);
        SetObject(form, "m0Gibs", EnsureGibSet("NathanM0"));
        SetObject(form, "m4Gibs", EnsureGibSet("NathanM4"));
    }

    // Fusil de Nathan M4 : coup de feu à l'image 7, pompe de 36 images ; bout portant = mort instantanée
    // des humains de l'Acte I (pulvérisés), moyenne distance = projection et étourdissement, au-delà = plombs dispersés.
    private static AttackData NathanShotgun()
    {
        AttackData far = Attack(NathanAttackFolder, "N11_FusilLoin", "Fusil à pompe (plombs dispersés)", 1, 1, 0, 15,
            DamageType.Bullet, KnockbackType.Light, new Vector2(3f, 0f), 14, 3, 0.2f);

        return Attack(NathanAttackFolder, "N10_FusilAPompe", "Fusil à pompe", 6, 2, 36, 60, DamageType.Bullet,
            KnockbackType.WallBounce, new Vector2(13f, 5f), 40, 10, 0.8f, a =>
            {
                a.animationId = 9;
                a.zoom = true;
                a.impactFlash = true;
                a.step = 0f;
                a.pellets = 8;
                a.coneAngle = 18f;
                a.closeRange = 2.5f;
                a.midRange = 5.5f;
                a.maxRange = 9f;
                a.lethalAtCloseRange = true;
                a.farHit = far;
                a.activeSound = Sound("Shotgun_Blast");
                a.recoverySound = Sound("Shotgun_Pump");
            });
    }

    // Fusil du Dealer : annonce de 0,9 s (pompe armée, épaulé, cône et lueur), rechargement lent de 1,25 s.
    private static AttackData DealerShotgun()
    {
        AttackData far = Attack(EnemyAttackFolder, "E02_Dealer_FusilLoin", "Fusil à pompe (plombs dispersés)", 1, 1, 0, 10,
            DamageType.Bullet, KnockbackType.Light, new Vector2(2.5f, 0f), 12, 3, 0.15f, a => a.cancelWindow = Vector2Int.zero);

        return Attack(EnemyAttackFolder, "E02_Dealer_FusilAPompe", "Fusil à pompe", 54, 3, 75, 45, DamageType.Bullet,
            KnockbackType.WallBounce, new Vector2(11f, 5f), 40, 8, 0.6f, a =>
            {
                a.cancelWindow = Vector2Int.zero;
                a.animationId = 5;
                a.pellets = 8;
                a.coneAngle = 16f;
                a.closeRange = 2.5f;
                a.midRange = 5.5f;
                a.maxRange = 9f;
                a.lethalAtCloseRange = true;
                a.farHit = far;
                a.windupSound = Sound("Shotgun_Pump");
                a.activeSound = Sound("Shotgun_Blast");
                a.recoverySound = Sound("Shotgun_Pump");
            });
    }

    // Dealer en sprites, fusil à pompe au lieu du pistolet (réécrit à chaque construction).
    private static void ApplyDealer(EnemyArchetype dealer, SpriteAnimationBuilder.Result sprites)
    {
        if (sprites != null)
            ApplySprites(dealer, sprites, new Color(0.12f, 0.12f, 0.15f));

        dealer.bodySize = new Vector2(0.9f, 1.8f);
        // Canon du fusil épaulé, mesuré dans Blender : 1,2 m devant les pieds, 1,3 m de haut.
        dealer.muzzleOffset = new Vector2(1.2f, 1.3f);
        dealer.gibs = EnsureGibSet("Dealer");
        AttackData order = Attack(EnemyAttackFolder, "E02_Dealer_Ordre", "Ordre « Chopez-le »", 60, 1, 30, 0, DamageType.Blunt,
            KnockbackType.Light, Vector2.zero, 0, 0, 0f, a => a.cancelWindow = Vector2Int.zero);
        AttackData butt = Attack(EnemyAttackFolder, "E02_Dealer_Crosse", "Coup de crosse", 24, 6, 30, 10, DamageType.Blunt,
            KnockbackType.Light, new Vector2(4f, 0f), 16, 5, 0.15f, a => a.cancelWindow = Vector2Int.zero);
        order.animationId = 7;
        butt.animationId = 6;
        EditorUtility.SetDirty(order);
        EditorUtility.SetDirty(butt);

        AttackData shotgun = DealerShotgun();
        dealer.attacks = new[]
        {
            new EnemyAttack
            {
                name = "Ordre « Chopez-le »", kind = EnemyAttackKind.Order, cooldown = 8f,
                buffWindupMultiplier = 0.8f, buffDuration = 5f, telegraphColor = new Color(1f, 0.3f, 0.9f), data = order,
            },
            new EnemyAttack
            {
                name = "Fusil à pompe", kind = EnemyAttackKind.Shotgun, range = 8.5f, cooldown = 1f,
                trackingFrames = shotgun.startup - 15, telegraphColor = AimRed, data = shotgun,
            },
            new EnemyAttack
            {
                name = "Coup de crosse", range = 1.1f, cooldown = 0.6f, telegraphColor = FlashYellow, data = butt,
            },
        };
        EditorUtility.SetDirty(dealer);
    }

    // Morceaux d'un personnage (Art/Gore/<nom>/head, torso, armL, armR, legL, legR), importés au cadrage des sprites.
    private static GibSet EnsureGibSet(string name)
    {
        string path = $"{GibsFolder}/{name}.asset";
        EnsureFolder(GibsFolder);
        var set = AssetDatabase.LoadAssetAtPath<GibSet>(path);

        if (set == null)
        {
            set = ScriptableObject.CreateInstance<GibSet>();
            AssetDatabase.CreateAsset(set, path);
        }

        set.head = GoreSprite(name, "head");
        set.torso = GoreSprite(name, "torso");
        set.armLeft = GoreSprite(name, "armL");
        set.armRight = GoreSprite(name, "armR");
        set.legLeft = GoreSprite(name, "legL");
        set.legRight = GoreSprite(name, "legR");
        EditorUtility.SetDirty(set);
        return set;
    }

    // Morceau rendu dans Blender (512 px pour 2,6 m, comme les personnages), pivot au centre.
    private static Sprite GoreSprite(string folder, string file)
    {
        string path = $"{GoreFolder}/{folder}/{file}.png";

        if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            return null;

        const float pixelsPerMeter = 512f / 2.6f;

        if (importer.textureType != TextureImporterType.Sprite || !Mathf.Approximately(importer.spritePixelsPerUnit, pixelsPerMeter))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = pixelsPerMeter;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    private static AudioClip Sound(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>($"{SoundFolder}/{name}.mp3");

    // ------------------------------------------------------------- Attaques

    // Crée l'attaque si elle n'existe pas. Images à 60 FPS ; recul : x vers l'arrière, y vers le haut.
    private static AttackData Attack(string folder, string file, string displayName, int startup, int active, int recovery,
        int damage, DamageType type, KnockbackType knockback, Vector2 velocity, int hitstun, int hitstop, float shake,
        Action<AttackData> extra = null)
    {
        string path = $"{folder}/{file}.asset";
        var attack = AssetDatabase.LoadAssetAtPath<AttackData>(path);

        if (attack != null)
            return attack;

        attack = ScriptableObject.CreateInstance<AttackData>();
        attack.displayName = displayName;
        attack.startup = startup;
        attack.active = active;
        attack.recovery = recovery;
        attack.cancelWindow = new Vector2Int(startup + 1, startup + active + recovery - 2);
        attack.damage = damage;
        attack.damageType = type;
        attack.knockback = knockback;
        attack.knockbackVelocity = velocity;
        attack.hitstun = hitstun;
        attack.hitstop = hitstop;
        attack.shake = shake;
        extra?.Invoke(attack);
        AssetDatabase.CreateAsset(attack, path);
        return attack;
    }

    // Profil sobre (docs/ENEMIES.md) : combo 12 / 16 / 25, coups lourds pour les rebonds.
    // Zones de frappe calées sur les sprites de Nathan M0 (docs/PRODUCTION.md, 2026-10-27) : de 0,15 m devant
    // le centre du corps jusqu'à 10 cm après le poing ou le pied de l'image active, à sa hauteur
    // (le centre du corps est à 0,80 m du sol). step : pas en avant pendant le démarrage et l'actif.
    private static void AssignNathanAttacks(PlayerCombat combat)
    {
        const string f = NathanAttackFolder;
        const DamageType blunt = DamageType.Blunt;

        SetObject(combat, "jab", Attack(f, "N01_Jab", "Jab", 4, 3, 10, 12, blunt, KnockbackType.Light, new Vector2(3.5f, 0f), 16, 4, 0.08f, a =>
        {
            a.animationId = 0; a.step = 0.3f; a.hitboxOffset = new Vector2(0.575f, 0.23f); a.hitboxSize = new Vector2(0.85f, 0.8f);
        }));

        SetObject(combat, "cross", Attack(f, "N02_Direct", "Direct", 5, 3, 12, 16, blunt, KnockbackType.Light, new Vector2(4.5f, 0f), 18, 6, 0.15f, a =>
        {
            a.animationId = 1; a.step = 0.35f; a.hitboxOffset = new Vector2(0.575f, 0.27f); a.hitboxSize = new Vector2(0.85f, 0.8f);
        }));

        SetObject(combat, "hook", Attack(f, "N03_Crochet", "Crochet", 7, 4, 18, 25, blunt, KnockbackType.Heavy, new Vector2(8f, 3f), 26, 9, 0.35f, a =>
        {
            a.animationId = 2; a.step = 0.4f; a.zoom = true;
            a.hitboxOffset = new Vector2(0.645f, 0.35f); a.hitboxSize = new Vector2(0.99f, 0.9f);
        }));

        SetObject(combat, "heavy", Attack(f, "N04_CoupLourd", "Coup de pied lourd", 10, 4, 20, 28, blunt, KnockbackType.WallBounce, new Vector2(16f, 4f), 40, 11, 0.6f, a =>
        {
            a.animationId = 3; a.step = 0.45f; a.zoom = true; a.impactFlash = true;
            a.hitboxOffset = new Vector2(0.575f, -0.02f); a.hitboxSize = new Vector2(0.85f, 0.9f);
        }));

        SetObject(combat, "launcher", Attack(f, "N05_Lanceur", "Lanceur (uppercut)", 8, 4, 20, 18, blunt, KnockbackType.Launch, new Vector2(1.5f, 15f), 45, 8, 0.35f, a =>
        {
            a.animationId = 4; a.step = 0.25f; a.hitboxOffset = new Vector2(0.475f, 0.35f); a.hitboxSize = new Vector2(0.65f, 1.4f);
        }));

        SetObject(combat, "airLight1", Attack(f, "N06_Air1", "Aérien 1", 4, 3, 9, 10, blunt, KnockbackType.Light, new Vector2(2f, 7f), 30, 5, 0.1f, a =>
        {
            a.animationId = 5; a.step = 0f; a.hitboxOffset = new Vector2(0.575f, 0.25f); a.hitboxSize = new Vector2(0.85f, 0.9f);
        }));

        SetObject(combat, "airLight2", Attack(f, "N07_Air2", "Aérien 2", 5, 3, 10, 12, blunt, KnockbackType.Light, new Vector2(2f, 7.5f), 30, 6, 0.12f, a =>
        {
            a.animationId = 6; a.step = 0f; a.hitboxOffset = new Vector2(0.575f, 0.25f); a.hitboxSize = new Vector2(0.85f, 0.9f);
        }));

        SetObject(combat, "airLight3", Attack(f, "N08_Air3", "Aérien 3", 6, 4, 12, 14, blunt, KnockbackType.Light, new Vector2(2.5f, 8f), 32, 7, 0.2f, a =>
        {
            a.animationId = 7; a.step = 0f; a.hitboxOffset = new Vector2(0.575f, -0.05f); a.hitboxSize = new Vector2(0.85f, 1f);
        }));

        SetObject(combat, "smash", Attack(f, "N09_Smash", "Smash", 7, 4, 16, 26, blunt, KnockbackType.GroundBounce, new Vector2(3f, -18f), 50, 12, 0.7f, a =>
        {
            a.animationId = 8; a.step = 0f; a.zoom = true; a.impactFlash = true;
            a.hitboxOffset = new Vector2(0.55f, -0.1f); a.hitboxSize = new Vector2(0.8f, 1.4f);
        }));
    }

    // --------------------------------------------------------------- Prefabs

    private static T SavePrefab<T>(GameObject go) where T : Component
    {
        EnsureFolder(PrefabFolder);
        GameObject asset = PrefabUtility.SaveAsPrefabAsset(go, $"{PrefabFolder}/{go.name}.prefab");
        UnityEngine.Object.DestroyImmediate(go);
        return asset.GetComponent<T>();
    }

    private static LineRenderer CreateLine(string name, Transform parent, Material material, bool loop)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var line = go.AddComponent<LineRenderer>();
        line.sharedMaterial = material;
        line.useWorldSpace = true;
        line.loop = loop;
        line.positionCount = 0;
        line.sortingOrder = 9;
        line.enabled = false;
        return line;
    }

    private static Projectile BuildProjectilePrefab(Sprite square)
    {
        GameObject go = CreateSprite("Projectile", null, square, new Color(1f, 0.9f, 0.45f), Vector3.zero, new Vector3(0.3f, 0.1f), 8);
        go.AddComponent<Projectile>();
        return SavePrefab<Projectile>(go);
    }

    private static Grenade BuildGrenadePrefab(Sprite square, Material lineMaterial)
    {
        GameObject go = CreateSprite("Grenade", null, square, new Color(0.3f, 0.4f, 0.2f), Vector3.zero, new Vector3(0.25f, 0.25f), 8);
        LineRenderer zone = CreateLine("Zone", go.transform, lineMaterial, true);
        SetObject(go.AddComponent<Grenade>(), "zone", zone);
        return SavePrefab<Grenade>(go);
    }

    // Abri de 1,4 unité de haut : il cache Nathan (1,6) des tirs et s'enjambe d'un saut.
    private static Cover BuildCoverPrefab(Sprite square, int groundLayer)
    {
        GameObject go = CreateSprite("Cover", null, square, new Color(0.38f, 0.33f, 0.3f), Vector3.zero, new Vector3(1f, 1.4f), 1);
        go.layer = groundLayer;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<Cover>();
        return SavePrefab<Cover>(go);
    }

    // Ennemi générique : l'archétype règle taille, couleur, PV, poids et comportement à l'apparition.
    private static Enemy BuildEnemyPrefab(Sprite square, PhysicsMaterial2D noFriction, Material lineMaterial, Material flashMaterial,
        int enemyLayer, int corpseIgnoredLayers, Projectile projectilePrefab, Grenade grenadePrefab)
    {
        var go = new GameObject("Enemy") { layer = enemyLayer };

        GameObject visual = CreateSprite("Visual", go.transform, square, Color.white, Vector3.zero, new Vector3(0.9f, 1.7f), 5);
        visual.GetComponent<SpriteRenderer>().sharedMaterial = flashMaterial;
        GameObject eye = CreateSprite("Eye", go.transform, square, Color.black, new Vector3(0.25f, 0.5f), new Vector3(0.15f, 0.15f), 6);
        LineRenderer aimLine = CreateLine("AimLine", go.transform, lineMaterial, false);

        AddBody(go, new Vector2(0.9f, 1.7f), noFriction);

        // Pas d'invulnérabilité après un coup : les combos de Nathan s'enchaînent.
        var health = go.AddComponent<Health>();
        SetFloat(health, "invulnerabilityTime", 0f);

        var telegraph = go.AddComponent<EnemyTelegraph>();
        SetObject(telegraph, "body", visual.GetComponent<SpriteRenderer>());
        SetObject(telegraph, "aimLine", aimLine);

        // Cône de danger du fusil à pompe (maillage rempli par EnemyTelegraph) et lueur rouge au canon.
        var cone = new GameObject("DangerCone");
        cone.transform.SetParent(go.transform, false);
        var coneRenderer = cone.AddComponent<MeshRenderer>();
        coneRenderer.sharedMaterial = lineMaterial;
        coneRenderer.sortingOrder = 4;
        SetObject(telegraph, "cone", cone.AddComponent<MeshFilter>());
        cone.SetActive(false);

        var glowSprite = AssetDatabase.LoadAssetAtPath<Sprite>(Root + "/Art/Backgrounds/RedMoon/Dot.png");
        GameObject glow = CreateSprite("MuzzleGlow", go.transform, glowSprite != null ? glowSprite : square, Color.red, Vector3.zero,
            Vector3.one * 0.25f, 8);
        // Matériau sans lumière : le matériau des braises (intensité 3) faisait virer la lueur au blanc.
        glow.GetComponent<SpriteRenderer>().sharedMaterial = lineMaterial;
        glow.GetComponent<SpriteRenderer>().enabled = false;
        SetObject(telegraph, "muzzleGlow", glow.GetComponent<SpriteRenderer>());

        var gibSource = go.AddComponent<GibSource>();
        SetObject(gibSource, "body", visual.GetComponent<SpriteRenderer>());

        go.AddComponent<HitStop>();
        var reaction = go.AddComponent<HitReaction>();
        SetObject(reaction, "visual", visual.transform);
        SetObject(reaction, "body", visual.GetComponent<SpriteRenderer>());

        var enemy = go.AddComponent<Enemy>();
        SetObject(enemy, "visual", visual.transform);
        SetObject(enemy, "eye", eye.transform);
        SetObject(enemy, "telegraph", telegraph);
        SetObject(enemy, "projectilePrefab", projectilePrefab);
        SetObject(enemy, "grenadePrefab", grenadePrefab);
        SetInt(enemy, "corpseIgnoredLayers", corpseIgnoredLayers);

        go.AddComponent<CharacterAnimator>();
        return SavePrefab<Enemy>(go);
    }

    // ----------------------------------------------------- Bestiaire (ENEMIES.md)

    private static readonly Color FlashYellow = new(1f, 0.85f, 0.2f);
    private static readonly Color AimRed = new(1f, 0.15f, 0.15f);

    // Apparence animée (réappliquée à chaque construction, même sur un archétype existant).
    private static void ApplySprites(EnemyArchetype archetype, SpriteAnimationBuilder.Result sprites, Color clothes)
    {
        if (sprites == null)
            return;

        archetype.animator = sprites.Controller;
        archetype.sprite = sprites.IdleSprite;
        archetype.bodyColor = Color.white;
        archetype.limbColor = clothes;
        EditorUtility.SetDirty(archetype);
    }

    // Image fixe rendue dans Blender (tools/blender/redmet_props.py), au cadrage des sprites des personnages :
    // 512 px pour 2,6 m, pieds à 0,3 m du bas. Sans Animator : le mannequin ne fait que se balancer (HitReaction).
    private static void ApplyStaticSprite(EnemyArchetype archetype, string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer == null)
            return;

        const float pixelsPerMeter = 512f / 2.6f;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = pixelsPerMeter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteAlignment = (int)SpriteAlignment.Custom;
        settings.spritePivot = new Vector2(0.5f, 0.3f * pixelsPerMeter / 512f);
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();

        archetype.animator = null;
        archetype.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        archetype.bodyColor = Color.white;
        archetype.limbColor = new Color(0.62f, 0.52f, 0.36f);
        EditorUtility.SetDirty(archetype);
    }

    private static EnemyArchetype EnsureArchetype(string file, Action<EnemyArchetype> setup)
    {
        string path = $"{ArchetypeFolder}/{file}.asset";
        var archetype = AssetDatabase.LoadAssetAtPath<EnemyArchetype>(path);

        if (archetype != null)
            return archetype;

        archetype = ScriptableObject.CreateInstance<EnemyArchetype>();
        setup(archetype);
        AssetDatabase.CreateAsset(archetype, path);
        return archetype;
    }

    // Attaque d'ennemi : durées de la fiche converties en images (0,35 s = 21 images).
    private static AttackData EnemyAttackData(string file, string displayName, int windup, int active, int recovery,
        int damage, DamageType type, KnockbackType knockback, Vector2 velocity, int hitstun, int hitstop, float shake) =>
        Attack(EnemyAttackFolder, file, displayName, windup, active, recovery, damage, type, knockback, velocity, hitstun, hitstop, shake,
            a => a.cancelWindow = Vector2Int.zero);

    // 01 — Voyou, trois variantes d'arme.
    private static EnemyArchetype Thug(string file, string displayName, Color color, EnemyAttack attack) =>
        EnsureArchetype(file, a =>
        {
            a.displayName = displayName;
            a.behaviour = EnemyBehaviourType.MeleeAggressive;
            a.maxHealth = 60;
            a.moveSpeed = 3.5f;
            a.detectionRange = 9f;
            a.weight = EnemyWeight.Light;
            a.bodyColor = color;
            a.attacks = new[] { attack };
        });

    // Flic corrompu en sprites (phase C) : un clip par attaque (SpriteAnimationBuilder.EnemyAttackClips),
    // 1,85 m, canon calé sur la pose de visée (0,9 m devant les pieds, 1,55 m de haut ; mesuré dans Blender).
    // Réappliqué à chaque construction, comme ApplySprites.
    private static void ApplyCopSprites(EnemyArchetype cop, SpriteAnimationBuilder.Result sprites)
    {
        if (sprites == null)
            return;

        ApplySprites(cop, sprites, new Color(0.14f, 0.16f, 0.28f));
        cop.bodySize = new Vector2(0.9f, 1.85f);
        cop.muzzleOffset = new Vector2(0.9f, 1.55f);

        string[] clips = { "Tir visé", "Rafale de 3", "Coup de matraque", "Appel radio" };
        int[] ids = { 1, 2, 3, 4 };

        foreach (EnemyAttack attack in cop.attacks)
        {
            int index = Array.IndexOf(clips, attack.name);

            if (index < 0 || attack.data == null)
                continue;

            attack.data.animationId = ids[index];
            EditorUtility.SetDirty(attack.data);
        }

        EditorUtility.SetDirty(cop);
    }

    // Vague lancée au démarrage (touche 7).
    private const int MannequinWave = 6;

    private static EnemyWave[] BuildWaves(SpriteAnimationBuilder.Result voyouSprites, SpriteAnimationBuilder.Result copSprites,
        SpriteAnimationBuilder.Result dealerSprites)
    {
        const DamageType blunt = DamageType.Blunt;
        const DamageType blade = DamageType.Blade;
        const DamageType bullet = DamageType.Bullet;

        EnemyArchetype fists = Thug("01_Voyou_MainsNues", "Voyou", new Color(0.8f, 0.15f, 0.15f), new EnemyAttack
        {
            name = "Coup de poing", range = 1.2f, cooldown = 0.5f, telegraphColor = FlashYellow,
            data = EnemyAttackData("E01_Voyou_Poing", "Coup de poing", 21, 6, 21, 10, blunt, KnockbackType.Light, new Vector2(4f, 0f), 18, 5, 0.15f),
        });

        EnemyArchetype knife = Thug("01_Voyou_Couteau", "Voyou au couteau", new Color(0.7f, 0.12f, 0.25f), new EnemyAttack
        {
            name = "Coup de couteau", range = 1.4f, cooldown = 0.6f, telegraphColor = new Color(0.85f, 0.95f, 1f),
            data = EnemyAttackData("E01_Voyou_Couteau", "Coup de couteau", 27, 5, 27, 15, blade, KnockbackType.Light, new Vector2(3.5f, 0f), 18, 6, 0.2f),
        });

        EnemyArchetype bar = Thug("01_Voyou_Barre", "Voyou à la barre", new Color(0.6f, 0.2f, 0.1f), new EnemyAttack
        {
            name = "Balayage à la barre", range = 1.8f, cooldown = 0.8f, telegraphColor = new Color(1f, 0.55f, 0.15f),
            data = EnemyAttackData("E01_Voyou_Barre", "Balayage à la barre", 36, 9, 36, 18, blunt, KnockbackType.Heavy, new Vector2(8f, 3f), 26, 9, 0.4f),
        });

        EnemyArchetype dealer = EnsureArchetype("02_Dealer", a =>
        {
            a.displayName = "Dealer";
            a.behaviour = EnemyBehaviourType.Commander;
            a.maxHealth = 90;
            a.moveSpeed = 2.5f;
            a.altSpeed = 4.5f;
            a.detectionRange = 12f;
            a.weight = EnemyWeight.Medium;
            a.hitstunScale = 0.8f;
            a.bodyColor = new Color(0.55f, 0.2f, 0.65f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Ordre « Chopez-le »", kind = EnemyAttackKind.Order, cooldown = 8f,
                    buffWindupMultiplier = 0.8f, buffDuration = 5f, telegraphColor = new Color(1f, 0.3f, 0.9f),
                    data = EnemyAttackData("E02_Dealer_Ordre", "Ordre « Chopez-le »", 60, 1, 30, 0, blunt, KnockbackType.Light, Vector2.zero, 0, 0, 0f),
                },
                new EnemyAttack
                {
                    name = "Tir au pistolet", kind = EnemyAttackKind.Projectile, range = 8f, cooldown = 1.2f,
                    projectileCount = 2, shotIntervalFrames = 15, projectileSpeed = 16f, aimLine = true, telegraphColor = AimRed,
                    data = EnemyAttackData("E02_Dealer_Pistolet", "Tir au pistolet", 36, 3, 54, 14, bullet, KnockbackType.Light, new Vector2(2.5f, 0f), 12, 3, 0.1f),
                },
                new EnemyAttack
                {
                    name = "Coup de crosse", range = 1.1f, cooldown = 0.6f, telegraphColor = FlashYellow,
                    data = EnemyAttackData("E02_Dealer_Crosse", "Coup de crosse", 24, 6, 30, 10, blunt, KnockbackType.Light, new Vector2(4f, 0f), 16, 5, 0.15f),
                },
            };
        });

        EnemyArchetype cop = EnsureArchetype("03_FlicCorrompu", a =>
        {
            a.displayName = "Flic corrompu";
            a.behaviour = EnemyBehaviourType.CoverShooter;
            a.maxHealth = 80;
            a.moveSpeed = 3f;
            a.detectionRange = 14f;
            a.weight = EnemyWeight.Medium;
            a.hitstunScale = 0.8f;
            a.bodyColor = new Color(0.15f, 0.25f, 0.55f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Tir visé", kind = EnemyAttackKind.Projectile, range = 9f, cooldown = 0.3f, weight = 3f,
                    projectileSpeed = 18f, aimLine = true, aimLineWidth = 0.03f, telegraphColor = AimRed,
                    data = EnemyAttackData("E03_Flic_TirVise", "Tir visé", 42, 3, 24, 14, bullet, KnockbackType.Light, new Vector2(2.5f, 0f), 12, 3, 0.1f),
                },
                new EnemyAttack
                {
                    name = "Rafale de 3", kind = EnemyAttackKind.Projectile, range = 9f, cooldown = 0.3f, weight = 1f,
                    projectileCount = 3, shotIntervalFrames = 6, projectileSpeed = 18f, aimLine = true, aimLineWidth = 0.1f, telegraphColor = AimRed,
                    data = EnemyAttackData("E03_Flic_Rafale", "Rafale de 3", 54, 18, 42, 10, bullet, KnockbackType.Light, new Vector2(2f, 0f), 10, 2, 0.08f),
                },
                new EnemyAttack
                {
                    name = "Coup de matraque", range = 1.5f, cooldown = 0.6f, telegraphColor = FlashYellow,
                    data = EnemyAttackData("E03_Flic_Matraque", "Coup de matraque", 27, 7, 30, 12, blunt, KnockbackType.Light, new Vector2(4.5f, 0f), 18, 6, 0.2f),
                },
                new EnemyAttack
                {
                    name = "Appel radio", kind = EnemyAttackKind.Summon, maxUses = 1, summon = fists, telegraphColor = new Color(0.3f, 0.9f, 1f),
                    data = EnemyAttackData("E03_Flic_Radio", "Appel radio", 72, 1, 18, 0, blunt, KnockbackType.Light, Vector2.zero, 0, 0, 0f),
                },
            };
        });

        EnemyArchetype soldier = EnsureArchetype("04_SoldatPrive", a =>
        {
            a.displayName = "Soldat privé";
            a.behaviour = EnemyBehaviourType.Tactical;
            a.maxHealth = 110;
            a.moveSpeed = 3.2f;
            a.detectionRange = 16f;
            a.weight = EnemyWeight.Medium;
            a.hitstunScale = 0.7f;
            a.bodyColor = new Color(0.25f, 0.3f, 0.22f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Rafale visée", kind = EnemyAttackKind.Projectile, range = 12f, cooldown = 0.8f,
                    projectileCount = 3, shotIntervalFrames = 6, projectileSpeed = 22f, aimLine = true, telegraphColor = AimRed,
                    data = EnemyAttackData("E04_Soldat_Rafale", "Rafale visée", 48, 15, 36, 8, bullet, KnockbackType.Light, new Vector2(2f, 0f), 10, 2, 0.08f),
                },
                new EnemyAttack
                {
                    name = "Grenade", kind = EnemyAttackKind.Grenade, range = 10f, cooldown = 8f, areaRadius = 2.5f, fuse = 1.2f,
                    telegraphColor = new Color(1f, 0.25f, 0.1f),
                    data = EnemyAttackData("E04_Soldat_Grenade", "Grenade", 36, 1, 48, 30, DamageType.Explosion, KnockbackType.Heavy, new Vector2(7f, 6f), 30, 6, 0.5f),
                },
                new EnemyAttack
                {
                    name = "Coup de crosse", range = 1.3f, cooldown = 0.6f, telegraphColor = FlashYellow,
                    data = EnemyAttackData("E04_Soldat_Crosse", "Coup de crosse", 24, 6, 27, 15, blunt, KnockbackType.Heavy, new Vector2(6f, 2f), 22, 7, 0.3f),
                },
            };
        });

        EnemyArchetype mutant = EnsureArchetype("06_Mutant", a =>
        {
            a.displayName = "Mutant";
            a.behaviour = EnemyBehaviourType.HeavyCharger;
            a.maxHealth = 260;
            a.moveSpeed = 2f;
            a.detectionRange = 10f;
            a.weight = EnemyWeight.Heavy;
            a.superArmor = true;
            a.hitstunScale = 0f;
            a.bodyColor = new Color(0.45f, 0.55f, 0.3f);
            a.bodySize = new Vector2(1.4f, 2.4f);
            a.bloodColor = new Color(0.3f, 0.35f, 0.05f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Charge", kind = EnemyAttackKind.Charge, range = 10f, cooldown = 3f, chargeSpeed = 11f,
                    trackingFrames = 36, wallRecoveryFrames = 150, telegraphColor = new Color(1f, 0.35f, 0.1f),
                    data = EnemyAttackData("E06_Mutant_Charge", "Charge", 60, 72, 90, 30, blunt, KnockbackType.Heavy, new Vector2(10f, 5f), 30, 10, 0.6f),
                },
                new EnemyAttack
                {
                    name = "Frappe au sol", kind = EnemyAttackKind.Shockwave, range = 3.5f, cooldown = 2.5f, telegraphColor = new Color(1f, 0.7f, 0.3f),
                    data = EnemyAttackData("E06_Mutant_FrappeSol", "Frappe au sol", 54, 12, 72, 22, blunt, KnockbackType.Launch, new Vector2(2f, 9f), 30, 9, 0.5f),
                },
                new EnemyAttack
                {
                    name = "Balayage", range = 2f, cooldown = 1f, telegraphColor = FlashYellow,
                    data = EnemyAttackData("E06_Mutant_Balayage", "Balayage", 42, 9, 48, 18, blunt, KnockbackType.Heavy, new Vector2(9f, 3f), 26, 8, 0.4f),
                },
            };
        });

        // Mannequin d'entraînement : ne riposte pas, se fait projeter, rebondir et démembrer.
        // Les trois Voyous partagent les sprites du Voyou classique (armes à modéliser plus tard).
        foreach (EnemyArchetype thug in new[] { fists, knife, bar })
            ApplySprites(thug, voyouSprites, new Color(0.16f, 0.19f, 0.32f));

        ApplyCopSprites(cop, copSprites);
        ApplyDealer(dealer, dealerSprites);

        // Morceaux de corps (fusil à pompe à bout portant) : humains de l'Acte I seulement.
        foreach (EnemyArchetype thug in new[] { fists, knife, bar })
            thug.gibs = EnsureGibSet("Voyou");

        cop.gibs = EnsureGibSet("Flic");
        mutant.gibbable = false;

        foreach (EnemyArchetype archetype in new[] { fists, knife, bar, cop, mutant })
            EditorUtility.SetDirty(archetype);

        EnemyArchetype dummy = EnsureArchetype("00_Mannequin", a =>
        {
            a.displayName = "Mannequin";
            a.behaviour = EnemyBehaviourType.TrainingDummy;
            a.maxHealth = 2000;
            a.moveSpeed = 0f;
            a.detectionRange = 0f;
            a.weight = EnemyWeight.Light;
            a.bodyColor = new Color(0.75f, 0.62f, 0.4f);
            a.bloodColor = new Color(0.55f, 0.02f, 0.03f);
        });

        ApplyStaticSprite(dummy, Root + "/Art/Characters/Mannequin/Sprites/Mannequin.png");
        dummy.gibbable = false;
        EditorUtility.SetDirty(dummy);

        AssetDatabase.SaveAssets();

        // Touches 1 à 8 (docs/ENEMIES.md, « Composition des rencontres de l'arène de test »).
        EnemyWave shotgunTest = Wave("M4 + fusil à pompe contre 5 Voyous", Array.Empty<float>(),
            (fists, 2f), (knife, 2.6f), (fists, 3.2f), (bar, 3.8f), (fists, 4.4f));
        shotgunTest.nathanM4 = true;

        return new[]
        {
            Wave("1 Voyou", Array.Empty<float>(), (fists, 6f)),
            Wave("Dealer + 2 Voyous", Array.Empty<float>(), (fists, 5f), (knife, 7f), (dealer, 13f)),
            Wave("Flic corrompu", new[] { 0f, 8f }, (cop, 12f)),
            Wave("Soldat privé", new[] { -1f, 7f }, (soldier, 13f)),
            Wave("Mutant", Array.Empty<float>(), (mutant, 8f)),
            Wave("Groupe mixte", Array.Empty<float>(), (fists, 5f), (knife, 6.5f), (bar, 8f), (cop, 14f)),
            Wave("Mannequin", Array.Empty<float>(), (dummy, 12f)),
            shotgunTest,
        };
    }

    private static EnemyWave Wave(string name, float[] covers, params (EnemyArchetype archetype, float x)[] enemies) =>
        new()
        {
            name = name,
            covers = covers,
            enemies = enemies.Select(e => new EnemySpawn { archetype = e.archetype, x = e.x }).ToArray(),
        };

    private static void SetWaves(EnemyWaveManager manager, EnemyWave[] waves)
    {
        var so = new SerializedObject(manager);
        SerializedProperty property = Find(so, "waves");

        if (property == null)
            return;

        property.arraySize = waves.Length;

        for (int i = 0; i < waves.Length; i++)
            property.GetArrayElementAtIndex(i).boxedValue = waves[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Rigidbody2D AddBody(GameObject go, Vector2 size, PhysicsMaterial2D material)
    {
        var rb = go.AddComponent<Rigidbody2D>();
        rb.gravityScale = 3f;
        rb.freezeRotation = true;
        rb.interpolation = RigidbodyInterpolation2D.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

        var capsule = go.AddComponent<CapsuleCollider2D>();
        capsule.size = size;
        capsule.sharedMaterial = material;
        return rb;
    }

    private static Camera BuildCamera(Transform target)
    {
        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.position = new Vector3(target.position.x, 0.9f, -10f);

        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        // Vue de 6,8 m de haut : un personnage de 1,70 m occupe un quart de l'écran.
        camera.orthographicSize = 3.4f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.02f, 0.03f);

        cameraGo.AddComponent<AudioListener>();
        SetObject(cameraGo.AddComponent<CameraFollow>(), "target", target);
        return camera;
    }

    private static CombatHUD BuildHud(Health playerHealth, PlayerShotgun shotgun, out Image flashOverlay)
    {
        var canvasGo = new GameObject("CombatHUD", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        canvasGo.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;

        var scaler = canvasGo.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        Transform canvas = canvasGo.transform;
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        RectTransform playerFill = CreateBar(canvas, "PlayerHealth", new Vector2(0f, 1f), new Vector2(40f, -60f), new Color(0.3f, 0.55f, 1f));
        RectTransform enemyFill = CreateBar(canvas, "EnemyHealth", new Vector2(1f, 1f), new Vector2(-40f, -60f), new Color(0.9f, 0.2f, 0.2f));

        CreateText(canvas, "PlayerLabel", "NATHAN", font, 28, new Vector2(0f, 1f), new Vector2(40f, -20f), new Vector2(500f, 40f), TextAnchor.MiddleLeft);
        Text enemyLabel = CreateText(canvas, "EnemyLabel", "ENNEMIS", font, 28, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(500f, 40f), TextAnchor.MiddleRight);
        Text ammo = CreateText(canvas, "Ammo", string.Empty, font, 26, new Vector2(0f, 1f), new Vector2(40f, -105f), new Vector2(600f, 36f), TextAnchor.MiddleLeft);
        ammo.enabled = false;
        CreateText(canvas, "Controls",
            "Q / D : se déplacer   ·   S : bas   ·   Espace : sauter   ·   J : léger   ·   K : lourd (S + K : lanceur)   ·   L ou double appui : dash   ·   Maj gauche : esquiver\n" +
            "U : fusil à pompe (M4)   ·   I : recharger   ·   1 à 8 : vagues de test   ·   Échap : options   ·   F1 : debug   ·   F2 : arme de test   ·   F3 : démo   ·   F4 : Nathan M4",
            font, 22, new Vector2(0.5f, 0f), new Vector2(0f, 40f), new Vector2(1800f, 60f), TextAnchor.MiddleCenter);
        Text message = CreateText(canvas, "Message", string.Empty, font, 72, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f), TextAnchor.MiddleCenter);
        message.enabled = false;

        // Image d'impact plein écran, au-dessus du jeu et de l'interface de combat.
        RectTransform flash = CreateRect("ImpactFlash", canvas);
        flash.anchorMin = Vector2.zero;
        flash.anchorMax = Vector2.one;
        flash.offsetMin = Vector2.zero;
        flash.offsetMax = Vector2.zero;
        flashOverlay = flash.gameObject.AddComponent<Image>();
        flashOverlay.raycastTarget = false;
        flashOverlay.enabled = false;

        BuildOptionsMenu(canvasGo, font);

        var hud = canvasGo.AddComponent<CombatHUD>();
        SetObject(hud, "playerHealth", playerHealth);
        SetObject(hud, "playerFill", playerFill);
        SetObject(hud, "enemyFill", enemyFill);
        SetObject(hud, "enemyLabel", enemyLabel);
        SetObject(hud, "messageText", message);
        SetObject(hud, "shotgun", shotgun);
        SetObject(hud, "ammoText", ammo);
        return hud;
    }

    private static void BuildOptionsMenu(GameObject canvasGo, Font font)
    {
        RectTransform panel = CreateRect("OptionsPanel", canvasGo.transform);
        panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
        panel.sizeDelta = new Vector2(1000f, 520f);
        panel.gameObject.AddComponent<Image>().color = new Color(0.05f, 0.02f, 0.03f, 0.92f);

        Text body = CreateText(panel, "Body", string.Empty, font, 36, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 460f), TextAnchor.MiddleLeft);
        body.supportRichText = true;
        body.horizontalOverflow = HorizontalWrapMode.Wrap;

        var menu = canvasGo.AddComponent<OptionsMenu>();
        SetObject(menu, "panel", panel.gameObject);
        SetObject(menu, "body", body);
    }

    private static RectTransform CreateBar(Transform parent, string name, Vector2 anchor, Vector2 position, Color fillColor)
    {
        RectTransform background = CreateRect(name, parent);
        background.anchorMin = background.anchorMax = background.pivot = anchor;
        background.anchoredPosition = position;
        background.sizeDelta = new Vector2(500f, 28f);
        background.gameObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.6f);

        RectTransform fill = CreateRect("Fill", background);
        fill.anchorMin = Vector2.zero;
        fill.anchorMax = Vector2.one;
        fill.offsetMin = Vector2.zero;
        fill.offsetMax = Vector2.zero;
        fill.gameObject.AddComponent<Image>().color = fillColor;
        return fill;
    }

    private static Text CreateText(Transform parent, string name, string content, Font font, int size,
        Vector2 anchor, Vector2 position, Vector2 dimensions, TextAnchor alignment)
    {
        RectTransform rect = CreateRect(name, parent);
        rect.anchorMin = rect.anchorMax = rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = dimensions;

        var text = rect.gameObject.AddComponent<Text>();
        text.text = content;
        text.font = font;
        text.fontSize = size;
        text.alignment = alignment;
        text.color = Color.white;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        return text;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        return (RectTransform)go.transform;
    }

    private static GameObject CreateSprite(string name, Transform parent, Sprite sprite, Color color,
        Vector3 localPosition, Vector3 localScale, int sortingOrder)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPosition;
        go.transform.localScale = new Vector3(localScale.x, localScale.y, 1f);

        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.color = color;
        renderer.sortingOrder = sortingOrder;
        return go;
    }

    private static void AddSceneToBuildSettings(string path)
    {
        var scenes = EditorBuildSettings.scenes.ToList();

        if (scenes.Any(s => s.path == path))
            return;

        scenes.Insert(0, new EditorBuildSettingsScene(path, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // ------------------------------------------- Champs privés [SerializeField]

    private static SerializedProperty Find(SerializedObject so, string field)
    {
        SerializedProperty property = so.FindProperty(field);

        if (property == null)
            Debug.LogError($"[RED MET] Champ introuvable : {so.targetObject.GetType().Name}.{field}");

        return property;
    }

    private static void SetObject(Component component, string field, UnityEngine.Object value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetInt(Component component, string field, int value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.intValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetBool(Component component, string field, bool value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.boolValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetFloat(Component component, string field, float value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetVector2(Component component, string field, Vector2 value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.vector2Value = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetObjects(Component component, string field, UnityEngine.Object[] values)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.arraySize = values.Length;

        for (int i = 0; i < values.Length; i++)
            property.GetArrayElementAtIndex(i).objectReferenceValue = values[i];

        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
