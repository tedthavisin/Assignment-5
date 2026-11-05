using UnityEngine;

// Part 5: lightsaber sounds. Ignition/retraction when the blades turn on/off, a hum that follows the blades
// and rises with their speed, and a "swing" whoosh when a blade tip moves fast enough for long enough.
//
// Attach to the "Lightsaber" root. The sounds are 3D, so they pan with the saber and fade when it is thrown.
public class SaberAudio : MonoBehaviour
{
    [Tooltip("The blades whose state and motion drive the sounds.")]
    public SaberScript[] blades;

    public AudioClip igniteClip;
    public AudioClip retractClip;
    public AudioClip humClip;
    public AudioClip swingClip;

    [Range(0f, 1f)] public float humVolume = 0.45f;
    [Range(0f, 1f)] public float effectsVolume = 0.9f;

    [Header("Swing detection")]
    [Tooltip("Blade-tip speed (units per second) that counts as a swing.")]
    public float swingSpeed = 6f;
    [Tooltip("How long (seconds) a tip must stay above that speed before the swing sound plays.")]
    public float minSwingTime = 0.04f;
    [Tooltip("Minimum time (seconds) between swing sounds.")]
    public float swingCooldown = 0.3f;

    // Speed of the fastest lit blade tip in the last frame (units per second).
    public float TipSpeed { get; private set; }

    private AudioSource humSource;
    private AudioSource fxSource;
    private AudioSource swingSource;
    private Vector3[] lastTips;
    private bool wasOn;
    private float fastTime;
    private float lastSwingTime = -10f;
    private float humLevel;
    private float motion;

    void Awake()
    {
        humSource = CreateSource(humClip, true);
        fxSource = CreateSource(null, false);
        swingSource = CreateSource(swingClip, false);
        lastTips = new Vector3[blades.Length];
    }

    void Start()
    {
        wasOn = AnyBladeOn();
        humLevel = MaxExtension();
        for (int i = 0; i < blades.Length; i++)
        {
            lastTips[i] = Tip(i);
        }

        if (humClip != null)
        {
            humSource.volume = humVolume * humLevel;
            humSource.Play();
        }
    }

    // LateUpdate, so the saber has already been moved this frame (by SaberController or a VR hand).
    void LateUpdate()
    {
        float dt = Mathf.Max(Time.deltaTime, 1e-4f);

        // Ignition / retraction
        bool on = AnyBladeOn();
        if (on != wasOn)
        {
            Play(fxSource, on ? igniteClip : retractClip, 1f);
            wasOn = on;
        }

        // Speed of the fastest lit blade tip
        float speed = 0f;
        for (int i = 0; i < blades.Length; i++)
        {
            Vector3 tip = Tip(i);
            if (BladeLit(i))
            {
                speed = Mathf.Max(speed, (tip - lastTips[i]).magnitude / dt);
            }
            lastTips[i] = tip;
        }
        TipSpeed = speed;

        // Swing: fast enough, for long enough, and not too soon after the last swing
        fastTime = speed > swingSpeed ? fastTime + dt : 0f;
        if (fastTime >= minSwingTime && Time.time - lastSwingTime >= swingCooldown)
        {
            swingSource.pitch = Random.Range(0.9f, 1.15f);
            Play(swingSource, swingClip, Mathf.Lerp(0.55f, 1f, Mathf.InverseLerp(swingSpeed, swingSpeed * 4f, speed)));
            lastSwingTime = Time.time;
        }

        // Hum: follows the blades' extension, and gets louder and higher as they move faster
        motion = Mathf.Lerp(motion, Mathf.InverseLerp(0f, 25f, speed), 1f - Mathf.Exp(-12f * dt));
        humLevel = Mathf.MoveTowards(humLevel, MaxExtension(), dt * 4f);
        humSource.volume = humVolume * humLevel * (1f + 0.6f * motion);
        humSource.pitch = 1f + 0.25f * motion;
    }

    private AudioSource CreateSource(AudioClip clip, bool loop)
    {
        var source = gameObject.AddComponent<AudioSource>();
        source.clip = clip;
        source.loop = loop;
        source.playOnAwake = false;
        source.spatialBlend = 1f;                          // 3D
        source.rolloffMode = AudioRolloffMode.Logarithmic;
        source.minDistance = 4f;                           // full volume at the saber's resting distance
        source.maxDistance = 60f;
        source.dopplerLevel = 0f;
        return source;
    }

    private void Play(AudioSource source, AudioClip clip, float volume)
    {
        if (clip == null)
        {
            return;
        }
        source.clip = clip;
        source.volume = effectsVolume * volume;
        source.Play();
    }

    private bool AnyBladeOn()
    {
        foreach (var blade in blades)
        {
            if (blade != null && blade.isActiveAndEnabled && blade.IsOn)
            {
                return true;
            }
        }
        return false;
    }

    private float MaxExtension()
    {
        float extension = 0f;
        foreach (var blade in blades)
        {
            if (blade != null && blade.isActiveAndEnabled)
            {
                extension = Mathf.Max(extension, blade.Extension);
            }
        }
        return extension;
    }

    private bool BladeLit(int i)
    {
        return blades[i] != null && blades[i].isActiveAndEnabled && blades[i].blade != null && blades[i].blade.activeInHierarchy;
    }

    // World position of a blade's tip (the top of its cylinder, which is 2 units tall around its center).
    private Vector3 Tip(int i)
    {
        return blades[i] != null && blades[i].blade != null
            ? blades[i].blade.transform.TransformPoint(Vector3.up)
            : transform.position;
    }
}
