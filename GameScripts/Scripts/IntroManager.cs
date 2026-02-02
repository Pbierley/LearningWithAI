using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using DG.Tweening;
using System;

public class IntroManager : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text mainText;
    [SerializeField] private TMP_Text titleText;
    [TextArea(1, 3)]
    [SerializeField] private string gameTitle = "Spell\nArena";

    [Header("Audio")]
    [SerializeField] private AudioClip introMusic;
    [SerializeField] private AudioClip writingSound;
    [SerializeField] private float writingSoundMinOffset = 10f;
    [SerializeField] private float writingSoundMaxOffset = 30f;

    [Header("Timing Settings")]
    [SerializeField] private float initialDelay = 2.5f;
    [SerializeField] private float charFadeDuration = 0.4f;      // How long each character takes to fully appear
    [SerializeField] private float charDelay = 0.045f;           // Delay between starting each character's fade
    [SerializeField] private float linePauseDuration = 1.5f;
    [SerializeField] private float sectionPauseDuration = 2f;
    [SerializeField] private float fadeOutDuration = 0.5f;
    [SerializeField] private float titleHoldDuration = 3f;
    [SerializeField] private float punctuationPause = 0.15f;     // Extra pause after punctuation
    [Tooltip("The time in seconds the music should reach before transitioning to the menu scene")]
    [SerializeField] private float transitionToMenuSongSeconds = 1f;

    [System.Serializable]
    public class IntroSection
    {
        [TextArea(2, 4)]
        public string[] lines;
        public float delayAfterSection = 2f;
    }

    [Header("Sections")]
    [SerializeField] private IntroSection[] sections;

    private void Awake()
    {
        // Initialize default sections if not set in inspector
        if (sections == null || sections.Length == 0)
        {
            sections = CreateDefaultSections();
        }
    }

    private void Start()
    {
        // Hide text initially
        if (mainText != null)
        {
            mainText.alpha = 0f;
            mainText.text = "";
        }
        if (titleText != null)
        {
            titleText.alpha = 0f;
            titleText.text = "";
        }

        // Start intro music
        if (AudioManager.Instance != null && introMusic != null)
        {
            AudioManager.Instance.PlayMusic(introMusic, false);
            Debug.Log("Playing intro music");
        }

        StartCoroutine(PlayIntroSequence());
    }

    private IntroSection[] CreateDefaultSections()
    {
        return new IntroSection[]
        {
            // Section 1 - The Myth
            new IntroSection
            {
                lines = new string[]
                {
                    "For centuries, a single wizard has stood above all others...",
                    "...the one chosen by the Rite of Ascendance."
                },
                delayAfterSection = 2f
            },
            // Section 2 - The Challenge
            new IntroSection
            {
                lines = new string[]
                {
                    "To claim the title of Archon...",
                    "...one must duel with pure will and motion."
                },
                delayAfterSection = 2f
            },
            // Section 3 - The Participants
            new IntroSection
            {
                lines = new string[]
                {
                    "Tonight, two rising sorceresses step forward.",
                    "Their hands shape the very battlefield."
                },
                delayAfterSection = 2f
            },
            // Section 4 - The Stakes
            new IntroSection
            {
                lines = new string[]
                {
                    "Power.  Honor.  Destiny.",
                    "Only one will ascend."
                },
                delayAfterSection = 2.5f
            },
            // Section 5 - Title Reveal
            new IntroSection
            {
                lines = new string[]
                {
                    "The Rite of Ascendance Begins."
                },
                delayAfterSection = 1.5f
            }
        };
    }

    private IEnumerator PlayIntroSequence()
    {
        // Start preloading Menu scene immediately in background
        AsyncOperation menuSceneLoad = SceneManager.LoadSceneAsync("Menu");
        menuSceneLoad.allowSceneActivation = false; // Don't activate the scene yet

        // Wait until music actually reaches the initial delay time (syncs to audio clock)
        yield return new WaitUntil(() => AudioManager.Instance != null && AudioManager.Instance.GetMusicTime() >= initialDelay);

        // Play through each section
        for (int i = 0; i < sections.Length; i++)
        {
            yield return StartCoroutine(PlaySection(sections[i], i));
        }

        // Show game title
        yield return StartCoroutine(ShowTitle());

        // Transition to menu
        // wait until music reaches the transition to menu delay time (syncs to audio clock)
        yield return new WaitUntil(() => AudioManager.Instance != null && AudioManager.Instance.GetMusicTime() >= transitionToMenuSongSeconds);
        menuSceneLoad.allowSceneActivation = true;// Scene is already loaded and waiting - activate INSTANTLY
    }

    private IEnumerator PlaySection(IntroSection section, int sectionIndex)
    {
        foreach (string line in section.lines)
        {
            // Set text but keep it invisible
            mainText.text = line;
            mainText.ForceMeshUpdate();

            // Make all characters invisible initially
            SetTextAlpha(mainText, 0f);

            // Fade in character by character (ghostly calligraphy effect)
            yield return StartCoroutine(FadeInCharacterByCharacter(mainText, line, sectionIndex));

            // Pause after line
            yield return new WaitForSeconds(linePauseDuration);
        }

        // Fade out the section
        yield return StartCoroutine(FadeOutAllCharacters(mainText, fadeOutDuration));

        // Pause between sections
        yield return new WaitForSeconds(section.delayAfterSection);
    }

    private IEnumerator FadeInCharacterByCharacter(TMP_Text textComponent, string fullText, int sectionIndex, bool playWritingSound = true)
    {
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        int characterCount = textInfo.characterCount;
        if (characterCount == 0) yield break;

        // Start writing sound at the beginning of each line
        if (playWritingSound)
        {
            StartWritingSound();
        }

        // Special handling for Section 4 "Power. Honor. Destiny." - more dramatic timing
        float currentCharDelay = charDelay;
        float currentCharFade = charFadeDuration;
        
        if (sectionIndex == 3 && fullText.Contains("Power"))
        {
            currentCharDelay = 0.06f;   // Slightly slower for dramatic effect
            currentCharFade = 0.5f;
        }

        // Fade in each character sequentially like ghostly calligraphy
        for (int i = 0; i < characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            
            // Skip invisible characters (spaces) but still add a tiny pause
            // if (!charInfo.isVisible)
            // {
            //     yield return new WaitForSeconds(currentCharDelay * 0.5f);
            //     continue;
            // }

            // Start fading in this character
            FadeCharacter(textComponent, i, 1f, currentCharFade);

            // Wait before starting the next character
            yield return new WaitForSeconds(currentCharDelay);

            // Add extra pause after punctuation for natural rhythm
            char c = charInfo.character;
            if (c == '.' || c == '…' || c == ',' || c == ';' || c == ':')
            {
                yield return new WaitForSeconds(punctuationPause);
            }
        }

        // Wait for the last character to finish fading
        yield return new WaitForSeconds(currentCharFade);

        // Stop writing sound when the line is complete
        if (playWritingSound)
        {
            StopWritingSound();
        }
    }

    private void FadeCharacter(TMP_Text textComponent, int characterIndex, float targetAlpha, float duration)
    {
        TMP_TextInfo textInfo = textComponent.textInfo;
        
        if (characterIndex >= textInfo.characterCount) return;
        
        TMP_CharacterInfo charInfo = textInfo.characterInfo[characterIndex];
        if (!charInfo.isVisible) return;

        int materialIndex = charInfo.materialReferenceIndex;
        int vertexIndex = charInfo.vertexIndex;

        Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;

        // Animate alpha from 0 to target with soft ghostly ease
        DOTween.To(() => 0f, (float alpha) =>
        {
            byte a = (byte)(alpha * 255);
            vertexColors[vertexIndex + 0].a = a;
            vertexColors[vertexIndex + 1].a = a;
            vertexColors[vertexIndex + 2].a = a;
            vertexColors[vertexIndex + 3].a = a;
            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }, targetAlpha, duration).SetEase(Ease.OutSine);  // Soft sine ease for ghostly calligraphy feel
    }

    private void StartWritingSound()
    {
        if (AudioManager.Instance == null || writingSound == null) return;
        
        // Start at a random offset for dynamic feel
        float randomOffset = UnityEngine.Random.Range(writingSoundMinOffset, writingSoundMaxOffset);
        AudioManager.Instance.PlayLoopingSFX(writingSound, randomOffset);
    }

    private void StopWritingSound()
    {
        if (AudioManager.Instance != null)
        {
            AudioManager.Instance.StopLoopingSFX();
        }
    }

    private void SetTextAlpha(TMP_Text textComponent, float alpha)
    {
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        byte a = (byte)(alpha * 255);

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
            if (!charInfo.isVisible) continue;

            int materialIndex = charInfo.materialReferenceIndex;
            int vertexIndex = charInfo.vertexIndex;

            Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;
            vertexColors[vertexIndex + 0].a = a;
            vertexColors[vertexIndex + 1].a = a;
            vertexColors[vertexIndex + 2].a = a;
            vertexColors[vertexIndex + 3].a = a;
        }

        textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
    }

    private IEnumerator FadeOutAllCharacters(TMP_Text textComponent, float duration)
    {
        textComponent.ForceMeshUpdate();
        TMP_TextInfo textInfo = textComponent.textInfo;

        float elapsed = 0f;
        
        // Store initial alpha values for each character vertex
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            float alpha = 1f - Mathf.SmoothStep(0f, 1f, t);  // Smooth fade from 1 to 0
            byte a = (byte)(alpha * 255);

            // Update all character vertices
            for (int i = 0; i < textInfo.characterCount; i++)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                int materialIndex = charInfo.materialReferenceIndex;
                int vertexIndex = charInfo.vertexIndex;

                Color32[] vertexColors = textInfo.meshInfo[materialIndex].colors32;
                vertexColors[vertexIndex + 0].a = a;
                vertexColors[vertexIndex + 1].a = a;
                vertexColors[vertexIndex + 2].a = a;
                vertexColors[vertexIndex + 3].a = a;
            }

            textComponent.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
            yield return null;
        }

        // Ensure fully faded out
        SetTextAlpha(textComponent, 0f);
    }

    private IEnumerator ShowTitle()
    {
        // Clear main text
        mainText.text = "";
        mainText.alpha = 0f;

        // Show title with dramatic fade
        if (titleText != null)
        {
            titleText.text = gameTitle;
            titleText.ForceMeshUpdate();
            SetTextAlpha(titleText, 0f);

            // Fade in the title character by character (no writing sound for title)
            yield return StartCoroutine(FadeInCharacterByCharacter(titleText, gameTitle, -1, false));

            // Hold on title
            yield return new WaitForSeconds(titleHoldDuration);

            // Fade out
            yield return StartCoroutine(FadeOutAllCharacters(titleText, fadeOutDuration * 2f));
        }
        else
        {
            // Fallback: use main text for title
            mainText.text = gameTitle;
            mainText.ForceMeshUpdate();
            SetTextAlpha(mainText, 0f);

            yield return StartCoroutine(FadeInCharacterByCharacter(mainText, gameTitle, -1, false));
            yield return new WaitForSeconds(titleHoldDuration);
            yield return StartCoroutine(FadeOutAllCharacters(mainText, fadeOutDuration * 2f));
        }
    }

    // Allow skipping intro with any key/click
    private void Update()
    {
        if (Input.anyKeyDown || Input.GetMouseButtonDown(0))
        {
            // Optional: Skip to menu immediately
            // StopAllCoroutines();
            // SceneManager.LoadScene("Menu");
        }
    }
}

