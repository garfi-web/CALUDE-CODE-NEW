using System.Collections.Generic;
using UnityEngine;

// Pont entre le gameplay de l'arène 2,5D et l'Animator, sur les mêmes paramètres que CharacterAnimator (les
// contrôleurs d'animation des sprites sont partagés) : Speed (vitesse dans le plan du sol), VerticalSpeed (vitesse
// de hauteur), Grounded, Dead, Stunned, Down, ComboStep, Attack, Dodge, Hurt. Le gameplay n'en dépend jamais.
public class CharacterAnimator25D : MonoBehaviour
{
    private static readonly int SpeedHash = Animator.StringToHash("Speed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int GroundedHash = Animator.StringToHash("Grounded");
    private static readonly int DeadHash = Animator.StringToHash("Dead");
    private static readonly int ComboStepHash = Animator.StringToHash("ComboStep");
    private static readonly int AttackHash = Animator.StringToHash("Attack");
    private static readonly int DodgeHash = Animator.StringToHash("Dodge");
    private static readonly int HurtHash = Animator.StringToHash("Hurt");
    private static readonly int StunnedHash = Animator.StringToHash("Stunned");
    private static readonly int DownHash = Animator.StringToHash("Down");

    [SerializeField] private Animator animator;

    private readonly HashSet<int> parameters = new();
    private Body25D body;
    private Health health;
    private HitReaction25D reaction;
    private IAttackSource25D attacks;
    private Brawler25D brawler;
    private HitStop hitStop;

    private void Awake()
    {
        body = GetComponent<Body25D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction25D>();
        attacks = GetComponent<IAttackSource25D>();
        brawler = GetComponent<Brawler25D>();
        hitStop = GetComponent<HitStop>();

        if (attacks != null) attacks.AttackStarted += OnAttack;
        if (brawler != null) brawler.DodgeStarted += OnDodge;
        if (hitStop != null) hitStop.FrozenChanged += OnFrozenChanged;

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }

        Rebind();
    }

    private void OnDestroy()
    {
        if (attacks != null) attacks.AttackStarted -= OnAttack;
        if (brawler != null) brawler.DodgeStarted -= OnDodge;
        if (hitStop != null) hitStop.FrozenChanged -= OnFrozenChanged;

        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    // Contrôleur changé (ennemi qui reçoit les sprites de son archétype).
    public void Rebind()
    {
        parameters.Clear();

        if (animator == null || animator.runtimeAnimatorController == null)
            return;

        animator.Rebind();
        animator.speed = 1f;

        foreach (AnimatorControllerParameter parameter in animator.parameters)
            parameters.Add(parameter.nameHash);
    }

    private void Update()
    {
        if (parameters.Count == 0)
            return;

        Vector2 velocity = body.Rigidbody.linearVelocity;
        SetFloat(SpeedHash, velocity.magnitude);
        SetFloat(VerticalSpeedHash, body.HeightVelocity);
        SetBool(GroundedHash, !body.Airborne);

        if (reaction != null)
        {
            SetBool(StunnedHash, reaction.IsInHitstun && !reaction.IsKnockedDown);
            SetBool(DownHash, reaction.IsKnockedDown);
        }
    }

    private void OnAttack(AttackData attack)
    {
        if (parameters.Contains(ComboStepHash))
            animator.SetInteger(ComboStepHash, attack != null ? attack.animationId : 0);

        Trigger(AttackHash);
    }

    private void OnDodge() => Trigger(DodgeHash);

    private void OnDamaged(int damage) => Trigger(HurtHash);

    private void OnDied() => SetBool(DeadHash, true);

    private void OnFrozenChanged(bool frozen)
    {
        if (animator != null)
            animator.speed = frozen ? 0f : 1f;
    }

    private void SetFloat(int hash, float value)
    {
        if (parameters.Contains(hash))
            animator.SetFloat(hash, value);
    }

    private void SetBool(int hash, bool value)
    {
        if (parameters.Contains(hash))
            animator.SetBool(hash, value);
    }

    private void Trigger(int hash)
    {
        if (parameters.Contains(hash))
            animator.SetTrigger(hash);
    }
}
