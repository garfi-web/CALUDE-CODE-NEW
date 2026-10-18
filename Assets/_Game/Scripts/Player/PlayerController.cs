using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 7f;
    [SerializeField] private float jumpForce = 14f;
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground detection")]
    [SerializeField] private Transform groundCheck;
    [SerializeField] private float groundRadius = 0.15f;
    [SerializeField] private LayerMask groundLayer;

    [Header("Hit reaction")]
    [SerializeField] private float hitStunTime = 0.2f;

    private Rigidbody2D rb;
    private Health health;
    private float horizontal;
    private float lastGroundedTime = float.NegativeInfinity;
    private float lastJumpPressedTime = float.NegativeInfinity;
    private float movementLockedUntil;
    private bool facingRight = true;

    public bool IsGrounded { get; private set; }
    public bool FacingRight => facingRight;
    public float HorizontalInput => horizontal;

    // Pendant un verrou (esquive, recul), le contrôleur ne réécrit pas la
    // vitesse horizontale, sinon il annulerait l'impulsion dès la frame suivante.
    public bool IsMovementLocked => Time.time < movementLockedUntil;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    private void OnDestroy()
    {
        if (health != null)
        {
            health.Damaged -= OnDamaged;
            health.Died -= OnDied;
        }
    }

    private void Update()
    {
        horizontal = GameInput.Horizontal;

        if (GameInput.JumpPressed)
            lastJumpPressedTime = Time.time;

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

        if (IsMovementLocked)
            return;

        if (horizontal > 0.01f)
            SetFacing(true);
        else if (horizontal < -0.01f)
            SetFacing(false);
    }

    private void FixedUpdate()
    {
        if (IsMovementLocked)
            return;

        Vector2 velocity = rb.linearVelocity;
        velocity.x = horizontal * moveSpeed;

        bool canJump = Time.time - lastGroundedTime <= coyoteTime;
        bool wantsJump = Time.time - lastJumpPressedTime <= jumpBufferTime;

        if (canJump && wantsJump)
        {
            velocity.y = jumpForce;
            lastJumpPressedTime = float.NegativeInfinity;
            lastGroundedTime = float.NegativeInfinity;
        }

        rb.linearVelocity = velocity;
    }

    public void LockMovement(float duration)
    {
        movementLockedUntil = Mathf.Max(movementLockedUntil, Time.time + duration);
    }

    public void SetFacing(bool right)
    {
        facingRight = right;

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (right ? 1f : -1f);
        transform.localScale = scale;
    }

    private void OnDamaged(int damage)
    {
        LockMovement(hitStunTime);
    }

    private void OnDied()
    {
        rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
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
