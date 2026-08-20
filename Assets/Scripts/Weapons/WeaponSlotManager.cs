using UnityEngine;

public class WeaponSlotManager : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponDatabase weaponDatabase;

    [Header("Weapon Animation")]
    [SerializeField] private int weaponIndex = -1;

    private WeaponData currentWeapon;
    private int currentWeaponLevel = 1;
    private Animator playerAnimator;
    private bool _weaponDirty;

    public WeaponData CurrentWeapon => currentWeapon;
    public int CurrentWeaponLevel => currentWeaponLevel;
    public int WeaponIndex => weaponIndex;

    private void Awake()
    {
        playerAnimator = GetComponent<Animator>();
        if (playerAnimator == null)
            playerAnimator = GetComponentInChildren<Animator>();
    }

    private void Start()
    {
        if (GameSession.Instance != null)
        {
            if (!string.IsNullOrEmpty(GameSession.Instance.equippedWeaponId) && weaponDatabase != null)
            {
                WeaponData savedWeapon = weaponDatabase.GetWeaponById(GameSession.Instance.equippedWeaponId);
                if (savedWeapon != null)
                {
                    currentWeapon = savedWeapon;
                    currentWeaponLevel = 1;
                }
            }
            SetWeaponIndex(GameSession.Instance.equippedWeaponIndex);
        }
        else
        {
            SetWeaponIndex(weaponIndex);
        }
    }

    private void LateUpdate()
    {
        if (!_weaponDirty || playerAnimator == null) return;

        if (playerAnimator.GetInteger("WeaponIndex") != weaponIndex)
        {
            playerAnimator.SetInteger("WeaponIndex", weaponIndex);
        }
        else
        {
            _weaponDirty = false;
        }
    }

    public void EquipWeapon(WeaponData newWeapon)
    {
        currentWeapon = newWeapon;
        currentWeaponLevel = 1;

        if (newWeapon != null)
        {
            GameSession.Instance?.EquipWeapon(newWeapon);
        }

        RefreshWeaponAnimation();
    }

    public void UpgradeCurrentWeapon()
    {
        if (currentWeapon == null) return;

        if (currentWeaponLevel < currentWeapon.MaxInternalLevel)
        {
            currentWeaponLevel++;
        }
    }

    public void SetWeaponIndex(int index)
    {
        weaponIndex = index;
        _weaponDirty = true;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.CurrentWeaponIndex = index;
        }

        if (playerAnimator == null) playerAnimator = GetComponent<Animator>();
        if (playerAnimator != null)
        {
            playerAnimator.enabled = true;
            playerAnimator.SetInteger("WeaponIndex", index);
            playerAnimator.Update(0f);
        }
    }

    public void RefreshWeaponAnimation()
    {
        if (playerAnimator == null) return;

        int index = (currentWeapon != null) ? currentWeapon.WeaponIndex : weaponIndex;
        SetWeaponIndex(index);
    }
}
