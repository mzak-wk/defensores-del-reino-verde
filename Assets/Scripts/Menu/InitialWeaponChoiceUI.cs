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
    private PlayerMovement player;

    private void Start()
    {
        // Ocultar el panel al arrancar por si acaso
        gameObject.SetActive(false);

        // Registrar los listeners de los botones
        if (buttonA != null) buttonA.onClick.AddListener(() => ChooseWeapon(weaponA));
        if (buttonB != null) buttonB.onClick.AddListener(() => ChooseWeapon(weaponB));
    }

    /// <summary>
    /// Abre el panel y configura las opciones basadas en la base de datos.
    /// </summary>
    public void OpenPanel()
    {
        gameObject.SetActive(true);

        // Buscar al jugador para desbloquearlo al finalizar
        player = FindFirstObjectByType<PlayerMovement>();

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

        // Asignar los datos del Index 0 y Index 1
        weaponA = weapons[0];
        weaponB = weapons[1];

        // Configurar UI del Botón A
        if (weaponA != null)
        {
            if (iconA != null) iconA.sprite = weaponA.Icon;
            if (nameA != null) nameA.text = weaponA.DisplayName;
        }

        // Configurar UI del Botón B
        if (weaponB != null)
        {
            if (iconB != null) iconB.sprite = weaponB.Icon;
            if (nameB != null) nameB.text = weaponB.DisplayName;
        }
    }

    /// <summary>
    /// Procesa la elección del arma y finaliza la introducción.
    /// </summary>
    private void ChooseWeapon(WeaponData chosenWeapon)
    {
        if (chosenWeapon == null) return;

        if (GameSession.Instance != null)
        {
            // 1. Añadir el WeaponId a las armas desbloqueadas
            if (!GameSession.Instance.unlockedWeapons.Contains(chosenWeapon.WeaponId))
            {
                GameSession.Instance.unlockedWeapons.Add(chosenWeapon.WeaponId);
            }

            // 2. Equipar el arma en el slot 0 del jugador
            WeaponSlotManager slotManager = FindFirstObjectByType<WeaponSlotManager>();
            if (slotManager != null)
            {
                slotManager.EquipWeaponAtSlot(0, chosenWeapon);
            }
            else
            {
                Debug.LogWarning("[InitialWeaponChoiceUI] No se encontró WeaponSlotManager en el jugador.");
            }

            // 3. Marcar la intro como completada
            GameSession.Instance.hasCompletedIntro = true;
        }

        // 4. Desbloquear el movimiento del jugador
        if (player != null)
        {
            player.SetMovementLocked(false);
        }
        else
        {
            // Fallback en caso de que PlayerMovement no esté o use IPlayerMovementLock
            var lockObj = FindFirstObjectByType<MonoBehaviour>() as IPlayerMovementLock;
            lockObj?.SetMovementLocked(false);
        }

        // 5. Guardar la partida inmediatamente (con protección contra nulos)
        try
        {
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.SaveCurrentGame();
            }
            else
            {
                Debug.LogWarning("[InitialWeaponChoiceUI] SaveManager no disponible en la escena actual. La partida continuará sin autoguardado.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[InitialWeaponChoiceUI] Error al intentar guardar partida: {ex.Message}");
        }

        // 6. Ocultar el panel de forma limpia
        gameObject.SetActive(false);
    }
}
