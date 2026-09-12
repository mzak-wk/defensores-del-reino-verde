using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controla el HUD de habilidades y ataques en la esquina inferior derecha.
/// Gestiona las 4 ranuras (Básico, Habilidad 1, Habilidad 2, Definitiva), sus iconos dinámicos según el arma,
/// el sombreado grisáceo con relleno radial de cooldown, y la letra/icono de la tecla configurada.
/// Permite ser ocultado desde el menú de opciones.
/// </summary>
public class AbilityHUDController : MonoBehaviour
{
    public static AbilityHUDController Instance { get; private set; }

    [System.Serializable]
    public class AbilitySlotUI
    {
        public CombatAction action;
        [Tooltip("Contenedor raíz del slot.")]
        public GameObject slotRoot;
        [Tooltip("Imagen principal del icono del ataque o habilidad.")]
        public Image iconImage;
        [Tooltip("Imagen oscura con Type: Filled (Radial 360) que representa el cooldown.")]
        public Image cooldownOverlay;
        [Tooltip("Texto numérico opcional del cooldown restante (ej. '4.2s').")]
        public TMP_Text cooldownText;
        [Tooltip("Texto con la tecla asignada (ej. 'CLICK', '1', '2', '3').")]
        public TMP_Text keybindText;
        [Tooltip("CanvasGroup para atenuar o iluminar el slot.")]
        public CanvasGroup canvasGroup;
    }

    [Header("Contenedor Principal")]
    [Tooltip("Objeto raíz de todo el HUD de habilidades (para poder ocultarlo en Opciones).")]
    [SerializeField] private GameObject hudContainer;

    [Header("Ranuras de Habilidad")]
    [SerializeField] private AbilitySlotUI basicSlot;
    [SerializeField] private AbilitySlotUI skill1Slot;
    [SerializeField] private AbilitySlotUI skill2Slot;
    [SerializeField] private AbilitySlotUI ultimateSlot;

    private PlayerCombat playerCombat;
    private WeaponSlotManager weaponSlotManager;
    private WeaponData lastWeapon;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
    }

    private void Start()
    {
        // 1. Cargar preferencia de visibilidad desde PlayerPrefs
        bool isVisible = PlayerPrefs.GetInt("ShowAbilityHUD", 1) == 1;
        SetHUDVisible(isVisible);

        // 2. Conectar a eventos de cambio de teclas
        KeybindManager.OnKeybindChanged += HandleKeybindChanged;

        // 3. Inicializar textos de teclas
        RefreshAllKeybindLabels();

        // 4. Buscar referencias en el jugador
        FindPlayerReferences();

        // 5. Inicializar slots en estado listo
        ResetSlotVisual(basicSlot);
        ResetSlotVisual(skill1Slot);
        ResetSlotVisual(skill2Slot);
        ResetSlotVisual(ultimateSlot);
    }

    private void OnDestroy()
    {
        KeybindManager.OnKeybindChanged -= HandleKeybindChanged;

        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (hudContainer != null && !hudContainer.activeSelf)
            return;

        if (playerCombat == null || weaponSlotManager == null)
        {
            FindPlayerReferences();
            return;
        }

        // Si el arma cambió, refrescar los iconos y nombres
        WeaponData currentWeapon = weaponSlotManager.CurrentWeapon;
        if (currentWeapon != lastWeapon)
        {
            lastWeapon = currentWeapon;
            RefreshWeaponIcons(currentWeapon);
        }

        // Actualizar cooldowns en tiempo real
        UpdateSlotCooldown(basicSlot, playerCombat.GetCooldownRemaining(CombatAction.BasicAttack), playerCombat.GetTotalCooldown(CombatAction.BasicAttack));
        UpdateSlotCooldown(skill1Slot, playerCombat.GetCooldownRemaining(CombatAction.Skill1), playerCombat.GetTotalCooldown(CombatAction.Skill1));
        UpdateSlotCooldown(skill2Slot, playerCombat.GetCooldownRemaining(CombatAction.Skill2), playerCombat.GetTotalCooldown(CombatAction.Skill2));
        UpdateSlotCooldown(ultimateSlot, playerCombat.GetCooldownRemaining(CombatAction.Ultimate), playerCombat.GetTotalCooldown(CombatAction.Ultimate));
    }

    private void FindPlayerReferences()
    {
        if (playerCombat == null)
            playerCombat = FindFirstObjectByType<PlayerCombat>();

        if (weaponSlotManager == null)
            weaponSlotManager = FindFirstObjectByType<WeaponSlotManager>();

        if (weaponSlotManager != null && weaponSlotManager.CurrentWeapon != null)
        {
            lastWeapon = weaponSlotManager.CurrentWeapon;
            RefreshWeaponIcons(lastWeapon);
        }
    }

    /// <summary>
    /// Actualiza los iconos de las habilidades basándose en el arma activa.
    /// </summary>
    public void RefreshWeaponIcons(WeaponData weapon)
    {
        if (weapon == null) return;

        if (basicSlot.iconImage != null)
            basicSlot.iconImage.sprite = weapon.BasicIcon;

        if (skill1Slot.iconImage != null)
            skill1Slot.iconImage.sprite = weapon.Skill1Icon;

        if (skill2Slot.iconImage != null)
            skill2Slot.iconImage.sprite = weapon.Skill2Icon;

        if (ultimateSlot.iconImage != null)
            ultimateSlot.iconImage.sprite = weapon.UltimateIcon;
    }

    private void UpdateSlotCooldown(AbilitySlotUI slot, float remaining, float total)
    {
        if (slot == null || slot.slotRoot == null) return;

        bool onCooldown = remaining > 0f && total > 0f;

        if (onCooldown)
        {
            float fill = Mathf.Clamp01(remaining / total);

            if (slot.cooldownOverlay != null)
            {
                slot.cooldownOverlay.gameObject.SetActive(true);
                slot.cooldownOverlay.fillAmount = fill;
            }

            if (slot.cooldownText != null)
            {
                slot.cooldownText.gameObject.SetActive(true);
                slot.cooldownText.text = remaining >= 1f ? $"{remaining:F0}s" : $"{remaining:F1}s";
            }

            if (slot.canvasGroup != null)
            {
                slot.canvasGroup.alpha = 0.55f; // Atenuado/Grisáceo
            }
        }
        else
        {
            ResetSlotVisual(slot);
        }
    }

    private void ResetSlotVisual(AbilitySlotUI slot)
    {
        if (slot == null) return;

        if (slot.cooldownOverlay != null)
        {
            slot.cooldownOverlay.fillAmount = 0f;
            slot.cooldownOverlay.gameObject.SetActive(false);
        }

        if (slot.cooldownText != null)
        {
            slot.cooldownText.gameObject.SetActive(false);
        }

        if (slot.canvasGroup != null)
        {
            slot.canvasGroup.alpha = 1f; // Completamente brillante y activo
        }
    }

    private void HandleKeybindChanged(CombatAction action, KeyCode newKey)
    {
        RefreshAllKeybindLabels();
    }

    public void RefreshAllKeybindLabels()
    {
        UpdateKeyLabel(basicSlot, CombatAction.BasicAttack);
        UpdateKeyLabel(skill1Slot, CombatAction.Skill1);
        UpdateKeyLabel(skill2Slot, CombatAction.Skill2);
        UpdateKeyLabel(ultimateSlot, CombatAction.Ultimate);
    }

    private void UpdateKeyLabel(AbilitySlotUI slot, CombatAction action)
    {
        if (slot != null && slot.keybindText != null)
        {
            slot.keybindText.text = KeybindManager.Instance.GetDisplayString(action);
        }
    }

    /// <summary>
    /// Muestra u oculta todo el HUD de habilidades (usado por el menú de Opciones).
    /// </summary>
    public void SetHUDVisible(bool visible)
    {
        if (hudContainer != null)
        {
            hudContainer.SetActive(visible);
        }
        PlayerPrefs.SetInt("ShowAbilityHUD", visible ? 1 : 0);
        PlayerPrefs.Save();
    }
}
