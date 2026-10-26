// 06 — Mutant : ennemi lourd. Balayage au contact, frappe au sol à moyenne portée,
// charge de loin. Super-armure pendant l'armement et charge qui le sonne contre un mur :
// ces règles sont dans Enemy et dans son archétype.
public class HeavyCharger : EnemyBehaviour
{
    [UnityEngine.SerializeField] private float minChargeDistance = 4f;

    public override void Tick()
    {
        if (TryAttackInRange(EnemyAttackKind.Melee))
            return;

        if (TryAttackInRange(EnemyAttackKind.Shockwave))
            return;

        if (TryAttackInRange(EnemyAttackKind.Charge, minChargeDistance))
            return;

        Enemy.Move(Enemy.DirectionToTarget, Archetype.moveSpeed);
    }
}
