using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class HandVignetteUI : MonoBehaviour
{
    [Header("Vignettes")]
    public Image leftVignette;
    public Image rightVignette;

    [Header("Settings")]
    public float flashAlpha = 0.6f;
    public float fadeInTime = 0.1f;
    public float fadeOutTime = 0.1f;

    public bool showLeft = false;
    public bool showRight = false;

    public float visibleAlpha = 0.6f;
    private bool leftShowing = false;
    private bool rightShowing = false;

    public void ShowLeft(bool show)
    {
        if (leftShowing == show) return; // Already in this state, do nothing
        
        leftShowing = show;
        Color c = leftVignette.color;
        c.a = show ? visibleAlpha : 0f;
        leftVignette.color = c;
    }

    public void ShowRight(bool show)
    {
        if (rightShowing == show) return; // Already in this state, do nothing
        
        rightShowing = show;
        Color c = rightVignette.color;
        c.a = show ? visibleAlpha : 0f;
        rightVignette.color = c;
    }

    public void FlashLeft()
    {
        leftVignette.DOKill();  // cancel ongoing fades
        leftVignette.DOFade(flashAlpha, fadeInTime);
        leftVignette.DOFade(0f, fadeOutTime).SetDelay(fadeInTime);
    }

    public void FlashRight()
    {
        rightVignette.DOKill();
        rightVignette.DOFade(flashAlpha, fadeInTime);
        rightVignette.DOFade(0f, fadeOutTime).SetDelay(fadeInTime);
    }
}
