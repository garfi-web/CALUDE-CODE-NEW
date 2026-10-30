using System.Collections;
using UnityEngine;

// Entrée d'ennemis dans le décor du fond de l'arène 2,5D : porte, fenêtre ou ruelle. La position est le pied de
// l'entrée, au bord arrière du sol. Chaque arrivée est annoncée avant que l'ennemi apparaisse : bruit, et l'ouverture
// (noir de la porte qui s'ouvre, éclat de la vitre, silhouette dans la ruelle) qui monte pendant warning secondes.
public class SpawnPoint25D : MonoBehaviour
{
    [SerializeField] private ArenaDirector25D.Entry kind = ArenaDirector25D.Entry.Door;
    [SerializeField] private SpriteRenderer opening;
    [Tooltip("Fenêtre : hauteur de l'appui au-dessus du sol (m), d'où l'ennemi saute.")]
    [SerializeField] private float height;
    [SerializeField] private float warning = 0.7f;
    [SerializeField] private float holdOpen = 1.2f;
    [SerializeField] private AudioClip sound;

    private Color openColor = Color.black;

    public ArenaDirector25D.Entry Kind => kind;
    public float X => transform.position.x;
    public float Height => height;
    public float Warning => warning;

    private void Awake()
    {
        if (opening == null)
            return;

        openColor = opening.color;
        opening.color = Clear(openColor);
    }

    public void Announce()
    {
        GameAudio.Play(sound);
        StopAllCoroutines();
        StartCoroutine(Open());
    }

    private IEnumerator Open()
    {
        if (opening == null)
            yield break;

        if (kind == ArenaDirector25D.Entry.Window && CombatVfx.Instance != null)
            CombatVfx.Instance.Dust(opening.transform.position, 10);

        for (float t = 0f; t < warning; t += Time.deltaTime)
        {
            opening.color = Color.Lerp(Clear(openColor), openColor, t / warning);
            yield return null;
        }

        opening.color = openColor;
        yield return new WaitForSeconds(holdOpen);

        for (float t = 0f; t < 0.5f; t += Time.deltaTime)
        {
            opening.color = Color.Lerp(openColor, Clear(openColor), t / 0.5f);
            yield return null;
        }

        opening.color = Clear(openColor);
    }

    private static Color Clear(Color c) => new(c.r, c.g, c.b, 0f);
}
