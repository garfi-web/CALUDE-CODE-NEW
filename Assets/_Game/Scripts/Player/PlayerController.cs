using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.1f;
    [Tooltip("Mémoire du saut, en images de jeu (gelée pendant le hitstop).")]
    [SerializeField] private int jumpBufferFrames = 6;

    [Header("Ground detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    private Rigidbody2D rb;
    private Health health;
    private HitReaction reaction;
    private HitStop hitStop;
    private float horizontal;
    private float lastGroundedTime = float.NegativeInfinity;
    private int jumpBufferLeft;
    private float movementLockedUntil;
    private bool facingRight = true;

    public bool IsGrounded { get; private set; }
    public bool FacingRight => facingRight;
    public float Facing => facingRight ? 1f : -1f;
    public float HorizontalInput => horizontal;

    // Pendant un verrou (esquive, dash, attaque), le contrôleur ne réécrit pas la
    // vitesse horizontale, sinon il annulerait l'impulsion dès la frame suivante.
    public bool IsMovementLocked => Time.time < movementLockedUntil;
    public bool IsFrozen => hitStop != null && hitStop.IsFrozen;
    public bool IsInHitstun => reaction != null && reaction.IsInHitstun;

    // Nathan peut lancer une action (attaque, dash) : ni figé ni étourdi.
    public bool CanAct => !IsFrozen && !IsInHitstun && (health == null || !health.IsDead);

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        reaction = GetComponent<HitReaction>();
        hitStop = GetComponent<HitStop>();

        if (health != null)
            health.Died += OnDied;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Died -= OnDied;
    }

    private void Update()
    {
        horizontal = GameInput.Horizontal;

        if (GameInput.JumpPressed)
            jumpBufferLeft = jumpBufferFrames;

        if (groundCheck != null)
        {
            IsGrounded = Physics2D.OverlapCircle(
                groundCheck.position,
                groundRadius,
                groundLayer
            ) != null;
        }

        if (IsGrounded)
            lastGroundedTime = Time.time;

        if (IsMovementLocked || !CanAct)
            return;

        if (horizontal > 0.01f)
            SetFacing(true);
        else if (horizontal < -0.01f)
            SetFacing(false);
    }

    private void FixedUpdate()
    {
        if (!IsFrozen && jumpBufferLeft > 0)
            jumpBufferLeft--;

        // Figé (hitstop) ou étourdi : le recul et la glissade s'appliquent seuls.
        if (IsMovementLocked || !CanAct)
            return;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = horizontal * moveSpeed;

        if (TryConsumeJump())
            velocity.y = jumpForce;

        rb.linearVelocity = velocity;
    }

    // Saut avec coyote time et mémoire de la commande. Aussi appelé pour annuler un lanceur.
    public bool TryConsumeJump()
    {
        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        if (!canJump || jumpBufferLeft <= 0)
            return false;

        jumpBufferLeft = 0;
        lastGroundedTime = float.NegativeInfinity;
        return true;
    }

    public void Jump()
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, jumpForce);
        lastGroundedTime = float.NegativeInfinity;
    }

    public void LockMovement(float duration)
    {
        movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + duration);
    }

    public void UnlockMovement() => movementLockedUntil = 0f;

    public void SetFacing(bool right)
    {
        facingRight = right;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (right ? 1f : -1f);
        transform.localScale = scale;
    }

    private void OnDied()
    {
        enabled = false;
    }

    private void OnDrawGizmosSelected()
    {
        if (groundCheck == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
