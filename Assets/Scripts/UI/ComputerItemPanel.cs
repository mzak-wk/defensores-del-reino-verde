using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Text.RegularExpressions;

[System.Serializable]
public class ItemSlot
{
    public Button button;
    public Image iconImage;
    [HideInInspector] public ItemData assignedItem;
}

public class ComputerItemPanel : MonoBehaviour
{
    [Header("Navegacion")]
    [SerializeField] private Button btnBackToMainPanel;

    [Header("Base de Datos")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Configuracion")]
    [SerializeField] private bool testModeInEditor = false;

    [Header("Grid de Items Desbloqueados (Izquierda - 15 Slots)")]
    [SerializeField] private ItemSlot[] gridSlots = new ItemSlot[15];

    [Header("Slots Equipados (Derecha - 3 Slots)")]
    [SerializeField] private ItemSlot[] equipSlots = new ItemSlot[3];
    [SerializeField] private Image[] equipSlotBorders = new Image[3];

    [Header("Panel de Descripcion (Inferior)")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Configuracion Visual")]
    [SerializeField] private Color emptySlotColor = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    [SerializeField] private Color filledSlotColor = Color.white;
    [SerializeField] private Color selectedSlotTint = new Color(1f, 1f, 0.85f);

    [Header("Shake (Feedback Slots Llenos)")]
    [SerializeField] private RectTransform panelContainer;
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 8f;
    [SerializeField] private float shakeFrequency = 0.03f;

    private List<ItemData> unlockedItemsCache = new List<ItemData>();
    private ItemData[] equippedItemsCache = new ItemData[3];
    private ItemData selectedItem;
    private int selectedGridIndex = -1;
    private int selectedEquipIndex = -1;
    private Coroutine shakeCoroutine;

    private void Awake()
    {
        if (btnBackToMainPanel != null)
        {
            btnBackToMainPanel.onClick.AddListener(OnBackToMainClicked);
        }
    }

    private void OnEnable()
    {
        RefreshPanel();
    }

    private void OnDisable()
    {
        DeselectAll();
        ClearDescription();
    }

    public void RefreshPanel()
    {
        DeselectAll();
        LoadUnlockedItems();
        LoadEquippedItems();
        PopulateGridSlots();
        UpdateEquipSlotsVisual();
        ClearDescription();
    }

    private void LoadUnlockedItems()
    {
        unlockedItemsCache.Clear();

        if (itemDatabase == null) return;

        bool useAllItems = testModeInEditor
            || GameSession.Instance == null
            || GameSession.Instance.unlockedItems == null;

        if (useAllItems)
        {
            foreach (ItemData item in itemDatabase.AllItems)
            {
                if (item != null)
                {
                    unlockedItemsCache.Add(item);
                }
            }
            return;
        }

        HashSet<string> unlockedIds = new HashSet<string>(GameSession.Instance.unlockedItems);

        foreach (ItemData item in itemDatabase.AllItems)
        {
            if (item == null) continue;
            if (unlockedIds.Contains(item.ItemId))
            {
                unlockedItemsCache.Add(item);
            }
        }
    }

    private void LoadEquippedItems()
    {
        for (int i = 0; i < 3; i++)
        {
            equippedItemsCache[i] = null;
        }

        if (GameSession.Instance == null || itemDatabase == null) return;

        string[] equippedIds = GameSession.Instance.equippedItems;
        if (equippedIds == null) return;

        for (int i = 0; i < Mathf.Min(3, equippedIds.Length); i++)
        {
            if (!string.IsNullOrEmpty(equippedIds[i]))
            {
                equippedItemsCache[i] = itemDatabase.GetItemById(equippedIds[i]);
            }
        }
    }

    private void PopulateGridSlots()
    {
        int count = Mathf.Min(unlockedItemsCache.Count, 15);

        for (int i = 0; i < gridSlots.Length; i++)
        {
            if (gridSlots[i] == null) continue;

            gridSlots[i].button.onClick.RemoveAllListeners();

            if (i < count)
            {
                ItemData item = unlockedItemsCache[i];
                gridSlots[i].assignedItem = item;
                gridSlots[i].iconImage.sprite = item.Icon;
                gridSlots[i].iconImage.enabled = true;

                bool isEquipped = GameSession.Instance != null
                    && GameSession.Instance.IsItemEquipped(item.ItemId);

                if (isEquipped)
                {
                    gridSlots[i].iconImage.color = new Color(0.25f, 0.25f, 0.25f, 0.6f);
                    gridSlots[i].button.interactable = false;
                }
                else
                {
                    gridSlots[i].iconImage.color = filledSlotColor;
                    gridSlots[i].button.interactable = true;

                    int capturedIndex = i;
                    gridSlots[i].button.onClick.AddListener(() => OnGridSlotClicked(capturedIndex));
                }
            }
            else
            {
                gridSlots[i].assignedItem = null;
                gridSlots[i].iconImage.sprite = null;
                gridSlots[i].iconImage.enabled = false;
                gridSlots[i].button.interactable = false;
            }
        }
    }

    private void UpdateEquipSlotsVisual()
    {
        for (int i = 0; i < equipSlots.Length; i++)
        {
            if (equipSlots[i] == null) continue;

            equipSlots[i].button.onClick.RemoveAllListeners();

            if (equippedItemsCache[i] != null)
            {
                equipSlots[i].assignedItem = equippedItemsCache[i];
                equipSlots[i].iconImage.sprite = equippedItemsCache[i].Icon;
                equipSlots[i].iconImage.color = filledSlotColor;
                equipSlots[i].iconImage.enabled = true;
                equipSlots[i].button.interactable = true;

                int capturedIndex = i;
                equipSlots[i].button.onClick.AddListener(() => OnEquipSlotClicked(capturedIndex));
            }
            else
            {
                equipSlots[i].assignedItem = null;
                equipSlots[i].iconImage.sprite = null;
                equipSlots[i].iconImage.enabled = false;
                equipSlots[i].button.interactable = false;
            }

            if (equipSlotBorders != null && i < equipSlotBorders.Length && equipSlotBorders[i] != null)
            {
                equipSlotBorders[i].color = equippedItemsCache[i] != null
                    ? GetRarityColor(equippedItemsCache[i].Rarity)
                    : new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
        }
    }

    private void OnGridSlotClicked(int gridIndex)
    {
        if (gridIndex < 0 || gridIndex >= gridSlots.Length) return;

        ItemSlot slot = gridSlots[gridIndex];
        if (slot.assignedItem == null) return;

        if (selectedGridIndex == gridIndex)
        {
            AttemptEquip(slot.assignedItem);
            return;
        }

        DeselectAll();
        selectedItem = slot.assignedItem;
        selectedGridIndex = gridIndex;

        slot.iconImage.color = selectedSlotTint;

        ShowItemDescription(slot.assignedItem);
    }

    private void OnEquipSlotClicked(int equipIndex)
    {
        if (equipIndex < 0 || equipIndex >= equipSlots.Length) return;

        ItemSlot slot = equipSlots[equipIndex];
        if (slot.assignedItem == null) return;

        if (selectedEquipIndex == equipIndex)
        {
            UnequipItem(equipIndex);
            return;
        }

        DeselectAll();
        selectedItem = slot.assignedItem;
        selectedEquipIndex = equipIndex;

        slot.iconImage.color = selectedSlotTint;

        ShowItemDescription(slot.assignedItem);
    }

    private void AttemptEquip(ItemData item)
    {
        if (item == null) return;

        if (GameSession.Instance.IsItemEquipped(item.ItemId))
        {
            Debug.Log($"[ComputerItemPanel] El item '{item.ItemName}' ya esta equipado.");
            return;
        }

        if (item.Rarity == ItemRarity.Legendario)
        {
            for (int i = 0; i < 3; i++)
            {
                if (string.IsNullOrEmpty(GameSession.Instance.equippedItems[i])) continue;

                ItemData equipped = itemDatabase.GetItemById(GameSession.Instance.equippedItems[i]);
                if (equipped != null && equipped.Rarity == ItemRarity.Legendario)
                {
                    PlayEquipSlotsShake();
                    ShowItemDescription(item);
                    Debug.Log($"[ComputerItemPanel] Ya hay un Legendario equipado. No se puede equipar '{item.ItemName}'.");
                    return;
                }
            }
        }

        int freeSlot = -1;
        for (int i = 0; i < 3; i++)
        {
            if (string.IsNullOrEmpty(GameSession.Instance.equippedItems[i]))
            {
                freeSlot = i;
                break;
            }
        }

        if (freeSlot == -1)
        {
            PlayEquipSlotsShake();
            return;
        }

        GameSession.Instance.EquipItem(item.ItemId);
        DeselectAll();
        ClearDescription();
        RefreshPanel();
        Debug.Log($"[ComputerItemPanel] Item '{item.ItemName}' equipado en slot {freeSlot}.");
    }

    private void UnequipItem(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= 3) return;

        ItemData removedItem = equippedItemsCache[slotIndex];
        if (removedItem == null) return;

        GameSession.Instance.UnequipItem(slotIndex);
        DeselectAll();
        ClearDescription();
        RefreshPanel();
        Debug.Log($"[ComputerItemPanel] Item '{removedItem.ItemName}' desequipado del slot {slotIndex}.");
    }

    private void DeselectAll()
    {
        if (selectedGridIndex >= 0 && selectedGridIndex < gridSlots.Length
            && gridSlots[selectedGridIndex] != null)
        {
            gridSlots[selectedGridIndex].iconImage.color = filledSlotColor;
        }

        if (selectedEquipIndex >= 0 && selectedEquipIndex < equipSlots.Length
            && equipSlots[selectedEquipIndex] != null)
        {
            equipSlots[selectedEquipIndex].iconImage.color = filledSlotColor;
        }

        selectedItem = null;
        selectedGridIndex = -1;
        selectedEquipIndex = -1;
    }

    private void PlayEquipSlotsShake()
    {
        if (shakeCoroutine != null)
            StopCoroutine(shakeCoroutine);

        shakeCoroutine = StartCoroutine(ShakePanelCoroutine());
    }

    private IEnumerator ShakePanelCoroutine()
    {
        if (panelContainer == null) yield break;

        Vector3 originalPos = panelContainer.localPosition;
        float elapsed = 0f;

        while (elapsed < shakeDuration)
        {
            float angle = Random.value * Mathf.PI * 2f;
            float radius = Random.value * shakeMagnitude;
            Vector3 offset = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;

            panelContainer.localPosition = originalPos + offset;

            elapsed += shakeFrequency;
            yield return new WaitForSeconds(shakeFrequency);
        }

        panelContainer.localPosition = originalPos;
        shakeCoroutine = null;
    }

    private Color GetRarityColor(ItemRarity rarity)
    {
        switch (rarity)
        {
            case ItemRarity.Comun: return Color.white;
            case ItemRarity.Raro: return new Color(0.3f, 0.6f, 1f);
            case ItemRarity.Epico: return new Color(0.7f, 0.2f, 1f);
            case ItemRarity.Legendario: return new Color(1f, 0.8f, 0.2f);
            default: return Color.white;
        }
    }

    private static string CleanText(string input)
    {
        return string.IsNullOrEmpty(input) ? "" : Regex.Replace(input, "<.*?>", string.Empty).Trim();
    }

    private void ShowItemDescription(ItemData item)
    {
        if (itemDescriptionText == null || item == null) return;

        string cleanName = CleanText(item.ItemName);
        string cleanRarity = item.Rarity.ToString();
        string cleanPassive = CleanText(item.PassiveConcept);

        int level = 0;
        if (GameSession.Instance != null)
            level = GameSession.Instance.GetItemLevel(item.ItemId);

        itemDescriptionText.text = $"{cleanName} [{cleanRarity}] (Nivel {level})\nPasiva: {cleanPassive}";
    }

    private void ClearDescription()
    {
        if (itemDescriptionText != null)
        {
            itemDescriptionText.text = "Selecciona un item para ver sus detalles.";
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
