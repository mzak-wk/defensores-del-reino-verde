using UnityEngine;

/// <summary>
/// Contiene toda la información serializable que se guardará en los archivos JSON.
/// Esta clase es un simple contenedor de datos ("Data Transfer Object").
/// </summary>
[System.Serializable]
public class SaveData
{
    // =====================================
    // Datos del Jugador
    // =====================================
    public float playerMaxHP;
    public float playerCurrentHP;
    public int gold;

    public int playerLevel;
    public float playerXP;
    public float playerXpToNext;

    public string equippedWeaponId;
    public int equippedWeaponIndex;

    // =====================================
    // Estado del HUB / Eventos de Inicio
    // =====================================
    public bool hasCompletedIntro;
    public int highestLevelUnlocked;
    public System.Collections.Generic.List<string> unlockedWeapons;

    // =====================================
    // Ítems
    // =====================================
    public System.Collections.Generic.List<string> unlockedItems;
    public string[] equippedItems;

    // =====================================
    // Ítems - Niveles
    // =====================================
    public System.Collections.Generic.List<string> itemLevelKeys;
    public System.Collections.Generic.List<int> itemLevelValues;

    // =====================================
    // Upgrades (Niveles de la tienda)
    // =====================================
    public int hpLevel;
    public int speedLevel;
    public int resistLevel;

    /// <summary>
    /// Constructor por defecto (necesario para la deserialización).
    /// </summary>
    public SaveData() { }

    /// <summary>
    /// Constructor que inicializa los datos desde la GameSession actual.
    /// </summary>
    public SaveData(GameSession session)
    {
        playerMaxHP = session.playerMaxHP;
        playerCurrentHP = session.playerCurrentHP;
        gold = session.gold;

        playerLevel = session.playerLevel;
        playerXP = session.playerXP;
        playerXpToNext = session.playerXpToNext;

        equippedWeaponId = session.equippedWeaponId;
        equippedWeaponIndex = session.equippedWeaponIndex;

        hasCompletedIntro = session.hasCompletedIntro;
        highestLevelUnlocked = session.highestLevelUnlocked;
        unlockedWeapons = new System.Collections.Generic.List<string>(session.unlockedWeapons);

        unlockedItems = new System.Collections.Generic.List<string>(session.unlockedItems);
        equippedItems = session.equippedItems != null
            ? new string[3] { session.equippedItems[0], session.equippedItems[1], session.equippedItems[2] }
            : new string[3];

        itemLevelKeys = new System.Collections.Generic.List<string>(session.itemLevelKeys);
        itemLevelValues = new System.Collections.Generic.List<int>(session.itemLevelValues);

        hpLevel = session.hpLevel;
        speedLevel = session.speedLevel;
        resistLevel = session.resistLevel;
    }
}
