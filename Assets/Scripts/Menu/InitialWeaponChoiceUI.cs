using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// InitialWeaponChoiceUI — Controla el panel de selección de armas inicial.
/// </summary>
public class InitialWeaponChoiceUI : MonoBehaviour
{
    [Header("Base de Datos")]
    [Tooltip("Referencia a la base de datos de armas del juego.")]
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Referencias")]
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] private PlayerMovement playerRef;

    [Header("Referencias de UI - Botón A")]
    [SerializeField] private Button buttonA;
    [SerializeField] private Image iconA;
    [SerializeField] private TMP_Text nameA;

    [Header("Referencias de UI - Botón B")]
    [SerializeField] private Button buttonB;
    [SerializeField] private Image iconB;
    [SerializeField] private TMP_Text nameB;

    private WeaponData weaponA;
    private WeaponData weaponB;

    private void Awake()
    {
        if (buttonA != null) buttonA.onClick.AddListener(() => ChooseWeapon(weaponA));
        if (buttonB != null) buttonB.onClick.AddListener(() => ChooseWeapon(weaponB));
    }

    /// <summary>
    /// Abre el panel y configura las opciones basadas en la base de datos.
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);

        if (weaponDatabase == null)
        {
            Debug.LogError("[InitialWeaponChoiceUI] WeaponDatabase no asignada.");
            return;
        }

        var weapons = weaponDatabase.Weapons;
        if (weapons.Count < 2)
        {
            Debug.LogError("[InitialWeaponChoiceUI] La base de datos debe contener al menos 2 armas.");
            return;
        }

        weaponA = weapons[0];
        weaponB = weapons[1];

        if (weaponA != null)
        {
            if (iconA != null) iconA.sprite = weaponA.Icon;
            if (nameA != null) nameA.text = weaponA.DisplayName;
        }

        if (weaponB != null)
        {
            if (iconB != null) iconB.sprite = weaponB.Icon;
            if (nameB != null) nameB.text = weaponB.DisplayName;
        }
    }

    /// <summary>
    /// Abre el panel de elección de arma inicial.
    /// Puede ser llamado desde una opción de diálogo o desde HubIntroSequenceManager.
    /// </summary>
    public void OpenPanelFromDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.CloseDialogue();
        }
        OpenPanel();
    }

    /// <summary>
    /// Procesa la elección del arma y finaliza la introducción.
    /// </summary>
    private void ChooseWeapon(WeaponData chosenWeapon)
    {
        if (chosenWeapon == null) return;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.UnlockWeapon(chosenWeapon);
            GameSession.Instance.EquipWeapon(chosenWeapon);
        }

        if (weaponSlotManager != null)
        {
            weaponSlotManager.EquipWeapon(chosenWeapon);
        }
        else
        {
            Debug.LogWarning("[InitialWeaponChoiceUI] WeaponSlotManager no asignado.");
        }

        if (GameSession.Instance != null)
        {
            GameSession.Instance.hasCompletedIntro = true;
        }

        if (playerRef != null)
        {
            playerRef.SetMovementLocked(false);
        }

        try
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveCurrentGame();
            }
            else
            {
                Debug.LogWarning("[InitialWeaponChoiceUI] SaveManager no disponible. La partida continuará sin autoguardado.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InitialWeaponChoiceUI] Error al intentar guardar partida: {ex.Message}");
        }

        gameObject.SetActive(false);
    }
}
