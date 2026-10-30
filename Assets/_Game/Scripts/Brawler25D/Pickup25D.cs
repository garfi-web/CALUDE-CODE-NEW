using System.Collections.Generic;
using UnityEngine;

// Objet posé au sol de l'arène 2,5D. Arme (barre de fer) : Nathan la ramasse avec J tout près (BrawlerCombat25D),
// amount = nombre de coups avant qu'elle casse. Nourriture : rend amount PV quand Nathan passe dessus.
// Rien n'est instancié pendant le combat : la nourriture d'une caisse existe déjà, désactivée (Breakable25D).
public class Pickup25D : MonoBehaviour
{
    public enum Kind { Weapon, Food }

    private static readonly List<Pickup25D> Active = new();

    [SerializeField] private Kind kind = Kind.Weapon;
    [SerializeField] private int amount = 8;
    [SerializeField] private SpriteRenderer sprite;
    [SerializeField] private float depthTolerance = 0.45f;
    [SerializeField] private AudioClip takeSound;

    private float bob;

    public Kind Type => kind;
    public Vector2 Ground => transform.position;

    private void OnEnable() => Active.Add(this);

    private void OnDisable() => Active.Remove(this);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics() => Active.Clear();

    // anyDepth : sans la condition de bande de profondeur (banc d'essai qui cherche l'objet pour y aller).
    public static Pickup25D Nearest(Vector2 ground, float range, Kind kind, bool anyDepth = false)
    {
        Pickup25D best = null;
        float bestDistance = range;

        foreach (Pickup25D pickup in Active)
        {
            if (pickup.kind != kind || (!anyDepth && Mathf.Abs(pickup.Ground.y - ground.y) > pickup.depthTolerance))
                continue;

            float distance = Mathf.Abs(pickup.Ground.x - ground.x);

            if (distance <= bestDistance)
            {
                best = pickup;
                bestDistance = distance;
            }
        }

        return best;
    }

    // Sortie d'une caisse cassée, à l'endroit donné.
    public void Drop(Vector2 ground)
    {
        transform.position = new Vector3(ground.x, ground.y, 0f);
        bob = 0f;
        gameObject.SetActive(true);
    }

    public int Take()
    {
        GameAudio.Play(takeSound);
        gameObject.SetActive(false);
        return amount;
    }

    private void Update()
    {
        // Petit rebond pour attirer l'œil, et dessiné juste derrière un personnage à la même profondeur.
        bob += Time.deltaTime;

        if (sprite != null)
        {
            sprite.transform.localPosition = new Vector3(0f, 0.08f + 0.05f * Mathf.Sin(bob * 4f), 0f);
            sprite.sortingOrder = Playfield25D.SortingOrder(transform.position.y) - 1;
        }

        if (kind != Kind.Food)
            return;

        foreach (Body25D body in Body25D.All)
        {
            if (body.Side != Body25D.Team.Player || body.Airborne || body.Health == null || body.Health.IsDead)
                continue;

            Vector2 delta = body.Ground - Ground;

            if (Mathf.Abs(delta.x) < 0.6f && Mathf.Abs(delta.y) < depthTolerance &&
                body.Health.CurrentHealth < body.Health.MaxHealth)
            {
                body.Health.Heal(Take());
                return;
            }
        }
    }
}
