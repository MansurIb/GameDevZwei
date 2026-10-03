using UnityEngine;

// Invisible trigger box across the track. Knows its own index and tells the RaceManager
// which car drove through it. Index 0 is the start/finish line.
[RequireComponent(typeof(BoxCollider))]
public class Checkpoint : MonoBehaviour
{
    [SerializeField] private int index;
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private Transform respawnPoint;   // where a car is put back when R is pressed

    public int Index => index;
    public Vector3 RespawnPosition => respawnPoint.position;
    public Quaternion RespawnRotation => respawnPoint.rotation;

    private void OnTriggerEnter(Collider other)
    {
        // Only cars have a Racer component, so barriers, decoration etc. never count.
        Racer racer = other.GetComponentInParent<Racer>();
        if (racer != null)
        {
            raceManager.OnCheckpointPassed(this, racer);
        }
    }

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.matrix = transform.localToWorldMatrix;
        Gizmos.color = index == 0 ? new Color(1f, 0.85f, 0.1f, 0.35f) : new Color(0.2f, 0.8f, 1f, 0.25f);
        Gizmos.DrawCube(box.center, box.size);
        Gizmos.color = index == 0 ? Color.yellow : Color.cyan;
        Gizmos.DrawWireCube(box.center, box.size);
    }
}
