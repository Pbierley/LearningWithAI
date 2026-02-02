using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class LowHealthVignetteUI : MonoBehaviour
{
    [Header("Vignettes")]
    public Image LowHealthVignette;

    [Header("Pulse Settings")]
    // [SerializeField] private float pulseMaxAlpha = 0.8f;
    // [SerializeField] private float pulseDuration = 1f;
    [SerializeField] private float lowHealthPulseDuration = 1f;
    [SerializeField] private float criticalHealthPulseDuration = 0.5f;
    // [SerializeField] private Ease pulseEase = Ease.InOutSine;
    private Sequence currentPulseSequence;

    public void PlayLowHealthVignette()
    {
        PlayDramaticPulse(lowHealthPulseDuration);
    }

    public void PlayUrgentPulse()
    {
        PlayDramaticPulse(criticalHealthPulseDuration);
    }

    public void PlayDramaticPulse(float duration)
    {
        currentPulseSequence?.Kill();

        // Pulse with both fade and scale
        currentPulseSequence = DOTween.Sequence();
        currentPulseSequence.Append(LowHealthVignette.DOFade(0.8f, duration));
        currentPulseSequence.Join(LowHealthVignette.transform.DOScale(1.05f, duration));
        currentPulseSequence.SetEase(Ease.InOutSine);
        currentPulseSequence.SetLoops(-1, LoopType.Yoyo);
    }

    public void StopLowHealthVignette()
    {
        Debug.LogWarning("Stopping low health vignette");
        currentPulseSequence?.Kill();
        LowHealthVignette.DOFade(0f, 0.5f);
        LowHealthVignette.transform.DOScale(1f, 0.5f); // Reset scale
    }


}
