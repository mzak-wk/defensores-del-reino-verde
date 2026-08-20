using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class FireballProjectile : MonoBehaviour
{
    [Header("Combat Stats (set via Init)")]
    [SerializeField] private float maxLifeTime = 5f;
    [SerializeField] private float damage = 75f;
    [SerializeField] private float speed = 10f;

    [Header("Area Config (Inspector)")]
    [SerializeField] private bool spawnAreaOnHit;
    [SerializeField] private GameObject areaPrefab;
    [SerializeField] private float areaDamagePerSecond;
    [SerializeField] private float areaDuration;
    [SerializeField] private float areaScaleMultiplier;

    [Header("Visuals (Inspector)")]
    [SerializeField] private Color projectileColor = Color.white;
    [SerializeField] private Color areaColor = Color.white;
    [SerializeField] private LayerMask enemyMask;

    Transform target;
    SpriteRenderer sr;

    void Awake()
    {
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;
        sr = GetComponentInChildren<SpriteRenderer>();

        if (enemyMask.value == 0)
            enemyMask = LayerMask.GetMask("Enemy");
    }

    public void Init(
        Transform target,
        float damage,
        float speed,
        float maxLifeTime)
    {
        this.target = target;
        this.damage = damage;
        this.speed = speed;
        this.maxLifeTime = maxLifeTime;

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