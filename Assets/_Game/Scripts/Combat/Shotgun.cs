using System.Collections.Generic;
using UnityEngine;

// Tir en cône du fusil à pompe, commun au Dealer et à Nathan M4 (AttackData.pellets > 0).
// Chaque cible dans le cône encaisse un seul coup selon sa distance au canon :
// bout portant (closeRange) : mort instantanée si l'attaque le permet et que la cible peut être pulvérisée ;
// moyenne distance (midRange) : le coup de l'attaque (gros dégâts, projection) ;
// au-delà (maxRange) : farHit, les plombs dispersés (dégâts faibles).
// Tout passe par Combat.Strike, donc par Health.TakeDamage : l'esquive (invulnérabilité) protège.
public static class Shotgun
{
    private static readonly Collider2D[] Overlaps = new Collider2D[32];
    private static readonly HashSet<Health> Hit = new();

    public struct Result
    {
        public int Hits;
        public int Kills;
    }

    public static Result Fire(GameObject shooter, Vector2 muzzle, Vector2 aim, AttackData attack, LayerMask targets)
    {
        var result = new Result();

        if (attack == null || !attack.IsCone)
            return result;

        aim = aim.sqrMagnitude > 0.0001f ? aim.normalized : Vector2.right;
        float facing = aim.x >= 0f ? 1f : -1f;
        Health self = shooter != null ? shooter.GetComponent<Health>() : null;
        var filter = new ContactFilter2D { useTriggers = false, useLayerMask = true, layerMask = targets };
        int count = Physics2D.OverlapCircle(muzzle, attack.maxRange, filter, Overlaps);
        Hit.Clear();

        if (CombatVfx.Instance != null)
            CombatVfx.Instance.ShotgunBlast(muzzle, aim, attack.coneAngle, attack.maxRange);

        for (int i = 0; i < count; i++)
        {
            Health target = Overlaps[i].GetComponentInParent<Health>();

            if (target == null || target == self || target.IsDead || !Hit.Add(target))
                continue;

            Vector2 point = Overlaps[i].ClosestPoint(muzzle);
            Vector2 toTarget = (Vector2)Overlaps[i].bounds.center - muzzle;
            float distance = Vector2.Distance(muzzle, point);

            // À bout portant, tout ce qui est devant le canon est touché ; plus loin, il faut être dans le cône.
            bool inFront = Mathf.Sign(toTarget.x) == facing || distance < 0.3f;
            bool inCone = Vector2.Angle(aim, toTarget) <= attack.coneAngle;

            if (!inFront || (distance > attack.closeRange && !inCone))
                continue;

            StrikeTarget(shooter, target, attack, point, distance, facing, ref result);
        }

        return result;
    }

    // Coup d'une cible dans le cône selon sa distance au canon (commun au tir latéral et au tir en 2,5D).
    public static bool StrikeTarget(GameObject shooter, Health target, AttackData attack, Vector2 point, float distance,
        float facing, ref Result result)
    {
        AttackData hit = distance > attack.midRange && attack.farHit != null ? attack.farHit : attack;
        bool lethal = distance <= attack.closeRange && attack.lethalAtCloseRange && CanBeGibbed(target);

        if (!Combat.Strike(shooter, target, hit, point, facing, DamageKind.Ranged, true, DamageType.Bullet, lethal))
            return false;

        result.Hits++;

        if (target.IsDead)
            result.Kills++;

        return true;
    }

    // Seuls les humains de l'Acte I (et Nathan) sont pulvérisés : les surnaturels et les boss encaissent.
    public static bool CanBeGibbed(Health target) =>
        target.TryGetComponent(out GibSource source) && source.Gibbable;
}
