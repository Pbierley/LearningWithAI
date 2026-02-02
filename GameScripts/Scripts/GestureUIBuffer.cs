using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class GestureUIBuffer : MonoBehaviour
{
    [SerializeField] private RectTransform bufferContainer;
    [SerializeField] private GridLayoutGroup gridLayoutGroup;
    [SerializeField] private GameObject gestureIconPrefab;
    [SerializeField] private GestureIconLibrary iconLibrary;

    
    public void AddToUIBuffer(GestureLabel left, GestureLabel right)
    {
        CreateIcon(left);
        CreateIcon(right);
        ResizeGrid();

        // Play feedback sound
        AudioManager.Instance.PlayGestureTing();
    }

    private void CreateIcon(GestureLabel gesture)
    {
        var iconObj = Instantiate(gestureIconPrefab, bufferContainer);
        var rect = iconObj.GetComponent<RectTransform>();
        var img = iconObj.GetComponent<Image>();
        img.sprite = iconLibrary.GetIcon(gesture);

        // Start small, like it’s flying in
        rect.localScale = Vector3.zero;

        // “Slam” animation using DOTween (starts big, stops abruptly)
        rect.localScale = Vector3.one * 1.8f; // start larger
        rect.DOScale(1f, 0.12f)
            .SetEase(Ease.OutCubic); // smooth deceleration, but no bounce

        // // “Slam” animation using DOTween
        // rect.DOScale(1.2f, 0.15f)
        //     .SetEase(Ease.OutBack)
        //     .OnComplete(() =>
        //     {
        //         // Small rebound
        //         rect.DOScale(1f, 0.1f).SetEase(Ease.InOutQuad);

        //         // Optional: spawn dust puff here
        //         //SpawnDust(rect);
        //     });
    }

    private void ResizeGrid()
    {
        int count = bufferContainer.childCount;
        float totalWidth = bufferContainer.rect.width;
        float newSize = totalWidth / count;

        float clampedSize = newSize > 100 ? 100 : newSize;

        gridLayoutGroup.cellSize = new Vector2(clampedSize, clampedSize);
    }

    public void ClearUIBuffer()
    {
        foreach (Transform child in bufferContainer)
            Destroy(child.gameObject);
    }
}
