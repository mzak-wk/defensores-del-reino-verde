using UnityEngine;

/// <summary>
/// HubIntroSequenceManager — Gestor independiente para controlar el flujo de bienvenida
/// en la escena del Hub en función del estado del Save Slot.
/// </summary>
public class HubIntroSequenceManager : MonoBehaviour
{
    [Header("Referencias de Diálogo (Opcional)")]
    [Tooltip("Los datos de diálogo de la presentación inicial (si aplica).")]
    [SerializeField] private DialogueData introDialogueData;

    [Header("Referencias de UI")]
    [Tooltip("Panel de elección de arma inicial.")]
    [SerializeField] private InitialWeaponChoiceUI weaponChoiceUI;

    private PlayerMovement player;

    private void Start()
    {
        // 1. Determinar si la partida actual es nueva / slot vacío
        bool isNewGame = true;

        if (GameSession.Instance != null)
        {
            isNewGame = !GameSession.Instance.hasCompletedIntro;
        }

        // 2. Si el slot ya no es nuevo (partida cargada existente), no hacer nada
        if (!isNewGame)
        {
            Debug.Log("[HubIntroSequenceManager] Save Slot contiene partida existente. Saltando secuencia de bienvenida.");
            Destroy(gameObject);
            return;
        }

        // 3. Buscar al jugador en la escena para pausar su movimiento
        player = FindFirstObjectByType<PlayerMovement>();
        if (player != null)
        {
            player.SetMovementLocked(true);
        }

        // 4. Si hay diálogo introductivo configurado, iniciarlo
        if (DialogueManager.Instance != null && introDialogueData != null)
        {
            DialogueManager.Instance.OnDialogueClosed += OnIntroDialogueEnded;
            DialogueManager.Instance.StartDialogue(introDialogueData, player);
        }
        else
        {
            // Si no hay diálogo introductivo, abrir directamente el panel de armas inicial
            ShowWeaponSelectionPanel();
        }
    }

    private void OnDestroy()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed -= OnIntroDialogueEnded;
        }
    }

    private void OnIntroDialogueEnded()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.OnDialogueClosed -= OnIntroDialogueEnded;
        }

        ShowWeaponSelectionPanel();
    }

    private void ShowWeaponSelectionPanel()
    {
        if (weaponChoiceUI != null)
        {
            if (weaponChoiceUI.gameObject.activeSelf) return;

            if (player != null)
            {
                player.SetMovementLocked(true);
            }
            weaponChoiceUI.OpenPanel();
        }
        else
        {
            Debug.LogWarning("[HubIntroSequenceManager] No se asignó InitialWeaponChoiceUI. Desbloqueando jugador.");
            if (player != null)
            {
                player.SetMovementLocked(false);
            }
        }
    }
}
