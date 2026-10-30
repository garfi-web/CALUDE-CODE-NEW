using System;
using System.Collections.Generic;
using UnityEngine;

// Ennemi de l'arène 2,5D. Les valeurs viennent de son EnemyArchetype (PV, vitesse, poids, super-armure, attaques et
// leurs AttackData) ; seules les attaques au corps à corps et le fusil à pompe sont jouées dans ce prototype.
// Règles communes reprises de Enemy : armement d'au moins 0,35 s (EnemyAttack.MinWindupFrames) annoncé par un voile
// de couleur, interruption par un coup sauf super-armure, jetons d'attaque partagés (AttackTokens : 2 au corps à
// corps, 1 tir à la fois). Comptes en images de jeu, gelés pendant le hitstop.
//
// Intelligence collective : les ennemis sans jeton tournent autour de Nathan sur une ellipse (places réparties,
// qui dérivent lentement), font des menaces (pas vif vers lui, puis retour) et se repositionnent ; celui qui obtient
// un jeton va frapper, de préférence dans le dos de Nathan (côté opposé à son regard, s'il n'est pas déjà pris).
[RequireComponent(typeof(Body25D), typeof(Health), typeof(HitReaction25D))]
public class Enemy25D : MonoBehaviour, IAttackSource25D
{
    public enum Phase { None, Windup, Active, Recovery }
    private enum Mode { Orbit, Engage, Threaten }

    public static readonly List<Enemy25D> Living = new();

    [SerializeField] private SpriteRenderer body;
    [SerializeField] private Animator animator;
    [Tooltip("Annonce au sol du cône du fusil à pompe.")]
    [SerializeField] private LineRenderer coneLine;
    [SerializeField] private GibSource gibs;

    [Header("Encerclement")]
    [SerializeField] private float orbitRadius = 2.9f;
    [SerializeField] private float orbitDepthRadius = 0.85f;
    [Tooltip("Rayon de l'ellipse pour un tireur (fusil à pompe).")]
    [SerializeField] private float rangedOrbitRadius = 5f;
    [Tooltip("Demi-ouverture (degrés) de chaque arc d'encerclement, à gauche et à droite de Nathan.")]
    [SerializeField] private float orbitArc = 55f;
    [Tooltip("Oscillation lente des places (degrés) : le groupe tourne autour de Nathan.")]
    [SerializeField] private float orbitLead = 14f;
    [SerializeField] private float separationRadius = 0.9f;
    [SerializeField] private float depthSpeedFactor = 0.65f;
    [SerializeField] private Vector2 decisionInterval = new(0.35f, 0.9f);
    [Tooltip("Abandon d'un assaut qui n'aboutit pas (s) : le jeton revient à un autre.")]
    [SerializeField] private float engageTimeout = 3.5f;
    [SerializeField] private float threatChance = 0.3f;
    [SerializeField] private float corpseTime = 1.6f;

    private static readonly List<(float side, Enemy25D enemy)> Orbiters = new();
    private static float slotsTime = -1f;

    private readonly HashSet<Health> hitTargets = new();
    private Body25D body25;
    private Rigidbody2D rb;
    private Health health;
    private HitReaction25D reaction;
    private CharacterAnimator25D characterAnimator;
    private EnemyArchetype archetype;
    private Body25D target;
    private float[] readyAt = Array.Empty<float>();
    private int[] uses = Array.Empty<int>();
    private EnemyAttack current;
    private int currentIndex;
    private bool shotFired;
    private Mode mode;
    private float engageSide;
    private float modeUntil;
    private float nextDecision;
    private float slotAngle;
    private bool arriving;
    private Vector2 arrivalTarget;
    private float deadTime;

    public EnemyArchetype Archetype => archetype;
    public Body25D Body => body25;
    public bool IsArriving => arriving;
    public bool IsDead => health.IsDead;
    public Phase CurrentPhase { get; private set; }
    public int PhaseFrame { get; private set; }
    public int PhaseLength { get; private set; }
    public bool HoldsToken { get; private set; }
    public bool IsEngaging => mode == Mode.Engage;
    public bool IsAimingShotgun => CurrentPhase == Phase.Windup && current != null && current.kind == EnemyAttackKind.Shotgun;

    public event Action<AttackData> AttackStarted;
    public event Action<Enemy25D> Defeated;

    private bool IsRanged => Find(EnemyAttackKind.Shotgun) >= 0;

    private void Awake()
    {
        body25 = GetComponent<Body25D>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction25D>();
        characterAnimator = GetComponent<CharacterAnimator25D>();
        health.Hit += OnHit;
        health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health == null)
            return;

        health.Hit -= OnHit;
        health.Died -= OnDied;
    }

    private void OnDisable()
    {
        Living.Remove(this);
        ReleaseToken();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Living.Clear();
        Orbiters.Clear();
        slotsTime = -1f;
    }

    // Prépare un ennemi de la réserve pour une nouvelle apparition (aucune instanciation pendant le combat).
    public void Setup(EnemyArchetype enemyArchetype, Body25D enemyTarget)
    {
        archetype = enemyArchetype;
        target = enemyTarget;
        health.SetMaxHealth(archetype.maxHealth);
        reaction.ResetState();
        reaction.KnockbackScale = archetype.KnockbackScale;
        reaction.HitstunScale = archetype.hitstunScale;
        readyAt = new float[archetype.attacks.Length];
        uses = new int[archetype.attacks.Length];
        CurrentPhase = Phase.None;
        current = null;
        mode = Mode.Orbit;
        nextDecision = Time.time + UnityEngine.Random.Range(0.3f, 0.8f);
        deadTime = 0f;

        if (body != null)
        {
            body.sprite = archetype.sprite;
            body.color = archetype.bodyColor;
            body.enabled = true;
            SpriteFlash.Set(body, 0f, Color.white);
        }

        if (animator != null)
        {
            animator.runtimeAnimatorController = archetype.animator;

            if (characterAnimator != null)
                characterAnimator.Rebind();
        }

        if (gibs != null)
            gibs.Configure(archetype.gibs, archetype.gibbable, archetype.bodySize.y);

        if (coneLine != null)
            coneLine.enabled = false;

        if (!Living.Contains(this))
            Living.Add(this);
    }

    // Entrée en scène : le corps part de start (hors de l'aire de jeu ou en hauteur), tombe s'il est en l'air, puis
    // marche jusqu'à entry. Intouchable tant qu'il n'est pas dans l'aire de jeu.
    public void BeginArrival(Vector2 start, float height, Vector2 entry, float heightVelocity = 0f)
    {
        body25.ClampToPlayfield = false;
        body25.Place(start, height);
        body25.HeightVelocity = heightVelocity;
        body25.SetFacing(entry.x >= start.x ? 1f : -1f);
        arriving = true;
        arrivalTarget = entry;
    }

    private void FixedUpdate()
    {
        if (archetype == null)
            return;

        if (health.IsDead)
        {
            UpdateCorpse();
            return;
        }

        if (body25.IsFrozen)
            return;

        if (arriving)
        {
            Arrive();
            return;
        }

        if (reaction.IsInHitstun)
        {
            if (CurrentPhase != Phase.None)
                CancelAction();

            ReleaseToken();
            mode = Mode.Orbit;
            return;
        }

        if (CurrentPhase != Phase.None)
        {
            StepAttack();
            return;
        }

        if (target == null || target.Health == null || target.Health.IsDead)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        UpdateSlots();
        Think();
    }

    // ---------------------------------------------------------------- Entrée

    private void Arrive()
    {
        bool inside = Playfield25D.Instance == null || Playfield25D.Instance.Contains(body25.Ground);

        if (!inside)
            health.GrantInvulnerability(2f / AttackData.FrameRate);

        if (body25.Airborne)
        {
            rb.linearVelocity = Vector2.zero;
            return;
        }

        Vector2 delta = arrivalTarget - body25.Ground;

        if (delta.sqrMagnitude < 0.04f)
        {
            arriving = false;
            body25.ClampToPlayfield = true;
            rb.linearVelocity = Vector2.zero;
            return;
        }

        rb.linearVelocity = delta.normalized * archetype.moveSpeed;
        body25.SetFacing(delta.x);
    }

    // ------------------------------------------------------------ Décisions

    private void Think()
    {
        Vector2 me = body25.Ground;
        Vector2 player = target.Ground;
        float dx = player.x - me.x;
        float dz = player.y - me.y;

        if (Time.time >= nextDecision)
            Decide(Mathf.Abs(dx));

        // L'assaillant (jeton obtenu) gère seul son approche et son coup.
        if (mode == Mode.Engage)
        {
            Engage(me, player, dx, dz);
            return;
        }

        Vector2 desired;
        float speed = archetype.moveSpeed;

        switch (mode)
        {
            case Mode.Threaten:
                // Pas vif vers Nathan sans frapper, puis retour à sa place.
                desired = player - new Vector2(Mathf.Sign(dx) * 1.6f, 0f);
                speed = archetype.altSpeed;

                if (Time.time >= modeUntil)
                    mode = Mode.Orbit;
                break;

            default:
                float radius = IsRanged ? rangedOrbitRadius : orbitRadius;
                desired = player + new Vector2(Mathf.Cos(slotAngle) * radius, Mathf.Sin(slotAngle) * orbitDepthRadius);
                break;
        }

        if (Playfield25D.Instance != null)
        {
            // Nathan près d'un bord : une place hors de la zone passe de l'autre côté de lui, au lieu de
            // s'écraser contre le bord avec les autres.
            if (mode == Mode.Orbit && !Playfield25D.Instance.Contains(new Vector2(desired.x, player.y)))
                desired.x = 2f * player.x - desired.x;

            desired = Playfield25D.Instance.Clamp(desired, out _);
        }

        MoveTo(desired, speed);
        body25.SetFacing(dx);
    }

    private void Decide(float distance)
    {
        nextDecision = Time.time + UnityEngine.Random.Range(decisionInterval.x, decisionInterval.y);

        if (mode == Mode.Engage)
        {
            if (Time.time < modeUntil)
                return;

            ReleaseToken();
            mode = Mode.Orbit;
        }

        int index = PickAttack(distance);

        if (index >= 0)
        {
            EnemyAttack attack = archetype.attacks[index];

            if (AttackTokens.TryAcquire(this, attack.IsRanged))
            {
                HoldsToken = true;
                currentIndex = index;
                mode = Mode.Engage;
                modeUntil = Time.time + engageTimeout;
                engageSide = PickSide();
                return;
            }
        }

        if (mode == Mode.Orbit && !IsRanged && UnityEngine.Random.value < threatChance)
        {
            mode = Mode.Threaten;
            modeUntil = Time.time + 0.35f;
        }
    }

    // Côté d'attaque : dans le dos de Nathan, sauf si un autre assaillant y est déjà.
    private float PickSide()
    {
        float back = -target.Facing;

        foreach (Enemy25D other in Living)
        {
            if (other != this && other.IsEngaging && Mathf.Approximately(other.engageSide, back))
                return -back;
        }

        return back;
    }

    private void Engage(Vector2 me, Vector2 player, float dx, float dz)
    {
        Vector2 desired;
        EnemyAttack attack = archetype.attacks[currentIndex];
        AttackData data = attack.data;
        float band = data.depthTolerance * 0.6f;

        if (attack.kind == EnemyAttackKind.Shotgun)
        {
            // Tireur : s'aligne sur la bande de Nathan à bonne distance, du côté où il se trouve.
            float side = me.x >= player.x ? 1f : -1f;
            float distance = Mathf.Clamp(Mathf.Abs(dx), data.closeRange + 0.5f, attack.range - 1f);
            desired = new Vector2(player.x + side * distance, player.y);

            if (Mathf.Abs(dz) <= band && Mathf.Abs(dx) <= attack.range && Mathf.Abs(dx) >= 1.5f)
            {
                body25.SetFacing(dx);
                StartAttack(currentIndex);
                return;
            }
        }
        else
        {
            desired = new Vector2(player.x + engageSide * attack.range * 0.8f, player.y);

            if (Mathf.Abs(dz) <= band && Mathf.Abs(dx) <= attack.range && Mathf.Sign(dx) == -engageSide)
            {
                body25.SetFacing(dx);
                StartAttack(currentIndex);
                return;
            }
        }

        if (Playfield25D.Instance != null)
            desired = Playfield25D.Instance.Clamp(desired, out _);

        MoveTo(desired, archetype.altSpeed);
        body25.SetFacing(dx);
    }

    private void MoveTo(Vector2 desired, float speed)
    {
        Vector2 delta = desired - body25.Ground;
        // Arrivée en douceur, et séparation : les ennemis ne s'empilent pas.
        Vector2 velocity = Vector2.ClampMagnitude(delta * 4f, speed);

        foreach (Enemy25D other in Living)
        {
            if (other == this || other.arriving)
                continue;

            Vector2 away = body25.Ground - other.body25.Ground;
            float distance = away.magnitude;

            if (distance > 0.001f && distance < separationRadius)
                velocity += away / distance * (separationRadius - distance) * speed * 1.5f;
        }

        velocity = Vector2.ClampMagnitude(velocity, speed);
        velocity.y *= depthSpeedFactor;
        rb.linearVelocity = velocity.sqrMagnitude < 0.04f ? Vector2.zero : velocity;
    }

    // Places autour de Nathan, réparties une fois par pas physique entre les ennemis qui tournent : deux arcs, à gauche
    // et à droite (une place juste devant ou derrière lui en profondeur le cacherait), chacun étagé en profondeur.
    // Chaque ennemi garde le côté et le rang où il se trouve ; les places oscillent lentement (le groupe tourne).
    private void UpdateSlots()
    {
        if (Mathf.Approximately(slotsTime, Time.fixedTime))
            return;

        slotsTime = Time.fixedTime;
        Orbiters.Clear();

        foreach (Enemy25D enemy in Living)
        {
            if (enemy.arriving || enemy.IsDead || enemy.mode == Mode.Engage || enemy.target == null)
                continue;

            Orbiters.Add((enemy.body25.Ground.x - enemy.target.Ground.x, enemy));
        }

        if (Orbiters.Count == 0)
            return;

        // Moitié gauche, moitié droite, selon la position actuelle.
        Orbiters.Sort((a, b) => a.side.CompareTo(b.side));
        int left = Orbiters.Count / 2;
        AssignArc(0, left, Mathf.PI);
        AssignArc(left, Orbiters.Count, 0f);
    }

    private void AssignArc(int from, int to, float baseAngle)
    {
        int count = to - from;

        if (count <= 0)
            return;

        // Rangés de l'arrière vers l'avant : chacun garde son étage.
        Orbiters.Sort(from, count, Comparer<(float, Enemy25D)>.Create((a, b) =>
            b.Item2.body25.Depth.CompareTo(a.Item2.body25.Depth)));
        float arc = orbitArc * Mathf.Deg2Rad;
        float sway = Mathf.Sin(Time.fixedTime * 0.6f) * orbitLead * Mathf.Deg2Rad;

        for (int i = 0; i < count; i++)
        {
            float t = count == 1 ? 0f : i / (count - 1f) * 2f - 1f;
            float offset = -t * arc + sway;
            // À droite, l'angle croît vers le fond ; à gauche (base = π), il décroît.
            Orbiters[from + i].enemy.slotAngle = baseAngle + (baseAngle == 0f ? offset : -offset);
        }
    }

    // --------------------------------------------------------------- Attaques

    private int PickAttack(float distance)
    {
        int best = -1;
        float total = 0f;

        for (int i = 0; i < archetype.attacks.Length; i++)
        {
            EnemyAttack attack = archetype.attacks[i];

            if (!CanUse(i))
                continue;

            // Le tireur garde le fusil pour la distance, la crosse pour le corps à corps.
            bool useful = attack.kind == EnemyAttackKind.Shotgun ? distance > 1.6f : distance < 3.5f || !IsRanged;

            if (!useful)
                continue;

            total += attack.weight;

            if (UnityEngine.Random.value * total <= attack.weight)
                best = i;
        }

        return best;
    }

    private int Find(EnemyAttackKind kind)
    {
        if (archetype == null)
            return -1;

        for (int i = 0; i < archetype.attacks.Length; i++)
        {
            if (archetype.attacks[i].kind == kind)
                return i;
        }

        return -1;
    }

    private bool CanUse(int index)
    {
        EnemyAttack attack = archetype.attacks[index];
        bool supported = attack.kind is EnemyAttackKind.Melee or EnemyAttackKind.Shotgun;
        return supported && attack.data != null && Time.time >= readyAt[index] && (attack.maxUses == 0 || uses[index] < attack.maxUses);
    }

    private void StartAttack(int index)
    {
        current = archetype.attacks[index];
        currentIndex = index;
        CurrentPhase = Phase.Windup;
        PhaseFrame = 0;
        PhaseLength = Mathf.Max(current.WindupFrames, EnemyAttack.MinWindupFrames);
        shotFired = false;
        hitTargets.Clear();
        rb.linearVelocity = Vector2.zero;
        uses[index]++;
        GameAudio.Play(current.data.windupSound);
        AttackStarted?.Invoke(current.data);
    }

    private void StepAttack()
    {
        PhaseFrame++;

        switch (CurrentPhase)
        {
            case Phase.Windup:
                // Annonce : voile de la couleur de l'attaque, qui pulse et se renforce.
                float t = PhaseFrame / (float)PhaseLength;
                Telegraph(0.2f + 0.4f * t * (0.6f + 0.4f * Mathf.Sin(PhaseFrame * 0.6f)));
                rb.linearVelocity = Vector2.zero;
                UpdateCone(true);

                if (PhaseFrame >= PhaseLength)
                    SetPhase(Phase.Active, current.ActiveFrames);
                break;

            case Phase.Active:
                if (current.kind == EnemyAttackKind.Shotgun)
                {
                    if (!shotFired)
                        FireShotgun();
                }
                else
                {
                    rb.linearVelocity = new Vector2(body25.Facing * current.data.StepSpeed, 0f);
                    Hit25D.Strike(body25, current.data, hitTargets);
                }

                if (PhaseFrame >= PhaseLength)
                    SetPhase(Phase.Recovery, current.RecoveryFrames);
                break;

            case Phase.Recovery:
                rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 30f * Time.fixedDeltaTime);

                if (PhaseFrame >= PhaseLength)
                    EndAttack();
                break;
        }
    }

    private void SetPhase(Phase phase, int length)
    {
        CurrentPhase = phase;
        PhaseFrame = 0;
        PhaseLength = Mathf.Max(1, length);

        if (phase != Phase.Windup)
        {
            Telegraph(0f);
            UpdateCone(false);
        }

        if (phase == Phase.Active)
            GameAudio.Play(current.data.activeSound);
        else if (phase == Phase.Recovery)
            GameAudio.Play(current.data.recoverySound, 0.8f);

        if (PhaseLength <= 0)
            EndAttack();
    }

    private void FireShotgun()
    {
        shotFired = true;
        Vector2 muzzle = body25.Ground + new Vector2(archetype.muzzleOffset.x * body25.Facing, archetype.muzzleOffset.y + body25.Height);
        Hit25D.Fire(body25, muzzle, current.data);
    }

    private void EndAttack()
    {
        readyAt[currentIndex] = Time.time + current.cooldown;
        CurrentPhase = Phase.None;
        current = null;
        ReleaseToken();
        mode = Mode.Orbit;
        nextDecision = Time.time + UnityEngine.Random.Range(0.4f, 1.1f);
    }

    private void CancelAction()
    {
        if (current != null)
            readyAt[currentIndex] = Time.time + current.cooldown;

        CurrentPhase = Phase.None;
        current = null;
        Telegraph(0f);
        UpdateCone(false);
    }

    private void ReleaseToken()
    {
        if (!HoldsToken)
            return;

        HoldsToken = false;
        AttackTokens.Release(this);
    }

    private void OnHit(DamageInfo info)
    {
        // Interrompu pendant l'armement, sauf super-armure.
        if (CurrentPhase == Phase.Windup && !archetype.superArmor)
        {
            CancelAction();
            ReleaseToken();
        }
    }

    private void OnDied()
    {
        CancelAction();
        ReleaseToken();
        Living.Remove(this);
        deadTime = 0f;
        Defeated?.Invoke(this);
    }

    private void UpdateCorpse()
    {
        deadTime += Time.fixedDeltaTime;

        // Le corps clignote puis disparaît dans la réserve (pas de Destroy pendant le combat).
        if (body != null && deadTime > corpseTime * 0.6f)
            body.enabled = Mathf.FloorToInt(deadTime * 12f) % 2 == 0;

        if (deadTime >= corpseTime)
            gameObject.SetActive(false);
    }

    private void Telegraph(float amount)
    {
        if (body != null && SpriteFlash.Supports(body))
            SpriteFlash.Set(body, amount, current != null ? current.telegraphColor : Color.white);
    }

    // Triangle au sol : la zone du tir, qui s'élargit en profondeur avec la distance (Hit25D.ConeHalfDepth).
    private void UpdateCone(bool visible)
    {
        if (coneLine == null)
            return;

        visible &= current != null && current.kind == EnemyAttackKind.Shotgun;
        coneLine.enabled = visible;

        if (!visible)
            return;

        AttackData data = current.data;
        float facing = body25.Facing;
        Vector2 origin = body25.Ground + new Vector2(archetype.muzzleOffset.x * facing, 0f);
        float far = data.maxRange;
        float half = Hit25D.ConeHalfDepth(data, far);
        coneLine.positionCount = 4;
        coneLine.SetPosition(0, new Vector3(origin.x, origin.y + data.depthTolerance, 0f));
        coneLine.SetPosition(1, new Vector3(origin.x + facing * far, origin.y + half, 0f));
        coneLine.SetPosition(2, new Vector3(origin.x + facing * far, origin.y - half, 0f));
        coneLine.SetPosition(3, new Vector3(origin.x, origin.y - data.depthTolerance, 0f));
        Color color = current.telegraphColor;
        color.a = 0.35f + 0.4f * (PhaseFrame / (float)Mathf.Max(1, PhaseLength));
        coneLine.startColor = color;
        coneLine.endColor = color;
    }
}
