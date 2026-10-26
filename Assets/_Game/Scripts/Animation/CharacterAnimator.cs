using System.Collections.Generic;
using UnityEngine;

// Pont entre le gameplay et l'Animator. Le gameplay ne dépend jamais de ce
// composant : sans Animator, ou sans un paramètre donné, rien ne casse.
// Paramètres reconnus : Speed, VerticalSpeed (float), Grounded, Dead (bool),
// ComboStep (int), Attack, Dodge, Hurt (trigger).
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

    [SerializeField] private Animator animator;

    private readonly HashSet<int> parameters = new();
    private Rigidbody2D rb;
    private Health health;
    private PlayerController controller;
    private PlayerCombat combat;
    private PlayerDodge dodge;
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
        enemy = GetComponent<Enemy>();

        if (animator != null && animator.runtimeAnimatorController != null)
        {
            foreach (AnimatorControllerParameter parameter in animator.parameters)
                parameters.Add(parameter.nameHash);
        }

        if (combat != null) combat.AttackStarted += OnPlayerAttack;
        if (enemy != null) enemy.AttackStarted += OnEnemyAttack;
        if (dodge != null) dodge.DodgeStarted += OnDodge;

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (combat != null) combat.AttackStarted -= OnPlayerAttack;
        if (enemy != null) enemy.AttackStarted -= OnEnemyAttack;
        if (dodge != null) dodge.DodgeStarted -= OnDodge;

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
            animator.SetBool(GroundedHash, controller == null || controller.IsGrounded);
    }

    private void OnPlayerAttack(int step)
    {
        if (parameters.Contains(ComboStepHash))
            animator.SetInteger(ComboStepHash, step);

        Trigger(AttackHash);
    }

    private void OnEnemyAttack(EnemyAttack attack) => Trigger(AttackHash);

    private void OnDodge() => Trigger(DodgeHash);

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
