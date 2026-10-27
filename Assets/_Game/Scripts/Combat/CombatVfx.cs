using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Effets d'impact et de gore, tous en réserve : systèmes de particules créés au chargement
// (Emit sans allocation), anneaux, taches et membres préparés une fois pour toutes.
// Écoute Health.AnyHit : tremblement et zoom de caméra, image d'impact, étincelles, ondes de choc,
// lignes de vitesse, sueur et salive (coups contondants), sang orienté, taches et démembrements
// (lames), selon les options (GameOptions). Le gameplay n'en dépend jamais.
public class CombatVfx : MonoBehaviour
{
    public static CombatVfx Instance { get; private set; }

    [Header("Références")]
    [SerializeField] private Material particleMaterial;
    [SerializeField] private Sprite square;
    [SerializeField] private Image flashOverlay;
    [SerializeField] private LayerMask bloodCollisionLayers;
    [SerializeField] private int debrisLayer;

    [Header("Limites (60 FPS)")]
    [SerializeField] private int maxStains = 60;
    [SerializeField] private int maxStainsReduced = 15;
    [SerializeField] private int maxLimbs = 8;
    [SerializeField] private int ringCount = 8;
    [SerializeField] private int maxStainsPerFrame = 6;

    [Header("Durées")]
    [SerializeField] private float stainLifetime = 5f;
    [SerializeField] private float stainFade = 2f;
    [SerializeField] private float limbLifetime = 6f;
    [SerializeField] private float slowMotionDuration = 0.3f;
    [SerializeField] private float slowMotionScale = 0.25f;

    [Header("Démembrement")]
    [SerializeField] private int bladeHitsToDismember = 4;
    [SerializeField] private int maxLimbsPerTarget = 2;

    private static readonly Color HumanBlood = new(0.55f, 0.02f, 0.03f);
    private static readonly Color SparkColor = new(1f, 0.9f, 0.55f);
    private static readonly Color SweatColor = new(0.8f, 0.92f, 1f, 0.9f);
    private static readonly Color DustColor = new(0.55f, 0.5f, 0.48f, 0.7f);

    private readonly List<ParticleCollisionEvent> collisionEvents = new();
    private readonly Dictionary<Health, int> bladeHits = new();
    private readonly Dictionary<Health, int> limbsTaken = new();
    private ParticleSystem sparks;
    private ParticleSystem dust;
    private ParticleSystem droplets;
    private ParticleSystem blood;
    private ParticleSystem speedLines;
    private Ring[] rings;
    private Stain[] stains;
    private Limb[] limbs;
    private int nextStain;
    private int nextLimb;
    private int stainsThisFrame;
    private int flashFramesLeft;

    private class Ring
    {
        public LineRenderer Line;
        public Vector2 Center;
        public float Age;
        public float Duration;
        public float Radius;
    }

    private class Stain
    {
        public SpriteRenderer Renderer;
        public float Age;
        public float Lifetime;
        public Color Color;
    }

    private class Limb
    {
        public Rigidbody2D Body;
        public SpriteRenderer Renderer;
        public float Age;
        public Color BloodColor;
        public Color Color;
    }

    private void Awake()
    {
        Instance = this;

        sparks = CreateSystem("Sparks", 300, 0.5f, true, 12);
        dust = CreateSystem("Dust", 200, -0.05f, false, 3);
        droplets = CreateSystem("Droplets", 200, 1.5f, false, 11);
        blood = CreateSystem("Blood", 600, 2f, false, 11);
        speedLines = CreateSystem("SpeedLines", 120, 0f, true, 13);

        // Gouttes de sang : elles meurent en touchant le sol ou un mur, et y laissent une tache.
        ParticleSystem.CollisionModule collision = blood.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision2D;
        collision.collidesWith = bloodCollisionLayers;
        collision.bounce = 0f;
        collision.lifetimeLoss = 1f;
        collision.sendCollisionMessages = true;
        blood.gameObject.AddComponent<BloodCollisionRelay>().Owner = this;

        rings = new Ring[ringCount];

        for (int i = 0; i < ringCount; i++)
        {
            var go = new GameObject("Ring");
            go.transform.SetParent(transform, false);
            var line = go.AddComponent<LineRenderer>();
            line.sharedMaterial = particleMaterial;
            line.useWorldSpace = true;
            line.loop = true;
            line.positionCount = 24;
            line.sortingOrder = 12;
            line.enabled = false;
            rings[i] = new Ring { Line = line, Age = float.MaxValue };
        }

        stains = new Stain[maxStains];

        for (int i = 0; i < maxStains; i++)
        {
            SpriteRenderer renderer = CreateSprite("Stain", 2);
            stains[i] = new Stain { Renderer = renderer, Age = float.MaxValue };
        }

        limbs = new Limb[maxLimbs];

        for (int i = 0; i < maxLimbs; i++)
        {
            SpriteRenderer renderer = CreateSprite("Limb", 6);
            GameObject go = renderer.gameObject;
            go.layer = debrisLayer;
            var body = go.AddComponent<Rigidbody2D>();
            body.gravityScale = 3f;
            go.AddComponent<BoxCollider2D>();
            go.SetActive(false);
            limbs[i] = new Limb { Body = body, Renderer = renderer, Age = float.MaxValue };
        }

        if (flashOverlay != null)
            flashOverlay.enabled = false;

        Health.AnyHit += OnAnyHit;
    }

    private void OnDestroy()
    {
        Health.AnyHit -= OnAnyHit;

        if (Instance == this)
            Instance = null;

        GameTime.Reset();
    }

    private ParticleSystem CreateSystem(string systemName, int max, float gravity, bool stretch, int sortingOrder)
    {
        var go = new GameObject(systemName);
        go.transform.SetParent(transform, false);
        var system = go.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = system.main;
        main.playOnAwake = false;
        main.loop = true;
        main.maxParticles = max;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.gravityModifier = gravity;
        main.startSpeed = 0f;

        ParticleSystem.EmissionModule emission = system.emission;
        emission.enabled = false;
        ParticleSystem.ShapeModule shape = system.shape;
        shape.enabled = false;

        // Fondu et rétrécissement en fin de vie.
        ParticleSystem.ColorOverLifetimeModule color = system.colorOverLifetime;
        color.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        color.color = gradient;

        ParticleSystem.SizeOverLifetimeModule size = system.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        var renderer = go.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = particleMaterial;
        renderer.sortingOrder = sortingOrder;
        renderer.renderMode = stretch ? ParticleSystemRenderMode.Stretch : ParticleSystemRenderMode.Billboard;
        renderer.velocityScale = stretch ? 0.035f : 0f;
        renderer.lengthScale = stretch ? 1.5f : 1f;

        system.Play();
        return system;
    }

    private SpriteRenderer CreateSprite(string objectName, int sortingOrder)
    {
        var go = new GameObject(objectName);
        go.transform.SetParent(transform, false);
        var renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = square;
        renderer.sortingOrder = sortingOrder;
        renderer.enabled = false;
        return renderer;
    }

    // ------------------------------------------------------------------ Coups

    private void OnAnyHit(Health target, DamageInfo info)
    {
        AttackData attack = info.Attack;
        Vector2 point = info.Point;
        float direction = Mathf.Abs(info.Direction) > 0.01f ? Mathf.Sign(info.Direction) : 1f;
        float power = attack != null ? Mathf.InverseLerp(0f, 12f, attack.hitstop) : 0.3f;
        bool finisher = target.IsDead;

        if (CameraFollow.Main != null)
        {
            CameraFollow.Main.Shake(attack != null ? attack.shake : 0.15f);

            if ((attack != null && attack.zoom) || (finisher && power > 0.4f))
                CameraFollow.Main.Zoom(point);
        }

        if (attack != null && attack.impactFlash && GameOptions.ImpactFlashes)
            flashFramesLeft = 2;

        // Étincelles et onde de choc au point de contact, plus fortes sur les gros coups.
        Emit(sparks, point, Mathf.RoundToInt(Mathf.Lerp(4, 16, power)), direction, 4f, 11f, 70f, 0.08f, 0.16f, SparkColor, 0.12f, 0.25f);
        SpawnRing(point, Mathf.Lerp(0.6f, 1.8f, power), Mathf.Lerp(0.15f, 0.3f, power));

        if (attack != null && (attack.IsHeavy || attack.zoom))
            EmitSpeedLines(point);

        // Poussière sous la cible qui glisse.
        Collider2D targetCollider = target.GetComponent<Collider2D>();

        if (targetCollider != null && attack != null && attack.knockbackVelocity.x > 3f)
            Dust(new Vector2(targetCollider.bounds.center.x, targetCollider.bounds.min.y), Mathf.RoundToInt(Mathf.Lerp(3, 10, power)));

        HitReaction reaction = target.GetComponent<HitReaction>();
        float goreScale = finisher ? 2.5f : reaction != null && (reaction.IsKnockedDown || reaction.IsAirborne) ? 1.5f : 1f;

        switch (info.Type)
        {
            case DamageType.Blade:
                BladeGore(target, point, direction, goreScale, finisher);
                break;

            case DamageType.Bullet:
                Bleed(target, point, direction, 0.4f * goreScale, false);
                break;

            case DamageType.Explosion:
                Bleed(target, point, direction, 0.6f * goreScale, false);
                break;

            default:
                // Coups de poing et de pied : sueur et salive, pas de sang.
                Emit(droplets, point + Vector2.up * 0.3f, Mathf.RoundToInt(Mathf.Lerp(3, 9, power)), direction, 2f, 6f, 50f,
                    0.05f, 0.09f, SweatColor, 0.35f, 0.6f);
                break;
        }
    }

    private void BladeGore(Health target, Vector2 point, float direction, float scale, bool finisher)
    {
        Bleed(target, point, direction, scale, true);

        if (GameOptions.Gore != GoreLevel.Full)
            return;

        int hits = bladeHits.TryGetValue(target, out int count) ? count + 1 : 1;
        bladeHits[target] = hits;

        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null || !enemy.Archetype.canBeDismembered || hits < bladeHitsToDismember)
            return;

        int taken = limbsTaken.TryGetValue(target, out int limbCount) ? limbCount : 0;

        // Après 4 coups de lame : une chance par coup, garantie sur le coup final.
        if (taken < maxLimbsPerTarget && (finisher || Random.value < 0.35f))
        {
            limbsTaken[target] = taken + 1;
            SpawnLimb(target, point, direction);
        }
    }

    // Giclée orientée dans le sens du coup, gouttes qui tombent et tachent sol et murs.
    private void Bleed(Health target, Vector2 point, float direction, float scale, bool spray)
    {
        GoreLevel gore = GameOptions.Gore;

        if (gore == GoreLevel.Off)
        {
            Emit(sparks, point, 4, direction, 3f, 8f, 60f, 0.06f, 0.12f, SparkColor, 0.1f, 0.2f);
            return;
        }

        float amount = scale * (gore == GoreLevel.Reduced ? 0.35f : 1f);
        Color color = BloodColorOf(target);

        if (spray)
            Emit(blood, point, Mathf.RoundToInt(28 * amount), direction, 5f, 12f, 28f, 0.05f, 0.12f, color, 0.5f, 1.2f);

        Emit(blood, point, Mathf.RoundToInt(10 * amount), direction, 1f, 4f, 80f, 0.06f, 0.14f, color, 0.6f, 1.4f);
    }

    private static Color BloodColorOf(Health target)
    {
        Enemy enemy = target.GetComponent<Enemy>();

        if (enemy == null || enemy.Archetype == null)
            return HumanBlood;

        Color color = enemy.Archetype.bloodColor;
        return enemy.Archetype.bloodGlows ? color * 1.6f : color;
    }

    // ---------------------------------------------------------------- Émission

    // Émet count particules en cône autour de la direction (en degrés de demi-ouverture).
    private void Emit(ParticleSystem system, Vector2 point, int count, float direction, float minSpeed, float maxSpeed,
        float spreadDegrees, float minSize, float maxSize, Color color, float minLife, float maxLife)
    {
        if (count <= 0)
            return;

        var parameters = new ParticleSystem.EmitParams { applyShapeToPosition = false };

        for (int i = 0; i < count; i++)
        {
            float angle = Random.Range(-spreadDegrees, spreadDegrees) * Mathf.Deg2Rad + Random.Range(0f, 0.35f);
            var dir = new Vector2(Mathf.Cos(angle) * direction, Mathf.Sin(angle));
            parameters.position = point;
            parameters.velocity = dir * Random.Range(minSpeed, maxSpeed);
            parameters.startSize = Random.Range(minSize, maxSize);
            parameters.startLifetime = Random.Range(minLife, maxLife);
            parameters.startColor = color;
            system.Emit(parameters, 1);
        }
    }

    public void Dust(Vector2 point, int count)
    {
        var parameters = new ParticleSystem.EmitParams { applyShapeToPosition = false };

        for (int i = 0; i < count; i++)
        {
            parameters.position = point + new Vector2(Random.Range(-0.3f, 0.3f), 0.05f);
            parameters.velocity = new Vector2(Random.Range(-1.5f, 1.5f), Random.Range(0.3f, 1.5f));
            parameters.startSize = Random.Range(0.15f, 0.35f);
            parameters.startLifetime = Random.Range(0.3f, 0.6f);
            parameters.startColor = DustColor;
            dust.Emit(parameters, 1);
        }
    }

    // Rebond au mur ou au sol : poussière, onde, petit tremblement.
    public void Bounce(Vector2 point)
    {
        Dust(point, 10);
        SpawnRing(point, 1.4f, 0.25f);

        if (CameraFollow.Main != null)
            CameraFollow.Main.Shake(0.35f);
    }

    public void Explosion(Vector2 point, float radius)
    {
        SpawnRing(point, radius, 0.35f);
        Dust(point, 18);
        Emit(sparks, point, 24, 1f, 4f, 12f, 180f, 0.08f, 0.2f, SparkColor, 0.15f, 0.35f);

        if (CameraFollow.Main != null)
            CameraFollow.Main.Shake(0.5f);
    }

    private void EmitSpeedLines(Vector2 point)
    {
        var parameters = new ParticleSystem.EmitParams { applyShapeToPosition = false };

        for (int i = 0; i < 14; i++)
        {
            float angle = i / 14f * Mathf.PI * 2f + Random.Range(-0.1f, 0.1f);
            var dir = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            parameters.position = point + dir * Random.Range(0.6f, 1f);
            parameters.velocity = dir * Random.Range(18f, 26f);
            parameters.startSize = 0.05f;
            parameters.startLifetime = 0.12f;
            parameters.startColor = Color.white;
            speedLines.Emit(parameters, 1);
        }
    }

    private void SpawnRing(Vector2 center, float radius, float duration)
    {
        Ring oldest = rings[0];

        foreach (Ring ring in rings)
        {
            if (ring.Age > oldest.Age)
                oldest = ring;
        }

        oldest.Center = center;
        oldest.Radius = radius;
        oldest.Duration = duration;
        oldest.Age = 0f;
        oldest.Line.enabled = true;
    }

    // -------------------------------------------------------- Taches et membres

    // Appelé par BloodCollisionRelay quand des gouttes touchent le sol ou un mur.
    public void OnBloodCollision(GameObject other)
    {
        int count = blood.GetCollisionEvents(other, collisionEvents);
        int limit = GameOptions.Gore == GoreLevel.Reduced ? maxStainsReduced : maxStains;

        for (int i = 0; i < count && stainsThisFrame < maxStainsPerFrame; i++)
        {
            stainsThisFrame++;
            Vector3 normal = collisionEvents[i].normal;
            bool wall = Mathf.Abs(normal.x) > 0.7f;
            SpawnStain(collisionEvents[i].intersection, wall, limit);
        }
    }

    private void SpawnStain(Vector2 point, bool wall, int limit)
    {
        Stain stain = stains[nextStain % limit];
        nextStain = (nextStain + 1) % limit;

        float size = Random.Range(0.12f, 0.3f);
        Transform t = stain.Renderer.transform;
        t.position = new Vector3(point.x, point.y, 0f);
        t.localScale = wall ? new Vector3(0.07f, size * 1.4f, 1f) : new Vector3(size * 1.6f, 0.06f, 1f);
        stain.Color = new Color(0.4f, 0.01f, 0.02f, 0.9f);
        stain.Renderer.color = stain.Color;
        stain.Renderer.enabled = true;
        stain.Age = 0f;
        stain.Lifetime = GameOptions.Gore == GoreLevel.Reduced ? stainLifetime * 0.5f : stainLifetime;
    }

    private void SpawnLimb(Health target, Vector2 point, float direction)
    {
        Limb limb = limbs[nextLimb];
        nextLimb = (nextLimb + 1) % limbs.Length;

        // Couleur de la fiche : le corps est peut-être en plein flash blanc d'impact.
        Enemy enemy = target.GetComponent<Enemy>();
        SpriteRenderer body = target.GetComponentInChildren<SpriteRenderer>();
        limb.Color = enemy != null && enemy.Archetype != null ? enemy.Archetype.LimbColor
            : body != null ? body.color : Color.gray;
        limb.BloodColor = BloodColorOf(target);
        limb.Renderer.color = limb.Color;
        limb.Renderer.enabled = true;
        limb.Renderer.transform.localScale = new Vector3(0.22f, 0.6f, 1f);
        limb.Body.gameObject.SetActive(true);
        limb.Body.position = point + Vector2.up * 0.2f;
        limb.Body.rotation = Random.Range(0f, 360f);
        limb.Body.linearVelocity = new Vector2(direction * Random.Range(4f, 7f), Random.Range(6f, 9f));
        limb.Body.angularVelocity = Random.Range(-720f, 720f);
        limb.Age = 0f;

        Emit(blood, point, 40, direction, 3f, 9f, 60f, 0.06f, 0.14f, limb.BloodColor, 0.6f, 1.3f);
    }

    // --------------------------------------------------------------- Images

    private void Update()
    {
        GameTime.Tick();
        stainsThisFrame = 0;
        float dt = Time.deltaTime;

        UpdateFlash();

        foreach (Ring ring in rings)
        {
            if (!ring.Line.enabled)
                continue;

            ring.Age += dt;
            float t = ring.Age / ring.Duration;

            if (t >= 1f)
            {
                ring.Line.enabled = false;
                continue;
            }

            float radius = Mathf.Lerp(0.15f, ring.Radius, 1f - (1f - t) * (1f - t));
            ring.Line.startWidth = ring.Line.endWidth = Mathf.Lerp(0.14f, 0.02f, t);
            Color color = new(1f, 1f, 1f, 1f - t);
            ring.Line.startColor = ring.Line.endColor = color;

            for (int i = 0; i < ring.Line.positionCount; i++)
            {
                float angle = i * Mathf.PI * 2f / ring.Line.positionCount;
                ring.Line.SetPosition(i, ring.Center + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * radius);
            }
        }

        foreach (Stain stain in stains)
        {
            if (!stain.Renderer.enabled)
                continue;

            stain.Age += dt;
            float fade = Mathf.InverseLerp(stain.Lifetime + stainFade, stain.Lifetime, stain.Age);

            if (fade <= 0f)
            {
                stain.Renderer.enabled = false;
                continue;
            }

            Color color = stain.Color;
            color.a *= fade;
            stain.Renderer.color = color;
        }

        foreach (Limb limb in limbs)
        {
            if (!limb.Body.gameObject.activeSelf)
                continue;

            limb.Age += dt;

            // Le membre saigne en volant, puis s'estompe.
            if (limb.Age < 1.2f && Time.frameCount % 3 == 0)
                Emit(blood, limb.Body.position, 1, -Mathf.Sign(limb.Body.linearVelocity.x), 0.2f, 1f, 30f, 0.05f, 0.1f, limb.BloodColor, 0.5f, 1f);

            float fade = Mathf.InverseLerp(limbLifetime, limbLifetime - 1f, limb.Age);
            Color color = limb.Color;
            color.a = fade;
            limb.Renderer.color = color;

            if (fade <= 0f)
                limb.Body.gameObject.SetActive(false);
        }
    }

    // Image d'impact : une image blanche puis une image sombre (contraste inversé), le temps de 2 images.
    private void UpdateFlash()
    {
        if (flashOverlay == null)
            return;

        if (flashFramesLeft <= 0)
        {
            flashOverlay.enabled = false;
            return;
        }

        flashOverlay.enabled = true;
        flashOverlay.color = flashFramesLeft == 2 ? new Color(1f, 1f, 1f, 0.8f) : new Color(0f, 0f, 0f, 0.55f);
        flashFramesLeft--;
    }

    // Coup qui achève le dernier ennemi d'une vague.
    public void FinalBlow()
    {
        GameTime.SlowMotion(slowMotionDuration, slowMotionScale);
    }

    // Une vague efface les taches et membres de la précédente.
    public void ClearGore()
    {
        foreach (Stain stain in stains)
            stain.Renderer.enabled = false;

        foreach (Limb limb in limbs)
            limb.Body.gameObject.SetActive(false);

        bladeHits.Clear();
        limbsTaken.Clear();
        blood.Clear();
    }
}
