using UnityEngine;

public interface IPlayerMovementLock
{
    void SetMovementLocked(bool locked);
}

public class HubComputerController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private HubUIManager hubUIManager;

    [Header("Interaction")]
    [SerializeField] private string playerTag = "Player";
    [SerializeField] private KeyCode interactionKey = KeyCode.E;
    [SerializeField] private bool closeOnExit = false;

    [Header("Indicador Visual [E]")]
    [SerializeField] private WorldInteractionPrompt promptUI;

    [Header("Physics Interaction Detection")]
    [SerializeField] private bool useRadiusDetection = true;
    [SerializeField] private float interactionRadius = 2.5f;

    private bool playerInRange;
    private IPlayerMovementLock currentMovementLock;

    private void Awake()
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
            promptUI.SetActionText("Computadora");
            promptUI.SetAlwaysShowCountdown(false);
            promptUI.SetPromptVisible(false);
        }
    }

    private void OnEnable()
    {
        if (hubUIManager != null)
        {
            hubUIManager.OnComputerClosed += HandleComputerClosed;
        }
    }

    private void OnDisable()
    {
        if (hubUIManager != null)
        {
            hubUIManager.OnComputerClosed -= HandleComputerClosed;
        }

        SetPlayerMovementLocked(false);

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(false);
        }
    }

    private void Update()
    {
        CheckProximityDetection();

        bool isDialogueOpen = DialogueManager.Instance != null && DialogueManager.Instance.IsOpen;
        bool isComputerOpen = hubUIManager != null && hubUIManager.IsOpen;
        bool canInteract = playerInRange && !isDialogueOpen && !isComputerOpen;

        if (promptUI != null)
        {
            promptUI.SetPromptVisible(canInteract);
        }

        if (!canInteract || hubUIManager == null)
        {
            return;
        }

        if (Input.GetKeyDown(interactionKey))
        {
            if (promptUI != null) promptUI.SetPromptVisible(false);
            OpenComputer();
        }
    }

    private Transform cachedPlayerTransform;

    private void CheckProximityDetection()
    {
        if (!useRadiusDetection) return;

        bool detected = false;
        IPlayerMovementLock detectedLock = null;

        // 1. Verificación directa por distancia al jugador (inmune a proyectiles, armas o ataques)
        if (cachedPlayerTransform != null && Vector2.Distance(transform.position, cachedPlayerTransform.position) <= interactionRadius)
        {
            detected = true;
            detectedLock = currentMovementLock;
        }
        else
        {
            GameObject pGO = GameObject.FindWithTag(playerTag);
            if (pGO != null && Vector2.Distance(transform.position, pGO.transform.position) <= interactionRadius)
            {
                detected = true;
                cachedPlayerTransform = pGO.transform;
                detectedLock = pGO.GetComponent<IPlayerMovementLock>() ?? pGO.GetComponentInChildren<IPlayerMovementLock>();
            }
            else
            {
                // 2. Comprobar todos los colisionadores en el radio (no solo el primero)
                Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, interactionRadius);
                foreach (var h in hits)
                {
                    if (h == null || h.gameObject == gameObject) continue;
                    if (h.CompareTag(playerTag) || (h.transform.root != null && h.transform.root.CompareTag(playerTag)))
                    {
                        detected = true;
                        cachedPlayerTransform = h.transform.root;
                        detectedLock = h.GetComponentInParent<IPlayerMovementLock>();
                        break;
                    }
                }
            }
        }

        if (detected)
        {
            if (!playerInRange)
            {
                playerInRange = true;
                currentMovementLock = detectedLock;
            }
        }
        else
        {
            if (playerInRange)
            {
                playerInRange = false;
                if (closeOnExit && hubUIManager != null && hubUIManager.IsOpen)
                {
                    hubUIManager.CloseComputer();
                }
                SetPlayerMovementLocked(false);
                currentMovementLock = null;
                cachedPlayerTransform = null;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other == null) return;
        if (other.CompareTag(playerTag) || (other.transform.root != null && other.transform.root.CompareTag(playerTag)))
        {
            playerInRange = true;
            cachedPlayerTransform = other.transform.root;
            currentMovementLock = other.GetComponentInParent<IPlayerMovementLock>();
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other == null) return;

        // Si el jugador aún se encuentra dentro del radio físico, ignorar salidas de colliders hijos de ataque
        if (cachedPlayerTransform != null && Vector2.Distance(transform.position, cachedPlayerTransform.position) <= interactionRadius)
        {
            return;
        }

        GameObject pGO = GameObject.FindWithTag(playerTag);
        if (pGO != null && Vector2.Distance(transform.position, pGO.transform.position) <= interactionRadius)
        {
            return;
        }

        if (other.CompareTag(playerTag) || (other.transform.root != null && other.transform.root.CompareTag(playerTag)))
        {
            playerInRange = false;

            if (closeOnExit && hubUIManager != null && hubUIManager.IsOpen)
            {
                hubUIManager.CloseComputer();
            }

            SetPlayerMovementLocked(false);
            currentMovementLock = null;
            cachedPlayerTransform = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (useRadiusDetection)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, interactionRadius);
        }
    }

    private void OpenComputer()
    {
        if (hubUIManager.IsOpen)
        {
            return;
        }

        hubUIManager.OpenComputer();
        SetPlayerMovementLocked(true);
    }

    private void HandleComputerClosed()
    {
        SetPlayerMovementLocked(false);
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (currentMovementLock != null)
        {
            currentMovementLock.SetMovementLocked(locked);
        }
    }
}
