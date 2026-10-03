using UnityEngine;

// Plays a crash sound when the car hits something (barrier, other car) hard enough.
public class CrashSound : MonoBehaviour
{
    [SerializeField] private AudioSource source;
    [SerializeField] private AudioClip crashClip;
    [SerializeField] private float minImpactSpeed = 3f;
    [SerializeField] private float fullVolumeImpactSpeed = 15f;
    [SerializeField] private float cooldown = 0.25f;

    private float lastCrashTime = -10f;

    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastCrashTime < cooldown)
            return;

        // Only the part of the velocity that goes into the obstacle counts (scraping along a wall is quiet).
        float impact = Mathf.Abs(Vector3.Dot(collision.relativeVelocity, collision.GetContact(0).normal));
        if (impact < minImpactSpeed)
            return;

        lastCrashTime = Time.time;
        source.pitch = Random.Range(0.85f, 1.15f);
        source.PlayOneShot(crashClip, Mathf.Clamp01(impact / fullVolumeImpactSpeed));
    }
}
