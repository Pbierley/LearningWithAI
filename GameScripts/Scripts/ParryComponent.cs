using UnityEngine;

public class ParryComponent : MonoBehaviour
{
    public bool IsParrying { get; private set; }
    public float parryWindow = 0.3f;
    public int manaRestoreAmount = 10;

    public void StartParry()
    {
        IsParrying = true;
        Invoke(nameof(EndParry), parryWindow);
    }

    private void EndParry()
    {
        IsParrying = false;
    }

    public void OnSuccessfulParry(ProjectileBase projectile)
    {
        Debug.Log("Perfect parry! Mana restored.");
        //ManaSystem.Instance.RestoreMana(manaRestoreAmount);
        // Optional: play VFX/SFX, slow-mo, etc.
    }
}
