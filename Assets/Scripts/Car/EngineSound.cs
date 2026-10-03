using UnityEngine;

// Looping engine sound whose pitch follows the speed of the car.
[RequireComponent(typeof(AudioSource))]
public class EngineSound : MonoBehaviour
{
    [SerializeField] private CarController car;
    [SerializeField] private float minPitch = 0.6f;
    [SerializeField] private float maxPitch = 2.0f;
    [SerializeField] private float throttlePitchBoost = 0.1f;
    [SerializeField] private float pitchSmoothing = 4f;
    [SerializeField] private float minVolume = 0.45f;
    [SerializeField] private float maxVolume = 0.8f;

    private AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
        source.loop = true;
        source.pitch = minPitch;
    }

    private void Start()
    {
        if (!source.isPlaying)
            source.Play();
    }

    private void Update()
    {
        float speed01 = Mathf.Clamp01(car.Speed / car.MaxSpeed);
        float targetPitch = Mathf.Lerp(minPitch, maxPitch, speed01);
        if (car.Throttle > 0.1f)
            targetPitch += throttlePitchBoost;
        targetPitch = Mathf.Clamp(targetPitch, minPitch, maxPitch);

        // Move towards the target instead of setting it directly -> smooth revving.
        source.pitch = Mathf.Lerp(source.pitch, targetPitch, pitchSmoothing * Time.deltaTime);
        source.volume = Mathf.Lerp(minVolume, maxVolume, Mathf.Max(speed01, Mathf.Abs(car.Throttle) * 0.5f));
    }
}
