using UnityEngine;
using System;
using System.Collections;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    public float maxHP = 50f;
    public float moveSpeed = 1.5f;
    public int xpReward = 5;
    public float contactDamage = 5f;

    float hp;
    public Action<Enemy> OnDeath;

    // NUEVO: si true, al morir NO se invoca OnDeath ni drops
    [HideInInspector]
    public bool suppressDrops = false;

    [HideInInspector] public EnemyData data;
    [HideInInspector] public SpawnZone originZone;

    // Contabilidad de oleadas a prueba de fallos.
    // Evita el doble decremento si el enemigo muere por OnDeath (que ya descuenta)
    // y además se desactiva/destruye por el pool, suppressDrops o limpieza.
    private bool zoneRemovalNotified;

    // --- Trapped / Suspensión de IA (Vórtice de Captura) ---
    public bool IsTrapped { get; private set; }
    private Rigidbody2D trappedRb;

    // --- Slow / Freeze State ---
    private SpriteRenderer spriteRenderer;
    private Color originalColor;
    private Coroutine slowCoroutine;
    private bool isSlowed = false;

    protected virtual void OnEnable()
    {
        hp = maxHP;
        if (spriteRenderer == null)
        {
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                originalColor = spriteRenderer.color;
        }
        ClearSlow();
        // Limpia cualquier estado "trapped" residual (reutilización del pool).
        ClearTrappedState();
    }

    private void OnDisable()
    {
        // Contabilidad a prueba de fallos: si el enemigo se desactiva o destruye
        // SIN pasar por OnDeath (suppressDrops, EnemyPool.Return, limpieza de zona),
        // notificamos igualmente a la zona de origen para que descuente el contador.
        // Sin esto, las Paredes Espectrales quedaban activas (barrera invisible)
        // porque activeEnemyCount nunca llegaba a 0.
        NotifyZoneRemoval();
    }

    /// <summary>
    /// Notifica a la zona de origen que este enemigo ya no está en la oleada.
    /// Idempotente: solo descuenta UNA vez por ciclo de vida (OnDeath o OnDisable,
    /// el que ocurra primero). Se rehabilita con ResetForPool() al reutilizarse.
    /// </summary>
    public void NotifyZoneRemoval()
    {
        if (zoneRemovalNotified) return;
        zoneRemovalNotified = true;

        if (originZone != null)
        {
            originZone.RegisterEnemyRemoved();
        }
    }

    public void Init(EnemyData data)
    {
        this.data = data;
        Init(data.maxHP, data.moveSpeed, data.xpReward, data.contactDamage);
    }

    public void Init(float hp, float speed, int xp, float dmg)
    {
        maxHP = hp; moveSpeed = speed; xpReward = xp; contactDamage = dmg;
        this.hp = maxHP;
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        Debug.Log($"[Enemy] {name} HP = {hp} (-{amount})");
        if (hp <= 0f) Die();
    }

    public void ApplySlow(float multiplier, float duration, Color tintColor)
    {
        if (slowCoroutine != null)
            StopCoroutine(slowCoroutine);

        slowCoroutine = StartCoroutine(SlowCoroutine(multiplier, duration, tintColor));
    }

    private IEnumerator SlowCoroutine(float multiplier, float duration, Color tintColor)
    {
        isSlowed = true;

        if (spriteRenderer != null)
            spriteRenderer.color = tintColor;

        var chase = GetComponent<EnemyChase>();
        if (chase != null)
            chase.SetSpeedMultiplier(multiplier);

        yield return new WaitForSeconds(duration);

        ClearSlow();
    }

    private void ClearSlow()
    {
        isSlowed = false;
        slowCoroutine = null;

        if (spriteRenderer != null)
            spriteRenderer.color = originalColor;

        var chase = GetComponent<EnemyChase>();
        if (chase != null)
            chase.SetSpeedMultiplier(1f);
    }

    /// <summary>
    /// Suspende o restaura la lógica de movimiento/IA del enemigo.
    /// Lo usa el Vórtice de Captura: mientras está "trapped", el Rigidbody2D se
    /// pone Kinematic con velocidad cero (evita conflictos de físicas y que la
    /// IA siga empujando), y se restaura a Dynamic al liberarlo.
    /// </summary>
    public void SetTrapped(bool trapped)
    {
        if (IsTrapped == trapped) return;
        IsTrapped = trapped;

        if (trappedRb == null)
            trappedRb = GetComponent<Rigidbody2D>();

        if (trappedRb == null) return;

        trappedRb.linearVelocity = Vector2.zero;
        trappedRb.bodyType = trapped ? RigidbodyType2D.Kinematic : RigidbodyType2D.Dynamic;
    }

    // Restaura el estado "trapped" (usado al reactivar/reutilizar desde el pool,
    // por si el enemigo fue destruido/desactivado mientras estaba capturado).
    private void ClearTrappedState()
    {
        IsTrapped = false;
        if (trappedRb != null)
        {
            trappedRb.bodyType = RigidbodyType2D.Dynamic;
            trappedRb.linearVelocity = Vector2.zero;
        }
    }

    protected virtual void Die()
    {
        if (suppressDrops)
        {
            // Destruye sin invocar OnDeath (evitamos drops / handlers)
            ReturnToPoolOrDestroy();
            return;
        }

        OnDeath?.Invoke(this);
        ReturnToPoolOrDestroy();
    }

    // NUEVO: función segura para destruir sin invocar OnDeath (usa desde EnemySpawner)
    public void SuppressDropsAndDestroy()
    {
        suppressDrops = true;
        ReturnToPoolOrDestroy();
    }

    private void ReturnToPoolOrDestroy()
    {
        if (EnemyPool.Instance != null)
        {
            EnemyPool.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForPool()
    {
        hp = maxHP;
        suppressDrops = false;
        ClearSlow();
        SetTrapped(false);
        // Rehabilita la notificación de la zona para el próximo ciclo de vida.
        zoneRemovalNotified = false;
    }
}
