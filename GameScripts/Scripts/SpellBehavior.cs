using UnityEngine;

//[CreateAssetMenu(fileName = "SpellBehavior", menuName = "Spells/SpellBehavior")]
public abstract class SpellBehavior : ScriptableObject
{
     public abstract void Cast(SpellCaster caster, Spell spell, Transform target = null);
}
