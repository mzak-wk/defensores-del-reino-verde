using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Combat Stats (set via Init)")]
    [SerializeField] private float speed = 6f;
    [SerializeField] private float damage = 50f;
    [SerializeField] private float maxLifetime = 5f;

    [Header("Visual / Behavior (Inspector)")]
    [SerializeField] private GameObject freezeExplosionPrefab;
    [SerializeField] private LayerMask enemyMask;

    private Vector2 direction = Vector2.right;

    private void Awake()
    {
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;

        if (enemyMask.value == 0)
            enemyMask = LayerMask.GetMask("Enemy");
    }

    public void Init(Vector2 dir, float dmg, float spd, float maxLifetime)
    {
        damage = dmg;
        speed = spd;
        this.maxLifetime = maxLifetime;

        float signX = 1f;

        if (Mathf.Abs(dir.x) > 0.01f)
        {
            signX = dir.x > 0f ? 1f : -1f;
        }
        else
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                SpriteRenderer sr = player.GetComponentInChildren<SpriteRenderer>();
                if (sr != null)
                {
                    signX = sr.flipX ? -1f : 1f;
                }
            }
        }

        direction = new Vector2(signX, 0f);

        transform.rotation = signX < 0f
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;
    }

    private void Update()
    {
        transform.position += (Vector3)(direction * speed * Time.deltaTime);

        maxLifetime -= Time.deltaTime;
        if (maxLifetime <= 0f)
        {
            Destroy(gameObject);
            return;
        }

        Camera cam = Camera.main;
        if (cam != null)
        {
            Vector3 vp = cam.WorldToViewportPoint(transform.position);
            if (vp.z < 0f || vp.x < 0f || vp.x > 1f || vp.y < 0f || vp.y > 1f)
            {
                Destroy(gameObject);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (enemyMask.value != 0 &&
            (enemyMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);

        SpawnFreezeExplosion();

        Destroy(gameObject);
    }

    private void SpawnFreezeExplosion()
    {
        if (freezeExplosionPrefab == null) return;

        GameObject explosion = Instantiate(freezeExplosionPrefab, transform.position, Quaternion.identity);
        var freeze = explosion.GetComponent<FreezeExplosion>();
        if (freeze != null)
        {
            freeze.damage = damage;
        }
    }
}