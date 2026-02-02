using UnityEngine;
using TMPro;
using Mediapipe.Unity.Sample.GestureRecognition;
using Mediapipe.Unity.Sample;
using System.Collections;

public class DebugFpsDisplay : MonoBehaviour
{
    [Header("UI Reference")]
    [SerializeField] private TMP_Text fpsText;   
    [SerializeField] private GestureRecognizerRunner runner;

    // These will be updated externally
    [HideInInspector] public float mediapipeInputFps;
    [HideInInspector] public float mediapipeOutputFps;

    private float gameFps;
    private float gameTimer;
    private int gameFrameCount;

    // Camera tracking
    private WebCamTexture webcamTex;
    private int camFrameCount;
    private float camTimer;
    private float cameraFps;

    void Start()
    {
        StartCoroutine(WaitForCamera());
    }

    private IEnumerator WaitForCamera()
    {
        // Wait until MediaPipe’s ImageSource is available
        while (ImageSourceProvider.ImageSource == null)
            yield return null;

        // Wait until MediaPipe finishes starting its WebCamTexture
        var src = ImageSourceProvider.ImageSource;
        WebCamTexture tex = null;

        // Keep checking until GetCurrentTexture() actually returns a WebCamTexture
        while (tex == null)
        {
            tex = src.GetCurrentTexture() as WebCamTexture;
            yield return null;
        }

        webcamTex = tex;
        Debug.Log($"[DebugFpsDisplay] Found webcam texture: {webcamTex.deviceName}");
    }


    void Update()
    {
        // Pull MediaPipe stats from the runner
        mediapipeInputFps = runner.inputFps;
        mediapipeOutputFps = runner.outputFps;

        // --- Game FPS ---
        gameFrameCount++;
        gameTimer += Time.unscaledDeltaTime;
        if (gameTimer >= 1f)
        {
            gameFps = gameFrameCount / gameTimer;
            gameFrameCount = 0;
            gameTimer = 0f;
        }

        // --- Camera FPS ---
        if (webcamTex != null && webcamTex.didUpdateThisFrame)
            camFrameCount++;

        camTimer += Time.unscaledDeltaTime;
        if (camTimer >= 1f)
        {
            cameraFps = camFrameCount / camTimer;
            camFrameCount = 0;
            camTimer = 0f;
        }

        // Update display periodically
        if (!Application.isEditor && !Debug.isDebugBuild) return;
        UpdateUI();
    }

    private void UpdateUI()
    {
        if (fpsText == null) return;

        // Choose colors per metric
        string gameColor   = GetColorTag(gameFps, 30, 200);
        string inputColor  = GetColorTag(mediapipeInputFps, 15, 60);
        string camColor    = GetColorTag(cameraFps, 20, 28);
        string outputColor = GetColorTag(mediapipeOutputFps, 20, 28);

        fpsText.text =
            $"<b>Game:</b> <color={gameColor}>{gameFps:F1}</color> FPS\n" +
            $"<b>MP Input:</b> <color={inputColor}>{mediapipeInputFps:F1}</color> FPS\n" +
            $"<b>Camera:</b> <color={camColor}>{cameraFps:F1}</color> FPS\n" +
            $"<b>MP Output:</b> <color={outputColor}>{mediapipeOutputFps:F1}</color> FPS";

    }

    private string GetColorTag(float fps, float low, float high)
    {
        if (fps < low)
            return "#FF4D4D";   // red
        else if (fps < high)
            return "#FFD966";   // yellow
        else
            return "#6BFF6B";   // green
    }

}
