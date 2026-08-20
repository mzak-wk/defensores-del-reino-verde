using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Vórtice de Captura - Arma ID "3".
/// Ataque continuo tipo "aspiradora" direccional (cono):
///   - Captura enemigos que entran al cono (PolygonCollider2D en modo Trigger).
///   - Los atrae hacia el firePoint y los ancla en la "zona segura".
///   - Les aplica daño exponencial por ticks usando WeaponData.BasicCooldown.
///   - Al soltar el botón entra en modo cierre y se destruye tras un colapso.
/// Los Jefes (BossHealth / BossAIController) son inmunes a la atracción y a la
/// suspensión de IA: solo se añaden a damageTargets para recibir daño.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class CaptureVortex : MonoBehaviour
{
    [Header("Referencias (auto-asignadas o en Inspector)")]
    [SerializeField] private PolygonCollider2D coneCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visual;

    [Header("Geometría del cono (unidades de mundo con areaSize = 1)")]
    [Tooltip("Alcance frontal del cono. Su borde exterior equivale al daño mínimo (1x).")]
    [SerializeField] private float coneLength = 4f;

    [Header("Atracción")]
    [Tooltip("Velocidad (u/s) con la que los enemigos son arrastrados hacia el firePoint.")]
    [SerializeField] private float pullSpeed = 6f;

    [Header("Daño por ticks")]
    [Tooltip("Multiplicador máximo de daño en el centro del cono (en el borde = 1x).")]
    [SerializeField] private float maxDamageMultiplier = 8f;

    [Header("Colapso")]
    [Tooltip("Duración del cierre antes de destruir el GameObject.")]
    [SerializeField] private float collapseDuration = 0.15f;
    [SerializeField] private string collapseTriggerName = "Collapse";

    // --- Estado en runtime ---
    private WeaponData weapon;
    private BaseStats stats;
    private Transform firePoint;
    private PlayerCombat combat;   // fuente de facing/posición del cañón (Bug 2)

    private float safeDistance;        // radio de la zona segura (cupo interior)
    private float coneReach;           // alcance frontal del cono en el mundo
    private int maxOuterCapacity;      // cupo exterior (cono)
    private int maxInnerCapacity;      // cupo interior (zona segura)
    private int innerOccupied;         // enemigos anclados en la zona segura
    private float tickTimer;
    private bool closing;
    private Vector3 baseLocalScale;    // escala del prefab (se multiplica por areaSize)

    // trappedEnemies : capturados en tránsito (aún siendo arrastrados).
    // anchoredEnemies : anclados milimétricamente en el borde de la zona segura.
    // damageTargets   : jefes en rango del cono (solo reciben daño, no se atraen).
    private readonly List<Enemy> trappedEnemies = new List<Enemy>();
    private readonly List<Enemy> anchoredEnemies = new List<Enemy>();
    private readonly List<Enemy> damageTargets = new List<Enemy>();

    private void Awake()
    {
        if (coneCollider == null) coneCollider = GetComponent<PolygonCollider2D>();
        if (animator == null) animator = GetComponent<Animator>();
        if (visual == null) visual = GetComponentInChildren<SpriteRenderer>();

        if (coneCollider != null)
            coneCollider.isTrigger = true;

        baseLocalScale = transform.localScale;
    }

    /// <summary>
    /// Fase 1 - Inicialización (llamado por PlayerCombat en el frame 0).
    /// Escala el cono con areaSize y calcula las capacidades.
    /// </summary>
    public void Init(WeaponData weapon, BaseStats stats, Transform firePoint, PlayerCombat combat)
    {
        this.weapon = weapon;
        this.stats = stats;
        this.firePoint = firePoint;
        this.combat = combat;

        // BUG 2 (corregido): el firePoint es estático (siempre a la derecha) y el
        // jugador se gira visualmente por el Flip del Sprite. Ya no se resetea
        // localPosition/localRotation: la orientación se ancla al cañón real cada
        // frame en UpdateFacing() vía GetFacingDirection()/GetSpawnPosition().

        // Escala visual + física del cono (el PolygonCollider2D escala con el transform).
        float s = Mathf.Max(0.1f, stats.areaSize);
        transform.localScale = Vector3.Scale(baseLocalScale, Vector3.one * s);
        coneReach = coneLength * s;

        // Zona segura: borde pegado al cañón (25% del alcance).
        safeDistance = coneReach * 0.25f;

        // Capacidades máximas según el diseño aprobado.
        maxOuterCapacity = Mathf.FloorToInt(2f + (s / 2f));
        maxInnerCapacity = Mathf.FloorToInt(1f + (s / 5f));

        // Primer tick casi inmediato.
        tickTimer = weapon != null ? weapon.BasicCooldown : 0f;

        trappedEnemies.Clear();
        anchoredEnemies.Clear();
        damageTargets.Clear();
        innerOccupied = 0;
        closing = false;

        if (coneCollider != null) coneCollider.enabled = true;

        // Primer frame de orientación: pega el vórtice al cañón.
        UpdateFacing();
    }

    private void Update()
    {
        if (closing) return;

        // Re-anclar al cañón real en cada frame (posición + rotación global).
        UpdateFacing();

        if (weapon == null || weapon.BasicCooldown <= 0f) return;

        // Fase 4 - Sistema de ticks: BasicCooldown marca el ritmo de daño.
        tickTimer += Time.deltaTime;
        if (tickTimer >= weapon.BasicCooldown)
        {
            tickTimer = 0f;
            ApplyTick();
        }
    }

    /// <summary>
    /// Orientación anclada al cañón (Bug 2 corregido). El firePoint es estático
    /// (siempre a la derecha) y el jugador se gira visualmente por el Flip del
    /// Sprite, por lo que NO se puede depender de coordenadas locales del padre.
    /// Cada frame se consulta a PlayerCombat:
    ///   - Rotación global: Euler(0,180,0) al mirar a la izquierda, identidad a la derecha.
    ///   - Posición mundial: GetSpawnPosition() refleja el offset del cañón según `dir`.
    /// </summary>
    private void UpdateFacing()
    {
        if (combat == null || firePoint == null) return;

        Vector2 dir = combat.GetFacingDirection();

        transform.rotation = (dir.x < 0f)
            ? Quaternion.Euler(0f, 180f, 0f)
            : Quaternion.identity;

        transform.position = combat.GetSpawnPosition(dir);
    }

    private void FixedUpdate()
    {
        if (closing || firePoint == null) return;

        // Ancla en la posición real del cañón (ya sincronizada por UpdateFacing),
        // NO en firePoint.position: este es estático y quedaría en la espalda del
        // jugador al mirar a la izquierda.
        Vector2 anchor = transform.position;

        // --- Fase 3 - Atracción de enemigos en tránsito hacia la zona segura. ---
        for (int i = trappedEnemies.Count - 1; i >= 0; i--)
        {
            Enemy e = trappedEnemies[i];
            if (!IsAlive(e)) { trappedEnemies.RemoveAt(i); continue; }

            Vector2 toAnchor = anchor - (Vector2)e.transform.position;
            float dist = toAnchor.magnitude;

            if (dist > safeDistance)
            {
                // Arrastre suave sin sobrepasar el borde de la zona segura.
                float step = Mathf.Min(pullSpeed * Time.fixedDeltaTime, dist - safeDistance);
                MoveEnemy(e, (Vector2)e.transform.position + toAnchor.normalized * step);
            }
            else if (innerOccupied < maxInnerCapacity)
            {
                // Hay cupo interior: anclar milimétricamente en el borde.
                MoveEnemy(e, anchor + toAnchor.normalized * safeDistance);
                anchoredEnemies.Add(e);
                trappedEnemies.RemoveAt(i);
                innerOccupied++;
            }
            else
            {
                // Cupo interior lleno: bloqueado en el borde exterior de la zona segura.
                MoveEnemy(e, anchor + toAnchor.normalized * safeDistance);
            }
        }

        // --- Anclados: re-sincronizar con el firePoint si el jugador camina. ---
        PruneAnchored();
        for (int i = 0; i < anchoredEnemies.Count; i++)
        {
            Enemy e = anchoredEnemies[i];
            Vector2 toAnchor = anchor - (Vector2)e.transform.position;
            float dist = toAnchor.magnitude;

            Vector2 target = (dist > 0.001f)
                ? anchor + toAnchor.normalized * safeDistance
                : anchor + Vector2.right * safeDistance; // caso degenerado
            MoveEnemy(e, target);
        }
    }

    // --- Captura (Fase 2) ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (closing) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return;

        // Los jefes no se atraen ni se inmovilizan: solo reciben daño.
        if (IsBoss(enemy))
        {
            if (!damageTargets.Contains(enemy))
                damageTargets.Add(enemy);
            return;
        }

        // Regla anti-duplicados (una captura por enemigo).
        if (enemy.IsTrapped) return;

        // Cupo exterior (cono).
        if (trappedEnemies.Count + anchoredEnemies.Count >= maxOuterCapacity) return;

        // Suspensión de IA: Kinematic + velocidad 0 (Enemy.SetTrapped).
        enemy.SetTrapped(true);
        trappedEnemies.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (closing) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        // Los jefes salen del cono: se retiran de los objetivos de daño.
        if (damageTargets.Remove(enemy))
            return;

        // Enemigos que escapan del cono: liberar IA y cupo.
        if (trappedEnemies.Remove(enemy))
        {
            enemy.SetTrapped(false);
            return;
        }

        if (anchoredEnemies.Remove(enemy))
        {
            innerOccupied = Mathf.Max(0, innerOccupied - 1);
            enemy.SetTrapped(false);
        }
    }

    // --- Fase 4 - Daño exponencial por ticks ---

    private void ApplyTick()
    {
        if (firePoint == null || stats.damage <= 0f) return;

        // Mismo ancla que FixedUpdate: la posición real del cañón.
        Vector2 anchor = transform.position;

        // Snapshots: el daño se aplica simultáneamente a todos los objetivos
        // (evita mutar las listas durante la iteración).
        Enemy[] trappedSnapshot = trappedEnemies.ToArray();
        Enemy[] anchoredSnapshot = anchoredEnemies.ToArray();
        Enemy[] bossSnapshot = damageTargets.ToArray();

        for (int i = 0; i < trappedSnapshot.Length; i++)
            ApplyDamageTo(trappedSnapshot[i], anchor);

        for (int i = 0; i < anchoredSnapshot.Length; i++)
            ApplyDamageTo(anchoredSnapshot[i], anchor);

        for (int i = 0; i < bossSnapshot.Length; i++)
            ApplyDamageTo(bossSnapshot[i], anchor);

        // Liberar cupos de los que murieron en este tick.
        PurgeDead();
    }

    private void ApplyDamageTo(Enemy e, Vector2 anchor)
    {
        if (!IsAlive(e)) return;

        // t = 1 en el centro, 0 en el borde del cono.
        float d = Vector2.Distance(anchor, e.transform.position);
        float t = 1f - Mathf.Clamp01(d / coneReach);

        // Daño exponencial: 1x en el borde, maxDamageMultiplier (8x) en el centro.
        float multiplier = Mathf.Pow(maxDamageMultiplier, t);
        float dmg = stats.damage * multiplier;

        // CRÍTICO: BossHealth oculta TakeDamage con 'new' (no es virtual).
        // Llamar e.TakeDamage() con una referencia Enemy invocaría la versión base
        // y rompería eventos de jefe (OnDamaged, OnBossDeath, drops).
        if (e is BossHealth boss)
            boss.TakeDamage(dmg);
        else
            e.TakeDamage(dmg);
    }

    /// <summary>
    /// Purga enemigos muertos/desactivados (pool) y libera el cupo ocupado.
    /// </summary>
    private void PurgeDead()
    {
        for (int i = trappedEnemies.Count - 1; i >= 0; i--)
            if (!IsAlive(trappedEnemies[i])) trappedEnemies.RemoveAt(i);

        PruneAnchored();

        for (int i = damageTargets.Count - 1; i >= 0; i--)
            if (!IsAlive(damageTargets[i])) damageTargets.RemoveAt(i);
    }

    private void PruneAnchored()
    {
        for (int i = anchoredEnemies.Count - 1; i >= 0; i--)
        {
            if (!IsAlive(anchoredEnemies[i]))
            {
                anchoredEnemies.RemoveAt(i);
                innerOccupied = Mathf.Max(0, innerOccupied - 1);
            }
        }
    }

    // --- Fase 5 - Soltar y destruir ---

    /// <summary>
    /// Modo cierre (llamado por PlayerCombat al soltar el botón):
    /// devuelve la IA, apaga el collider, reproduce el colapso y destruye.
    /// </summary>
    public void Release()
    {
        if (closing) return;
        closing = true;

        // 1) Devolver la IA a los supervivientes (seteando false en SetTrapped).
        foreach (Enemy e in trappedEnemies)
            if (IsAlive(e)) e.SetTrapped(false);
        foreach (Enemy e in anchoredEnemies)
            if (IsAlive(e)) e.SetTrapped(false);

        trappedEnemies.Clear();
        anchoredEnemies.Clear();
        damageTargets.Clear();
        innerOccupied = 0;

        // 2) Apagar el collider: ya no pueden entrar enemigos.
        if (coneCollider != null) coneCollider.enabled = false;

        // 3) Animación ultracorta de colapso (~0.15s).
        if (animator != null && !string.IsNullOrEmpty(collapseTriggerName))
            animator.SetTrigger(collapseTriggerName);
        else if (visual != null)
            StartCoroutine(CollapseVisual()); // fallback sin Animator

        // 4) Destrucción segura.
        Destroy(gameObject, collapseDuration);
    }

    /// <summary>
    /// Red de seguridad: si el vórtice se destruye sin pasar por Release()
    /// (muerte del jugador, cierre de escena...), libera la IA de los atrapados.
    /// </summary>
    private void OnDestroy()
    {
        if (closing) return;

        closing = true;
        foreach (Enemy e in trappedEnemies)
            if (IsAlive(e)) e.SetTrapped(false);
        foreach (Enemy e in anchoredEnemies)
            if (IsAlive(e)) e.SetTrapped(false);
    }

    private IEnumerator CollapseVisual()
    {
        Vector3 startScale = transform.localScale;
        float t = 0f;
        while (t < collapseDuration)
        {
            t += Time.deltaTime;
            transform.localScale = Vector3.Lerp(startScale, Vector3.zero, t / collapseDuration);
            yield return null;
        }
    }

    // --- Helpers ---

    /// <summary>
    /// Un "Jefe" es inmune a la atracción: BossHealth (jefes reales) o cualquier
    /// enemigo con BossAIController. Los mini-bosses SÍ se capturan.
    /// </summary>
    private static bool IsBoss(Enemy e)
    {
        return e is BossHealth || e.GetComponent<BossAIController>() != null;
    }

    private static bool IsAlive(Enemy e)
    {
        return e != null && e.gameObject.activeInHierarchy;
    }

    /// <summary>
    /// Mueve al enemigo vía rb.MovePosition (Kinematic, sin conflictos de físicas).
    /// </summary>
    private static void MoveEnemy(Enemy e, Vector2 targetPos)
    {
        if (e.TryGetComponent(out Rigidbody2D rb))
            rb.MovePosition(targetPos);
        else
            e.transform.position = targetPos;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (firePoint == null) return;

        float s = Application.isPlaying ? Mathf.Max(0.1f, stats.areaSize) : 1f;
        float reach = coneLength * s;
        // Ancla visual alineada con el runtime: transform.position (cañón real).
        Vector3 anchor = Application.isPlaying ? transform.position : firePoint.position;

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.4f);
        Gizmos.DrawWireSphere(anchor, reach * 0.25f);

        Gizmos.color = new Color(0f, 1f, 1f, 0.5f);
        Gizmos.DrawLine(anchor, anchor + Vector3.right * reach);
        Gizmos.DrawLine(anchor, anchor + Vector3.left * reach);
    }
#endif
}
