using UnityEngine;

public class WeaponInstance : MonoBehaviour
{
    private WeaponData weaponData;
    private int currentWeaponLevel = 1;

    public void Init(WeaponData data, int level)
    {
        weaponData = data;
        currentWeaponLevel = level;
    }

    public void ExecuteBaseAttack(Transform firePoint, Vector2 direction, float damageMultiplier)
    {
        if (weaponData == null) return;
        
        float finalDamage = weaponData.Stats.damage * damageMultiplier;

        if (weaponData.IsRanged)
        {
            if (weaponData.ProjectilePrefab != null)
            {
                // In a full implementation, we'd use an Object Pool or projectile specific init
                GameObject proj = Instantiate(weaponData.ProjectilePrefab, firePoint.position, GetRotationFromDirection(direction));
                // Set damage on projectile
                // ...
            }
        }
        else
        {
            PerformMeleeSweep(firePoint, direction, weaponData.AttackRange, finalDamage);
        }
    }

    public void ExecuteAttackX(Transform firePoint, Vector2 direction, float damageMultiplier)
    {
        if (weaponData == null) return;
        float finalDamage = weaponData.DamageX * damageMultiplier;
        ExecuteSpecialAttack(firePoint, direction, weaponData.PrefabX, weaponData.RangeX, finalDamage);
    }

    public void ExecuteAttackC(Transform firePoint, Vector2 direction, float damageMultiplier)
    {
        if (weaponData == null) return;
        float finalDamage = weaponData.DamageC * damageMultiplier;
        ExecuteSpecialAttack(firePoint, direction, weaponData.PrefabC, weaponData.RangeC, finalDamage);
    }

    public void ExecuteAttackV(Transform firePoint, Vector2 direction, float damageMultiplier)
    {
        if (weaponData == null) return;
        float finalDamage = weaponData.DamageV * damageMultiplier;
        ExecuteSpecialAttack(firePoint, direction, weaponData.PrefabV, weaponData.RangeV, finalDamage);
    }

    private void ExecuteSpecialAttack(Transform firePoint, Vector2 direction, GameObject prefab, float range, float damage)
    {
        if (prefab != null)
        {
            Instantiate(prefab, firePoint.position, GetRotationFromDirection(direction));
            // Initialize prefab with damage if it's a projectile or area effect
        }
        else
        {
            // Fallback to melee sweep if no prefab is assigned but we have a range
            PerformMeleeSweep(firePoint, direction, range, damage);
        }
    }

    private void PerformMeleeSweep(Transform firePoint, Vector2 direction, float range, float damage)
    {
        Vector2 origin = (Vector2)firePoint.position + (direction.normalized * (range / 2f));
        Collider2D[] hits = Physics2D.OverlapCircleAll(origin, range / 2f); // Simple circle sweep
        
        foreach (var hit in hits)
        {
            Enemy enemy = hit.GetComponentInParent<Enemy>();
            if (enemy != null)
            {
                enemy.TakeDamage(damage);
            }
        }
    }

    private Quaternion GetRotationFromDirection(Vector2 dir)
    {
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        return Quaternion.AngleAxis(angle, Vector3.forward);
    }
}
