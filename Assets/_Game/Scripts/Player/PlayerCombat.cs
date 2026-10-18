using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Combo à trois coups. Appuyer sur J pendant un coup prépare le suivant.
public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController controller;
    [SerializeField] private Hitbox attackHitbox;

    [Header("Combo")]
    [SerializeField] private int[] damages = { 12, 16, 25 };
    [SerializeField] private float[] activeTimes = { 0.12f, 0.15f, 0.22f };
    [SerializeField] private float[] recoveryTimes = { 0.22f, 0.25f, 0.45f };
    [SerializeField] private float[] knockbackForces = { 3f, 3f, 7f };
    [SerializeField] private float knockbackLift = 1.5f;
    [SerializeField] private float comboResetTime = 0.8f;

    private readonly HashSet<Health> hitTargets = new();
    private Health health;
    private Coroutine attackRoutine;
    private bool attacking;
    private bool bufferedAttack;
    private int comboStep;
    private float lastAttackEndTime = float.NegativeInfinity;

    public bool IsAttacking => attacking;
    public int CurrentStep { get; private set; }

    // Paramètre : index du coup dans le combo (0, 1, 2).
    public event Action<int> AttackStarted;

    private void Awake()
    {
        if (controller == null)
            controller = GetComponent<PlayerController>();

        health = GetComponent<Health>();

        if (attackHitbox != null)
        {
            attackHitbox.SetActive(false);
            attackHitbox.Hit += OnHitboxHit;
        }
    }

    private void OnDestroy()
    {
        if (attackHitbox != null)
            attackHitbox.Hit -= OnHitboxHit;
    }

    private void OnDisable()
    {
        CancelAttack();
    }

    private void Update()
    {
        if (health != null && health.IsDead)
            return;

        if (!GameInput.AttackPressed)
            return;

        if (attacking)
        {
            bufferedAttack = true;
            return;
        }

        if (Time.time - lastAttackEndTime > comboResetTime)
            comboStep = 0;

        attackRoutine = StartCoroutine(AttackSequence());
    }

    private IEnumerator AttackSequence()
    {
        if (attackHitbox == null)
            yield break;

        attacking = true;

        do
        {
            bufferedAttack = false;
            CurrentStep = comboStep;
            hitTargets.Clear();
            AttackStarted?.Invoke(CurrentStep);

            // La hitbox est un enfant : elle suit déjà le retournement du parent.
            attackHitbox.SetActive(true);
            yield return new WaitForSeconds(Get(activeTimes, CurrentStep));
            attackHitbox.SetActive(false);

            yield return new WaitForSeconds(Get(recoveryTimes, CurrentStep));

            comboStep = (comboStep + 1) % damages.Length;
        }
        while (bufferedAttack && comboStep != 0);

        lastAttackEndTime = Time.time;
        attacking = false;
        attackRoutine = null;
    }

    // Interrompt le coup en cours, par exemple quand le joueur esquive.
    public void CancelAttack()
    {
        if (attackRoutine != null)
        {
            StopCoroutine(attackRoutine);
            attackRoutine = null;
        }

        if (attackHitbox != null)
            attackHitbox.SetActive(false);

        if (attacking)
        {
            attacking = false;
            bufferedAttack = false;
            lastAttackEndTime = Time.time;
        }
    }

    private void OnHitboxHit(Collider2D other)
    {
        if (!attacking)
            return;

        Health target = other.GetComponentInParent<Health>();

        if (target == null || target == health || !hitTargets.Add(target))
            return;

        if (!target.TakeDamage(Get(damages, CurrentStep)))
            return;

        Rigidbody2D targetRb = other.attachedRigidbody;

        if (targetRb != null)
        {
            float direction = Mathf.Sign(target.transform.position.x - transform.position.x);

            targetRb.AddForce(
                new Vector2(direction * Get(knockbackForces, CurrentStep), knockbackLift),
                ForceMode2D.Impulse
            );
        }
    }

    private static T Get<T>(T[] values, int index) =>
        values[Mathf.Clamp(index, 0, values.Length - 1)];
}
