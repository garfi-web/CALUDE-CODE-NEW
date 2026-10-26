using System;
using System.Collections;
using UnityEngine;

// Esquive (Maj gauche) : courte accélération latérale avec invulnérabilité.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerDodge : MonoBehaviour
{
    [SerializeField] private PlayerController controller;
    [SerializeField] private float dodgeSpeed = 15f;
    [SerializeField] private float dodgeDuration = 0.18f;
    // Couvre l'armement le plus court des ennemis (0,35 s, docs/ENEMIES.md).
    [SerializeField] private float dodgeInvulnerability = 0.4f;
    [SerializeField] private float cooldown = 0.65f;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private float blinkInterval = 0.05f;

    private const string EnemyLayerName = "Enemy";

    private Rigidbody2D rb;
    private Health health;
    private PlayerCombat combat;
    private float nextDodgeTime;

    public bool IsDodging { get; private set; }

    public event Action DodgeStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();
        combat = GetComponent<PlayerCombat>();

        if (controller == null)
            controller = GetComponent<PlayerController>();
    }

    private void OnDisable()
    {
        if (IsDodging)
            SetPassThroughEnemies(false);

        IsDodging = false;

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        bool canAct = controller == null || controller.CanAct;

        if (GameInput.DodgePressed && canAct && !IsDodging && Time.time >= nextDodgeTime)
            StartCoroutine(DodgeRoutine());
    }

    private IEnumerator DodgeRoutine()
    {
        IsDodging = true;
        nextDodgeTime = Time.time + cooldown;

        if (health != null)
            health.GrantInvulnerability(dodgeInvulnerability);

        if (combat != null)
            combat.CancelAttack();

        // Esquive dans la direction pressée, sinon vers l'avant.
        float input = controller != null ? controller.HorizontalInput : 0f;
        float direction = Mathf.Abs(input) > 0.01f
            ? Mathf.Sign(input)
            : (controller == null || controller.FacingRight ? 1f : -1f);

        if (controller != null)
            controller.LockMovement(dodgeDuration);

        // Nathan traverse les ennemis pendant l'esquive (charge du Mutant, pris entre deux Voyous).
        SetPassThroughEnemies(true);
        DodgeStarted?.Invoke();

        float endTime = Time.time + dodgeDuration;
        float nextBlinkTime = 0f;

        while (Time.time < endTime)
        {
            rb.linearVelocity = new Vector2(direction * dodgeSpeed, rb.linearVelocity.y);

            // Le clignotement n'est qu'un indicateur visuel ;
            // l'invulnérabilité réelle est gérée par Health.
            if (spriteRenderer != null && Time.time >= nextBlinkTime)
            {
                spriteRenderer.enabled = !spriteRenderer.enabled;
                nextBlinkTime = Time.time + blinkInterval;
            }

            yield return new WaitForFixedUpdate();
        }

        if (spriteRenderer != null)
            spriteRenderer.enabled = true;

        SetPassThroughEnemies(false);
        IsDodging = false;
    }

    private void SetPassThroughEnemies(bool value)
    {
        int enemyLayer = LayerMask.NameToLayer(EnemyLayerName);

        if (enemyLayer >= 0 && gameObject.layer != 0)
            Physics2D.IgnoreLayerCollision(gameObject.layer, enemyLayer, value);
    }
}
