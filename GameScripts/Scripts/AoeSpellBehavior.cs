using UnityEngine;

[CreateAssetMenu(fileName = "Aoe Spell Behavior", menuName = "Spells/AoeSpellBehavior")]
public class AoeSpellBehavior : SpellBehavior
{
    public float radius = 5f;
    public float heightOffset = 0.1f;
  

    public override void Cast(SpellCaster caster, Spell data, Transform target)
    {
        // Get Target Points
        var targets = target.GetComponent<TargetPoints>();
        if (targets == null)
        {
            Debug.LogWarning("Missing TargetPoints on target!");
            return;
        }

        var targetFeet = targets.feet.position + Vector3.up * heightOffset;

        Vector3 center = target != null
            ? targetFeet        // AOE on enemy
            : caster.centerPoint.position;  // AOE around caster

        // Spawn visual effect and auto-cleanup
        if (data.spawnPrefab)
        {
            var vfx = Instantiate(data.spawnPrefab, center, caster.transform.rotation);
            Destroy(vfx, data.lifetime);
        }

        FindFirstObjectByType<CameraEffects>().Shake();

        // Apply instant damage
        Collider[] hits = Physics.OverlapSphere(center, radius);
        foreach (var h in hits)
        {
            // --- SHIELD CHECK ---
            ShieldComponent shield = h.GetComponent<ShieldComponent>();
            if (shield != null && shield.IsActive)
            {
                //shield.BlockProjectile(this);
                Debug.Log("Shield blocked the spell");
                return;
            }

            var dmg = h.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(data.damage, caster.transform);
        }
    }
}
