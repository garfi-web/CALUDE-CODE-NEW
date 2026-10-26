using System;
using UnityEngine;

// Réaction d'un combattant aux coups : arrêt sur image, recul selon le type (glissade, projection,
// rebond au mur ou au sol), étourdissement, jonglage, flash blanc et pose de douleur.
// Le gameplay lit IsInHitstun / IsAirborne ; l'aspect visuel ne touche que l'objet « visual ».
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(HitStop))]
public class HitReaction : MonoBehaviour
{
    private const float GroundNormal = 0.7f;

    [Tooltip("Multiplicateur de recul : 1 léger, 0,5 moyen, 0 lourd.")]
    [SerializeField] private float knockbackScale = 1f;
    [Tooltip("Multiplicateur d'étourdissement (0 = jamais étourdi).")]
    [SerializeField] private float hitstunScale = 1f;
    [SerializeField] private float groundFriction = 28f;
    [SerializeField] private int knockdownFrames = 36;

    [Header("Rebonds")]
    [SerializeField] private float wallBounceSpeed = 7f;
    [SerializeField] private float wallBounceLift = 9f;
    [SerializeField] private float groundBounceLift = 11f;
    [SerializeField] private float minBounceSpeed = 6f;

    [Header("Visuel")]
    [SerializeField] private Transform visual;
    [SerializeField] private SpriteRenderer body;
    [SerializeField] private int whiteFlashFrames = 3;
    [SerializeField] private int painPoseFrames = 10;
    [Tooltip("Personnage provisoire (rectangle) : couché par déformation. Un sprite animé a ses propres poses.")]
    [SerializeField] private bool placeholderPoses = true;

    private readonly ContactPoint2D[] contacts = new ContactPoint2D[8];
    private Rigidbody2D rb;
    private Health health;
    private HitStop hitStop;
    private EnemyTelegraph telegraph;
    private int hitstunLeft;
    private int knockdownLeft;
    private int flashLeft;
    private int poseLeft;
    private KnockbackType armedBounce;
    private bool bounceArmed;
    private bool airborne;
    private bool knockedDown;
    private Vector3 visualScale = Vector3.one;
    private Vector3 visualPosition;
    private Color flashRestoreColor;

    public bool IsInHitstun => hitstunLeft > 0 || airborne || knockedDown;
    public bool IsAirborne => airborne;
    public bool IsKnockedDown => knockedDown;
    public bool IsGrounded { get; private set; }
    public int HitstunFrames => hitstunLeft;

    // Type de rebond, position. Pour les effets (poussière, tremblement).
    public event Action<KnockbackType, Vector2> Bounced;
    public event Action<Vector2> Landed;

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
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        hitStop = GetComponent<HitStop>();
        telegraph = GetComponent<EnemyTelegraph>();
        health.Hit += OnHit;

        if (visual != null)
        {
            visualScale = visual.localScale;
            visualPosition = visual.localPosition;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Hit -= OnHit;
    }

    // La silhouette peut changer après l'apparition (archétype d'ennemi, sprite animé).
    public void SetVisual(Vector3 scale, Vector3 position, bool placeholder)
    {
        visualScale = scale;
        visualPosition = position;
        placeholderPoses = placeholder;

        if (visual != null)
            visual.localPosition = position;

        SetVisualScale(scale);
    }

    public void SetVisualScale(Vector3 scale)
    {
        visualScale = scale;

        if (visual != null && poseLeft <= 0 && !knockedDown)
            visual.localScale = scale;
    }

    private void OnHit(DamageInfo info)
    {
        AttackData attack = info.Attack;

        flashLeft = whiteFlashFrames;
        poseLeft = painPoseFrames;
        StartFlash();

        if (attack == null)
            return;

        // Pas de recul pour les lourds, sauf sur le coup mortel (le corps est projeté).
        float scale = health.IsDead ? Mathf.Max(knockbackScale, 0.6f) : knockbackScale;
        float direction = Mathf.Abs(info.Direction) > 0.01f ? Mathf.Sign(info.Direction) : 1f;
        Vector2 velocity = new(attack.knockbackVelocity.x * direction * scale, attack.knockbackVelocity.y * scale);

        if (hitstunScale > 0f || health.IsDead)
            hitstunLeft = Mathf.Max(hitstunLeft, Mathf.RoundToInt(attack.hitstun * Mathf.Max(hitstunScale, 1f)));

        if (scale <= 0f)
        {
            hitStop.Freeze(attack.hitstop);
            return;
        }

        // Projection en l'air ou cible déjà en l'air : jonglage jusqu'à l'atterrissage.
        bool launches = attack.knockback is KnockbackType.Launch or KnockbackType.WallBounce or KnockbackType.GroundBounce
                        || velocity.y > 4f || airborne;

        if (launches)
        {
            airborne = true;
            knockedDown = false;
        }

        bounceArmed = attack.knockback is KnockbackType.WallBounce or KnockbackType.GroundBounce;
        armedBounce = attack.knockback;
        hitStop.Freeze(attack.hitstop, velocity);
    }

    private void FixedUpdate()
    {
        IsGrounded = CheckGrounded();

        if (hitStop.IsFrozen)
            return;

        UpdateVisuals();

        if (hitstunLeft > 0)
            hitstunLeft--;

        if (airborne)
        {
            // Atterrissage d'une projection : au sol quelques images, puis relevé.
            if (IsGrounded && rb.linearVelocity.y <= 0.1f && !bounceArmed)
            {
                airborne = false;
                knockedDown = true;
                knockdownLeft = knockdownFrames;
                Landed?.Invoke(rb.position);

                if (CombatVfx.Instance != null)
                    CombatVfx.Instance.Dust(rb.position + Vector2.down * 0.7f, 8);
            }
        }
        else if (knockedDown)
        {
            if (--knockdownLeft <= 0 && !health.IsDead)
                knockedDown = false;
        }

        // Glissade freinée au sol pendant l'étourdissement, et pour les corps.
        if (IsGrounded && (hitstunLeft > 0 || knockedDown || health.IsDead))
        {
            Vector2 v = rb.linearVelocity;
            v.x = Mathf.MoveTowards(v.x, 0f, groundFriction * Time.fixedDeltaTime);
            rb.linearVelocity = v;

            // Nuage de poussière tant que le corps glisse.
            if (Mathf.Abs(v.x) > 2.5f && Time.frameCount % 4 == 0 && CombatVfx.Instance != null)
                CombatVfx.Instance.Dust(rb.position + Vector2.down * 0.7f, 1);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (!bounceArmed || collision.collider.GetComponentInParent<Health>() != null)
            return;

        Vector2 normal = collision.GetContact(0).normal;
        Vector2 velocity = -collision.relativeVelocity;

        if (armedBounce == KnockbackType.WallBounce && Mathf.Abs(normal.x) > GroundNormal &&
            Mathf.Abs(velocity.x) >= minBounceSpeed)
        {
            bounceArmed = false;
            rb.linearVelocity = new Vector2(Mathf.Sign(normal.x) * wallBounceSpeed, wallBounceLift);
            hitstunLeft = Mathf.Max(hitstunLeft, 30);
            Bounced?.Invoke(KnockbackType.WallBounce, collision.GetContact(0).point);
            NotifyBounce(collision.GetContact(0).point);
        }
        else if (armedBounce == KnockbackType.GroundBounce && normal.y > GroundNormal &&
                 Mathf.Abs(velocity.y) >= minBounceSpeed)
        {
            bounceArmed = false;
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * 0.5f, groundBounceLift);
            hitstunLeft = Mathf.Max(hitstunLeft, 30);
            Bounced?.Invoke(KnockbackType.GroundBounce, collision.GetContact(0).point);
            NotifyBounce(collision.GetContact(0).point);
        }
        else if (normal.y > GroundNormal && armedBounce == KnockbackType.WallBounce)
        {
            // Projeté fort mais retombé avant le mur : plus de rebond possible.
            bounceArmed = false;
        }
    }

    private static void NotifyBounce(Vector2 point)
    {
        if (CombatVfx.Instance != null)
            CombatVfx.Instance.Bounce(point);
    }

    private bool CheckGrounded()
    {
        int count = rb.GetContacts(contacts);

        for (int i = 0; i < count; i++)
        {
            if (contacts[i].normal.y > GroundNormal && contacts[i].collider.GetComponentInParent<Health>() == null)
                return true;
        }

        return false;
    }

    private void StartFlash()
    {
        if (telegraph != null)
        {
            telegraph.Flash(whiteFlashFrames);
            return;
        }

        if (body == null)
            return;

        if (SpriteFlash.Supports(body))
        {
            SpriteFlash.Set(body, 1f, Color.white);
            return;
        }

        if (body.color != Color.white)
            flashRestoreColor = body.color;

        body.color = Color.white;
    }

    private void UpdateVisuals()
    {
        if (flashLeft > 0 && --flashLeft == 0 && telegraph == null && body != null)
        {
            if (SpriteFlash.Supports(body))
                SpriteFlash.Set(body, 0f, Color.white);
            else
                body.color = flashRestoreColor;
        }

        if (visual == null)
            return;

        if (placeholderPoses && (knockedDown || (health.IsDead && IsGrounded)))
        {
            // Au sol : silhouette couchée.
            visual.localScale = new Vector3(visualScale.y, visualScale.x, 1f);
            visual.localPosition = visualPosition + new Vector3(0f, -(visualScale.y - visualScale.x) * 0.5f, 0f);
            return;
        }

        visual.localPosition = visualPosition;

        // Pose de douleur : écrasé et penché vers l'arrière, puis retour.
        if (poseLeft > 0)
        {
            poseLeft--;
            float t = poseLeft / (float)Mathf.Max(1, painPoseFrames);
            visual.localScale = new Vector3(visualScale.x * (1f - 0.15f * t), visualScale.y * (1f + 0.08f * t), 1f);
            visual.localRotation = Quaternion.Euler(0f, 0f, 10f * t);
        }
        else
        {
            visual.localScale = visualScale;
            visual.localRotation = Quaternion.identity;
        }
    }

    // Sécurité : une reprise de vague remet un combattant debout.
    public void ResetState()
    {
        hitstunLeft = 0;
        knockdownLeft = 0;
        airborne = false;
        knockedDown = false;
        bounceArmed = false;
        poseLeft = 0;

        if (visual != null)
        {
            visual.localScale = visualScale;
            visual.localPosition = visualPosition;
            visual.localRotation = Quaternion.identity;
        }
    }
}
