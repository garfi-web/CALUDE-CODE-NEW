using UnityEngine;

// 03 — Flic corrompu : gagne l'abri le plus proche qui le sépare de Nathan et tire depuis là.
// Recharge après 6 tirs (1,5 s). Matraque si Nathan est à moins de 2 unités.
// Appelle des renforts une fois par combat, quand il passe sous la moitié de ses PV.
public class CoverShooter : EnemyBehaviour
{
    [SerializeField] private int magazine = 6;
    [SerializeField] private float reloadTime = 1.5f;
    [SerializeField] private Color reloadColor = new(0.4f, 0.45f, 0.55f);
    [SerializeField] private float meleeDistance = 2f;
    [SerializeField] private float radioBelowHealth = 0.5f;
    [SerializeField] private float coverOffset = 0.9f;
    [SerializeField] private float minCoverGap = 2f;

    private int shotsLeft;

    protected override void Awake()
    {
        base.Awake();
        shotsLeft = magazine;
    }

    public override void Tick()
    {
        if (Enemy.DistanceToTarget < meleeDistance)
        {
            Enemy.LeaveCover();

            if (!TryAttackInRange(EnemyAttackKind.Melee))
                Enemy.Move(Enemy.DirectionToTarget, Archetype.moveSpeed);

            return;
        }

        EnemyAttack radio = Enemy.Pick(EnemyAttackKind.Summon);

        if (radio != null && Enemy.HealthRatio <= radioBelowHealth && Enemy.TryAttack(radio))
            return;

        if (shotsLeft <= 0)
        {
            shotsLeft = magazine;
            Enemy.Hold(reloadTime, reloadColor);
            return;
        }

        Cover cover = FindCover(out float spot);

        if (cover != null && !Enemy.MoveTo(spot, Archetype.moveSpeed, 0.25f))
        {
            Enemy.LeaveCover();
            return;
        }

        Enemy.TakeCover(cover);
        Enemy.FaceTarget();

        EnemyAttack shot = Enemy.Pick(EnemyAttackKind.Projectile);

        if (shot != null && Enemy.DistanceToTarget <= shot.range)
        {
            if (Enemy.TryAttack(shot))
                shotsLeft -= Mathf.Max(1, shot.projectileCount);

            return;
        }

        // Sans abri, ou Nathan hors de portée : il s'approche pour tirer.
        if (cover == null && shot != null)
            Enemy.Move(Enemy.DirectionToTarget, Archetype.moveSpeed);
    }

    // Le poste de tir est du côté de l'abri opposé à Nathan.
    private Cover FindCover(out float spot)
    {
        Cover best = null;
        float bestDistance = float.MaxValue;
        float targetX = Enemy.TargetPoint.x;
        float range = Enemy.Find(EnemyAttackKind.Projectile)?.range ?? 9f;
        spot = 0f;

        foreach (Cover cover in Cover.All)
        {
            if (cover.Occupant != null && cover.Occupant != Enemy)
                continue;

            float gap = Mathf.Abs(targetX - cover.X);

            if (gap < minCoverGap)
                continue;

            float candidate = cover.X - Mathf.Sign(targetX - cover.X) * coverOffset;

            if (Mathf.Abs(targetX - candidate) > range)
                continue;

            float distance = Mathf.Abs(candidate - Enemy.Position.x);

            if (distance < bestDistance)
            {
                best = cover;
                bestDistance = distance;
                spot = candidate;
            }
        }

        return best;
    }
}
