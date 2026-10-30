using UnityEngine;

// Objet cassable de l'arène 2,5D (caisse) : un corps du camp neutre, que seul Nathan frappe (Hit25D.CanHit).
// Cassé : éclats, objet caché, et la nourriture qu'il contient (déjà dans la scène, désactivée) tombe à sa place.
[RequireComponent(typeof(Body25D), typeof(Health))]
public class Breakable25D : MonoBehaviour
{
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private Pickup25D content;
    [SerializeField] private AudioClip breakSound;

    private Health health;
    private Body25D body;

    private void Awake()
    {
        health = GetComponent<Health>();
        body = GetComponent<Body25D>();
        health.Hit += OnHit;
        health.Died += OnBroken;

        if (content != null)
            content.gameObject.SetActive(false);
    }

    private void OnDestroy()
    {
        if (health == null)
            return;

        health.Hit -= OnHit;
        health.Died -= OnBroken;
    }

    // Une caisse ne recule pas : elle tremble.
    private void OnHit(DamageInfo info)
    {
        GetComponent<Rigidbody2D>().linearVelocity = Vector2.zero;

        if (sprite != null)
            sprite.transform.localPosition = new Vector3(Random.Range(-0.06f, 0.06f), 0f, 0f);
    }

    private void OnBroken()
    {
        GameAudio.Play(breakSound);

        if (CombatVfx.Instance != null)
        {
            CombatVfx.Instance.Dust(body.Ground, 14);
            CombatVfx.Instance.Bounce(body.Center);
        }

        if (content != null)
            content.Drop(body.Ground + new Vector2(0f, -0.05f));

        gameObject.SetActive(false);
    }
}
