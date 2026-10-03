using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// In-race HUD: speed (text + gauge), lap, position, lap time, total time and best lap.
public class RaceHUD : MonoBehaviour
{
    [SerializeField] private RaceManager raceManager;
    [SerializeField] private CanvasFader fader;

    [Header("Texts")]
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text lapText;
    [SerializeField] private TMP_Text positionText;
    [SerializeField] private TMP_Text lapTimeText;
    [SerializeField] private TMP_Text totalTimeText;
    [SerializeField] private TMP_Text bestLapText;

    [Header("Speedometer")]
    [SerializeField] private Image gaugeFill;            // radial filled image
    [SerializeField] private RectTransform needle;
    [SerializeField] private float gaugeMaxKmh = 150f;
    [SerializeField] private float gaugeAngle = 270f;

    [Header("Best lap highlight")]
    [SerializeField] private Color bestLapColor = Color.white;
    [SerializeField] private Color highlightColor = new Color(0.3f, 1f, 0.4f);

    private Coroutine highlight;

    public void Show()
    {
        fader.FadeIn();
    }

    public void Hide()
    {
        fader.FadeOut();
    }

    private void Update()
    {
        Racer player = raceManager.Player;

        float kmh = player.Car.Speed * 3.6f;
        speedText.text = Mono(Mathf.RoundToInt(kmh).ToString());
        float gauge01 = Mathf.Clamp01(kmh / gaugeMaxKmh);
        gaugeFill.fillAmount = gauge01 * gaugeAngle / 360f;
        needle.localEulerAngles = new Vector3(0f, 0f, gaugeAngle * 0.5f - gauge01 * gaugeAngle);

        int lap = Mathf.Min(player.CompletedLaps + 1, raceManager.TotalLaps);
        lapText.text = $"Lap {lap} / {raceManager.TotalLaps}";
        positionText.text = $"P{raceManager.GetPosition(player)} / {raceManager.RacerCount}";

        float lapTime = player.Finished ? player.LapTimes[player.LapTimes.Count - 1] : raceManager.RaceTime - player.LapStartTime;
        lapTimeText.text = Mono(FormatTime(lapTime));
        totalTimeText.text = Mono(FormatTime(raceManager.RaceTime));
        bestLapText.text = Mono(player.HasBestLap ? FormatTime(player.BestLap) : "--:--.---");
    }

    // Called by the RaceManager whenever the player sets a new best lap.
    public void HighlightBestLap()
    {
        if (highlight != null)
            StopCoroutine(highlight);
        highlight = StartCoroutine(HighlightRoutine());
    }

    private IEnumerator HighlightRoutine()
    {
        const float duration = 2f;
        float time = 0f;
        while (time < duration)
        {
            time += Time.deltaTime;
            float t = time / duration;
            bestLapText.color = Color.Lerp(highlightColor, bestLapColor, t * t);
            float pop = t < 0.15f ? Mathf.Lerp(1f, 1.4f, t / 0.15f) : Mathf.Lerp(1.4f, 1f, (t - 0.15f) / 0.35f);
            bestLapText.transform.localScale = Vector3.one * Mathf.Max(1f, pop);
            yield return null;
        }
        bestLapText.color = bestLapColor;
        bestLapText.transform.localScale = Vector3.one;
    }

    // mm:ss.mmm (built from integers so the decimal separator does not depend on the system language)
    public static string FormatTime(float seconds)
    {
        int totalMilliseconds = Mathf.Max(0, Mathf.RoundToInt(seconds * 1000f));
        int minutes = totalMilliseconds / 60000;
        int secs = totalMilliseconds / 1000 % 60;
        int millis = totalMilliseconds % 1000;
        return $"{minutes:00}:{secs:00}.{millis:000}";
    }

    // Monospaced digits, so the text does not jump around while the numbers change.
    private static string Mono(string text)
    {
        return $"<mspace=0.6em>{text}</mspace>";
    }
}
