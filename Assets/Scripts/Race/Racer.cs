using System.Collections.Generic;
using UnityEngine;

// Race progress of one car (player or opponent): its own lap counter, lap times and checkpoints.
// The RaceManager updates these values when the car passes a checkpoint.
public class Racer : MonoBehaviour
{
    [SerializeField] private string racerName = "Player";
    [SerializeField] private bool isPlayer;
    [SerializeField] private CarController car;
    [SerializeField] private float fallResetHeight = -15f;

    private readonly List<float> lapTimes = new List<float>();
    private Vector3 startPosition;
    private Quaternion startRotation;

    public string RacerName => racerName;
    public bool IsPlayer => isPlayer;
    public CarController Car => car;
    public IReadOnlyList<float> LapTimes => lapTimes;
    public int CompletedLaps => lapTimes.Count;
    public float BestLap { get; private set; } = float.MaxValue;
    public bool HasBestLap => BestLap < float.MaxValue;

    public int NextCheckpoint { get; set; }
    public int CheckpointsPassed { get; set; }   // all valid checkpoints since the start, used for the positions
    public bool HasCrossedStartLine { get; set; }
    public float LapStartTime { get; set; }
    public bool Finished { get; set; }
    public float FinishTime { get; set; }
    public Checkpoint LastCheckpoint { get; set; }

    private void Awake()
    {
        startPosition = transform.position;
        startRotation = transform.rotation;
    }

    private void Update()
    {
        // Fell off the world somehow -> put it back on the track.
        if (transform.position.y < fallResetHeight)
            ResetToLastCheckpoint();
    }

    // Stores a lap time and returns true if it is a new best lap.
    public bool AddLap(float lapTime)
    {
        lapTimes.Add(lapTime);
        if (lapTime < BestLap)
        {
            BestLap = lapTime;
            return true;
        }
        return false;
    }

    // Back to the last passed checkpoint (position, rotation and velocity), or to the grid if there is none yet.
    public void ResetToLastCheckpoint()
    {
        if (LastCheckpoint != null)
            car.ResetTo(LastCheckpoint.RespawnPosition, LastCheckpoint.RespawnRotation);
        else
            car.ResetTo(startPosition + Vector3.up * 0.3f, startRotation);
    }
}
