using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BarraVida : MonoBehaviour
{
    [Header("Vida")]
    public float maxHP = 100f;
    public float currentHP;

    [Header("Regeneración")]
    public float regenPerSecond = 0.2f;   // base, la sobreescribe GameSession

    [Header("Invulnerabilidad al recibir daño")]
    public float invulnTimeOnHit = 0.3f;   // 300 ms
    public bool invulnerable { get; private set; }

    [Header("UI (World Space)")]
    public Image fill;        // barra principal (instante)
    public Image fillDelay;   // barra lenta (retardo)

    [Header("UI - Velocidad del retardo")]
    public float chipSpeed = 1.5f; // más alto = más rápido

    public Action<float> OnDamaged;  // pasa cantidad
    public Action OnDeath;

    void Awake()
    {
        if (GameSession.Instance != null)
        {
            maxHP = GameSession.Instance.playerMaxHP;
            regenPerSecond = GameSession.Instance.regenPerSecond;

            if (GameSession.Instance.playerCurrentHP <= 0f)
            {
                currentHP = maxHP;
                GameSession.Instance.playerCurrentHP = currentHP;
            }
            else
            {
                currentHP = Mathf.Clamp(GameSession.Instance.playerCurrentHP, 0f, maxHP);
            }
        }
        else
        {
            currentHP = Mathf.Clamp(currentHP, 0f, maxHP);
        }

        if (fill) fill.fillAmount = (maxHP <= 0f) ? 0f : currentHP / maxHP;
        if (fillDelay) fillDelay.fillAmount = (maxHP <= 0f) ? 0f : currentHP / maxHP;
    }

    void Update()
    {
        float t = (maxHP <= 0f) ? 0f : Mathf.Clamp01(currentHP / maxHP);

        if (fill) fill.fillAmount = t;

        if (fillDelay)
            fillDelay.fillAmount = Mathf.MoveTowards(
                fillDelay.fillAmount,
                t,
                chipSpeed * Time.unscaledDeltaTime
            );

        // --- Regeneración de vida ---
        if (regenPerSecond > 0f && currentHP > 0f && currentHP < maxHP)
        {
            float amount = regenPerSecond * Time.deltaTime;
            Curar(amount);
        }
    }

    public void RecibirDanio(float cantidad)
    {
        if (invulnerable || cantidad <= 0f || currentHP <= 0f) return;

        currentHP = Mathf.Max(0f, currentHP - cantidad);

        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;

        OnDamaged?.Invoke(cantidad);

        if (currentHP <= 0f)
        {
            OnDeath?.Invoke();
            return;
        }

        if (invulnTimeOnHit > 0f)
            StartCoroutine(InvulnCR());
    }

    IEnumerator InvulnCR()
    {
        invulnerable = true;
        yield return new WaitForSeconds(invulnTimeOnHit);
        invulnerable = false;
    }

    public void Curar(float cantidad)
    {
        if (currentHP <= 0f) return;
        currentHP = Mathf.Min(maxHP, currentHP + Mathf.Max(0f, cantidad));
        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;
    }

    // Helper editor / debug
    public void FillToMax()
    {
        currentHP = maxHP;
        if (GameSession.Instance != null) GameSession.Instance.playerCurrentHP = currentHP;
    }

    public void RevivirAFull()
    {
        currentHP = maxHP;
        invulnerable = false;

        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;
    }
}
