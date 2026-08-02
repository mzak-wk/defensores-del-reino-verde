using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BossHealthBarUI : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image healthFill;
    [SerializeField] private Image healthDelay;
    [SerializeField] private TMP_Text bossNameText;

    [Header("Configuración")]
    [SerializeField] private float chipSpeed = 2f;

    private BossHealth boundBoss;
    private float maxHP;
    private bool isShowing;

    public event Action OnBossHealthBarHidden;

    void Update()
    {
        if (!isShowing || boundBoss == null) return;

        float t = maxHP > 0f ? Mathf.Clamp01(boundBoss.CurrentHP / maxHP) : 0f;

        if (healthFill != null)
            healthFill.fillAmount = t;

        if (healthDelay != null)
            healthDelay.fillAmount = Mathf.MoveTowards(healthDelay.fillAmount, t, chipSpeed * Time.unscaledDeltaTime);
    }

    public void Show(BossHealth bossHealth, string bossName)
    {
        if (bossHealth == null)
        {
            Debug.LogWarning("[BossHealthBarUI] BossHealth is null, cannot show.");
            return;
        }

        boundBoss = bossHealth;
        maxHP = bossHealth.MaxHP;
        isShowing = true;

        if (bossNameText != null)
            bossNameText.text = bossName;

        float t = maxHP > 0f ? boundBoss.CurrentHP / maxHP : 0f;
        if (healthFill != null) healthFill.fillAmount = t;
        if (healthDelay != null) healthDelay.fillAmount = t;

        boundBoss.OnDamaged += HandleDamaged;
        boundBoss.OnBossDeath += HandleDeath;

        if (root != null)
            root.SetActive(true);
    }

    public void Hide()
    {
        isShowing = false;

        if (boundBoss != null)
        {
            boundBoss.OnDamaged -= HandleDamaged;
            boundBoss.OnBossDeath -= HandleDeath;
            boundBoss = null;
        }

        if (root != null)
            root.SetActive(false);

        OnBossHealthBarHidden?.Invoke();
    }

    private void HandleDamaged(float amount)
    {
        // Update is handled in Update()
    }

    private void HandleDeath()
    {
        Hide();
    }

    void OnDestroy()
    {
        if (boundBoss != null)
        {
            boundBoss.OnDamaged -= HandleDamaged;
            boundBoss.OnBossDeath -= HandleDeath;
        }
    }
}
