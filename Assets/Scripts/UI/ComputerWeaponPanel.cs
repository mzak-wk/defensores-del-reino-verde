using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ComputerWeaponPanel : MonoBehaviour
{
    [Header("Navegación")]
    [SerializeField] private Button btnBackToMainPanel;

    [Header("Referencias - Base de Datos & Manager")]
    [SerializeField] private WeaponDatabase weaponDatabase;
    [SerializeField] private WeaponSlotManager weaponSlotManager;

    [Header("Navegación (Carrusel)")]
    [SerializeField] private Button btnPrevious;
    [SerializeField] private Button btnNext;

    [Header("Visualización")]
    [SerializeField] private Image weaponIconDisplay;
    [SerializeField] private TextMeshProUGUI weaponInfoText;

    [Header("Equipamiento")]
    [SerializeField] private Button equipButton;
    [SerializeField] private TMP_Text currentEquippedLabel;

    private int currentWeaponIndex = 0;
    private List<WeaponData> unlockedWeapons = new List<WeaponData>();

    private void Awake()
    {
        if (btnBackToMainPanel != null)
        {
            btnBackToMainPanel.onClick.AddListener(OnBackToMainClicked);
        }

        if (btnPrevious != null)
        {
            btnPrevious.onClick.AddListener(OnPreviousClicked);
        }

        if (btnNext != null)
        {
            btnNext.onClick.AddListener(OnNextClicked);
        }

        if (equipButton != null)
        {
            equipButton.onClick.AddListener(OnEquipButtonClicked);
        }
    }

    private void OnEnable()
    {
        Refresh();
    }

    public void Refresh()
    {
        if (weaponSlotManager == null)
        {
            weaponSlotManager = FindFirstObjectByType<WeaponSlotManager>();
        }

        BuildUnlockedWeaponsList();

        if (unlockedWeapons.Count == 0)
        {
            if (weaponInfoText != null) weaponInfoText.text = "No hay armas disponibles.";
            if (weaponIconDisplay != null) weaponIconDisplay.sprite = null;
            return;
        }

        // Si el índice actual excede el rango tras actualizar, resetear a 0
        if (currentWeaponIndex >= unlockedWeapons.Count)
        {
            currentWeaponIndex = 0;
        }

        UpdateWeaponDisplay();
    }

    private void BuildUnlockedWeaponsList()
    {
        unlockedWeapons.Clear();

        if (weaponDatabase == null || weaponDatabase.Weapons == null)
        {
            Debug.LogWarning("[ComputerWeaponPanel] WeaponDatabase not assigned or empty.");
            return;
        }

        HashSet<string> unlockedIds = new HashSet<string>();

        if (GameSession.Instance != null && GameSession.Instance.unlockedWeapons != null)
        {
            foreach (string id in GameSession.Instance.unlockedWeapons)
            {
                if (!string.IsNullOrEmpty(id))
                {
                    unlockedIds.Add(id);
                }
            }
        }

        foreach (var weapon in weaponDatabase.Weapons)
        {
            if (weapon == null) continue;

            // Filtro: Solo armas desbloqueadas en GameSession
            if (unlockedIds.Contains(weapon.WeaponId))
            {
                unlockedWeapons.Add(weapon);
            }
        }
    }

    private void OnPreviousClicked()
    {
        if (unlockedWeapons.Count == 0) return;

        currentWeaponIndex--;
        if (currentWeaponIndex < 0)
        {
            currentWeaponIndex = unlockedWeapons.Count - 1; // Wrap-around
        }

        UpdateWeaponDisplay();
    }

    private void OnNextClicked()
    {
        if (unlockedWeapons.Count == 0) return;

        currentWeaponIndex++;
        if (currentWeaponIndex >= unlockedWeapons.Count)
        {
            currentWeaponIndex = 0; // Wrap-around
        }

        UpdateWeaponDisplay();
    }

    private void UpdateWeaponDisplay()
    {
        if (unlockedWeapons.Count == 0 || currentWeaponIndex < 0 || currentWeaponIndex >= unlockedWeapons.Count)
        {
            return;
        }

        WeaponData weapon = unlockedWeapons[currentWeaponIndex];

        // 1. Asignar Icono
        if (weaponIconDisplay != null)
        {
            weaponIconDisplay.sprite = weapon.Icon;
        }

        // 2. Construir string formateado usando StringBuilder
        StringBuilder sb = new StringBuilder();

        sb.AppendLine($"<size=120%><b>{weapon.DisplayName}</b></size>");
        if (weapon.Rarity != null)
        {
            string colorHex = ColorUtility.ToHtmlStringRGB(weapon.Rarity.DisplayColor);
            sb.AppendLine($"<color=#{colorHex}><b>[{weapon.Rarity.DisplayName}]</b></color>");
        }
        sb.AppendLine();

        sb.AppendLine(weapon.Description);
        sb.AppendLine();

        sb.AppendLine("<b>──────── Estats Base ────────</b>");
        string attackType = weapon.IsRanged ? "A Distancia" : "Cuerpo a Cuerpo";
        sb.AppendLine($"• Tipo: {attackType}");
        sb.AppendLine($"• Daño Base: {weapon.Stats.damage}");
        sb.AppendLine($"• Cooldown: {weapon.Stats.cooldown}s");
        if (weapon.IsRanged)
        {
            sb.AppendLine($"• Velocidad Proyectil: {weapon.Stats.projectileSpeed}");
            sb.AppendLine($"• Piercing: {weapon.Stats.pierce}");
        }
        sb.AppendLine();

        sb.AppendLine("<b>──────── Ataques Especiales ────────</b>");
        
        // Skill X
        string nameX = string.IsNullOrEmpty(weapon.SkillXName) ? "Ataque X" : weapon.SkillXName;
        sb.AppendLine($"<b>[X] {nameX}</b>");
        sb.AppendLine($"   Daño: {weapon.DamageX} | Cooldown: {weapon.CooldownX}s | Rango: {weapon.RangeX}m");

        // Skill C
        string nameC = string.IsNullOrEmpty(weapon.SkillCName) ? "Ataque C" : weapon.SkillCName;
        sb.AppendLine($"<b>[C] {nameC}</b>");
        sb.AppendLine($"   Daño: {weapon.DamageC} | Cooldown: {weapon.CooldownC}s | Rango: {weapon.RangeC}m");

        // Ultimate V
        string nameV = string.IsNullOrEmpty(weapon.SkillVName) ? "Definitiva V" : weapon.SkillVName;
        sb.AppendLine($"<b>[V] {nameV}</b>");
        sb.AppendLine($"   Daño: {weapon.DamageV} | Cooldown: {weapon.CooldownV}s | Rango: {weapon.RangeV}m");

        if (weaponInfoText != null)
        {
            weaponInfoText.text = sb.ToString();
        }

        UpdateEquippedStatus(weapon);
    }

    private void OnEquipButtonClicked()
    {
        if (unlockedWeapons.Count == 0 || currentWeaponIndex < 0 || currentWeaponIndex >= unlockedWeapons.Count)
        {
            return;
        }

        WeaponData selectedWeapon = unlockedWeapons[currentWeaponIndex];

        if (weaponSlotManager != null)
        {
            weaponSlotManager.EquipWeapon(selectedWeapon);
        }
        else if (GameSession.Instance != null)
        {
            GameSession.Instance.EquipWeapon(selectedWeapon);
        }

        UpdateEquippedStatus(selectedWeapon);
    }

    private void UpdateEquippedStatus(WeaponData selectedWeapon)
    {
        if (currentEquippedLabel == null) return;

        string currentEquippedId = "";
        if (GameSession.Instance != null)
        {
            currentEquippedId = GameSession.Instance.equippedWeaponId;
        }

        if (selectedWeapon != null && selectedWeapon.WeaponId == currentEquippedId)
        {
            currentEquippedLabel.text = "<color=#55FF55><b>[EQUIPADA]</b></color>";
            if (equipButton != null) equipButton.interactable = false;
        }
        else
        {
            currentEquippedLabel.text = "No Equipada";
            if (equipButton != null) equipButton.interactable = true;
        }
    }

    private void OnBackToMainClicked()
    {
        gameObject.SetActive(false);
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.ShowMainMenu();
        }
    }
}
