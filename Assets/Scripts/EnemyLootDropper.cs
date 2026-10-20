using UnityEngine;
using System;

[RequireComponent(typeof(Enemy))]
public class EnemyLootDropper : MonoBehaviour
{
    [Header("XP Orbes")]
    [Range(0f, 1f)] public float xpChance = 0.8f;   // prob. de soltar XP (0–1)
    public GameObject xpOrbPrefab;
    public int xpPerOrb = 5;

    [Header("Monedas")]
    [Range(0f, 1f)] public float coinChance = 0.70f;
    public GameObject coinPrefab;
    public int coinValue = 1;
    public int minCoins = 1;
    public int maxCoins = 2;

    [Header("Pollo (curación)")]
    [Range(0f, 1f)] public float chickenChance = 0.15f;
    public GameObject chickenPrefab;
    public float chickenHeal = 30f;

    [Header("Hollín (Nivel de Hielo)")]
    [Range(0f, 1f)] public float sootChance = 0f;
    public GameObject sootPrefab;

    [Header("Dispersión")]
    public float scatterRadius = 0.6f;

    Enemy enemy;

    // 🔒 para evitar drops dobles
    bool hasDropped = false;

    void Awake()
    {
        enemy = GetComponent<Enemy>();

        if (coinPrefab == null)
            coinPrefab = Resources.Load<GameObject>("Coin");

        // por si acaso ya estaba suscrito
        enemy.OnDeath -= HandleDeath;
        enemy.OnDeath += HandleDeath;
    }

    void OnDestroy()
    {
        if (enemy != null)
            enemy.OnDeath -= HandleDeath;
    }

    void HandleDeath(Enemy e)
    {
        // ⛔ si ya soltó loot, no volver a hacerlo
        if (hasDropped) return;
        hasDropped = true;

        if (coinPrefab == null)
            coinPrefab = Resources.Load<GameObject>("Coin");

        // ===== XP =====
        if (xpOrbPrefab && UnityEngine.Random.value <= xpChance)
        {
            SpawnXPOrb();
        }

        // ===== Monedas (LutroCoins) Balanceadas =====
        DropCoins();

        // ===== Pollo =====
        if (chickenPrefab && UnityEngine.Random.value <= chickenChance)
        {
            var go = Instantiate(chickenPrefab, RandPos(), Quaternion.identity);
            var pick = go.GetComponent<ChickenPickup>();
            if (pick) pick.healAmount = chickenHeal;
        }

        // ===== Hollín =====
        if (sootPrefab && UnityEngine.Random.value <= sootChance)
        {
            Instantiate(sootPrefab, transform.position, Quaternion.identity);
        }
    }

    void DropCoins()
    {
        if (coinPrefab == null) return;

        bool isBoss = (enemy != null && (enemy.maxHP >= 800f || GetComponent<BossAIController>() != null));
        bool isElite = (enemy != null && !isBoss && enemy.maxHP >= 180f);

        float dropProbability = (isBoss || isElite) ? 1.0f : coinChance;
        if (UnityEngine.Random.value > dropProbability) return;

        int numCoinsToSpawn = 1;
        int valuePerCoin = 1;

        if (isBoss)
        {
            numCoinsToSpawn = UnityEngine.Random.Range(8, 14);
            valuePerCoin = 2; // ~16 a 28 monedas
        }
        else if (isElite)
        {
            numCoinsToSpawn = UnityEngine.Random.Range(3, 6); // 3 a 5 monedas
            valuePerCoin = 1;
        }
        else
        {
            // Enemigo regular: 1 a 2 monedas
            numCoinsToSpawn = UnityEngine.Random.Range(minCoins, maxCoins + 1);
            valuePerCoin = (enemy != null && enemy.data != null && enemy.data.coinReward > 0)
                ? enemy.data.coinReward
                : coinValue;
        }

        // Aplicar bonus porcentual de ítems equipados
        float bonusPercent = GameSession.Instance ? GameSession.Instance.GetTotalLutroCoinBonusPercent() : 0f;
        if (bonusPercent > 0f)
        {
            float mult = 1f + (bonusPercent / 100f);
            valuePerCoin = Mathf.Max(1, Mathf.RoundToInt(valuePerCoin * mult));
        }

        for (int i = 0; i < numCoinsToSpawn; i++)
        {
            var go = Instantiate(coinPrefab, RandPos(), Quaternion.identity);
            var pick = go.GetComponent<CoinPickup>();
            if (pick) pick.amount = valuePerCoin;
        }
    }

    void SpawnXPOrb()
    {
        var go = Instantiate(xpOrbPrefab, RandPos(), Quaternion.identity);
        var pick = go.GetComponent<XPPickup>();
        if (pick) pick.amount = xpPerOrb;
    }

    Vector2 RandPos()
    {
        return (Vector2)transform.position + UnityEngine.Random.insideUnitCircle * scatterRadius;
    }
}
