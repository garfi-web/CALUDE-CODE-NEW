using UnityEngine;

// Caméra latérale qui suit la cible en douceur, bornée aux limites de l'arène.
// Effets d'impact : tremblement proportionnel au coup (réglé par GameOptions.Shake)
// et zoom bref vers le point d'impact. Les effets ne déplacent que l'image, pas le suivi.
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    public static CameraFollow Main { get; private set; }

    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new(0f, 1.5f);
    [SerializeField] private float smoothTime = 0.15f;
    // Bornes pour une vue de 6,8 m de haut en 16:9 (demi-largeur 6 m) : le bord s'arrête aux murs (±20 m).
    [SerializeField] private float minX = -14f;
    [SerializeField] private float maxX = 14f;
    [SerializeField] private float minY = 0.9f;

    [Header("Tremblement")]
    [SerializeField] private float maxShakeOffset = 0.4f;
    [SerializeField] private float maxShakeAngle = 1.5f;
    [SerializeField] private float shakeDecay = 2.5f;
    [SerializeField] private float shakeFrequency = 28f;

    [Header("Zoom d'impact")]
    [SerializeField] private float zoomSize = 0.8f;
    [SerializeField] private float zoomPull = 0.35f;
    [SerializeField] private float zoomIn = 0.05f;
    [SerializeField] private float zoomHold = 0.1f;
    [SerializeField] private float zoomOut = 0.3f;

    private Camera cam;
    private Vector3 velocity;
    private Vector3 basePosition;
    private float baseSize;
    private float trauma;
    private float zoomStart = float.NegativeInfinity;
    private Vector2 zoomPoint;

    private void Awake()
    {
        Main = this;
        cam = GetComponent<Camera>();
        baseSize = cam.orthographicSize;
        basePosition = transform.position;
    }

    private void OnDestroy()
    {
        if (Main == this)
            Main = null;
    }

    // Bornes du suivi en X (zone de combat bloquée de l'arène 2,5D, puis ouverte vers la suivante).
    public void SetBounds(float min, float max)
    {
        minX = min;
        maxX = Mathf.Max(min, max);
    }

    public void Shake(float amount)
    {
        trauma = Mathf.Clamp01(trauma + amount);
    }

    public void Zoom(Vector2 point)
    {
        zoomPoint = point;
        zoomStart = Time.unscaledTime;
    }

    private void LateUpdate()
    {
        if (target != null)
        {
            Vector3 desired = new(
                Mathf.Clamp(target.position.x + offset.x, minX, maxX),
                Mathf.Max(target.position.y + offset.y, minY),
                basePosition.z
            );

            basePosition = Vector3.SmoothDamp(basePosition, desired, ref velocity, smoothTime);
        }

        // Le zoom et le tremblement suivent le temps réel : ils restent nets pendant un ralenti.
        float zoom = ZoomAmount(Time.unscaledTime - zoomStart);
        Vector3 position = basePosition + (Vector3)((zoomPoint - (Vector2)basePosition) * (zoomPull * zoom));
        cam.orthographicSize = baseSize * Mathf.Lerp(1f, zoomSize, zoom);

        float shake = trauma * trauma * GameOptions.Shake;
        float time = Time.unscaledTime * shakeFrequency;
        position.x += (Mathf.PerlinNoise(time, 0.1f) * 2f - 1f) * maxShakeOffset * shake;
        position.y += (Mathf.PerlinNoise(0.7f, time) * 2f - 1f) * maxShakeOffset * shake;
        transform.SetPositionAndRotation(position,
            Quaternion.Euler(0f, 0f, (Mathf.PerlinNoise(time, 3.3f) * 2f - 1f) * maxShakeAngle * shake));

        trauma = Mathf.Max(0f, trauma - shakeDecay * Time.unscaledDeltaTime);
    }

    private float ZoomAmount(float elapsed)
    {
        if (elapsed < 0f || elapsed > zoomIn + zoomHold + zoomOut)
            return 0f;

        if (elapsed < zoomIn)
            return elapsed / zoomIn;

        if (elapsed < zoomIn + zoomHold)
            return 1f;

        float t = (elapsed - zoomIn - zoomHold) / zoomOut;
        return 1f - t * t;
    }
}
