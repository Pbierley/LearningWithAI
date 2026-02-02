using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using TMPro;

/// <summary>
/// Manages the tutorial flow, tracking steps and progression.
/// Singleton pattern for easy access from other scripts.
/// </summary>
public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Tutorial Steps")]
    [Tooltip("Define each tutorial step in order")]
    public TutorialStep[] steps;

    [Header("UI References")]
    [Tooltip("Text component to display the current prompt")]
    public TextMeshProUGUI promptText;
    
    [Tooltip("Optional panel that contains the prompt")]
    public GameObject promptPanel;

    [Header("Progression")]
    [Tooltip("Automatically advance to next step when current is completed")]
    public bool autoAdvance = true;
    
    [Tooltip("Delay before advancing to next step (seconds)")]
    public float advanceDelay = 1.5f;

    [Header("Completion")]
    [Tooltip("Scene to load when all steps are complete (leave empty to stay)")]
    public string sceneOnComplete;
    
    public UnityEvent OnTutorialComplete;

    [Header("Player Reference")]
    [Tooltip("Reference to the player's SpellCaster (for damage/heal tutorial steps)")]
    public SpellCaster playerSpellCaster;

    private int currentStepIndex = 0;
    private bool tutorialComplete = false;

    void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    void Start()
    {
        if (steps != null && steps.Length > 0)
        {
            StartStep(0);
        }
        else
        {
            Debug.LogWarning("TutorialManager: No steps defined!");
        }
    }

    /// <summary>
    /// Get the current tutorial step
    /// </summary>
    public TutorialStep CurrentStep => 
        (currentStepIndex >= 0 && currentStepIndex < steps.Length) ? steps[currentStepIndex] : null;

    /// <summary>
    /// Get the current step index
    /// </summary>
    public int CurrentStepIndex => currentStepIndex;

    /// <summary>
    /// Start a specific tutorial step
    /// </summary>
    public void StartStep(int index)
    {
        if (index < 0 || index >= steps.Length)
        {
            Debug.LogError($"TutorialManager: Invalid step index {index}");
            return;
        }

        currentStepIndex = index;
        TutorialStep step = steps[index];
        
        // Reset completion flag when starting a step (fixes issue where step won't complete)
        step.isCompleted = false;

        Debug.Log($"TutorialManager: Starting step {index} - {step.stepName}");

        // Update UI
        if (promptText != null)
        {
            promptText.text = step.promptText;
        }
        if (promptPanel != null)
        {
            promptPanel.SetActive(true);
        }

        // Deactivate objects first
        if (step.deactivateOnStart != null)
        {
            foreach (var obj in step.deactivateOnStart)
            {
                if (obj != null) obj.SetActive(false);
            }
        }

        // Activate objects
        if (step.activateOnStart != null)
        {
            foreach (var obj in step.activateOnStart)
            {
                if (obj != null) obj.SetActive(true);
            }
        }

        // Damage player if configured (useful for heal tutorials)
        if (step.damagePlayerOnStart)
        {
            if (playerSpellCaster == null)
            {
                Debug.LogError("TutorialManager: damagePlayerOnStart is enabled but playerSpellCaster is not assigned! Drag SapphiArtchan into the Player Spell Caster field.");
            }
            else
            {
                float healthPercent = playerSpellCaster.currentHealth / playerSpellCaster.maxHealth;
                Debug.Log($"TutorialManager: Player health is {playerSpellCaster.currentHealth}/{playerSpellCaster.maxHealth} ({healthPercent * 100}%), threshold is {step.onlyIfHealthAbovePercent * 100}%");
                
                if (healthPercent >= step.onlyIfHealthAbovePercent)
                {
                    Debug.Log($"TutorialManager: Damaging player by {step.damageAmount} for heal tutorial");
                    playerSpellCaster.TakeDamage(step.damageAmount, transform);
                }
                else
                {
                    Debug.Log($"TutorialManager: Player already below threshold, skipping damage");
                }
            }
        }

        // Auto-complete after delay if configured (useful for congrats/info screens)
        if (step.autoCompleteAfterDelay)
        {
            Debug.Log($"TutorialManager: Step will auto-complete in {step.autoCompleteDelay} seconds");
            Invoke(nameof(CompleteCurrentStep), step.autoCompleteDelay);
        }

        // Fire step start event
        step.OnStepStart?.Invoke();
    }

    /// <summary>
    /// Call this when the current step's objective is completed.
    /// Can be called from TutorialCrystal, ShieldComponent, etc.
    /// </summary>
    public void CompleteCurrentStep()
    {
        Debug.Log($"TutorialManager: CompleteCurrentStep() called. Current step: {currentStepIndex}, tutorialComplete: {tutorialComplete}");
        
        if (tutorialComplete)
        {
            Debug.Log("TutorialManager: Tutorial already complete, ignoring.");
            return;
        }
        if (currentStepIndex >= steps.Length)
        {
            Debug.Log("TutorialManager: Step index out of range, ignoring.");
            return;
        }

        TutorialStep step = steps[currentStepIndex];
        if (step.isCompleted)
        {
            Debug.Log($"TutorialManager: Step {currentStepIndex} already completed, ignoring.");
            return;
        }

        step.isCompleted = true;
        Debug.Log($"TutorialManager: ✓ Completed step {currentStepIndex} - {step.stepName}");

        // Fire step complete event
        step.OnStepComplete?.Invoke();

        // Check if there are more steps
        if (currentStepIndex + 1 < steps.Length)
        {
            if (autoAdvance)
            {
                // Advance after delay
                Invoke(nameof(AdvanceToNextStep), advanceDelay);
            }
        }
        else
        {
            // Tutorial complete!
            CompleteTutorial();
        }
    }

    /// <summary>
    /// Manually advance to the next step
    /// </summary>
    public void AdvanceToNextStep()
    {
        if (currentStepIndex + 1 < steps.Length)
        {
            StartStep(currentStepIndex + 1);
        }
    }

    /// <summary>
    /// Skip to a specific step (useful for debugging or skip buttons)
    /// </summary>
    public void SkipToStep(int index)
    {
        // Mark all previous steps as complete
        for (int i = 0; i < index && i < steps.Length; i++)
        {
            steps[i].isCompleted = true;
        }
        StartStep(index);
    }

    /// <summary>
    /// Called when all tutorial steps are complete
    /// </summary>
    private void CompleteTutorial()
    {
        tutorialComplete = true;
        Debug.Log("TutorialManager: Tutorial Complete!");

        OnTutorialComplete?.Invoke();

        if (!string.IsNullOrEmpty(sceneOnComplete))
        {
            SceneManager.LoadScene(sceneOnComplete);
        }
    }

    /// <summary>
    /// Reset the tutorial to start from the beginning
    /// </summary>
    public void ResetTutorial()
    {
        tutorialComplete = false;
        foreach (var step in steps)
        {
            step.isCompleted = false;
        }
        StartStep(0);
    }
}

