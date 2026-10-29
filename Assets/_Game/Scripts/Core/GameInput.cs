using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public enum GameAction
{
    Jump,
    Light,
    Heavy,
    Dash,
    Dodge,
    Shotgun,
    Reload,
}

// Point d'entrée unique pour les commandes du prototype.
// Fonctionne avec le nouvel Input System (par défaut dans Unity 6) comme avec
// l'ancien Input Manager, sans toucher aux Project Settings.
// Les touches du nouvel Input System sont physiques : la touche "A" QWERTY
// correspond à Q sur un clavier AZERTY, "W" à Z.
//
// Clavier (AZERTY) : Q/D se déplacer, S bas, Espace sauter, J coup léger, K coup lourd,
// L dash, Maj gauche esquive, U fusil à pompe (M4), I recharger, Échap options, F1 debug des images,
// F2 arme de test, F3 démo, F4 Nathan M4 avec le fusil, F5 sauvegarder, F6 prendre la dose,
// F7 la refuser, F8 nouvelle partie, F9 charger, F10 et F11 scènes de Mara, F12 ciel du chapitre suivant.
// Manette : stick/croix, A saut, X léger, Y lourd, LB dash, RB esquive, RT fusil, LT recharger, Select options.
public static class GameInput
{
    private const float StickDeadZone = 0.3f;
    private const float DownThreshold = -0.5f;

    // Commandes injectées (démo de combo, tests) : consommées à la première lecture.
    private static readonly bool[] Injected = new bool[System.Enum.GetValues(typeof(GameAction)).Length];

    public static float? HorizontalOverride;
    public static float? VerticalOverride;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetStatics()
    {
        System.Array.Clear(Injected, 0, Injected.Length);
        HorizontalOverride = null;
        VerticalOverride = null;
    }

    public static void Inject(GameAction action) => Injected[(int)action] = true;

    private static bool Consume(GameAction action)
    {
        if (!Injected[(int)action])
            return false;

        Injected[(int)action] = false;
        return true;
    }

    public static bool JumpPressed => RawJump || Consume(GameAction.Jump);
    public static bool LightPressed => RawLight || Consume(GameAction.Light);
    public static bool HeavyPressed => RawHeavy || Consume(GameAction.Heavy);
    public static bool DashPressed => RawDash || Consume(GameAction.Dash);
    public static bool DodgePressed => RawDodge || Consume(GameAction.Dodge);
    public static bool ShotgunPressed => RawShotgun || Consume(GameAction.Shotgun);
    public static bool ReloadPressed => RawReload || Consume(GameAction.Reload);

    public static bool DownHeld => Vertical < DownThreshold;

#if ENABLE_INPUT_SYSTEM
    public static float Horizontal
    {
        get
        {
            if (HorizontalOverride.HasValue)
                return HorizontalOverride.Value;

            float x = 0f;
            Keyboard kb = Keyboard.current;

            if (kb != null)
            {
                if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) x -= 1f;
                if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            }

            Gamepad pad = Gamepad.current;

            if (pad != null)
            {
                float stick = pad.leftStick.x.ReadValue();

                if (Mathf.Abs(stick) > StickDeadZone)
                    x = Mathf.Sign(stick);
                else if (pad.dpad.left.isPressed)
                    x = -1f;
                else if (pad.dpad.right.isPressed)
                    x = 1f;
            }

            return x;
        }
    }

    public static float Vertical
    {
        get
        {
            if (VerticalOverride.HasValue)
                return VerticalOverride.Value;

            float y = 0f;
            Keyboard kb = Keyboard.current;

            if (kb != null)
            {
                if (kb.sKey.isPressed || kb.downArrowKey.isPressed) y -= 1f;
                if (kb.wKey.isPressed || kb.upArrowKey.isPressed) y += 1f;
            }

            Gamepad pad = Gamepad.current;

            if (pad != null)
            {
                float stick = pad.leftStick.y.ReadValue();

                if (Mathf.Abs(stick) > StickDeadZone)
                    y = Mathf.Sign(stick);
                else if (pad.dpad.down.isPressed)
                    y = -1f;
                else if (pad.dpad.up.isPressed)
                    y = 1f;
            }

            return y;
        }
    }

    private static bool RawJump => Pressed(Keyboard.current?.spaceKey) || Pressed(Gamepad.current?.buttonSouth);
    private static bool RawLight => Pressed(Keyboard.current?.jKey) || Pressed(Gamepad.current?.buttonWest);
    private static bool RawHeavy => Pressed(Keyboard.current?.kKey) || Pressed(Gamepad.current?.buttonNorth);
    private static bool RawDash => Pressed(Keyboard.current?.lKey) || Pressed(Gamepad.current?.leftShoulder);
    private static bool RawDodge => Pressed(Keyboard.current?.leftShiftKey) || Pressed(Gamepad.current?.rightShoulder);
    private static bool RawShotgun => Pressed(Keyboard.current?.uKey) || Pressed(Gamepad.current?.rightTrigger);
    private static bool RawReload => Pressed(Keyboard.current?.iKey) || Pressed(Gamepad.current?.leftTrigger);

    public static bool RestartPressed =>
        Pressed(Keyboard.current?.rKey) || Pressed(Gamepad.current?.startButton);

    public static bool OptionsPressed =>
        Pressed(Keyboard.current?.escapeKey) || Pressed(Gamepad.current?.selectButton);

    public static bool DebugOverlayPressed => Pressed(Keyboard.current?.f1Key);
    public static bool DebugWeaponPressed => Pressed(Keyboard.current?.f2Key);
    public static bool DemoPressed => Pressed(Keyboard.current?.f3Key);
    public static bool DebugM4Pressed => Pressed(Keyboard.current?.f4Key);

    // État de la partie (StorySession) : sauvegarde, chargement, doses, nouvelle partie.
    public static bool DebugSavePressed => Pressed(Keyboard.current?.f5Key);
    public static bool DebugTakeDosePressed => Pressed(Keyboard.current?.f6Key);
    public static bool DebugRefuseDosePressed => Pressed(Keyboard.current?.f7Key);
    public static bool DebugNewGamePressed => Pressed(Keyboard.current?.f8Key);
    public static bool DebugLoadPressed => Pressed(Keyboard.current?.f9Key);

    // Visual novel : réplique suivante (Entrée, Espace, J, bouton bas de la manette) ;
    // test des scènes de Mara du chapitre en cours (F10 ouverture, F11 fin).
    public static bool DialogueNextPressed =>
        Pressed(Keyboard.current?.enterKey) || Pressed(Keyboard.current?.spaceKey) ||
        Pressed(Keyboard.current?.jKey) || Pressed(Gamepad.current?.buttonSouth);
    public static bool DebugMaraOpeningPressed => Pressed(Keyboard.current?.f10Key);
    public static bool DebugMaraEndingPressed => Pressed(Keyboard.current?.f11Key);

    // Arène de test : ciel du chapitre suivant (SkyController).
    public static bool DebugSkyPressed => Pressed(Keyboard.current?.f12Key);

    // Navigation des menus (flèches, Z/Q/S/D en AZERTY, croix de la manette).
    public static bool MenuUp => Pressed(Keyboard.current?.upArrowKey) || Pressed(Keyboard.current?.wKey) || Pressed(Gamepad.current?.dpad.up);
    public static bool MenuDown => Pressed(Keyboard.current?.downArrowKey) || Pressed(Keyboard.current?.sKey) || Pressed(Gamepad.current?.dpad.down);
    public static bool MenuLeft => Pressed(Keyboard.current?.leftArrowKey) || Pressed(Keyboard.current?.aKey) || Pressed(Gamepad.current?.dpad.left);
    public static bool MenuRight => Pressed(Keyboard.current?.rightArrowKey) || Pressed(Keyboard.current?.dKey) || Pressed(Gamepad.current?.dpad.right);

    // Arène de test : touches 1 à 9 de la rangée du haut ou du pavé numérique.
    // Touche physique : sur AZERTY, c'est la touche « & / 1 », sans Maj.
    public static bool WavePressed(int index)
    {
        Keyboard kb = Keyboard.current;

        if (kb == null || index < 0 || index > 8)
            return false;

        return kb[Key.Digit1 + index].wasPressedThisFrame || kb[Key.Numpad1 + index].wasPressedThisFrame;
    }

    private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl button) =>
        button != null && button.wasPressedThisFrame;
#else
    public static float Horizontal => HorizontalOverride ?? Input.GetAxisRaw("Horizontal");
    public static float Vertical => VerticalOverride ?? Input.GetAxisRaw("Vertical");
    private static bool RawJump => Input.GetButtonDown("Jump");
    private static bool RawLight => Input.GetKeyDown(KeyCode.J);
    private static bool RawHeavy => Input.GetKeyDown(KeyCode.K);
    private static bool RawDash => Input.GetKeyDown(KeyCode.L);
    private static bool RawDodge => Input.GetKeyDown(KeyCode.LeftShift);
    private static bool RawShotgun => Input.GetKeyDown(KeyCode.U);
    private static bool RawReload => Input.GetKeyDown(KeyCode.I);
    public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
    public static bool OptionsPressed => Input.GetKeyDown(KeyCode.Escape);
    public static bool DebugOverlayPressed => Input.GetKeyDown(KeyCode.F1);
    public static bool DebugWeaponPressed => Input.GetKeyDown(KeyCode.F2);
    public static bool DemoPressed => Input.GetKeyDown(KeyCode.F3);
    public static bool DebugM4Pressed => Input.GetKeyDown(KeyCode.F4);
    public static bool DebugSavePressed => Input.GetKeyDown(KeyCode.F5);
    public static bool DebugTakeDosePressed => Input.GetKeyDown(KeyCode.F6);
    public static bool DebugRefuseDosePressed => Input.GetKeyDown(KeyCode.F7);
    public static bool DebugNewGamePressed => Input.GetKeyDown(KeyCode.F8);
    public static bool DebugLoadPressed => Input.GetKeyDown(KeyCode.F9);
    public static bool DialogueNextPressed =>
        Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.J);
    public static bool DebugMaraOpeningPressed => Input.GetKeyDown(KeyCode.F10);
    public static bool DebugMaraEndingPressed => Input.GetKeyDown(KeyCode.F11);
    public static bool DebugSkyPressed => Input.GetKeyDown(KeyCode.F12);
    public static bool MenuUp => Input.GetKeyDown(KeyCode.UpArrow);
    public static bool MenuDown => Input.GetKeyDown(KeyCode.DownArrow);
    public static bool MenuLeft => Input.GetKeyDown(KeyCode.LeftArrow);
    public static bool MenuRight => Input.GetKeyDown(KeyCode.RightArrow);

    public static bool WavePressed(int index) =>
        index >= 0 && index <= 8 &&
        (Input.GetKeyDown(KeyCode.Alpha1 + index) || Input.GetKeyDown(KeyCode.Keypad1 + index));
#endif
}
