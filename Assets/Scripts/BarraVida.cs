using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Gestiona la vida, el daño por contacto, la curación, la regeneración y la invulnerabilidad temporal de Andrius.
/// Emite eventos desacoplados para la barra de vida de la UI y el sistema de muerte.
/// </summary>
public class BarraVida : MonoBehaviour
{
    [Header("Vida")]
    public float maxHP = 100f;
    public float currentHP = 100f;

    [Header("Regeneración")]
    public float regenPerSecond = 0.2f;

    [Header("Invulnerabilidad al recibir daño")]
    [Tooltip("Ventana de invulnerabilidad tras recibir un impacto (en segundos).")]
    public float invulnTimeOnHit = 0.3f;
    public bool invulnerable { get; private set; }
    public void SetInvulnerable(bool value) => invulnerable = value;

    [Header("UI Opcional (World Space)")]
    public Image fill;
    public Image fillDelay;
    public float chipSpeed = 1.5f;

    [Header("Feedback Visual de Daño")]
    [Tooltip("SpriteRenderer para parpadeo al recibir daño.")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color hurtColor = new Color(1f, 0.4f, 0.4f, 1f);

    // Eventos de Instancia
    public event Action<float, float> OnHealthChanged; // (currentHP, maxHP)
    public Action<float> OnDamaged;                     // (cantidad)
    public Action OnDeath;

    // Eventos Estáticos Globales
    public static event Action<float, float> OnGlobalHealthChanged;
    public static event Action<DeathReason> OnPlayerDeathWithReason;

    private Color originalSpriteColor = Color.white;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (spriteRenderer != null)
            originalSpriteColor = spriteRenderer.color;

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
            if (maxHP <= 0f) maxHP = 100f;
            currentHP = Mathf.Clamp(currentHP <= 0f ? maxHP : currentHP, 0f, maxHP);
        }

        UpdateWorldSpaceUI();
    }

    private void Start()
    {
        NotifyHealthChanged();
    }

    private void Update()
    {
        UpdateWorldSpaceUI();

        // Regeneración pasiva continua
        if (regenPerSecond > 0f && currentHP > 0f && currentHP < maxHP && !invulnerable)
        {
            float amount = regenPerSecond * Time.deltaTime;
            Curar(amount, false);
        }
    }

    private void UpdateWorldSpaceUI()
    {
        float t = (maxHP <= 0f) ? 0f : Mathf.Clamp01(currentHP / maxHP);

        if (fill != null) fill.fillAmount = t;

        if (fillDelay != null)
        {
            fillDelay.fillAmount = Mathf.MoveTowards(
                fillDelay.fillAmount,
                t,
                chipSpeed * Time.unscaledDeltaTime
            );
        }
    }

    public void RecibirDanio(float cantidad)
    {
        if (invulnerable || cantidad <= 0f || currentHP <= 0f) return;

        currentHP = Mathf.Max(0f, currentHP - cantidad);

        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;

        OnDamaged?.Invoke(cantidad);
        NotifyHealthChanged();

        if (currentHP <= 0f)
        {
            invulnerable = true;
            OnDeath?.Invoke();
            OnPlayerDeathWithReason?.Invoke(DeathReason.EnemyDamage);

            if (DeathMenuController.Instance == null)
            {
#if UNITY_2023_1_OR_NEWER
                var deathMenu = FindAnyObjectByType<DeathMenuController>(FindObjectsInactive.Include);
#else
                var deathMenu = FindObjectOfType<DeathMenuController>(true);
#endif
                if (deathMenu != null)
                {
                    deathMenu.gameObject.SetActive(true);
                }
            }

            if (DeathMenuController.Instance != null)
            {
                DeathMenuController.Instance.TriggerDeathSequence(DeathReason.EnemyDamage);
            }
            else
            {
                // Fallback a GameOverController si DeathMenuController no está presente
#if UNITY_2023_1_OR_NEWER
                var goCtrl = FindAnyObjectByType<GameOverController>(FindObjectsInactive.Include);
#else
                var goCtrl = FindObjectOfType<GameOverController>(true);
#endif
                if (goCtrl != null)
                {
                    if (!goCtrl.gameObject.activeInHierarchy) goCtrl.gameObject.SetActive(true);
                    if (goCtrl.gameOverUI != null) goCtrl.gameOverUI.SetActive(true);
                }
            }
            return;
        }

        if (invulnTimeOnHit > 0f && gameObject.activeInHierarchy)
        {
            StartCoroutine(InvulnFeedbackCR());
        }
    }

    private IEnumerator InvulnFeedbackCR()
    {
        invulnerable = true;

        if (spriteRenderer != null)
        {
            float elapsed = 0f;
            bool isFlash = true;

            while (elapsed < invulnTimeOnHit)
            {
                spriteRenderer.color = isFlash ? hurtColor : originalSpriteColor;
                isFlash = !isFlash;
                yield return new WaitForSeconds(0.06f);
                elapsed += 0.06f;
            }

            spriteRenderer.color = originalSpriteColor;
        }
        else
        {
            yield return new WaitForSeconds(invulnTimeOnHit);
        }

        invulnerable = false;
    }

    public void Curar(float cantidad, bool notifyImmediately = true)
    {
        if (currentHP <= 0f || cantidad <= 0f) return;

        currentHP = Mathf.Min(maxHP, currentHP + cantidad);

        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;

        if (notifyImmediately)
            NotifyHealthChanged();
    }

    public void FillToMax()
    {
        currentHP = maxHP;
        if (GameSession.Instance != null) GameSession.Instance.playerCurrentHP = currentHP;
        NotifyHealthChanged();
    }

    public void RevivirAFull()
    {
        currentHP = maxHP;
        invulnerable = false;

        if (spriteRenderer != null)
            spriteRenderer.color = originalSpriteColor;

        if (GameSession.Instance != null)
            GameSession.Instance.playerCurrentHP = currentHP;

        NotifyHealthChanged();
    }

    public void NotifyHealthChanged()
    {
        OnHealthChanged?.Invoke(currentHP, maxHP);
        OnGlobalHealthChanged?.Invoke(currentHP, maxHP);
    }
}
