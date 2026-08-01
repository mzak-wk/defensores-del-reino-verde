using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Weapon Database", menuName = "Game/Weapons/Weapon Database")]
public class WeaponDatabase : ScriptableObject
{
    private const int ExpectedTotalWeapons = 6;
    private const int ExpectedWeaponsPerRarity = 2;

    [SerializeField] private List<WeaponData> weapons = new List<WeaponData>();

    public IReadOnlyList<WeaponData> Weapons => weapons;

    /// <summary>
    /// Busca y devuelve el WeaponData correspondiente al ID proporcionado.
    /// Útil para restaurar el arma equipada desde GameSession al cargar una escena.
    /// </summary>
    /// <param name="weaponId">El identificador único del arma (ej. "sword_basic")</param>
    /// <returns>El WeaponData si se encuentra, o null si no existe.</returns>
    public WeaponData GetWeaponById(string weaponId)
    {
        if (string.IsNullOrEmpty(weaponId)) return null;

        foreach (var weapon in weapons)
        {
            if (weapon != null && weapon.WeaponId == weaponId)
            {
                return weapon;
            }
        }
        
        Debug.LogWarning($"[WeaponDatabase] No se encontró ningún arma con el ID: {weaponId}");
        return null;
    }

    private void OnValidate()
    {
        ValidateWeaponMatrix();
    }

    public bool ValidateWeaponMatrix()
    {
        bool isValid = true;

        if (weapons == null)
        {
            Debug.LogError("[WeaponDatabase] Weapon list is null.", this);
            return false;
        }

        if (weapons.Count != ExpectedTotalWeapons)
        {
            Debug.LogError($"[WeaponDatabase] Expected exactly {ExpectedTotalWeapons} weapons, but found {weapons.Count}.", this);
            isValid = false;
        }

        Dictionary<WeaponRarityId, int> rarityCounts = CreateEmptyRarityCounts();

        for (int i = 0; i < weapons.Count; i++)
        {
            WeaponData weapon = weapons[i];

            if (weapon == null)
            {
                Debug.LogError($"[WeaponDatabase] Weapon slot {i} is empty.", this);
                isValid = false;
                continue;
            }

            if (weapon.Rarity == null)
            {
                Debug.LogError($"[WeaponDatabase] Weapon '{weapon.name}' has no RarityData assigned.", weapon);
                isValid = false;
                continue;
            }

            rarityCounts[weapon.Rarity.Id]++;
        }

        foreach (KeyValuePair<WeaponRarityId, int> pair in rarityCounts)
        {
            if (pair.Value != ExpectedWeaponsPerRarity)
            {
                Debug.LogError($"[WeaponDatabase] Expected {ExpectedWeaponsPerRarity} {pair.Key} weapons, but found {pair.Value}.", this);
                isValid = false;
            }
        }

        return isValid;
    }

    private static Dictionary<WeaponRarityId, int> CreateEmptyRarityCounts()
    {
        return new Dictionary<WeaponRarityId, int>
        {
            { WeaponRarityId.Common, 0 },
            { WeaponRarityId.Rare, 0 },
            { WeaponRarityId.Epic, 0 }
        };
    }
}
