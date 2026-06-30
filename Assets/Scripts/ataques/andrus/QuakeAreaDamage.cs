using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuakeAreaDamage : MonoBehaviour
{
    [Header("Daño")]
    public float damage = 100f;

    [Header("Vida")]
    public float lifeTime = 0.5f;

    [Header("Temblor (nivel 10)")]
    public bool enableShake = false;
    public float shakeIntensity = 0.05f;
    public float shakeFrequency = 30f;
    [Header("Rango")]
    public float radiusMultiplier = 1f;   // ← lo que viene del PlayerAutoAttack

    Vector3 baseScale; 

    HashSet<Enemy> alreadyHit = new HashSet<Enemy>();
    Vector3 originalPos;

    void OnEnable()
    {
        originalPos = transform.position;
        transform.localScale = baseScale * radiusMultiplier;

        if (lifeTime > 0f)
            StartCoroutine(DieAfterDelay());

        if (enableShake)
            StartCoroutine(ShakeRoutine());
    }
    void Awake()
{
    baseScale = transform.localScale;

    var col = GetComponent<Collider2D>();
    col.isTrigger = true;
}


    IEnumerator DieAfterDelay()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }

    IEnumerator ShakeRoutine()
    {
        float elapsed = 0f;
        while (elapsed < lifeTime)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.value * shakeIntensity;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            transform.position = originalPos + offset;

            float dt = 1f / shakeFrequency;
            elapsed += dt;
            yield return new WaitForSeconds(dt);
        }

        // volver al centro antes de destruirse
        transform.position = originalPos;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // buscar el script Enemy en este objeto o en sus padres
        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        if (alreadyHit.Contains(enemy)) return; // ya le pegamos antes

        enemy.TakeDamage(damage);
        alreadyHit.Add(enemy);

        Debug.Log($"[QuakeTrigger] golpeó a {enemy.name} por {damage} de daño");
    }
}
