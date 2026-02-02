using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class EnemyGestureDisplay : MonoBehaviour
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
    }

    private void CreateIcon(GestureLabel gesture)
    {
        var iconObj = Instantiate(gestureIconPrefab, bufferContainer);
        var img = iconObj.GetComponent<Image>();
        img.sprite = iconLibrary.GetIcon(gesture);

        // 🔴 Tint red
        img.color = new Color(1f, 0.3f, 0.3f, 1f);
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
