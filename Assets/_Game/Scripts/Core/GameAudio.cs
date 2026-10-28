using UnityEngine;

// Bruitages ponctuels (fusil à pompe, gore). Réserve de sources créée au chargement : aucun
// Instantiate pendant un combat. Sons en 2D, légère variation de hauteur pour éviter la répétition.
// Provisoire : la musique, les voix et le mixage viendront à la fin du projet.
public class GameAudio : MonoBehaviour
{
    public static GameAudio Instance { get; private set; }

    [SerializeField] private int voices = 12;
    [SerializeField, Range(0f, 1f)] private float volume = 0.8f;

    private AudioSource[] sources;
    private int next;

    private void Awake()
    {
        Instance = this;
        sources = new AudioSource[voices];

        for (int i = 0; i < voices; i++)
        {
            var source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
            source.spatialBlend = 0f;
            sources[i] = source;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    public static void Play(AudioClip clip, float clipVolume = 1f, float pitchVariation = 0.05f)
    {
        if (clip == null || Instance == null)
            return;

        AudioSource source = Instance.sources[Instance.next];
        Instance.next = (Instance.next + 1) % Instance.sources.Length;
        source.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        source.PlayOneShot(clip, clipVolume * Instance.volume);
    }
}
