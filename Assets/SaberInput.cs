using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Mouse and keyboard controls for the Part 5 lightsaber features, kept in one place.
// This project's Active Input Handling is the Input System package; the legacy Input Manager calls are a
// fallback for projects that use the old one.
public static class SaberInput
{
    // Mouse position in screen pixels (the Game view, when running in the Editor).
    public static Vector2 PointerPosition()
    {
#if ENABLE_INPUT_SYSTEM
        return Mouse.current != null
            ? Mouse.current.position.ReadValue()
            : new Vector2(Screen.width, Screen.height) * 0.5f;
#else
        return Input.mousePosition;
#endif
    }

    // T: throw the saber.
    public static bool ThrowPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.T);
#endif
    }

    // F: Force-pull the thrown saber back to your hand.
    public static bool ForcePullPressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.fKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.F);
#endif
    }
}
