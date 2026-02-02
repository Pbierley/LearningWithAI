using UnityEngine;
using UnityEngine.Events;


[RequireComponent(typeof(Collider))]
public class TutorialCrystal : MonoBehaviour, IDamageable
{
    [Header("Effects")]
    [Tooltip("Optional VFX to spawn when crystal is destroyed")]
    public GameObject destroyVFX;
    
    [Tooltip("How long before the VFX is cleaned up")]
    public float vfxLifetime = 2f;

    [Header("Audio")]
    [Tooltip("Optional sound to play when destroyed")]
    public AudioClip destroySound;
    
    [Header("Events")]
    [Tooltip("Event fired when the crystal is destroyed")]
    public UnityEvent OnCrystalDestroyed;

    [Header("Settings")]
    [Tooltip("If true, destroy the gameObject. If false, just deactivate it.")]
    public bool destroyOnHit = true;
    
    [Tooltip("If true, automatically notify TutorialManager when destroyed")]
    public bool notifyTutorialManager = true;

    private bool hasBeenHit = false;

    /// <summary>
    /// Called by ProjectileBase when a mana ball hits this crystal.
    /// </summary>
    public void TakeDamage(float amount, Transform source)
    {
        if (hasBeenHit) return; // Prevent multiple triggers
        hasBeenHit = true;

        Debug.Log($"TutorialCrystal hit by {source?.name ?? "unknown"} for {amount} damage!");

        // Spawn VFX if assigned
        if (destroyVFX != null)
        {
            GameObject vfx = Instantiate(destroyVFX, transform.position, Quaternion.identity);
            Destroy(vfx, vfxLifetime);
        }

        // Play sound if assigned
        if (destroySound != null)
        {
            AudioSource.PlayClipAtPoint(destroySound, transform.position);
        }

        // Fire the event (can be used to trigger tutorial progression, UI updates, etc.)
        OnCrystalDestroyed?.Invoke();

        // Notify TutorialManager if enabled
        if (notifyTutorialManager && TutorialManager.Instance != null)
        {
            TutorialManager.Instance.CompleteCurrentStep();
        }

        // Destroy or deactivate the crystal
        if (destroyOnHit)
        {
            Destroy(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        // Check if it's a projectile
        ProjectileBase projectile = other.GetComponent<ProjectileBase>();
        if (projectile != null && !hasBeenHit)
        {
            TakeDamage(projectile.damage, other.transform);
        }
    }
}

