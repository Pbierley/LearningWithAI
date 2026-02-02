using System.Collections;
using UnityEngine;

[CreateAssetMenu(fileName = "SlashSpellBehavior", menuName = "Spells/SlashSpellBehavior")]
public class SlashSpellBehavior : SpellBehavior
{


    public override void Cast(SpellCaster caster, Spell spell, Transform target)
    {

        // Spawn visual effect and auto-cleanup
        if (spell.spawnPrefab == null)
        {
            Debug.Log("forgot the spell prefab");
            return;
        }

        var vfx = Instantiate(spell.spawnPrefab, caster.leftHandPoint.position, caster.transform.rotation);
        Destroy(vfx, spell.lifetime);

        var front = vfx.GetComponentInChildren<SweepingDamageFront>();

        if (front)
            caster.StartCoroutine(DelayedInit(front, caster, spell, 0.45f));
        else
            Debug.Log("didn't get SweepingDamageFront");

    }
    
    private IEnumerator DelayedInit(SweepingDamageFront front, SpellCaster caster, Spell spell, float seconds)
    {
        yield return new WaitForSeconds(seconds);
        front.Init(caster, spell);
    }
}
