using System.Text;
using TMPro;
using UnityEngine;

// Victory / defeat screen: final position, total time, all lap times and the best lap.
// The Restart and Exit buttons call RaceManager.RestartRace / RaceManager.ExitGame (set in the inspector).
public class ResultScreen : MonoBehaviour
{
    [SerializeField] private CanvasFader fader;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text lapTimesText;
    [SerializeField] private TMP_Text bestLapText;

    public void Show(Racer player, int position, int racerCount, float finalTime)
    {
        positionText.text = $"Position  P{position} / {racerCount}";
        totalTimeText.text = $"Total time  {RaceHUD.FormatTime(finalTime)}";

        StringBuilder laps = new StringBuilder();
        for (int i = 0; i < player.LapTimes.Count; i++)
        {
            bool best = Mathf.Approximately(player.LapTimes[i], player.BestLap);
            laps.Append($"Lap {i + 1}   {RaceHUD.FormatTime(player.LapTimes[i])}");
            laps.AppendLine(best ? "  <color=#6CFF7A>best</color>" : "");
        }
        if (player.LapTimes.Count == 0)
            laps.AppendLine("No lap completed");
        lapTimesText.text = laps.ToString();

        bestLapText.text = player.HasBestLap ? $"Best lap  {RaceHUD.FormatTime(player.BestLap)}" : "Best lap  --:--.---";
        fader.FadeIn();
    }
}
