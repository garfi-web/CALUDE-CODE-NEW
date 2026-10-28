using System;
using UnityEngine;

// Force des ennemis à une incursion, appliquée par-dessus leur archétype.
[Serializable]
public class IncursionScaling
{
    public static readonly IncursionScaling None = new();

    public float health = 1f;
    public float damage = 1f;
    [Tooltip("Déplacement et charges. L'armement ne change jamais (0,35 s minimum).")]
    public float speed = 1f;
}

// Les ennemis montent en puissance à chaque incursion, quelle que soit la trajectoire de Nathan
// (docs/GDD.md, « Difficulté »). Au-delà de la dernière ligne, la dernière s'applique.
[CreateAssetMenu(menuName = "RED MET/Table des incursions", fileName = "Incursions")]
public class IncursionTable : ScriptableObject
{
    [Tooltip("Incursions 1, 2, 3...")]
    public IncursionScaling[] incursions = Array.Empty<IncursionScaling>();

    public IncursionScaling For(int incursion)
    {
        if (incursions.Length == 0)
            return IncursionScaling.None;

        return incursions[Mathf.Clamp(incursion - 1, 0, incursions.Length - 1)];
    }
}
