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
    [SerializeField] private bool isUnlockedByDefault = false;
    [SerializeField] private RarityData rarity;
    [SerializeField] private List<string> upgradeTags = new List<string>();

    [Header("Animator")]
    [SerializeField] private AnimatorOverrideController weaponAnimator;

    [Header("Base Attack")]
    [SerializeField] private bool isRanged;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private GameObject basicAttackPrefab;
    [SerializeField] private BaseStats baseStats = BaseStats.Default;
    [SerializeField] private float basicCooldown = 0.5f;

    [Header("Skill X")]
    [SerializeField] private string skillXName = "Skill X";
    [SerializeField] private float damageX = 20f;
    [SerializeField] private float cooldownX = 5f;
    [SerializeField] private float rangeX = 3f;
    [SerializeField] private int requiredLevelX = 5;
    [SerializeField] private GameObject prefabX;

    [Header("Skill C")]
    [SerializeField] private string skillCName = "Skill C";
    [SerializeField] private float damageC = 40f;
    [SerializeField] private float cooldownC = 10f;
    [SerializeField] private float rangeC = 4f;
    [SerializeField] private int requiredLevelC = 10;
    [SerializeField] private GameObject prefabC;

    [Header("Ultimate V")]
    [SerializeField] private string skillVName = "Ultimate V";
    [SerializeField] private float damageV = 100f;
    [SerializeField] private float cooldownV = 30f;
    [SerializeField] private float rangeV = 6f;
    [SerializeField] private int requiredLevelV = 5;
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
    public bool IsUnlockedByDefault => isUnlockedByDefault;
    public RarityData Rarity => rarity;
    public IReadOnlyList<string> UpgradeTags => upgradeTags;

    public AnimatorOverrideController WeaponAnimator => weaponAnimator;

    public bool IsRanged => isRanged;
    public float AttackRange => attackRange;
    public GameObject BasicAttackPrefab => basicAttackPrefab;
    public BaseStats Stats => baseStats;
    public float BasicCooldown => basicCooldown;

    public string SkillXName => skillXName;
    public float DamageX => damageX;
    public float CooldownX => cooldownX;
    public float RangeX => rangeX;
    public int RequiredLevelX => requiredLevelX;
    public GameObject PrefabX => prefabX;

    public string SkillCName => skillCName;
    public float DamageC => damageC;
    public float CooldownC => cooldownC;
    public float RangeC => rangeC;
    public int RequiredLevelC => requiredLevelC;
    public GameObject PrefabC => prefabC;

    public string SkillVName => skillVName;
    public float DamageV => damageV;
    public float CooldownV => cooldownV;
    public float RangeV => rangeV;
    public int RequiredLevelV => requiredLevelV;
    public GameObject PrefabV => prefabV;

    public IReadOnlyList<UpgradeData> UniqueUpgradePool => uniqueUpgradePool;
    public IReadOnlyList<UpgradeData> PassiveUpgradePool => passiveUpgradePool;
    public int MaxInternalLevel => maxInternalLevel;

    public bool HasUpgradeTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && upgradeTags.Contains(tag);
    }
}
