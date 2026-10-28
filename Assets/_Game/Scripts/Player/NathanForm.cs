using UnityEngine;

// Stade de Nathan dans l'arène de test : M0 (le faible) ou M4 (le prédateur, sous Red Met, fusil à pompe).
// Change les sprites (Animator), les PV, les morceaux de corps et l'accès au fusil.
// F4 bascule (debug) ; une vague peut imposer M4 (EnemyWave.nathanM4). Le prototype narratif
// remplacera ce réglage par l'état de la partie (doses prises).
public class NathanForm : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private RuntimeAnimatorController m0Controller;
    [SerializeField] private RuntimeAnimatorController m4Controller;
    [SerializeField] private GibSet m0Gibs;
    [SerializeField] private GibSet m4Gibs;
    [SerializeField] private int m0Health = 100;
    [Tooltip("Profil Red Met (docs/ENEMIES.md) : 200 PV.")]
    [SerializeField] private int m4Health = 200;
    [SerializeField] private float m0Height = 1.7f;
    [SerializeField] private float m4Height = 2.05f;

    private Health health;
    private PlayerShotgun shotgun;
    private GibSource gibSource;
    private CharacterAnimator bridge;

    public bool IsM4 { get; private set; }

    private void Awake()
    {
        health = GetComponent<Health>();
        shotgun = GetComponent<PlayerShotgun>();
        gibSource = GetComponent<GibSource>();
        bridge = GetComponent<CharacterAnimator>();
    }

    private void Start() => SetM4(IsM4);

    private void Update()
    {
        if (GameInput.DebugM4Pressed && (health == null || !health.IsDead))
            SetM4(!IsM4);
    }

    public void SetM4(bool value)
    {
        IsM4 = value;
        RuntimeAnimatorController controller = value ? m4Controller : m0Controller;

        if (animator != null && controller != null && animator.runtimeAnimatorController != controller)
        {
            animator.runtimeAnimatorController = controller;

            if (bridge != null)
                bridge.SetAnimator(animator);
        }

        if (health != null)
            health.SetMaxHealth(value ? m4Health : m0Health);

        if (gibSource != null)
            gibSource.Configure(value ? m4Gibs : m0Gibs, true, value ? m4Height : m0Height);

        if (shotgun != null)
            shotgun.SetArmed(value);
    }
}
