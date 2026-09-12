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

    [Header("Slash / Melee")]
    [Min(0.01f)] public float slashLifetime;
    [Min(0f)] public float slashForwardDistance;
    [Min(0f)] public float slashInitialScale;
    [Min(0f)] public float slashFinalScale;
    [Range(0f, 1f)] public float slashInitialAlpha;
    [Range(0f, 1f)] public float slashFinalAlpha;
    [Min(0)] public int slashMaxHits;
    public bool slashDestroyOnFirstHit;
    public bool slashAnimateScaleAndFade;
    public bool slashUseUnscaledTime;
    public bool autoSquareHitbox;
    [Min(0f)] public float slashSquareMultiplier;
    [Min(0f)] public float slashPadding;

    [Header("Projectile / Ranged")]
    [Min(0.01f)] public float projectileMaxLifetime;
    public GameObject freezeExplosionPrefab;
    public GameObject areaPrefab;
    [Min(0f)] public float areaDamagePerSecond;
    [Min(0f)] public float areaDuration;
    [Min(0f)] public float areaScaleMultiplier;

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
        criticalMultiplier = 1.5f,
        slashLifetime = 0.35f,
        slashForwardDistance = 1.2f,
        slashInitialScale = 0.5f,
        slashFinalScale = 1.2f,
        slashInitialAlpha = 1f,
        slashFinalAlpha = 0f,
        slashMaxHits = 999,
        slashDestroyOnFirstHit = false,
        slashAnimateScaleAndFade = true,
        slashUseUnscaledTime = false,
        autoSquareHitbox = true,
        slashSquareMultiplier = 1f,
        slashPadding = 0.15f,
        projectileMaxLifetime = 5f,
        freezeExplosionPrefab = null,
        areaPrefab = null,
        areaDamagePerSecond = 0f,
        areaDuration = 5f,
        areaScaleMultiplier = 1.5f
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

    [Header("Base Attack")]
    [SerializeField] private bool isRanged;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private GameObject basicAttackPrefab;
    [SerializeField] private BaseStats baseStats = BaseStats.Default;
    [SerializeField] private float basicCooldown = 0.5f;
    [SerializeField] private Sprite basicIcon;

    [Header("Skill 1")]
    [SerializeField] private string skillXName = "Habilidad 1";
    [SerializeField] private Sprite skill1Icon;
    [SerializeField] private float damageX = 20f;
    [SerializeField] private float cooldownX = 5f;
    [SerializeField] private float rangeX = 3f;
    [SerializeField] private int requiredLevelX = 5;
    [SerializeField] private GameObject prefabX;

    [Header("Skill 2")]
    [SerializeField] private string skillCName = "Habilidad 2";
    [SerializeField] private Sprite skill2Icon;
    [SerializeField] private float damageC = 40f;
    [SerializeField] private float cooldownC = 10f;
    [SerializeField] private float rangeC = 4f;
    [SerializeField] private int requiredLevelC = 10;
    [SerializeField] private GameObject prefabC;

    [Header("Ultimate")]
    [SerializeField] private string skillVName = "Definitiva";
    [SerializeField] private Sprite ultimateIcon;
    [SerializeField] private float damageV = 100f;
    [SerializeField] private float cooldownV = 30f;
    [SerializeField] private float rangeV = 6f;
    [SerializeField] private int requiredLevelV = 5;
    [SerializeField] private GameObject prefabV;

    [Header("Progression")]
    [Min(1)]
    [SerializeField] private int maxInternalLevel = 5;

    [Header("Animation")]
    [SerializeField] private string idleStateName;
    [SerializeField] private int weaponIndex = 0;

    public string WeaponId => weaponId;
    public string DisplayName => displayName;
    public string Description => description;
    public Sprite Icon => icon;
    public RarityData Rarity => rarity;
    public IReadOnlyList<string> UpgradeTags => upgradeTags;

    public bool IsRanged => isRanged;
    public float AttackRange => attackRange;
    public GameObject BasicAttackPrefab => basicAttackPrefab;
    public BaseStats Stats => baseStats;
    public float BasicCooldown => basicCooldown;
    public Sprite BasicIcon => basicIcon != null ? basicIcon : icon;

    public string SkillXName => skillXName;
    public string Skill1Name => skillXName;
    public Sprite Skill1Icon => skill1Icon != null ? skill1Icon : icon;
    public float DamageX => damageX;
    public float CooldownX => cooldownX;
    public float Cooldown1 => cooldownX;
    public float RangeX => rangeX;
    public int RequiredLevelX => requiredLevelX;
    public GameObject PrefabX => prefabX;
    public GameObject Prefab1 => prefabX;

    public string SkillCName => skillCName;
    public string Skill2Name => skillCName;
    public Sprite Skill2Icon => skill2Icon != null ? skill2Icon : icon;
    public float DamageC => damageC;
    public float CooldownC => cooldownC;
    public float Cooldown2 => cooldownC;
    public float RangeC => rangeC;
    public int RequiredLevelC => requiredLevelC;
    public GameObject PrefabC => prefabC;
    public GameObject Prefab2 => prefabC;

    public string SkillVName => skillVName;
    public string UltimateName => skillVName;
    public Sprite UltimateIcon => ultimateIcon != null ? ultimateIcon : icon;
    public float DamageV => damageV;
    public float CooldownV => cooldownV;
    public float CooldownUltimate => cooldownV;
    public float RangeV => rangeV;
    public int RequiredLevelV => requiredLevelV;
    public GameObject PrefabV => prefabV;
    public GameObject PrefabUltimate => prefabV;

    public int MaxInternalLevel => maxInternalLevel;
    public string IdleStateName => idleStateName;
    public int WeaponIndex => weaponIndex;

    public bool HasUpgradeTag(string tag)
    {
        return !string.IsNullOrWhiteSpace(tag) && upgradeTags.Contains(tag);
    }
}
