using UnityEngine;

// Cerveau d'un ennemi. Enemy appelle Tick() à chaque pas physique quand l'ennemi est
// alerté, libre (ni attaque en cours, ni étourdi) et que sa cible est en vie.
// Le comportement choisit un déplacement ou lance une attaque via Enemy.TryAttack.
[RequireComponent(typeof(Enemy))]
public abstract class EnemyBehaviour : MonoBehaviour
{
    protected Enemy Enemy { get; private set; }
    protected EnemyArchetype Archetype => Enemy.Archetype;

    protected virtual void Awake()
    {
        Enemy = GetComponent<Enemy>();
    }

    public abstract void Tick();

    // Lance l'attaque si elle est prête et que la cible est à portée.
    protected bool TryAttackInRange(EnemyAttackKind kind, float minDistance = 0f)
    {
        EnemyAttack attack = Enemy.Pick(kind);

        if (attack == null || Enemy.DistanceToTarget > attack.range || Enemy.DistanceToTarget < minDistance)
            return false;

        return Enemy.TryAttack(attack);
    }

    // Reste entre min et max de la cible ; recule seulement s'il n'est pas dos au mur.
    protected void KeepDistance(float min, float max, float speed)
    {
        float distance = Enemy.DistanceToTarget;
        float away = -Enemy.DirectionToTarget;

        if (distance < min && !Enemy.IsBlocked(away, 0.5f))
        {
            Enemy.Move(away, speed);
            Enemy.FaceTarget();
        }
        else if (distance > max)
        {
            Enemy.Move(Enemy.DirectionToTarget, speed);
        }
        else
        {
            Enemy.Stop();
            Enemy.FaceTarget();
        }
    }

    protected int CountAllies()
    {
        int count = 0;

        foreach (Enemy other in Enemy.Living)
        {
            if (other != Enemy && !other.IsDead)
                count++;
        }

        return count;
    }
}
