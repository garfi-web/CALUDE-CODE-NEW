using UnityEngine;

// Caméra latérale qui suit la cible en douceur, bornée aux limites de l'arène.
public class CameraFollow : MonoBehaviour
{
    [SerializeField] private Transform target;
    [SerializeField] private Vector2 offset = new(0f, 1.5f);
    [SerializeField] private float smoothTime = 0.15f;
    [SerializeField] private float minX = -11f;
    [SerializeField] private float maxX = 11f;
    [SerializeField] private float minY = 1.5f;

    private Vector3 velocity;

    private void LateUpdate()
    {
        if (target == null)
            return;

        Vector3 desired = new(
            Mathf.Clamp(target.position.x + offset.x, minX, maxX),
            Mathf.Max(target.position.y + offset.y, minY),
            transform.position.z
        );

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }
}
