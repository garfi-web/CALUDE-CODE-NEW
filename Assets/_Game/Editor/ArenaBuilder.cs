using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu RED MET > Construire l'arène prototype.
// Crée les dossiers, les layers Ground, Player et Enemy, des sprites provisoires, les prefabs
// des ennemis (Enemy, Projectile, Grenade, Cover), les archétypes du bestiaire et la scène
// Assets/_Game/Scenes/Arena.unity avec joueur, vagues de test, caméra et interface.
// Relancer la commande reconstruit la scène et les prefabs de zéro. Les archétypes existants
// sont conservés, pour garder les réglages faits dans l'inspecteur : supprimez-en un pour
// le recréer avec les valeurs de docs/ENEMIES.md.
public static class ArenaBuilder
{
    private const string Root = "Assets/_Game";
    private const string ScenePath = Root + "/Scenes/Arena.unity";
    private const string SpritePath = Root + "/Art/Placeholder/Square.png";
    private const string NoFrictionPath = Root + "/Settings/NoFriction.physicsMaterial2D";
    private const string LineMaterialPath = Root + "/Art/VFX/TelegraphLine.mat";
    private const string PrefabFolder = Root + "/Prefabs";
    private const string ArchetypeFolder = Root + "/Data/Enemies";
    private const string GroundLayerName = "Ground";
    private const string PlayerLayerName = "Player";
    private const string EnemyLayerName = "Enemy";

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

        if (groundLayer < 0 || playerLayer < 0 || enemyLayer < 0)
        {
            Debug.LogError("[RED MET] Pas assez d'emplacements libres pour créer les layers Ground, Player et Enemy.");
            return;
        }

        // Les ennemis se croisent sans se bloquer (groupes, renforts, Dealer derrière ses gardes).
        Physics2D.IgnoreLayerCollision(enemyLayer, enemyLayer, true);

        Sprite square = EnsureSquareSprite();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();
        Material lineMaterial = EnsureLineMaterial();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGlobalLight();
        BuildLevel(square, groundLayer);

        GameObject player = BuildPlayer(square, noFriction, groundLayer, playerLayer);

        Projectile projectilePrefab = BuildProjectilePrefab(square);
        Grenade grenadePrefab = BuildGrenadePrefab(square, lineMaterial);
        Cover coverPrefab = BuildCoverPrefab(square, groundLayer);
        Enemy enemyPrefab = BuildEnemyPrefab(square, noFriction, lineMaterial, enemyLayer, projectilePrefab, grenadePrefab);

        BuildCamera(player.transform);

        Health playerHealth = player.GetComponent<Health>();
        CombatHUD hud = BuildHud(playerHealth);

        var waves = new GameObject("Waves").AddComponent<EnemyWaveManager>();
        SetObject(waves, "enemyPrefab", enemyPrefab);
        SetObject(waves, "coverPrefab", coverPrefab);
        SetObject(waves, "target", player.transform);
        SetWaves(waves, BuildWaves());

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        SetObject(manager, "player", playerHealth);
        SetObject(manager, "waves", waves);
        SetObject(manager, "hud", hud);

        EditorSceneManager.SaveScene(scene, ScenePath);
        AddSceneToBuildSettings(ScenePath);
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

        CreateSprite("Background", level, square, new Color(0.12f, 0.04f, 0.06f), new Vector3(0f, 4f), new Vector3(60f, 20f), -10);

        GameObject ground = CreateSprite("Ground", level, square, new Color(0.24f, 0.2f, 0.22f), new Vector3(0f, -2f), new Vector3(40f, 1f), 0);
        ground.AddComponent<BoxCollider2D>();
        ground.layer = groundLayer;

        CreatePlatform("Platform Left", level, square, new Vector3(-6f, 0.6f), groundLayer);
        CreatePlatform("Platform Right", level, square, new Vector3(5f, 1.8f), groundLayer);

        // Les murs restent sur le layer Default pour ne pas compter comme du sol.
        CreateSprite("Wall Left", level, square, new Color(0.18f, 0.14f, 0.16f), new Vector3(-20.5f, 4f), new Vector3(1f, 12f), 0)
            .AddComponent<BoxCollider2D>();
        CreateSprite("Wall Right", level, square, new Color(0.18f, 0.14f, 0.16f), new Vector3(20.5f, 4f), new Vector3(1f, 12f), 0)
            .AddComponent<BoxCollider2D>();
    }

    // Plateforme traversable par le dessous.
    private static void CreatePlatform(string name, Transform parent, Sprite square, Vector3 position, int groundLayer)
    {
        GameObject platform = CreateSprite(name, parent, square, new Color(0.35f, 0.18f, 0.2f), position, new Vector3(4f, 0.4f), 0);
        platform.layer = groundLayer;
        platform.AddComponent<BoxCollider2D>().usedByEffector = true;
        platform.AddComponent<PlatformEffector2D>().useOneWay = true;
    }

    private static GameObject BuildPlayer(Sprite square, PhysicsMaterial2D noFriction, int groundLayer, int playerLayer)
    {
        var player = new GameObject("Player") { tag = "Player", layer = playerLayer };
        player.transform.position = new Vector3(-4f, -0.6f, 0f);

        GameObject visual = CreateSprite("Visual", player.transform, square, new Color(0.2f, 0.35f, 0.9f), Vector3.zero, new Vector3(0.8f, 1.6f), 5);
        CreateSprite("Eye", player.transform, square, Color.white, new Vector3(0.22f, 0.45f), new Vector3(0.15f, 0.15f), 6);

        Rigidbody2D rb = AddBody(player, new Vector2(0.8f, 1.6f), noFriction);
        rb.mass = 1f;

        var groundCheck = new GameObject("GroundCheck").transform;
        groundCheck.SetParent(player.transform, false);
        groundCheck.localPosition = new Vector3(0f, -0.8f, 0f);

        var hitboxGo = new GameObject("AttackHitbox");
        hitboxGo.transform.SetParent(player.transform, false);
        hitboxGo.transform.localPosition = new Vector3(0.9f, 0.1f, 0f);
        var hitboxCollider = hitboxGo.AddComponent<BoxCollider2D>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.size = new Vector2(1.2f, 0.9f);
        hitboxCollider.enabled = false;
        GameObject hitboxVisual = CreateSprite("HitboxVisual", hitboxGo.transform, square, new Color(1f, 0.3f, 0.2f, 0.35f), Vector3.zero, new Vector3(1.2f, 0.9f), 7);
        SpriteRenderer hitboxRenderer = hitboxVisual.GetComponent<SpriteRenderer>();
        hitboxRenderer.enabled = false;
        var hitbox = hitboxGo.AddComponent<Hitbox>();
        SetObject(hitbox, "debugVisual", hitboxRenderer);

        var health = player.AddComponent<Health>();
        SetInt(health, "maxHealth", 100);

        var controller = player.AddComponent<PlayerController>();
        SetObject(controller, "groundCheck", groundCheck);
        SetInt(controller, "groundLayer", 1 << groundLayer);

        var combat = player.AddComponent<PlayerCombat>();
        SetObject(combat, "controller", controller);
        SetObject(combat, "attackHitbox", hitbox);

        var dodge = player.AddComponent<PlayerDodge>();
        SetObject(dodge, "controller", controller);
        SetObject(dodge, "spriteRenderer", visual.GetComponent<SpriteRenderer>());
        SetFloat(dodge, "dodgeInvulnerability", 0.4f);

        player.AddComponent<CharacterAnimator>();
        return player;
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
    private static Enemy BuildEnemyPrefab(Sprite square, PhysicsMaterial2D noFriction, Material lineMaterial,
        int enemyLayer, Projectile projectilePrefab, Grenade grenadePrefab)
    {
        var go = new GameObject("Enemy") { layer = enemyLayer };

        GameObject visual = CreateSprite("Visual", go.transform, square, Color.white, Vector3.zero, new Vector3(0.9f, 1.7f), 5);
        GameObject eye = CreateSprite("Eye", go.transform, square, Color.black, new Vector3(0.25f, 0.5f), new Vector3(0.15f, 0.15f), 6);
        LineRenderer aimLine = CreateLine("AimLine", go.transform, lineMaterial, false);

        AddBody(go, new Vector2(0.9f, 1.7f), noFriction);
        go.AddComponent<Health>();

        var telegraph = go.AddComponent<EnemyTelegraph>();
        SetObject(telegraph, "body", visual.GetComponent<SpriteRenderer>());
        SetObject(telegraph, "aimLine", aimLine);

        var enemy = go.AddComponent<Enemy>();
        SetObject(enemy, "visual", visual.transform);
        SetObject(enemy, "eye", eye.transform);
        SetObject(enemy, "telegraph", telegraph);
        SetObject(enemy, "projectilePrefab", projectilePrefab);
        SetObject(enemy, "grenadePrefab", grenadePrefab);

        go.AddComponent<CharacterAnimator>();
        return SavePrefab<Enemy>(go);
    }

    // ----------------------------------------------------- Bestiaire (ENEMIES.md)

    private static readonly Color FlashYellow = new(1f, 0.85f, 0.2f);
    private static readonly Color AimRed = new(1f, 0.15f, 0.15f);

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
            a.hitStunTime = 0.3f;
            a.bodyColor = color;
            a.attacks = new[] { attack };
        });

    private static EnemyWave[] BuildWaves()
    {
        EnemyArchetype fists = Thug("01_Voyou_MainsNues", "Voyou", new Color(0.8f, 0.15f, 0.15f), new EnemyAttack
        {
            name = "Coup de poing", range = 1.2f, windup = 0.35f, active = 0.1f, recovery = 0.35f,
            cooldown = 0.5f, damage = 10, knockback = 5f, telegraphColor = FlashYellow,
        });

        EnemyArchetype knife = Thug("01_Voyou_Couteau", "Voyou au couteau", new Color(0.7f, 0.12f, 0.25f), new EnemyAttack
        {
            name = "Coup de couteau", range = 1.4f, windup = 0.45f, active = 0.08f, recovery = 0.45f,
            cooldown = 0.6f, damage = 15, knockback = 4f, telegraphColor = new Color(0.85f, 0.95f, 1f),
        });

        EnemyArchetype bar = Thug("01_Voyou_Barre", "Voyou à la barre", new Color(0.6f, 0.2f, 0.1f), new EnemyAttack
        {
            name = "Balayage à la barre", range = 1.8f, windup = 0.6f, active = 0.15f, recovery = 0.6f,
            cooldown = 0.8f, damage = 18, knockback = 7f, telegraphColor = new Color(1f, 0.55f, 0.15f),
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
            a.hitStunTime = 0.25f;
            a.bodyColor = new Color(0.55f, 0.2f, 0.65f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Ordre « Chopez-le »", kind = EnemyAttackKind.Order, windup = 1f, active = 0f, recovery = 0.5f,
                    cooldown = 8f, buffWindupMultiplier = 0.8f, buffDuration = 5f, telegraphColor = new Color(1f, 0.3f, 0.9f),
                },
                new EnemyAttack
                {
                    name = "Tir au pistolet", kind = EnemyAttackKind.Projectile, range = 8f, windup = 0.6f, recovery = 0.9f,
                    cooldown = 1.2f, damage = 14, knockback = 3f, projectileCount = 2, shotInterval = 0.25f,
                    projectileSpeed = 16f, aimLine = true, telegraphColor = AimRed,
                },
                new EnemyAttack
                {
                    name = "Coup de crosse", range = 1.1f, windup = 0.4f, active = 0.1f, recovery = 0.5f,
                    cooldown = 0.6f, damage = 10, knockback = 5f, telegraphColor = FlashYellow,
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
            a.hitStunTime = 0.25f;
            a.bodyColor = new Color(0.15f, 0.25f, 0.55f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Tir visé", kind = EnemyAttackKind.Projectile, range = 9f, windup = 0.7f, active = 0.05f,
                    recovery = 0.4f, cooldown = 0.3f, weight = 3f, damage = 14, knockback = 3f, projectileSpeed = 18f,
                    aimLine = true, aimLineWidth = 0.03f, telegraphColor = AimRed,
                },
                new EnemyAttack
                {
                    name = "Rafale de 3", kind = EnemyAttackKind.Projectile, range = 9f, windup = 0.9f, active = 0.3f,
                    recovery = 0.7f, cooldown = 0.3f, weight = 1f, damage = 10, knockback = 2f, projectileCount = 3,
                    shotInterval = 0.1f, projectileSpeed = 18f, aimLine = true, aimLineWidth = 0.1f, telegraphColor = AimRed,
                },
                new EnemyAttack
                {
                    name = "Coup de matraque", range = 1.5f, windup = 0.45f, active = 0.12f, recovery = 0.5f,
                    cooldown = 0.6f, damage = 12, knockback = 5f, telegraphColor = FlashYellow,
                },
                new EnemyAttack
                {
                    name = "Appel radio", kind = EnemyAttackKind.Summon, windup = 1.2f, active = 0f, recovery = 0.3f,
                    maxUses = 1, summon = fists, telegraphColor = new Color(0.3f, 0.9f, 1f),
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
            a.hitStunTime = 0.2f;
            a.bodyColor = new Color(0.25f, 0.3f, 0.22f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Rafale visée", kind = EnemyAttackKind.Projectile, range = 12f, windup = 0.8f, active = 0.25f,
                    recovery = 0.6f, cooldown = 0.8f, damage = 8, knockback = 2f, projectileCount = 3, shotInterval = 0.1f,
                    projectileSpeed = 22f, aimLine = true, telegraphColor = AimRed,
                },
                new EnemyAttack
                {
                    name = "Grenade", kind = EnemyAttackKind.Grenade, range = 10f, windup = 0.6f, active = 0f,
                    recovery = 0.8f, cooldown = 8f, damage = 30, knockback = 8f, areaRadius = 2.5f, fuse = 1.2f,
                    telegraphColor = new Color(1f, 0.25f, 0.1f),
                },
                new EnemyAttack
                {
                    name = "Coup de crosse", range = 1.3f, windup = 0.4f, active = 0.1f, recovery = 0.45f,
                    cooldown = 0.6f, damage = 15, knockback = 6f, telegraphColor = FlashYellow,
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
            a.hitStunTime = 0f;
            a.bodyColor = new Color(0.45f, 0.55f, 0.3f);
            a.bodySize = new Vector2(1.4f, 2.4f);
            a.attacks = new[]
            {
                new EnemyAttack
                {
                    name = "Charge", kind = EnemyAttackKind.Charge, range = 10f, windup = 1f, active = 1.2f,
                    recovery = 1.5f, wallRecovery = 2.5f, cooldown = 3f, damage = 30, knockback = 10f,
                    chargeSpeed = 11f, trackingTime = 0.6f, telegraphColor = new Color(1f, 0.35f, 0.1f),
                },
                new EnemyAttack
                {
                    name = "Frappe au sol", kind = EnemyAttackKind.Shockwave, range = 3.5f, windup = 0.9f, active = 0.2f,
                    recovery = 1.2f, cooldown = 2.5f, damage = 22, knockback = 7f, telegraphColor = new Color(1f, 0.7f, 0.3f),
                },
                new EnemyAttack
                {
                    name = "Balayage", range = 2f, windup = 0.7f, active = 0.15f, recovery = 0.8f,
                    cooldown = 1f, damage = 18, knockback = 8f, telegraphColor = FlashYellow,
                },
            };
        });

        AssetDatabase.SaveAssets();

        // Touches 1 à 6 (docs/ENEMIES.md, « Composition des rencontres de l'arène de test »).
        return new[]
        {
            Wave("1 Voyou", Array.Empty<float>(), (fists, 6f)),
            Wave("Dealer + 2 Voyous", Array.Empty<float>(), (fists, 5f), (knife, 7f), (dealer, 13f)),
            Wave("Flic corrompu", new[] { 0f, 8f }, (cop, 12f)),
            Wave("Soldat privé", new[] { -1f, 7f }, (soldier, 13f)),
            Wave("Mutant", Array.Empty<float>(), (mutant, 8f)),
            Wave("Groupe mixte", Array.Empty<float>(), (fists, 5f), (knife, 6.5f), (bar, 8f), (cop, 14f)),
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

    private static void BuildCamera(Transform target)
    {
        var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
        cameraGo.transform.position = new Vector3(target.position.x, 1.5f, -10f);

        var camera = cameraGo.AddComponent<Camera>();
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.02f, 0.03f);

        cameraGo.AddComponent<AudioListener>();
        SetObject(cameraGo.AddComponent<CameraFollow>(), "target", target);
    }

    private static CombatHUD BuildHud(Health playerHealth)
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
        CreateText(canvas, "Controls", "Q / D : se déplacer   ·   Espace : sauter   ·   J : attaquer (x3)   ·   Maj gauche : esquiver   ·   1 à 6 : vagues de test",
            font, 24, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 40f), TextAnchor.MiddleCenter);
        Text message = CreateText(canvas, "Message", string.Empty, font, 72, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f), TextAnchor.MiddleCenter);
        message.enabled = false;

        var hud = canvasGo.AddComponent<CombatHUD>();
        SetObject(hud, "playerHealth", playerHealth);
        SetObject(hud, "playerFill", playerFill);
        SetObject(hud, "enemyFill", enemyFill);
        SetObject(hud, "enemyLabel", enemyLabel);
        SetObject(hud, "messageText", message);
        return hud;
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

    private static void SetFloat(Component component, string field, float value)
    {
        var so = new SerializedObject(component);
        SerializedProperty property = Find(so, field);

        if (property == null)
            return;

        property.floatValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
}
