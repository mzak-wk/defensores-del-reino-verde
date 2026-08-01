using UnityEngine;
using Sirenix.OdinInspector;

public class WeaponSlotManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    public WeaponData[] equippedWeapons = new WeaponData[4];
    public int currentSlotIndex = 0;

    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Weapon Levels")]
    public int[] weaponLevels = new int[4] { 1, 1, 1, 1 };

    private Animator playerAnimator;
    private RuntimeAnimatorController originalAnimatorController;

    public WeaponData CurrentWeapon =>
        currentSlotIndex >= 0 && currentSlotIndex < equippedWeapons.Length
            ? equippedWeapons[currentSlotIndex]
            : null;

    public int CurrentWeaponLevel =>
        currentSlotIndex >= 0 && currentSlotIndex < weaponLevels.Length
            ? weaponLevels[currentSlotIndex]
            : 1;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
    }

    private void Start()
    {
        if (playerAnimator != null && originalAnimatorController == null)
        {
            originalAnimatorController = playerAnimator.runtimeAnimatorController;
        }

        if (GameSession.Instance != null
            && !string.IsNullOrEmpty(GameSession.Instance.equippedWeaponId)
            && weaponDatabase != null)
        {
            WeaponData savedWeapon = weaponDatabase.GetWeaponById(GameSession.Instance.equippedWeaponId);
            if (savedWeapon != null)
            {
                EquipWeaponAtSlot(currentSlotIndex, savedWeapon);
                return;
            }
        }

        SwitchWeapon(currentSlotIndex);
    }

    public void EquipWeaponAtSlot(int slot, WeaponData newWeapon)
    {
        if (slot < 0 || slot >= equippedWeapons.Length) return;

        equippedWeapons[slot] = newWeapon;

        if (currentSlotIndex == slot)
        {
            SwitchWeapon(slot);
        }

        if (GameSession.Instance != null && newWeapon != null)
        {
            GameSession.Instance.equippedWeaponId = newWeapon.WeaponId;
        }
    }

    public void SwitchWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedWeapons.Length) return;

        currentSlotIndex = slotIndex;
        WeaponData weaponToEquip = equippedWeapons[currentSlotIndex];

        if (weaponToEquip != null)
        {
            ApplyWeaponAnimator(weaponToEquip.WeaponAnimator);

            if (GameSession.Instance != null)
            {
                GameSession.Instance.equippedWeaponId = weaponToEquip.WeaponId;
            }
        }
        else
        {
            ApplyWeaponAnimator(null);
        }
    }

    private void ApplyWeaponAnimator(AnimatorOverrideController overrideController)
    {
        if (playerAnimator == null) return;

        playerAnimator.runtimeAnimatorController =
            overrideController != null ? overrideController : originalAnimatorController;
    }

    public void UpgradeCurrentWeapon()
    {
        WeaponData weapon = CurrentWeapon;
        if (weapon == null) return;

        if (weaponLevels[currentSlotIndex] < weapon.MaxInternalLevel)
        {
            weaponLevels[currentSlotIndex]++;
            SwitchWeapon(currentSlotIndex);
        }
    }
}
