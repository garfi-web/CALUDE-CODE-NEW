using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Socle commun des ennemis : applique l'archétype, gère le déplacement, le cycle d'attaque
// (armement annoncé, actif, récupération), l'étourdissement, la super-armure, les jetons
// d'attaque et l'abri. Le comportement (EnemyBehaviour) décide quoi faire quand l'ennemi est libre.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(EnemyTelegraph))]
public class Enemy : MonoBehaviour, IDamageFilter
{
    private enum Phase { None, Windup, Active, Recovery }

    private const float WallNormal = 0.7f;
    private const float KnockbackLift = 2f;

    // Ennemis vivants de la scène (ordres du Dealer, alerte de groupe).
    public static readonly List<Enemy> Living = new();

    [SerializeField] private EnemyArchetype archetype;
    [SerializeField] private Transform target;

    [Header("Parties du prefab")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform eye;
    [SerializeField] private EnemyTelegraph telegraph;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Grenade grenadePrefab;

    [Header("Réglages communs")]
    [SerializeField] private float coverRangedMultiplier = 0.25f;
    [SerializeField] private float hopSpeed = 12f;
    [SerializeField] private float maxHopHeight = 2f;
    [SerializeField] private float feintRecovery = 0.2f;
    [SerializeField] private Color stunnedColor = new(0.45f, 0.5f, 0.75f);

    private readonly Dictionary<EnemyAttack, float> readyTimes = new();
    private readonly Dictionary<EnemyAttack, int> useCounts = new();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Health health;
    private Health targetHealth;
    private Rigidbody2D targetRb;
    private Collider2D targetCollider;
    private EnemyBehaviour behaviour;
    private Coroutine actionRoutine;
    private EnemyAttack currentAttack;
    private Phase phase;
    private float stunnedUntil;
    private float windupMultiplier = 1f;
    private float buffUntil;
    private bool chargeHitWall;

    public EnemyArchetype Archetype => archetype;
    public Health Health => health;
    public Transform Target => target;
    public bool IsAlerted { get; private set; }
    public bool IsBusy => actionRoutine != null;
    public bool IsStunned => Time.time < stunnedUntil;
    public bool IsDead => health != null && health.IsDead;
    public float Facing => transform.localScale.x >= 0f ? 1f : -1f;
    public Cover CurrentCover { get; private set; }
    public Vector2 Position => rb.position;
    public Vector2 TargetPoint => targetCollider != null ? (Vector2)targetCollider.bounds.center : (Vector2)target.position;
    public float DeltaToTarget => target == null ? 0f : TargetPoint.x - Position.x;
    public float DistanceToTarget => Mathf.Abs(DeltaToTarget);
    public float DirectionToTarget => Mathf.Sign(DeltaToTarget);
    public float HealthRatio => health.MaxHealth > 0 ? (float)health.CurrentHealth / health.MaxHealth : 0f;
    public bool TargetIsDead => targetHealth != null && targetHealth.IsDead;

    private Vector2 Muzzle => Position + new Vector2(Facing * BodySize.x * 0.5f, BodySize.y * 0.15f);
    private Vector2 BodySize => archetype != null ? archetype.bodySize : Vector2.one;

    public event Action<EnemyAttack> AttackStarted;
    public event Action<Enemy, EnemyArchetype> ReinforcementRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Living.Clear();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<Health>();

        if (telegraph == null)
            telegraph = GetComponent<EnemyTelegraph>();

        health.Damaged += OnDamaged;
        health.Died += OnDied;

        if (archetype != null)
            Apply();
    }

    private void OnEnable()
    {
        if (!IsDead && !Living.Contains(this))
            Living.Add(this);
    }

    private void OnDisable()
    {
        Living.Remove(this);
        AttackTokens.Release(this);
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                SetTarget(player.transform);
        }
        else
        {
            SetTarget(target);
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }

        LeaveCover();
    }

    // Appelé par le gestionnaire de vagues juste après l'instanciation du prefab.
    public void Initialize(EnemyArchetype enemyArchetype, Transform enemyTarget)
    {
        archetype = enemyArchetype;
        Apply();
        SetTarget(enemyTarget);
    }

    private void SetTarget(Transform newTarget)
    {
        target = newTarget;
        targetHealth = target != null ? target.GetComponent<Health>() : null;
        targetRb = target != null ? target.GetComponent<Rigidbody2D>() : null;
        targetCollider = target != null ? target.GetComponent<Collider2D>() : null;
    }

    private void Apply()
    {
        health.SetMaxHealth(archetype.maxHealth);

        // Le poids passe par la masse : le recul des coups de Nathan est une impulsion.
        rb.mass = archetype.weight switch
        {
            EnemyWeight.Light => 1f,
            EnemyWeight.Medium => 2f,
            _ => 1000f,
        };

        Vector2 size = archetype.bodySize;

        if (visual != null)
            visual.localScale = new Vector3(size.x, size.y, 1f);

        if (eye != null)
            eye.localPosition = new Vector3(size.x * 0.28f, size.y * 0.3f, 0f);

        if (bodyCollider is CapsuleCollider2D capsule)
            capsule.size = size;

        if (telegraph != null)
            telegraph.SetBaseColor(archetype.bodyColor);

        readyTimes.Clear();
        useCounts.Clear();
        gameObject.name = archetype.displayName;
        AddBehaviour();
    }

    private void AddBehaviour()
    {
        Type type = archetype.behaviour switch
        {
            EnemyBehaviourType.Commander => typeof(Commander),
            EnemyBehaviourType.CoverShooter => typeof(CoverShooter),
            EnemyBehaviourType.Tactical => typeof(Tactical),
            EnemyBehaviourType.HeavyCharger => typeof(HeavyCharger),
            EnemyBehaviourType.Ambusher => typeof(Ambusher),
            EnemyBehaviourType.Feinter => typeof(Feinter),
            EnemyBehaviourType.Trickster => typeof(Trickster),
            _ => typeof(MeleeAggressive),
        };

        if (behaviour != null && behaviour.GetType() != type)
            Destroy(behaviour);

        behaviour = (EnemyBehaviour)GetComponent(type);

        if (behaviour == null)
            behaviour = (EnemyBehaviour)gameObject.AddComponent(type);
    }

    private void FixedUpdate()
    {
        if (IsDead || archetype == null)
            return;

        if (windupMultiplier < 1f && Time.time >= buffUntil)
        {
            windupMultiplier = 1f;
            telegraph.SetBuffed(false);
        }

        if (target == null || TargetIsDead)
        {
            if (!IsBusy)
                Stop();

            return;
        }

        if (!IsAlerted)
        {
            if (Vector2.Distance(TargetPoint, Position) > archetype.detectionRange)
            {
                Stop();
                return;
            }

            Alert();
        }

        // Pendant l'étourdissement, on laisse le recul s'appliquer.
        if (IsStunned || IsBusy)
            return;

        if (behaviour != null && behaviour.enabled)
            behaviour.Tick();
    }

    // Un ennemi qui repère Nathan (ou qui est frappé) alerte ceux qui le voient.
    public void Alert()
    {
        if (IsAlerted)
            return;

        IsAlerted = true;

        foreach (Enemy other in Living.ToArray())
        {
            if (other != this && other.archetype != null &&
                Vector2.Distance(other.Position, Position) <= other.archetype.detectionRange)
                other.Alert();
        }
    }

    // ------------------------------------------------------------ Déplacement

    public void Move(float direction, float speed)
    {
        rb.linearVelocity = new Vector2(direction * speed, rb.linearVelocity.y);

        if (Mathf.Abs(direction) < 0.01f)
            return;

        Face(direction);
        TryHop(direction);
    }

    // Renvoie vrai une fois arrivé.
    public bool MoveTo(float x, float speed, float tolerance = 0.2f)
    {
        float dx = x - Position.x;

        if (Mathf.Abs(dx) <= tolerance)
        {
            Stop();
            return true;
        }

        Move(Mathf.Sign(dx), speed);
        return false;
    }

    public void Stop() => rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

    public void Face(float direction)
    {
        if (Mathf.Abs(direction) < 0.01f)
            return;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * Mathf.Sign(direction);
        transform.localScale = scale;
    }

    public void FaceTarget()
    {
        if (target != null)
            Face(DeltaToTarget);
    }

    // Mur ou abri dans cette direction, à moins de distance.
    public bool IsBlocked(float direction, float distance) =>
        CastBody(new Vector2(Mathf.Sign(direction), 0f), distance, IsWall, out _);

    private bool IsGrounded => CastBody(Vector2.down, 0.05f, hit => hit.normal.y > WallNormal, out _);

    // Saute par-dessus un obstacle bas (abri), jamais par-dessus un mur de l'arène.
    private void TryHop(float direction)
    {
        if (!IsGrounded || !CastBody(new Vector2(direction, 0f), 0.15f, IsWall, out RaycastHit2D hit))
            return;

        if (hit.collider.bounds.max.y - bodyCollider.bounds.min.y <= maxHopHeight)
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, hopSpeed);
    }

    private static bool IsWall(RaycastHit2D hit) => Mathf.Abs(hit.normal.x) > WallNormal;

    // Les personnages (Health) et les plateformes traversables ne comptent pas comme obstacles.
    private bool CastBody(Vector2 direction, float distance, Func<RaycastHit2D, bool> accept, out RaycastHit2D result)
    {
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = rb.Cast(direction, filter, castHits, distance);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = castHits[i].collider;

            if (other.GetComponentInParent<Health>() != null)
                continue;

            if (other.GetComponent<PlatformEffector2D>() != null && direction.y >= 0f)
                continue;

            if (accept(castHits[i]))
            {
                result = castHits[i];
                return true;
            }
        }

        result = default;
        return false;
    }

    // ------------------------------------------------------------------ Abri

    public void TakeCover(Cover cover)
    {
        if (CurrentCover == cover)
            return;

        LeaveCover();

        if (cover != null && cover.TryOccupy(this))
            CurrentCover = cover;
    }

    public void LeaveCover()
    {
        if (CurrentCover != null)
            CurrentCover.Release(this);

        CurrentCover = null;
    }

    // Derrière un abri, les tirs venus d'en face ne font que 25 % des dégâts.
    public DamageInfo Filter(DamageInfo damage)
    {
        if (damage.Kind != DamageKind.Ranged || CurrentCover == null || !CurrentCover.Shields(Position, damage.Origin))
            return damage;

        return damage.WithAmount(Mathf.CeilToInt(damage.Amount * coverRangedMultiplier));
    }

    // ------------------------------------------------------------- Attaques

    public bool CanUse(EnemyAttack attack)
    {
        if (attack == null || IsBusy || IsDead)
            return false;

        if (attack.maxUses > 0 && useCounts.TryGetValue(attack, out int uses) && uses >= attack.maxUses)
            return false;

        if (readyTimes.TryGetValue(attack, out float readyTime) && Time.time < readyTime)
            return false;

        return !NeedsToken(attack) || AttackTokens.IsAvailable(this, attack.IsRanged);
    }

    // Tirage pondéré parmi les attaques prêtes de ce type.
    public EnemyAttack Pick(EnemyAttackKind kind)
    {
        float total = 0f;

        foreach (EnemyAttack attack in archetype.attacks)
        {
            if (attack.kind == kind && CanUse(attack))
                total += Mathf.Max(0.01f, attack.weight);
        }

        if (total <= 0f)
            return null;

        float roll = UnityEngine.Random.value * total;

        foreach (EnemyAttack attack in archetype.attacks)
        {
            if (attack.kind != kind || !CanUse(attack))
                continue;

            roll -= Mathf.Max(0.01f, attack.weight);

            if (roll <= 0f)
                return attack;
        }

        return null;
    }

    // Première attaque de ce type, prête ou non (pour connaître sa portée).
    public EnemyAttack Find(EnemyAttackKind kind) =>
        Array.Find(archetype.attacks, attack => attack.kind == kind);

    public bool TryAttack(EnemyAttack attack)
    {
        if (!CanUse(attack))
            return false;

        if (NeedsToken(attack) && !AttackTokens.TryAcquire(this, attack.IsRanged))
            return false;

        actionRoutine = StartCoroutine(AttackRoutine(attack));
        return true;
    }

    // Immobile pendant un temps (rechargement...) : une fenêtre de vulnérabilité.
    public void Hold(float duration, Color color)
    {
        if (!IsBusy)
            actionRoutine = StartCoroutine(HoldRoutine(duration, color));
    }

    public void ApplyWindupBuff(float multiplier, float duration)
    {
        windupMultiplier = Mathf.Min(windupMultiplier, multiplier);
        buffUntil = Mathf.Max(buffUntil, Time.time + duration);
        telegraph.SetBuffed(true);
    }

    private static bool NeedsToken(EnemyAttack attack) =>
        attack.kind is not (EnemyAttackKind.Order or EnemyAttackKind.Summon);

    private IEnumerator AttackRoutine(EnemyAttack attack)
    {
        currentAttack = attack;
        phase = Phase.Windup;
        AttackStarted?.Invoke(attack);

        // L'ordre du Dealer raccourcit l'armement, jamais sous le minimum commun.
        float windup = Mathf.Max(EnemyAttack.MinWindup, attack.windup * windupMultiplier);
        float direction = DirectionToTarget;
        Vector2 aimPoint = TargetPoint;
        telegraph.ShowWindup(attack.telegraphColor);

        for (float t = 0f; t < windup; t += Time.fixedDeltaTime)
        {
            if (!IsStunned)
                Stop();

            // La charge cesse de suivre la cible après trackingTime.
            if (attack.kind != EnemyAttackKind.Charge || t < attack.trackingTime)
            {
                FaceTarget();
                direction = DirectionToTarget;
                aimPoint = TargetPoint;
            }

            if (attack.aimLine)
                telegraph.ShowAim(Muzzle, aimPoint, attack.telegraphColor, attack.aimLineWidth);

            yield return new WaitForFixedUpdate();
        }

        telegraph.HideWindup();
        telegraph.HideAim();

        if (attack.feint)
        {
            phase = Phase.Recovery;
            yield return Wait(feintRecovery);
            EndAttack(attack);
            yield break;
        }

        phase = Phase.Active;
        useCounts[attack] = useCounts.TryGetValue(attack, out int uses) ? uses + 1 : 1;
        float recovery = attack.recovery;

        switch (attack.kind)
        {
            case EnemyAttackKind.Melee:
                yield return StrikeRoutine(attack, () => TargetInReach(attack.range));
                break;

            case EnemyAttackKind.Shockwave:
                yield return StrikeRoutine(attack, () => TargetInShockwave(attack.range));
                break;

            case EnemyAttackKind.Projectile:
                yield return FireRoutine(attack, aimPoint);
                break;

            case EnemyAttackKind.Grenade:
                ThrowGrenade(attack, aimPoint);
                break;

            case EnemyAttackKind.Charge:
                yield return ChargeRoutine(attack, direction);

                if (chargeHitWall)
                {
                    recovery = attack.wallRecovery;
                    telegraph.SetState(stunnedColor);
                }
                break;

            case EnemyAttackKind.Order:
                foreach (Enemy ally in Living.ToArray())
                {
                    if (ally != this && !ally.IsDead)
                        ally.ApplyWindupBuff(attack.buffWindupMultiplier, attack.buffDuration);
                }
                break;

            case EnemyAttackKind.Summon:
                if (attack.summon != null)
                    ReinforcementRequested?.Invoke(this, attack.summon);
                break;
        }

        // Le jeton est rendu dès la récupération : un autre ennemi peut enchaîner.
        phase = Phase.Recovery;
        AttackTokens.Release(this);
        yield return Wait(recovery);
        EndAttack(attack);
    }

    private IEnumerator HoldRoutine(float duration, Color color)
    {
        phase = Phase.Recovery;
        telegraph.SetState(color);
        yield return Wait(duration);
        EndAttack(null);
    }

    // Un seul coup par attaque, pendant toute la durée de l'actif.
    private IEnumerator StrikeRoutine(EnemyAttack attack, Func<bool> inReach)
    {
        float end = Time.time + attack.active;
        bool hit = false;

        do
        {
            if (!hit && inReach())
                hit = HitTarget(attack, DamageKind.Melee);

            yield return new WaitForFixedUpdate();
        }
        while (Time.time < end);
    }

    private IEnumerator FireRoutine(EnemyAttack attack, Vector2 aimPoint)
    {
        int count = Mathf.Max(1, attack.projectileCount);

        for (int i = 0; i < count; i++)
        {
            if (projectilePrefab != null)
            {
                Vector2 muzzle = Muzzle;
                Vector2 direction = aimPoint - muzzle;
                direction = direction.sqrMagnitude > 0.001f ? direction.normalized : new Vector2(Facing, 0f);

                Projectile projectile = Instantiate(projectilePrefab, muzzle, Quaternion.identity);
                projectile.Launch(gameObject, direction, attack.projectileSpeed, attack.damage, attack.knockback);
            }

            if (i < count - 1)
                yield return Wait(attack.shotInterval);
        }
    }

    private void ThrowGrenade(EnemyAttack attack, Vector2 aimPoint)
    {
        if (grenadePrefab == null)
            return;

        // Lancée par-dessus la tête : elle ne part pas dans l'abri derrière lequel il se poste.
        float groundY = targetCollider != null ? targetCollider.bounds.min.y : aimPoint.y;
        Vector2 hand = Position + new Vector2(0f, BodySize.y * 0.65f);
        Grenade grenade = Instantiate(grenadePrefab, hand, Quaternion.identity);
        grenade.Launch(gameObject, new Vector2(aimPoint.x, groundY), attack.damage, attack.areaRadius, attack.fuse, attack.knockback);
    }

    private IEnumerator ChargeRoutine(EnemyAttack attack, float direction)
    {
        chargeHitWall = false;
        Face(direction);
        float reach = BodySize.x * 0.5f + 0.5f;
        float end = Time.time + attack.active;

        while (Time.time < end)
        {
            if (IsBlocked(direction, 0.05f + attack.chargeSpeed * Time.fixedDeltaTime))
            {
                chargeHitWall = true;
                break;
            }

            rb.linearVelocity = new Vector2(direction * attack.chargeSpeed, rb.linearVelocity.y);

            // La charge s'arrête sur Nathan ; s'il esquive à travers, elle continue jusqu'au mur.
            if (TargetInReach(reach) && HitTarget(attack, DamageKind.Melee))
                break;

            yield return new WaitForFixedUpdate();
        }

        Stop();
    }

    // Attente qui maintient l'ennemi sur place, sauf pendant le recul d'un coup reçu.
    private IEnumerator Wait(float duration)
    {
        float end = Time.time + duration;

        while (Time.time < end)
        {
            if (!IsStunned)
                Stop();

            yield return new WaitForFixedUpdate();
        }
    }

    private void EndAttack(EnemyAttack attack)
    {
        phase = Phase.None;
        currentAttack = null;
        actionRoutine = null;
        AttackTokens.Release(this);

        if (telegraph != null)
        {
            telegraph.HideWindup();
            telegraph.HideAim();
            telegraph.SetState(null);
        }

        if (attack != null)
            readyTimes[attack] = Time.time + attack.cooldown;
    }

    private void Interrupt()
    {
        if (actionRoutine != null)
            StopCoroutine(actionRoutine);

        EndAttack(currentAttack);
    }

    private bool TargetInReach(float range)
    {
        if (target == null)
            return false;

        Vector2 delta = TargetPoint - Position;
        return Mathf.Abs(delta.x) <= range + 0.3f && delta.x * Facing > -0.3f && Mathf.Abs(delta.y) < 1.5f;
    }

    // L'onde court au sol : un saut l'évite.
    private bool TargetInShockwave(float radius)
    {
        if (target == null || targetCollider == null)
            return false;

        return Mathf.Abs(DeltaToTarget) <= radius &&
               targetCollider.bounds.min.y - bodyCollider.bounds.min.y < 0.5f;
    }

    private bool HitTarget(EnemyAttack attack, DamageKind kind)
    {
        if (targetHealth == null || !targetHealth.TakeDamage(new DamageInfo(attack.damage, kind, Position)))
            return false;

        if (targetRb != null && !targetHealth.IsDead)
        {
            float direction = Mathf.Sign(TargetPoint.x - Position.x);
            targetRb.AddForce(new Vector2(direction * attack.knockback, KnockbackLift), ForceMode2D.Impulse);
        }

        return true;
    }

    // ------------------------------------------------------- Coups reçus

    private void OnDamaged(int damage)
    {
        Alert();

        // Super-armure : l'armement continue et l'ennemi n'est pas étourdi.
        if (phase == Phase.Windup && archetype.superArmor)
            return;

        // Une charge lancée ne s'arrête pas sur un coup.
        if (phase == Phase.Active && currentAttack != null && currentAttack.kind == EnemyAttackKind.Charge)
            return;

        // Un coup reçu pendant l'armement l'interrompt.
        if (phase == Phase.Windup)
            Interrupt();

        if (archetype.hitStunTime > 0f)
            stunnedUntil = Time.time + archetype.hitStunTime;
    }

    private void OnDied()
    {
        StopAllCoroutines();
        EndAttack(null);
        LeaveCover();

        // Le corps reste visible mais ne bloque plus personne.
        rb.linearVelocity = Vector2.zero;
        rb.bodyType = RigidbodyType2D.Kinematic;
        bodyCollider.enabled = false;

        telegraph.SetDead();

        if (behaviour != null)
            behaviour.enabled = false;

        enabled = false;
    }
}
