using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class HealthBar : MonoBehaviour
{
    [SerializeField] private Slider frontBar;
    [SerializeField] private Slider backBar;
    [SerializeField] private float delay = 0.5f;
    [SerializeField] private float tweenTime = 0.4f;

    private Tween backTween;
    private Tween frontTween;

    public void InitHealth(float health)
    {
        frontBar.maxValue = health;
        frontBar.value = health;

        backBar.maxValue = health;
        backBar.value = health;
    }

    public void SetHealth(float health)
    {
        // See if its gaining or losing health
        bool gainingHealth = health > frontBar.value;

        // Flash bar on damage
        // frontBar.transform.DOScale(1.1f, 0.1f).From();
        // backBar.transform.DOScale(1.1f, 0.1f).From();

        // Kill any old tween to prevent stacking
        backTween?.Kill();
        frontTween?.Kill();

        // Start after a delay, then animate back bar down
        if (gainingHealth)
        {
            // Flash bar on gaining health
            frontBar.transform.DOScale(1.1f, 0.1f).From();
            backBar.transform.DOScale(1.1f, 0.1f).From();

            // Set back bar first, then animate front bar
            backBar.value = health;

            // Animate front bar
            frontTween = DOVirtual.Float(
                frontBar.value,
                health,
                tweenTime,
                v => frontBar.value = v
            )
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);
        }
        else
        {
                        // Flash bar on gaining health
            frontBar.transform.DOScale(1.1f, 0.1f).From();
            backBar.transform.DOScale(1.1f, 0.1f).From();
            
            // Set front bar first, then animate back bar
            frontBar.value = health;

            // Animate back bar
            backTween = DOVirtual.Float(
                backBar.value,
                health,
                tweenTime,
                v => backBar.value = v
            )
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);
        }
    }


}
