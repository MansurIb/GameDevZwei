using System.Collections;
using UnityEngine;

// Fades a whole canvas in and out through its CanvasGroup.
[RequireComponent(typeof(CanvasGroup))]
public class CanvasFader : MonoBehaviour
{
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private bool visibleAtStart;

    private CanvasGroup group;
    private Coroutine running;

    private void Awake()
    {
        group = GetComponent<CanvasGroup>();
        SetAlpha(visibleAtStart ? 1f : 0f);
    }

    public void FadeIn()
    {
        StartFade(1f, fadeDuration);
    }

    public void FadeOut()
    {
        StartFade(0f, fadeDuration);
    }

    public void StartFade(float targetAlpha, float duration)
    {
        if (running != null)
            StopCoroutine(running);
        running = StartCoroutine(Fade(targetAlpha, duration));
    }

    public IEnumerator Fade(float targetAlpha, float duration)
    {
        float startAlpha = group.alpha;
        float time = 0f;
        while (time < duration)
        {
            time += Time.unscaledDeltaTime;
            SetAlpha(Mathf.Lerp(startAlpha, targetAlpha, time / duration));
            yield return null;
        }
        SetAlpha(targetAlpha);
    }

    public void SetAlpha(float alpha)
    {
        group.alpha = alpha;
        // Buttons only react while the canvas is (mostly) visible.
        group.interactable = alpha > 0.5f;
        group.blocksRaycasts = alpha > 0.5f;
    }
}
