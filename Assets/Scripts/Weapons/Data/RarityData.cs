using UnityEngine;

public enum WeaponRarityId
{
    Common,
    Rare,
    Epic
}

[CreateAssetMenu(fileName = "New Rarity Data", menuName = "Game/Weapons/Rarity Data")]
public class RarityData : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private WeaponRarityId id = WeaponRarityId.Common;
    [SerializeField] private string displayName = "Common";
    [SerializeField] private Color displayColor = Color.white;

    [Header("Power Multipliers")]
    [Min(0f)]
    [SerializeField] private float statPowerMultiplier = 1f;
    [Min(0f)]
    [SerializeField] private float upgradePowerMultiplier = 1f;

    [Header("Appearance Curve")]
    [Tooltip("X = run stage normalized from 0 to 1. Y = relative weight for this rarity.")]
    [SerializeField] private AnimationCurve appearanceWeightCurve = AnimationCurve.Linear(0f, 1f, 1f, 1f);

    public WeaponRarityId Id => id;
    public string DisplayName => displayName;
    public Color DisplayColor => displayColor;
    public float StatPowerMultiplier => statPowerMultiplier;
    public float UpgradePowerMultiplier => upgradePowerMultiplier;

    public float GetAppearanceWeight(float normalizedRunStage)
    {
        float stage = Mathf.Clamp01(normalizedRunStage);
        return Mathf.Max(0f, appearanceWeightCurve.Evaluate(stage));
    }
}
