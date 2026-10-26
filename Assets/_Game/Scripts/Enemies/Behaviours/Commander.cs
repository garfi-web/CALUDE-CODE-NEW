using UnityEngine;

// 02 — Dealer : reste à 6-9 unités derrière ses gardes, donne l'ordre « Chopez-le »
// (armement des gardes plus court) et tire quand l'occasion se présente.
// Sans garde, il fuit vers le bord de l'arène ; acculé, il se bat.
public class Commander : EnemyBehaviour
{
    [SerializeField] private float minDistance = 6f;
    [SerializeField] private float maxDistance = 9f;
    [SerializeField] private float minShotDistance = 3f;
    [SerializeField] private float cornerDistance = 1.5f;

    public override void Tick()
    {
        if (TryAttackInRange(EnemyAttackKind.Melee))
            return;

        if (CountAllies() > 0)
        {
            EnemyAttack order = Enemy.Pick(EnemyAttackKind.Order);

            if (order != null && Enemy.TryAttack(order))
                return;

            if (TryAttackInRange(EnemyAttackKind.Projectile, minShotDistance))
                return;

            KeepDistance(minDistance, maxDistance, Archetype.moveSpeed);
            return;
        }

        float away = -Enemy.DirectionToTarget;

        if (!Enemy.IsBlocked(away, cornerDistance))
        {
            Enemy.Move(away, Archetype.altSpeed);
            return;
        }

        // Acculé : il tire, puis vient au contact.
        if (TryAttackInRange(EnemyAttackKind.Projectile))
            return;

        Enemy.Move(Enemy.DirectionToTarget, Archetype.moveSpeed);
    }
}
