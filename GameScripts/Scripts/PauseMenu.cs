using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class PauseMenu : MonoBehaviour
{
    [SerializeField] private GameObject pauseMenu;

    public static bool GameIsPaused = false;

    [Header("Spellbook Display")]
    [SerializeField] private SpellBook spellBook;
    [SerializeField] private GestureIconLibrary iconLibrary;

    [SerializeField] private GameObject spellRowPrefab;
    [SerializeField] private GameObject gestureIconPrefab;

    [SerializeField] private Transform spellListContainer;

    private bool uiBuilt = false;   // Ensures IU builds only once

    void Start()
    {
        BuildSpellbookUIOnce();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (GameIsPaused)
                Resume();
            else
                Pause();
        }
    }

    public void Pause()
    {
        pauseMenu.SetActive(true);
        GameIsPaused = true;
        Time.timeScale = 0;

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
    }

    public void Resume()
    {
        pauseMenu.SetActive(false);
        GameIsPaused = false;
        Time.timeScale = 1;

        // Hide cursor if match is not over
        if (MatchManager.Instance != null && MatchManager.Instance.CurrentState != MatchState.Results)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }

    }

    public void LeaveMatch()
    {
        Time.timeScale = 1;
        SceneManager.LoadScene("Menu");
    }

    // ------------------------------------------------------------
    // BUILD THE SPELL UI ONLY ONCE (runs at scene start)
    // ------------------------------------------------------------
    private void BuildSpellbookUIOnce()
    {
        if (uiBuilt) return;
        uiBuilt = true;

        if (spellBook == null || iconLibrary == null ||
            spellRowPrefab == null || gestureIconPrefab == null ||
            spellListContainer == null)
        {
            Debug.LogError("[Spellbook] Missing references!");
            return;
        }

        var entries = spellBook.GetEntries();

        foreach (var entry in entries)
        {
            if (entry.spell == null)
                continue;

            // Create row
            GameObject row = Instantiate(spellRowPrefab, spellListContainer);

            // Set spell name
            TMP_Text tmp = row.GetComponentInChildren<TMP_Text>();
            if (tmp != null)
                tmp.text = entry.spell.spellName + ":";

            // Find icon container (child with HorizontalLayoutGroup)
            Transform iconContainer = null;
            foreach (Transform child in row.transform)
            {
                if (child.GetComponent<HorizontalLayoutGroup>() != null)
                {
                    iconContainer = child;
                    break;
                }
            }

            if (iconContainer == null)
            {
                Debug.LogWarning("[Spellbook] No icon container found in row prefab.");
                continue;
            }

            // Add gesture icons
            if (entry.sequence != null)
            {
                foreach (var pair in entry.sequence)
                {
                    CreateGestureIcon(pair.Left, iconContainer);
                    CreateGestureIcon(pair.Right, iconContainer);
                }
            }
        }
    }

    private void CreateGestureIcon(GestureLabel gesture, Transform parent)
    {
        GameObject iconObj = Instantiate(gestureIconPrefab, parent);

        Image img = iconObj.GetComponent<Image>();
        if (img != null)
        {
            Sprite sprite = iconLibrary.GetIcon(gesture);
            img.sprite = sprite;
        }
    }
}
