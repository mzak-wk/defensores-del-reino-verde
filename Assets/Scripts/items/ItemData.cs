using UnityEngine;

public enum ItemRarity { Comun, Raro, Epico, Legendario }

[CreateAssetMenu(fileName = "New Item Data", menuName = "Game/Items/Item Data")]
public class ItemData : ScriptableObject
{
    [Header("Identificación")]
    [SerializeField] private string itemId;
    [SerializeField] private string itemName;
    [SerializeField] private Sprite icon;
    [SerializeField] private ItemRarity rarity;

    [Header("Descripción")]
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [TextArea(1, 3)]
    [SerializeField] private string passiveConcept;

    [Header("Estadísticas / Efectos Pasivos")]
    [SerializeField] private float maxHpBonusPercent;
    [SerializeField] private float autoAttackCooldownReduction;
    [SerializeField] private float damageBonusPercent;
    [SerializeField] private float projectileSizeBonusPercent;
    [SerializeField] private float defenseBonusPercent;
    [SerializeField] private float lutroCoinBonusPercent;
    [SerializeField] private float activeSkillCooldownReduction;
    [SerializeField] private float pickupRangeBonusPercent;
    [SerializeField] private float dashDamageBonusNextAttack;
    [SerializeField] private int extraDashes;
    [SerializeField] private bool leavesDashTrail;
    [SerializeField] private bool specialHitsTwice;

    [Header("Ítems Únicos / Activos (Legendarios)")]
    [SerializeField] private bool isUniquePurchase;
    [SerializeField] private bool isExclusiveGroup;
    [SerializeField] private KeyCode activeAbilityKey = KeyCode.None;
    [SerializeField] private float activeAbilityDuration;

    public string ItemId => itemId;
    public string ItemName => itemName;
    public Sprite Icon => icon;
    public ItemRarity Rarity => rarity;
    public string Description => description;
    public string PassiveConcept => passiveConcept;

    public float MaxHpBonusPercent => maxHpBonusPercent;
    public float AutoAttackCooldownReduction => autoAttackCooldownReduction;
    public float DamageBonusPercent => damageBonusPercent;
    public float ProjectileSizeBonusPercent => projectileSizeBonusPercent;
    public float DefenseBonusPercent => defenseBonusPercent;
    public float LutroCoinBonusPercent => lutroCoinBonusPercent;
    public float ActiveSkillCooldownReduction => activeSkillCooldownReduction;
    public float PickupRangeBonusPercent => pickupRangeBonusPercent;
    public float DashDamageBonusNextAttack => dashDamageBonusNextAttack;
    public int ExtraDashes => extraDashes;
    public bool LeavesDashTrail => leavesDashTrail;
    public bool SpecialHitsTwice => specialHitsTwice;

    public bool IsUniquePurchase => isUniquePurchase;
    public bool IsExclusiveGroup => isExclusiveGroup;
    public KeyCode ActiveAbilityKey => activeAbilityKey;
    public float ActiveAbilityDuration => activeAbilityDuration;

    public const float DEFAULT_DIMINISHING_DECAY = 0.70f;

    /// <summary>
    /// Multiplicador acumulado con rendimientos decrecientes (Diminishing Returns).
    /// Nivel 1 = 1.0f
    /// Nivel 2 = 1.70f (+70% extra)
    /// Nivel 3 = 2.19f (+49% extra)
    /// Nivel 4 = 2.53f (+34% extra)
    /// Nivel 5 = 2.77f (+24% extra)
    /// </summary>
    public static float GetDiminishingMultiplier(int level, float decayRate = DEFAULT_DIMINISHING_DECAY)
    {
        if (level <= 0) return 0f;
        if (level == 1) return 1f;

        float totalMultiplier = 0f;
        float currentIncrement = 1f;

        for (int i = 0; i < level; i++)
        {
            totalMultiplier += currentIncrement;
            currentIncrement *= decayRate;
        }

        return totalMultiplier;
    }

    /// <summary>
    /// Retorna cuánto incremento relativo dará subir de currentLevel a (currentLevel + 1).
    /// </summary>
    public static float GetNextLevelIncrementMultiplier(int currentLevel, float decayRate = DEFAULT_DIMINISHING_DECAY)
    {
        if (currentLevel <= 0) return 1f;
        return Mathf.Pow(decayRate, currentLevel);
    }

    public float GetEffectiveMaxHpBonus(int level) => maxHpBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectiveDamageBonus(int level) => damageBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectiveAutoAttackCDR(int level) => autoAttackCooldownReduction * GetDiminishingMultiplier(level);
    public float GetEffectiveActiveSkillCDR(int level) => activeSkillCooldownReduction * GetDiminishingMultiplier(level);
    public float GetEffectiveProjectileSizeBonus(int level) => projectileSizeBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectiveDefenseBonus(int level) => defenseBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectiveLutroCoinBonus(int level) => lutroCoinBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectivePickupRangeBonus(int level) => pickupRangeBonusPercent * GetDiminishingMultiplier(level);
    public float GetEffectiveDashDamageBonus(int level) => dashDamageBonusNextAttack * GetDiminishingMultiplier(level);
    public int GetEffectiveExtraDashes(int level) => Mathf.Max(0, Mathf.FloorToInt(extraDashes * (level > 0 ? 1 + (level - 1) * 0.5f : 0)));

    public bool HasPositiveStats()
    {
        return maxHpBonusPercent > 0f
            || autoAttackCooldownReduction > 0f
            || damageBonusPercent > 0f
            || projectileSizeBonusPercent > 0f
            || defenseBonusPercent > 0f
            || lutroCoinBonusPercent > 0f
            || activeSkillCooldownReduction > 0f
            || pickupRangeBonusPercent > 0f
            || dashDamageBonusNextAttack > 0f
            || extraDashes > 0
            || leavesDashTrail
            || specialHitsTwice;
    }

    public string GetStatsSummary()
    {
        return GetDetailedDescription(1, -1);
    }

    public string GetDetailedDescription(int currentLevel, int nextLevel = -1)
    {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        void AppendStat(string name, float baseVal, string suffix = "%", bool isReduction = false)
        {
            if (baseVal <= 0f) return;

            string sign = isReduction ? "-" : "+";
            if (currentLevel <= 0)
            {
                // Nuevo ítem -> mostrará su valor en nivel 1
                sb.AppendLine($"  {sign}{baseVal:0.#}{suffix} {name}");
            }
            else if (nextLevel > currentLevel)
            {
                float currentVal = baseVal * GetDiminishingMultiplier(currentLevel);
                float nextVal = baseVal * GetDiminishingMultiplier(nextLevel);
                float diff = nextVal - currentVal;
                sb.AppendLine($"  {name}: {sign}{currentVal:0.#}{suffix} → {sign}{nextVal:0.#}{suffix} ({sign}{diff:0.#}{suffix})");
            }
            else
            {
                float currentVal = baseVal * GetDiminishingMultiplier(currentLevel);
                sb.AppendLine($"  {sign}{currentVal:0.#}{suffix} {name}");
            }
        }

        AppendStat("Vida Máxima", maxHpBonusPercent);
        AppendStat("Cooldown Ataque Auto", autoAttackCooldownReduction, "%", true);
        AppendStat("Daño", damageBonusPercent);
        AppendStat("Tamaño Proyectil", projectileSizeBonusPercent);
        AppendStat("Defensa", defenseBonusPercent);
        AppendStat("Monedas Lutro", lutroCoinBonusPercent);
        AppendStat("Cooldown Habilidad Activa", activeSkillCooldownReduction, "%", true);
        AppendStat("Rango Recogida", pickupRangeBonusPercent);
        AppendStat("Daño tras Dash", dashDamageBonusNextAttack, "");

        if (extraDashes > 0)
        {
            int curD = GetEffectiveExtraDashes(currentLevel);
            if (nextLevel > currentLevel)
            {
                int nextD = GetEffectiveExtraDashes(nextLevel);
                sb.AppendLine($"  Dash(s) Extra(s): +{curD} → +{nextD}");
            }
            else
            {
                sb.AppendLine($"  +{curD} Dash(s) Extra(s)");
            }
        }

        if (leavesDashTrail) sb.AppendLine("  Deja estela al hacer Dash");
        if (specialHitsTwice) sb.AppendLine("  Golpe especial impacta dos veces");

        if (sb.Length == 0) return "Sin efectos pasivos.";
        return sb.ToString().TrimEnd();
    }
}
