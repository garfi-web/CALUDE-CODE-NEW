using UnityEngine;

// 04 — Soldat privé. En groupe, le premier soldat tient la position et tire,
// les autres contournent (s'approchent). Le jeton de tir garantit un seul tir à la fois.
// Grenade (8 s au plus) quand Nathan se cache derrière un abri. Change de position
// régulièrement : 0,6 s à découvert, la fenêtre pour le punir.
public class Tactical : EnemyBehaviour
{
    [SerializeField] private float holdMin = 8f;
    [SerializeField] private float holdMax = 10f;
    [SerializeField] private float flankMin = 3.5f;
    [SerializeField] private float flankMax = 5f;
    [SerializeField] private float repositionInterval = 6f;
    [SerializeField] private float repositionTime = 0.6f;
    [SerializeField] private float coverHideDistance = 1.8f;

    private float nextRepositionTime;
    private float repositionUntil;
    private float repositionDirection;

    protected override void Awake()
    {
        base.Awake();
        nextRepositionTime = Time.time + repositionInterval;
    }

    public override void Tick()
    {
        if (Time.time < repositionUntil)
        {
            Enemy.Move(repositionDirection, Archetype.moveSpeed);
            return;
        }

        if (TryAttackInRange(EnemyAttackKind.Melee))
            return;

        if (TargetBehindCover() && TryAttackInRange(EnemyAttackKind.Grenade))
            return;

        bool holder = IsHolder();

        if ((holder || Enemy.DistanceToTarget <= flankMax + 1f) && TryAttackInRange(EnemyAttackKind.Projectile))
            return;

        if (holder && Time.time >= nextRepositionTime)
        {
            nextRepositionTime = Time.time + repositionInterval;
            repositionUntil = Time.time + repositionTime;
            repositionDirection = Random.value < 0.5f ? 1f : -1f;

            if (Enemy.IsBlocked(repositionDirection, 1f))
                repositionDirection = -repositionDirection;

            return;
        }

        if (holder)
            KeepDistance(holdMin, holdMax, Archetype.moveSpeed);
        else
            KeepDistance(flankMin, flankMax, Archetype.moveSpeed);
    }

    // Le premier soldat vivant de la liste tient la position.
    private bool IsHolder()
    {
        foreach (Enemy other in Enemy.Living)
        {
            if (!other.IsDead && other.GetComponent<Tactical>() != null)
                return other == Enemy;
        }

        return true;
    }

    // Nathan est collé à un abri libre, l'abri entre lui et le soldat.
    private bool TargetBehindCover()
    {
        float targetX = Enemy.TargetPoint.x;

        foreach (Cover cover in Cover.All)
        {
            if (cover.Occupant != null)
                continue;

            bool between = Mathf.Sign(cover.X - Enemy.Position.x) == Mathf.Sign(targetX - cover.X);

            if (between && Mathf.Abs(targetX - cover.X) <= coverHideDistance)
                return true;
        }

        return false;
    }
}
