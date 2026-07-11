using UnityEngine;
using Sirenix.OdinInspector;

public class WeaponSlotManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    public WeaponData[] equippedWeapons = new WeaponData[4];
    public int currentSlotIndex = 0;

    [Header("References")]
    [SerializeField, ShowInInspector] private Transform weaponHandPoint;
    [SerializeField, ShowInInspector] private LayerMask enemyMask;
    private WeaponInstance activeWeaponInstance;

    [Header("Weapon Levels (For testing)")]
    public int[] weaponLevels = new int[4] { 1, 1, 1, 1 }; // Default all slots to level 1
    
    // Cooldown trackers
    private float nextBaseAttackTime = 0f;
    private float nextSkillXTime = 0f;
    private float nextSkillCTime = 0f;
    private float nextSkillVTime = 0f;

    // We can assume ExperienceSystem manages player level.
    private ExperienceSystem experienceSystem;
    private const int UNLOCK_LEVEL_X = 5;
    private const int UNLOCK_LEVEL_C = 10;

    private void Awake()
    {
        experienceSystem = GetComponent<ExperienceSystem>();

        if (weaponHandPoint == null)
        {
            Transform hand = transform.Find("FirePoint");
            if (hand == null) hand = transform.Find("Mano");

            if (hand == null)
            {
                GameObject handObj = new GameObject("FirePoint");
                handObj.transform.SetParent(this.transform);
                // Default offset for right-facing sprite
                handObj.transform.localPosition = new Vector3(0.5f, 0f, 0f); 
                weaponHandPoint = handObj.transform;
            }
            else
            {
                weaponHandPoint = hand;
            }
        }
    }

    private void Start()
    {
        // Try equipping initial weapons
        SwitchWeapon(currentSlotIndex);
    }

    private void Update()
    {
        // Handle Auto-Attack loop (hybrid combat)
        AutoAttackProximityCheck();

        // Update the hand transform based on aim direction
        UpdateHandTransform();
    }

    private float lastFacingDirection = 1f;

    private void UpdateHandTransform()
    {
        if (weaponHandPoint == null) return;

        // Utilizamos la dirección de movimiento horizontal
        float moveX = Input.GetAxisRaw("Horizontal");

        // Actualizamos la dirección solo si hay un input claro, ignorando el Idle (0)
        if (moveX > 0.1f)
        {
            lastFacingDirection = 1f;
        }
        else if (moveX < -0.1f)
        {
            lastFacingDirection = -1f;
        }

        // Mantenemos la magnitud del offset independientemente de su signo actual
        float offsetMagnitude = Mathf.Abs(weaponHandPoint.localPosition.x);
        if (offsetMagnitude < 0.01f) offsetMagnitude = 0.5f;

        // Aplicamos la simetría estricta usando la última dirección válida
        if (lastFacingDirection < 0)
        {
            weaponHandPoint.localPosition = new Vector3(-offsetMagnitude, weaponHandPoint.localPosition.y, weaponHandPoint.localPosition.z);
            weaponHandPoint.localScale = new Vector3(-1f, 1f, 1f);
        }
        else
        {
            weaponHandPoint.localPosition = new Vector3(offsetMagnitude, weaponHandPoint.localPosition.y, weaponHandPoint.localPosition.z);
            weaponHandPoint.localScale = new Vector3(1f, 1f, 1f);
        }
    }

    public void EquipWeapon(WeaponData newWeapon, int slot)
    {
        if (slot >= 0 && slot < equippedWeapons.Length)
        {
            equippedWeapons[slot] = newWeapon;
            if (currentSlotIndex == slot)
            {
                SwitchWeapon(slot); // Refresh if it's currently active
            }
        }
    }

    public void SwitchWeapon(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= equippedWeapons.Length) return;

        currentSlotIndex = slotIndex;
        WeaponData weaponToEquip = equippedWeapons[currentSlotIndex];

        // Clean up previous weapon visual
        if (activeWeaponInstance != null)
        {
            Destroy(activeWeaponInstance.gameObject);
        }

        if (weaponToEquip != null)
        {
            GameObject visualPrefab = weaponToEquip.WeaponPrefab;
            if (visualPrefab != null)
            {
                GameObject weaponObj = Instantiate(visualPrefab, weaponHandPoint.position, Quaternion.identity, weaponHandPoint);
                weaponObj.transform.localPosition = Vector3.zero;

                activeWeaponInstance = weaponObj.GetComponent<WeaponInstance>();
                if (activeWeaponInstance == null)
                {
                    activeWeaponInstance = weaponObj.AddComponent<WeaponInstance>();
                }
                
                activeWeaponInstance.Init(weaponToEquip, weaponLevels[currentSlotIndex]);
            }
            else 
            {
                // Create a virtual instance if no prefab exists
                GameObject virtualWeapon = new GameObject("VirtualWeapon_" + weaponToEquip.WeaponId);
                virtualWeapon.transform.SetParent(weaponHandPoint);
                virtualWeapon.transform.localPosition = Vector3.zero;
                activeWeaponInstance = virtualWeapon.AddComponent<WeaponInstance>();
                activeWeaponInstance.Init(weaponToEquip, weaponLevels[currentSlotIndex]);
            }
        }
    }

    public void RequestBaseAttack(Vector2 aimDirection, float damageMultiplier = 1f)
    {
        if (activeWeaponInstance == null || equippedWeapons[currentSlotIndex] == null) return;
        
        if (Time.time >= nextBaseAttackTime)
        {
            activeWeaponInstance.ExecuteBaseAttack(weaponHandPoint, aimDirection, damageMultiplier);
            nextBaseAttackTime = Time.time + equippedWeapons[currentSlotIndex].Stats.cooldown;
        }
    }

    public void RequestAttackX(Vector2 aimDirection, float damageMultiplier = 1f)
    {
        if (activeWeaponInstance == null || equippedWeapons[currentSlotIndex] == null) return;
        
        int playerLevel = experienceSystem != null ? experienceSystem.level : 1;
        if (playerLevel < UNLOCK_LEVEL_X)
        {
            Debug.Log($"[WeaponSlotManager] Level {UNLOCK_LEVEL_X} required for Skill X. Current: {playerLevel}");
            return;
        }

        if (Time.time >= nextSkillXTime)
        {
            activeWeaponInstance.ExecuteAttackX(weaponHandPoint, aimDirection, damageMultiplier);
            nextSkillXTime = Time.time + equippedWeapons[currentSlotIndex].CooldownX;
        }
    }

    public void RequestAttackC(Vector2 aimDirection, float damageMultiplier = 1f)
    {
        if (activeWeaponInstance == null || equippedWeapons[currentSlotIndex] == null) return;

        int playerLevel = experienceSystem != null ? experienceSystem.level : 1;
        if (playerLevel < UNLOCK_LEVEL_C)
        {
            Debug.Log($"[WeaponSlotManager] Level {UNLOCK_LEVEL_C} required for Skill C. Current: {playerLevel}");
            return;
        }

        if (Time.time >= nextSkillCTime)
        {
            activeWeaponInstance.ExecuteAttackC(weaponHandPoint, aimDirection, damageMultiplier);
            nextSkillCTime = Time.time + equippedWeapons[currentSlotIndex].CooldownC;
        }
    }

    public void RequestAttackV(Vector2 aimDirection, float damageMultiplier = 1f)
    {
        if (activeWeaponInstance == null || equippedWeapons[currentSlotIndex] == null) return;

        int currentLvl = weaponLevels[currentSlotIndex];
        int maxLvl = equippedWeapons[currentSlotIndex].MaxInternalLevel;

        if (currentLvl < maxLvl)
        {
            Debug.Log($"[WeaponSlotManager] Weapon Level {maxLvl} required for Skill V. Current: {currentLvl}");
            return;
        }

        if (Time.time >= nextSkillVTime)
        {
            activeWeaponInstance.ExecuteAttackV(weaponHandPoint, aimDirection, damageMultiplier);
            nextSkillVTime = Time.time + equippedWeapons[currentSlotIndex].CooldownV;
        }
    }

    private void AutoAttackProximityCheck()
    {
        if (activeWeaponInstance == null || equippedWeapons[currentSlotIndex] == null) return;
        if (Time.time < nextBaseAttackTime) return; // Not ready

        float range = equippedWeapons[currentSlotIndex].AttackRange;
        
        // Find closest enemy
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, range);
        Transform closestEnemy = null;
        float minDst = float.MaxValue;

        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                float dst = Vector2.Distance(transform.position, enemy.transform.position);
                if (dst < minDst)
                {
                    minDst = dst;
                    closestEnemy = enemy.transform;
                }
            }
        }

        if (closestEnemy != null)
        {
            Vector2 dir = (closestEnemy.position - transform.position).normalized;
            RequestBaseAttack(dir); // Execute
        }
    }

    // Call to upgrade weapon manually
    public void UpgradeCurrentWeapon()
    {
        if (equippedWeapons[currentSlotIndex] != null)
        {
            if (weaponLevels[currentSlotIndex] < equippedWeapons[currentSlotIndex].MaxInternalLevel)
            {
                weaponLevels[currentSlotIndex]++;
                SwitchWeapon(currentSlotIndex); // re-init
            }
        }
    }
}
