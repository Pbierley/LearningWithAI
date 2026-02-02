using UnityEngine;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(SpellCaster))]
public class AISpellManager2 : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpellBook spellBook;
    [SerializeField] private SpellCaster spellCaster;
    [SerializeField] private Transform player;
    [SerializeField] private EnemyGestureDisplay gestureDisplay;

    [Header("Behavior Settings")]
    [Tooltip("Seconds between each gesture being shown.")]
    [SerializeField] private float gestureInterval = 0.6f;
    [Tooltip("Seconds to wait between casting different spells.")]
    [SerializeField] private float pauseBetweenSpells = 2.5f;
    [Tooltip("If true, the AI will keep casting spells.")]
    [SerializeField] private bool isActive = true;
    [Tooltip("If true, start casting automatically when enabled (useful for tutorials)")]
    [SerializeField] private bool autoStartOnEnable = false;

    private void Awake()
    {
        if (!spellCaster) spellCaster = GetComponent<SpellCaster>();
        //if (!player) player = GameObject.FindWithTag("Player")?.transform;
    }

    private void Start()
    {
        if (spellBook == null)
        {
            Debug.LogError($"{name} has no SpellBook assigned!");
            return;
        }
        
        // Don't auto-start by default - MatchManager will call StartAI() when fighting begins
        isActive = false;
        
        // But if autoStartOnEnable is true, start immediately (useful for tutorials)
        if (autoStartOnEnable)
        {
            StartAI();
        }
    }
    
    private void OnEnable()
    {
        // Auto-start when GameObject is enabled (e.g., tutorial step activates PicoChan)
        if (autoStartOnEnable && spellBook != null)
        {
            StartAI();
        }
    }
    
    /// <summary>
    /// Starts the AI spell casting loop.
    /// </summary>
    public void StartAI()
    {
        if (!isActive)
        {
            isActive = true;
            StartCoroutine(AILoop());
            Debug.Log("[AISpellManager2] Started");
        }
    }

    /// <summary>
    /// Stops the AI spell casting loop.
    /// </summary>
    public void StopAI()
    {
        isActive = false;
        StopAllCoroutines();
        Debug.Log("[AISpellManager2] Stopped");
    }

    private IEnumerator AILoop()
    {
        while (isActive)
        {
            // wait before next spell
            yield return new WaitForSeconds(pauseBetweenSpells);

            // pick a random spell entry from the spellbook
            SpellBook.Entry entry = PickRandomEntry();

            // FOR TESTING ONE SPELL AT A TIME
            //entry = spellBook.GetEntryByName("Holy Sword");

            if (entry == null || entry.spell == null) continue;

            // only cast if enough mana
            if (!spellCaster.CanCast(entry.spell))
                continue;

            // face the player
            //AimAtPlayer();

            // show the gesture sequence visually
            yield return StartCoroutine(DisplayGestureSequence(entry.sequence));

            // finally cast
            spellCaster.Cast(entry.spell);
        }
    }

    private SpellBook.Entry PickRandomEntry()
    {
        var entries = spellBook.GetEntries();
        if (entries == null || entries.Count == 0)
            return null;

        return entries[Random.Range(0, entries.Count)];
    }

    private IEnumerator DisplayGestureSequence(List<GesturePair> sequence)
    {
        if (sequence == null || sequence.Count == 0)
            yield break;

        foreach (var pair in sequence)
        {
            gestureDisplay?.AddToUIBuffer(pair.Left, pair.Right);
            yield return new WaitForSeconds(gestureInterval);
        }

        gestureDisplay?.ClearUIBuffer();
    }

    // private void AimAtPlayer()
    // {
    //     if (player == null) return;
    //     Vector3 dir = (player.position - transform.position).normalized;
    //     transform.forward = dir;
    // }
}
