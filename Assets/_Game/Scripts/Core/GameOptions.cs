using System;
using UnityEngine;

public enum GoreLevel
{
    Off,
    Reduced,
    Full,
}

// Options d'accessibilité, enregistrées dans les PlayerPrefs.
public static class GameOptions
{
    private const string GoreKey = "redmet.gore";
    private const string ShakeKey = "redmet.shake";
    private const string FlashKey = "redmet.flash";

    private static bool loaded;
    private static GoreLevel gore = GoreLevel.Full;
    private static float shake = 1f;
    private static bool impactFlashes = true;

    public static event Action Changed;

    public static GoreLevel Gore
    {
        get { Load(); return gore; }
        set { Load(); gore = value; Save(); }
    }

    // Intensité du tremblement de caméra, de 0 à 1.
    public static float Shake
    {
        get { Load(); return shake; }
        set { Load(); shake = Mathf.Clamp01(value); Save(); }
    }

    // Images d'impact plein écran (risque épileptique : désactivables).
    public static bool ImpactFlashes
    {
        get { Load(); return impactFlashes; }
        set { Load(); impactFlashes = value; Save(); }
    }

    private static void Load()
    {
        if (loaded)
            return;

        loaded = true;
        gore = (GoreLevel)Mathf.Clamp(PlayerPrefs.GetInt(GoreKey, (int)GoreLevel.Full), 0, 2);
        shake = Mathf.Clamp01(PlayerPrefs.GetFloat(ShakeKey, 1f));
        impactFlashes = PlayerPrefs.GetInt(FlashKey, 1) == 1;
    }

    private static void Save()
    {
        PlayerPrefs.SetInt(GoreKey, (int)gore);
        PlayerPrefs.SetFloat(ShakeKey, shake);
        PlayerPrefs.SetInt(FlashKey, impactFlashes ? 1 : 0);
        PlayerPrefs.Save();
        Changed?.Invoke();
    }
}
