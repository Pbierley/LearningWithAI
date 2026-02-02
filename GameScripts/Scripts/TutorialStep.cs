using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Defines a single tutorial step with its objective, UI prompt, and completion logic.
/// </summary>
[System.Serializable]
public class TutorialStep
{
    [Header("Step Info")]
    public string stepName;
    [TextArea(2, 4)]
    public string promptText;
    
    [Header("Objects to Activate/Deactivate")]
    [Tooltip("GameObjects to enable when this step starts (e.g., crystals, enemies, UI)")]
    public GameObject[] activateOnStart;
    
    [Tooltip("GameObjects to disable when this step starts")]
    public GameObject[] deactivateOnStart;
    
    [Header("Player Damage on Start")]
    [Tooltip("If true, damage the player when this step starts (useful for heal tutorials)")]
    public bool damagePlayerOnStart = false;
    
    [Tooltip("Amount of damage to deal")]
    public float damageAmount = 20f;
    
    [Tooltip("Only damage if player health is above this percentage (0-1). Set to 1 to only damage if at full health.")]
    [Range(0f, 1f)]
    public float onlyIfHealthAbovePercent = 0.8f;
    
    [Header("Auto Complete")]
    [Tooltip("If true, this step will automatically complete after a delay (useful for congrats/info screens)")]
    public bool autoCompleteAfterDelay = false;
    
    [Tooltip("Delay in seconds before auto-completing")]
    public float autoCompleteDelay = 5f;
    
    [Header("Events")]
    public UnityEvent OnStepStart;
    public UnityEvent OnStepComplete;
    
    [HideInInspector]
    public bool isCompleted = false;
}

/// <summary>
/// Types of tutorial objectives the system can track
/// </summary>
public enum TutorialObjectiveType
{
    DestroyCrystal,
    BlockWithShield,
    CastSpell,
    PerformGesture,
    Custom
}

