#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using TMPro;

public class ReplaceTMPFonts : MonoBehaviour
{
    [MenuItem("Tools/Replace TMP Fonts In Scene")]
    public static void ReplaceFonts()
    {
        TMP_FontAsset newFont = Selection.activeObject as TMP_FontAsset;
        if (newFont == null)
        {
            Debug.LogError("Select a TMP_FontAsset in the Project first.");
            return;
        }

        TMP_Text[] texts = GameObject.FindObjectsByType<TMP_Text>(FindObjectsSortMode.None);
        int count = 0;

        foreach (var t in texts)
        {
            Undo.RecordObject(t, "Font Replace");
            t.font = newFont;
            count++;
        }

        Debug.Log("Replaced fonts on " + count + " TextMeshPro components.");
    }
}
#endif
