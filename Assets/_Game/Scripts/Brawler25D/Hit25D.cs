using System;
using System.Collections.Generic;
using UnityEngine;

// Frappes de l'arène 2,5D. Une AttackData garde son sens : zone devant l'attaquant en X (hitboxOffset.x ± hitboxSize.x / 2),
// à hauteur du corps (centre à 0,80 m + hauteur du saut + hitboxOffset.y ± hitboxSize.y / 2). S'y ajoute la profondeur :
// le coup ne touche que si les deux corps sont sur la même bande (écart ≤ depthTolerance).
// Tout passe par Combat.Strike, donc par Health.TakeDamage, le hitstop, CombatVfx et le gore.
public static class Hit25D
{
    // Le joueur frappe les ennemis et les objets ; les ennemis ne frappent que le joueur.
    public static bool CanHit(Body25D.Team attacker, Body25D.Team target) =>
        attacker == Body25D.Team.Player ? target != Body25D.Team.Player : attacker == Body25D.Team.Enemy && target == Body25D.Team.Player;

    public static int Strike(Body25D attacker, AttackData attack, HashSet<Health> alreadyHit, DamageType? type = null,
        Action<Health> onHit = null)
    {
        if (attacker == null || attack == null)
            return 0;

        float facing = attacker.Facing;
        Vector2 origin = attacker.Ground;
        float near = attack.hitboxOffset.x - attack.hitboxSize.x * 0.5f;
        float far = attack.hitboxOffset.x + attack.hitboxSize.x * 0.5f;
        float center = attacker.Height + Body25D.CenterHeight + attack.hitboxOffset.y;
        float low = center - attack.hitboxSize.y * 0.5f;
        float high = center + attack.hitboxSize.y * 0.5f;
        int hits = 0;

        // À rebours : un coup mortel peut retirer un corps de la liste.
        for (int i = Body25D.All.Count - 1; i >= 0; i--)
        {
            if (i >= Body25D.All.Count)
                continue;

            Body25D target = Body25D.All[i];

            if (target == attacker || !CanHit(attacker.Side, target.Side) || target.Health == null || target.Health.IsDead)
                continue;

            float dx = (target.Ground.x - origin.x) * facing;
            float half = target.BodyWidth * 0.5f;

            if (dx + half < near || dx - half > far)
                continue;

            if (Mathf.Abs(target.Depth - origin.y) > attack.depthTolerance)
                continue;

            if (target.Height + target.BodyHeight < low || target.Height > high)
                continue;

            if (alreadyHit != null && !alreadyHit.Add(target.Health))
                continue;

            float pointHeight = Mathf.Clamp(center, target.Height + 0.2f, target.Height + target.BodyHeight - 0.1f);
            var point = new Vector2(target.Ground.x - facing * half * 0.8f, target.Depth + pointHeight);

            if (!Combat.Strike(attacker.gameObject, target.Health, attack, point, facing, DamageKind.Melee, true, type))
                continue;

            hits++;
            onHit?.Invoke(target.Health);
        }

        return hits;
    }

    // Fusil à pompe en 2,5D : le cône part du canon dans le plan du sol et s'élargit aussi en profondeur
    // (demi-largeur = depthTolerance + distance × tan(coneAngle)). Même règle de distance que le tir latéral
    // (bout portant mortel pour un corps pulvérisable, moyenne distance, plombs dispersés) : Shotgun.StrikeTarget.
    public static Shotgun.Result Fire(Body25D shooter, Vector2 muzzle, AttackData attack)
    {
        var result = new Shotgun.Result();

        if (shooter == null || attack == null || !attack.IsCone)
            return result;

        float facing = shooter.Facing;
        float spread = Mathf.Tan(attack.coneAngle * Mathf.Deg2Rad);

        if (CombatVfx.Instance != null)
            CombatVfx.Instance.ShotgunBlast(muzzle, new Vector2(facing, 0f), attack.coneAngle, attack.maxRange);

        for (int i = Body25D.All.Count - 1; i >= 0; i--)
        {
            if (i >= Body25D.All.Count)
                continue;

            Body25D target = Body25D.All[i];

            if (target == shooter || !CanHit(shooter.Side, target.Side) || target.Health == null || target.Health.IsDead)
                continue;

            float dx = (target.Ground.x - shooter.Ground.x) * facing;
            float dz = target.Depth - shooter.Depth;

            // Tir à hauteur d'homme : un corps projeté très haut passe au-dessus de la gerbe.
            if (dx < -0.2f || target.Height > 2.2f)
                continue;

            if (Mathf.Abs(dz) > attack.depthTolerance + Mathf.Max(0f, dx) * spread)
                continue;

            float distance = Mathf.Sqrt(dx * dx + dz * dz);

            if (distance > attack.maxRange)
                continue;

            var point = new Vector2(target.Ground.x - facing * target.BodyWidth * 0.4f, target.Depth + target.Height + 1.1f);
            Shotgun.StrikeTarget(shooter.gameObject, target.Health, attack, point, distance, facing, ref result);
        }

        return result;
    }

    // Demi-largeur du cône en profondeur à la distance donnée (annonce au sol du tir du Dealer).
    public static float ConeHalfDepth(AttackData attack, float distance) =>
        attack.depthTolerance + Mathf.Max(0f, distance) * Mathf.Tan(attack.coneAngle * Mathf.Deg2Rad);
}
