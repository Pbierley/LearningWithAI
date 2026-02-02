using UnityEngine;
using UnityEngine.Events;

public class ShieldComponent : MonoBehaviour
{
    public bool IsActive { get; private set; }
    public float activeTime = 4f;
    public GameObject shieldVFX;
    
    [Header("Debug")]
    [Tooltip("For testing: Start with shield already active")]
    public bool startActive = false;
    
    void Start()
    {
        if (startActive)
        {
            ActivateShield();
        }
    }

    [Header("Tutorial Integration")]
    [Tooltip("If true, notify TutorialManager when successfully blocking")]
    public bool notifyTutorialOnBlock = false;
    
    [Header("Events")]
    public UnityEvent OnShieldBlock;

    public void ActivateShield()
    {
        IsActive = true;
        Debug.Log($"Shield ACTIVATED on: {gameObject.name} (InstanceID: {GetInstanceID()})");

        if (shieldVFX) shieldVFX.SetActive(true);
        else Debug.Log("no public GameObject shieldVFX");
        
        Invoke(nameof(DeactivateShield), activeTime);
    }

    private void DeactivateShield()
    {
        IsActive = false;
        Debug.Log($"Shield DEACTIVATED on: {gameObject.name} (after {activeTime}s)");
        if (shieldVFX) shieldVFX.SetActive(false);
    }

    public void BlockProjectile(ProjectileBase projectile)
    {
        Debug.Log("Shield blocked projectile!");
        
        // Fire block event
        OnShieldBlock?.Invoke();
        
        // Notify TutorialManager if enabled (only used in tutorial scenes)
        if (notifyTutorialOnBlock && TutorialManager.Instance != null)
        {
            Debug.Log("TutorialManager.CompleteCurrentStep() called from shield block!");
            TutorialManager.Instance.CompleteCurrentStep();
        }
        
        // Destroy the projectile
        projectile.ImpactAndDestroy();
    }
}
