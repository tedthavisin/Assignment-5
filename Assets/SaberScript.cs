using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

// Extends / collapses the lightsaber blade when Space is pressed.
//
// Attach this to the empty "BladeBase" GameObject that sits at the bottom of the blade and is the
// parent of the "Blade" cylinder. Scaling BladeBase along its local y axis makes the blade grow out of
// (and shrink back into) its base, instead of from the cylinder's own pivot in its middle.
public class SaberScript : MonoBehaviour
{
    // 1. Extend / collapse speed: the number of seconds a full extend or collapse takes.
    [Range(0.01f, 1f)]
    public float extendSpeed = 0.1f;

    // The blade cylinder. It is deactivated (so Unity stops rendering it) while the saber is off.
    public GameObject blade;

    // 2. Lightsaber state: on (true) or off (false).
    private bool weaponActive;

    // 3. Minimum scale along the local y axis (fully collapsed).
    private const float SCALE_MIN = 0f;

    // 4. Maximum scale along the local y axis (fully extended: however big it is in the scene).
    private float scaleMax;

    // 5. Interpolation value: how much the scale changes per second.
    //    Positive while extending, negative while collapsing.
    private float extendDelta;

    // 6. Current scale along the local y axis.
    private float scaleCurrent;

    // 7. Initial scale along the local x and z axes (these never change).
    private float localScaleX;
    private float localScaleZ;

    // Read-only state for other scripts (e.g. SaberAudio).
    // True while the blade is on or extending; false while it is collapsing or off.
    public bool IsOn => extendDelta > 0;

    // How far the blade is extended: 0 = collapsed, 1 = fully extended.
    public float Extension => scaleMax > 0 ? scaleCurrent / scaleMax : 0f;

    void Awake()
    {
        localScaleX = transform.localScale.x;
        localScaleZ = transform.localScale.z;

        scaleMax = transform.localScale.y;

        // The lightsaber starts on and fully extended.
        scaleCurrent = scaleMax;

        extendDelta = scaleMax / extendSpeed;

        weaponActive = true;
    }

    void Update()
    {
        if (TogglePressed())
        {
            ToggleBlade();
        }

        // Grow or shrink along the local y axis (local, so it follows the blade however the saber is rotated),
        // never going past the minimum or maximum scale.
        scaleCurrent += extendDelta * Time.deltaTime;
        scaleCurrent = Mathf.Clamp(scaleCurrent, SCALE_MIN, scaleMax);
        transform.localScale = new Vector3(localScaleX, scaleCurrent, localScaleZ);

        weaponActive = scaleCurrent > 0;

        // Hide the blade once it has fully collapsed, and show it again as soon as it starts extending.
        if (blade == null)
        {
            return;
        }

        if (weaponActive && !blade.activeSelf)
        {
            blade.SetActive(true);
        }
        else if (!weaponActive && blade.activeSelf)
        {
            blade.SetActive(false);
        }
    }

    // Flips the direction of the blade: collapses it if it is on, extends it if it is off.
    public void ToggleBlade()
    {
        extendDelta = weaponActive ? -Mathf.Abs(extendDelta) :
                                      Mathf.Abs(extendDelta);
    }

    // This project's Active Input Handling (Project Settings > Player) is the Input System package,
    // where the legacy Input.GetKeyDown(KeyCode.Space) throws an exception, so Space is read through
    // the Input System instead. The legacy call is kept for projects that use the old Input Manager.
    private static bool TogglePressed()
    {
#if ENABLE_INPUT_SYSTEM
        return Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
#else
        return Input.GetKeyDown(KeyCode.Space);
#endif
    }
}
