using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpellCaster : MonoBehaviour, IDamageable
{
    // Knows about spawn points (hand, staff tip, enemy, etc.).
    // Instantiates spell prefabs at the correct position.
    // Handles cleanup, particle auto-destroy, pooling, etc.
    // Later: handles mana, cooldowns, targeting.]

    [Header("Player stats")]
    public float maxHealth = 100f;
    public float currentHealth;
    public float maxMana = 100f;
    public float currentMana;

    [Header("Mana Restoration")]
    [SerializeField] private float manaRestoreDelay = 1f;
    [SerializeField] private float manaRestoreAmount = 2f;
    private Coroutine restoreCoroutine;

    [Header("Casting")]
    public Transform rightHandPoint;
    public Transform leftHandPoint;
    public Transform staffTip;
    public Transform centerPoint;
    public Transform underLeftHand;
    public Transform feetPoint; // for AOEs or self-buffs
    [SerializeField] private Transform enemyPoint; // where projectiles aim & aoe spells spawn

    [Header("Visuals")]
    [SerializeField] private GameObject fizzleVFX; // disabled GameObject under player
    [SerializeField] private GameObject outOfManaSmokeVFX; // disabled GameObject under player
    public GameObject leftHandAura;
    public GameObject rightHandAura;

    // Spell Casting
    public static event Action<SpellCaster, Spell> OnSpellCast; // <- global broadcast
    
    // Death event for MatchManager to subscribe
    public event Action<SpellCaster> OnDeath;
    public Animator characterAnimator;
    
    // Spell queue to handle multiple casts before animations complete
    private class QueuedSpell
    {
        public Spell spell;
        public int releasesRemaining;
    }
    private Queue<QueuedSpell> spellQueue = new Queue<QueuedSpell>();
    // private Transform currentTarget;

    [Header("HealthBar")]
    [SerializeField] HealthBar healthBar;

    [Header("ManaBar")]
    [SerializeField] ManaBar ManaBar;

    [Header("Hit Feedback")]
    [SerializeField] private float flashDuration = 0.5f; private Renderer[] allRenderers;
    private Color[] originalColors; // Store colors, not materials!

    [Header("Low Health Vignette")]
    [SerializeField] private LowHealthVignetteUI lowHealthVignette;
    [SerializeField] private float lowHealthThreshold = 0.3f; // 30%
    [SerializeField] private float criticalHealthThreshold = 0.15f; // 15%

    [Header("Shield")]
    [SerializeField] private ShieldComponent shield;

    [Header("Tutorial Integration")]
    [Tooltip("If true, notify TutorialManager when player heals (for tutorial scenes)")]
    public bool notifyTutorialOnHeal = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        //  initialize health
        characterAnimator = GetComponent<Animator>();
        currentHealth = maxHealth;
        healthBar.InitHealth(currentHealth);

        // initialize mana
        currentMana = maxMana;
        if (ManaBar != null)
        {
            ManaBar.InitMana(currentMana);
        }

        // Get all renderers on this character (including children)
        allRenderers = GetComponentsInChildren<Renderer>();

        // Store original COLORS (not materials!)
        if (allRenderers.Length > 0)
        {
            originalColors = new Color[allRenderers.Length];

            for (int i = 0; i < allRenderers.Length; i++)
            {
                Material mat = allRenderers[i].material; // Creates ONE instance per renderer

                // Store the original color
                if (mat.HasProperty("_BaseColor"))
                {
                    originalColors[i] = mat.GetColor("_BaseColor");
                }
                else if (mat.HasProperty("_Color"))
                {
                    originalColors[i] = mat.GetColor("_Color");
                }
            }

            Debug.Log($"Stored {originalColors.Length} original colors");
        }

        // Determine if this is the local player
        // SINGLE-PLAYER: Uses Unity tag "Player" to identify the local player
        // MULTIPLAYER: Replace CompareTag with network ownership check:
        //   - Photon: isLocal = photonView.IsMine;
        //   - Mirror: isLocal = isLocalPlayer;
        //   - Unity Netcode: isLocal = IsOwner;
        bool isLocal = gameObject.CompareTag("Player");

        // Register with GameManager (stores LocalPlayer reference)
        if (isLocal)
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetLocalPlayer(this);
            }
            else
            {
                Debug.LogWarning("[SpellCaster] GameManager.Instance is null! Make sure GameManager exists in the scene.");
            }
        }

        // Register with MatchManager (handles match flow, victory/defeat)
        if (MatchManager.Instance != null)
        {
            MatchManager.Instance.RegisterPlayer(this, isLocal);
        }
        else
        {
            Debug.LogWarning("[SpellCaster] MatchManager.Instance is null! Make sure MatchManager exists in the scene.");
        }

        // Initialize shield
        if (shield == null)
        {
            shield = GetComponent<ShieldComponent>();
            if (shield == null)
            {
                Debug.LogWarning("[SpellCaster] ShieldComponent is null! Make sure ShieldComponent exists in the scene.");
            }
        }

        // Initialize hand auras
        leftHandAura.SetActive(false);
        rightHandAura.SetActive(false);

    }

    public void Cast(Spell spell)
    {
        if (spell.spawnPrefab == null) return;
        if (!CanCast(spell) || currentMana < spell.manaCost)
        {
            Debug.Log($"Not enough mana for {spell.name}!!!");
            FindFirstObjectByType<CameraEffects>().Shake();
            PlaySmoke();
            return;
        }

        // spell.behavior.Cast(this, spell, enemyPoint);

        // characterAnimator.SetTrigger("CastProjectileRight");
        // StartCoroutine(CastAfterDelay(0.4f, spell));

        // Only enqueue if the spell needs releases
        if (spell.releaseCount > 0)
        {
            spellQueue.Enqueue(new QueuedSpell
            {
                spell = spell,
                releasesRemaining = spell.releaseCount
            });
        }

        Debug.Log($"In spellcaster.cast. Trying to cast {spell.name}. Activating characterAnimator.");
        // Trigger Animation Event
        if (spell.castTriggerName != null && spell.castTriggerName != "")
            characterAnimator.SetTrigger(spell.castTriggerName);
        else
            characterAnimator.SetTrigger("CastProjectileRight");
    }

    // private IEnumerator CastAfterDelay(float delay, Spell spell)
    // {
    //     Debug.Log("in castafterdelay");
    //     yield return new WaitForSeconds(delay);
    //     Debug.Log("After delay");
    //     spell.behavior.Cast(this, spell, enemyPoint);
    // }

    // Called by Animation Event
    public void OnReleaseSpell()
    {
        // Check if there's a spell in the queue
        if (spellQueue.Count == 0)
        {
            Debug.LogWarning("OnReleaseSpell called but spell queue is empty!");
            return;
        }

        // Peek the current spell (don't dequeue yet - multi-release spells need multiple calls)
        QueuedSpell queued = spellQueue.Peek();
        Spell spell = queued.spell;

        // Move this to cast() if we want to use mana instantly instead of waiting for the animation to finish
        UseMana(spell.manaCost / (float)spell.releaseCount);

        Debug.Log($"on spell release from animation event ({spell.spellName}) ----------");
        if (spell.behavior != null)
        {
            spell.behavior.Cast(this, spell, enemyPoint);
        }

        // Broadcast the event
        OnSpellCast?.Invoke(this, spell);

        // Decrement releases remaining and dequeue when done
        queued.releasesRemaining--;
        if (queued.releasesRemaining <= 0)
        {
            spellQueue.Dequeue();
        }
    }

    public bool CanCast(Spell spell)
    {
        return currentMana >= spell.manaCost;
    }

    public void TakeDamage(float dmg, Transform source)
    {
        if (MatchManager.Instance != null && !MatchManager.Instance.CanFight)
        {
            Debug.Log($"{name} cannot take damage during pre-round or post-match!");
            return;
        }

        characterAnimator.SetTrigger("DamagedTrigger");

        Debug.Log($"DAMAGED BY {dmg} AMOUNT");

        // Subtract damage from current health
        currentHealth -= dmg;

        Debug.Log($"CURRENT HEALTH {currentHealth}");
        Debug.Log($"MAX HEALTH {maxHealth}");

        // Update health bar
        healthBar.SetHealth(currentHealth);

        // Trigger low health vignette (only for local player)
        if (GameManager.Instance != null && GameManager.Instance.LocalPlayer == this && lowHealthVignette != null)
        {
            float healthPercent = currentHealth / maxHealth;

            if (healthPercent <= criticalHealthThreshold)
            {
                // Critical health - urgent pulse
                lowHealthVignette.PlayUrgentPulse();
            }
            else if (healthPercent <= lowHealthThreshold)
            {
                // Low health - normal pulse
                lowHealthVignette.PlayLowHealthVignette();
            }
        }

        // Flash on hit
        StartCoroutine(FlashOnHit());
        //StartCoroutine(HitStop(0.1f));

        if (currentHealth <= 0) Die();
    }

    private IEnumerator FlashOnHit()
    {
        if (allRenderers == null || allRenderers.Length == 0)
        {
            Debug.LogWarning("No renderers found on character!");
            yield break;
        }

        Debug.Log($"Flashing {allRenderers.Length} renderers");

        // Flash all materials to red
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                Material mat = allRenderers[i].material;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", Color.red);
                    //Debug.Log($"Set {allRenderers[i].name} to red");
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", Color.red);
                }
            }
        }

        yield return new WaitForSecondsRealtime(flashDuration);

        // Restore original colors
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] != null)
            {
                Material mat = allRenderers[i].material;

                if (mat.HasProperty("_BaseColor"))
                {
                    mat.SetColor("_BaseColor", originalColors[i]);
                    //Debug.Log($"Restored {allRenderers[i].name} to original color: {originalColors[i]}");
                }
                else if (mat.HasProperty("_Color"))
                {
                    mat.SetColor("_Color", originalColors[i]);
                }
            }
        }

        Debug.Log("Flash complete!");
    }

    // private IEnumerator HitStop(float duration = 0.05f)
    // {
    //     Time.timeScale = 0f;
    //     yield return new WaitForSecondsRealtime(duration);
    //     Time.timeScale = 1f;
    // }

    public void UseMana(float mana)
    {
        characterAnimator.SetTrigger("ManaUsedTrigger");
        Debug.Log($"Used {mana} Mana");
        currentMana -= mana;
        Debug.Log($"CURRENT Mana {currentMana}");
        Debug.Log($"MAX Mana {maxMana}");
        ManaBar.SetMana(currentMana);
        StartManaRestore();
    }

    private void Die()
    {
        Debug.Log($"{name} died!");
        characterAnimator.SetTrigger("Die");
        OnDeath?.Invoke(this);
    }

    // 🔥 Mini-pool style fizzle feedback
    public void PlayFizzle()
    {
        if (fizzleVFX == null)
        {
            Debug.Log("fizzle effect is null");
            return;
        }

        StartCoroutine(FizzleRoutine());
    }

    private IEnumerator FizzleRoutine()
    {
        fizzleVFX.SetActive(true);
        var ps = fizzleVFX.GetComponent<ParticleSystem>();
        ps?.Play();

        // Wait for duration or default 1s
        yield return new WaitForSeconds(ps?.main.duration ?? 1f);

        fizzleVFX.SetActive(false);
    }

    // 🔥 Mini-pool style fizzle feedback
    private void PlaySmoke()
    {
        if (fizzleVFX == null)
        {
            Debug.Log("fizzle effect is null");
            return;
        }

        StartCoroutine(SmokeRoutine());
    }

    private IEnumerator SmokeRoutine()
    {
        outOfManaSmokeVFX.SetActive(true);
        var ps = outOfManaSmokeVFX.GetComponent<ParticleSystem>();
        ps?.Play();

        // Wait for duration or default 1s
        yield return new WaitForSeconds(ps?.main.duration ?? 1f);

        outOfManaSmokeVFX.SetActive(false);
    }

    // Mana restoration logic - restores mana over time
    private IEnumerator RestoreMana()
    {
        // initial delay before starting the restore
        yield return new WaitForSeconds(manaRestoreDelay);

        if (currentMana >= maxMana)
        {
            yield break; // already full
        }

        float remaining = maxMana - currentMana;

        // keep restoring manaRestoreAmount per second (last step may be less)
        while (remaining > 0f && currentMana < maxMana)
        {
            float amountToRestore = Mathf.Min(manaRestoreAmount, remaining, maxMana - currentMana);
            currentMana += amountToRestore;
            ManaBar.SetMana(currentMana);
            remaining -= amountToRestore;
            yield return new WaitForSeconds(1f);
        }
    }

    // Start or restart the mana restoration process
    public void StartManaRestore()
    {
        // stop any existing restore first
        StopManaRestore();

        restoreCoroutine = StartCoroutine(RestoreMana());
    }

    // Stop the mana restoration
    public void StopManaRestore()
    {
        if (restoreCoroutine != null)
        {
            StopCoroutine(restoreCoroutine);
            restoreCoroutine = null;
        }
    }

    public void Heal(float amount, Transform source)
    {
        Debug.Log($"Healed by {amount}");
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth); // Cap at max health
        Debug.Log($"CURRENT HEALTH {currentHealth}");
        Debug.Log($"MAX HEALTH {maxHealth}");
        healthBar.SetHealth(currentHealth);

        // Stop low health vignette if health is restored above threshold (only for local player)
        if (GameManager.Instance != null && GameManager.Instance.LocalPlayer == this && lowHealthVignette != null)
        {
            float healthPercent = currentHealth / maxHealth;

            if (healthPercent > lowHealthThreshold)
            {
                // Health is back to safe levels - stop the vignette
                lowHealthVignette.StopLowHealthVignette();
            }
        }

        // Notify TutorialManager if enabled (only used in tutorial scenes)
        if (notifyTutorialOnHeal && TutorialManager.Instance != null)
        {
            Debug.Log("TutorialManager.CompleteCurrentStep() called from heal!");
            TutorialManager.Instance.CompleteCurrentStep();
        }

        // Optional: trigger healing animation
        // characterAnimator.SetTrigger("HealTrigger");
    }


}
