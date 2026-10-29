using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Applique le ciel d'un chapitre (SkyProfile) au décor construit par RedMoonDecor : ciel peint, astres,
// nuages, paysage, brouillard, aurores, éclairs, particules, lumière globale 2D, liseré des personnages et
// post-traitement. Tous les objets existent déjà dans la scène : changer de ciel ne crée ni ne détruit rien.
// F12 (GameInput.DebugSkyPressed) passe au chapitre suivant dans l'arène de test.
public class SkyController : MonoBehaviour
{
    [Serializable]
    public class CelestialSlot
    {
        public SpriteRenderer body;
        public SpriteRenderer halo;
        public Light2D light;
    }

    [Serializable]
    public class LandscapeLayer
    {
        public SpriteRenderer renderer;
        public Sprite original;
        public Sprite neutral;
        // Teinte de la couche dans le décor d'origine (plus sombre au premier plan).
        public Color baseTint = Color.white;
    }

    private static readonly int RimColorId = Shader.PropertyToID("_SkyRimColor");
    private static readonly int RimDirectionId = Shader.PropertyToID("_SkyRimDirection");
    private static readonly int AmbientId = Shader.PropertyToID("_SkyAmbient");
    private static readonly int ColorAId = Shader.PropertyToID("_ColorA");
    private static readonly int ColorBId = Shader.PropertyToID("_ColorB");
    private static readonly int IntensityId = Shader.PropertyToID("_Intensity");
    // Teinte du shader RED MET/Sprite Glow (astres, halos, éclairs) : la couleur du SpriteRenderer n'y arrive pas.
    private static readonly int TintId = Shader.PropertyToID("_Tint");
    private static readonly int MainTexId = Shader.PropertyToID("_MainTex");
    private static readonly int SpeedId = Shader.PropertyToID("_Speed");

    [SerializeField] private SkyProfile[] chapters = new SkyProfile[10];
    [SerializeField] private int startChapter = 1;
    // Départ de la caméra : les astres sont placés par rapport à lui (RedMoonDecor.CameraStart).
    [SerializeField] private Vector2 cameraStart = new(-4f, 0.9f);

    [Header("Décor")]
    [SerializeField] private SpriteRenderer sky;
    [SerializeField] private SpriteRenderer clouds;
    [SerializeField] private CelestialSlot[] celestials = Array.Empty<CelestialSlot>();
    [SerializeField] private LandscapeLayer[] landscape = Array.Empty<LandscapeLayer>();
    [SerializeField] private SpriteRenderer mist;
    [SerializeField] private SpriteRenderer horizon;
    [SerializeField] private SpriteRenderer frontFog;
    [SerializeField] private SpriteRenderer aurora;
    [SerializeField] private SpriteRenderer flash;
    [SerializeField] private SpriteRenderer[] bolts = Array.Empty<SpriteRenderer>();

    [Header("Ambiance")]
    [SerializeField] private ParticleSystem ash;
    [SerializeField] private ParticleSystem embers;
    [SerializeField] private ParticleSystem rain;
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Volume volume;

    private SkyProfile profile;
    private MaterialPropertyBlock block;
    private float nextStrike;
    private float strikeTime = -10f;
    private float strikeStrength;
    private float frontFogDrift;
    private Bloom bloom;
    private ColorAdjustments colorAdjustments;
    private Vector3 frontFogStart;

    public int Chapter { get; private set; }
    public SkyProfile Profile => profile;

    private void Awake()
    {
        block = new MaterialPropertyBlock();

        if (volume != null)
        {
            // Copie propre à la partie : le profil de post-traitement du projet n'est jamais modifié.
            VolumeProfile instance = volume.profile;
            instance.TryGet(out bloom);
            instance.TryGet(out colorAdjustments);
        }

        if (frontFog != null)
            frontFogStart = frontFog.transform.localPosition;

        Apply(startChapter);
    }

    private void OnDisable()
    {
        Shader.SetGlobalVector(RimColorId, Vector4.zero);
        Shader.SetGlobalVector(RimDirectionId, Vector4.zero);
        Shader.SetGlobalVector(AmbientId, Vector4.zero);
    }

    public void Apply(int chapter)
    {
        chapter = Mathf.Clamp(chapter, 1, chapters.Length);
        SkyProfile next = chapters[chapter - 1];

        if (next == null)
            return;

        Chapter = chapter;
        profile = next;

        sky.sprite = profile.sky;
        sky.color = profile.skyTint;
        ScaleToWidth(sky, profile.skyWidth);

        clouds.sprite = profile.clouds;
        clouds.color = profile.cloudTint;
        clouds.enabled = profile.clouds != null;
        SetDrift(clouds, profile.cloudDrift);

        for (int i = 0; i < celestials.Length; i++)
            ApplyCelestial(celestials[i], i < profile.celestials.Length ? profile.celestials[i] : null);

        foreach (LandscapeLayer layer in landscape)
        {
            layer.renderer.sprite = profile.neutralLandscape && layer.neutral != null ? layer.neutral : layer.original;
            layer.renderer.color = layer.baseTint * profile.landscapeTint;
        }

        mist.color = profile.fogColor;
        SetDrift(mist, profile.fogDrift);
        horizon.color = profile.horizonColor;
        frontFog.enabled = profile.frontFog > 0f;
        frontFog.color = new Color(profile.fogColor.r, profile.fogColor.g, profile.fogColor.b, profile.frontFog);

        aurora.enabled = profile.aurora;

        if (profile.aurora)
        {
            block.Clear();
            block.SetColor(ColorAId, profile.auroraColorA);
            block.SetColor(ColorBId, profile.auroraColorB);
            block.SetFloat(IntensityId, profile.auroraIntensity);
            block.SetFloat(SpeedId, profile.auroraSpeed);
            aurora.SetPropertyBlock(block);
        }

        strikeTime = -10f;
        nextStrike = Time.time + UnityEngine.Random.Range(profile.lightningInterval.x, profile.lightningInterval.y);
        flash.enabled = false;

        foreach (SpriteRenderer bolt in bolts)
            bolt.enabled = false;

        SetParticles(ash, profile.ashRate, profile.ashColor);
        SetParticles(embers, profile.emberRate, null);
        SetParticles(rain, profile.rainRate, profile.rainColor);

        if (rain != null)
        {
            // Pluie légèrement en biais ; les gouttes s'étirent selon leur vitesse (rendu Stretch).
            ParticleSystem.VelocityOverLifetimeModule velocity = rain.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(-0.15f * profile.rainSpeed);
            velocity.y = new ParticleSystem.MinMaxCurve(-profile.rainSpeed);
            velocity.z = new ParticleSystem.MinMaxCurve(0f);
        }

        Shader.SetGlobalColor(RimColorId, profile.rimColor);
        Shader.SetGlobalVector(RimDirectionId, new Vector4(profile.rimDirection.x, profile.rimDirection.y, 0f, 1f));
        ApplyLight(0f);
        ApplyPostProcessing();
    }

    private void Update()
    {
        if (GameInput.DebugSkyPressed)
        {
            Apply(Chapter % chapters.Length + 1);
            Debug.Log($"[RED MET] Ciel du chapitre {Chapter} ({profile.name}).");
        }

        if (profile == null)
            return;

        if (profile.frontFog > 0f)
        {
            // Le brouillard roule lentement devant le plan de jeu (couche en mosaïque de 8 m).
            frontFogDrift = Mathf.Repeat(frontFogDrift + profile.fogDrift * 0.6f * Time.deltaTime, 8f);
            frontFog.transform.localPosition = frontFogStart + new Vector3(frontFogDrift, 0f);
        }

        if (profile.lightning)
            UpdateLightning();
    }

    // Éclair : un trait dans le ciel, un flash sur le ciel (le paysage passe en silhouette) et une montée de
    // la lumière globale, en deux battements. Sans les flashs (option d'accessibilité), un seul éclat doux.
    private void UpdateLightning()
    {
        if (Time.time >= nextStrike)
        {
            strikeTime = Time.time;
            strikeStrength = UnityEngine.Random.Range(0.6f, 1f);
            nextStrike = Time.time + UnityEngine.Random.Range(profile.lightningInterval.x, profile.lightningInterval.y);

            if (bolts.Length > 0)
            {
                SpriteRenderer bolt = bolts[UnityEngine.Random.Range(0, bolts.Length)];
                Transform t = bolt.transform;
                t.localPosition = new Vector3(UnityEngine.Random.Range(-7f, 7f), t.localPosition.y, t.localPosition.z);
                t.localScale = new Vector3(UnityEngine.Random.value < 0.5f ? -1f : 1f, 1f, 1f);
                SetGlow(bolt, profile.lightningColor, 2.5f);
            }
        }

        float age = Time.time - strikeTime;
        bool flashes = GameOptions.ImpactFlashes;
        float pulse;

        if (flashes)
            pulse = Mathf.Max(Mathf.Clamp01(1f - age / 0.12f), 0.7f * Mathf.Clamp01(1f - Mathf.Abs(age - 0.2f) / 0.08f),
                Mathf.Clamp01(1f - age / 0.5f) * 0.35f);
        else
            pulse = 0.25f * Mathf.Sin(Mathf.Clamp01(age / 0.6f) * Mathf.PI);

        pulse *= strikeStrength;
        bool active = age < 0.6f;
        flash.enabled = active;
        flash.color = new Color(profile.lightningColor.r, profile.lightningColor.g, profile.lightningColor.b, 0.55f * pulse);

        foreach (SpriteRenderer bolt in bolts)
            bolt.enabled = false;

        if (active && flashes && age < 0.3f && bolts.Length > 0)
            bolts[Mathf.Abs(Mathf.FloorToInt(strikeTime * 1000f)) % bolts.Length].enabled = true;

        ApplyLight(pulse * profile.lightningFlash);
    }

    private void ApplyLight(float boost)
    {
        if (globalLight != null)
        {
            globalLight.color = Color.Lerp(profile.lightColor, profile.lightningColor, Mathf.Clamp01(boost));
            globalLight.intensity = profile.lightIntensity * (1f + boost);
        }

        Color ambient = Color.Lerp(profile.ambient, Color.white, Mathf.Clamp01(boost * 0.5f));
        Shader.SetGlobalVector(AmbientId, new Vector4(ambient.r, ambient.g, ambient.b, 1f));
    }

    private void ApplyCelestial(CelestialSlot slot, SkyProfile.Celestial data)
    {
        bool on = data != null && data.sprite != null;
        slot.body.gameObject.SetActive(on);

        if (!on)
            return;

        slot.body.sprite = data.sprite;
        ScaleToWidth(slot.body, data.width);
        SetGlow(slot.body, data.color, data.glow);

        var position = new Vector3(cameraStart.x + data.offset.x, cameraStart.y + data.offset.y, 0f);
        SetAnchor(slot.body.transform, position);

        slot.halo.enabled = data.haloColor.a > 0f;
        SetGlow(slot.halo, data.haloColor, 0.9f);
        // Halo enfant de l'astre : sa largeur est donnée en largeurs d'astre.
        float haloNative = slot.halo.sprite != null ? slot.halo.sprite.bounds.size.x : 1f;
        float haloScale = data.width * data.haloScale / haloNative / slot.body.transform.localScale.x;
        slot.halo.transform.localScale = new Vector3(haloScale, haloScale, 1f);

        if (slot.halo.TryGetComponent(out SkyPulse pulse))
        {
            pulse.BaseScale = haloScale;
            pulse.Amplitude = data.haloPulse;
        }

        slot.light.enabled = data.lightIntensity > 0f;
        slot.light.color = data.lightColor;
        slot.light.intensity = data.lightIntensity;
    }

    // Bloc neuf à chaque fois : relu depuis le renderer, il garderait la texture de l'image précédente.
    private void SetGlow(SpriteRenderer renderer, Color tint, float intensity)
    {
        block.Clear();

        if (renderer.sprite != null)
            block.SetTexture(MainTexId, renderer.sprite.texture);

        block.SetColor(TintId, tint);
        block.SetFloat(IntensityId, intensity);
        renderer.SetPropertyBlock(block);
    }

    private void ApplyPostProcessing()
    {
        if (bloom != null)
        {
            bloom.tint.Override(profile.bloomTint);
            bloom.intensity.Override(profile.bloomIntensity);
        }

        if (colorAdjustments != null)
        {
            colorAdjustments.colorFilter.Override(profile.colorFilter);
            colorAdjustments.postExposure.Override(profile.postExposure);
            colorAdjustments.contrast.Override(profile.contrast);
            colorAdjustments.saturation.Override(profile.saturation);
        }

        if (volume != null && volume.profile.TryGet(out Vignette vignette))
        {
            vignette.color.Override(profile.vignetteColor);
            vignette.intensity.Override(profile.vignette);
        }
    }

    private static void ScaleToWidth(SpriteRenderer renderer, float width)
    {
        if (renderer.sprite == null || width <= 0f)
            return;

        float native = renderer.sprite.bounds.size.x;
        renderer.transform.localScale = Vector3.one * (width / native);
    }

    private static void SetDrift(SpriteRenderer renderer, float drift)
    {
        if (renderer.TryGetComponent(out ParallaxLayer layer))
            layer.Drift = drift;
    }

    private static void SetAnchor(Transform target, Vector3 position)
    {
        if (target.TryGetComponent(out ParallaxLayer layer))
            layer.SetAnchor(position);
        else
            target.position = position;
    }

    private static void SetParticles(ParticleSystem ps, float rate, Color? color)
    {
        if (ps == null)
            return;

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = rate;

        if (color.HasValue)
        {
            ParticleSystem.MainModule main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color.Value, color.Value * new Color(0.6f, 0.6f, 0.6f, 0.85f));
        }

        if (rate <= 0f)
            ps.Clear();
        else if (!ps.isPlaying)
            ps.Play();
    }
}
