using UnityEngine;

// Couche de décor en parallaxe : suit la caméra d'une fraction de son déplacement.
// 1 = collée à la caméra (ciel), 0 = immobile (plan du jeu), négatif = premier plan qui défile plus vite.
// drift fait glisser la couche (nuages, brume) ; le décalage boucle sur wrapWidth, la largeur d'une
// répétition de l'image, pour qu'une couche en mosaïque glisse sans fin.
// Passe après CameraFollow : le tremblement d'impact est donc atténué sur les plans lointains.
[DefaultExecutionOrder(100)]
public class ParallaxLayer : MonoBehaviour
{
    [SerializeField] private Vector2 factor = new(0.5f, 0.5f);
    [SerializeField] private float drift;
    [SerializeField] private float wrapWidth;

    private Transform cam;
    private Vector3 start;
    private Vector3 camStart;
    private float driftOffset;

    private void Start()
    {
        cam = Camera.main.transform;
        start = transform.position;
        camStart = cam.position;
    }

    private void LateUpdate()
    {
        if (drift != 0f)
        {
            driftOffset += drift * Time.deltaTime;

            if (wrapWidth > 0f)
                driftOffset = Mathf.Repeat(driftOffset, wrapWidth);
        }

        Vector3 delta = cam.position - camStart;
        transform.position = new Vector3(start.x + delta.x * factor.x + driftOffset, start.y + delta.y * factor.y, start.z);
    }
}
