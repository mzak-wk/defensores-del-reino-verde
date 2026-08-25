using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SlashAttack : MonoBehaviour
{
    [Header("Combat Stats (set via Init)")]
    [SerializeField] private float damage = 50f;
    [SerializeField] private float lifetime = 0.35f;
    [SerializeField] private int maxHitsPerActivation = 999;
    [SerializeField] private bool destroyOnFirstHit = false;
    [SerializeField] private float forwardDistance = 1.2f;

    [Header("Visual / Behavior (Inspector)")]
    [SerializeField] private float initialScaleMultiplier = 0.5f;
    [SerializeField] private float finalScaleMultiplier = 1.2f;
    [SerializeField] private float initialAlpha = 1.0f;
    [SerializeField] private float finalAlpha = 0.0f;
    [SerializeField] private bool animateScaleAndFade = true;
    [SerializeField] private bool useUnscaledTime = false;
    [SerializeField] private bool autoSquareHitbox = true;
    [SerializeField] private float squareMultiplier = 1.0f;
    [SerializeField] private float padding = 0.15f;
    [SerializeField] private bool faceRight = true;
    [SerializeField] private bool trackFacing = false;
    [SerializeField] private bool followTarget = true;
    [SerializeField] private float verticalOffset = 0f;
    [SerializeField] private LayerMask enemyMask;

    private BoxCollider2D box;
    private SpriteRenderer sr;
    private readonly HashSet<Collider2D> hitSet = new();
    private float expireAt;
    private float initialZ;
    private float spawnTime;
    private Transform target;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;
        sr = GetComponentInChildren<SpriteRenderer>();

        if (enemyMask.value == 0)
            enemyMask = LayerMask.GetMask("Enemy");
    }

    public void Init(
        Transform target,
        Vector2 dir,
        float damage,
        float lifetime,
        int maxHitsPerActivation,
        bool destroyOnFirstHit,
        float forwardDistance)
    {
        this.target = target;
        this.damage = damage;
        this.lifetime = lifetime;
        this.maxHitsPerActivation = maxHitsPerActivation;
        this.destroyOnFirstHit = destroyOnFirstHit;
        this.forwardDistance = forwardDistance;

        float signX = 1f;
        if (Mathf.Abs(dir.x) > 0.01f)
            signX = dir.x > 0f ? 1f : -1f;
        else if (target != null)
        {
            var srTarget = target.GetComponentInChildren<SpriteRenderer>();
            if (srTarget != null)
                signX = srTarget.flipX ? -1f : 1f;
        }
        faceRight = signX > 0f;

        initialZ = faceRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, initialZ);

        RebuildSquareHitbox();
        ForceFollowNow();

        if (sr && animateScaleAndFade)
        {
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, initialAlpha);
            transform.localScale = Vector3.one * initialScaleMultiplier;
        }

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        spawnTime = now;
        expireAt = now + Mathf.Max(0.01f, lifetime);

        hitSet.Clear();
        gameObject.SetActive(true);
    }

    void Update()
    {
        if (!target)
        {
            Destroy(gameObject);
            return;
        }

        if (followTarget)
        {
            if (trackFacing && target != null)
            {
                var targetSr = target.GetComponentInChildren<SpriteRenderer>();
                if (targetSr != null)
                {
                    faceRight = !targetSr.flipX;
                }
            }

            float sign = faceRight ? 1f : -1f;
            float halfSquareWorld = box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);
            Vector3 pos = target.position + new Vector3(sign * (halfSquareWorld + forwardDistance), verticalOffset, 0f);
            transform.position = pos;
            transform.rotation = Quaternion.Euler(0f, 0f, initialZ);
        }

        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (sr && animateScaleAndFade)
        {
            float totalDuration = lifetime;
            float elapsedTime = now - spawnTime;
            float t = Mathf.Clamp01(elapsedTime / totalDuration);

            float currentScale = Mathf.Lerp(initialScaleMultiplier, finalScaleMultiplier, t);
            transform.localScale = Vector3.one * currentScale;

            float currentAlpha = Mathf.Lerp(initialAlpha, finalAlpha, t);
            Color currentColor = sr.color;
            currentColor.a = currentAlpha;
            sr.color = currentColor;
        }

        if (now >= expireAt)
        {
            Destroy(gameObject);
        }
    }

    void RebuildSquareHitbox()
    {
        if (!autoSquareHitbox) return;

        float size = 1f;
        if (sr && sr.sprite)
        {
            var b = sr.sprite.bounds;
            size = Mathf.Max(b.size.x, b.size.y);
        }
        size = size * squareMultiplier + (padding * 2f);

        box.size = new Vector2(size, size);
        box.offset = Vector2.zero;
    }

    void ForceFollowNow()
    {
        if (!target) return;
        float sign = faceRight ? 1f : -1f;
        float halfSquareWorld = box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);
        transform.position = target.position + new Vector3(sign * (halfSquareWorld + forwardDistance), verticalOffset, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if ((enemyMask.value & (1 << other.gameObject.layer)) == 0) return;
        if (hitSet.Contains(other)) return;

        var enemy = other.GetComponentInParent<Enemy>();
        if (enemy)
        {
            enemy.TakeDamage(damage);
            hitSet.Add(other);
            if (destroyOnFirstHit) Destroy(gameObject);
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (!box) box = GetComponent<BoxCollider2D>();
        Gizmos.color = new Color(0, 1, 1, 0.25f);
        var center = transform.TransformPoint((Vector3)box.offset);
        var size = Vector3.Scale((Vector3)box.size, transform.lossyScale);
        Gizmos.DrawCube(center, size);
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(center, size);
    }
#endif
}