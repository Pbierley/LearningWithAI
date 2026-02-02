using UnityEngine;

[CreateAssetMenu]
public class ShieldSpellBehavior : SpellBehavior
{
    public override void Cast(SpellCaster caster, Spell spell, Transform target = null)
    {
        var shield = caster.GetComponent<ShieldComponent>();
        Debug.Log("in ShieldSpellBehavior");
        if (shield != null)
        {
            Debug.Log("shield != null");
            shield.ActivateShield();
        }
    }
}

