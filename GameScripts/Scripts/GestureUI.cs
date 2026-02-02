using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class GestureUI : MonoBehaviour
{
    [SerializeField] private Image leftImage;
    [SerializeField] private Image rightImage;
    [SerializeField] private GestureIconLibrary iconLibrary;


    public void ShowGesture(GestureLabel left, GestureLabel right)
    {
        leftImage.sprite = iconLibrary.GetIcon(left);
        rightImage.sprite = iconLibrary.GetIcon(right);

        AnimateThump(leftImage.transform);
        AnimateThump(rightImage.transform);
    }


    private void AnimateThump(Transform t)
    {
        // Cancel any ongoing tweens
        t.DOKill();

        // Reset to base scale just in case it drifted
        t.localScale = Vector3.one;

        // Do a clean “scale up then down” sequence (like your coroutine)
        t.DOScale(1.2f, 0.1f).SetEase(Ease.OutQuad)
        .OnComplete(() => t.DOScale(1f, 0.1f).SetEase(Ease.InQuad));
    }


}
