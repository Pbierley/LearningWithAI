
using System.Collections.Generic;

public static class GestureMapper
{
    private static readonly Dictionary<string, GestureLabel> map = new()
    {
        { "None", GestureLabel.None },
        { "Closed_Fist", GestureLabel.ClosedFist },
        { "Open_Palm", GestureLabel.OpenPalm },
        { "Pointing_Up", GestureLabel.PointingUp },
        { "Thumb_Down", GestureLabel.ThumbsDown },
        { "Thumb_Up", GestureLabel.ThumbsUp },
        { "Victory", GestureLabel.Victory },
        { "ILoveYou", GestureLabel.ILoveYou }
    };

    public static GestureLabel ToEnum(string gestureName)
    {
        if (gestureName != null && map.TryGetValue(gestureName, out var label))
            return label;

        return GestureLabel.None;
    }

}