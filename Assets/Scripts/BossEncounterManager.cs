using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Orquesta el evento cinematográfico de pre-combate del Jefe en la EndZone:
///   - Detecta la entrada del jugador (OnTriggerEnter2D).
///   - Asegura ser el ÚNICO orquestador (neutraliza LevelEntryDirector activos).
///   - Congela movimiento y combate (SetMovementLocked / SetCombatLocked).
///   - El jugador conserva el arma equipada al entrar a la zona.
///   - Mantiene el jefe ACTIVO y visible durante el diálogo cinemático.
///   - Reproduce el diálogo de intro y se suscribe a OnDialogueClosed (con guard:
///     si no hay dialogo válido, salta directo a StartBattle()).
///   - Al cerrar el diálogo arranca StartBattle() y devuelve el control al jugador.
///
/// FIX del "imán" / barrera invisible: al activarse fuerza el cierre de TODAS
/// las Paredes Espectrales de los SpawnZone (la zona anterior [4,0], la propia
/// EndZone y cualquier otra activa), evitando que el paso [4,0] -> [5,0] quede
/// bloqueado por contadores desincronizados de la zona previa.
///
/// Reutilizable: en escenas por nivel (Woods/Beach/Ice) ejecuta el flujo completo.
/// En LevelFinal, si BossRushDirector está presente, delega el orquestado en él
/// y solo aplica la apertura forzada de barreras.
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class BossEncounterManager : MonoBehaviour
{
    [Header("Diálogo")]
    [Tooltip("Diálogo de introducción que se reproduce antes de la batalla.")]
    [SerializeField] private DialogueData bossDialogueData;

    [Header("Jefe")]
    [Tooltip("Instancia del jefe en escena. Se mantiene activa y visible durante el diálogo cinemático. Si es un prefab (asset), se instancia al iniciar la batalla.")]
    [SerializeField] private GameObject bossObject;

    [Tooltip("Opcional: override de stats que se aplicará al iniciar la batalla.")]
    [SerializeField] private BossStatsOverride statsOverride;

    [Header("UI")]
    [Tooltip("Barra de vida del jefe (opcional).")]
    [SerializeField] private BossHealthBarUI bossHealthBar;

    [Header("Eventos")]
    [Tooltip("Se invoca cuando la batalla comienza oficialmente (tras el diálogo).")]
    public UnityEvent OnBattleStarted;

    [Header("Referencias (auto-detectadas si se dejan vacías)")]
    [SerializeField] private PlayerMovement playerMovement;
    [SerializeField] private PlayerCombat playerCombat;
    [SerializeField] private WeaponSlotManager weaponSlotManager;

    private Collider2D triggerCollider;
    private bool hasStarted;

    private void Awake()
    {
        triggerCollider = GetComponent<Collider2D>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasStarted) return;
        if (!other.CompareTag("Player")) return;

        hasStarted = true;

        // Modo B (LevelFinal): BossRushDirector ya orquesta todo el flujo
        // (bloqueo, diálogo, spawn del boss, healthbar). Solo garantizamos que
        // las barreras espectrales queden abiertas para que el paso no se bloquee.
        if (BossRushDirector.Instance != null)
        {
            ForceOpenSpectralWalls();
            return;
        }

        if (triggerCollider != null)
            triggerCollider.enabled = false;

        ForceOpenSpectralWalls();
        StartCoroutine(StartCinematicSequence());
    }

    /// <summary>
    /// Apertura forzada y definitiva de las Paredes Espectrales:
    ///   1. La zona anterior donde estaba el jugador (GetPlayerZone).
    ///   2. La propia EndZone, si tiene un SpawnZone con paredes propias.
    ///   3. Red de seguridad: cualquier SpawnZone activo en la escena.
    /// Idempotente (ForceComplete no re-descuenta); es seguro llamarlo varias veces.
    /// </summary>
    public void ForceOpenSpectralWalls()
    {
        if (ZoneSpawner.Instance != null)
        {
            SpawnZone previousZone = ZoneSpawner.Instance.GetPlayerZone();
            if (previousZone != null)
                previousZone.ForceComplete();
        }

        SpawnZone ownZone = GetComponentInParent<SpawnZone>();
        if (ownZone != null)
            ownZone.ForceComplete();

        foreach (SpawnZone zone in FindObjectsByType<SpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            zone.ForceComplete();
        }
    }

    private IEnumerator StartCinematicSequence()
    {
        ResolveReferences();

        // 0. Asegurar que este manager sea el ÚNICO orquestador de la EndZone:
        // neutralizar cualquier LevelEntryDirector activo antes de bloquear, para
        // que su desbloqueo posterior no deshaga nuestro lock ni abra otro diálogo.
        CancelLevelEntryDirector();

        // Paso 1: bloquear movimiento. El jugador mantiene el arma equipada.
        if (playerMovement != null)
            playerMovement.SetMovementLocked(true);

        // Frame de gracia para asegurar que el Animator procese los locks.
        yield return null;

        // Paso 2: bloquear combate (libera vórtices/proyectores activos).
        if (playerCombat != null)
            playerCombat.SetCombatLocked(true);

        // Desactivar movimiento y IA del jefe para que permanezca inmóvil durante el diálogo
        if (bossObject != null)
        {
            // Desactivar EnemyChase para evitar persecución durante diálogo
            EnemyChase enemyChase = bossObject.GetComponentInChildren<EnemyChase>();
            if (enemyChase != null)
                enemyChase.enabled = false;

            BossAIController bossAI = bossObject.GetComponent<BossAIController>();
            if (bossAI != null)
            {
                bossAI.Deactivate();
                Debug.Log("[BossEncounterManager] IA del jefe desactivada durante diálogo.");
            }
        }

        // El jefe permanece ACTIVO y visible durante el diálogo cinemático.

        // 4. Diálogo de introducción. Guard de validación: si DialogueManager o la
        // DialogueData no tienen contenido válido, StartDialogue aborta
        // internamente y OnDialogueClosed NUNCA se dispara (jugador bloqueado y
        // jefe sin spawn). En ese caso saltamos directo a StartBattle().
        bool canStartDialogue = DialogueManager.Instance != null
            && bossDialogueData != null
            && bossDialogueData.Lines != null
            && bossDialogueData.Lines.Count > 0;

        if (canStartDialogue)
        {
            DialogueManager.Instance.OnDialogueClosed += HandleDialogueEnded;
            DialogueManager.Instance.StartDialogue(bossDialogueData, playerMovement);
        }
        else
        {
            Debug.LogWarning("[BossEncounterManager] No hay datos de diálogo asignados. Iniciando batalla directamente.");
            HandleDialogueEnded();
        }
    }

    /// <summary>
    /// Se ejecuta al cerrar el diálogo: arranca la batalla y devuelve el control
    /// al jugador.
    /// </summary>
    private void HandleDialogueEnded()
    {
        UnsubscribeFromDialogue();

        // Arrancar la batalla (jefe ya está activo y visible).
        StartBattle();

        // Devolver el control (idempotente, seguro aunque DialogueManager ya
        // haya gestionado el desbloqueo de movimiento).
        if (playerCombat != null)
            playerCombat.SetCombatLocked(false);
        if (playerMovement != null)
            playerMovement.SetMovementLocked(false);
    }

    /// <summary>
    /// Asegura que este manager sea el ÚNICO orquestador en la EndZone: si aún
    /// queda un LevelEntryDirector activo (cinemática/diálogo de entrada de
    /// nivel), lo cierra de forma limpia — desuscribe su diálogo, devuelve el
    /// control al jugador y se auto-destruye — antes de que este flujo bloquee.
    /// </summary>
    private void CancelLevelEntryDirector()
    {
        LevelEntryDirector[] directors = FindObjectsByType<LevelEntryDirector>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
        foreach (LevelEntryDirector director in directors)
        {
            if (director == null) continue;
            Debug.Log($"[BossEncounterManager] Neutralizando LevelEntryDirector {director.name}.");
            director.OnEntryDialogueEnded();
        }
    }

    private void StartBattle()
    {
        if (bossObject != null)
        {
            if (!IsSceneInstance(bossObject))
            {
                // Referencia de prefab (asset): instanciar en el punto del trigger.
                bossObject = Instantiate(bossObject, transform.position, Quaternion.identity);
            }
            else
            {
                bossObject.SetActive(true);
            }
        }

        BossAIController bossAI = null;
        if (bossObject != null)
        {
            // Reactivar EnemyChase antes de iniciar batalla
            EnemyChase enemyChase = bossObject.GetComponentInChildren<EnemyChase>();
            if (enemyChase != null)
                enemyChase.enabled = true;

            bossAI = bossObject.GetComponent<BossAIController>();
            if (bossAI == null)
                bossAI = bossObject.GetComponentInChildren<BossAIController>();
        }

        if (bossAI != null)
        {
            if (statsOverride != null)
                bossAI.SetStatsOverride(statsOverride);
            bossAI.StartBattle();
        }
        else
        {
            Debug.LogWarning("[BossEncounterManager] No se encontró BossAIController en el bossObject.", bossObject);
        }

        // Cerrar paredes de combate para encerrar la arena
        SpawnZone spawnZone = FindBossSpawnZone();
        if (spawnZone != null)
        {
            spawnZone.SetCombatBarriers(true);
            Debug.Log("[BossEncounterManager] Paredes de combate activadas.");
        }

        BossHealth bossHealth = bossObject != null ? bossObject.GetComponent<BossHealth>() : null;
        if (bossHealth != null && bossHealthBar != null)
        {
            string bossName = statsOverride != null ? statsOverride.bossName : "Boss";
            bossHealthBar.Show(bossHealth, bossName);
        }

        OnBattleStarted?.Invoke();
    }

    private void ResolveReferences()
    {
        if (playerMovement == null)
            playerMovement = FindFirstObjectByType<PlayerMovement>();

        if (playerCombat == null)
            playerCombat = playerMovement != null
                ? playerMovement.GetComponent<PlayerCombat>()
                : FindFirstObjectByType<PlayerCombat>();

        if (weaponSlotManager == null)
            weaponSlotManager = playerMovement != null
                ? playerMovement.GetComponent<WeaponSlotManager>()
                : FindFirstObjectByType<WeaponSlotManager>();
    }

    private static SpawnZone FindBossSpawnZone()
    {
        foreach (SpawnZone zone in FindObjectsByType<SpawnZone>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (zone.IsBossZone)
                return zone;
        }

        return null;
    }

    private void UnsubscribeFromDialogue()
    {
        if (DialogueManager.Instance != null)
            DialogueManager.Instance.OnDialogueClosed -= HandleDialogueEnded;
    }

    private static bool IsSceneInstance(GameObject go)
    {
        return go.scene.IsValid() && go.scene.isLoaded;
    }

    private void OnDestroy()
    {
        UnsubscribeFromDialogue();
    }
}
