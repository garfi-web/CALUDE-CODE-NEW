using System;
using UnityEngine;

// Dash rapide au sol et en l'air (un seul dash aérien par saut) : touche L, LB à la manette,
// ou double appui sur une direction. Pas d'invulnérabilité (c'est le rôle de l'esquive).
// Une attaque lancée pendant le dash l'annule en gardant une partie de l'élan.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDash : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private float groundSpeed = 17f;
    [SerializeField] private int groundFrames = 10;
    [SerializeField] private float airSpeed = 15f;
    [SerializeField] private int airFrames = 9;
    [SerializeField] private int cooldownFrames = 8;
    [SerializeField] private int inputBufferFrames = 6;
    [SerializeField] private float doubleTapWindow = 0.22f;
    [SerializeField] private float momentumKeptOnAttack = 0.5f;

    private Rigidbody2D rb;
    private Health health;
    private PlayerCombat combat;
    private PlayerDodge dodge;
    private int framesLeft;
    private int cooldownLeft;
    private float direction;
    private bool airDashUsed;
    private bool airDash;
    private float requestTime = float.NegativeInfinity;
    private float requestDirection;
    private float lastTapTime = float.NegativeInfinity;
    private float lastTapDirection;
    private float previousHorizontal;

    public bool IsDashing => framesLeft > 0;

    public event Action DashStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        combat = GetComponent<PlayerCombat>();
        dodge = GetComponent<PlayerDodge>();

        if (controller == null)
            controller = GetComponent<PlayerController>();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        float horizontal = GameInput.Horizontal;

        // Double appui : deux pressions dans la même direction, rapprochées.
        if (Mathf.Abs(horizontal) > 0.5f && Mathf.Abs(previousHorizontal) < 0.5f)
        {
            float tap = Mathf.Sign(horizontal);

            if (Time.time - lastTapTime <= doubleTapWindow && Mathf.Approximately(tap, lastTapDirection))
                Request(tap);

            lastTapTime = Time.time;
            lastTapDirection = tap;
        }

        previousHorizontal = horizontal;

        if (GameInput.DashPressed)
            Request(Mathf.Abs(horizontal) > 0.01f ? Mathf.Sign(horizontal) : 0f);
    }

    private void Request(float dir)
    {
        requestTime = Time.time;
        requestDirection = dir;
    }

    private void FixedUpdate()
    {
        if (controller.IsFrozen)
            return;

        if (controller.IsGrounded && !IsDashing)
            airDashUsed = false;

        if (cooldownLeft > 0)
            cooldownLeft--;

        if (IsDashing)
        {
            if (controller.IsInHitstun)
            {
                framesLeft = 0;
                return;
            }

            framesLeft--;
            float speed = airDash ? airSpeed : groundSpeed;
            rb.linearVelocity = new Vector2(direction * speed, airDash ? 0f : rb.linearVelocity.y);
            controller.LockMovement(2f / AttackData.FrameRate);
            return;
        }

        bool buffered = Time.time - requestTime <= inputBufferFrames / AttackData.FrameRate;

        if (!buffered || cooldownLeft > 0 || !controller.CanAct)
            return;

        if ((combat != null && combat.IsAttacking) || (dodge != null && dodge.IsDodging))
            return;

        if (!controller.IsGrounded && airDashUsed)
            return;

        requestTime = float.NegativeInfinity;
        StartDash(Mathf.Abs(requestDirection) > 0.01f ? requestDirection : controller.Facing);
    }

    private void StartDash(float dir)
    {
        direction = dir;
        airDash = !controller.IsGrounded;
        airDashUsed |= airDash;
        framesLeft = airDash ? airFrames : groundFrames;
        cooldownLeft = cooldownFrames + framesLeft;
        controller.SetFacing(dir > 0f);
        controller.LockMovement(framesLeft / AttackData.FrameRate);
        DashStarted?.Invoke();
    }

    public void CancelIntoAttack()
    {
        framesLeft = 0;
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * momentumKeptOnAttack, rb.linearVelocity.y);
    }
}
