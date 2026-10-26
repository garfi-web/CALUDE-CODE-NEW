using UnityEngine;

// 01 — Voyou : fonce au contact et frappe. Sans jeton d'attaque (2 attaquants au plus),
// il tourne autour de Nathan à distance, prêt à prendre la place libre.
public class MeleeAggressive : EnemyBehaviour
{
    [SerializeField] private float circleDistance = 2.5f;
    [SerializeField] private float circleSway = 0.6f;
    [SerializeField] private float circleSpeedFactor = 0.6f;

    private float swayPhase;

    protected override void Awake()
    {
        base.Awake();
        swayPhase = Random.value * 10f;
    }

    public override void Tick()
    {
        EnemyAttack attack = Enemy.Pick(EnemyAttackKind.Melee);

        if (attack != null)
        {
            if (Enemy.DistanceToTarget <= attack.range)
            {
                Enemy.TryAttack(attack);
                return;
            }

            Enemy.Move(Enemy.DirectionToTarget, Archetype.moveSpeed);
            return;
        }

        Circle();
    }

    // Garde ses distances en oscillant, sans quitter Nathan des yeux.
    protected void Circle()
    {
        float sway = Mathf.Sin(Time.time * 1.5f + swayPhase) * circleSway;
        float x = Enemy.TargetPoint.x - Enemy.DirectionToTarget * (circleDistance + sway);
        Enemy.MoveTo(x, Archetype.moveSpeed * circleSpeedFactor);
        Enemy.FaceTarget();
    }
}
