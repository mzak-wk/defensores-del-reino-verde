using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla la UI y el comportamiento de un botón de ranura de guardado.
/// Delega la lógica de carga/creación al SaveManager.
/// </summary>
public class SaveSlotUI : MonoBehaviour
{
    [Header("Configuración del Slot")]
    [Tooltip("El ID de este slot (ej. 1, 2, 3)")]
    public int slotId = 1;

    [Header("Referencias de UI")]
    [Tooltip("El texto donde se mostrará el estado ('Vacío' o info de la partida)")]
    public TextMeshProUGUI statusText;
    
    [Tooltip("El botón principal de este slot")]
    public Button slotButton;

    private bool hasSaveData = false;

    private void OnEnable()
    {
        // Actualizamos la UI cada vez que se muestra el panel
        RefreshUI();
    }

    private void Start()
    {
        if (slotButton != null)
        {
            slotButton.onClick.AddListener(OnSlotClicked);
        }
    }

    /// <summary>
    /// Actualiza el texto del botón consultando al SaveManager si existe partida.
    /// </summary>
    public void RefreshUI()
    {
        if (SaveManager.Instance == null)
        {
            Debug.LogWarning("[SaveSlotUI] SaveManager no está instanciado aún.");
            return;
        }

        hasSaveData = SaveManager.Instance.HasSave(slotId);

        if (statusText != null)
        {
            if (hasSaveData)
            {
                SaveData data = SaveManager.Instance.GetSaveData(slotId);
                if (data != null)
                {
                    statusText.text = $"Nivel {data.playerLevel} - Oro {data.gold}";
                }
                else
                {
                    statusText.text = "Error al leer datos";
                }
            }
            else
            {
                statusText.text = "Vacío";
            }
        }
    }

    /// <summary>
    /// Evento disparado al hacer clic en este slot.
    /// </summary>
    private void OnSlotClicked()
    {
        if (SaveManager.Instance == null) return;

        if (hasSaveData)
        {
            // Cargar la partida existente
            SaveManager.Instance.LoadGame(slotId);
        }
        else
        {
            // Crear partida nueva
            SaveManager.Instance.StartNewGame(slotId);
        }
    }
}
