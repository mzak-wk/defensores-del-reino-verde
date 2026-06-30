using UnityEngine;
using System;

[RequireComponent(typeof(Collider2D))]
public class Enemy : MonoBehaviour
{
    public float maxHP = 50f;
    public float moveSpeed = 1.5f;
    public int xpReward = 5;
    public float contactDamage = 5f;

    float hp;
    public Action<Enemy> OnDeath;

    // NUEVO: si true, al morir NO se invoca OnDeath ni drops
    [HideInInspector]
    public bool suppressDrops = false;

    [HideInInspector] public EnemyData data;
    [HideInInspector] public SpawnZone originZone;

    protected virtual void OnEnable() => hp = maxHP;

    public void Init(EnemyData data)
    {
        this.data = data;
        Init(data.maxHP, data.moveSpeed, data.xpReward, data.contactDamage);
    }

    public void Init(float hp, float speed, int xp, float dmg)
    {
        maxHP = hp; moveSpeed = speed; xpReward = xp; contactDamage = dmg;
        this.hp = maxHP;
    }

    public void TakeDamage(float amount)
    {
        hp -= amount;
        Debug.Log($"[Enemy] {name} HP = {hp} (-{amount})");
        if (hp <= 0f) Die();
    }

    protected virtual void Die()
    {
        if (suppressDrops)
        {
            // Destruye sin invocar OnDeath (evitamos drops / handlers)
            ReturnToPoolOrDestroy();
            return;
        }

        OnDeath?.Invoke(this);
        ReturnToPoolOrDestroy();
    }

    // NUEVO: función segura para destruir sin invocar OnDeath (usa desde EnemySpawner)
    public void SuppressDropsAndDestroy()
    {
        suppressDrops = true;
        ReturnToPoolOrDestroy();
    }

    private void ReturnToPoolOrDestroy()
    {
        if (EnemyPool.Instance != null)
        {
            EnemyPool.Instance.Return(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ResetForPool()
    {
        hp = maxHP;
        suppressDrops = false;
    }
    
}
