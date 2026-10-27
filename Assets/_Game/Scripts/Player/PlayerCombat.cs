using System;
using System.Collections.Generic;
using UnityEngine;

// Attaques de Nathan, image par image (60 FPS), sur des AttackData.
// Au sol : J léger → J moyen → J crochet ; K coup lourd ; bas + K lanceur (le saut l'annule
// s'il touche, pour suivre en l'air). En l'air : J ×3 puis K smash qui renvoie au sol.
// Annulations : léger → moyen → lourd, pendant la fenêtre d'annulation de chaque attaque.
// Les commandes restent en mémoire inputBufferFrames images.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private LayerMask targetLayers;
    [SerializeField] private SpriteRenderer hitboxDebug;

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

    [Header("Réglages")]
    [SerializeField] private int inputBufferFrames = 6;
    [Tooltip("Vitesse de chute maximale pendant une attaque aérienne (Nathan plane un peu).")]
    [SerializeField] private float airAttackFallSpeed = -1.5f;

    private readonly HashSet<Health> hitTargets = new();
    private readonly Collider2D[] overlaps = new Collider2D[16];
    private Rigidbody2D rb;
    private Health health;
    private PlayerDash dash;
    // Mémoire des commandes, en images de jeu : elle ne s'écoule pas pendant le hitstop.
    private int lightBufferLeft;
    private int heavyBufferLeft;
    private bool airAttack;

    public AttackData CurrentAttack { get; private set; }
    // Image en cours de l'attaque, à partir de 1.
    public int CurrentFrame { get; private set; }
    public bool IsAttacking => CurrentAttack != null;
    public bool HitConnected { get; private set; }

    // Arme de test (F2) : les coups de Nathan comptent comme des coups de lame.
    public bool TestBlade { get; set; }

    public string Phase
    {
        get
        {
            if (CurrentAttack == null)
                return "—";

            if (CurrentFrame <= CurrentAttack.startup)
                return "démarrage";

            return CurrentFrame <= CurrentAttack.startup + CurrentAttack.active ? "actif" : "récupération";
        }
    }

    public bool InCancelWindow =>
        CurrentAttack != null &&
        CurrentFrame >= CurrentAttack.cancelWindow.x &&
        CurrentFrame <= CurrentAttack.cancelWindow.y;

    private bool IsActiveFrame =>
        CurrentAttack != null &&
        CurrentFrame > CurrentAttack.startup &&
        CurrentFrame <= CurrentAttack.startup + CurrentAttack.active;

    public event Action<AttackData> AttackStarted;
    public event Action<AttackData, Health> HitLanded;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        dash = GetComponent<PlayerDash>();

        if (controller == null)
            controller = GetComponent<PlayerController>();

        if (hitboxDebug != null)
            hitboxDebug.enabled = false;
    }

    private void OnDisable()
    {
        CancelAttack();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (GameInput.LightPressed)
            lightBufferLeft = inputBufferFrames;

        if (GameInput.HeavyPressed)
            heavyBufferLeft = inputBufferFrames;
    }

    private void FixedUpdate()
    {
        if (health != null && health.IsDead)
        {
            CancelAttack();
            return;
        }

        if (controller.IsFrozen)
            return;

        if (lightBufferLeft > 0) lightBufferLeft--;
        if (heavyBufferLeft > 0) heavyBufferLeft--;

        // Touché pendant une attaque : elle est perdue.
        if (controller.IsInHitstun)
        {
            CancelAttack();
            return;
        }

        if (CurrentAttack == null)
        {
            TryStartFromBuffer();
            return;
        }

        CurrentFrame++;
        UpdateMovement();

        if (IsActiveFrame)
            CheckHits();

        UpdateHitboxDebug();

        if (TryCancel())
            return;

        // Une attaque aérienne s'arrête à l'atterrissage.
        bool landed = airAttack && controller.IsGrounded && CurrentFrame > 2 && rb.linearVelocity.y <= 0f;

        if (landed || CurrentFrame >= CurrentAttack.TotalFrames)
            EndAttack();
    }

    private void TryStartFromBuffer()
    {
        if (!controller.CanAct || (TryGetComponent(out PlayerDodge dodge) && dodge.IsDodging))
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
        if (CurrentAttack == launcher && HitConnected && controller.TryConsumeJump())
        {
            EndAttack();
            controller.Jump();
            return true;
        }

        AttackData next = NextAttack(CurrentAttack);

        if (next == null)
            return false;

        StartAttack(next);
        return true;
    }

    // Chaîne : léger → moyen → lourd au sol, J ×3 → smash en l'air. Jamais en arrière.
    private AttackData NextAttack(AttackData from)
    {
        bool light = lightBufferLeft > 0;
        bool heavyInput = heavyBufferLeft > 0;

        if (!light && !heavyInput)
            return null;

        AttackData next = null;

        if (!controller.IsGrounded)
        {
            if (heavyInput && from != smash)
                next = smash;
            else if (light)
                next = from == null ? airLight1 : from == airLight1 ? airLight2 : from == airLight2 ? airLight3 : null;
        }
        else
        {
            bool canGoHeavy = from == null || from == jab || from == cross || from == hook;

            if (heavyInput && canGoHeavy)
                next = GameInput.DownHeld ? launcher : heavy;
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

        // Un dash peut être annulé en attaque : Nathan garde une partie de son élan.
        if (dash != null && dash.IsDashing)
            dash.CancelIntoAttack();

        // On peut se retourner au moment de frapper.
        float input = controller.HorizontalInput;

        if (Mathf.Abs(input) > 0.01f)
            controller.SetFacing(input > 0f);

        CurrentAttack = attack;
        CurrentFrame = 0;
        HitConnected = false;
        airAttack = !controller.IsGrounded;
        hitTargets.Clear();
        controller.LockMovement(attack.TotalFrames / AttackData.FrameRate);
        AttackStarted?.Invoke(attack);
    }

    private void UpdateMovement()
    {
        controller.LockMovement(2f / AttackData.FrameRate);
        Vector2 velocity = rb.linearVelocity;

        if (airAttack)
        {
            velocity.y = Mathf.Max(velocity.y, airAttackFallSpeed);
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 20f * Time.fixedDeltaTime);
        }
        else if (CurrentFrame <= CurrentAttack.startup + CurrentAttack.active)
        {
            velocity.x = controller.Facing * CurrentAttack.StepSpeed;
        }
        else
        {
            velocity.x = Mathf.MoveTowards(velocity.x, 0f, 40f * Time.fixedDeltaTime);
        }

        rb.linearVelocity = velocity;
    }

    private void CheckHits()
    {
        Vector2 center = HitboxCenter();
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = targetLayers };
        int count = Physics2D.OverlapBox(center, CurrentAttack.hitboxSize, 0f, filter, overlaps);
        DamageType? type = TestBlade ? DamageType.Blade : null;

        for (int i = 0; i < count; i++)
        {
            Health target = overlaps[i].GetComponentInParent<Health>();

            if (target == null || target == health || target.IsDead || !hitTargets.Add(target))
                continue;

            // Point d'impact : bord de la cible le plus proche du centre du coup.
            Vector2 point = overlaps[i].ClosestPoint(center);

            if (Combat.Strike(gameObject, target, CurrentAttack, point, controller.Facing, DamageKind.Melee, true, type))
            {
                HitConnected = true;
                HitLanded?.Invoke(CurrentAttack, target);
            }
        }
    }

    private Vector2 HitboxCenter() =>
        rb.position + new Vector2(CurrentAttack.hitboxOffset.x * controller.Facing, CurrentAttack.hitboxOffset.y);

    private void UpdateHitboxDebug()
    {
        if (hitboxDebug == null)
            return;

        bool show = CombatDebugOverlay.Visible && CurrentAttack != null;
        hitboxDebug.enabled = show;

        if (!show)
            return;

        // Gris au démarrage, rouge pendant l'actif, bleu en récupération.
        hitboxDebug.color = IsActiveFrame ? new Color(1f, 0.2f, 0.2f, 0.45f)
            : CurrentFrame <= CurrentAttack.startup ? new Color(0.8f, 0.8f, 0.8f, 0.25f)
            : new Color(0.3f, 0.5f, 1f, 0.2f);

        Transform box = hitboxDebug.transform;
        box.position = HitboxCenter();
        Vector3 parentScale = transform.lossyScale;
        box.localScale = new Vector3(CurrentAttack.hitboxSize.x / Mathf.Abs(parentScale.x), CurrentAttack.hitboxSize.y / parentScale.y, 1f);
    }

    private void EndAttack()
    {
        CurrentAttack = null;
        CurrentFrame = 0;
        airAttack = false;
        controller.UnlockMovement();

        if (hitboxDebug != null)
            hitboxDebug.enabled = false;
    }

    // Interrompt le coup en cours, par exemple quand le joueur esquive ou se fait toucher.
    public void CancelAttack()
    {
        if (CurrentAttack == null)
            return;

        EndAttack();
    }
}
