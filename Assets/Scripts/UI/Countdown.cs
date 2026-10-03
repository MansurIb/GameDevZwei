using System;
using System.Collections;
using TMPro;
using UnityEngine;

// "3 - 2 - 1 - GO!" at the start of the race. Every step is faded in and out.
public class Countdown : MonoBehaviour
{
    [SerializeField] private CanvasFader fader;
    [SerializeField] private TMP_Text label;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip tickClip;
    [SerializeField] private AudioClip goClip;
    [SerializeField] private float startDelay = 1f;
    [SerializeField] private float stepDuration = 1f;
    [SerializeField] private float fadeDuration = 0.2f;
    [SerializeField] private Color numberColor = Color.white;
    [SerializeField] private Color goColor = new Color(0.35f, 1f, 0.45f);

    public IEnumerator Run(Action onGo)
    {
        fader.SetAlpha(0f);
        yield return new WaitForSeconds(startDelay);

        for (int number = 3; number >= 1; number--)
        {
            yield return ShowStep(number.ToString(), numberColor, tickClip, null);
        }
        // The race (and the timer) starts together with "GO!".
        yield return ShowStep("GO!", goColor, goClip, onGo);
    }

    private IEnumerator ShowStep(string text, Color color, AudioClip clip, Action onShow)
    {
        label.text = text;
        label.color = color;
        if (clip != null)
            audioSource.PlayOneShot(clip);
        onShow?.Invoke();

        StartCoroutine(Punch());
        yield return fader.Fade(1f, fadeDuration);
        yield return new WaitForSeconds(stepDuration - 2f * fadeDuration);
        yield return fader.Fade(0f, fadeDuration);
    }

    // Small scale "pop" of the number.
    private IEnumerator Punch()
    {
        float time = 0f;
        while (time < 0.35f)
        {
            time += Time.deltaTime;
            float scale = Mathf.Lerp(1.6f, 1f, Mathf.SmoothStep(0f, 1f, time / 0.35f));
            label.transform.localScale = Vector3.one * scale;
            yield return null;
        }
        label.transform.localScale = Vector3.one;
    }
}
