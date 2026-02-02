using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using Mediapipe.Unity.Sample.GestureRecognition;

public enum MatchState { PreRound, Fighting, EndRound, Results }

/// <summary>
/// Manages match flow: countdown, fighting, victory/defeat, and end-of-match UI.
/// Subscribes to SpellCaster death events to determine winner.
/// </summary>
public class MatchManager : MonoBehaviour
{
    public static MatchManager Instance { get; private set; }

    // Global event for match end - other systems can subscribe (AudioManager, menus, analytics)
    public static event Action<SpellCaster> OnMatchEnded;

    // Players - registered via RegisterPlayer(), not serialized
    private SpellCaster localPlayer;
    private SpellCaster opponent;
    private bool matchInitialized = false;

    [Header("UI References")]
    [SerializeField] private TMP_Text countdownText;
    [SerializeField] private TMP_Text resultText;
    [SerializeField] private TMP_Text victoryText;
    [SerializeField] private TMP_Text defeatText;
    [SerializeField] private GameObject endMatchButtons;
    [SerializeField] private EnemyGestureDisplay enemyGestureDisplay;
    [SerializeField] private GestureUIBuffer gestureUIBuffer;

    [Header("Camera")]
    [SerializeField] private CameraEffects cameraEffects;
    //[SerializeField] private Transform victoryCameraTarget;

    [Header("Timing")]
    [SerializeField] private float countdownDuration = 3f;
    [SerializeField] private float victoryZoomDuration = 1.5f;
    [SerializeField] private float resultDisplayDelay = 1f;
    [SerializeField] private float showButtonsDelay = 2f;

    [Header("INPUT MANAGEMENT")]
    [Tooltip("Runner that drives the GestureRecognizer task. Pauses/resumes when fighting begins/ends.")]
    [SerializeField] private GestureRecognizerRunner gestureRecognizerRunner;
    [Tooltip("AI spell manager that casts spells when fighting begins.")]
    [SerializeField] private AISpellManager2 aiSpellManager;
    [SerializeField] private SpellManager spellManager;

    [Header("Audio")]
    [SerializeField] private AudioClip gameMusic;
    [SerializeField] private float gameMusicVolume = 0.3f;
    

    // Runtime state
    public MatchState CurrentState { get; private set; }
    private SpellCaster winner;

    /// <summary>
    /// Returns the winner of the match. Null if match hasn't ended.
    /// Useful for stats, replays, post-round effects.
    /// </summary>
    public SpellCaster Winner => winner;

    /// <summary>
    /// Returns true if players are allowed to cast spells (only during Fighting state).
    /// </summary>
    public bool CanFight => CurrentState == MatchState.Fighting;

    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

    }

    private void Start()
    {
        // Hide UI elements initially
        if (resultText != null) resultText.gameObject.SetActive(false);
        if (victoryText != null) victoryText.gameObject.SetActive(false);
        if (defeatText != null) defeatText.gameObject.SetActive(false);
        if (endMatchButtons != null) endMatchButtons.SetActive(false);

        // Match flow starts when both players register via RegisterPlayer()
        spellManager.PauseCasting();

        // Lock mouse cursor/ controller
        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    /// <summary>
    /// Called by SpellCaster during Start() to register with the match.
    /// SpellCaster determines if it's local (via tag or network ownership).
    /// </summary>
    public void RegisterPlayer(SpellCaster caster, bool isLocal)
    {
        if (isLocal)
            localPlayer = caster;
        else
            opponent = caster;

        // Subscribe to death event
        caster.OnDeath += HandlePlayerDeath;

        Debug.Log($"[MatchManager] Registered {caster.name} as {(isLocal ? "LocalPlayer" : "Opponent")}");

        // Start match when both players are registered
        if (localPlayer != null && opponent != null && !matchInitialized)
        {
            matchInitialized = true;
            StartCoroutine(MatchFlow());
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe from events
        if (localPlayer != null)
        {
            localPlayer.OnDeath -= HandlePlayerDeath;
        }
        if (opponent != null)
        {
            opponent.OnDeath -= HandlePlayerDeath;
        }
    }

    private IEnumerator MatchFlow()
    {
        // Start game music
        if (AudioManager.Instance != null && gameMusic != null)
        {
            AudioManager.Instance.PlayMusic(gameMusic, true, gameMusicVolume);
        }

        // PreRound - Countdown
        CurrentState = MatchState.PreRound;
        yield return StartCoroutine(RunCountdown());

        // Fighting - Players can cast spells
        CurrentState = MatchState.Fighting;
        Debug.Log("[MatchManager] FIGHT!");

        // Enable player and AI casting
        gestureRecognizerRunner?.ResumeRecognition();
        aiSpellManager?.StartAI();
        spellManager.ResumeCasting();
        // Wait for a player to die (handled by HandlePlayerDeath)
    }

    private IEnumerator RunCountdown()
    {
        if (countdownText != null)
        {
            countdownText.gameObject.SetActive(true);

            // Count down from countdownDuration
            for (int i = (int)countdownDuration; i > 0; i--)
            {
                countdownText.text = i.ToString();
                Debug.Log($"[MatchManager] Countdown: {i}");
                yield return new WaitForSeconds(1f);
            }

            // Show FIGHT!
            countdownText.text = "FIGHT!";
            Debug.Log("[MatchManager] Countdown: FIGHT!");
            yield return new WaitForSeconds(0.5f);

            // Hide countdown text
            countdownText.gameObject.SetActive(false);
        }
        else
        {
            // No UI, just wait
            yield return new WaitForSeconds(countdownDuration);
        }
    }

    private void HandlePlayerDeath(SpellCaster loser)
    {
        // Prevent multiple triggers
        if (CurrentState == MatchState.EndRound || CurrentState == MatchState.Results)
            return;

        CurrentState = MatchState.EndRound;

        // Disable all casting immediately
        gestureRecognizerRunner?.PauseRecognition();
        aiSpellManager?.StopAI();
        spellManager.PauseCasting();
        // Determine winner (the one who didn't die)
        winner = (loser == localPlayer) ? opponent : localPlayer;

        Debug.Log($"[MatchManager] {loser.name} died! Winner: {winner.name}");

        // Start victory sequence
        StartCoroutine(MatchEndSequence());
    }

    private IEnumerator MatchEndSequence()
    {
        // Clear gesture UI buffer
        gestureUIBuffer.ClearUIBuffer();
        enemyGestureDisplay.ClearUIBuffer();

        // Trigger winner's victory animation
        if (winner != null && winner.characterAnimator != null)
        {
            winner.characterAnimator.SetTrigger("Victory");
        }

        // Zoom camera to winner
        // if (cameraEffects != null && victoryCameraTarget != null)
        // {
        //     cameraEffects.ZoomToTarget(victoryCameraTarget, victoryZoomDuration);
        // }

        if (cameraEffects != null )
        {
            //var targetPoints = winner.GetComponent<TargetPoints>();
            cameraEffects.ZoomToTarget(winner.transform, victoryZoomDuration);
        }

        // Wait before showing result text
        yield return new WaitForSeconds(resultDisplayDelay);

        // Show VICTORY or DEFEAT based on local player
        if (victoryText != null || defeatText != null)
        {
            victoryText.gameObject.SetActive(winner == localPlayer);
            defeatText.gameObject.SetActive(winner != localPlayer);
        }

        // Fire global event
        OnMatchEnded?.Invoke(winner);

        // Wait before showing buttons
        yield return new WaitForSeconds(showButtonsDelay);

        // Transition to Results state and show buttons
        CurrentState = MatchState.Results;

        if (endMatchButtons != null)
        {
            endMatchButtons.SetActive(true);
        }

        // Unlock mouse cursor/ controller
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        Debug.Log("[MatchManager] Match ended. Showing end buttons.");
    }

    /// <summary>
    /// Called by Rematch button. Reloads the current scene.
    /// </summary>
    public void OnRematchClicked()
    {
        Debug.Log("[MatchManager] Rematch requested.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Called by Return to Menu button. Loads the Menu scene.
    /// </summary>
    public void OnReturnToMenuClicked()
    {
        Debug.Log("[MatchManager] Returning to menu.");
        SceneManager.LoadScene("Menu");
    }
}

