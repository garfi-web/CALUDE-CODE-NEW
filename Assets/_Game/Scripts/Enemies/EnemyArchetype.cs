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
    TrainingDummy,
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
    Shotgun,    // tir en cône (fusil à pompe), annoncé par le cône de danger et la lueur du canon
}

// Une attaque d'ennemi telle que décrite dans docs/ENEMIES.md. Les images (démarrage = armement,
// actif, récupération), les dégâts, l'impact et le recul sont dans l'AttackData ; ici, ce qui
// est propre à l'IA (portée, recharge, tirage) et aux attaques spéciales.
[Serializable]
public class EnemyAttack
{
    // Règle commune : aucune attaque ne frappe plus vite que 0,35 s (docs/ENEMIES.md, section 0).
    public const int MinWindupFrames = 21;

    public string name = "Attaque";
    public EnemyAttackKind kind = EnemyAttackKind.Melee;
    public AttackData data;

    [Header("IA")]
    public float range = 1.2f;
    [Tooltip("Délai minimal (s) entre deux utilisations de cette attaque, compté à la fin de la récupération.")]
    public float cooldown = 0.6f;
    [Tooltip("Nombre d'utilisations par combat (0 = illimité).")]
    public int maxUses;
    [Tooltip("Poids du tirage quand plusieurs attaques du même type sont prêtes.")]
    public float weight = 1f;
    [Tooltip("Feinte : l'armement s'annule sans frapper. L'annonce doit avoir une couleur propre.")]
    public bool feint;

    [Header("Projectiles et grenades")]
    public int projectileCount = 1;
    [Tooltip("Images entre deux tirs d'une rafale.")]
    public int shotIntervalFrames = 6;
    public float projectileSpeed = 18f;
    [Tooltip("Grenade : rayon de la zone. Onde au sol : la portée sert de rayon.")]
    public float areaRadius = 2.5f;
    [Tooltip("Grenade : délai (s) entre l'impact et l'explosion.")]
    public float fuse = 1.2f;

    [Header("Charge")]
    public float chargeSpeed = 11f;
    [Tooltip("Images, au début de l'armement, pendant lesquelles la charge suit la cible.")]
    public int trackingFrames = 36;
    [Tooltip("Récupération (images) quand la charge finit dans un mur (ennemi sonné).")]
    public int wallRecoveryFrames = 150;

    [Header("Ordre et renfort")]
    public float buffWindupMultiplier = 0.8f;
    public float buffDuration = 5f;
    public EnemyArchetype summon;

    [Header("Annonce")]
    public Color telegraphColor = new(1f, 0.85f, 0.2f);
    [Tooltip("Ligne de visée vers la cible pendant l'armement.")]
    public bool aimLine;
    public float aimLineWidth = 0.04f;

    public bool IsRanged => kind is EnemyAttackKind.Projectile or EnemyAttackKind.Grenade or EnemyAttackKind.Shotgun;
    public int WindupFrames => data != null ? data.startup : MinWindupFrames;
    public int ActiveFrames => data != null ? data.active : 1;
    public int RecoveryFrames => data != null ? data.recovery : 0;
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
    [Tooltip("Multiplicateur de l'étourdissement des coups reçus (0 = jamais étourdi).")]
    public float hitstunScale = 1f;

    [Header("Apparence")]
    [Tooltip("Couleur du rectangle provisoire ; blanc pour un sprite animé.")]
    public Color bodyColor = new(0.8f, 0.15f, 0.15f);
    public Vector2 bodySize = new(0.9f, 1.7f);
    [Tooltip("Bout du canon (tirs, ligne de visée), depuis les pieds, vers l'avant : à caler sur la pose de visée des sprites. " +
             "Zéro : bord avant du corps, aux deux tiers de la hauteur.")]
    public Vector2 muzzleOffset;
    [Tooltip("Sprites animés (pivot aux pieds). Vide : rectangle provisoire de couleur bodyColor.")]
    public RuntimeAnimatorController animator;
    public Sprite sprite;
    [Tooltip("Couleur d'un membre arraché quand l'ennemi est en sprites (bodyColor vaut alors blanc) : la teinte des vêtements.")]
    public Color limbColor = new(0.16f, 0.17f, 0.22f);

    public Color LimbColor => animator != null || sprite != null ? limbColor : bodyColor;

    [Header("Sang")]
    [Tooltip("Rouge pour les humains ; noir, rouge lumineux... pour les ennemis surnaturels.")]
    public Color bloodColor = new(0.55f, 0.02f, 0.03f);
    [Tooltip("Sang lumineux (ennemis surnaturels).")]
    public bool bloodGlows;
    public bool canBeDismembered = true;
    [Tooltip("Pulvérisé par un tir à bout portant (humains de l'Acte I). Faux : surnaturels et boss, qui encaissent selon leur fiche.")]
    public bool gibbable = true;
    [Tooltip("Morceaux de corps du personnage (tête, torse, bras, jambes).")]
    public GibSet gibs;

    public EnemyAttack[] attacks = Array.Empty<EnemyAttack>();

    public float KnockbackScale => weight switch
    {
        EnemyWeight.Light => 1f,
        EnemyWeight.Medium => 0.5f,
        _ => 0f,
    };
}
