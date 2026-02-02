using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using Mediapipe;
using UnityEngine;
using Debug = UnityEngine.Debug;

public class SpellManager : MonoBehaviour
{
    public enum CastMode { Auto, Manual }

    [Header("References")]
    [SerializeField] private SpellBook spellbook;
    [SerializeField] private GestureUI gestureUI; // current gesture
    [SerializeField] private GestureUIBuffer gestureUIBuffer; // bottom left
    [SerializeField] private HandVignetteUI handVignetteUI; // NEW: vignette flash

    [Header("Casting Settings")]
    [SerializeField] private CastMode castMode;
    [SerializeField] private float requiredStableTime = 0.1f;
    [SerializeField] private float requiredNoHandTime = 0.1f;
    [SerializeField] private GestureLabel manualConfirmLeft;
    [SerializeField] private GestureLabel manualConfirmRight;

    [Header("Caster")]
    [SerializeField] private SpellCaster playerCaster;

    [Header("Debug")]
    [SerializeField] private bool debugGestures;
    private string debugString = "";

    private List<GesturePair> gestureBuffer = new(); // for current input

    // For OnGestureRecognized
    private readonly ConcurrentQueue<(GestureLabel left, GestureLabel right)> gestureQueue = new();

    private GestureLabel stableLeft = GestureLabel.None;
    private GestureLabel stableRight = GestureLabel.None;

    private GestureLabel pendingLeft = GestureLabel.None;
    private GestureLabel pendingRight = GestureLabel.None;

    private float stableStartTime = -1f;
    private float noLeftStartTime = -1f;
    private float noRightStartTime = -1f;
    private bool isPaused = false;

    public void PauseCasting()
    {
        isPaused = true;
    }

    public void ResumeCasting()
    {
        isPaused = false;
    }


    // Called by MediaPipe callback (worker thread), need to pipe to main thread
    public void OnGestureRecognized(string leftStr, string rightStr)
    {
        // Convert raw strings to enum-safe values
        var left = GestureMapper.ToEnum(leftStr);
        var right = GestureMapper.ToEnum(rightStr);

        // Add to queue so it can be processed safely in the main thread
        gestureQueue.Enqueue((left, right));

    }

    // Called from Update(), this is thread safe
    private void HandleGesture(GestureLabel left, GestureLabel right)
    {
        if (isPaused)
        {
            Debug.Log("Casting is paused");
            return;
        }
        
        // Display the hands in UI
            gestureUI.ShowGesture(left, right);

        // NEW: Flash vignette if either hand is not detected
        if (left == GestureLabel.None)
        {
            if (noLeftStartTime < 0f) noLeftStartTime = Time.time;
            if (Time.time - noLeftStartTime >= requiredNoHandTime)
            {
                //handVignetteUI?.ShowLeft(true);
                playerCaster.leftHandAura.SetActive(false);
            }
        }
        if (right == GestureLabel.None)
        {
            if (noRightStartTime < 0f) noRightStartTime = Time.time;
            if (Time.time - noRightStartTime >= requiredNoHandTime)
            {
                //handVignetteUI?.ShowRight(true);
                playerCaster.rightHandAura.SetActive(false);
            }
        }

        if (left != GestureLabel.None)
        {
            noLeftStartTime = -1f;
            //handVignetteUI?.ShowLeft(false);
            playerCaster.leftHandAura.SetActive(true);
        }
        if (right != GestureLabel.None)
        {
            noRightStartTime = -1f;
            //handVignetteUI?.ShowRight(false);
            playerCaster.rightHandAura.SetActive(true);
        }

        // Don't process if either hand is "None"
        if (left == GestureLabel.None || right == GestureLabel.None)
        {
            return;
        }


        // If same as pending, track time
        if (pendingLeft == left && pendingRight == right)
        {
            if (stableStartTime < 0f) stableStartTime = Time.time;

            // Only if has been >= required time, and only proceed if changed from previous
            if (Time.time - stableStartTime >= requiredStableTime && (left != stableLeft || right != stableRight))
            {
                // Commit new stable gesture
                stableLeft = left;
                stableRight = right;

                // Check cast mode manual 
                if (castMode == CastMode.Manual)
                {
                    // CHECK IF SUBMIT GESTURE
                    if (left == manualConfirmLeft && right == manualConfirmRight)
                    {
                        // Check if it matches a spell
                        if (spellbook.TryGetSpell(gestureBuffer, out var spell))
                        {
                            playerCaster.Cast(spell);

                            // Reset buffer for next spell
                            gestureBuffer.Clear();
                            gestureUIBuffer.ClearUIBuffer();

                        }
                        else
                        {
                            // Play fizzle out animation....
                            Debug.Log("fizzle");
                            playerCaster.PlayFizzle();
                            // Reset buffer for next spell
                            gestureBuffer.Clear();
                            gestureUIBuffer.ClearUIBuffer();
                            Debug.Log("Not valid spell, buffer cleared");

                        }

                        // RETURN FROM SUBMIT GESTURE
                        return;
                    }
                }
                //NOT SUBMIT GESTURE, CAN CONTINUE, SHARED LOGIC FOR AUTO AND MANUAL

                // Add this frame’s pair to buffer
                gestureBuffer.Add(new GesturePair(left, right));
                gestureUIBuffer.AddToUIBuffer(left, right);

                // DEBUG: REMOVE
                if (debugGestures)
                {
                    foreach (GesturePair pair in gestureBuffer)
                    {
                        debugString += pair.ToString();
                    }
                    Debug.Log(debugString);
                }
                //////////

                if (castMode == CastMode.Auto)
                {
                    // Check if it matches a spell
                    if (spellbook.TryGetSpell(gestureBuffer, out var spell))
                    {
                        playerCaster.Cast(spell);

                        // Reset buffer for next spell
                        gestureBuffer.Clear();
                        gestureUIBuffer.ClearUIBuffer();

                        debugString = ""; //REMOVE
                    }
                }


            }
        }
        else
        {
            // reset pending
            pendingLeft = left;
            pendingRight = right;
            stableStartTime = Time.time;
        }

    }

    private void Update()
    {
        // Grabbing the left and right gesture, sending to HandleGesture
        while (gestureQueue.TryDequeue(out var gesture))
        {
            var (left, right) = gesture;
            HandleGesture(left, right);
        }

        // Clear buffer for auto cast, or just for development ease
        if (Input.anyKeyDown)
        {
            gestureBuffer.Clear();
            gestureUIBuffer.ClearUIBuffer();
            Debug.Log("************ Cleared buffer");
        }
    }
}
