using System.Collections.Generic;
using UnityEngine;

// Pont entre le gameplay et l'Animator. Le gameplay ne dépend jamais de ce
// composant : sans Animator, ou sans un paramètre donné, rien ne casse.
// Paramètres reconnus : Speed, VerticalSpeed (float), Grounded, Dead (bool),
// Stunned, Down (bool, d'après HitReaction), ComboStep (int, AttackData.animationId),
// Attack, Dodge, Dash, Hurt, Hold (trigger). Pour un ennemi, ComboStep vaut l'animationId de son attaque.
// Pendant un arrêt sur image (HitStop), la vitesse de l'Animator passe à 0.
public class CharacterAnimator : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int DashHash = Animator.StringToHash("Dash");
    private static readonly int StunnedHash = Animator.StringToHash("Stunned");
    private static readonly int DownHash = Animator.StringToHash("Down");
    private static readonly int HoldHash = Animator.StringToHash("Hold");

    [SerializeField] private Animator animator;

    private readonly HashSet<int> parameters = new();
    private Rigidbody2D rb;
    private Health health;
    private PlayerController controller;
    private PlayerCombat combat;
    private PlayerDodge dodge;
    private PlayerDash dash;
    private PlayerShotgun shotgun;
    private HitStop hitStop;
    private HitReaction reaction;
    private Enemy enemy;

    private void Awake()
    {
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        controller = GetComponent<PlayerController>();
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();
        dash = GetComponent<PlayerDash>();
        shotgun = GetComponent<PlayerShotgun>();
        hitStop = GetComponent<HitStop>();
        reaction = GetComponent<HitReaction>();
        enemy = GetComponent<Enemy>();

        CacheParameters();

        if (combat != null) combat.AttackStarted += OnPlayerAttack;
        if (shotgun != null) shotgun.Fired += OnPlayerAttack;
        if (shotgun != null) shotgun.ShellLoading += OnEnemyHold;
        if (enemy != null) enemy.AttackStarted += OnEnemyAttack;
        if (enemy != null) enemy.HoldStarted += OnEnemyHold;
        if (dodge != null) dodge.DodgeStarted += OnDodge;
        if (dash != null) dash.DashStarted += OnDash;
        if (hitStop != null) hitStop.FrozenChanged += OnFrozenChanged;

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    // Animator ajouté après coup (ennemi qui reçoit les sprites de son archétype à l'apparition).
    public void SetAnimator(Animator newAnimator)
    {
        animator = newAnimator;
        CacheParameters();
    }

    private void CacheParameters()
    {
        parameters.Clear();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            parameters.Add(parameter.nameHash);
    }

    private void OnDestroy()
    {
        if (combat != null) combat.AttackStarted -= OnPlayerAttack;
        if (shotgun != null) shotgun.Fired -= OnPlayerAttack;
        if (shotgun != null) shotgun.ShellLoading -= OnEnemyHold;
        if (enemy != null) enemy.AttackStarted -= OnEnemyAttack;
        if (enemy != null) enemy.HoldStarted -= OnEnemyHold;
        if (dodge != null) dodge.DodgeStarted -= OnDodge;
        if (dash != null) dash.DashStarted -= OnDash;
        if (hitStop != null) hitStop.FrozenChanged -= OnFrozenChanged;

        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void Update()
    {
        if (parameters.Count == 0 || rb == null)
            return;

        Vector2 velocity = rb.linearVelocity;

        if (parameters.Contains(SpeedHash))
            animator.SetFloat(SpeedHash, Mathf.Abs(velocity.x));

        if (parameters.Contains(VerticalSpeedHash))
            animator.SetFloat(VerticalSpeedHash, velocity.y);

        if (parameters.Contains(GroundedHash))
            animator.SetBool(GroundedHash, controller != null ? controller.IsGrounded : reaction == null || reaction.IsGrounded);

        if (reaction != null && parameters.Contains(StunnedHash))
            animator.SetBool(StunnedHash, reaction.IsInHitstun && !reaction.IsKnockedDown);

        if (reaction != null && parameters.Contains(DownHash))
            animator.SetBool(DownHash, reaction.IsKnockedDown);
    }

    private void OnPlayerAttack(AttackData attack)
    {
        if (parameters.Contains(ComboStepHash))
            animator.SetInteger(ComboStepHash, attack.animationId);

        Trigger(AttackHash);
    }

    // L'attaque d'un ennemi choisit son clip par son AttackData.animationId, comme le combo de Nathan.
    private void OnEnemyAttack(EnemyAttack attack)
    {
        if (parameters.Contains(ComboStepHash))
            animator.SetInteger(ComboStepHash, attack.data != null ? attack.data.animationId : 0);

        Trigger(AttackHash);
    }

    // Immobilisation : rechargement du Flic, cartouche glissée dans le fusil de Nathan M4.
    private void OnEnemyHold() => Trigger(HoldHash);

    private void OnDodge() => Trigger(DodgeHash);

    private void OnDash() => Trigger(DashHash);

    private void OnFrozenChanged(bool frozen)
    {
        if (animator != null)
            animator.speed = frozen ? 0f : 1f;
    }

    private void OnDamaged(int damage) => Trigger(HurtHash);

    private void OnDied()
    {
        if (parameters.Contains(DeadHash))
            animator.SetBool(DeadHash, true);
    }

    private void Trigger(int hash)
    {
        if (parameters.Contains(hash))
            animator.SetTrigger(hash);
    }
}
