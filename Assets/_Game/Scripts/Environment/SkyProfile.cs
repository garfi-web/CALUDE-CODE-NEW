using System;
using UnityEngine;

// Ciel d'un chapitre du monde parallèle (demande de l'auteur du 2026-10-29) : la lune rouge est réservée
// aux ch. 1 et 10, qui se répondent ; les ch. 2 à 9 ont chacun leur ciel. L'identité Red Met reste portée
// par les fissures rouges du sol, jamais par le ciel.
// Toutes les valeurs vivent ici (Data/Sky/chNN.asset) ; SkyController ne fait que les appliquer.
[CreateAssetMenu(menuName = "RED MET/Ciel de chapitre", fileName = "SkyProfile")]
public class SkyProfile : ScriptableObject
{
    [Serializable]
    public class Celestial
    {
        public Sprite sprite;
        // Position par rapport au départ de la caméra, en mètres, et largeur du disque.
        public Vector2 offset = new(3.6f, 1.9f);
        public float width = 4.4f;
        public Color color = Color.white;
        [Range(0f, 4f)] public float glow = 1f;
        // Halo autour de l'astre (alpha 0 = aucun), largeur en largeurs d'astre, pulsation lente (reflets du soleil).
        public Color haloColor = new(1f, 1f, 1f, 0f);
        public float haloScale = 1.82f;
        [Range(0f, 0.5f)] public float haloPulse;
        // Lumière ponctuelle 2D portée par l'astre (0 = aucune).
        public Color lightColor = Color.white;
        public float lightIntensity;
    }

    [Header("Ciel peint")]
    public Sprite sky;
    public Color skyTint = Color.white;
    public float skyWidth = 17f;
    public Celestial[] celestials = Array.Empty<Celestial>();

    [Header("Nuages qui dérivent")]
    public Sprite clouds;
    public Color cloudTint = new(1f, 1f, 1f, 0.8f);
    public float cloudDrift = 0.22f;

    [Header("Paysage (couches en parallaxe)")]
    // Faux : couches peintes rouges d'origine (ch. 1 et 10) ; vrai : versions neutres, colorées par la lumière.
    public bool neutralLandscape;
    public Color landscapeTint = Color.white;

    [Header("Brouillard")]
    public Color fogColor = new(0.42f, 0.1f, 0.1f, 1f);
    public float fogDrift = 0.35f;
    public Color horizonColor = new(0.55f, 0.08f, 0.06f, 1f);
    // Brouillard qui roule devant le plan de jeu (aube blafarde), 0 = aucun.
    [Range(0f, 1f)] public float frontFog;

    [Header("Aurores")]
    public bool aurora;
    public Color auroraColorA = new(0.2f, 1f, 0.55f, 1f);
    public Color auroraColorB = new(0.2f, 0.55f, 1f, 1f);
    [Range(0f, 2f)] public float auroraIntensity = 1f;
    public float auroraSpeed = 0.15f;

    [Header("Éclairs")]
    public bool lightning;
    public Vector2 lightningInterval = new(2.5f, 6f);
    public Color lightningColor = new(0.85f, 0.9f, 1f, 1f);
    [Range(0f, 3f)] public float lightningFlash = 1.6f;

    [Header("Particules (par seconde)")]
    public float ashRate = 40f;
    public Color ashColor = new(0.6f, 0.55f, 0.56f, 0.85f);
    public float emberRate = 9f;
    public float rainRate;
    public Color rainColor = new(0.7f, 0.75f, 0.8f, 0.35f);
    public float rainSpeed = 14f;

    [Header("Lumière globale 2D")]
    public Color lightColor = Color.white;
    public float lightIntensity = 1f;

    [Header("Personnages")]
    // Liseré de contre-jour (vient de l'astre principal) et teinte d'ambiance multipliée.
    [ColorUsage(false, true)] public Color rimColor = new(1.6f, 0.18f, 0.1f, 1f);
    public Vector2 rimDirection = new(0.85f, 0.55f);
    public Color ambient = Color.white;

    [Header("Post-traitement")]
    public Color bloomTint = new(1f, 0.6f, 0.55f);
    public float bloomIntensity = 1.1f;
    public Color colorFilter = Color.white;
    public float postExposure;
    public float contrast = 12f;
    public float saturation = 6f;
    public Color vignetteColor = Color.black;
    public float vignette = 0.38f;
}
