using System;
using System.Collections;
using UnityEngine;

// IA de test : poursuit le joueur, s'arrête à portée et frappe après un temps
// d'armement visible (l'ennemi change de couleur), ce qui laisse le temps d'esquiver.
[RequireComponent(typeof(Rigidbody2D))]
public class EnemyAI : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackCooldown = 1.2f;
    [SerializeField] private int attackDamage = 10;
    [SerializeField] private float attackWindup = 0.35f;
    [SerializeField] private float attackRecovery = 0.2f;
    [SerializeField] private float attackKnockback = 5f;
    [SerializeField] private float hitStunTime = 0.3f;

    [Header("Feedback")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color windupColor = new(1f, 0.85f, 0.2f);
    [SerializeField] private Color deadColor = new(0.25f, 0.2f, 0.2f);

    private Rigidbody2D rb;
    private Health health;
    private Health targetHealth;
    private Rigidbody2D targetRb;
    private Coroutine attackRoutine;
    private Color baseColor = Color.white;
    private float nextAttackTime;
    private float stunnedUntil;
    private bool attacking;

    public bool IsAttacking => attacking;

    public event Action AttackStarted;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        health = GetComponent<Health>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        if (health != null)
        {
            health.Damaged += OnDamaged;
            health.Died += OnDied;
        }
    }

    private void Start()
    {
        if (target == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");

            if (player != null)
                target = player.transform;
        }

        if (target != null)
        {
            targetHealth = target.GetComponent<Health>();
            targetRb = target.GetComponent<Rigidbody2D>();
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

    private void FixedUpdate()
    {
        // Pendant l'étourdissement, on laisse le recul s'appliquer.
        if (Time.time < stunnedUntil)
            return;

        if (target == null || attacking || (targetHealth != null && targetHealth.IsDead))
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
            return;
        }

        float deltaX = target.position.x - transform.position.x;
        float distance = Mathf.Abs(deltaX);

        if (distance > attackRange)
        {
            rb.linearVelocity = new Vector2(Mathf.Sign(deltaX) * moveSpeed, rb.linearVelocity.y);
        }
        else
        {
            rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);

            if (Time.time >= nextAttackTime)
                attackRoutine = StartCoroutine(AttackRoutine());
        }

        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * (deltaX >= 0f ? 1f : -1f);
        transform.localScale = scale;
    }

    private IEnumerator AttackRoutine()
    {
        attacking = true;
        nextAttackTime = Time.time + attackCooldown;
        AttackStarted?.Invoke();

        SetColor(windupColor);
        yield return new WaitForSeconds(attackWindup);
        SetColor(baseColor);

        if (target != null && targetHealth != null &&
            Mathf.Abs(target.position.x - transform.position.x) <= attackRange + 0.3f &&
            targetHealth.TakeDamage(attackDamage) && targetRb != null && !targetHealth.IsDead)
        {
            float direction = Mathf.Sign(target.position.x - transform.position.x);
            targetRb.AddForce(new Vector2(direction * attackKnockback, 2f), ForceMode2D.Impulse);
        }

        yield return new WaitForSeconds(attackRecovery);

        attacking = false;
        attackRoutine = null;
    }

    private void OnDamaged(int damage)
    {
        stunnedUntil = Time.time + hitStunTime;

        // Un coup reçu interrompt l'attaque en préparation.
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
            attacking = false;
            SetColor(baseColor);
        }
    }

    private void OnDied()
    {
        StopAllCoroutines();
        attacking = false;
        rb.linearVelocity = Vector2.zero;
        SetColor(deadColor);
        enabled = false;
    }

    private void SetColor(Color color)
    {
        if (spriteRenderer != null)
            spriteRenderer.color = color;
    }
}
