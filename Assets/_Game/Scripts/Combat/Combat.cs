using UnityEngine;

// Point d'entrée commun des coups portés avec des AttackData (Nathan, ennemis, projectiles).
public static class Combat
{
    // Applique le coup à la cible. En cas de succès, l'attaquant se fige aussi (hitstop partagé).
    // Les réactions de la cible et les effets passent par Health.Hit / Health.AnyHit.
    public static bool Strike(GameObject attacker, Health target, AttackData attack, Vector2 point,
        float direction, DamageKind kind = DamageKind.Melee, bool freezeAttacker = true, DamageType? typeOverride = null)
    {
        if (target == null || attack == null)
            return false;

        Vector2 origin = attacker != null ? (Vector2)attacker.transform.position : point;
        DamageType type = typeOverride ?? attack.damageType;
        var info = new DamageInfo(attack.damage, kind, origin, attack, attacker, direction, point, type);

        if (!target.TakeDamage(info))
            return false;

        if (freezeAttacker && attacker != null && attacker.TryGetComponent(out HitStop hitStop))
            hitStop.Freeze(attack.hitstop);

        return true;
    }
}
