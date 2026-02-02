using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using TMPro;
using UnityEditor.SceneManagement;

public class IntroSceneSetup : EditorWindow
{
    [MenuItem("Tools/Setup Intro Scene")]
    public static void SetupIntroScene()
    {
        // Open Intro scene
        string scenePath = "Assets/Scenes/Intro.unity";
        var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

        // Find or create EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            var eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        // Create Canvas
        GameObject canvasObj = new GameObject("IntroCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        
        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.matchWidthOrHeight = 0.5f;
        
        canvasObj.AddComponent<GraphicRaycaster>();

        // Create black background
        GameObject bgObj = new GameObject("Background");
        bgObj.transform.SetParent(canvasObj.transform, false);
        Image bgImage = bgObj.AddComponent<Image>();
        bgImage.color = Color.black;
        RectTransform bgRect = bgObj.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // Load font
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/Fonts/THE_ALISTAREN SDF.asset");
        if (font == null)
        {
            Debug.LogWarning("Could not find THE_ALISTAREN font, using default TMP font");
        }

        // Create main text
        GameObject mainTextObj = new GameObject("MainText");
        mainTextObj.transform.SetParent(canvasObj.transform, false);
        TMP_Text mainText = mainTextObj.AddComponent<TextMeshProUGUI>();
        mainText.text = "";
        mainText.fontSize = 48;
        mainText.alignment = TextAlignmentOptions.Center;
        mainText.color = Color.white;
        if (font != null) mainText.font = font;
        
        RectTransform mainTextRect = mainTextObj.GetComponent<RectTransform>();
        mainTextRect.anchorMin = new Vector2(0.1f, 0.35f);
        mainTextRect.anchorMax = new Vector2(0.9f, 0.65f);
        mainTextRect.offsetMin = Vector2.zero;
        mainTextRect.offsetMax = Vector2.zero;

        // Create title text (larger, for SPELL ARENA)
        GameObject titleTextObj = new GameObject("TitleText");
        titleTextObj.transform.SetParent(canvasObj.transform, false);
        TMP_Text titleText = titleTextObj.AddComponent<TextMeshProUGUI>();
        titleText.text = "";
        titleText.fontSize = 96;
        titleText.alignment = TextAlignmentOptions.Center;
        titleText.color = new Color(1f, 0.9f, 0.6f); // Warm golden color
        titleText.fontStyle = FontStyles.Bold;
        if (font != null) titleText.font = font;
        
        RectTransform titleTextRect = titleTextObj.GetComponent<RectTransform>();
        titleTextRect.anchorMin = new Vector2(0.1f, 0.4f);
        titleTextRect.anchorMax = new Vector2(0.9f, 0.6f);
        titleTextRect.offsetMin = Vector2.zero;
        titleTextRect.offsetMax = Vector2.zero;

        // Create IntroManager
        GameObject managerObj = new GameObject("IntroManager");
        IntroManager manager = managerObj.AddComponent<IntroManager>();

        // Load audio clip
        AudioClip introMusic = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Sounds/intro-theme.mp3");
        
        // Use SerializedObject to set private serialized fields
        SerializedObject so = new SerializedObject(manager);
        so.FindProperty("mainText").objectReferenceValue = mainText;
        so.FindProperty("titleText").objectReferenceValue = titleText;
        if (introMusic != null)
        {
            so.FindProperty("introMusic").objectReferenceValue = introMusic;
        }
        else
        {
            Debug.LogWarning("Could not find intro-theme.mp3 in Assets/Sounds/");
        }
        so.ApplyModifiedProperties();

        // Create Camera (if none exists)
        if (Object.FindFirstObjectByType<Camera>() == null)
        {
            GameObject camObj = new GameObject("Main Camera");
            camObj.tag = "MainCamera";
            Camera cam = camObj.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camObj.AddComponent<AudioListener>();
        }

        // Mark scene dirty so it can be saved
        EditorSceneManager.MarkSceneDirty(scene);
        
        Debug.Log("Intro scene setup complete! Don't forget to save the scene (Ctrl+S).");
        
        // Select the manager so user can see it
        Selection.activeGameObject = managerObj;
    }
}

