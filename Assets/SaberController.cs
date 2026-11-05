using UnityEngine;

// Part 5: swing the lightsaber with the mouse, throw it with T, and Force-pull it back with F.
//
// Attach to the "Lightsaber" root, which sits at the center of the hilt. Its position and rotation in the
// scene are the resting "hand" pose; the mouse moves the hand a little and tilts the saber around it, so
// moving the mouse swings the blades in an arc.
public class SaberController : MonoBehaviour
{
    public enum State { Held, Thrown, Returning }

    [Header("Mouse swinging")]
    [Tooltip("Degrees the saber tilts left/right with the mouse at the left/right edge of the screen.")]
    public float maxRoll = 70f;
    [Tooltip("Degrees the saber tips forward/back with the mouse at the bottom/top of the screen.")]
    public float maxPitch = 45f;
    [Tooltip("How far (units) the hand moves with the mouse, horizontally and vertically.")]
    public Vector2 handTravel = new Vector2(0.6f, 0.35f);
    [Tooltip("How quickly the saber follows the mouse. Higher is snappier.")]
    public float responsiveness = 14f;

    [Header("Throw (T)")]
    [Tooltip("Launch speed in units per second, toward the point under the mouse cursor.")]
    public float throwSpeed = 14f;
    [Tooltip("How quickly a thrown saber slows down until it hovers, held by the Force.")]
    public float throwDrag = 2.2f;
    [Tooltip("Spin in degrees per second while the saber is in the air.")]
    public float spinSpeed = 900f;

    [Header("Force pull (F)")]
    [Tooltip("How quickly the returning saber speeds up, in units per second squared.")]
    public float pullAcceleration = 35f;
    [Tooltip("Top speed of the returning saber, in units per second.")]
    public float maxPullSpeed = 20f;

    public State CurrentState => state;

    private State state = State.Held;
    private Vector3 restPosition;
    private Quaternion restRotation;
    private Vector3 velocity;
    private Vector3 spinAxis = Vector3.forward;
    private float pullSpeed;
    private Camera cam;

    void Awake()
    {
        restPosition = transform.position;
        restRotation = transform.rotation;
        cam = Camera.main;
    }

    void Update()
    {
        float dt = Time.deltaTime;
        HandPose(out Vector3 handPosition, out Quaternion handRotation);

        switch (state)
        {
            case State.Held:
                // Ease toward the hand pose, which gives swings a little weight.
                float follow = 1f - Mathf.Exp(-responsiveness * dt);
                transform.SetPositionAndRotation(
                    Vector3.Lerp(transform.position, handPosition, follow),
                    Quaternion.Slerp(transform.rotation, handRotation, follow));
                if (SaberInput.ThrowPressed())
                {
                    Throw();
                }
                break;

            case State.Thrown:
                transform.position += velocity * dt;
                velocity *= Mathf.Exp(-throwDrag * dt);
                Spin(spinSpeed, dt);
                if (SaberInput.ForcePullPressed())
                {
                    ForcePull();
                }
                break;

            case State.Returning:
                pullSpeed = Mathf.Min(maxPullSpeed, pullSpeed + pullAcceleration * dt);
                transform.position = Vector3.MoveTowards(transform.position, handPosition, pullSpeed * dt);
                float distance = Vector3.Distance(transform.position, handPosition);
                // Keep spinning on the way back, slowing down over the last two units.
                Spin(spinSpeed * Mathf.Clamp01(distance / 2f), dt);
                if (distance < 0.05f)
                {
                    state = State.Held; // caught: the Held pose straightens it out in the hand
                }
                break;
        }
    }

    // Throws the saber toward the point under the mouse cursor. Public so other inputs (e.g. VR) can use it.
    public void Throw()
    {
        if (state != State.Held)
        {
            return;
        }

        Vector3 target = cam != null
            ? cam.ScreenPointToRay(SaberInput.PointerPosition()).GetPoint(12f)
            : transform.position + transform.forward * 12f;
        Vector3 direction = (target - transform.position).normalized;
        velocity = direction * throwSpeed;
        spinAxis = direction; // spins like a pinwheel as it flies away
        state = State.Thrown;
    }

    // Pulls a thrown saber back into the hand.
    public void ForcePull()
    {
        if (state != State.Thrown)
        {
            return;
        }

        pullSpeed = 0f;
        state = State.Returning;
    }

    private void Spin(float degreesPerSecond, float dt)
    {
        transform.rotation = Quaternion.AngleAxis(degreesPerSecond * dt, spinAxis) * transform.rotation;
    }

    // The hand pose for the current mouse position. Left/right rolls the saber and up/down tips it
    // forward/back, while the hand itself moves a little with the mouse.
    private void HandPose(out Vector3 position, out Quaternion rotation)
    {
        Vector2 pointer = SaberInput.PointerPosition();
        float x = Mathf.Clamp(pointer.x / Mathf.Max(1, Screen.width) * 2f - 1f, -1f, 1f);
        float y = Mathf.Clamp(pointer.y / Mathf.Max(1, Screen.height) * 2f - 1f, -1f, 1f);

        Vector3 right = cam != null ? cam.transform.right : Vector3.right;
        Vector3 up = cam != null ? cam.transform.up : Vector3.up;
        position = restPosition + right * (x * handTravel.x) + up * (y * handTravel.y);
        rotation = restRotation * Quaternion.Euler(-y * maxPitch, 0f, -x * maxRoll);
    }
}
