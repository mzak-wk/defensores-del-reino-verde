using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Transform))]
public class BossHowlController : MonoBehaviour
{
    [Header("Referencias")]
    [Tooltip("Script que controla el movimiento. Si está vacío, se busca automáticamente.")]
    public MonoBehaviour movementComponent;
    public Transform player;

    [Header("Howl (ajustables)")]
    public float castTime = 0.8f;
    public float duration = 4.0f;
    public float radius = 4.0f;
    public float damage = 20f;
    public float tickInterval = 0.5f;

    [Header("Prefabs/Visual")]
    public GameObject telegraphPrefab;
    public GameObject attackPrefab;

    [Header("Animación / Sprite durante casteo")]
    public Animator animator; // <--- Tu variable está perfecta
    public string animatorCastingBool = "isCasting";
    public Sprite castingSprite;
    SpriteRenderer spriteRenderer;
    Sprite originalSprite;

    [Header("Layers & Proyectiles")]
    public LayerMask playerLayerMask;
    public string projectileTag = "Projectile";
    public bool destroyProjectiles = true;

    [Header("Trigger por proximidad")]
    public bool autoTriggerByDistance = false;
    public float triggerDistance = 6f;
    public float cooldown = 8f;

    // estado interno
    bool running = false;
    float lastTriggeredTime = -999f;
    bool hadMovementEnabled = false;
    bool rbSimulatedWasTrue = true;
    Rigidbody2D bossRb;

    // ---------------------------------------------------------
    // NUEVO: OnValidate ayuda a asignar cosas en el editor automáticamente
    // cuando cambias valores o compilas el script.
    // ---------------------------------------------------------
    void OnValidate()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
    }

    void Awake()
    {
        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (movementComponent == null)
        {
            var chase = GetComponent<EnemyChase>();
            if (chase != null) movementComponent = chase;
        }

        // <--- MEJORA: Búsqueda automática de seguridad en Runtime --->
        if (animator == null)
        {
            // Intentamos buscarlo en el mismo objeto
            animator = GetComponent<Animator>();

            // Si no está, lo buscamos en los hijos (común en bosses 2D donde el visual es hijo del logic)
            if (animator == null)
                animator = GetComponentInChildren<Animator>();
        }
        // <----------------------------------------------------------->

        spriteRenderer = GetComponentInChildren<SpriteRenderer>();
        if (spriteRenderer) originalSprite = spriteRenderer.sprite;

        bossRb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // ... (El resto de tu código Update se mantiene igual) ...
        if (autoTriggerByDistance && !running && Time.time - lastTriggeredTime >= cooldown)
        {
            if (player != null)
            {
                float dist = Vector2.Distance(transform.position, player.position);
                if (dist <= triggerDistance)
                {
                    StartHowl();
                }
            }
        }
    }

    // API pública
    public void StartHowl()
    {
        if (!running)
        {
            lastTriggeredTime = Time.time;
            StartCoroutine(HowlRoutine());
        }
    }

    IEnumerator HowlRoutine()
    {
        running = true;

        // 1. CONGELAR MOVIMIENTO
        hadMovementEnabled = (movementComponent != null) ? movementComponent.enabled : false;
        if (movementComponent != null) movementComponent.enabled = false;

        if (bossRb != null)
        {
            rbSimulatedWasTrue = bossRb.simulated;
            bossRb.linearVelocity = Vector2.zero;
            bossRb.angularVelocity = 0f;
            bossRb.simulated = false;
        }

        // 2. INICIAR ANIMACIÓN (El Boss empieza a "gritar")
        if (animator != null && animator.runtimeAnimatorController != null)
        {
            animator.SetBool(animatorCastingBool, true);
            // Forzamos que empiece desde el inicio por si acaso
            animator.Play("rojataque", 0, 0f);
        }
        else if (castingSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = castingSprite;
        }

        // 3. TELEGRAPH (Círculo de aviso)
        GameObject t = null;
        if (telegraphPrefab != null)
        {
            t = Instantiate(telegraphPrefab, transform.position, Quaternion.identity);
            var tr = t.GetComponent<TelegraphResizer>();
            if (tr != null) tr.SetRadius(radius);
            else t.transform.localScale = new Vector3(radius * 2f, radius * 2f, 1f);
            t.transform.position = transform.position;
        }

        // Esperamos el tiempo de carga (El boss sigue animado)
        float elapsed = 0f;
        while (elapsed < castTime)
        {
            elapsed += Time.deltaTime;
            if (t != null) t.transform.position = transform.position;
            yield return null;
        }

        if (t != null) Destroy(t);

        // (AQUÍ ANTES APAGÁBAMOS LA ANIMACIÓN, LO HEMOS QUITADO DE AQUÍ)

        // 4. CREAR EL ÁREA DE DAÑO
        GameObject areaGO = null;
        if (attackPrefab != null)
        {
            areaGO = Instantiate(attackPrefab, transform.position, Quaternion.identity);
        }
        else
        {
            areaGO = new GameObject("HowlArea");
            var sr = areaGO.AddComponent<SpriteRenderer>();
        }

        if (areaGO != null) areaGO.transform.position = transform.position;

        // Configurar físicas y daño del área...
        if (areaGO != null)
        {
            CircleCollider2D cc = areaGO.GetComponent<CircleCollider2D>();
            if (cc == null) cc = areaGO.AddComponent<CircleCollider2D>();
            cc.isTrigger = true;
            cc.radius = Mathf.Max(0.01f, radius);

            var howlArea = areaGO.GetComponent<HowlArea>();
            if (howlArea == null) howlArea = areaGO.AddComponent<HowlArea>();
            howlArea.duration = duration;
            howlArea.tickInterval = tickInterval;
            howlArea.damage = damage;
            howlArea.playerLayerMask = playerLayerMask;
            howlArea.destroyProjectiles = destroyProjectiles;
            howlArea.projectileTag = projectileTag;
        }

        // 5. ESPERAR DURACIÓN DEL DAÑO (¡AQUÍ ES EL CAMBIO!)
        // Durante todo este tiempo, la animación SE MANTIENE en ataque 
        // porque no hemos apagado el bool todavía.
        float elapsed2 = 0f;
        while (elapsed2 < duration)
        {
            if (areaGO != null) areaGO.transform.position = transform.position;
            elapsed2 += Time.deltaTime;
            yield return null;
        }

        if (areaGO != null) Destroy(areaGO);

        // ----------------------------------------------------------
        // 6. AHORA SÍ: APAGAR ANIMACIÓN Y RESTAURAR
        // ----------------------------------------------------------
        // El ataque terminó, los círculos se fueron, ahora el boss se relaja.
        if (animator != null)
        {
            animator.SetBool(animatorCastingBool, false);
        }
        else if (castingSprite != null && spriteRenderer != null)
        {
            spriteRenderer.sprite = originalSprite;
        }

        // Restaurar movimiento
        if (bossRb != null)
        {
            bossRb.simulated = rbSimulatedWasTrue;
            bossRb.linearVelocity = Vector2.zero;
        }

        if (movementComponent != null)
            movementComponent.enabled = hadMovementEnabled;

        running = false;
    }
}