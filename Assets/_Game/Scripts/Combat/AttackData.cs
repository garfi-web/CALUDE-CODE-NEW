using UnityEngine;

public enum KnockbackType
{
    Light,        // petit recul glissé
    Heavy,        // grand recul glissé, léger décollage
    Launch,       // projection en l'air (jonglage)
    WallBounce,   // projeté fort : rebondit sur le mur
    GroundBounce, // écrasé au sol : rebondit vers le haut
}

public enum DamageType
{
    Blunt,     // poing, pied, crosse : sueur, salive, étincelles
    Blade,     // couteau, lame : sang orienté, taches, démembrement
    Bullet,
    Explosion,
}

// Données d'une attaque, communes à Nathan et aux ennemis. Les durées sont en images à 60 FPS
// (Time.fixedDeltaTime vaut 1/60 : une image = un pas physique).
[CreateAssetMenu(menuName = "RED MET/Attaque", fileName = "AttackData")]
public class AttackData : ScriptableObject
{
    public const float FrameRate = 60f;

    public string displayName = "Attaque";
    [Tooltip("Index transmis à l'Animator (paramètre ComboStep). N'influence jamais le gameplay.")]
    public int animationId;

    [Header("Images (60 FPS)")]
    [Min(1)] public int startup = 6;
    [Min(1)] public int active = 3;
    [Min(0)] public int recovery = 12;
    [Tooltip("Fenêtre d'annulation vers l'attaque suivante, en images depuis le début de l'attaque.")]
    public Vector2Int cancelWindow = new(8, 20);

    [Header("Coup")]
    public int damage = 10;
    public DamageType damageType = DamageType.Blunt;
    [Tooltip("Zone de frappe devant l'attaquant (x vers l'avant), depuis le centre du corps. Calée sur le poing ou le pied de l'image active du sprite.")]
    public Vector2 hitboxOffset = new(0.9f, 0.1f);
    public Vector2 hitboxSize = new(1.2f, 0.9f);
    [Tooltip("Pas en avant (m) pendant le démarrage et l'actif : le coup « avale » la distance et garde l'ennemi à portée.")]
    public float step = 0.3f;
    [Tooltip("Beat'em up 2,5D : écart de profondeur maximal (m) entre l'attaquant et la cible pour que le coup touche " +
             "(même bande de profondeur). Fusil à pompe : largeur du cône au canon, qui s'élargit ensuite avec la distance.")]
    public float depthTolerance = 0.35f;

    // Vitesse qui parcourt le pas pendant le démarrage et l'actif.
    public float StepSpeed => step * FrameRate / (startup + active);

    [Header("Impact")]
    [Range(0, 12)] public int hitstop = 5;
    [Range(0f, 1f)] public float shake = 0.2f;
    [Tooltip("Zoom bref de la caméra vers l'impact (coups lourds, fin de combo).")]
    public bool zoom;
    [Tooltip("Image d'impact plein écran (désactivable dans les options).")]
    public bool impactFlash;

    [Header("Recul")]
    public KnockbackType knockback = KnockbackType.Light;
    [Tooltip("Vitesse donnée à la cible : x vers l'arrière de la cible, y vers le haut.")]
    public Vector2 knockbackVelocity = new(4f, 0f);
    [Tooltip("Images pendant lesquelles la cible ne peut pas agir.")]
    public int hitstun = 15;

    [Header("Tir en cône (fusil à pompe)")]
    [Tooltip("Plombs par tir (effets) ; 0 = pas un tir en cône.")]
    public int pellets;
    [Tooltip("Demi-ouverture du cône (degrés).")]
    public float coneAngle = 16f;
    [Tooltip("Bout portant (m), depuis le canon.")]
    public float closeRange = 2.5f;
    [Tooltip("Au-delà (m), les plombs se dispersent : farHit s'applique.")]
    public float midRange = 5.5f;
    public float maxRange = 9f;
    [Tooltip("Mort instantanée à bout portant, quels que soient les PV (passe par Health.TakeDamage). " +
             "Un ennemi qui ne peut pas être pulvérisé (EnemyArchetype.gibbable) encaisse le coup normal.")]
    public bool lethalAtCloseRange;
    [Tooltip("Coup appliqué au-delà de midRange : dégâts faibles, petit recul.")]
    public AttackData farHit;

    [Header("Sons (présentation, jamais le gameplay)")]
    [Tooltip("Au début du démarrage (armement du fusil : « tchak-tchak »).")]
    public AudioClip windupSound;
    [Tooltip("À la première image active (coup de feu).")]
    public AudioClip activeSound;
    [Tooltip("Au début de la récupération (pompe après le tir).")]
    public AudioClip recoverySound;

    public bool IsCone => pellets > 0;
    public int TotalFrames => startup + active + recovery;
    public float StartupTime => startup / FrameRate;

    public bool IsHeavy => hitstop >= 9 || knockback is KnockbackType.WallBounce or KnockbackType.GroundBounce or KnockbackType.Launch;
}
