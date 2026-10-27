using UnityEngine;

// Mannequin d'entraînement : ne riposte jamais, encaisse tout (projections, rebonds) et
// remonte à pleine vie quand on le laisse tranquille.
public class TrainingDummy : EnemyBehaviour
{
    [SerializeField] private float healDelay = 2f;

    private float lastHitTime = float.NegativeInfinity;

    private void OnEnable()
    {
        if (Enemy != null && Enemy.Health != null)
            Enemy.Health.Damaged += OnDamaged;

        // Chaque coup le fait osciller sur son socle (HitReaction, visuel seulement).
        if (TryGetComponent(out HitReaction reaction))
            reaction.SetSway(true);
    }

    private void OnDisable()
    {
        if (Enemy != null && Enemy.Health != null)
            Enemy.Health.Damaged -= OnDamaged;

        if (TryGetComponent(out HitReaction reaction))
            reaction.SetSway(false);
    }

    private void OnDamaged(int damage) => lastHitTime = Time.time;

    private void Update()
    {
        Health health = Enemy.Health;

        if (!health.IsDead && health.CurrentHealth < health.MaxHealth && Time.time - lastHitTime > healDelay)
            health.Heal(health.MaxHealth);
    }

    public override void Tick()
    {
        Enemy.Stop();
        Enemy.FaceTarget();
    }
}
