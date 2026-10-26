using UnityEngine;

public enum DamageKind
{
    Melee,
    Ranged,
    Explosion,
}

// Description d'un coup. Health.TakeDamage(int) reste valable : il crée un coup au corps à corps.
public readonly struct DamageInfo
{
    public readonly int Amount;
    public readonly DamageKind Kind;
    public readonly Vector2 Origin;

    public DamageInfo(int amount, DamageKind kind = DamageKind.Melee, Vector2 origin = default)
    {
        Amount = amount;
        Kind = kind;
        Origin = origin;
    }

    public DamageInfo WithAmount(int amount) => new(amount, Kind, Origin);
}

// Composant qui modifie les dégâts reçus avant qu'Health les applique (abri, armure...).
public interface IDamageFilter
{
    DamageInfo Filter(DamageInfo damage);
}
