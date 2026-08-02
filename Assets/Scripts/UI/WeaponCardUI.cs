using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class WeaponCardUI : MonoBehaviour
{
    [SerializeField] private Image weaponIcon;
    [SerializeField] private TMP_Text weaponNameText;
    [SerializeField] private Image cardBackground; 
    [SerializeField] private Image rarityBorder;
    [SerializeField] private Button cardButton;

    private WeaponData data;
    public System.Action<WeaponData> OnCardClicked;

    public void Setup(WeaponData weaponData)
    {
        data = weaponData;
        
        if (weaponIcon != null)
            weaponIcon.sprite = weaponData.Icon;
            
        if (weaponNameText != null)
            weaponNameText.text = weaponData.DisplayName;
            
        if (weaponData.Rarity != null && rarityBorder != null)
            rarityBorder.color = weaponData.Rarity.DisplayColor;
            
        if (cardButton != null)
        {
            cardButton.onClick.RemoveAllListeners();
            cardButton.onClick.AddListener(() => OnCardClicked?.Invoke(data));
        }
    }
}
