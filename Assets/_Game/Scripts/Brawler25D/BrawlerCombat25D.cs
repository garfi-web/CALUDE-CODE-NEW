using System;
using System.Collections.Generic;
using UnityEngine;

// Source des attaques pour CharacterAnimator25D (Nathan et ennemis de l'arène 2,5D).
public interface IAttackSource25D
{
    event Action<AttackData> AttackStarted;
}

// Attaques de Nathan dans l'arène 2,5D, image par image (60 FPS), sur les AttackData de l'arène latérale.
// Au sol : J léger → J moyen → J crochet ; K coup lourd ; arrière + K lanceur (le bas sert à marcher vers l'avant).
// En l'air : J ×3 puis K smash qui plonge. Barre de fer : J près d'elle pour la ramasser, puis J frappe avec elle
// jusqu'à ce qu'elle casse. Fusil à pompe (U) : cône qui s'élargit en profondeur (Hit25D.Fire), rechargement (I).
// Les coups ne touchent que la bande de profondeur de Nathan (AttackData.depthTolerance).
[RequireComponent(typeof(Brawler25D), typeof(Body25D))]
public class BrawlerCombat25D : MonoBehaviour, IAttackSource25D
{
    [Header("Au sol")]
    [SerializeField] private AttackData jab;
    [SerializeField] private AttackData cross;
    [SerializeField] private AttackData hook;
    [SerializeField] private AttackData heavy;
    [SerializeField] private AttackData launcher;

    [Header("En l'air")]
    [SerializeField] private AttackData airLight1;
    [SerializeField] private AttackData airLight2;
    [SerializeField] private AttackData airLight3;
    [SerializeField] private AttackData smash;
    [SerializeField] private float airAttackFallSpeed = -1.5f;
    [SerializeField] private float smashDiveSpeed = -16f;

    [Header("Barre de fer")]
    [SerializeField] private AttackData[] weaponCombo = Array.Empty<AttackData>();
    [SerializeField] private SpriteRenderer weaponSprite;
    [SerializeField] private float pickupRange = 0.9f;

    [Header("Fusil à pompe")]
    [SerializeField] private AttackData shotgun;
    [Tooltip("Bout du canon depuis les pieds, vers l'avant.")]
    [SerializeField] private Vector2 muzzleOffset = new(1.1f, 1.15f);
    [SerializeField] private int magazine = 6;
    [SerializeField] private int shellFrames = 24;
    [SerializeField] private float recoilSpeed = 6f;
    [SerializeField] private AudioClip shellSound;

    [SerializeField] private int inputBufferFrames = 6;

    private readonly HashSet<Health> hitTargets = new();
    private Brawler25D mover;
    private Body25D body;
    private Rigidbody2D rb;
    private Health health;
    private int lightBufferLeft;
    private int heavyBufferLeft;
    private bool fireRequested;
    private bool reloadRequested;
    private bool airAttack;
    private bool shot;
    private int reloadFrame;
    private int weaponHits;

    public AttackData CurrentAttack { get; private set; }
    public int CurrentFrame { get; private set; }
    public bool IsAttacking => CurrentAttack != null;
    public bool HitConnected { get; private set; }
    public bool HasWeapon => weaponHits > 0;
    public int Shells { get; private set; }
    public bool IsReloading { get; private set; }

    public event Action<AttackData> AttackStarted;
    public event Action<AttackData, Health> HitLanded;
    public event Action<int, int> AmmoChanged;

    private bool InCancelWindow =>
        CurrentAttack != null && CurrentFrame >= CurrentAttack.cancelWindow.x && CurrentFrame <= CurrentAttack.cancelWindow.y;

    private bool IsActiveFrame =>
        CurrentAttack != null && CurrentFrame > CurrentAttack.startup && CurrentFrame <= CurrentAttack.startup + CurrentAttack.active;

    private bool IsShotgun => CurrentAttack != null && CurrentAttack == shotgun;

    private void Awake()
    {
        mover = GetComponent<Brawler25D>();
        body = GetComponent<Body25D>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        Shells = magazine;

        if (weaponSprite != null)
            weaponSprite.enabled = false;
    }

    private void Start() => AmmoChanged?.Invoke(Shells, magazine);

    private void OnDisable() => CancelAttack();

    private void Update()
    {
        if (health.IsDead)
            return;

        if (GameInput.LightPressed)
            lightBufferLeft = inputBufferFrames;

        if (GameInput.HeavyPressed)
            heavyBufferLeft = inputBufferFrames;

        if (shotgun != null && GameInput.ShotgunPressed)
            fireRequested = true;

        if (shotgun != null && GameInput.ReloadPressed)
            reloadRequested = true;
    }

    private void FixedUpdate()
    {
        if (health.IsDead)
        {
            CancelAttack();
            return;
        }

        if (mover.IsFrozen)
            return;

        if (lightBufferLeft > 0) lightBufferLeft--;
        if (heavyBufferLeft > 0) heavyBufferLeft--;

        if (mover.IsInHitstun || mover.IsDodging)
        {
            CancelAttack();
            IsReloading = false;
            return;
        }

        if (CurrentAttack == null)
        {
            if (TryPickup() || TryShotgun())
                return;

            StepReload();
            TryStartFromBuffer();
            return;
        }

        CurrentFrame++;
        UpdateMovement();

        if (IsActiveFrame)
        {
            if (IsShotgun)
                Shoot();
            else
                CheckHits();
        }

        UpdateWeaponPose();

        if (!IsShotgun && TryCancel())
            return;

        bool landed = airAttack && !body.Airborne && CurrentFrame > 2;

        if (landed || CurrentFrame >= CurrentAttack.TotalFrames)
            EndAttack();
    }

    // ------------------------------------------------------------------ Combo

    private void TryStartFromBuffer()
    {
        if (!mover.CanAct)
            return;

        AttackData next = NextAttack(null);

        if (next != null)
            StartAttack(next);
    }

    private bool TryCancel()
    {
        if (!InCancelWindow)
            return false;

        // Lanceur réussi : le saut l'annule pour suivre l'ennemi en l'air.
        if (CurrentAttack == launcher && HitConnected && mover.TryConsumeJump())
        {
            EndAttack();
            return true;
        }

        AttackData next = NextAttack(CurrentAttack);

        if (next == null)
            return false;

        StartAttack(next);
        return true;
    }

    private AttackData NextAttack(AttackData from)
    {
        bool light = lightBufferLeft > 0;
        bool heavyInput = heavyBufferLeft > 0;

        if (!light && !heavyInput)
            return null;

        AttackData next = null;

        if (body.Airborne)
        {
            if (heavyInput && from != smash)
                next = smash;
            else if (light)
                next = from == null ? airLight1 : from == airLight1 ? airLight2 : from == airLight2 ? airLight3 : null;
        }
        else if (HasWeapon && light && weaponCombo.Length > 0)
        {
            // Barre de fer : la chaîne tourne sur ses coups tant qu'elle tient.
            bool chained = from == null || Array.IndexOf(weaponCombo, from) >= 0;
            next = chained ? weaponCombo[from == null ? 0 : (Array.IndexOf(weaponCombo, from) + 1) % weaponCombo.Length] : null;
        }
        else
        {
            bool canGoHeavy = from == null || from == jab || from == cross || from == hook;
            bool back = mover.Input.x * body.Facing < -0.5f;

            if (heavyInput && canGoHeavy)
                next = back ? launcher : heavy;
            else if (light)
                next = from == null ? jab : from == jab ? cross : from == cross ? hook : null;
        }

        if (next == null)
            return null;

        if (heavyInput && (next == heavy || next == launcher || next == smash))
            heavyBufferLeft = 0;
        else
            lightBufferLeft = 0;

        return next;
    }

    private void StartAttack(AttackData attack)
    {
        if (attack == null)
            return;

        // On peut se retourner au moment de frapper, sauf pour le lanceur (arrière + K).
        if (attack != launcher && Mathf.Abs(mover.Input.x) > 0.01f)
            body.SetFacing(mover.Input.x);

        CurrentAttack = attack;
        CurrentFrame = 0;
        HitConnected = false;
        airAttack = body.Airborne;
        shot = false;
        hitTargets.Clear();
        mover.LockMovement(attack.TotalFrames / AttackData.FrameRate);
        AttackStarted?.Invoke(attack);
    }

    private void UpdateMovement()
    {
        mover.LockMovement(2f / AttackData.FrameRate);

        if (airAttack)
        {
            if (CurrentAttack == smash && CurrentFrame == CurrentAttack.startup)
                body.HeightVelocity = smashDiveSpeed;
            else if (CurrentAttack != smash)
                body.HeightVelocity = Mathf.Max(body.HeightVelocity, airAttackFallSpeed);

            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 20f * Time.fixedDeltaTime);
        }
        else if (IsShotgun)
        {
            // Le recul du tir glisse ; sinon immobile.
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 25f * Time.fixedDeltaTime);
        }
        else if (CurrentFrame <= CurrentAttack.startup + CurrentAttack.active)
        {
            rb.linearVelocity = new Vector2(body.Facing * CurrentAttack.StepSpeed, 0f);
        }
        else
        {
            rb.linearVelocity = Vector2.MoveTowards(rb.linearVelocity, Vector2.zero, 40f * Time.fixedDeltaTime);
        }
    }

    private void CheckHits()
    {
        bool weapon = HasWeapon && Array.IndexOf(weaponCombo, CurrentAttack) >= 0;

        int hits = Hit25D.Strike(body, CurrentAttack, hitTargets, null, target =>
        {
            HitConnected = true;
            HitLanded?.Invoke(CurrentAttack, target);
        });

        if (weapon && hits > 0 && (weaponHits -= hits) <= 0)
            BreakWeapon();
    }

    private void EndAttack()
    {
        CurrentAttack = null;
        CurrentFrame = 0;
        airAttack = false;
        mover.UnlockMovement();
        UpdateWeaponPose();
    }

    public void CancelAttack()
    {
        if (CurrentAttack != null)
            EndAttack();
    }

    // ------------------------------------------------------------ Barre de fer

    private bool TryPickup()
    {
        if (HasWeapon || lightBufferLeft <= 0 || !mover.CanAct || body.Airborne)
            return false;

        Pickup25D pickup = Pickup25D.Nearest(body.Ground, pickupRange, Pickup25D.Kind.Weapon);

        if (pickup == null)
            return false;

        lightBufferLeft = 0;
        weaponHits = pickup.Take();

        if (weaponSprite != null)
            weaponSprite.enabled = true;

        return true;
    }

    private void BreakWeapon()
    {
        weaponHits = 0;

        if (weaponSprite != null)
        {
            weaponSprite.enabled = false;

            if (CombatVfx.Instance != null)
                CombatVfx.Instance.Dust(weaponSprite.transform.position, 6);
        }
    }

    // Barre tenue : levée au démarrage, abattue pendant l'actif.
    private void UpdateWeaponPose()
    {
        if (weaponSprite == null || !HasWeapon)
            return;

        float angle = 35f;

        if (CurrentAttack != null && Array.IndexOf(weaponCombo, CurrentAttack) >= 0)
        {
            int swingEnd = CurrentAttack.startup + CurrentAttack.active;
            angle = CurrentFrame <= CurrentAttack.startup ? 100f
                : CurrentFrame <= swingEnd ? Mathf.Lerp(100f, -40f, (CurrentFrame - CurrentAttack.startup) / (float)CurrentAttack.active)
                : -40f;
        }

        weaponSprite.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }

    // ---------------------------------------------------------- Fusil à pompe

    private bool TryShotgun()
    {
        if (!fireRequested || shotgun == null)
            return false;

        fireRequested = false;

        if (!mover.CanAct || body.Airborne)
            return false;

        if (Shells <= 0)
        {
            reloadRequested = true;
            return false;
        }

        IsReloading = false;

        if (Mathf.Abs(mover.Input.x) > 0.01f)
            body.SetFacing(mover.Input.x);

        StartAttack(shotgun);
        return true;
    }

    private void Shoot()
    {
        if (shot)
            return;

        shot = true;
        Shells--;
        AmmoChanged?.Invoke(Shells, magazine);
        GameAudio.Play(shotgun.activeSound);
        Vector2 muzzle = body.Ground + new Vector2(muzzleOffset.x * body.Facing, muzzleOffset.y + body.Height);
        Shotgun.Result result = Hit25D.Fire(body, muzzle, shotgun);

        rb.linearVelocity = new Vector2(-body.Facing * recoilSpeed, 0f);
        GameAudio.Play(shotgun.recoverySound, 0.8f);

        if (CombatVfx.Instance != null)
        {
            CombatVfx.Instance.EjectShell(muzzle - new Vector2(body.Facing * 0.6f, 0f), body.Facing);

            if (result.Kills >= 2)
                CombatVfx.Instance.MultiKill();
        }
    }

    private void StepReload()
    {
        if (reloadRequested)
        {
            reloadRequested = false;

            if (Shells < magazine)
            {
                IsReloading = true;
                reloadFrame = 0;
            }
        }

        if (!IsReloading || ++reloadFrame < shellFrames)
            return;

        reloadFrame = 0;
        Shells++;
        GameAudio.Play(shellSound);
        AmmoChanged?.Invoke(Shells, magazine);
        IsReloading = Shells < magazine;
    }
}
