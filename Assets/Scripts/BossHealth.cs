using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class BossHealth : Enemy
{
    [Header("Eventos")]
    [Tooltip("Evento invocado cuando el jefe es derrotado.")]
    new public UnityEvent OnDeath;

    public event Action<float> OnDamaged;
    public event Action OnBossDeath;

    [Header("Configuración de Drops")]
    public bool suppressNormalDropsOnBossDeath = true;

    public float CurrentHP => currentHP;
    public float MaxHP => maxHP;
    public bool IsDead => currentHP <= 0f;

    private float currentHP;

    protected override void OnEnable()
    {
        base.OnEnable();
        currentHP = maxHP;
    }

    public void SetHP(float newMaxHP)
    {
        maxHP = newMaxHP;
        currentHP = maxHP;
    }

    public new void TakeDamage(float amount)
    {
        if (currentHP <= 0f) return;

        currentHP -= amount;
        OnDamaged?.Invoke(amount);

        Debug.Log($"[BossHealth] {name} HP = {currentHP} (-{amount})");
        if (currentHP <= 0f) Die();
    }

    protected override void Die()
    {
        OnDeath?.Invoke();
        OnBossDeath?.Invoke();

        NotifyTutorialDirector();

        if (suppressNormalDropsOnBossDeath)
        {
            suppressDrops = true;
        }

        base.Die();
    }

    void NotifyTutorialDirector()
    {
        TutorialDirector director = FindFirstObjectByType<TutorialDirector>();
        if (director != null)
        {
            director.OnBossDefeated();
        }
    }
}
