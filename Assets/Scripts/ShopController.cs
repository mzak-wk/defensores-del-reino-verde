using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

[System.Serializable]
public class ShopSlot
{
    public Button button;
    public Image iconImage;
    public TextMeshProUGUI priceText;
    public Image coinIcon;
    [HideInInspector] public ItemData currentItem;
    [HideInInspector] public int calculatedPrice;
    [HideInInspector] public int currentLevel;
    [HideInInspector] public int maxLevel;
}

public class ShopController : MonoBehaviour
{
    [Header("Base de Datos")]
    [SerializeField] private ItemDatabase itemDatabase;

    [Header("Slots de Tienda (3)")]
    [SerializeField] private ShopSlot[] shopSlots = new ShopSlot[3];

    [Header("UI Descripcion")]
    [SerializeField] private TextMeshProUGUI itemDescriptionText;

    [Header("Panel Container (para Shake)")]
    [SerializeField] private RectTransform panelContainer;

    [Header("Reroll")]
    [SerializeField] private Button btnReroll;
    [SerializeField] private TextMeshProUGUI rerollCostText;

    [Header("Oro")]
    [SerializeField] private TextMeshProUGUI goldText;

    [Header("Precios Base por Rareza")]
    [SerializeField] private int basePriceComun = 5;
    [SerializeField] private int basePriceRaro = 15;
    [SerializeField] private int basePriceEpico = 30;
    [SerializeField] private int basePriceLegendario = 60;

    [Header("Reroll")]
    [SerializeField] private int initialRerollCost = 5;

    [Header("Shake")]
    [SerializeField] private float shakeDuration = 0.3f;
    [SerializeField] private float shakeMagnitude = 8f;
    [SerializeField] private float shakeFrequency = 0.03f;

    private int currentRerollCost;
    private int selectedSlotIndex = -1;
    private Coroutine shakeCoroutine;
    private bool isShopGenerated = false;

    private void Awake()
    {
        currentRerollCost = initialRerollCost;

        if (btnReroll != null)
            btnReroll.onClick.AddListener(OnRerollClicked);

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i]?.button != null)
            {
                int capturedIndex = i;
                shopSlots[i].button.onClick.AddListener(() => OnSlotClicked(capturedIndex));
            }
        }
    }

    private void OnEnable()
    {
        selectedSlotIndex = -1;

        if (!isShopGenerated)
            GenerateNewShop();
        else
            RefreshShopVisuals();
    }

    private void OnDisable()
    {
        selectedSlotIndex = -1;
    }

    private void GenerateNewShop()
    {
        selectedSlotIndex = -1;
        PopulateSlots();
        UpdateGoldDisplay();
        UpdateRerollDisplay();
        ClearDescription();
        isShopGenerated = true;
    }

    private void RefreshShopVisuals()
    {
        selectedSlotIndex = -1;

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null) continue;

            shopSlots[i].button.onClick.RemoveAllListeners();
            int capturedIndex = i;
            shopSlots[i].button.onClick.AddListener(() => OnSlotClicked(capturedIndex));

            if (shopSlots[i].currentItem != null)
            {
                shopSlots[i].iconImage.enabled = true;
                shopSlots[i].iconImage.color = Color.white;
                shopSlots[i].button.interactable = true;
            }
            else
            {
                shopSlots[i].iconImage.sprite = null;
                shopSlots[i].iconImage.enabled = false;
                shopSlots[i].priceText.text = "---";
                shopSlots[i].button.interactable = false;
            }
        }

        UpdateGoldDisplay();
        UpdateRerollDisplay();
        ClearDescription();
    }

    private void PopulateSlots()
    {
        HashSet<string> excludeIds = new HashSet<string>();

        for (int i = 0; i < shopSlots.Length; i++)
        {
            if (shopSlots[i] == null) continue;

            shopSlots[i].button.onClick.RemoveAllListeners();
            int capturedIndex = i;
            shopSlots[i].button.onClick.AddListener(() => OnSlotClicked(capturedIndex));

            ItemData item = GetRandomAvailableItem(excludeIds);

            if (item != null)
            {
                excludeIds.Add(item.ItemId);
                int currentLevel = GameSession.Instance.GetItemLevel(item.ItemId);
                int maxLevel = GameSession.Instance.GetMaxLevelForRarity(item.Rarity);

                shopSlots[i].currentItem = item;
                shopSlots[i].currentLevel = currentLevel;
                shopSlots[i].maxLevel = maxLevel;
                shopSlots[i].calculatedPrice = CalculatePrice(item, currentLevel);
                shopSlots[i].iconImage.sprite = item.Icon;
                shopSlots[i].iconImage.enabled = true;
                shopSlots[i].iconImage.color = Color.white;
                shopSlots[i].priceText.text = shopSlots[i].calculatedPrice.ToString();
                shopSlots[i].button.interactable = true;
            }
            else
            {
                shopSlots[i].currentItem = null;
                shopSlots[i].iconImage.sprite = null;
                shopSlots[i].iconImage.enabled = false;
                shopSlots[i].priceText.text = "---";
                shopSlots[i].button.interactable = false;
            }
        }
    }

    private ItemData GetRandomAvailableItem(HashSet<string> excludeIds)
    {
        if (itemDatabase == null || GameSession.Instance == null) return null;

        int highestLevel = GameSession.Instance.highestLevelUnlocked;
        float[] weights = CalculateDropWeights(highestLevel);

        List<ItemData> candidates = new List<ItemData>();
        List<float> candidateWeights = new List<float>();

        foreach (ItemData item in itemDatabase.AllItems)
        {
            if (item == null) continue;

            int currentLevel = GameSession.Instance.GetItemLevel(item.ItemId);
            int maxLevel = GameSession.Instance.GetMaxLevelForRarity(item.Rarity);

            if (currentLevel >= maxLevel) continue;
            if (excludeIds != null && excludeIds.Contains(item.ItemId)) continue;

            candidates.Add(item);
            candidateWeights.Add(weights[(int)item.Rarity]);
        }

        if (candidates.Count == 0) return null;

        float totalWeight = 0f;
        foreach (float w in candidateWeights) totalWeight += w;

        float random = Random.Range(0f, totalWeight);
        float cumulative = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            cumulative += candidateWeights[i];
            if (random <= cumulative)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    private float[] CalculateDropWeights(int highestLevel)
    {
        float[] weights = new float[4];

        float comunBase = 70f;
        float raroBase = 25f;
        float epicoBase = 4f;
        float legendarioBase = 1f;

        float bonus = Mathf.Min(highestLevel * 0.5f, 20f);

        weights[0] = Mathf.Max(comunBase - bonus * 2, 20f);
        weights[1] = Mathf.Min(raroBase + bonus, 45f);
        weights[2] = Mathf.Min(epicoBase + bonus * 0.5f, 20f);
        weights[3] = Mathf.Min(legendarioBase + bonus * 0.2f, 5f);

        return weights;
    }

    private int CalculatePrice(ItemData item, int currentLevel)
    {
        int basePrice;
        switch (item.Rarity)
        {
            case ItemRarity.Comun: basePrice = basePriceComun; break;
            case ItemRarity.Raro: basePrice = basePriceRaro; break;
            case ItemRarity.Epico: basePrice = basePriceEpico; break;
            case ItemRarity.Legendario: basePrice = basePriceLegendario; break;
            default: basePrice = basePriceComun; break;
        }

        return Mathf.CeilToInt(basePrice * (1f + 0.5f * currentLevel));
    }

    private void OnSlotClicked(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= shopSlots.Length) return;

        ShopSlot slot = shopSlots[slotIndex];
        if (slot.currentItem == null) return;

        if (selectedSlotIndex == slotIndex)
        {
            AttemptPurchase(slotIndex);
            return;
        }

        DeselectAll();
        selectedSlotIndex = slotIndex;
        slot.iconImage.color = new Color(1f, 1f, 0.85f);

        ShowItemDescription(slot.currentItem, slot.currentLevel, slot.maxLevel);
    }

    private void AttemptPurchase(int slotIndex)
    {
        ShopSlot slot = shopSlots[slotIndex];
        if (slot.currentItem == null || GameSession.Instance == null) return;

        if (!GameSession.Instance.TrySpend(slot.calculatedPrice))
        {
            PlayShake();
            return;
        }

        int newLevel = GameSession.Instance.GetItemLevel(slot.currentItem.ItemId) + 1;
        GameSession.Instance.SetItemLevel(slot.currentItem.ItemId, newLevel);

        if (!GameSession.Instance.unlockedItems.Contains(slot.currentItem.ItemId))
            GameSession.Instance.unlockedItems.Add(slot.currentItem.ItemId);

        ClearSlot(slotIndex);
        DeselectAll();
        ClearDescription();
        UpdateGoldDisplay();
        UpdateRerollDisplay();
    }

    private void ClearSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= shopSlots.Length) return;

        ShopSlot slot = shopSlots[slotIndex];
        slot.currentItem = null;
        slot.iconImage.sprite = null;
        slot.iconImage.enabled = false;
        slot.priceText.text = "---";
        slot.button.interactable = false;
    }

    private void OnRerollClicked()
    {
        if (GameSession.Instance == null) return;

        if (!GameSession.Instance.TrySpend(currentRerollCost))
        {
            PlayShake();
            return;
        }

        currentRerollCost *= 2;
        GenerateNewShop();
    }

    private void DeselectAll()
    {
        if (selectedSlotIndex >= 0 && selectedSlotIndex < shopSlots.Length
            && shopSlots[selectedSlotIndex] != null)
        {
            shopSlots[selectedSlotIndex].iconImage.color = Color.white;
        }

        selectedSlotIndex = -1;
    }

    private void ShowItemDescription(ItemData item, int currentLevel, int maxLevel)
    {
        if (itemDescriptionText == null || item == null) return;

        string name = item.ItemName;
        string rarity = item.Rarity.ToString();
        string levelInfo;
        if (currentLevel == 0)
        {
            levelInfo = "Nivel: Nuevo → Pasará a Nivel 1";
        }
        else if (maxLevel >= 999)
        {
            levelInfo = $"Nivel actual: {currentLevel} → Siguiente: {currentLevel + 1}";
        }
        else
        {
            levelInfo = $"Nivel actual: {currentLevel} → Siguiente: {currentLevel + 1} (Máx: {maxLevel})";
        }
        string passive = string.IsNullOrEmpty(item.PassiveConcept) ? "Sin pasiva" : item.PassiveConcept;

        itemDescriptionText.text = $"{name} [{rarity}]\n{levelInfo}\n{passive}";
    }

    private void ClearDescription()
    {
        if (itemDescriptionText != null)
            itemDescriptionText.text = "Selecciona un item para ver sus detalles.";
    }

    private void UpdateGoldDisplay()
    {
        if (goldText == null || GameSession.Instance == null) return;
        goldText.text = GameSession.Instance.gold.ToString();
    }

    private void UpdateRerollDisplay()
    {
        if (rerollCostText == null) return;
        rerollCostText.text = currentRerollCost.ToString();
    }

    private void PlayShake()
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

    public void ResetRerollCost()
    {
        currentRerollCost = initialRerollCost;
        isShopGenerated = false;
    }
}
