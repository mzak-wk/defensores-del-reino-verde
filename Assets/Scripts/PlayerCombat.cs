using UnityEngine;

public class PlayerCombat : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private WeaponSlotManager weaponSlotManager;
    [SerializeField] private Animator animator;
    [SerializeField] private Transform firePoint;

    [Header("Facing")]
    [Tooltip("SpriteRenderer del jugador para detectar orientacion FlipX.")]
    [SerializeField] private SpriteRenderer playerSprite;

    private float nextBasicTime;
    private float nextSkillXTime;
    private float nextSkillCTime;
    private float nextSkillVTime;

    private void Awake()
    {
        if (weaponSlotManager == null)
            weaponSlotManager = GetComponent<WeaponSlotManager>();
        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void Update()
    {
        HandleBasicAttack();
        HandleSkillX();
        HandleSkillC();
        HandleSkillV();
    }

    private void HandleBasicAttack()
    {
        if (!Input.GetMouseButton(0)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.BasicAttackPrefab == null) return;
        if (Time.time < nextBasicTime) return;

        nextBasicTime = Time.time + weapon.BasicCooldown;

        animator.SetTrigger("BasicAttack");
        SpawnAttack(weapon.BasicAttackPrefab);
    }

    private void HandleSkillX()
    {
        if (!Input.GetKeyDown(KeyCode.X)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabX == null) return;
        if (Time.time < nextSkillXTime) return;

        int playerLevel = GetPlayerLevel();
        if (playerLevel < weapon.RequiredLevelX)
        {
            Debug.Log($"[PlayerCombat] Nivel {weapon.RequiredLevelX} requerido para Skill X. Actual: {playerLevel}");
            return;
        }

        nextSkillXTime = Time.time + weapon.CooldownX;

        animator.SetTrigger("AttackX");
        SpawnAttack(weapon.PrefabX);
    }

    private void HandleSkillC()
    {
        if (!Input.GetKeyDown(KeyCode.C)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabC == null) return;
        if (Time.time < nextSkillCTime) return;

        int playerLevel = GetPlayerLevel();
        if (playerLevel < weapon.RequiredLevelC)
        {
            Debug.Log($"[PlayerCombat] Nivel {weapon.RequiredLevelC} requerido para Skill C. Actual: {playerLevel}");
            return;
        }

        nextSkillCTime = Time.time + weapon.CooldownC;

        animator.SetTrigger("AttackC");
        SpawnAttack(weapon.PrefabC);
    }

    private void HandleSkillV()
    {
        if (!Input.GetKeyDown(KeyCode.V)) return;

        WeaponData weapon = weaponSlotManager.CurrentWeapon;
        if (weapon == null) return;
        if (weapon.PrefabV == null) return;
        if (Time.time < nextSkillVTime) return;

        int weaponLevel = weaponSlotManager.CurrentWeaponLevel;
        if (weaponLevel < weapon.RequiredLevelV)
        {
            Debug.Log($"[PlayerCombat] Nivel de arma {weapon.RequiredLevelV} requerido para Ultimate V. Actual: {weaponLevel}");
            return;
        }

        nextSkillVTime = Time.time + weapon.CooldownV;

        animator.SetTrigger("AttackV");
        SpawnAttack(weapon.PrefabV);
    }

    private void SpawnAttack(GameObject prefab)
    {
        if (prefab == null) return;

        Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position;
        Quaternion spawnRot = GetFacingRotation();

        Instantiate(prefab, spawnPos, spawnRot);
    }

    private Quaternion GetFacingRotation()
    {
        bool facingLeft = playerSprite != null && playerSprite.flipX;

        float angle = facingLeft ? 180f : 0f;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }

    private int GetPlayerLevel()
    {
        ExperienceSystem xp = GetComponent<ExperienceSystem>();
        return xp != null ? xp.level : 1;
    }
}
