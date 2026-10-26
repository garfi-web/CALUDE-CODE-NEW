using UnityEngine;

// Balle : avance en ligne droite et teste sa trajectoire à chaque pas (pas de Rigidbody).
// Elle ne touche pas les personnages du même camp (même tag que le tireur).
// Si la cible est invulnérable (esquive), la balle la traverse. Objet en réserve (Pool).
public class Projectile : MonoBehaviour
{
    [SerializeField] private float radius = 0.08f;
    [SerializeField] private float lifetime = 3f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    private GameObject owner;
    private AttackData attack;
    private Vector2 velocity;
    private float dieTime;

    public void Launch(GameObject shooter, Vector2 direction, float speed, AttackData data)
    {
        owner = shooter;
        attack = data;
        velocity = direction.normalized * speed;
        dieTime = Time.time + lifetime;
        transform.right = direction;
    }

    private void FixedUpdate()
    {
        if (Time.time >= dieTime)
        {
            Pool.Release(this);
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
            if (HandleHit(hits[i].collider, hits[i].point))
            {
                Pool.Release(this);
                return;
            }
        }

        transform.position = position + step;
    }

    // Renvoie vrai si la balle s'arrête.
    private bool HandleHit(Collider2D other, Vector2 point)
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

            return Combat.Strike(owner, health, attack, point, Mathf.Sign(velocity.x), DamageKind.Ranged, false);
        }

        Cover cover = other.GetComponent<Cover>();
        return cover == null || !cover.LetsShotsThrough;
    }
}
