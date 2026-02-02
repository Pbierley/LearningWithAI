using UnityEngine;

[CreateAssetMenu(fileName = "HealSpellBehavior", menuName = "Spells/HealSpellBehavior")]
public class HealSpellBehavior : SpellBehavior
{
    public float heightOffset = 0.1f;

    public override void Cast(SpellCaster caster, Spell spell, Transform target)
    {
        // Spawn visual effect on the caster (healing particles)
        if (spell.spawnPrefab)
        {
            Vector3 spawnPos = caster.feetPoint ? caster.feetPoint.position + Vector3.up * heightOffset : caster.transform.position;
            var vfx = Instantiate(spell.spawnPrefab, spawnPos, caster.transform.rotation);
            Destroy(vfx, spell.lifetime);
        }

        // Heal the caster
        // Note: spell.damage is reused as healAmount (or add a new field to Spell)
        caster.Heal(spell.damage, caster.transform);

        Debug.Log($"Cast heal spell: restored {spell.damage} health");
    }
}