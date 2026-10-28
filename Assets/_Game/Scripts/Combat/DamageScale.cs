using UnityEngine;

// Multiplicateur des dégâts infligés par ce combattant, appliqué par Combat.Strike : puissance de
// Nathan selon ses doses (NathanForm), force des ennemis selon l'incursion (Enemy).
// La mort instantanée (fusil à bout portant) n'en dépend pas.
public class DamageScale : MonoBehaviour
{
    public float Multiplier { get; set; } = 1f;

    // Le composant du combattant, ajouté s'il manque (prefab ou scène construits avant lui).
    public static DamageScale On(GameObject owner) =>
        owner.TryGetComponent(out DamageScale scale) ? scale : owner.AddComponent<DamageScale>();

    public int Apply(int damage) => damage <= 0 ? damage : Mathf.Max(1, Mathf.RoundToInt(damage * Multiplier));
}
