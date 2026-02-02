// GestureRecognizerRunner.cs
// Based on homuler's HandLandmarkerRunner sample, adapted for GestureRecognizer
// MIT License (c) 2023 homuler

using System.Collections;
using System.Diagnostics;
using System.Threading;
using Mediapipe;
using Mediapipe.Tasks.Vision.GestureRecognizer;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;
using Debug = UnityEngine.Debug;

namespace Mediapipe.Unity.Sample.GestureRecognition
{
  /// <summary>
  /// Configuration for GestureRecognizer.
  /// Exposed as public fields so you can tweak them in the Inspector.
  /// </summary>
  [System.Serializable]
  public class GestureRecognizerConfig
  {
    [Header("General")]
    public Tasks.Core.BaseOptions.Delegate Delegate =
#if UNITY_EDITOR_WIN || UNITY_STANDALONE_WIN || UNITY_EDITOR_OSX || UNITY_STANDALONE_OSX
      Tasks.Core.BaseOptions.Delegate.CPU; // Force CPU on desktop
#else
      Tasks.Core.BaseOptions.Delegate.GPU; // GPU on mobile if available
#endif

    [Tooltip("How to read frames from Unity into MediaPipe")]
    public ImageReadMode ImageReadMode = ImageReadMode.CPUAsync;

    [Tooltip("Running mode: IMAGE (single frame), VIDEO (sequential frames), or LIVE_STREAM (async callback).")]
    public Tasks.Vision.Core.RunningMode RunningMode = Tasks.Vision.Core.RunningMode.LIVE_STREAM;

    [Header("Model")]
    [Tooltip("Gesture recognizer model file (must exist in StreamingAssets).")]
    public string ModelPath = "gesture_recognizer.task";

    [Header("Other")]
    [Tooltip("How many hands to detect at once.")]
    public int NumHands = 2;
  

    public GestureRecognizerOptions GetGestureRecognizerOptions(GestureRecognizerOptions.ResultCallback resultCallback = null)
    {
      var baseOptions = new Tasks.Core.BaseOptions(Delegate, modelAssetPath: ModelPath);

      return new GestureRecognizerOptions(
        baseOptions,
        runningMode: RunningMode,
        numHands: NumHands,
        resultCallback: resultCallback
      );
    }
  }

  /// <summary>
  /// Runner that drives the GestureRecognizer task.
  /// Inherits from VisionTaskApiRunner so lifecycle matches MediaPipe samples.
  /// </summary>
  public class GestureRecognizerRunner : VisionTaskApiRunner<GestureRecognizer>
  {
    [Header("Configuration")]
    public GestureRecognizerConfig config = new GestureRecognizerConfig();

    [Header("Events")]
    [Tooltip("Triggered when a gesture is recognized. Sends the gesture label string (hand:gestureName, hand:gestureName).")]
    public UnityEvent<string, string> OnGestureRecognized;

    private Experimental.TextureFramePool _textureFramePool;

    [Header("UI FPS Debug")]
    //public DebugFpsDisplay debugFpsDisplay; // drag a UI Text or TMP_Text in the Inspector
    private int inputFrameCount = 0;
    private float inputTimer = 0f;
    public volatile float inputFps = 0f;
    private int outputFrameCount = 0;
    // private float outputTimer = 0f;
    public volatile float outputFps = 0f;
    private Stopwatch outputStopwatch;

    float inputTargetFps = 45f;


    /// <summary>
    /// Pauses gesture recognition. Called by MatchManager during non-fighting states.
    /// </summary>
    public void PauseRecognition()
    {
        isPaused = true;
        Debug.Log("[GestureRecognizerRunner] Paused");
    }

    /// <summary>
    /// Resumes gesture recognition. Called by MatchManager when fighting begins.
    /// </summary>
    public void ResumeRecognition()
    {
        isPaused = false;
        Debug.Log("[GestureRecognizerRunner] Resumed");
    }

    public override void Stop()
    {
      base.Stop();
      _textureFramePool?.Dispose();
      _textureFramePool = null;
    }

    protected override IEnumerator Run()
    {
      outputStopwatch = new Stopwatch();
      outputStopwatch.Start();

      // --- Step 1: Debug log config ---
      Debug.Log($"Delegate = {config.Delegate}");
      Debug.Log($"Image Read Mode = {config.ImageReadMode}");
      Debug.Log($"Running Mode = {config.RunningMode}");
      Debug.Log($"NumHands = {config.NumHands}");

      // --- Step 2: Ensure model is available ---
      yield return AssetLoader.PrepareAssetAsync(config.ModelPath);

      // --- Step 3: Create recognizer ---
      var options = config.GetGestureRecognizerOptions(
        config.RunningMode == Tasks.Vision.Core.RunningMode.LIVE_STREAM ? OnGestureOutput : null
      );
      taskApi = GestureRecognizer.CreateFromOptions(options, GpuManager.GpuResources);

      // --- Step 4: Start camera input ---
      var imageSource = ImageSourceProvider.ImageSource;
      yield return imageSource.Play();

      if (!imageSource.isPrepared)
      {
        Debug.LogError("Failed to start ImageSource, exiting...");
        yield break;
      }

      // --- Step 5: Setup frame pool ---
      _textureFramePool = new Experimental.TextureFramePool(
        imageSource.textureWidth,
        imageSource.textureHeight,
        TextureFormat.RGBA32,
        10
      );

      // Screen for aspect ratio
      //screen.Initialize(imageSource);

      // Flip/rotation info
      var transformationOptions = imageSource.GetTransformationOptions();
      var flipHorizontally = transformationOptions.flipHorizontally;
      var flipVertically = transformationOptions.flipVertically;
      var imageProcessingOptions = new Tasks.Vision.Core.ImageProcessingOptions(
        rotationDegrees: (int)transformationOptions.rotationAngle
      );

      AsyncGPUReadbackRequest req = default;
      var waitUntilReqDone = new WaitUntil(() => req.done);
      var waitForEndOfFrame = new WaitForEndOfFrame();

      // GPU check
      var canUseGpuImage = SystemInfo.graphicsDeviceType == GraphicsDeviceType.OpenGLES3 && GpuManager.GpuResources != null;
      using var glContext = canUseGpuImage ? GpuManager.GetGlContext() : null;

      // --- Step 6: Main loop ---
      while (true)
      {
        if (isPaused)
        {
          yield return new WaitWhile(() => isPaused);
        }

        if (!_textureFramePool.TryGetTextureFrame(out var textureFrame))
        {
          yield return new WaitForEndOfFrame();
          continue;
        }

        // --- Step 7: Build MediaPipe Image ---
        Image image;
        switch (config.ImageReadMode)
        {
          case ImageReadMode.GPU:
            if (!canUseGpuImage)
            {
              throw new System.Exception("ImageReadMode.GPU is not supported on this platform.");
            }
            textureFrame.ReadTextureOnGPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildGPUImage(glContext);
            yield return waitForEndOfFrame; // ensure GPU copy finishes
            break;

          case ImageReadMode.CPU:
            yield return waitForEndOfFrame;
            textureFrame.ReadTextureOnCPU(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;

          case ImageReadMode.CPUAsync:
          default:
            req = textureFrame.ReadTextureAsync(imageSource.GetCurrentTexture(), flipHorizontally, flipVertically);
            yield return waitUntilReqDone;
            if (req.hasError)
            {
              Debug.LogWarning("Failed async texture readback");
              continue;
            }
            image = textureFrame.BuildCPUImage();
            textureFrame.Release();
            break;
        }

        // --- Step 8: Run recognition ---
        switch (taskApi.runningMode)
        {
          case Tasks.Vision.Core.RunningMode.IMAGE:
            var imageResult = taskApi.Recognize(image, imageProcessingOptions);
            ProcessResult(imageResult);
            break;

          case Tasks.Vision.Core.RunningMode.VIDEO:
            var videoResult = taskApi.RecognizeForVideo(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            ProcessResult(videoResult);
            break;

          case Tasks.Vision.Core.RunningMode.LIVE_STREAM:
            taskApi.RecognizeAsync(image, GetCurrentTimestampMillisec(), imageProcessingOptions);
            break;
        }


        // --- Measure MediaPipe input FPS ---
        inputFrameCount++;
        inputTimer += Time.unscaledDeltaTime; // independent of game timescale
        if (inputTimer >= 1f)
        {
          inputFps = inputFrameCount / inputTimer;
          inputFrameCount = 0;
          inputTimer = 0f;

        }
        
        yield return new WaitForSecondsRealtime(1f / inputTargetFps);

      }
    }

    /// <summary>
    /// Callback when in LIVE_STREAM mode.
    /// </summary>
    private void OnGestureOutput(GestureRecognizerResult result, Image image, long timestamp)
    {
      // --- Measure MediaPipe output FPS ---
      outputFrameCount++;
      if (outputStopwatch.ElapsedMilliseconds >= 1000)
      {
        float seconds = outputStopwatch.ElapsedMilliseconds / 1000f;
        outputFps = outputFrameCount / seconds;

        outputFrameCount = 0;
        outputStopwatch.Restart();
      }

      // Actual result handling
      ProcessResult(result);
    }

    // private string lastLeftGesture = "";
    // private string lastRightGesture = "";
    // private string lastGesture = "";

    /// <summary>
    /// Handles the result: logs it and fires the UnityEvent.
    /// </summary>
    private void ProcessResult(GestureRecognizerResult result)
    {
        string leftGesture = "None";
        string rightGesture = "None";

        // No hands detected
        if (result.gestures == null || result.gestures.Count == 0)
        {
            OnGestureRecognized?.Invoke(leftGesture, rightGesture);
            return;
        }

        // One hand detected
        if (result.gestures.Count == 1)
        {
            var topCategory = result.gestures[0].categories[0];
            string gestureName = topCategory.categoryName;
            string hand = result.handedness[0].categories[0].categoryName;

            if (hand == "Left")
            {
                leftGesture = "None";
                rightGesture = gestureName;
            }
            else
            {
                leftGesture = gestureName;
                rightGesture = "None";
            }

            OnGestureRecognized?.Invoke(leftGesture, rightGesture);
            return;
        }

        // Two hands detected (original logic)
        var topCategory2Hands = result.gestures[0].categories[0];
        string gestureName1 = topCategory2Hands.categoryName;
        string hand1 = result.handedness[0].categories[0].categoryName;

        var topCategory2 = result.gestures[1].categories[0];
        string gestureName2 = topCategory2.categoryName;
        string hand2 = result.handedness[1].categories[0].categoryName;

        if (hand1 == "Left")
        {
            leftGesture = gestureName2;
            rightGesture = gestureName1;
        }
        else
        {
            leftGesture = gestureName1;
            rightGesture = gestureName2;
        }

        OnGestureRecognized?.Invoke(leftGesture, rightGesture);
    }
  
  }
}
