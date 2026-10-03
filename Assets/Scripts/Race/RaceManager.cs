using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Central race logic: countdown, checkpoints and laps of every car, timing, positions and the end of the race.
public class RaceManager : MonoBehaviour
{
    private enum RaceState { Countdown, Racing, Over }

    [SerializeField] private int totalLaps = 3;
    [SerializeField] private Checkpoint[] checkpoints;   // in driving order, element 0 = start/finish line
    [SerializeField] private Racer[] racers;
    [SerializeField] private Countdown countdown;
    [SerializeField] private RaceHUD hud;
    [SerializeField] private ResultScreen victoryScreen;
    [SerializeField] private ResultScreen defeatScreen;

    private RaceState state = RaceState.Countdown;
    private float raceStartTime;
    private float raceEndTime;
    private Racer player;
    private readonly List<Racer> finishOrder = new List<Racer>();

    public int TotalLaps => totalLaps;
    public int RacerCount => racers.Length;
    public Racer Player => player;
    public bool IsRacing => state == RaceState.Racing;

    // Time since "GO!". Stops when the race is over for the player.
    public float RaceTime
    {
        get
        {
            if (state == RaceState.Countdown) return 0f;
            return (state == RaceState.Over ? raceEndTime : Time.time) - raceStartTime;
        }
    }

    private void Awake()
    {
        foreach (Racer racer in racers)
        {
            if (racer.IsPlayer)
                player = racer;
        }
    }

    private void Start()
    {
        // Nobody may drive during the countdown.
        foreach (Racer racer in racers)
            racer.Car.ControlsEnabled = false;

        StartCoroutine(countdown.Run(StartRace));
    }

    // Called by the countdown at "GO!".
    private void StartRace()
    {
        state = RaceState.Racing;
        raceStartTime = Time.time;
        foreach (Racer racer in racers)
        {
            racer.LapStartTime = 0f;
            racer.Car.ControlsEnabled = true;
        }
        hud.Show();
    }

    public void OnCheckpointPassed(Checkpoint checkpoint, Racer racer)
    {
        if (state == RaceState.Countdown || racer.Finished)
            return;

        // Only the expected checkpoint counts. Driving back and forth over the line does nothing.
        if (checkpoint.Index != racer.NextCheckpoint)
            return;

        racer.LastCheckpoint = checkpoint;
        racer.CheckpointsPassed++;
        racer.NextCheckpoint = (checkpoint.Index + 1) % checkpoints.Length;

        if (checkpoint.Index != 0)
            return;

        // The grid is behind the line, so the first crossing only starts lap 1.
        if (!racer.HasCrossedStartLine)
        {
            racer.HasCrossedStartLine = true;
            return;
        }

        // All checkpoints were passed in order and the car crossed the finish line -> lap complete.
        float now = Time.time - raceStartTime;
        bool newBestLap = racer.AddLap(now - racer.LapStartTime);
        racer.LapStartTime = now;

        if (racer.IsPlayer && newBestLap && state == RaceState.Racing)
            hud.HighlightBestLap();

        if (racer.CompletedLaps >= totalLaps)
            FinishRacer(racer, now);
    }

    private void FinishRacer(Racer racer, float time)
    {
        racer.Finished = true;
        racer.FinishTime = time;
        racer.Car.ControlsEnabled = false;   // controls are taken away, the car rolls out
        finishOrder.Add(racer);

        if (state != RaceState.Racing)
            return;

        // The race is over for the player when they finish, or when an opponent finishes first.
        EndRace(racer.IsPlayer);
    }

    private void EndRace(bool playerWon)
    {
        state = RaceState.Over;
        raceEndTime = Time.time;
        player.Car.ControlsEnabled = false;
        hud.Hide();

        ResultScreen screen = playerWon ? victoryScreen : defeatScreen;
        screen.Show(player, GetPosition(player), racers.Length, RaceTime);
    }

    // 1 = leading. Finished cars are ranked by finish order, all others by checkpoints passed
    // and the distance to their next checkpoint.
    public int GetPosition(Racer racer)
    {
        float progress = Progress(racer);
        int position = 1;
        foreach (Racer other in racers)
        {
            if (other != racer && Progress(other) > progress)
                position++;
        }
        return position;
    }

    private float Progress(Racer racer)
    {
        int finishIndex = finishOrder.IndexOf(racer);
        if (finishIndex >= 0)
            return 1000000f - finishIndex;

        Vector3 next = checkpoints[racer.NextCheckpoint].transform.position;
        return racer.CheckpointsPassed * 1000f - Vector3.Distance(racer.transform.position, next);
    }

    // Restart button: reloading the scene resets cars, laps, times and starts the countdown again.
    public void RestartRace()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // Exit button
    public void ExitGame()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
        Application.Quit();
    }
}
