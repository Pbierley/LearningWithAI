using UnityEngine;

[CreateAssetMenu(fileName = "GestureIconLibrary", menuName = "UI/Gesture Icon Library")]
public class GestureIconLibrary : ScriptableObject
{
    [Header("Gesture Icons")]
    public Sprite openPalmIcon;
    public Sprite closedFistIcon;
    public Sprite pointingUpIcon;
    public Sprite iLoveYouIcon;
    public Sprite thumbsUpIcon;
    public Sprite thumbsDownIcon;
    public Sprite victoryIcon;

    public Sprite GetIcon(GestureLabel gesture) => gesture switch
    {
        GestureLabel.OpenPalm => openPalmIcon,
        GestureLabel.ClosedFist => closedFistIcon,
        GestureLabel.ILoveYou => iLoveYouIcon,
        GestureLabel.PointingUp => pointingUpIcon,
        GestureLabel.ThumbsDown => thumbsDownIcon,
        GestureLabel.ThumbsUp => thumbsUpIcon,
        GestureLabel.Victory => victoryIcon,
        _ => null
    };
}
