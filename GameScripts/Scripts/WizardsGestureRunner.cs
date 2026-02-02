// WizardGestureRunner.cs
// A simplified MonoBehaviour runner for MediaPipe GestureRecognizer.
// Includes public config variables (Inspector friendly) and UnityEvent for gameplay integration.

using System.Collections;
using Mediapipe;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using Mediapipe.Unity;
using Mediapipe.Unity.Sample;
using Tasks = Mediapipe.Tasks;
using Experimental = Mediapipe.Unity.Experimental;
using AssetLoader = Mediapipe.Unity.Sample.AssetLoader;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

public class WizardGestureRunner : MonoBehaviour
{
    [Header("Game Settings")]
    [Tooltip("Called whenever a gesture is recognized. Passes gesture label as string.")]
    public UnityEvent<string> OnGestureRecognized;

    [Header("Model Settings")]
    [Tooltip("Gesture recognizer model file. Must exist in Assets/StreamingAssets/")]
    public string modelFile = "gesture_recognizer.task";

    [Header("Runtime Settings")]
    [Tooltip("How frames are passed from Unity to MediaPipe.")]
    public ImageReadMode imageReadMode = ImageReadMode.CPUAsync;

    [Tooltip("Mode of operation: IMAGE, VIDEO, or LIVE_STREAM.")]
    public Tasks.Vision.Core.RunningMode runningMode = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

    [Tooltip("How many hands to detect at once.")]
    [Range(1, 2)] public int numHands = 1;

    [Tooltip("Target FPS for recognition. Limits how often frames are sent to MediaPipe.")]
    [Range(5, 60)] public int targetFps = 15;

    // --- Internals ---

    private WebCamTexture webcamTexture;         // Unity webcam feed
    private GestureRecognizer recognizer;        // MediaPipe recognizer
    private Experimental.TextureFramePool pool;  // Reusable frame pool
    private GpuResources gpuResources;           // GPU context (if available)
    private GlContext glContext;                 // OpenGL context (for GPU mode)

    private void Start()
    {
        // Kick off initialization in coroutine (async-friendly)
        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        // --- Step 1. Prepare the model file ---
        string modelPath = System.IO.Path.Combine(Application.streamingAssetsPath, modelFile);
        //yield return Mediapipe.Unity.Sample.AssetLoader.PrepareAssetAsync(modelPath); // makes model accessible on all platforms
        if (!System.IO.File.Exists(modelPath)) {
            Debug.LogError("Model file not found at: " + modelPath);
            yield break;
        }

        // --- Step 2. GPU setup ---
        gpuResources = GpuManager.GpuResources;
        bool canUseGpu = (SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && gpuResources != null);
        if (canUseGpu)
        {
            glContext = GpuManager.GetGlContext();
        }

        // --- Step 3. Create GestureRecognizer instance ---
    var baseOptions = new Tasks.Core.BaseOptions(delegateCase: Tasks.Core.BaseOptions.Delegate.CPU, modelAssetPath: modelPath);
        var options = new GestureRecognizerOptions(baseOptions, runningMode: runningMode, numHands: numHands,
            resultCallback: (runningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM) ? OnGestureOutput : null);

        recognizer = GestureRecognizer.CreateFromOptions(options, gpuResources);
        Debug.Log("GestureRecognizer initialized with model: " + modelPath);

        // --- Step 4. Start webcam ---
        webcamTexture = new WebCamTexture();
        webcamTexture.Play();

        // Wait for the webcam to initialize and report a valid resolution.
        float startWait = Time.realtimeSinceStartup;
        float timeout = 5f; // seconds
        while (webcamTexture.width <= 16 || webcamTexture.height <= 16)
        {
            if (Time.realtimeSinceStartup - startWait > timeout)
            {
                Debug.LogError($"Webcam failed to initialize within {timeout} seconds. width={webcamTexture.width} height={webcamTexture.height}");
                yield break;
            }
            yield return null;
        }

        if (!webcamTexture.isPlaying)
        {
            Debug.LogError("Failed to start webcam.");
            yield break;
        }

        Debug.Log($"Webcam initialized: {webcamTexture.width}x{webcamTexture.height}, mirrored={webcamTexture.videoVerticallyMirrored}, rotation={webcamTexture.videoRotationAngle}");

        // --- Step 5. Frame pool (efficient texture reuse) ---
        // Use the actual webcam resolution when creating the pool to avoid stretched/rotated frames.
        pool = new Experimental.TextureFramePool(webcamTexture.width, webcamTexture.height, TextureFormat.RGBA32, 10);

        // --- Step 6. Main loop ---
        WaitForSeconds delay = new WaitForSeconds(1f / targetFps);
        AsyncGPUReadbackRequest req = default;
        var waitUntilReqDone = new WaitUntil(() => req.done);
        var waitForEndOfFrame = new WaitForEndOfFrame();

        while (true)
        {
            yield return delay;

            // Grab a frame slot from the pool
            if (!pool.TryGetTextureFrame(out var textureFrame))
            {
                yield return waitForEndOfFrame;
                continue;
            }

            // --- Step 7. Convert Unity webcam texture → MediaPipe Image ---
            Image image = null;
            switch (imageReadMode)
            {
                case ImageReadMode.GPU:
                    if (!canUseGpu)
                    {
                        Debug.LogWarning("GPU mode not supported, falling back to CPUAsync.");
                        imageReadMode = ImageReadMode.CPUAsync;
                        goto case ImageReadMode.CPUAsync;
                    }
                    // Honor webcam mirroring/rotation when uploading to GPU/MediaPipe.
                    bool flipH = webcamTexture.videoRotationAngle == 180 || webcamTexture.videoRotationAngle == 0 ? webcamTexture.videoVerticallyMirrored : !webcamTexture.videoVerticallyMirrored;
                    bool flipV = false;
                    textureFrame.ReadTextureOnGPU(webcamTexture, flipHorizontally: flipH, flipVertically: flipV);
                    image = textureFrame.BuildGPUImage(glContext);
                    yield return waitForEndOfFrame; // ensure GPU upload is done
                    break;

                case ImageReadMode.CPU:
                    yield return waitForEndOfFrame; // wait until frame rendered
                    // Honor webcam mirroring/rotation when reading CPU textures.
                    bool cpuFlipH = webcamTexture.videoRotationAngle == 180 || webcamTexture.videoRotationAngle == 0 ? webcamTexture.videoVerticallyMirrored : !webcamTexture.videoVerticallyMirrored;
                    bool cpuFlipV = false;
                    textureFrame.ReadTextureOnCPU(webcamTexture, flipHorizontally: cpuFlipH, flipVertically: cpuFlipV);
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;

                case ImageReadMode.CPUAsync:
                default:
                    bool asyncFlipH = webcamTexture.videoRotationAngle == 180 || webcamTexture.videoRotationAngle == 0 ? webcamTexture.videoVerticallyMirrored : !webcamTexture.videoVerticallyMirrored;
                    bool asyncFlipV = false;
                    req = textureFrame.ReadTextureAsync(webcamTexture, flipHorizontally: asyncFlipH, flipVertically: asyncFlipV);
                    yield return waitUntilReqDone;
                    if (req.hasError)
                    {
                        Debug.LogWarning("Async GPU readback failed.");
                        continue;
                    }
                    image = textureFrame.BuildCPUImage();
                    textureFrame.Release();
                    break;
            }

            // --- Step 8. Run recognition ---
            switch (runningMode)
            {
                case Tasks.Vision.Core.RunningMode.IMAGE:
                    var imageResult = recognizer.Recognize(image);
                    ProcessResult(imageResult);
                    break;

                case Tasks.Vision.Core.RunningMode.VIDEO:
                    var videoResult = recognizer.RecognizeForVideo(image, GetCurrentTimestampMillisec());
                    ProcessResult(videoResult);
                    break;

                case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
                    recognizer.RecognizeAsync(image, GetCurrentTimestampMillisec());
                    break;
            }
        }
    }

    // Called only in LIVE_STREAM mode (async results)
    private void OnGestureOutput(GestureRecognizerResult result, Image image, long timestamp)
    {
        ProcessResult(result);
    }

    // Extracts top gesture label and fires UnityEvent
    private void ProcessResult(GestureRecognizerResult result)
    {
        if (result.gestures == null || result.gestures.Count == 0)
            return;

        var topCategory = result.gestures[0].categories[0];
        string gestureName = topCategory.categoryName;
        float confidence = topCategory.score;

        Debug.Log($"Gesture: {gestureName} ({confidence:P1})");

        // Fire UnityEvent for gameplay scripts
        OnGestureRecognized?.Invoke(gestureName);
    }

    private long GetCurrentTimestampMillisec()
    {
        return (long)(Time.realtimeSinceStartup * 1000);
    }

    private void OnDestroy()
    {
        // Cleanup
        webcamTexture?.Stop();
        recognizer?.Close();
        pool?.Dispose();
        glContext?.Dispose();
    }
}
