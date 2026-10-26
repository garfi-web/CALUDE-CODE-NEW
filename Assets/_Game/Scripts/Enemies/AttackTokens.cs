using System.Collections.Generic;
using UnityEngine;

// Jetons d'attaque partagés : limite le nombre d'ennemis qui frappent Nathan en même temps.
// Corps à corps : 2 au plus (Voyous). Distance : 1 tir à la fois (jeton de tir des Soldats),
// étendu à tous les tireurs pour que chaque ligne de visée reste lisible.
public static class AttackTokens
{
    public const int MaxMelee = 2;
    public const int MaxRanged = 1;

    private static readonly HashSet<Object> Melee = new();
    private static readonly HashSet<Object> Ranged = new();

    // Le rechargement de domaine peut être désactivé dans l'éditeur : on vide à chaque Play.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        Melee.Clear();
        Ranged.Clear();
    }

    public static bool IsAvailable(Object owner, bool ranged)
    {
        HashSet<Object> pool = ranged ? Ranged : Melee;
        Purge(pool);
        return pool.Contains(owner) || pool.Count < (ranged ? MaxRanged : MaxMelee);
    }

    public static bool TryAcquire(Object owner, bool ranged)
    {
        if (!IsAvailable(owner, ranged))
            return false;

        (ranged ? Ranged : Melee).Add(owner);
        return true;
    }

    public static void Release(Object owner)
    {
        Melee.Remove(owner);
        Ranged.Remove(owner);
    }

    // Un ennemi détruit sans avoir rendu son jeton ne doit pas le garder.
    private static void Purge(HashSet<Object> pool) => pool.RemoveWhere(owner => owner == null);
}
