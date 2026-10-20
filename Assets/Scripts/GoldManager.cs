using System;
using UnityEngine;
using TMPro;

/// <summary>
/// Gestor central de la economía y monedas (LutroCoins).
/// Mantiene sincronía con GameSession, emite eventos para la UI del HUD y la Tienda,
/// y persiste entre escenas como Singleton seguro.
/// </summary>
public class GoldManager : MonoBehaviour
{
    private static GoldManager _instance;
    public static GoldManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindFirstObjectByType<GoldManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("GoldManager");
                    _instance = go.AddComponent<GoldManager>();
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Estado de Monedas")]
    public int gold = 0;

    [Header("UI Opcional (Asignación Directa)")]
    public TMP_Text goldText;

    /// <summary>
    /// Evento estático global emitido cuando la cantidad de oro cambia.
    /// Parámetros: (int totalGold, int delta)
    /// </summary>
    public static event Action<int, int> OnGoldChanged;

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);

        // Cargar oro desde GameSession si existe
        if (GameSession.Instance != null)
        {
            gold = Mathf.Max(0, GameSession.Instance.gold);
        }

        UpdateUI();
        CoinHUDController.EnsureInstance();
    }

    private void OnEnable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private void OnDisable()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded -= HandleSceneLoaded;
    }

    private void HandleSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        SyncFromGameSession();
        CoinHUDController.EnsureInstance();
    }

    private void Start()
    {
        SyncFromGameSession();
        CoinHUDController.EnsureInstance();
    }

    public void SyncFromGameSession()
    {
        if (GameSession.Instance != null)
        {
            gold = Mathf.Max(0, GameSession.Instance.gold);
            UpdateUI();
            OnGoldChanged?.Invoke(gold, 0);
        }
    }

    public void AddGold(int amount)
    {
        if (amount <= 0) return;

        gold = Mathf.Max(0, gold + amount);

        // Sincronizar de inmediato con GameSession (tanto persistente como temporal de run)
        if (GameSession.Instance != null)
        {
            GameSession.Instance.gold = gold;
            GameSession.Instance.AddTempGold(amount);
        }

        UpdateUI();
        OnGoldChanged?.Invoke(gold, amount);
    }

    public bool TrySpend(int amount)
    {
        if (amount < 0) return false;
        if (gold < amount) return false;

        gold -= amount;

        if (GameSession.Instance != null)
        {
            GameSession.Instance.gold = gold;
        }

        UpdateUI();
        OnGoldChanged?.Invoke(gold, -amount);
        return true;
    }

    public void SetGold(int value)
    {
        int delta = value - gold;
        gold = Mathf.Max(0, value);

        if (GameSession.Instance != null)
        {
            GameSession.Instance.gold = gold;
        }

        UpdateUI();
        OnGoldChanged?.Invoke(gold, delta);
    }

    private void UpdateUI()
    {
        if (goldText != null)
            goldText.text = gold.ToString();
    }
}