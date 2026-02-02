using System;
using System.Collections;
using System.Collections.Concurrent;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;


/// <summary>
/// TutorialGesture listens for gesture strings (matches the signature
/// OnGestureRecognized(string left, string right) used by GestureRecognizerRunner)
/// and detects when both hands show the configured gesture (default: "ThumbsUp")
/// for a configurable number of stable frames. When detected it invokes the
/// UnityEvent OnReady. Thread-safe: OnGestureRecognized can be called from
/// any thread.
/// </summary>
public class TutorialGesture : MonoBehaviour
{
    [Header("Gesture Settings")]
    [Tooltip("Name of the gesture to require on both hands (exact match).")]
    public GestureLabel requiredGestureName = GestureLabel.ThumbsUp;

    [Tooltip("How many consecutive stable frames the required gesture must be held for.")]
    public int requiredStableFrames = 5;

    [Tooltip("If true, the OnReady event will only fire once.")]
    public bool triggerOnce = true;

    [Header("Optional Scene Load")]
    [Tooltip("If true, the specified scene will be loaded when OnReady fires.")]
    public bool loadSceneOnReady = false;

    [Tooltip("Scene name to load when ready (only used if loadSceneOnReady is true).")]

    [Header("Events")]
    public UnityEvent OnReady;

    // Thread-safe queue where gesture callbacks enqueue recognized gestures
    private readonly ConcurrentQueue<(GestureLabel left, GestureLabel right)> _gestureQueue = new();

    // Pending / stable tracking
    private GestureLabel _pendingLeft = GestureLabel.None;
    private GestureLabel _pendingRight = GestureLabel.None;
    private GestureLabel _stableLeft = GestureLabel.None;
    private GestureLabel _stableRight = GestureLabel.None;
    private int _stableCounter = 0;

    private bool _hasTriggered = false;
    [Header("Circle Controller")]
    [SerializeField] private CircleColorController circleController;

    // coroutine that lights the circles while thumbs-up is held
    private Coroutine circleSequenceCoroutine;

    /// <summary>
    /// Public method expected to be invoked by GestureRecognizerRunner.OnGestureRecognized
    /// or other sources. This is thread-safe.
    /// </summary>
    public void OnGestureRecognized(string leftStr, string rightStr)
    {
        // Convert raw strings to enum-safe values
        var left = GestureMapper.ToEnum(leftStr);
        var right = GestureMapper.ToEnum(rightStr);

        // Add to queue so it can be processed safely in the main thread
        _gestureQueue.Enqueue((left, right));
    }

    private void Update()
    {
        Debug.Log("TutorialGesture Update called");
        if (triggerOnce && _hasTriggered) return;

        // Process any pending queue items (thread-safe dequeue)
        while (_gestureQueue.TryDequeue(out var pair))
        {
            Debug.Log($"Gesture pair: left={pair.left}, right={pair.right}");

            // Ignore if either is "None"
            if (pair.left == GestureLabel.None || pair.right == GestureLabel.None)
            {
                // treat as break in stability
                _pendingLeft = pair.left;
                _pendingRight = pair.right;
                _stableCounter = 0;
                continue;
            }

            // If same as pending, increment stability counter
            if (pair.left == _pendingLeft && pair.right == _pendingRight)
            {
                _stableCounter++;
                Debug.Log($"TutorialGesture: Same gesture repeated. Stable count: {_stableCounter}");
            }
            else
            {
                // New pending values
                Debug.Log($"TutorialGesture: New gesture detected. left={pair.left}, right={pair.right}");
                _pendingLeft = pair.left;
                _pendingRight = pair.right;
                _stableCounter = 1;
            }

            // Start/stop the per-second circle activation while both hands hold the required gesture
            bool leftMatch = pair.left == requiredGestureName;
            bool rightMatch = pair.right == requiredGestureName;

            Debug.Log($"Checking: stableCount={_stableCounter} >= required={requiredStableFrames} ? leftMatch={leftMatch}, rightMatch={rightMatch}");

            if (_stableCounter >= requiredStableFrames && leftMatch && rightMatch)
            {
                Debug.Log("TutorialGesture: Required gesture held stable, starting circle sequence.");
                // start the activation coroutine if not already running
                if (circleSequenceCoroutine == null)
                {
                    if (circleController == null)
                        circleController = FindFirstObjectByType<CircleColorController>();

                    if (circleController != null)
                        circleSequenceCoroutine = StartCoroutine(CircleSequence());
                }
            }
            else
            {
                // stop and reset when gesture is broken
                if (circleSequenceCoroutine != null)
                {
                    StopCoroutine(circleSequenceCoroutine);
                    circleSequenceCoroutine = null;
                }
                if (circleController != null)
                    circleController.ResetProgress();
            }
        }
    }

    private IEnumerator CircleSequence()
    {
        // keep activating one circle every second while this coroutine runs
        while (true)
        {
            yield return new WaitForSeconds(.5f);
            if (circleController == null)
                yield break;

            bool activated = circleController.ActivateNext();
            if (!activated)
            {
                // all circles are active; trigger the ready event and load scene
                Debug.Log("TutorialGesture: All circles activated, invoking OnReady.");
                OnReady?.Invoke();
                _hasTriggered = true;

                if (loadSceneOnReady)
                {
                    SceneManager.LoadScene("TutorialGameScene");
                }

                circleSequenceCoroutine = null;
                yield break;
            }
        }
    }

    /// <summary>
    /// Reset internal state so the recognition process can run again.
    /// </summary>
    public void ResetState()
    {
        _gestureQueue.Clear();
        _pendingLeft = _pendingRight = _stableLeft = _stableRight = GestureLabel.None;
        _stableCounter = 0;
        _hasTriggered = false;
    }
}
