using System.Collections;
using System.Reflection;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class BossGorgonController : MonoBehaviour
{
    [Header("Referencias")]
    public Transform player;
    public EnemyChase enemyChase;
    public Animator animator;

    // [NUEVO] Referencia al Rigidbody para control de físicas
    Rigidbody2D bossRb;
    bool rbSimulatedWasTrue;

    [Header("Condiciones / loop")]
    public float attackInterval = 2f;
    [Tooltip("Distancia máxima para que el boss intente atacar (si está fuera no ataca).")]
    public float attackDistance = 8f;
    [Tooltip("Tiempo de espera obligatorio después de cada ataque.")]
    public float attackCooldown = 2f; // <--- NUEVO COOLDOWN

    // ... (Probabilidades, Prefabs - Sin cambios) ...

    [Header("Prefabs de ataque (ASIGNAR PREFABS, NO SCRIPTS)")]
    public GameObject tailAttackPrefab;
    public GameObject screamRingPrefab;
    public GameObject coneSpecialPrefab;

    [Header("Escala de Ataques (MODIFICAR AQUÍ)")] // <--- NUEVO
    public Vector3 tailAttackScale = new Vector3(1.5f, 1f, 1f);
    public Vector3 screamRingScale = new Vector3(2f, 2f, 1f);
    public Vector3 coneSpecialScale = new Vector3(0.5f, 0.5f, 1f);

    [Header("Animaciones (nombres)")]
    public string animAttack1 = "ataque1";
    public string animAttack2 = "ataque2";
    public string animSpecial = "Special";
    public string animIdleOrRunning = "running"; // <--- NUEVO: Animación de reposo/movimiento
    // ... después de la sección de ataqueCooldown

    [Header("Probabilidades (suman 1.0)")] // <--- ESTO ES LO QUE FALTA
    [Range(0f, 1f)] public float probAttack1 = 0.35f;
    [Range(0f, 1f)] public float probAttack2 = 0.35f;
    [Range(0f, 1f)] public float probSpecial = 0.30f;
    // ...
    bool isCasting = false;

    void Awake()
    {
        if (!player) player = GameObject.FindGameObjectWithTag("Player")?.transform;
        if (!enemyChase) enemyChase = GetComponent<EnemyChase>();
        if (animator == null) animator = GetComponentInChildren<Animator>();
        bossRb = GetComponent<Rigidbody2D>(); // <--- NUEVO
    }

    void Start()
    {
        StartCoroutine(MainLoop());
    }

    IEnumerator MainLoop()
    {
        while (true)
        {
            // [CAMBIO] Espera inicial + cooldown. Ahora esperamos el cooldown aquí.
            yield return new WaitForSeconds(attackInterval + attackCooldown);

            if (isCasting) continue;
            if (player == null) continue;

            float dist = Vector2.Distance(transform.position, player.position);
            if (dist > attackDistance) continue;

            // ... (Decide ataque según probabilidades - Sin cambios) ...

            float roll = Random.value;
            GameObject chosenPrefab = null;
            string animToPlay = null;
            Vector3 scaleToUse = Vector3.one; // <--- NUEVO

            if (roll < probAttack1)
            {
                chosenPrefab = tailAttackPrefab;
                animToPlay = animAttack1;
                scaleToUse = tailAttackScale; // <--- ASIGNAR ESCALA
            }
            else if (roll < probAttack1 + probAttack2)
            {
                chosenPrefab = screamRingPrefab;
                animToPlay = animAttack2;
                scaleToUse = screamRingScale; // <--- ASIGNAR ESCALA
            }
            else
            {
                chosenPrefab = coneSpecialPrefab;
                animToPlay = animSpecial;
                scaleToUse = coneSpecialScale; // <--- ASIGNAR ESCALA
            }

            if (chosenPrefab == null)
            {
                Debug.LogWarning("[BossGorgon] Prefab de ataque no asignado para la elección.");
                continue;
            }
            // Dentro de BossGorgonController.cs, dentro de MainLoop:
            // ...

            // Instancia el prefab del "controller del ataque"
            // Se instancia en la posición del boss (luego el script del ataque lo mueve)
            // Dentro de BossGorgonController.cs, método MainLoop, antes de Instantiate(chosenPrefab, ...)

            Vector3 offset = Vector3.zero;

            if (chosenPrefab == screamRingPrefab || chosenPrefab == coneSpecialPrefab)
            {
                // 1. Determina la dirección de "atrás"
                // Si la escala X es positiva, el jefe mira a la derecha, "atrás" es -1.
                // Si la escala X es negativa (flip), el jefe mira a la izquierda, "atrás" es +1.
                float backDirection = transform.localScale.x > 0 ? -1f : 1f;

                // 2. Aplica el offset en esa dirección (0.5f es la distancia de separación)
                offset = new Vector3(0.5f * backDirection, 0f, 0f);
            }

            // Instancia el prefab del ataque, aplicando el offset
            GameObject go = Instantiate(chosenPrefab, transform.position + offset, Quaternion.identity);

            // ... (El resto del código de Reflection sigue igual) ...
            // ... (Búsqueda de MonoBehaviour y MethodInfo - Sin cambios) ...

            var comp = go.GetComponent<MonoBehaviour>();
            // ... (Lógica para encontrar el método Execute) ...

            MethodInfo mi = null;
            // ... (Se asume que la lógica de Reflection encontró el método mi) ...

            // ------------------------------------------------------------------------------------
            // [NUEVO] BUSCAMOS MÉTODO Execute con la nueva firma: 
            // IEnumerator Execute(Transform origin, Transform player, Vector3 scale)
            // ------------------------------------------------------------------------------------

            // Re-chequeo simplificado de la Reflection para asegurar que pase la escala
            foreach (var m in comp.GetType().GetMethods(BindingFlags.Public | BindingFlags.Instance))
            {
                if (m.Name == "Execute")
                {
                    var pars = m.GetParameters();
                    if (pars.Length == 3 && pars[0].ParameterType == typeof(Transform) && pars[1].ParameterType == typeof(Transform) && pars[2].ParameterType == typeof(Vector3))
                    {
                        mi = m;
                        break;
                    }
                }
            }

            if (mi == null)
            {
                Debug.LogWarning($"[BossGorgon] El prefab no implementa public IEnumerator Execute(Transform origin, Transform player, Vector3 scale).");
                Destroy(go);
                continue;
            }

            // Disable movement / set animator / mark casting
            isCasting = true;
            if (enemyChase) enemyChase.enabled = false;

            // [NUEVO] Bloquear físicas para que el jefe no sea empujado
            if (bossRb != null)
            {
                rbSimulatedWasTrue = bossRb.simulated;
                bossRb.simulated = false;
                bossRb.linearVelocity = Vector2.zero;
            }

            if (animator != null && !string.IsNullOrEmpty(animToPlay))
            {
                animator.Play(animToPlay);
            }

            // Llamar al método Execute y esperar a que termine, pasando la ESCALA
            IEnumerator exec = (IEnumerator)mi.Invoke(comp, new object[] { transform, player, scaleToUse }); // <--- PASAR ESCALA
            if (exec != null)
            {
                yield return StartCoroutine(exec);
            }
            else
            {
                Debug.LogWarning("[BossGorgon] Execute devolvió null IEnumerator inesperadamente.");
            }

            // Restaurar movement / animator / isCasting
            if (enemyChase) enemyChase.enabled = true;

            // [NUEVO] Restaurar físicas
            if (bossRb != null)
            {
                bossRb.simulated = rbSimulatedWasTrue;
                bossRb.linearVelocity = Vector2.zero; // Limpiamos la velocidad al reactivar
            }

            // [NUEVO] Volver a la animación de running
            if (animator != null && !string.IsNullOrEmpty(animIdleOrRunning))
            {
                animator.Play(animIdleOrRunning);
            }

            isCasting = false;
        }
    }
}