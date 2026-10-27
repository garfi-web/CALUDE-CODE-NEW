using UnityEngine;

// Couche de décor en parallaxe : suit la caméra d'une fraction de son déplacement.
// 1 = collée à la caméra (ciel), 0 = immobile (plan du jeu), négatif = premier plan qui défile plus vite.
// Passe après CameraFollow : le tremblement d'impact est donc atténué sur les plans lointains.
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 factor = new(0.5f, 0.5f);

    private Transform cam;
    private Vector3 start;
    private Vector3 camStart;

    private void Start()
    {
        cam = Camera.main.transform;
        start = transform.position;
        camStart = cam.position;
    }

    private void LateUpdate()
    {
        Vector3 delta = cam.position - camStart;
        transform.position = new Vector3(start.x + delta.x * factor.x, start.y + delta.y * factor.y, start.z);
    }
}
