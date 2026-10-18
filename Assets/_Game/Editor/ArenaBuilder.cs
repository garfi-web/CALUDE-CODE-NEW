using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Menu RED MET > Construire l'arène prototype.
// Crée les dossiers, le layer Ground, des sprites provisoires et la scène
// Assets/_Game/Scenes/Arena.unity avec joueur, ennemi, caméra et interface.
// Relancer la commande reconstruit la scène de zéro.
public static class ArenaBuilder
{
    private const string Root = "Assets/_Game";
    private const string ScenePath = Root + "/Scenes/Arena.unity";
    private const string SpritePath = Root + "/Art/Placeholder/Square.png";
    private const string NoFrictionPath = Root + "/Settings/NoFriction.physicsMaterial2D";
    private const string GroundLayerName = "Ground";

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
    };

    [MenuItem("RED MET/Construire l'arène prototype")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        foreach (string folder in Folders)
            EnsureFolder(folder);

        int groundLayer = EnsureLayer(GroundLayerName);

        if (groundLayer < 0)
        {
            Debug.LogError("[RED MET] Aucun emplacement libre pour créer le layer Ground.");
            return;
        }

        Sprite square = EnsureSquareSprite();
        PhysicsMaterial2D noFriction = EnsureNoFrictionMaterial();

        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateGlobalLight();
        BuildLevel(square, groundLayer);

        GameObject player = BuildPlayer(square, noFriction, groundLayer);
        GameObject enemy = BuildEnemy(square, noFriction, player.transform);

        BuildCamera(player.transform);

        Health playerHealth = player.GetComponent<Health>();
        Health enemyHealth = enemy.GetComponent<Health>();
        CombatHUD hud = BuildHud(playerHealth, enemyHealth);

        var manager = new GameObject("GameManager").AddComponent<GameManager>();
        SetObject(manager, "player", playerHealth);
        SetObject(manager, "hud", hud);
        SetArray(manager, "enemies", enemyHealth);

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

    // ----------------------------------------------------------------- Scene

    // Ajoute une lumière globale 2D si l'URP est présent, sans en dépendre à la compilation.
    private static void CreateGlobalLight()
    {
        Type lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");

        if (lightType == null)
            return;

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

    private static GameObject BuildPlayer(Sprite square, PhysicsMaterial2D noFriction, int groundLayer)
    {
        var player = new GameObject("Player") { tag = "Player" };
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

        player.AddComponent<CharacterAnimator>();
        return player;
    }

    private static GameObject BuildEnemy(Sprite square, PhysicsMaterial2D noFriction, Transform target)
    {
        var enemy = new GameObject("Enemy");
        enemy.transform.position = new Vector3(7f, -0.55f, 0f);

        GameObject visual = CreateSprite("Visual", enemy.transform, square, new Color(0.8f, 0.15f, 0.15f), Vector3.zero, new Vector3(0.9f, 1.7f), 5);
        CreateSprite("Eye", enemy.transform, square, Color.black, new Vector3(0.25f, 0.5f), new Vector3(0.15f, 0.15f), 6);

        AddBody(enemy, new Vector2(0.9f, 1.7f), noFriction);

        var health = enemy.AddComponent<Health>();
        SetInt(health, "maxHealth", 60);

        var ai = enemy.AddComponent<EnemyAI>();
        SetObject(ai, "target", target);
        SetObject(ai, "spriteRenderer", visual.GetComponent<SpriteRenderer>());

        enemy.AddComponent<CharacterAnimator>();
        return enemy;
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

    private static CombatHUD BuildHud(Health playerHealth, Health enemyHealth)
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
        CreateText(canvas, "EnemyLabel", "ENNEMI", font, 28, new Vector2(1f, 1f), new Vector2(-40f, -20f), new Vector2(500f, 40f), TextAnchor.MiddleRight);
        CreateText(canvas, "Controls", "Q / D : se déplacer   ·   Espace : sauter   ·   J : attaquer (x3)   ·   Maj gauche : esquiver",
            font, 24, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(1600f, 40f), TextAnchor.MiddleCenter);
        Text message = CreateText(canvas, "Message", string.Empty, font, 72, new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1400f, 300f), TextAnchor.MiddleCenter);
        message.enabled = false;

        var hud = canvasGo.AddComponent<CombatHUD>();
        SetObject(hud, "playerHealth", playerHealth);
        SetObject(hud, "enemyHealth", enemyHealth);
        SetObject(hud, "playerFill", playerFill);
        SetObject(hud, "enemyFill", enemyFill);
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

    private static void SetArray(Component component, string field, params UnityEngine.Object[] values)
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
