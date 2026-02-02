using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class ManaBar : MonoBehaviour
{
    [SerializeField] private Slider frontBar;
    [SerializeField] private Slider backBar;
    [SerializeField] private float delay = 0.5f;
    [SerializeField] private float tweenTime = 0.4f;
    private Tween backTween;

    public void InitMana(float mana)
    {
        frontBar.maxValue = mana;
        frontBar.value = mana;

        backBar.maxValue = mana;
        backBar.value = mana;
    }

    public void SetMana(float mana)
    {
        // Clamp to valid range and update the front bar instantly
        float clamped = Mathf.Clamp(mana, 0f, frontBar.maxValue);

        // Want to animate differently if gaining vs subtracting mana
        bool gainingMana = mana > frontBar.value;

        //Changing the Front (Dark bar)
        frontBar.value = clamped;

        backTween?.Kill();

        // Start after a delay, then animate back bar down
        if (gainingMana)
        {
            // Changing back bar immediately, no lag
            backBar.value = clamped;
        }
        else
        {
            backTween = DOVirtual.Float(
                backBar.value,
                clamped,
                tweenTime,
                v => backBar.value = v
            )
            .SetDelay(delay)
            .SetEase(Ease.OutQuad);
        }
    }
}
