using System;
using UnityEngine;

// Points de vie communs au joueur et aux ennemis.
public class Health : MonoBehaviour
{
    [SerializeField] private int maxHealth = 100;
    [SerializeField] private float invulnerabilityTime = 0.15f;

    public int CurrentHealth { get; private set; }
    public int MaxHealth => maxHealth;
    public bool IsDead => CurrentHealth <= 0;
    public bool IsInvulnerable => Time.time < invulnerableUntil;

    public event Action<int, int> HealthChanged;
    public event Action<int> Damaged;
    public event Action Died;

    private float invulnerableUntil;

    private void Awake()
    {
        CurrentHealth = maxHealth;
    }

    public bool TakeDamage(int damage)
    {
        if (IsDead || IsInvulnerable || damage <= 0)
            return false;

        CurrentHealth = Mathf.Max(0, CurrentHealth - damage);
        GrantInvulnerability(invulnerabilityTime);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
        Damaged?.Invoke(damage);

        if (IsDead)
            Died?.Invoke();

        return true;
    }

    public void GrantInvulnerability(float duration)
    {
        invulnerableUntil = Mathf.Max(
            invulnerableUntil,
            Time.time + Mathf.Max(0f, duration)
        );
    }

    public void Heal(int amount)
    {
        if (IsDead || amount <= 0)
            return;

        CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(CurrentHealth, maxHealth);
    }
}
