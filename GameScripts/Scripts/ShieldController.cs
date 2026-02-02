using System.Collections;
using UnityEngine;

public class ShieldController : MonoBehaviour
{
    private float lifetime;
    private SpellCaster owner;
    public GameObject shieldVFX;

    public void Init(SpellCaster caster, float life)
    {
        owner = caster;
        lifetime = life;

        if (shieldVFX) shieldVFX.SetActive(true);
        StartCoroutine(LifetimeRoutine());
    }

    private IEnumerator LifetimeRoutine()
    {
        yield return new WaitForSeconds(lifetime);
        if (shieldVFX) shieldVFX.SetActive(false);
        Destroy(gameObject);
    }

    void OnTriggerEnter(Collider other)
    {
        
    }

}
