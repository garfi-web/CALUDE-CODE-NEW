using System;
using UnityEngine;

// Nathan dans l'arène 2,5D : marche dans le plan du sol (gauche/droite et profondeur), saut sur l'axe de hauteur,
// esquive dans les 4 directions (pas de côté vers le fond ou vers l'avant). L'esquive dure et se compte en images
// de jeu (gelées pendant le hitstop), traverse les ennemis et rend invulnérable via Health.GrantInvulnerability.
// Les attaques sont dans BrawlerCombat25D.
[RequireComponent(typeof(Body25D), typeof(Health))]
public class Brawler25D : MonoBehaviour
{
    [Header("Déplacement")]
    [SerializeField] private float moveSpeed = 5.5f;
    [Tooltip("Plus lent en profondeur, comme dans le genre : la profondeur se lit moins bien que la largeur.")]
    [SerializeField] private float depthSpeed = 3.6f;
    [SerializeField] private float jumpSpeed = 13f;
    [SerializeField] private int jumpBufferFrames = 6;

    [Header("Esquive")]
    [SerializeField] private float dodgeSpeed = 13f;
    [SerializeField] private float dodgeDepthSpeed = 8f;
    [SerializeField] private int dodgeFrames = 11;
    // Couvre l'armement le plus court des ennemis (0,35 s, docs/ENEMIES.md).
    [SerializeField] private float dodgeInvulnerability = 0.4f;
    [SerializeField] private float dodgeCooldown = 0.55f;
    [SerializeField] private SpriteRenderer spriteRenderer;

    private Body25D body;
    private Rigidbody2D rb;
    private Health health;
    private HitReaction25D reaction;
    private BrawlerCombat25D combat;
    private Vector2 input;
    private int jumpBufferLeft;
    private int dodgeLeft;
    private Vector2 dodgeVelocity;
    private float nextDodgeTime;
    private float movementLockedUntil;

    public Body25D Body => body;
    public Vector2 Input => input;
    public bool IsDodging => dodgeLeft > 0;
    public bool IsMovementLocked => Time.time < movementLockedUntil;
    public bool IsFrozen => body.IsFrozen;
    public bool IsInHitstun => reaction != null && reaction.IsInHitstun;
    public bool CanAct => !IsFrozen && !IsInHitstun && !health.IsDead;
    public bool Grounded => !body.Airborne;

    public event Action DodgeStarted;
    public event Action Jumped;

    private void Awake()
    {
        body = GetComponent<Body25D>();
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction25D>();
        combat = GetComponent<BrawlerCombat25D>();
    }

    private void OnDisable()
    {
        dodgeLeft = 0;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    public void LockMovement(float duration) => movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + duration);

    public void UnlockMovement() => movementLockedUntil = 0f;

    public bool TryConsumeJump()
    {
        if (jumpBufferLeft <= 0 || body.Airborne)
            return false;

        jumpBufferLeft = 0;
        Jump();
        return true;
    }

    public void Jump()
    {
        body.HeightVelocity = jumpSpeed;
        Jumped?.Invoke();
    }

    private void Update()
    {
        if (health.IsDead)
            return;

        input = new Vector2(GameInput.Horizontal, GameInput.Vertical);

        if (GameInput.JumpPressed)
            jumpBufferLeft = jumpBufferFrames;

        if (GameInput.DodgePressed && CanAct && !IsDodging && !body.Airborne && Time.time >= nextDodgeTime)
            StartDodge();
    }

    private void StartDodge()
    {
        nextDodgeTime = Time.time + dodgeCooldown;
        health.GrantInvulnerability(dodgeInvulnerability);

        if (combat != null)
            combat.CancelAttack();

        // Direction pressée, sinon vers l'avant. Vers le haut ou le bas seul : pas de côté.
        Vector2 direction = input.sqrMagnitude > 0.01f ? input.normalized : new Vector2(body.Facing, 0f);
        dodgeVelocity = new Vector2(direction.x * dodgeSpeed, direction.y * dodgeDepthSpeed);
        dodgeLeft = dodgeFrames;
        LockMovement(dodgeFrames / AttackData.FrameRate);

        if (Mathf.Abs(direction.x) > 0.3f)
            body.SetFacing(direction.x);

        DodgeStarted?.Invoke();
    }

    private void FixedUpdate()
    {
        if (health.IsDead || IsFrozen)
            return;

        if (jumpBufferLeft > 0)
            jumpBufferLeft--;

        if (IsDodging)
        {
            rb.linearVelocity = dodgeVelocity;

            // Clignotement : simple indicateur, l'invulnérabilité est dans Health.
            if (spriteRenderer != null)
                spriteRenderer.enabled = --dodgeLeft <= 0 || dodgeLeft % 4 >= 2;

            if (dodgeLeft <= 0)
                rb.linearVelocity = Vector2.zero;

            return;
        }

        // Étourdi ou attaque en cours : la réaction ou l'attaque règlent la vitesse.
        if (IsMovementLocked || !CanAct)
            return;

        rb.linearVelocity = new Vector2(input.x * moveSpeed, input.y * depthSpeed);

        if (input.x > 0.01f)
            body.SetFacing(1f);
        else if (input.x < -0.01f)
            body.SetFacing(-1f);

        TryConsumeJump();
    }
}
