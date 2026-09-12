using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Controlador de ataque cinemático / ola de energía (estilo Mugetsu / Final Getsuga Tenshou de Bleach).
/// Permite que la animación de la ola se reproduzca completa de principio a fin, viajando hacia adelante
/// o expandiéndose, haciendo daño continuo o por pulsos a todos los enemigos que atraviese,
/// y autodestruyéndose exactamente al finalizar la animación o por Animation Event.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class WaveUltimateAttack : MonoBehaviour
{
    [Header("Configuración de Daño")]
    [SerializeField] private float damage = 150f;
    [Tooltip("Si es true, daña a cada enemigo una sola vez. Si es false, daña por pulsos continuos.")]
    [SerializeField] private bool hitOncePerEnemy = true;
    [Tooltip("Intervalo entre pulsos de daño si hitOncePerEnemy es false.")]
    [SerializeField] private float damageTickInterval = 0.2f;

    [Header("Movimiento / Proyección")]
    [Tooltip("Velocidad de avance de la ola hacia adelante. Pon 0 si la ola es estática en el suelo.")]
    [SerializeField] private float travelSpeed = 8f;
    [Tooltip("Tiempo máximo de seguridad antes de autodestruirse si la animación no lo hace.")]
    [SerializeField] private float maxLifetime = 2.5f;

    [Header("Posicionamiento")]
    [Tooltip("Desplazamiento relativo al punto de spawn (ej. Y = -0.5 para bajar la ola).")]
    [SerializeField] private Vector3 spawnOffset = Vector3.zero;

    [Header("Capas")]
    [SerializeField] private LayerMask enemyMask;

    private Vector2 moveDirection = Vector2.right;
    private Animator animator;
    private SpriteRenderer sr;
    private readonly Dictionary<Collider2D, float> nextDamageTimePerEnemy = new();
    private readonly HashSet<Collider2D> hitEnemiesOnce = new();
    private bool isInitialized = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        sr = GetComponentInChildren<SpriteRenderer>();

        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            col.isTrigger = true;
        }

        if (enemyMask.value == 0)
        {
            enemyMask = LayerMask.GetMask("Enemy");
        }
    }

    /// <summary>
    /// Inicializa la ola con la dirección del jugador, el daño escalado y la orientación de los sprites.
    /// </summary>
    public void Init(Vector2 direction, float scaledDamage, float customSpeed = -1f)
    {
        damage = scaledDamage;
        moveDirection = direction.sqrMagnitude > 0.01f ? direction.normalized : Vector2.right;

        if (customSpeed >= 0f)
        {
            travelSpeed = customSpeed;
        }

        // Orientar sprite según la dirección
        bool facingLeft = moveDirection.x < 0f;
        if (sr != null)
        {
            sr.flipX = facingLeft;
        }
        else
        {
            transform.rotation = facingLeft ? Quaternion.Euler(0f, 180f, 0f) : Quaternion.identity;
        }

        // Aplicar offset de posicionamiento (respetando la dirección horizontal)
        Vector3 adjustedOffset = spawnOffset;
        if (facingLeft)
        {
            adjustedOffset.x = -adjustedOffset.x;
        }
        transform.position += adjustedOffset;

        isInitialized = true;

        // Auto-destrucción de seguridad basada en maxLifetime o duración del Animator
        float destroyDelay = maxLifetime;
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            AnimatorClipInfo[] clips = animator.GetCurrentAnimatorClipInfo(0);
            if (clips.Length > 0 && clips[0].clip != null)
            {
                destroyDelay = clips[0].clip.length;
            }
        }

        Destroy(gameObject, destroyDelay);
    }

    private void Update()
    {
        if (!isInitialized) return;

        // Avanzar en la dirección de disparo
        if (travelSpeed > 0f)
        {
            transform.Translate((Vector3)(moveDirection * travelSpeed * Time.deltaTime), Space.World);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!hitOncePerEnemy)
        {
            TryDamage(other);
        }
    }

    private void TryDamage(Collider2D other)
    {
        if ((enemyMask.value & (1 << other.gameObject.layer)) == 0) return;

        if (hitOncePerEnemy)
        {
            if (hitEnemiesOnce.Contains(other)) return;
            hitEnemiesOnce.Add(other);
        }
        else
        {
            if (nextDamageTimePerEnemy.TryGetValue(other, out float nextTime) && Time.time < nextTime)
            {
                return;
            }
            nextDamageTimePerEnemy[other] = Time.time + damageTickInterval;
        }

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy != null)
        {
            enemy.TakeDamage(damage);
        }
    }

    /// <summary>
    /// Puede ser llamado desde un Animation Event en el último fotograma de la animación de la ola.
    /// </summary>
    public void OnAnimationFinished()
    {
        Destroy(gameObject);
    }
}
