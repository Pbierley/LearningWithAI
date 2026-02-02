using UnityEngine;

[CreateAssetMenu(fileName = "ProjectileSpellBehavior", menuName = "Spells/ProjectileSpellBehavior")]
public class ProjectileSpellBehavior : SpellBehavior
{
    public override void Cast(SpellCaster caster, Spell spell, Transform target)
    {
        // Where to fire from — could be dynamic
        Transform firePoint = caster.staffTip ? caster.staffTip : caster.rightHandPoint;

        // Get Target Points
        var targets = target.GetComponent<TargetPoints>();
        if (targets == null)
        {
            Debug.LogWarning("Missing TargetPoints on target!");
            return;
        }

        // Aim at chest
        var chest = targets.chest;
        Vector3 dir = (target != null)
            ? (chest.position - firePoint.position).normalized
            : firePoint.forward;

        // Instantiate the projectile prefab (it should have Projectile Base script attached)
        ProjectileBase p = Instantiate(
            spell.spawnPrefab,
            firePoint.position,
            Quaternion.LookRotation(dir)
        ).GetComponent<ProjectileBase>();

        // Fire projectile
        p.Fire(dir, caster.transform);
        p.damage = spell.damage / spell.releaseCount;
    }

    public void OnSpellRelease()
    {
        Debug.Log("=========== on spell release from Projectile Spell Behavior (not used) ----------");
        //spell.behavior.Cast(this, spell, enemyPoint);
    }
}


