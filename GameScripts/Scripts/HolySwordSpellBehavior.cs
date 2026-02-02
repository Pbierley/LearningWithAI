using UnityEngine;

[CreateAssetMenu(fileName = "HolySwordSpellBehavior", menuName = "Spells/HolySwordSpellBehavior")]
public class HolySwordSpellBehavior : SpellBehavior
{
    public float heightOffset = 0.1f;
    public override void Cast(SpellCaster caster, Spell spell, Transform target)
    {

        // Spawn visual effect and auto-cleanup
        if (spell.spawnPrefab == null)
        {
            Debug.Log("forgot the spell prefab");
            return;
        }

        var vfx = Instantiate(spell.spawnPrefab, target.position + Vector3.up * heightOffset, caster.transform.rotation);
        var hitbox = vfx.GetComponentInChildren<HolySwordHitbox>();

        if (hitbox) hitbox.Init(caster, spell);
        else Debug.Log("didn't get HolySwordHitbox");

        Destroy(vfx, spell.lifetime);

        // // Apply instant damage
        // Collider[] hits = Physics.OverlapSphere(center, radius);
        // foreach (var h in hits)
        // {
        //     var dmg = h.GetComponent<IDamageable>();
        //     if (dmg != null)
        //         dmg.TakeDamage(spell.damage, caster.transform);
        // }
    }
}
