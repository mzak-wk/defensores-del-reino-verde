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
        System.Text.StringBuilder sb = new System.Text.StringBuilder();

        if (maxHpBonusPercent > 0f) sb.AppendLine($"  +{maxHpBonusPercent}% Vida Máxima");
        if (autoAttackCooldownReduction > 0f) sb.AppendLine($"  -{autoAttackCooldownReduction}% Cooldown Ataque Auto");
        if (damageBonusPercent > 0f) sb.AppendLine($"  +{damageBonusPercent}% Daño");
        if (projectileSizeBonusPercent > 0f) sb.AppendLine($"  +{projectileSizeBonusPercent}% Tamaño Proyectil");
        if (defenseBonusPercent > 0f) sb.AppendLine($"  +{defenseBonusPercent}% Defensa");
        if (lutroCoinBonusPercent > 0f) sb.AppendLine($"  +{lutroCoinBonusPercent}% Monedas Lutro");
        if (activeSkillCooldownReduction > 0f) sb.AppendLine($"  -{activeSkillCooldownReduction}% Cooldown Habilidad Activa");
        if (pickupRangeBonusPercent > 0f) sb.AppendLine($"  +{pickupRangeBonusPercent}% Rango Recogida");
        if (dashDamageBonusNextAttack > 0f) sb.AppendLine($"  +{dashDamageBonusNextAttack} Daño Próximo Ataque tras Dash");
        if (extraDashes > 0) sb.AppendLine($"  +{extraDashes} Dash(s) Extra(s)");
        if (leavesDashTrail) sb.AppendLine("  Deja estela al hacer Dash");
        if (specialHitsTwice) sb.AppendLine("  Golpe especial impacta dos veces");

        if (sb.Length == 0) return "Sin efectos pasivos.";
        return sb.ToString();
    }
}
