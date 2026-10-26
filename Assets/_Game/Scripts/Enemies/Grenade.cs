using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// Grenade : lancer en cloche vers un point, puis cercle rouge au sol pendant la fusée,
// puis explosion qui touche tout personnage de l'autre camp dans le rayon.
public class Grenade : MonoBehaviour
{
    [SerializeField] private LineRenderer zone;
    [SerializeField] private float gravity = 20f;
    [SerializeField] private float apexHeight = 2.5f;
    [SerializeField] private float radius = 0.12f;
    [SerializeField] private float maxFlightTime = 3f;
    [SerializeField] private int zoneSegments = 40;
    [SerializeField] private Color zoneColor = new(1f, 0.1f, 0.1f);
    [SerializeField] private float knockbackLift = 4f;

    private readonly RaycastHit2D[] hits = new RaycastHit2D[8];
    private GameObject owner;
    private Vector2 velocity;
    private int damage;
    private float areaRadius;
    private float fuse;
    private float knockback;
    private float launchTime;
    private bool landed;

    public void Launch(GameObject thrower, Vector2 targetPoint, int hitDamage, float area, float fuseTime, float hitKnockback)
    {
        owner = thrower;
        damage = hitDamage;
        areaRadius = area;
        fuse = fuseTime;
        knockback = hitKnockback;
        launchTime = Time.time;

        // Cloche de hauteur fixe au-dessus du point le plus haut, pour passer par-dessus les abris :
        // montée jusqu'au sommet, puis chute jusqu'au point visé.
        Vector2 start = transform.position;
        float apexY = Mathf.Max(start.y, targetPoint.y) + apexHeight;
        float riseTime = Mathf.Sqrt(2f * (apexY - start.y) / gravity);
        float fallTime = Mathf.Sqrt(2f * (apexY - targetPoint.y) / gravity);
        velocity = new Vector2((targetPoint.x - start.x) / (riseTime + fallTime), gravity * riseTime);

        if (zone != null)
            zone.enabled = false;
    }

    private void FixedUpdate()
    {
        if (landed)
            return;

        if (Time.time - launchTime > maxFlightTime)
        {
            Land(transform.position);
            return;
        }

        velocity.y -= gravity * Time.fixedDeltaTime;
        Vector2 position = transform.position;
        Vector2 step = velocity * Time.fixedDeltaTime;
        var filter = ContactFilter2D.noFilter;
        filter.useTriggers = false;
        int count = Physics2D.CircleCast(position, radius, step.normalized, filter, hits, step.magnitude);

        for (int i = 0; i < count; i++)
        {
            Collider2D other = hits[i].collider;

            // Elle passe à travers les personnages et rebondit sur rien : elle s'arrête au sol ou au mur.
            if (other.GetComponentInParent<Health>() != null)
                continue;

            if (other.GetComponent<PlatformEffector2D>() != null && velocity.y > 0f)
                continue;

            Land(hits[i].centroid);
            return;
        }

        transform.position = position + step;
    }

    private void Land(Vector2 point)
    {
        landed = true;
        transform.position = point;
        StartCoroutine(FuseRoutine());
    }

    private IEnumerator FuseRoutine()
    {
        DrawZone(0.06f);

        for (float t = 0f; t < fuse; t += Time.deltaTime)
        {
            // Le cercle clignote de plus en plus vite avant l'explosion.
            if (zone != null)
                zone.enabled = Mathf.Repeat(t * Mathf.Lerp(3f, 12f, t / fuse), 1f) < 0.65f;

            yield return null;
        }

        Explode();
        DrawZone(0.25f);
        yield return new WaitForSeconds(0.12f);
        Destroy(gameObject);
    }

    private void Explode()
    {
        var damaged = new HashSet<Health>();

        foreach (Collider2D other in Physics2D.OverlapCircleAll(transform.position, areaRadius))
        {
            Health health = other.GetComponentInParent<Health>();

            if (health == null || !damaged.Add(health) || (owner != null && health.CompareTag(owner.tag)))
                continue;

            if (!health.TakeDamage(new DamageInfo(damage, DamageKind.Explosion, transform.position)))
                continue;

            Rigidbody2D body = other.attachedRigidbody;

            if (body != null && !health.IsDead)
            {
                float direction = Mathf.Sign(body.position.x - transform.position.x);
                body.AddForce(new Vector2(direction * knockback, knockbackLift), ForceMode2D.Impulse);
            }
        }
    }

    private void DrawZone(float width)
    {
        if (zone == null)
            return;

        zone.enabled = true;
        zone.useWorldSpace = true;
        zone.loop = true;
        zone.startWidth = zone.endWidth = width;
        zone.startColor = zone.endColor = zoneColor;
        zone.positionCount = zoneSegments;

        for (int i = 0; i < zoneSegments; i++)
        {
            float angle = i * Mathf.PI * 2f / zoneSegments;
            zone.SetPosition(i, (Vector2)transform.position + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * areaRadius);
        }
    }
}
