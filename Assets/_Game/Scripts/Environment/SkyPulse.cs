using UnityEngine;

// Pulsation lente d'un halo (reflets du soleil). SkyController règle la taille de base et l'amplitude
// selon le ciel du chapitre ; amplitude 0 = halo fixe.
public class SkyPulse : MonoBehaviour
{
    [SerializeField] private float period = 4f;

    public float Amplitude { get; set; }
    public float BaseScale { get; set; } = 1f;

    private void LateUpdate()
    {
        float s = BaseScale * (1f + Amplitude * Mathf.Sin(Time.time * Mathf.PI * 2f / period));
        transform.localScale = new Vector3(s, s, 1f);
    }
}
