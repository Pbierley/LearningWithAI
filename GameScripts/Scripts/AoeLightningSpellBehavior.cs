using UnityEngine;

[CreateAssetMenu]
public class AoeLightningSpell : SpellBehavior
{
    public float radius = 5f;

    public override void Cast(SpellCaster caster, Spell spell, Transform t = null)
    {
        Collider[] hits = Physics.OverlapSphere(caster.transform.position, radius);
        foreach (var h in hits)
        {
            var dmg = h.GetComponent<IDamageable>();
            if (dmg != null)
                dmg.TakeDamage(spell.damage, caster.transform);
        }
    }
}

