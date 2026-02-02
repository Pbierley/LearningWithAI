using UnityEngine;
using System.Collections.Generic;

public class HolySwordHitbox : MonoBehaviour
{
    public float fallSpeed = 30f;
    public float lifeTime = 1.2f;

    private SpellCaster owner;
    private Spell spell;
    private float life;
    private HashSet<Collider> hitAlready = new();

    public void Init(SpellCaster caster, Spell spellData)
    {
        owner = caster;
        spell = spellData;
    }

    void Update()
    {
        // Move the hitbox downward to match the sword visual
        transform.position += Vector3.down * fallSpeed * Time.deltaTime;

        life += Time.deltaTime;
        if (life > lifeTime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        // ignore self
        if (owner && other.transform == owner.transform) return;
        if (hitAlready.Contains(other)) return;

        hitAlready.Add(other);

        // --- SHIELD CHECK ---
        ShieldComponent shield = other.GetComponent<ShieldComponent>();
        if (shield != null && shield.IsActive)
        {
            //shield.BlockProjectile(this);
            Debug.Log("Shield blocked the spell");
            return;
        }

        if (other.TryGetComponent(out IDamageable dmg))
        {
            dmg.TakeDamage(spell.damage, owner.transform);
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, GetComponent<BoxCollider>().size);
    }
}
