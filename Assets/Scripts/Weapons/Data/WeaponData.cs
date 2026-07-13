using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct BaseStats
{
    [Min(0f)] public float damage;
    [Min(0.01f)] public float cooldown;
    [Min(0)] public int projectileCount;
    [Min(0f)] public float projectileSpeed;
    [Min(0f)] public float areaSize;
    [Min(0f)] public float duration;
    [Min(0)] public int pierce;
    [Min(0f)] public float knockback;
    [Range(0f, 1f)] public float criticalChance;
    [Min(1f)] public float criticalMultiplier;

    public static BaseStats Default => new BaseStats
    {
        damage = 10f,
        cooldown = 1f,
        projectileCount = 1,
        projectileSpeed = 5f,
        areaSize = 1f,
        duration = 1f,
        pierce = 0,
        knockback = 0f,
        criticalChance = 0f,
        criticalMultiplier = 1.5f
    };
}

[CreateAssetMenu(fileName = "New Weapon Data", menuName = "Game/Weapons/Weapon Data")]
public class WeaponData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private string weaponId;
    [SerializeField] private string displayName;
    [TextArea(2, 5)]
    [SerializeField] private string description;
    [SerializeField] private Sprite icon;

    [Header("Classification")]
    [SerializeField] private RarityData rarity;
    [SerializeField] private List<string> upgradeTags = new List<string>();

    [Header("Runtime References")]
    [Tooltip("Prefab that will contain the future weapon behaviour for this weapon.")]
    [SerializeField] private GameObject weaponPrefab;
    [Tooltip("Optional identifier for the future runtime behaviour resolver.")]
    [SerializeField] private string weaponBehaviourType;
    [SerializeField] private RuntimeAnimatorController weaponAnimator;

    [Header("Base Attack (Left Click / Auto)")]
    [SerializeField] private bool isRanged;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private BaseStats baseStats = BaseStats.Default;

    [Header("Skill X (Unlocks at Player Level 5)")]
    [SerializeField] private float damageX = 20f;
    [SerializeField] private float cooldownX = 5f;
    [SerializeField] private float rangeX = 3f;
    [SerializeField] private GameObject prefabX;

    [Header("Skill C (Unlocks at Player Level 10)")]
    [SerializeField] private float damageC = 40f;
    [SerializeField] private float cooldownC = 10f;
    [SerializeField] private float rangeC = 4f;
    [SerializeField] private GameObject prefabC;

    [Header("Ultimate V (Unlocks at Max Weapon Level)")]
    [SerializeField] private float damageV = 100f;
    [SerializeField] private float cooldownV = 30f;
    [SerializeField] private float rangeV = 6f;
    [SerializeField] private GameObject prefabV;

    [Header("Upgrade Pools")]
    [SerializeField] private List<UpgradeData> uniqueUpgradePool = new List<UpgradeData>();
    [SerializeField] private List<UpgradeData> passiveUpgradePool = new List<UpgradeData>();

    [Header("Progression")]
    [Min(1)]
    [SerializeField] private int maxInternalLevel = 5;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public RarityData Rarity => rarity;
    public IReadOnlyList<string> UpgradeTags => upgradeTags;
    
    public GameObject WeaponPrefab => weaponPrefab;
    public string WeaponBehaviourType => weaponBehaviourType;
    public RuntimeAnimatorController WeaponAnimator => weaponAnimator;

    public bool IsRanged => isRanged;
    public float AttackRange => attackRange;
    public GameObject ProjectilePrefab => projectilePrefab;
    public BaseStats Stats => baseStats;

    public float DamageX => damageX;
    public float CooldownX => cooldownX;
    public float RangeX => rangeX;
    public GameObject PrefabX => prefabX;

    public float DamageC => damageC;
    public float CooldownC => cooldownC;
    public float RangeC => rangeC;
    public GameObject PrefabC => prefabC;

    public float DamageV => damageV;
    public float CooldownV => cooldownV;
    public float RangeV => rangeV;
    public GameObject PrefabV => prefabV;

    public IReadOnlyList<UpgradeData> UniqueUpgradePool => uniqueUpgradePool;
    public IReadOnlyList<UpgradeData> PassiveUpgradePool => passiveUpgradePool;
    public int MaxInternalLevel => maxInternalLevel;

    public bool HasUpgradeTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && upgradeTags.Contains(tag);
    }
}
