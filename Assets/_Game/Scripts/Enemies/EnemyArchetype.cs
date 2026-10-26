using System;
using UnityEngine;

public enum EnemyWeight
{
    Light,   // recul complet
    Medium,  // recul réduit de moitié
    Heavy,   // pas de recul
}

// Comportement ajouté à l'ennemi à l'apparition (voir Enemy.AddBehaviour).
public enum EnemyBehaviourType
{
    MeleeAggressive,
    Commander,
    CoverShooter,
    Tactical,
    HeavyCharger,
    Ambusher,
    Feinter,
    Trickster,
}

public enum EnemyAttackKind
{
    Melee,      // coup devant l'ennemi, sur la portée
    Projectile, // un ou plusieurs tirs vers la cible
    Grenade,    // lancer en cloche, zone au sol puis explosion
    Charge,     // ruée jusqu'au mur, jusqu'à la cible ou jusqu'à la fin de l'actif
    Shockwave,  // onde au sol autour de l'ennemi (un saut l'évite)
    Order,      // renforce les alliés : armement plus court pendant un temps
    Summon,     // appelle un renfort
}

// Une attaque telle que décrite dans docs/ENEMIES.md.
[Serializable]
public class EnemyAttack
{
    // Règle commune : aucune attaque ne frappe plus vite (docs/ENEMIES.md, section 0).
    public const float MinWindup = 0.35f;

    public string name = "Attaque";
    public EnemyAttackKind kind = EnemyAttackKind.Melee;

    [Header("Timing (secondes)")]
    public float range = 1.2f;
    public float windup = 0.35f;
    public float active = 0.1f;
    public float recovery = 0.35f;
    [Tooltip("Délai minimal entre deux utilisations de cette attaque, compté à la fin de la récupération.")]
    public float cooldown = 0.6f;
    [Tooltip("Nombre d'utilisations par combat (0 = illimité).")]
    public int maxUses;
    [Tooltip("Poids du tirage quand plusieurs attaques du même type sont prêtes.")]
    public float weight = 1f;

    [Header("Effet")]
    public int damage = 10;
    public float knockback = 5f;
    [Tooltip("Feinte : l'armement s'annule sans frapper. L'annonce doit avoir une couleur propre.")]
    public bool feint;

    [Header("Projectiles et grenades")]
    public int projectileCount = 1;
    public float shotInterval = 0.1f;
    public float projectileSpeed = 18f;
    [Tooltip("Grenade : rayon de la zone. Onde au sol : la portée sert de rayon.")]
    public float areaRadius = 2.5f;
    [Tooltip("Grenade : délai entre l'impact et l'explosion.")]
    public float fuse = 1.2f;

    [Header("Charge")]
    public float chargeSpeed = 11f;
    [Tooltip("Pendant ce temps au début de l'armement, la charge suit la cible.")]
    public float trackingTime = 0.6f;
    [Tooltip("Récupération quand la charge finit dans un mur (ennemi sonné).")]
    public float wallRecovery = 2.5f;

    [Header("Ordre et renfort")]
    public float buffWindupMultiplier = 0.8f;
    public float buffDuration = 5f;
    public EnemyArchetype summon;

    [Header("Annonce")]
    public Color telegraphColor = new(1f, 0.85f, 0.2f);
    [Tooltip("Ligne de visée vers la cible pendant l'armement.")]
    public bool aimLine;
    public float aimLineWidth = 0.04f;

    public bool IsRanged => kind is EnemyAttackKind.Projectile or EnemyAttackKind.Grenade;
}

// Fiche d'un ennemi : statistiques identiques sur les deux trajectoires (BESTIARY.md, section 6).
[CreateAssetMenu(menuName = "RED MET/Archétype d'ennemi", fileName = "EnemyArchetype")]
public class EnemyArchetype : ScriptableObject
{
    public string displayName = "Ennemi";
    public EnemyBehaviourType behaviour = EnemyBehaviourType.MeleeAggressive;

    [Header("Statistiques")]
    public int maxHealth = 60;
    public float moveSpeed = 3.5f;
    [Tooltip("Vitesse secondaire : fuite (Dealer), traque (Chasseur), accélération (Psycho).")]
    public float altSpeed = 4.5f;
    public float detectionRange = 9f;
    public EnemyWeight weight = EnemyWeight.Light;
    [Tooltip("Les coups reçus pendant l'armement ne l'interrompent pas.")]
    public bool superArmor;
    public float hitStunTime = 0.3f;

    [Header("Silhouette provisoire")]
    public Color bodyColor = new(0.8f, 0.15f, 0.15f);
    public Vector2 bodySize = new(0.9f, 1.7f);

    public EnemyAttack[] attacks = Array.Empty<EnemyAttack>();
}
