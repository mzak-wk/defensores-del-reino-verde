using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Proyector de Reforestación - Arma ID "5".
/// Ataque canalizado por botón sostenido en forma de cono continuo:
///   - Configura un PolygonCollider2D (Trigger) que escala con scaledStats.areaSize.
///   - Sigue al cañón real del jugador cada frame (gira y reposiciona) consultando
///     PlayerCombat.GetFacingDirection()/GetSpawnPosition() (mismo patrón que el Vórtice).
///   - Mantiene una lista simple targetsInBeam actualizada con OnTriggerEnter/Exit.
///   - Daño progresivo GLOBAL por ticks (no por enemigo): cada tickInterval se suma
///     globalTicks y se aplica dmg = stats.damage + (damageStep * globalTicks) a todos
///     los enemigos dentro del cono. Un enemigo que entra tarde recibe el daño
///     acumulado del rayo de forma inmediata (sin diccionarios).
///   - Release(bool overheated): apaga el collider, limpia la lista y ejecuta el
///     trigger "Overheat" o "Collapse" del Animator según el motivo del cierre.
/// Los Jefes (BossHealth) reciben daño igual que el resto: BossHealth oculta
/// TakeDamage con 'new' (no es virtual), por lo que se requiere el cast explícito.
/// </summary>
[RequireComponent(typeof(PolygonCollider2D))]
public class ReforestationProjector : MonoBehaviour
{
    [Header("Referencias (auto-asignadas o en Inspector)")]
    [SerializeField] private PolygonCollider2D coneCollider;
    [SerializeField] private Animator animator;
    [SerializeField] private SpriteRenderer visual;

    [Header("Geometría del cono (unidades de mundo con areaSize = 1)")]
    [Tooltip("Alcance frontal del cono. Se multiplica por scaledStats.areaSize.")]
    [SerializeField] private float coneLength = 4f;

    [Header("Daño por ticks (progresivo global)")]
    [Tooltip("Intervalo en segundos entre cada tick de daño.")]
    [SerializeField, Min(0.05f)] private float tickInterval = 0.5f;
    [Tooltip("Daño extra que se suma por cada tick acumulado del rayo.")]
    [SerializeField, Min(0f)] private float damageStep = 5f;
    [Tooltip("Tope máximo de ticks acumulados (0 = sin tope).")]
    [SerializeField, Min(0)] private int maxTicks = 20;

    [Header("Cierre")]
    [Tooltip("Duración del cierre antes de destruir el GameObject.")]
    [SerializeField] private float collapseDuration = 0.15f;
    [SerializeField] private string collapseTriggerName = "Collapse";
    [SerializeField] private string overheatTriggerName = "Overheat";

    // --- Estado en runtime ---
    private WeaponData weapon;
    private BaseStats stats;
    private Transform firePoint;
    private PlayerCombat combat;      // fuente de facing/posición del cañón

    private float tickTimer;
    private int globalTicks;          // contador GLOBAL del ataque (no por enemigo)
    private bool closing;
    private Vector3 baseLocalScale;   // escala del prefab (se multiplica por areaSize)

    // Enemigos actualmente dentro del cono (sin diccionarios).
    private readonly List<Enemy> targetsInBeam = new List<Enemy>();

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
    /// Escala el cono (visual + collider) con scaledStats.areaSize y reinicia el estado.
    /// </summary>
    public void Init(WeaponData weapon, BaseStats stats, Transform firePoint, PlayerCombat combat)
    {
        this.weapon = weapon;
        this.stats = stats;
        this.firePoint = firePoint;
        this.combat = combat;

        // El PolygonCollider2D escala con el transform: multiplicar la escala base
        // del prefab por areaSize estira visual y física a la vez.
        float s = Mathf.Max(0.1f, stats.areaSize);
        transform.localScale = Vector3.Scale(baseLocalScale, Vector3.one * s);

        // Estado limpio en cada spawn.
        globalTicks = 0;
        tickTimer = 0f;
        closing = false;
        targetsInBeam.Clear();

        if (coneCollider != null) coneCollider.enabled = true;

        // Primer frame de orientación: pega el proyector al cañón.
        UpdateFacing();
    }

    private void Update()
    {
        if (closing) return;

        // Re-anclar al cañón real en cada frame (posición + rotación global).
        UpdateFacing();

        // Fase 3 - Reloj de ticks: tickInterval marca el ritmo de daño.
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            tickTimer = 0f;
            ApplyTick();
        }
    }

    /// <summary>
    /// Orientación anclada al cañón. El firePoint es estático (siempre a la derecha)
    /// y el jugador se gira visualmente por el Flip del Sprite, por lo que NO se puede
    /// depender de coordenadas locales del padre. Cada frame se consulta a PlayerCombat:
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

    // --- Fase 2 - Gestión de objetivos dentro del cono ---

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (closing) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null || !enemy.gameObject.activeInHierarchy) return;

        // Regla anti-duplicados.
        if (!targetsInBeam.Contains(enemy))
            targetsInBeam.Add(enemy);
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (closing) return;

        Enemy enemy = other.GetComponentInParent<Enemy>();
        if (enemy == null) return;

        targetsInBeam.Remove(enemy);
    }

    // --- Fase 3 - Daño progresivo global por ticks ---

    private void ApplyTick()
    {
        if (stats.damage <= 0f) return;

        // Acumula el contador GLOBAL del ataque (con tope opcional).
        globalTicks = (maxTicks > 0) ? Mathf.Min(globalTicks + 1, maxTicks) : globalTicks + 1;

        // El daño escala para el ATAQUE, no por enemigo: cualquier objetivo dentro
        // del cono recibe el valor acumulado completo (sin prorratear por tiempo).
        float dmg = stats.damage + (damageStep * globalTicks);

        // Iteración inversa: purga en vivo a los muertos/desactivados (pool).
        for (int i = targetsInBeam.Count - 1; i >= 0; i--)
        {
            Enemy e = targetsInBeam[i];
            if (!IsAlive(e))
            {
                targetsInBeam.RemoveAt(i);
                continue;
            }

            // CRÍTICO: BossHealth oculta TakeDamage con 'new' (no es virtual).
            // Llamar e.TakeDamage() con una referencia Enemy invocaría la versión base
            // y rompería eventos de jefe (OnDamaged, OnBossDeath, drops).
            if (e is BossHealth boss)
                boss.TakeDamage(dmg);
            else
                e.TakeDamage(dmg);
        }
    }

    // --- Fase 4 - Cierre (normal o por sobrecalentamiento) ---

    /// <summary>
    /// Modo cierre (llamado por PlayerCombat al soltar el botón o sobrecalentar):
    /// apaga el collider, limpia la lista de objetivos y ejecuta el trigger del
    /// Animator correspondiente antes de destruirse de forma segura.
    /// </summary>
    public void Release(bool overheated)
    {
        if (closing) return;
        closing = true;

        // 1) Apagar el collider: ya no pueden entrar enemigos.
        if (coneCollider != null) coneCollider.enabled = false;

        // 2) Limpiar la lista de objetivos.
        targetsInBeam.Clear();

        // 3) Feedback visual de cierre: "Overheat" si se sobrecalentó, "Collapse" si no.
        string trigger = overheated ? overheatTriggerName : collapseTriggerName;
        if (animator != null && !string.IsNullOrEmpty(trigger))
            animator.SetTrigger(trigger);
        else if (visual != null)
            StartCoroutine(CollapseVisual()); // fallback sin Animator

        // 4) Destrucción segura.
        Destroy(gameObject, collapseDuration);
    }

    /// <summary>
    /// Red de seguridad: si el proyector se destruye sin pasar por Release()
    /// (muerte del jugador, cierre de escena...), solo limpia la lista.
    /// No atrapa enemigos, por lo que no hay IA que liberar.
    /// </summary>
    private void OnDestroy()
    {
        if (closing) return;
        closing = true;
        targetsInBeam.Clear();
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

    private static bool IsAlive(Enemy e)
    {
        return e != null && e.gameObject.activeInHierarchy;
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (combat == null) return;

        float s = Application.isPlaying ? Mathf.Max(0.1f, stats.areaSize) : 1f;
        float reach = coneLength * s;
        // Ancla visual alineada con el runtime: transform.position (cañón real).
        Vector3 anchor = Application.isPlaying ? transform.position : firePoint.position;

        Gizmos.color = new Color(0.2f, 1f, 0.4f, 0.4f);
        Gizmos.DrawWireSphere(anchor, reach * 0.25f);

        Gizmos.color = new Color(0.4f, 1f, 0.2f, 0.5f);
        Gizmos.DrawLine(anchor, anchor + Vector3.right * reach);
        Gizmos.DrawLine(anchor, anchor + Vector3.left * reach);
    }
#endif
}
