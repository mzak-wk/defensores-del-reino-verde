using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Defenders/Enemy Data", order = 1)]
public class EnemyData : ScriptableObject
{
    public string enemyName = "Enemy";
    public GameObject prefab;
    
    [Header("Stats Fijos")]
    public float maxHP = 50f;
    public float moveSpeed = 1.5f;
    public float contactDamage = 5f;
    public int xpReward = 5;
    
    [Header("Loot Extra (Opcional)")]
    public int coinReward = 1;
    
    [Header("UI (Opcional)")]
    public Sprite icon;
}
