using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

// Crée les ciels des 10 chapitres (Data/Sky/chNN.asset) avec leurs réglages de départ, d'après la demande de
// l'auteur du 2026-10-29. Un profil déjà créé n'est jamais écrasé : les réglages se font ensuite dans
// l'asset. Seules les images manquantes sont rebranchées. Images peintes : tools/gen3d/skies.py.
public static class SkyProfiles
{
    public const string Folder = "Assets/_Game/Art/Backgrounds/Skies";
    private const string DataFolder = "Assets/_Game/Data/Sky";
    private const string RedMoonFolder = "Assets/_Game/Art/Backgrounds/RedMoonPainted";

    [MenuItem("RED MET/Recréer les ciels des chapitres")]
    public static void Recreate()
    {
        for (int chapter = 1; chapter <= 10; chapter++)
            AssetDatabase.DeleteAsset($"{DataFolder}/ch{chapter:00}.asset");

        Debug.Log("[RED MET] Ciels supprimés : relancez Construire l'arène prototype pour les recréer.");
    }

    public static SkyProfile[] EnsureAll(Dictionary<string, Sprite> art)
    {
        if (!AssetDatabase.IsValidFolder(DataFolder))
            AssetDatabase.CreateFolder("Assets/_Game/Data", "Sky");

        var profiles = new SkyProfile[10];

        for (int chapter = 1; chapter <= 10; chapter++)
        {
            string path = $"{DataFolder}/ch{chapter:00}.asset";
            var profile = AssetDatabase.LoadAssetAtPath<SkyProfile>(path);

            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<SkyProfile>();
                Defaults(chapter, profile, art);
                AssetDatabase.CreateAsset(profile, path);
            }
            else if (profile.sky == null)
            {
                // Image peinte arrivée après la création du profil.
                profile.sky = SkySprite(chapter);
                EditorUtility.SetDirty(profile);
            }

            profiles[chapter - 1] = profile;
        }

        AssetDatabase.SaveAssets();
        return profiles;
    }

    private static Sprite SkySprite(int chapter) => chapter is 1 or 10
        ? RedMoonDecor.ImportPainted($"{RedMoonFolder}/sky.png", 17f, new Vector2(0.5f, 0.5f), false)
        : RedMoonDecor.ImportPainted($"{Folder}/ch{chapter:00}_sky.png", 17f, new Vector2(0.5f, 0.5f), false);

    private static void Defaults(int chapter, SkyProfile p, Dictionary<string, Sprite> art)
    {
        p.sky = SkySprite(chapter);
        Sprite soft = art["CloudSoft"];

        // Réglages du ch. 1 (décor validé de la tranche verticale) : valeurs par défaut de SkyProfile.
        p.clouds = RedMoonDecor.ImportPainted($"{RedMoonFolder}/clouds.png", 10f, new Vector2(0.5f, 0.5f), true);
        var moon = new SkyProfile.Celestial
        {
            sprite = RedMoonDecor.ImportPainted($"{RedMoonFolder}/moon.png", 4.4f, new Vector2(0.5f, 0.5f), false),
            glow = 1.1f, haloColor = new Color(1f, 0.25f, 0.15f, 1f), lightColor = new Color(1f, 0.32f, 0.22f), lightIntensity = 0.8f,
        };

        switch (chapter)
        {
            case 1:
                p.celestials = new[] { moon };
                break;

            case 10:
                // Répond au ch. 1 : la même lune, plus grande et plus basse, plus de cendres et de braises.
                moon.width = 6.4f;
                moon.offset = new Vector2(2.4f, 1.6f);
                moon.haloPulse = 0.05f;
                moon.lightIntensity = 1.1f;
                p.celestials = new[] { moon };
                p.cloudTint = new Color(1f, 1f, 1f, 0.9f);
                p.cloudDrift = 0.4f;
                p.fogColor = new Color(0.5f, 0.08f, 0.08f);
                p.ashRate = 75f;
                p.emberRate = 22f;
                p.lightColor = new Color(1f, 0.88f, 0.88f);
                p.lightIntensity = 0.95f;
                p.rimColor = new Color(2f, 0.2f, 0.1f);
                p.rimDirection = new Vector2(0.8f, 0.45f);
                p.bloomIntensity = 1.4f;
                p.contrast = 18f;
                p.vignette = 0.45f;
                break;

            case 2:
                // Peste : ciel lourd gris-vert, bruine.
                Neutral(p, soft, new Color(0.55f, 0.6f, 0.52f, 0.6f), 0.3f, new Color(0.42f, 0.48f, 0.42f));
                Fog(p, new Color(0.45f, 0.52f, 0.42f), new Color(0.5f, 0.56f, 0.46f));
                p.rainRate = 220f;
                p.rainColor = new Color(0.75f, 0.8f, 0.72f, 0.25f);
                p.rainSpeed = 9f;
                Light(p, new Color(0.78f, 0.86f, 0.76f), 0.9f, new Color(0.75f, 0.9f, 0.7f), new Vector2(0.2f, 1f), new Color(0.85f, 0.9f, 0.82f));
                Post(p, new Color(0.8f, 0.9f, 0.75f), 0.6f, new Color(0.92f, 1f, 0.92f), 0f, 8f, -20f, new Color(0.05f, 0.08f, 0.05f), 0.4f);
                break;

            case 3:
                // Coucher de soleil chaud et brumeux, orange et rose.
                p.celestials = new[]
                {
                    new SkyProfile.Celestial
                    {
                        sprite = art["Sun"], offset = new Vector2(2f, 0.5f), width = 2.2f, color = new Color(1f, 0.75f, 0.45f), glow = 1.6f,
                        haloColor = new Color(1f, 0.55f, 0.3f, 1f), haloScale = 3.5f, haloPulse = 0.05f, lightColor = new Color(1f, 0.6f, 0.35f), lightIntensity = 0.9f,
                    },
                };
                Neutral(p, soft, new Color(1f, 0.7f, 0.6f, 0.5f), 0.12f, new Color(0.55f, 0.38f, 0.38f));
                Fog(p, new Color(0.95f, 0.6f, 0.5f), new Color(1f, 0.65f, 0.45f));
                p.emberRate = 3f;
                Light(p, new Color(1f, 0.82f, 0.7f), 1f, new Color(2f, 0.9f, 0.35f), new Vector2(0.95f, 0.3f), new Color(1f, 0.9f, 0.82f));
                Post(p, new Color(1f, 0.75f, 0.5f), 1.3f, new Color(1f, 0.95f, 0.9f), 0f, 10f, 8f, new Color(0.15f, 0.05f, 0.08f), 0.35f);
                break;

            case 4:
                // Ciel dégagé, bleu lumineux, grand soleil, quelques nuages blancs.
                p.celestials = new[]
                {
                    new SkyProfile.Celestial
                    {
                        sprite = art["Sun"], offset = new Vector2(4.5f, 3f), width = 1.6f, color = new Color(1f, 0.97f, 0.88f), glow = 2f,
                        haloColor = new Color(1f, 0.95f, 0.8f, 1f), haloScale = 4f, haloPulse = 0.08f, lightColor = new Color(1f, 0.97f, 0.9f), lightIntensity = 0.6f,
                    },
                };
                Neutral(p, soft, new Color(1f, 1f, 1f, 0.75f), 0.18f, new Color(0.62f, 0.68f, 0.78f));
                Fog(p, new Color(0.75f, 0.85f, 1f), new Color(0.8f, 0.9f, 1f));
                Light(p, new Color(1f, 1f, 0.97f), 1.15f, new Color(1.8f, 1.8f, 1.7f), new Vector2(0.6f, 0.8f), Color.white);
                Post(p, new Color(1f, 1f, 0.95f), 0.8f, Color.white, 0.1f, 8f, 10f, Color.black, 0.2f);
                break;

            case 5:
                // Nuit, aurores vertes et bleues, deux lunes pâles.
                Sprite pale = art["PaleMoon"];
                p.celestials = new[]
                {
                    new SkyProfile.Celestial
                    {
                        sprite = pale, offset = new Vector2(-3.5f, 2.6f), width = 1.5f, color = new Color(0.85f, 0.9f, 1f), glow = 1f,
                        haloColor = new Color(0.7f, 0.8f, 1f, 0.6f), haloScale = 2.5f, lightColor = new Color(0.6f, 0.75f, 1f), lightIntensity = 0.4f,
                    },
                    new SkyProfile.Celestial
                    {
                        sprite = pale, offset = new Vector2(-1.6f, 2f), width = 0.8f, color = new Color(0.9f, 0.95f, 0.9f), glow = 0.9f,
                        haloColor = new Color(0.7f, 0.9f, 0.85f, 0.5f), haloScale = 2.5f,
                    },
                };
                p.aurora = true;
                p.auroraIntensity = 1.1f;
                Neutral(p, soft, new Color(0.3f, 0.4f, 0.6f, 0.3f), 0.1f, new Color(0.25f, 0.33f, 0.42f));
                Fog(p, new Color(0.2f, 0.35f, 0.45f), new Color(0.15f, 0.4f, 0.45f));
                Light(p, new Color(0.55f, 0.75f, 0.85f), 0.85f, new Color(0.4f, 1.6f, 1.2f), new Vector2(-0.5f, 0.85f), new Color(0.75f, 0.88f, 0.95f));
                Post(p, new Color(0.6f, 1f, 0.85f), 1.2f, new Color(0.88f, 0.95f, 1f), 0f, 10f, 5f, new Color(0.02f, 0.04f, 0.08f), 0.42f);
                break;

            case 6:
                // Orage violent, éclairs, pluie.
                Neutral(p, soft, new Color(0.25f, 0.22f, 0.35f, 0.7f), 0.6f, new Color(0.3f, 0.28f, 0.42f));
                Fog(p, new Color(0.3f, 0.28f, 0.42f), new Color(0.35f, 0.3f, 0.5f));
                p.lightning = true;
                p.rainRate = 400f;
                p.rainColor = new Color(0.7f, 0.75f, 0.9f, 0.35f);
                p.rainSpeed = 16f;
                Light(p, new Color(0.55f, 0.55f, 0.75f), 0.8f, new Color(0.9f, 0.95f, 1.6f), new Vector2(0.3f, 1f), new Color(0.75f, 0.75f, 0.9f));
                Post(p, new Color(0.8f, 0.8f, 1f), 1f, new Color(0.9f, 0.9f, 1f), 0f, 16f, -10f, new Color(0.03f, 0.02f, 0.06f), 0.45f);
                break;

            case 7:
                // Aube blafarde, brouillard épais qui roule.
                p.celestials = new[]
                {
                    new SkyProfile.Celestial
                    {
                        sprite = art["Sun"], offset = new Vector2(-2.5f, 0.8f), width = 2f, color = new Color(1f, 0.97f, 0.85f, 0.5f), glow = 0.6f,
                        haloColor = new Color(1f, 1f, 0.9f, 0.4f), haloScale = 3f,
                    },
                };
                Neutral(p, soft, new Color(0.9f, 0.9f, 0.88f, 0.6f), 0.1f, new Color(0.62f, 0.62f, 0.6f));
                Fog(p, new Color(0.82f, 0.82f, 0.78f), new Color(0.85f, 0.85f, 0.8f));
                p.frontFog = 0.35f;
                p.fogDrift = 0.25f;
                Light(p, new Color(0.9f, 0.9f, 0.88f), 0.95f, new Color(1.2f, 1.2f, 1.1f), new Vector2(-0.8f, 0.6f), new Color(0.9f, 0.9f, 0.9f));
                Post(p, new Color(1f, 1f, 0.95f), 0.7f, Color.white, 0.05f, 0f, -30f, new Color(0.3f, 0.3f, 0.3f), 0.25f);
                break;

            case 8:
                // Crépuscule violet et toxique, néons de la ville réelle.
                Neutral(p, soft, new Color(0.6f, 0.3f, 0.7f, 0.5f), 0.2f, new Color(0.35f, 0.25f, 0.45f));
                Fog(p, new Color(0.5f, 0.25f, 0.6f), new Color(0.9f, 0.3f, 0.8f));
                Light(p, new Color(0.75f, 0.6f, 0.9f), 0.9f, new Color(1.6f, 0.4f, 1.5f), new Vector2(-0.7f, 0.7f), new Color(0.9f, 0.82f, 0.95f));
                Post(p, new Color(1f, 0.5f, 1f), 1.4f, new Color(0.95f, 0.9f, 1f), 0f, 14f, 15f, new Color(0.08f, 0f, 0.1f), 0.4f);
                break;

            case 9:
                // Ciel d'incendie, colonnes de fumée, pluie de cendres.
                Neutral(p, soft, new Color(0.18f, 0.12f, 0.1f, 0.75f), 0.4f, new Color(0.35f, 0.2f, 0.15f));
                Fog(p, new Color(0.6f, 0.3f, 0.15f), new Color(1f, 0.45f, 0.15f));
                p.ashRate = 90f;
                p.ashColor = new Color(0.45f, 0.42f, 0.4f, 0.85f);
                p.emberRate = 30f;
                Light(p, new Color(1f, 0.7f, 0.5f), 0.95f, new Color(2f, 0.7f, 0.2f), new Vector2(0.4f, -0.9f), new Color(1f, 0.88f, 0.8f));
                Post(p, new Color(1f, 0.6f, 0.3f), 1.4f, new Color(1f, 0.92f, 0.85f), 0f, 14f, 6f, new Color(0.1f, 0.03f, 0f), 0.42f);
                break;
        }

        // Pas de cendres rouges de la lune dans les ciels des ch. 2 à 8.
        if (chapter is >= 2 and <= 8)
        {
            p.ashRate = 0f;

            if (chapter != 3)
                p.emberRate = 0f;
        }
    }

    private static void Neutral(SkyProfile p, Sprite clouds, Color cloudTint, float drift, Color landscape)
    {
        p.clouds = clouds;
        p.cloudTint = cloudTint;
        p.cloudDrift = drift;
        p.neutralLandscape = true;
        p.landscapeTint = landscape;
    }

    private static void Fog(SkyProfile p, Color fog, Color horizon)
    {
        p.fogColor = fog;
        p.horizonColor = horizon;
    }

    private static void Light(SkyProfile p, Color color, float intensity, Color rim, Vector2 rimDirection, Color ambient)
    {
        p.lightColor = color;
        p.lightIntensity = intensity;
        p.rimColor = rim;
        p.rimDirection = rimDirection;
        p.ambient = ambient;
    }

    private static void Post(SkyProfile p, Color bloomTint, float bloom, Color filter, float exposure, float contrast, float saturation,
        Color vignetteColor, float vignette)
    {
        p.bloomTint = bloomTint;
        p.bloomIntensity = bloom;
        p.colorFilter = filter;
        p.postExposure = exposure;
        p.contrast = contrast;
        p.saturation = saturation;
        p.vignetteColor = vignetteColor;
        p.vignette = vignette;
    }
}
