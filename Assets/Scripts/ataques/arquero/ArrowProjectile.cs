using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class ArrowProjectile : MonoBehaviour
{
    [Header("Stats")]
    public float speed = 6f;
    public float damage = 50f;
    public float maxLifetime = 5f;

    [Header("Viento (lvl 5+)")]
    public bool windTrailEnabled = false;
    public float windLengthMultiplier = 4f;  // largo del rastro
    public float windHeightMultiplier = 1f;  // alto del rastro
    public LayerMask enemyMask;              // capa de enemigos para el rastro

    [Header("Viento visual")]
    public Transform windVisual;             // hijo con el sprite del viento
    Vector3 baseWindScale;

    Vector2 direction = Vector2.right;

    BoxCollider2D boxCol;
    Vector2 baseSize;
    Vector2 baseOffset;

    void Awake()
    {
        // collider principal
        var col = GetComponent<Collider2D>();
        col.isTrigger = true;

        boxCol = col as BoxCollider2D;
        if (boxCol != null)
        {
            baseSize = boxCol.size;
            baseOffset = boxCol.offset;
        }

        if (windVisual != null)
            baseWindScale = windVisual.localScale;

        // rigidbody
        var rb = GetComponent<Rigidbody2D>();
        if (!rb) rb = gameObject.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.gravityScale = 0f;
    }

    /// <summary>
    /// Se llama desde ArcherAutoAttack cuando se instancia la flecha.
    /// </summary>
    public void Init(Vector2 dir, float dmg, float spd, bool enableWind)
    {
        direction = dir.normalized;
        damage = dmg;
        speed = spd;
        windTrailEnabled = enableWind;

        SetupWindCollider();
        SetupWindVisual();
    }

    void SetupWindCollider()
    {
        if (boxCol == null) return;

        if (!windTrailEnabled)
        {
            // tamaño original
            boxCol.size = baseSize;
            boxCol.offset = baseOffset;
            return;
        }

        // estirar el collider a lo largo
        Vector2 newSize = baseSize;
        newSize.x *= windLengthMultiplier;
        newSize.y *= windHeightMultiplier;

        float extraLength = newSize.x - baseSize.x;
        Vector2 newOffset = baseOffset + new Vector2(-extraLength * 0.5f, 0f);

        boxCol.size = newSize;
        boxCol.offset = newOffset;
    }

    void SetupWindVisual()
    {
        if (windVisual == null) return;

        if (!windTrailEnabled)
        {
            windVisual.gameObject.SetActive(false);
            windVisual.localScale = baseWindScale;
            return;
        }

        windVisual.gameObject.SetActive(true);

        Vector3 s = baseWindScale;
        s.x *= windLengthMultiplier;
        s.y *= windHeightMultiplier;
        windVisual.localScale = s;
    }

    void Update()
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

    void OnTriggerEnter2D(Collider2D other)
    {
        // solo capas de enemigo (para no pegar al player/props)
        if (enemyMask.value != 0 &&
            (enemyMask.value & (1 << other.gameObject.layer)) == 0)
            return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        enemy.TakeDamage(damage);

        // si el viento está activo, la flecha atraviesa y sigue pegando a otros
        if (windTrailEnabled)
            return;

        // si no hay viento, se destruye en el primer impacto
        Destroy(gameObject);
    }
}
