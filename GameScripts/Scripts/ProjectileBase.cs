using System;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class ProjectileBase : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 20f;
    public float damage = 10f;
    public float lifetime = 5f;
    public bool destroyOnHit = true;

    [Header("References")]
    public GameObject impactVFX;

    private Rigidbody rb;
    private Transform owner;
    private float lifeTimer;


    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    public void Fire(Vector3 direction, Transform firedBy)
    {
        //transform.rotation = Quaternion.LookRotation(direction);
        owner = firedBy;
        lifeTimer = 0f;
        rb.linearVelocity = direction * speed;
    }

    void Update()
    {
        lifeTimer += Time.deltaTime;
        if (lifeTimer >= lifetime)
            Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.transform == owner) return; // don't hit self\

        Debug.Log($"Projectile hit: {other.name} (tag: {other.tag}, layer: {LayerMask.LayerToName(other.gameObject.layer)})");

        // --- SHIELD CHECK ---
        // Use GetComponentInParent to find ShieldComponent on parent objects (e.g., shield VFX is child of player)
        ShieldComponent shield = other.GetComponentInParent<ShieldComponent>();
        if (shield != null)
        {
            Debug.Log($"Found ShieldComponent on: {shield.gameObject.name} (InstanceID: {shield.GetInstanceID()}), IsActive={shield.IsActive}");
            if (shield.IsActive)
            {
                shield.BlockProjectile(this);
                return;
            }
            else
            {
                Debug.Log("Shield found but NOT active - projectile passes through! Make sure the shield gesture activates THIS ShieldComponent.");
            }
        }

        // --- PARRY CHECK ---
        ParryComponent parry = other.GetComponent<ParryComponent>();
        if (parry != null && parry.IsParrying)
        {
            parry.OnSuccessfulParry(this);
            ReflectProjectile(parry.transform);
            return;
        }

        // --- NORMAL DAMAGE ---
        IDamageable target = other.GetComponent<IDamageable>();
        if (target != null)
            target.TakeDamage(damage, owner);

        ImpactAndDestroy();
    }

    private void ReflectProjectile(Transform newTarget)
    {
        // Simple reflection — just reverse direction toward target
        Vector3 dir = (newTarget.position - transform.position).normalized;
        rb.linearVelocity = dir * speed;
        owner = newTarget;
    }

    public void ImpactAndDestroy()
    {
        if (impactVFX)
        {
            GameObject i = Instantiate(impactVFX, transform.position, Quaternion.identity);
            Destroy(i, 1);
        }


        if (destroyOnHit)
            Destroy(gameObject);
            Debug.Log("Projectile destroyed at time " + Time.time);
    }
}
