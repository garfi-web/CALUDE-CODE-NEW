using UnityEngine;

// Réaction aux coups dans l'arène 2,5D, sur les mêmes AttackData que l'arène latérale : knockbackVelocity.x pousse
// dans le plan du sol (dans le sens du coup), knockbackVelocity.y devient une vitesse de HAUTEUR (projection, smash).
// Lanceur : la cible monte et peut être jonglée. Smash (GroundBounce) : plaquée au sol, elle rebondit une fois.
// Coup lourd (WallBounce) : elle rebondit sur le bord de la zone de combat. Une projection finit au sol (mise au sol),
// pendant laquelle le corps est intouchable, comme dans les beat'em up du genre.
[RequireComponent(typeof(Body25D), typeof(Health), typeof(HitStop))]
public class HitReaction25D : MonoBehaviour
{
    [Tooltip("Multiplicateur de recul : 1 léger, 0,5 moyen, 0 lourd (EnemyArchetype.KnockbackScale).")]
    [SerializeField] private float knockbackScale = 1f;
    [SerializeField] private float hitstunScale = 1f;
    [SerializeField] private float groundFriction = 26f;
    [SerializeField] private int knockdownFrames = 40;

    [Header("Rebonds")]
    [SerializeField] private float wallBounceSpeed = 6f;
    [SerializeField] private float wallBounceLift = 8f;
    [SerializeField] private float groundBounceLift = 10f;
    [SerializeField] private float minBounceSpeed = 6f;
    [Tooltip("Coup léger sur une cible en l'air : elle remonte un peu (jonglage).")]
    [SerializeField] private float juggleLift = 6f;

    [Header("Visuel")]
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private int whiteFlashFrames = 3;

    private Body25D body25;
    private Rigidbody2D rb;
    private Health health;
    private HitStop hitStop;
    private int hitstunLeft;
    private int knockdownLeft;
    private int flashLeft;
    private bool airborne;
    private bool knockedDown;
    private KnockbackType armedBounce;
    private bool bounceArmed;

    public bool IsInHitstun => hitstunLeft > 0 || airborne || knockedDown;
    public bool IsAirborne => airborne;
    public bool IsKnockedDown => knockedDown;

    public float KnockbackScale
    {
        get => knockbackScale;
        set => knockbackScale = value;
    }

    public float HitstunScale
    {
        get => hitstunScale;
        set => hitstunScale = value;
    }

    private void Awake()
    {
        body25 = GetComponent<Body25D>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        hitStop = GetComponent<HitStop>();
        health.Hit += OnHit;
        body25.Landed += OnLanded;
        body25.HitWall += OnHitWall;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Hit -= OnHit;

        if (body25 != null)
        {
            body25.Landed -= OnLanded;
            body25.HitWall -= OnHitWall;
        }
    }

    public void ResetState()
    {
        hitstunLeft = 0;
        knockdownLeft = 0;
        flashLeft = 0;
        airborne = false;
        knockedDown = false;
        bounceArmed = false;
        SetFlash(0f);
    }

    private void OnHit(DamageInfo info)
    {
        flashLeft = whiteFlashFrames;
        SetFlash(1f);

        AttackData attack = info.Attack;

        if (attack == null)
            return;

        float scale = health.IsDead ? Mathf.Max(knockbackScale, 0.6f) : knockbackScale;
        float direction = Mathf.Abs(info.Direction) > 0.01f ? Mathf.Sign(info.Direction) : 1f;

        if (hitstunScale > 0f || health.IsDead)
            hitstunLeft = Mathf.Max(hitstunLeft, Mathf.RoundToInt(attack.hitstun * Mathf.Max(hitstunScale, 1f)));

        if (scale <= 0f)
        {
            hitStop.Freeze(attack.hitstop);
            return;
        }

        var planar = new Vector2(attack.knockbackVelocity.x * direction * scale, 0f);
        float lift = attack.knockbackVelocity.y * scale;
        bool wasAirborne = airborne || body25.Airborne;

        if (attack.knockback == KnockbackType.GroundBounce)
        {
            // Smash : en l'air, plaqué vers le sol puis rebond ; au sol, rebond immédiat.
            airborne = true;
            knockedDown = false;
            bounceArmed = wasAirborne;
            armedBounce = KnockbackType.GroundBounce;
            body25.HeightVelocity = wasAirborne ? Mathf.Min(lift, -10f) : groundBounceLift;
        }
        else if (attack.knockback is KnockbackType.Launch or KnockbackType.WallBounce || lift > 4f || wasAirborne || health.IsDead)
        {
            airborne = true;
            knockedDown = false;
            bounceArmed = attack.knockback == KnockbackType.WallBounce;
            armedBounce = attack.knockback;
            float minimum = wasAirborne ? juggleLift : health.IsDead ? 5f : 0f;
            body25.HeightVelocity = Mathf.Max(lift, minimum);
        }

        // Le recul part à la fin de l'arrêt sur image (HitStop rend la vitesse).
        hitStop.Freeze(attack.hitstop, planar);
    }

    private void OnLanded(float impact)
    {
        if (bounceArmed && armedBounce == KnockbackType.GroundBounce && impact >= minBounceSpeed)
        {
            bounceArmed = false;
            body25.HeightVelocity = groundBounceLift;
            hitstunLeft = Mathf.Max(hitstunLeft, 30);
            Bounce(body25.Ground);
            return;
        }

        if (!airborne)
            return;

        airborne = false;
        bounceArmed = false;
        knockedDown = true;
        knockdownLeft = knockdownFrames;

        if (CombatVfx.Instance != null)
            CombatVfx.Instance.Dust(body25.Ground, 8);
    }

    private void OnHitWall(int side, float speed)
    {
        if (!bounceArmed || armedBounce != KnockbackType.WallBounce || speed < minBounceSpeed)
            return;

        bounceArmed = false;
        airborne = true;
        rb.linearVelocity = new Vector2(-side * wallBounceSpeed, 0f);
        body25.HeightVelocity = Mathf.Max(body25.HeightVelocity, wallBounceLift);
        hitstunLeft = Mathf.Max(hitstunLeft, 30);
        Bounce(new Vector2(body25.Ground.x + side * 0.4f, body25.Depth + body25.Height + 1f));
    }

    private static void Bounce(Vector2 point)
    {
        if (CombatVfx.Instance != null)
            CombatVfx.Instance.Bounce(point);

        if (CameraFollow.Main != null)
            CameraFollow.Main.Shake(0.3f);
    }

    private void FixedUpdate()
    {
        if (hitStop.IsFrozen)
            return;

        if (flashLeft > 0 && --flashLeft == 0)
            SetFlash(0f);

        if (hitstunLeft > 0)
            hitstunLeft--;

        if (knockedDown)
        {
            // Au sol : intouchable jusqu'au relevé.
            health.GrantInvulnerability(2f / AttackData.FrameRate);

            if (--knockdownLeft <= 0 && !health.IsDead)
                knockedDown = false;
        }

        // Glissade freinée au sol pendant l'étourdissement, et pour les corps.
        if (!body25.Airborne && (hitstunLeft > 0 || knockedDown || health.IsDead))
        {
            Vector2 v = rb.linearVelocity;
            rb.linearVelocity = Vector2.MoveTowards(v, Vector2.zero, groundFriction * Time.fixedDeltaTime);

            if (v.sqrMagnitude > 6f && Time.frameCount % 4 == 0 && CombatVfx.Instance != null)
                CombatVfx.Instance.Dust(body25.Ground, 1);
        }
    }

    private void SetFlash(float amount)
    {
        if (body == null)
            return;

        if (SpriteFlash.Supports(body))
            SpriteFlash.Set(body, amount, Color.white);
    }
}
