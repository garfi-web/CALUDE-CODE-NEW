using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Point d'entrée unique pour les commandes du prototype.
// Fonctionne avec le nouvel Input System (par défaut dans Unity 6) comme avec
// l'ancien Input Manager, sans toucher aux Project Settings.
// Les touches du nouvel Input System sont physiques : la touche "A" QWERTY
// correspond à Q sur un clavier AZERTY.
public static class GameInput
{
    private const float StickDeadZone = 0.3f;

#if ENABLE_INPUT_SYSTEM
    public static float Horizontal
    {
        get
        {
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

    public static bool JumpPressed =>
        Pressed(Keyboard.current?.spaceKey) || Pressed(Gamepad.current?.buttonSouth);

    public static bool AttackPressed =>
        Pressed(Keyboard.current?.jKey) || Pressed(Gamepad.current?.buttonWest);

    public static bool DodgePressed =>
        Pressed(Keyboard.current?.leftShiftKey) || Pressed(Gamepad.current?.rightShoulder);

    public static bool RestartPressed =>
        Pressed(Keyboard.current?.rKey) || Pressed(Gamepad.current?.startButton);

    private static bool Pressed(UnityEngine.InputSystem.Controls.ButtonControl button) =>
        button != null && button.wasPressedThisFrame;
#else
    public static float Horizontal => Input.GetAxisRaw("Horizontal");
    public static bool JumpPressed => Input.GetButtonDown("Jump");
    public static bool AttackPressed => Input.GetKeyDown(KeyCode.J);
    public static bool DodgePressed => Input.GetKeyDown(KeyCode.LeftShift);
    public static bool RestartPressed => Input.GetKeyDown(KeyCode.R);
#endif
}
