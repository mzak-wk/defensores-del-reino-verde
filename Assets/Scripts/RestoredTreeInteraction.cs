using UnityEngine;

/// <summary>
/// Componente de interacción para árboles y objetos restaurados tras resolver
/// mecánicas de daño ecológico (erosión de suelo).
/// Solo permite interactuar una vez derrotados todos los enemigos de la zona.
/// Permite reproducir 3 diálogos diferentes de concientización climática de forma secuencial/cíclica.
/// </summary>
[DisallowMultipleComponent]
public class RestoredTreeInteraction : MonoBehaviour
{
    [Header("Detección de Jugador")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private float interactionRadius = 2.5f;

    [Header("Indicador Visual [E]")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Diálogos Concientizadores de Cambio Climático (3)")]
    [SerializeField] private DialogueData[] climateDialogues;

    [Header("Detección de Zona y Enemigos")]
    [Tooltip("SpawnZone asociada a esta área (opcional; si está vacía se busca automáticamente por proximidad).")]
    [SerializeField] private SpawnZone boundSpawnZone;
    [Tooltip("Radio de seguridad para verificar enemigos vivos si no hay SpawnZone directa.")]
    [SerializeField] private float enemyCheckRadius = 30f;

    private int currentDialogueIndex = 0;
    private bool playerInRange = false;
    private Transform playerTransform;
    private IPlayerMovementLock currentMovementLock;

    private void Awake()
    {
        EnsureDialoguesLoaded();
        EnsurePromptUI();
        FindBoundSpawnZone();
    }

    private void Start()
    {
        EnsureDialoguesLoaded();
        FindBoundSpawnZone();
    }

    private void EnsurePromptUI()
    {
        if (promptUI == null)
        {
            promptUI = GetComponent<WorldInteractionPrompt>() ?? GetComponentInChildren<WorldInteractionPrompt>();
            if (promptUI == null)
            {
                promptUI = gameObject.AddComponent<WorldInteractionPrompt>();
            }
        }

        if (promptUI != null)
        {
            promptUI.SetActionText("Aprender");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetPromptVisible(false);
        }
    }

    private void EnsureDialoguesLoaded()
    {
        if (climateDialogues != null && climateDialogues.Length >= 3 && climateDialogues[0] != null)
            return;

        climateDialogues = new DialogueData[3];
        climateDialogues[0] = Resources.Load<DialogueData>("Dialogues/tree_climate_dialogue_1");
        climateDialogues[1] = Resources.Load<DialogueData>("Dialogues/tree_climate_dialogue_2");
        climateDialogues[2] = Resources.Load<DialogueData>("Dialogues/tree_climate_dialogue_3");
    }

    private void FindBoundSpawnZone()
    {
        if (boundSpawnZone != null) return;

        var allZones = Object.FindObjectsByType<SpawnZone>(FindObjectsSortMode.None);
        float bestDist = float.MaxValue;
        foreach (var z in allZones)
        {
            if (z == null) continue;
            float dist = Vector2.Distance(transform.position, z.transform.position);
            if (dist < bestDist && dist < 35f)
            {
                bestDist = dist;
                boundSpawnZone = z;
            }
        }
    }

    private void Update()
    {
        CheckPlayerProximity();

        bool enemiesCleared = AreEnemiesClearedInZone();
        bool isDialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        bool canInteract = playerInRange && enemiesCleared && !isDialogueOpen;

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(canInteract);
        }

        if (!canInteract) return;

        if (Input.GetKeyDown(interactionKey))
        {
            TriggerDialogue();
        }
    }

    public bool AreEnemiesClearedInZone()
    {
        // 1. Si hay una SpawnZone vinculada, consultar si ya fue completada (oleadas terminadas)
        if (boundSpawnZone != null)
        {
            if (!boundSpawnZone.IsCleared)
            {
                return false;
            }
        }

        // 2. Verificar si quedan enemigos activos en el radio del área
        var enemies = Object.FindObjectsByType<Enemy>(FindObjectsSortMode.None);
        foreach (var e in enemies)
        {
            if (e != null && e.gameObject.activeInHierarchy && e.IsAlive)
            {
                float dist = Vector2.Distance(transform.position, e.transform.position);
                if (dist <= enemyCheckRadius)
                {
                    return false;
                }
            }
        }

        return true;
    }

    private void TriggerDialogue()
    {
        EnsureDialoguesLoaded();

        if (climateDialogues == null || climateDialogues.Length == 0) return;

        DialogueData targetDialogue = climateDialogues[currentDialogueIndex % climateDialogues.Length];
        if (targetDialogue == null) return;

        if (DialogueManager.Instance == null)
        {
            Debug.LogWarning("[RestoredTreeInteraction] No se encontró DialogueManager en la escena.");
            return;
        }

        if (promptUI != null) promptUI.SetPromptVisible(false);

        // Avanzar el índice al siguiente diálogo para la próxima vez que el jugador interactúe
        currentDialogueIndex = (currentDialogueIndex + 1) % climateDialogues.Length;

        DialogueManager.Instance.StartDialogue(targetDialogue, currentMovementLock);
    }

    private void CheckPlayerProximity()
    {
        bool detected = false;
        Transform detectedPlayer = null;
        IPlayerMovementLock detectedLock = null;

        if (playerTransform != null && Vector2.Distance(transform.position, playerTransform.position) <= interactionRadius)
        {
            detected = true;
            detectedPlayer = playerTransform;
            detectedLock = currentMovementLock;
        }
        else
        {
            var pGO = GameObject.FindWithTag(playerTag);
            if (pGO != null && Vector2.Distance(transform.position, pGO.transform.position) <= interactionRadius)
            {
                detected = true;
                detectedPlayer = pGO.transform;
                detectedLock = pGO.GetComponent<IPlayerMovementLock>();
            }
            else
            {
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
                foreach (var hit in hits)
                {
                    if (hit == null || hit.gameObject == gameObject) continue;
                    if (hit.CompareTag(playerTag) || hit.GetComponentInParent<PlayerMovement>() != null)
                    {
                        detected = true;
                        detectedPlayer = hit.transform;
                        detectedLock = hit.GetComponentInParent<IPlayerMovementLock>();
                        break;
                    }
                }
            }
        }

        if (detected)
        {
            playerInRange = true;
            playerTransform = detectedPlayer;
            currentMovementLock = detectedLock;
        }
        else
        {
            playerInRange = false;
            playerTransform = null;
            currentMovementLock = null;
        }
    }

    private void OnDisable()
    {
        playerInRange = false;
        playerTransform = null;
        currentMovementLock = null;
        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
        }
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRadius);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, enemyCheckRadius);
    }
}
