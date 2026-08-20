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
    [Range(0f, 1f)] public float coinChance = 0.5f;
    public GameObject coinPrefab;
    public int coinValue = 1;

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

        // ===== XP =====
        if (xpOrbPrefab && UnityEngine.Random.value <= xpChance)
        {
            SpawnXPOrb();
        }

        // ===== Monedas =====
        if (coinPrefab && UnityEngine.Random.value <= coinChance)
        {
            var go = Instantiate(coinPrefab, RandPos(), Quaternion.identity);
            var pick = go.GetComponent<CoinPickup>();
            if (pick) pick.amount = coinValue;
        }

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
