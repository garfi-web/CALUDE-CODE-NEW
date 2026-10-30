using UnityEngine;

// Calque de visage anime, posé sur la tête du sprite à chaque image d'animation (piste relevée dans Blender) et caché
// quand la tête se détourne ou qu'un bras passe devant. L'expression suit le jeu : douleur quand il est touché, rage
// (Nathan : coup lourd, lanceur, smash, fusil, ou vie basse ; ennemi : armement de son coup), effort pendant une
// attaque, neutre sinon. Le dessin est le même à chaque image : pas de scintillement.
public class FaceLayer25D : MonoBehaviour
{
    public enum Expression { Neutral, Effort, Pain, Rage }

    [SerializeField] private SpriteRenderer body;
    [SerializeField] private SpriteRenderer face;
    [SerializeField] private CharacterLook25D look;
    [Tooltip("Durée de l'expression de douleur après un coup reçu (images à 60 FPS).")]
    [SerializeField] private int painFrames = 24;
    [Tooltip("Vie (fraction) sous laquelle Nathan garde la rage.")]
    [SerializeField] private float rageHealth = 0.3f;

    private MaterialPropertyBlock flash;
    private Health health;
    private Enemy25D enemy;
    private IAttackSource25D attacks;
    private int painLeft;
    private int effortLeft;
    private bool rageAttack;

    public Expression Current { get; private set; }

    public void SetLook(CharacterLook25D newLook)
    {
        look = newLook;
        painLeft = effortLeft = 0;
    }

    private void Awake()
    {
        health = GetComponent<Health>();
        enemy = GetComponent<Enemy25D>();
        attacks = GetComponent<IAttackSource25D>();

        if (health != null)
            health.Damaged += OnDamaged;

        if (attacks != null)
            attacks.AttackStarted += OnAttack;
    }

    private void OnDestroy()
    {
        if (health != null)
            health.Damaged -= OnDamaged;

        if (attacks != null)
            attacks.AttackStarted -= OnAttack;
    }

    private void OnDamaged(int damage) => painLeft = painFrames;

    private void OnAttack(AttackData attack)
    {
        effortLeft = attack != null ? attack.TotalFrames : 20;
        rageAttack = attack != null && (attack.IsHeavy || attack.IsCone);
    }

    private void FixedUpdate()
    {
        // Compté en images de jeu : figé pendant l'arrêt sur image, comme le reste du combat.
        if (TryGetComponent(out HitStop hitStop) && hitStop.IsFrozen)
            return;

        if (painLeft > 0) painLeft--;
        if (effortLeft > 0) effortLeft--;
    }

    private void LateUpdate()
    {
        if (face == null || body == null || look == null || !look.TryGetFace(body.sprite, out CharacterLook25D.FaceFrame frame) ||
            !frame.visible || !body.enabled)
        {
            if (face != null)
                face.enabled = false;

            return;
        }

        Current = PickExpression();
        Sprite sprite = look.expressions.Length > (int)Current ? look.expressions[(int)Current] : null;
        face.sprite = sprite != null ? sprite : look.expressions[0];
        face.enabled = face.sprite != null;

        // Position dans l'image du corps, depuis son pivot (les pieds), en unités du monde.
        Sprite current = body.sprite;
        Rect rect = current.rect;
        Vector2 pixel = new(frame.position.x * rect.width, frame.position.y * rect.height);
        Vector2 local = (pixel - current.pivot) / current.pixelsPerUnit;
        Transform t = face.transform;
        t.localPosition = new Vector3(local.x, local.y, 0f);
        t.localRotation = Quaternion.Euler(0f, 0f, frame.angle);
        float size = look.faceScale;
        t.localScale = new Vector3((frame.flipped ? -frame.scale : frame.scale) * size, size, 1f);

        // Même voile que le corps (flash blanc d'impact, annonce d'armement) : le visage ne ressort pas en couleur.
        flash ??= new MaterialPropertyBlock();
        body.GetPropertyBlock(flash);
        face.SetPropertyBlock(flash);
    }

    private Expression PickExpression()
    {
        if (painLeft > 0 || (health != null && health.IsDead))
            return Expression.Pain;

        if (enemy != null)
        {
            if (enemy.CurrentPhase == Enemy25D.Phase.Windup)
                return Expression.Rage;

            return enemy.CurrentPhase == Enemy25D.Phase.None ? Expression.Neutral : Expression.Effort;
        }

        if (effortLeft > 0 && rageAttack)
            return Expression.Rage;

        if (health != null && health.CurrentHealth < health.MaxHealth * rageHealth)
            return Expression.Rage;

        return effortLeft > 0 ? Expression.Effort : Expression.Neutral;
    }
}
