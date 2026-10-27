using UnityEngine;

// Corbeaux qui traversent le ciel de temps en temps, en petite volée. Purement décoratif.
// Les oiseaux sont créés une fois au chargement (aucune création pendant le combat) ; chacun bat des ailes
// en alternant deux images et ondule légèrement. La volée suit la caméra (couche lointaine du ciel).
public class CrowFlock : MonoBehaviour
{
    [SerializeField] private Sprite wingsUp;
    [SerializeField] private Sprite wingsDown;
    [SerializeField] private int count = 5;
    [SerializeField] private Vector2 interval = new(14f, 28f);
    [SerializeField] private float speed = 2.6f;
    [SerializeField] private float flapRate = 6f;
    [SerializeField] private int sortingOrder = -92;

    private SpriteRenderer[] crows;
    private Vector2[] offsets;
    private Transform cam;
    private float nextFlight;
    private float flightTime = -1f;
    private float direction = 1f;
    private float height;

    private void Start()
    {
        cam = Camera.main.transform;
        crows = new SpriteRenderer[count];
        offsets = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            var go = new GameObject("Crow " + i);
            go.transform.SetParent(transform, false);
            crows[i] = go.AddComponent<SpriteRenderer>();
            crows[i].sprite = wingsUp;
            crows[i].color = new Color(0.04f, 0.02f, 0.03f);
            crows[i].sortingOrder = sortingOrder;
            go.transform.localScale = Vector3.one * Random.Range(0.7f, 1f);
            go.SetActive(false);
            offsets[i] = new Vector2(-i * Random.Range(0.6f, 1f), Random.Range(-0.5f, 0.5f));
        }

        nextFlight = Time.time + Random.Range(4f, 8f);
    }

    private void Update()
    {
        if (flightTime < 0f)
        {
            if (Time.time < nextFlight)
                return;

            direction = Random.value < 0.5f ? 1f : -1f;
            height = Random.Range(1.4f, 2.8f);
            flightTime = 0f;

            foreach (SpriteRenderer crow in crows)
                crow.gameObject.SetActive(true);
        }

        flightTime += Time.deltaTime;
        float halfWidth = 8.5f;
        float x = -direction * halfWidth + direction * speed * flightTime;
        bool visible = false;

        for (int i = 0; i < crows.Length; i++)
        {
            float t = flightTime * flapRate + i * 1.7f;
            float cx = x + direction * offsets[i].x;
            Transform crow = crows[i].transform;
            crow.position = new Vector3(cam.position.x + cx, cam.position.y + height + offsets[i].y + 0.08f * Mathf.Sin(t * 0.5f), 0f);
            crow.localScale = new Vector3(Mathf.Abs(crow.localScale.x) * direction, crow.localScale.y, 1f);
            crows[i].sprite = Mathf.Repeat(t, 2f) < 1f ? wingsUp : wingsDown;
            visible |= Mathf.Abs(cx) < halfWidth + 1f;
        }

        if (visible || flightTime < 1f)
            return;

        foreach (SpriteRenderer crow in crows)
            crow.gameObject.SetActive(false);

        flightTime = -1f;
        nextFlight = Time.time + Random.Range(interval.x, interval.y);
    }
}
