using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;

// Menu RED MET > Importer les sprites des personnages.
// Pour chaque dossier Assets/_Game/Art/Characters/<Perso>/Sprites contenant un sprites.json
// (écrit par tools/blender/redmet_animation.py) : découpe les feuilles en grille, règle l'échelle
// (1 unité = 1 m) et le pivot aux pieds, crée un clip par animation à 30 images/s et un
// Animator Controller piloté par CharacterAnimator. Relancer la commande remplace le tout.
public static class SpriteAnimationBuilder
{
    private const string CharactersRoot = "Assets/_Game/Art/Characters";
    private const string AnimationsRoot = "Assets/_Game/Animations";

    // Ordre des attaques de Nathan : AttackData.animationId → clip.
    private static readonly string[] AttackClips = { "jab", "cross", "hook", "heavy", "launcher", "air1", "air2", "air3", "smash" };

    [Serializable]
    private class SheetInfo
    {
        public float fps = 30f;
        public float pixels_per_meter = 100f;
        public float feet_from_bottom_m;
        public ClipInfo[] clips;
    }

    [Serializable]
    private class ClipInfo
    {
        public string name;
        public int frames;
        public int columns;
        public int rows;
        public int[] size;
        public bool loop;
    }

    public class Result
    {
        public AnimatorController Controller;
        public Sprite IdleSprite;
    }

    [MenuItem("RED MET/Importer les sprites des personnages")]
    public static void BuildAll()
    {
        foreach (string folder in Directory.GetDirectories(CharactersRoot))
        {
            string character = Path.GetFileName(folder);

            if (File.Exists(Path.Combine(folder, "Sprites", "sprites.json")))
                Build(character, character != "Nathan_M0");
        }
    }

    // enemy : l'attaque joue « enemy_punch » quel que soit ComboStep.
    public static Result Build(string character, bool enemy)
    {
        string sheetFolder = $"{CharactersRoot}/{character}/Sprites";
        string jsonPath = $"{sheetFolder}/sprites.json";

        if (!File.Exists(jsonPath))
        {
            Debug.LogError($"[RED MET] {jsonPath} introuvable : lancez d'abord le rendu Blender.");
            return null;
        }

        var info = JsonUtility.FromJson<SheetInfo>(File.ReadAllText(jsonPath));
        string prefix = Directory.GetFiles(sheetFolder, "*.png").Select(Path.GetFileNameWithoutExtension)
            .First(n => n.EndsWith("_idle")).Replace("_idle", string.Empty);

        string animFolder = $"{AnimationsRoot}/{character}";
        Directory.CreateDirectory(animFolder);
        AssetDatabase.Refresh();

        var clips = new Dictionary<string, AnimationClip>();
        Sprite idleSprite = null;

        foreach (ClipInfo clip in info.clips)
        {
            string sheetPath = $"{sheetFolder}/{prefix}_{clip.name}.png";
            Sprite[] sprites = SliceSheet(sheetPath, clip, info);
            clips[clip.name] = SaveClip($"{animFolder}/{clip.name}.anim", sprites, info.fps, clip.loop);

            if (clip.name == "idle")
                idleSprite = sprites[0];
        }

        AnimatorController controller = BuildController($"{animFolder}/{character}.controller", clips, enemy);
        AssetDatabase.SaveAssets();
        Debug.Log($"[RED MET] {character} : {clips.Count} animations importées.");
        return new Result { Controller = controller, IdleSprite = idleSprite };
    }

    private static Sprite[] SliceSheet(string path, ClipInfo clip, SheetInfo info)
    {
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.spritePixelsPerUnit = info.pixels_per_meter;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.filterMode = FilterMode.Bilinear;
        importer.maxTextureSize = 8192;
        importer.textureCompression = TextureImporterCompression.CompressedHQ;

        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        provider.InitSpriteEditorDataProvider();

        int w = clip.size[0], h = clip.size[1];
        float pivotY = info.feet_from_bottom_m * info.pixels_per_meter / h;
        var rects = new List<SpriteRect>();

        // Grille lue de gauche à droite et de haut en bas ; l'origine des textures est en bas.
        for (int i = 0; i < clip.frames; i++)
        {
            int row = i / clip.columns, column = i % clip.columns;

            rects.Add(new SpriteRect
            {
                name = $"{Path.GetFileNameWithoutExtension(path)}_{i:00}",
                rect = new Rect(column * w, (clip.rows - 1 - row) * h, w, h),
                alignment = SpriteAlignment.Custom,
                pivot = new Vector2(0.5f, pivotY),
                spriteID = GUID.Generate(),
            });
        }

        provider.SetSpriteRects(rects.ToArray());

        var names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        names?.SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)).ToList());

        provider.Apply();
        importer.SaveAndReimport();

        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => s.name, StringComparer.Ordinal).ToArray();
    }

    private static AnimationClip SaveClip(string path, Sprite[] sprites, float fps, bool loop)
    {
        var clip = new AnimationClip { frameRate = fps };
        var binding = EditorCurveBinding.PPtrCurve(string.Empty, typeof(SpriteRenderer), "m_Sprite");
        var keys = new ObjectReferenceKeyframe[sprites.Length + 1];

        for (int i = 0; i < sprites.Length; i++)
            keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[i] };

        // Dernière image tenue jusqu'au bout du clip.
        keys[^1] = new ObjectReferenceKeyframe { time = sprites.Length / fps, value = sprites[^1] };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

        AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
        settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);

        // On remplace le contenu d'un clip existant pour garder ses références.
        var existing = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (existing == null)
        {
            AssetDatabase.CreateAsset(clip, path);
            return clip;
        }

        EditorUtility.CopySerialized(clip, existing);
        UnityEngine.Object.DestroyImmediate(clip);
        return existing;
    }

    private static AnimatorController BuildController(string path, Dictionary<string, AnimationClip> clips, bool enemy)
    {
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);

        foreach ((string name, AnimatorControllerParameterType type) in new[]
                 {
                     ("Speed", AnimatorControllerParameterType.Float), ("VerticalSpeed", AnimatorControllerParameterType.Float),
                     ("Grounded", AnimatorControllerParameterType.Bool), ("Dead", AnimatorControllerParameterType.Bool),
                     ("Stunned", AnimatorControllerParameterType.Bool), ("Down", AnimatorControllerParameterType.Bool),
                     ("ComboStep", AnimatorControllerParameterType.Int), ("Attack", AnimatorControllerParameterType.Trigger),
                     ("Dodge", AnimatorControllerParameterType.Trigger), ("Dash", AnimatorControllerParameterType.Trigger),
                     ("Hurt", AnimatorControllerParameterType.Trigger),
                 })
        {
            controller.AddParameter(name, type);
        }

        AnimatorStateMachine sm = controller.layers[0].stateMachine;
        AnimatorState idle = AddState(sm, "Idle", clips["idle"]);
        sm.defaultState = idle;
        AnimatorState run = AddState(sm, "Run", clips["run"]);
        AnimatorState jump = AddState(sm, "Jump", clips["jump"]);
        AnimatorState hurt = AddState(sm, "Hurt", clips["hurt"]);
        AnimatorState down = AddState(sm, "Down", clips["death"]);
        AnimatorState dead = AddState(sm, "Dead", clips["death"]);
        AnimatorState dash = AddState(sm, "Dash", clips["dash"]);
        AnimatorState dodge = AddState(sm, "Dodge", clips["dodge"]);

        // Priorité des transitions depuis n'importe quel état : mort, au sol, attaques, esquive, coup reçu.
        AnyTo(sm, dead, false).AddCondition(AnimatorConditionMode.If, 0, "Dead");
        AnyTo(sm, down, false).AddCondition(AnimatorConditionMode.If, 0, "Down");
        Leave(down, idle, false).AddCondition(AnimatorConditionMode.IfNot, 0, "Down");

        if (enemy)
        {
            AnimatorState attack = AddState(sm, "Attack", clips["enemy_punch"]);
            AnyTo(sm, attack, true).AddCondition(AnimatorConditionMode.If, 0, "Attack");
            Leave(attack, idle, true);
        }
        else
        {
            for (int i = 0; i < AttackClips.Length; i++)
            {
                AnimatorState attack = AddState(sm, "Attack_" + AttackClips[i], clips[AttackClips[i]]);
                AnimatorStateTransition into = AnyTo(sm, attack, true);
                into.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                into.AddCondition(AnimatorConditionMode.Equals, i, "ComboStep");
                Leave(attack, idle, true);
            }
        }

        AnyTo(sm, dash, true).AddCondition(AnimatorConditionMode.If, 0, "Dash");
        Leave(dash, idle, true);
        AnyTo(sm, dodge, true).AddCondition(AnimatorConditionMode.If, 0, "Dodge");
        Leave(dodge, idle, true);
        AnyTo(sm, hurt, true).AddCondition(AnimatorConditionMode.If, 0, "Hurt");
        Leave(hurt, idle, true);

        // Déplacement.
        Leave(idle, run, false).AddCondition(AnimatorConditionMode.Greater, 0.5f, "Speed");
        Leave(run, idle, false).AddCondition(AnimatorConditionMode.Less, 0.5f, "Speed");
        Leave(idle, jump, false).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        Leave(run, jump, false).AddCondition(AnimatorConditionMode.IfNot, 0, "Grounded");
        Leave(jump, idle, false).AddCondition(AnimatorConditionMode.If, 0, "Grounded");

        return controller;
    }

    private static AnimatorState AddState(AnimatorStateMachine sm, string name, AnimationClip clip)
    {
        AnimatorState state = sm.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = false;
        return state;
    }

    // Transitions immédiates : un jeu de combat ne fond pas ses poses.
    private static AnimatorStateTransition AnyTo(AnimatorStateMachine sm, AnimatorState state, bool toSelf)
    {
        AnimatorStateTransition t = sm.AddAnyStateTransition(state);
        t.duration = 0f;
        t.hasExitTime = false;
        t.canTransitionToSelf = toSelf;
        return t;
    }

    private static AnimatorStateTransition Leave(AnimatorState from, AnimatorState to, bool atEnd)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.duration = 0f;
        t.hasExitTime = atEnd;
        t.exitTime = 1f;
        return t;
    }
}
