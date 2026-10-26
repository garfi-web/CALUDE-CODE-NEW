using UnityEngine;

// Seul endroit qui règle Time.timeScale : pause du menu d'options et ralenti de fin de vague.
// (Le hitstop, lui, ne touche jamais au temps global : voir HitStop.)
public static class GameTime
{
    private static bool paused;
    private static float slowScale = 1f;
    private static float slowUntil;

    public static bool IsPaused => paused;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        paused = false;
        slowScale = 1f;
        slowUntil = 0f;
    }

    public static void Reset()
    {
        ResetStatics();
        Apply();
    }

    public static void SetPaused(bool value)
    {
        paused = value;
        Apply();
    }

    // Ralenti pendant duration secondes réelles.
    public static void SlowMotion(float duration, float scale)
    {
        slowScale = Mathf.Clamp(scale, 0.05f, 1f);
        slowUntil = Time.unscaledTime + duration;
        Apply();
    }

    // Appelé à chaque image (CombatVfx) pour terminer le ralenti.
    public static void Tick()
    {
        if (slowUntil > 0f && Time.unscaledTime >= slowUntil)
        {
            slowUntil = 0f;
            slowScale = 1f;
            Apply();
        }
    }

    private static void Apply()
    {
        Time.timeScale = paused ? 0f : slowUntil > 0f ? slowScale : 1f;
    }
}
