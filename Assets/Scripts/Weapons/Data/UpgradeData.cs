using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeKind
{
    StatModifier,
    PassiveEffect,
    WeaponEvolution,
    SpecialTrigger
}

public enum UpgradeTargetScope
{
    SpecificWeapon,
    AnyActiveWeapon,
    AllWeapons,
    PlayerPassive
}

public enum WeaponStatType
{
    Damage,
    Cooldown,
    ProjectileCount,
    ProjectileSpeed,
    AreaSize,
    Duration,
    Pierce,
    Knockback,
    CriticalChance,
    CriticalMultiplier
}

public enum StatModifierMode
{
    AddFlat,
    AddPercent,
    Multiply
}

[Serializable]
public struct StatModifierData
{
    public WeaponStatType statType;
    public StatModifierMode modifierMode;
    public float value;
}

[CreateAssetMenu(fileName = "New Upgrade Data", menuName = "Game/Weapons/Upgrade Data")]
public class UpgradeData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string upgradeId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string descriptionTemplate;
    [SerializeField] private Sprite icon;

    [Header("Rules")]
    [SerializeField] private UpgradeKind kind = UpgradeKind.StatModifier;
    [SerializeField] private UpgradeTargetScope targetScope = UpgradeTargetScope.AnyActiveWeapon;
    [SerializeField] private RarityData minimumRarity;
    [SerializeField] private WeaponData targetSpecificWeapon;
    [SerializeField] private List<string> targetWeaponTags = new List<string>();

    [Header("Selection")]
    [Min(0f)]
    [SerializeField] private float weight = 1f;
    [Min(1)]
    [SerializeField] private int maxStacks = 1;

    [Header("Effects")]
    [SerializeField] private List<StatModifierData> statModifiers = new List<StatModifierData>();

    [Header("Compatibility")]
    [SerializeField] private List<UpgradeData> requiredUpgrades = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> incompatibleUpgrades = new List<UpgradeData>();

    public string UpgradeId => upgradeId;
    public string DisplayName => displayName;
    public string DescriptionTemplate => descriptionTemplate;
    public Sprite Icon => icon;
    public UpgradeKind Kind => kind;
    public UpgradeTargetScope TargetScope => targetScope;
    public RarityData MinimumRarity => minimumRarity;
    public WeaponData TargetSpecificWeapon => targetSpecificWeapon;
    public IReadOnlyList<string> TargetWeaponTags => targetWeaponTags;
    public float Weight => weight;
    public int MaxStacks => maxStacks;
    public IReadOnlyList<StatModifierData> StatModifiers => statModifiers;
    public IReadOnlyList<UpgradeData> RequiredUpgrades => requiredUpgrades;
    public IReadOnlyList<UpgradeData> IncompatibleUpgrades => incompatibleUpgrades;
}
