using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class TornadoAreaDamage : MonoBehaviour
{
    [Header("Daño")]
    public float damagePerSecond = 100f;

    [Header("Vida")]
    public float lifeTime = 5f;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
    }

    void OnEnable()
    {
        if (lifeTime > 0f)
            StartCoroutine(DieAfterTime());
    }

    IEnumerator DieAfterTime()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    void OnTriggerStay2D(Collider2D other)
    {
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damagePerSecond * Time.deltaTime);
    }
}
