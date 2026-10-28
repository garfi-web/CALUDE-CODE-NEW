using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Socle commun des ennemis : applique l'archétype, gère le déplacement, le cycle d'attaque
// (armement annoncé, actif, récupération) en images à 60 FPS, l'interruption, la super-armure,
// les jetons d'attaque et l'abri. Les coups reçus (recul, étourdissement, jonglage) passent par
// HitReaction, l'arrêt sur image par HitStop. Le comportement (EnemyBehaviour) décide quoi faire
// quand l'ennemi est libre.
[RequireComponent(typeof(Rigidbody2D), typeof(Health), typeof(EnemyTelegraph))]
[RequireComponent(typeof(HitStop), typeof(HitReaction))]
public class Enemy : MonoBehaviour, IDamageFilter
{
    public enum Phase { None, Windup, Active, Recovery }

    private const float WallNormal = 0.7f;

    // Ennemis vivants de la scène (ordres du Dealer, alerte de groupe).
    public static readonly List<Enemy> Living = new();

    private static readonly WaitForFixedUpdate FixedStep = new();

    [SerializeField] private EnemyArchetype archetype;
    [SerializeField] private Transform target;

    [Header("Parties du prefab")]
    [SerializeField] private Transform visual;
    [SerializeField] private Transform eye;
    [SerializeField] private EnemyTelegraph telegraph;
    [SerializeField] private Projectile projectilePrefab;
    [SerializeField] private Grenade grenadePrefab;
    [Tooltip("Layers que le corps traverse une fois mort (Nathan, ennemis).")]
    [SerializeField] private LayerMask corpseIgnoredLayers;

    [Header("Réglages communs")]
    [SerializeField] private float coverRangedMultiplier = 0.25f;
    [SerializeField] private float hopSpeed = 12f;
    [SerializeField] private float maxHopHeight = 2f;
    [SerializeField] private int feintRecoveryFrames = 12;
    [SerializeField] private Color stunnedColor = new(0.45f, 0.5f, 0.75f);

    private readonly Dictionary<EnemyAttack, float> readyTimes = new();
    private readonly Dictionary<EnemyAttack, int> useCounts = new();
    private readonly RaycastHit2D[] castHits = new RaycastHit2D[8];
    private Rigidbody2D rb;
    private Collider2D bodyCollider;
    private Health health;
    private HitStop hitStop;
    private HitReaction reaction;
    private Health targetHealth;
    private Collider2D targetCollider;
    private EnemyBehaviour behaviour;
    private Coroutine actionRoutine;
    private EnemyAttack currentAttack;
    private float windupMultiplier = 1f;
    private float buffUntil;
    private bool chargeHitWall;

    public EnemyArchetype Archetype => archetype;
    public Health Health => health;
    public Transform Target => target;
    public bool IsAlerted { get; private set; }
    public bool IsBusy => actionRoutine != null;
    public bool IsStunned => reaction.IsInHitstun;
    public bool IsFrozen => hitStop.IsFrozen;
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

    // Lecture pour l'affichage de debug (F1).
    public Phase CurrentPhase { get; private set; }
    public EnemyAttack CurrentAttack => currentAttack;
    public int PhaseFrame { get; private set; }
    public int PhaseLength { get; private set; }

    // Position est le centre du corps ; muzzleOffset se compte depuis les pieds.
    private Vector2 Muzzle => archetype != null && archetype.muzzleOffset != Vector2.zero
        ? Position + new Vector2(Facing * archetype.muzzleOffset.x, archetype.muzzleOffset.y - BodySize.y * 0.5f)
        : Position + new Vector2(Facing * BodySize.x * 0.5f, BodySize.y * 0.15f);
    private Vector2 BodySize => archetype != null ? archetype.bodySize : Vector2.one;

    public event Action<EnemyAttack> AttackStarted;
    // Immobilisation volontaire (rechargement du Flic) : l'Animator peut la jouer (état Hold).
    public event Action HoldStarted;
    public event Action<Enemy, EnemyArchetype> ReinforcementRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Living.Clear();

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        bodyCollider = GetComponent<Collider2D>();
        health = GetComponent<Health>();
        hitStop = GetComponent<HitStop>();
        reaction = GetComponent<HitReaction>();

        if (telegraph == null)
            telegraph = GetComponent<EnemyTelegraph>();

        health.Hit += OnHit;
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
            health.Hit -= OnHit;
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
        targetCollider = target != null ? target.GetComponent<Collider2D>() : null;
    }

    private void Apply()
    {
        health.SetMaxHealth(archetype.maxHealth);

        // La masse règle la poussée entre les corps ; le recul des coups passe par HitReaction.
        rb.mass = archetype.weight switch
        {
            EnemyWeight.Light => 1f,
            EnemyWeight.Medium => 2f,
            _ => 1000f,
        };

        reaction.KnockbackScale = archetype.KnockbackScale;
        reaction.HitstunScale = archetype.hitstunScale;

        Vector2 size = archetype.bodySize;

        if (bodyCollider is CapsuleCollider2D capsule)
            capsule.size = size;

        // Sprites animés, ou image fixe seule (mannequin d'entraînement).
        if (archetype.animator != null || archetype.sprite != null)
            ApplySprites(size);
        else
            ApplyPlaceholder(size);

        readyTimes.Clear();
        useCounts.Clear();
        gameObject.name = archetype.displayName;

        if (TryGetComponent(out GibSource gibSource))
            gibSource.Configure(archetype.gibs, archetype.gibbable && archetype.gibs != null, size.y);
        AddBehaviour();
    }

    // Rectangle provisoire de la couleur de l'archétype, avec un œil.
    private void ApplyPlaceholder(Vector2 size)
    {
        var visualScale = new Vector3(size.x, size.y, 1f);
        reaction.SetVisual(visualScale, Vector3.zero, true);

        if (eye != null)
        {
            eye.gameObject.SetActive(true);
            eye.localPosition = new Vector3(size.x * 0.28f, size.y * 0.3f, 0f);
        }

        telegraph.SetBaseColor(archetype.bodyColor);
    }

    // Sprites animés : pivot aux pieds, au bas de la capsule ; l'Animator est piloté par CharacterAnimator.
    private void ApplySprites(Vector2 size)
    {
        if (visual == null)
            return;

        reaction.SetVisual(Vector3.one, new Vector3(0f, -size.y * 0.5f, 0f), false);

        if (eye != null)
            eye.gameObject.SetActive(false);

        var spriteRenderer = visual.GetComponent<SpriteRenderer>();

        if (spriteRenderer != null && archetype.sprite != null)
            spriteRenderer.sprite = archetype.sprite;

        if (!visual.TryGetComponent(out Animator animator))
            animator = visual.gameObject.AddComponent<Animator>();

        animator.runtimeAnimatorController = archetype.animator;

        if (TryGetComponent(out CharacterAnimator bridge))
            bridge.SetAnimator(animator);

        telegraph.SetBaseColor(archetype.bodyColor);
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
            EnemyBehaviourType.TrainingDummy => typeof(TrainingDummy),
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

        // Figé ou étourdi : le recul, la glissade et le jonglage s'appliquent seuls.
        if (IsFrozen || IsStunned)
            return;

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

        if (IsBusy)
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
        if (IsBusy)
            return;

        actionRoutine = StartCoroutine(HoldRoutine(Mathf.RoundToInt(duration * AttackData.FrameRate), color));
        HoldStarted?.Invoke();
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
        AttackStarted?.Invoke(attack);

        // L'ordre du Dealer raccourcit l'armement, jamais sous le minimum commun.
        int windup = Mathf.Max(EnemyAttack.MinWindupFrames, Mathf.RoundToInt(attack.WindupFrames * windupMultiplier));
        float direction = DirectionToTarget;
        Vector2 aimPoint = TargetPoint;
        telegraph.ShowWindup(attack.telegraphColor);
        BeginPhase(Phase.Windup, windup);

        if (attack.data != null)
            GameAudio.Play(attack.data.windupSound);

        // Le fusil à pompe ne tire jamais sans son annonce : le cône est affiché pendant tout l'armement.
        bool cone = attack.kind == EnemyAttackKind.Shotgun && attack.data != null && attack.data.IsCone;

        while (PhaseFrame < windup)
        {
            yield return FixedStep;

            if (IsFrozen)
                continue;

            PhaseFrame++;

            if (!IsStunned)
                Stop();

            // La charge et le fusil cessent de suivre la cible après trackingFrames : la visée se fige,
            // le joueur peut sortir du cône ou esquiver le tir.
            bool tracks = attack.kind is not (EnemyAttackKind.Charge or EnemyAttackKind.Shotgun) || PhaseFrame <= attack.trackingFrames;

            if (tracks)
            {
                FaceTarget();
                direction = DirectionToTarget;
                aimPoint = TargetPoint;
            }

            if (attack.aimLine)
                telegraph.ShowAim(Muzzle, aimPoint, attack.telegraphColor, attack.aimLineWidth);

            if (cone)
                telegraph.ShowCone(Muzzle, aimPoint - Muzzle, attack.data.coneAngle, attack.data.closeRange, attack.data.maxRange,
                    attack.telegraphColor, !tracks);
        }

        telegraph.HideWindup();
        telegraph.HideAim();

        if (attack.feint)
        {
            BeginPhase(Phase.Recovery, feintRecoveryFrames);
            yield return Frames(feintRecoveryFrames);
            EndAttack(attack);
            yield break;
        }

        BeginPhase(Phase.Active, attack.ActiveFrames);
        useCounts[attack] = useCounts.TryGetValue(attack, out int uses) ? uses + 1 : 1;
        int recovery = attack.RecoveryFrames;

        if (attack.data != null)
            GameAudio.Play(attack.data.activeSound);

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
                    recovery = attack.wallRecoveryFrames;
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

            case EnemyAttackKind.Shotgun:
                if (targetCollider != null)
                    Shotgun.Fire(gameObject, Muzzle, aimPoint - Muzzle, attack.data, 1 << targetCollider.gameObject.layer);

                yield return Frames(Mathf.Max(0, attack.ActiveFrames - 1));
                break;
        }

        // Le jeton est rendu dès la récupération : un autre ennemi peut enchaîner.
        if (attack.data != null)
            GameAudio.Play(attack.data.recoverySound);

        BeginPhase(Phase.Recovery, recovery);
        AttackTokens.Release(this);
        yield return Frames(recovery);
        EndAttack(attack);
    }

    private IEnumerator HoldRoutine(int frames, Color color)
    {
        BeginPhase(Phase.Recovery, frames);
        telegraph.SetState(color);
        yield return Frames(frames);
        EndAttack(null);
    }

    // Un seul coup par attaque, pendant toutes les images actives.
    private IEnumerator StrikeRoutine(EnemyAttack attack, Func<bool> inReach)
    {
        bool hit = false;

        while (PhaseFrame < attack.ActiveFrames)
        {
            if (!IsFrozen)
            {
                if (!hit && inReach())
                    hit = HitTarget(attack, DamageKind.Melee);

                PhaseFrame++;
            }

            yield return FixedStep;
        }
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

                Projectile projectile = Pool.Get(projectilePrefab, muzzle);
                projectile.Launch(gameObject, direction, attack.projectileSpeed, attack.data);

                if (CombatVfx.Instance != null)
                    CombatVfx.Instance.MuzzleFlash(muzzle, Mathf.Sign(direction.x));
            }

            if (i < count - 1)
                yield return Frames(attack.shotIntervalFrames);
        }
    }

    private void ThrowGrenade(EnemyAttack attack, Vector2 aimPoint)
    {
        if (grenadePrefab == null)
            return;

        // Lancée par-dessus la tête : elle ne part pas dans l'abri derrière lequel il se poste.
        float groundY = targetCollider != null ? targetCollider.bounds.min.y : aimPoint.y;
        Vector2 hand = Position + new Vector2(0f, BodySize.y * 0.65f);
        Grenade grenade = Pool.Get(grenadePrefab, hand);
        grenade.Launch(gameObject, new Vector2(aimPoint.x, groundY), attack.data, attack.areaRadius, attack.fuse);
    }

    private IEnumerator ChargeRoutine(EnemyAttack attack, float direction)
    {
        chargeHitWall = false;
        Face(direction);
        float reach = BodySize.x * 0.5f + 0.5f;

        while (PhaseFrame < attack.ActiveFrames)
        {
            yield return FixedStep;

            if (IsFrozen)
                continue;

            PhaseFrame++;

            if (IsBlocked(direction, 0.05f + attack.chargeSpeed * Time.fixedDeltaTime))
            {
                chargeHitWall = true;
                break;
            }

            rb.linearVelocity = new Vector2(direction * attack.chargeSpeed, rb.linearVelocity.y);

            // La charge s'arrête sur Nathan ; s'il esquive à travers, elle continue jusqu'au mur.
            if (TargetInReach(reach) && HitTarget(attack, DamageKind.Melee))
                break;
        }

        Stop();
    }

    // Attente en images (le hitstop ne les fait pas avancer), sur place sauf pendant un recul.
    private IEnumerator Frames(int count)
    {
        for (int frame = 0; frame < count;)
        {
            yield return FixedStep;

            if (IsFrozen)
                continue;

            if (!IsStunned)
                Stop();

            frame++;
            PhaseFrame++;
        }
    }

    private void BeginPhase(Phase phase, int length)
    {
        CurrentPhase = phase;
        PhaseFrame = 0;
        PhaseLength = length;
    }

    private void EndAttack(EnemyAttack attack)
    {
        BeginPhase(Phase.None, 0);
        currentAttack = null;
        actionRoutine = null;
        AttackTokens.Release(this);
        telegraph.HideWindup();
        telegraph.HideAim();
        telegraph.SetState(null);

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
        if (targetHealth == null)
            return false;

        Vector2 point = targetCollider != null ? targetCollider.ClosestPoint(Position) : TargetPoint;
        return Combat.Strike(gameObject, targetHealth, attack.data, point, Mathf.Sign(TargetPoint.x - Position.x), kind);
    }

    // ------------------------------------------------------- Coups reçus

    private void OnHit(DamageInfo info)
    {
        Alert();

        // Super-armure : l'armement continue.
        if (CurrentPhase == Phase.Windup && archetype.superArmor)
            return;

        // Une charge lancée ne s'arrête pas sur un coup.
        if (CurrentPhase == Phase.Active && currentAttack != null && currentAttack.kind == EnemyAttackKind.Charge)
            return;

        // Un coup pendant l'armement l'interrompt ; un coup qui étourdit coupe aussi la récupération.
        if (CurrentPhase == Phase.Windup || (IsBusy && archetype.hitstunScale > 0f))
            Interrupt();
    }

    private void OnDied()
    {
        StopAllCoroutines();
        EndAttack(null);
        LeaveCover();

        // Le corps est projeté par le dernier coup, puis reste au sol sans bloquer personne.
        rb.excludeLayers = corpseIgnoredLayers;
        telegraph.SetDead();

        if (behaviour != null)
            behaviour.enabled = false;

        enabled = false;
    }
}
