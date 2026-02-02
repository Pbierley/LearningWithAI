using UnityEngine;
using UnityEngine.UI;

public class CircleColorController : MonoBehaviour
{
    [SerializeField] private SpriteRenderer[] circles;   // assign via Inspector
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.white;
    [SerializeField] private  int index = 0; // which circle to activate next start at 1 and go to 3

    // We want this to be called once a second to turn the next circle active (green)
    // Activate the next circle in sequence. Returns true if a circle was activated,
    // false if there are no more circles to activate.
    public bool ActivateNext()
    {
        if (circles == null || circles.Length == 0) return false;

        if (index < 0) index = 0;
        if (index >= circles.Length) return false; // already complete

        circles[index].color = activeColor;
        index++;
        return true;
    }

    // Call this whenever we want to reset all circles to inactive (white)
    public void ResetColors()
    {
        foreach (var c in circles)
            c.color = inactiveColor;
    }

    // Reset the activation progress (set all circles inactive and start from first)
    public void ResetProgress()
    {
        index = 0;
        ResetColors();
    }
}
