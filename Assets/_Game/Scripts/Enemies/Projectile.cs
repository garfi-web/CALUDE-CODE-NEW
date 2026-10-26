using UnityEngine;

// Balle : avance en ligne droite et teste sa trajectoire à chaque pas (pas de Rigidbody).
// Elle ne touche pas les personnages du même camp (même tag que le tireur).
// Si la cible est invulnérable (esquive), la balle la traverse.
public class Projectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.08f;
    [SerializeField] private float lifetime = 3f;
    [SerializeField] private float knockbackLift = 1f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    private GameObject owner;
    private Vector2 velocity;
    private Vector2 origin;
    private int damage;
    private float knockback;
    private float dieTime;

    public void Launch(GameObject shooter, Vector2 direction, float speed, int hitDamage, float hitKnockback)
    {
        owner = shooter;
        velocity = direction.normalized * speed;
        origin = transform.position;
        damage = hitDamage;
        knockback = hitKnockback;
        dieTime = Time.time + lifetime;
        transform.right = direction;
    }

    private void FixedUpdate()
    {
        if (Time.time >= dieTime)
        {
            Destroy(gameObject);
            return;
        }

        Vector2 position = transform.position;
        Vector2 step = velocity * Time.fixedDeltaTime;
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.CircleCast(position, radius, step.normalized, filter, hits, step.magnitude);

        // Physics2D.CircleCast trie les contacts du plus proche au plus lointain.
        for (int i = 0; i < count; i++)
        {
            if (HandleHit(hits[i].collider))
            {
                Destroy(gameObject);
                return;
            }
        }

        transform.position = position + step;
    }

    // Renvoie vrai si la balle s'arrête.
    private bool HandleHit(Collider2D other)
    {
        if (owner != null && other.transform.IsChildOf(owner.transform))
            return false;

        if (other.GetComponent<PlatformEffector2D>() != null)
            return false;

        Health health = other.GetComponentInParent<Health>();

        if (health != null)
        {
            if (owner != null && health.CompareTag(owner.tag))
                return false;

            if (!health.TakeDamage(new DamageInfo(damage, DamageKind.Ranged, origin)))
                return false;

            Rigidbody2D body = other.attachedRigidbody;

            if (body != null && !health.IsDead)
                body.AddForce(new Vector2(Mathf.Sign(velocity.x) * knockback, knockbackLift), ForceMode2D.Impulse);

            return true;
        }

        Cover cover = other.GetComponent<Cover>();
        return cover == null || !cover.LetsShotsThrough;
    }
}
