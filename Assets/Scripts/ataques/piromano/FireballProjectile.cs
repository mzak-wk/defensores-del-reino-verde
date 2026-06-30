using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireballProjectile : MonoBehaviour
{
    [Header("Ajustes generales")]
    public float maxLifeTime = 5f;

    Transform target;
    float damage;
    float speed;

    bool spawnAreaOnHit;
    GameObject areaPrefab;
    float areaDamagePerSecond;
    float areaDuration;
    float areaScaleMultiplier;
    Color projectileColor = Color.white;
    Color areaColor = Color.white;

    SpriteRenderer sr;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponentInChildren<SpriteRenderer>();
    }

    public void Init(
        Transform target,
        float damage,
        float speed,
        bool spawnAreaOnHit,
        GameObject areaPrefab,
        float areaDamagePerSecond,
        float areaDuration,
        float areaScaleMultiplier,
        Color projectileColor,
        Color areaColor)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.spawnAreaOnHit = spawnAreaOnHit;
        this.areaPrefab = areaPrefab;
        this.areaDamagePerSecond = areaDamagePerSecond;
        this.areaDuration = areaDuration;
        this.areaScaleMultiplier = areaScaleMultiplier;
        this.areaColor = areaColor;
        this.projectileColor = projectileColor;

        if (sr != null)
            sr.color = projectileColor;

        Destroy(gameObject, maxLifeTime);
    }

    void Update()
    {
        if (!target)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;

        // Si está muy cerca del objetivo, consideramos impacto
        if ((target.position - transform.position).sqrMagnitude < 0.05f)
        {
            Hit(target.position, target);
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!target) return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null && enemy.transform == target)
        {
            Hit(other.transform.position, enemy.transform);
        }
    }

    void Hit(Vector3 hitPos, Transform enemyTransform)
    {
        var enemy = enemyTransform.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }

        if (spawnAreaOnHit && areaPrefab != null)
        {
            GameObject areaGO = Instantiate(areaPrefab, hitPos, Quaternion.identity);

            // Escala de área según el nivel
            areaGO.transform.localScale *= areaScaleMultiplier;

            var area = areaGO.GetComponent<FireAreaDamage>();
            if (area != null)
            {
                area.Configure(areaDamagePerSecond, areaDuration, areaColor);
            }
        }

        Destroy(gameObject);
    }
}
