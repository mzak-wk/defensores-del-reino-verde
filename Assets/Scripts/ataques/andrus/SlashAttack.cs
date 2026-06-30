using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class SlashAttack : MonoBehaviour
{
    // Dentro de la clase SlashAttack
// ...

[Header("Animación Visual")]
public bool animateScaleAndFade = true;
public float initialScaleMultiplier = 0.5f; // Empieza al 50%
public float finalScaleMultiplier = 1.2f;   // Termina al 120%
public float initialAlpha = 1.0f;           // Empieza totalmente visible
public float finalAlpha = 0.0f;             // Termina totalmente transparente

private float spawnTime; // Almacena el tiempo exacto en que se creó

// ...
    [Header("Tiempo de vida")]
    public float lifetime = 0.35f;        // ajustable (0.35 = +0.2s sobre 0.15)
    public bool useUnscaledTime = false;  // si usas pausas con timeScale, pon true

    [Header("Daño")]
    public float damage = 50f;
    public LayerMask enemyMask;
    public bool destroyOnFirstHit = false;

    [Header("Seguimiento (en tiempo real)")]
    public Transform target;              // Player
    public bool followTarget = true;
    public float forwardDistance = 1.2f;  // GAP real frente al player
    public float verticalOffset = 0f;

    [Header("Dirección (EXPLÍCITA)")]
    public bool faceRight = true;         // <- El PlayerAutoAttack lo setea y listo
    public bool trackFacing = false;      // <- ¡dejamos esto en false!

    [Header("Hitbox cuadrada")]
    public bool autoSquareHitbox = true;
    public float squareMultiplier = 1.0f; // tam. base del cuadrado vs sprite
    public float padding = 0.15f;         // borde extra

    private BoxCollider2D box;
    private SpriteRenderer sr;
    private readonly HashSet<Collider2D> hitSet = new();
    private float expireAt;               // reloj absoluto
    private float initialZ;

    void Awake()
    {
        box = GetComponent<BoxCollider2D>();
        box.isTrigger = true;

        sr = GetComponentInChildren<SpriteRenderer>();
    }

    void OnEnable()
    {
        hitSet.Clear();

        // fija rotación SOLO en Z según faceRight
        initialZ = faceRight ? 0f : 180f;
        transform.rotation = Quaternion.Euler(0f, 0f, initialZ);

        // reloj absoluto (no acumulamos delta por si hay timeScale)
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;
        spawnTime = now;
        expireAt = now + Mathf.Max(0.01f, lifetime);

        RebuildSquareHitbox();
        ForceFollowNow();

        if (sr && animateScaleAndFade) 
        {
            // Opcional: Asegurar que el color inicial es correcto
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, initialAlpha); 
        }
    }

    void Update()
    {
        // seguir al objetivo (lado fijo: faceRight)
        if (followTarget && target)
        {
            float sign = faceRight ? 1f : -1f;

            float halfSquareWorld = box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);
            Vector3 pos =
                target.position +
                new Vector3(sign * (halfSquareWorld + forwardDistance), verticalOffset, 0f);

            transform.position = pos;

            // Mantener X/Y = 0 (nada de rotaciones raras)
            transform.rotation = Quaternion.Euler(0f, 0f, initialZ);
        }

        // lifetime absoluto (respeta useUnscaledTime)
        float now = useUnscaledTime ? Time.unscaledTime : Time.time;

        if (sr && animateScaleAndFade)
        {
            float totalDuration = lifetime; // Usamos el lifetime como duración total
            float elapsedTime = now - spawnTime;
            float t = Mathf.Clamp01(elapsedTime / totalDuration); // t va de 0 a 1

            // 1. ESCALA: Pasa de 'initialScaleMultiplier' a 'finalScaleMultiplier'
            float currentScale = Mathf.Lerp(initialScaleMultiplier, finalScaleMultiplier, t);
            transform.localScale = Vector3.one * currentScale;

            // 2. DESVANECIMIENTO (ALPHA): Pasa de 'initialAlpha' (visible) a 'finalAlpha' (transparente)
            float currentAlpha = Mathf.Lerp(initialAlpha, finalAlpha, t);
            
            Color currentColor = sr.color;
            currentColor.a = currentAlpha;
            sr.color = currentColor;
        }
        // ----------------------------------

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
        box.offset = Vector2.zero; // el desplazamiento hacia delante lo hace el seguimiento
    }

    void ForceFollowNow()
    {
        if (!target) return;
        float sign = faceRight ? 1f : -1f;
        float halfSquareWorld = box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);
        transform.position =
            target.position +
            new Vector3(sign * (halfSquareWorld + forwardDistance), verticalOffset, 0f);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // filtra por layer (Enemy)
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
