using UnityEngine;

// Stade de Nathan dans le monde de la Red Met (M0 à M4), lu dans l'état de la partie : il suit les
// doses prises (GameSession, ProgressionTable). Change les sprites (Animator), les PV, le multiplicateur
// de dégâts, les morceaux de corps et l'accès au fusil à pompe.
// F4 force M4 ou revient au stade de la partie (debug) ; une vague peut imposer M4 (EnemyWave.nathanM4).
[RequireComponent(typeof(DamageScale))]
public class NathanForm : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private ProgressionTable progression;

    private Health health;
    private PlayerShotgun shotgun;
    private GibSource gibSource;
    private CharacterAnimator bridge;
    private DamageScale damageScale;
    private bool forcedM4;

    public NathanStage Stage { get; private set; }
    public bool IsM4 => Stage == NathanStage.M4;

    private void Awake()
    {
        health = GetComponent<Health>();
        shotgun = GetComponent<PlayerShotgun>();
        gibSource = GetComponent<GibSource>();
        bridge = GetComponent<CharacterAnimator>();
        damageScale = DamageScale.On(gameObject);
    }

    private void OnEnable() => GameSession.Changed += Refresh;

    private void OnDisable() => GameSession.Changed -= Refresh;

    private void Start() => Refresh();

    private void Update()
    {
        if (GameInput.DebugM4Pressed && (health == null || !health.IsDead))
            SetM4(!forcedM4);
    }

    // Test : force M4 (fusil à pompe) ; faux rend le stade de la partie.
    public void SetM4(bool value)
    {
        forcedM4 = value;
        Refresh();
    }

    private void Refresh() => Apply(forcedM4 ? NathanStage.M4 : GameSession.NathanStage);

    public void Apply(NathanStage stage)
    {
        NathanStageData data = progression != null ? progression.Get(stage) : null;

        if (data == null)
            return;

        Stage = stage;
        RuntimeAnimatorController controller = progression.AnimatorFor(data);

        if (animator != null && controller != null && animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;

            if (bridge != null)
                bridge.SetAnimator(animator);
        }

        if (health != null && health.MaxHealth != data.maxHealth)
            health.SetMaxHealth(data.maxHealth);

        damageScale.Multiplier = data.damageMultiplier;

        if (gibSource != null)
            gibSource.Configure(progression.GibsFor(data), true, data.height);

        if (shotgun != null)
            shotgun.SetArmed(data.shotgun);
    }
}
