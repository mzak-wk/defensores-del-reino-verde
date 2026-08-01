using UnityEngine;

[CreateAssetMenu(menuName = "Boss/Boss Stats Override", fileName = "New Boss Stats Override")]
public class BossStatsOverride : ScriptableObject
{
    [Header("Identificación")]
    public string bossName = "Boss";

    [Header("Stats")]
    public float maxHP = 500f;
    public float contactDamage = 10f;
    public float moveSpeed = 3f;

    [Header("UI")]
    public Sprite bossPortrait;

    [Header("Audio")]
    public AudioClip bossMusic;

    [Header("Ataques")]
    public BossAttackData[] attacks;
}
