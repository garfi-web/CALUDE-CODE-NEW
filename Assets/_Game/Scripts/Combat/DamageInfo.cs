using UnityEngine;

public enum DamageKind
{
    Melee,
    Ranged,
    Explosion,
}

// Description d'un coup. Health.TakeDamage(int) reste valable : il crée un coup au corps à corps
// sans données d'attaque (pas de recul ni d'effet d'impact).
public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly DamageKind Kind;
    public readonly Vector2 Origin;
    public readonly AttackData Attack;
    public readonly GameObject Attacker;
    // Sens du coup : +1 vers la droite, -1 vers la gauche.
    public readonly float Direction;
    public readonly Vector2 Point;
    // Nature du coup pour les effets (sang, étincelles) : celle de l'attaque, sauf arme imposée.
    public readonly DamageType Type;

    public DamageInfo(int amount, DamageKind kind = DamageKind.Melee, Vector2 origin = default)
        : this(amount, kind, origin, null, null, 0f, origin, DefaultType(kind))
    {
    }

    public DamageInfo(int amount, DamageKind kind, Vector2 origin, AttackData attack, GameObject attacker,
        float direction, Vector2 point, DamageType type)
    {
        Amount = amount;
        Kind = kind;
        Origin = origin;
        Attack = attack;
        Attacker = attacker;
        Direction = direction;
        Point = point;
        Type = type;
    }

    public DamageInfo WithAmount(int amount) => new(amount, Kind, Origin, Attack, Attacker, Direction, Point, Type);

    public static DamageType DefaultType(DamageKind kind) => kind switch
    {
        DamageKind.Ranged => DamageType.Bullet,
        DamageKind.Explosion => DamageType.Explosion,
        _ => DamageType.Blunt,
    };
}

// Composant qui modifie les dégâts reçus avant qu'Health les applique (abri, armure...).
public interface IDamageFilter
{
    DamageInfo Filter(DamageInfo damage);
}
