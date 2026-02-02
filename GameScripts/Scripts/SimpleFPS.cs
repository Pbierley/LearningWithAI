#if UNITY_EDITOR
using UnityEngine;

public class SimpleFPS : MonoBehaviour
{
    private float deltaTime;
    public bool showFPS = false;

    void Update()
    {
        // Toggle visibility with F3
        if (Input.GetKeyDown(KeyCode.F3))
            showFPS = !showFPS;

        // Smooth FPS calculation
        deltaTime += (Time.deltaTime - deltaTime) * 0.1f;
    }

    void OnGUI()
    {
        if (!showFPS) return;

        int w = Screen.width, h = Screen.height;
        GUIStyle style = new GUIStyle
        {
            alignment = TextAnchor.UpperCenter,
            fontSize = h * 2 / 100,
            normal = { textColor = Color.white }
        };

        float fps = 1.0f / deltaTime;
        string text = $"{fps:0.} FPS";

        // Optional background box
        Color originalColor = GUI.color;
        GUI.color = new Color(0, 0, 0, 0.4f);
        GUI.Box(new Rect(10, 10, 80, 30), GUIContent.none);
        GUI.color = originalColor;

        GUI.Label(new Rect(15, 10, w, h * 2 / 100), text, style);
    }
}
#endif
