using UnityEngine;
using TMPro;

public class GoldManager : MonoBehaviour
{
    public static GoldManager Instance { get; private set; }

    [Header("Estado")]
    public int gold = 0;

    [Header("UI (opcional)")]
    public TMP_Text goldText;  // arrastra un TMP arriba-derecha

    void Awake()
    {
        if (Instance && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Cargar oro desde GameSession si existe
        if (GameSession.Instance != null)
        {
            gold = Mathf.Max(0, GameSession.Instance.gold);
        }

        UpdateUI();
    }

    public void AddGold(int amount)
    {
        gold = Mathf.Max(0, gold + amount);

        // Guardar en GameSession
        if (GameSession.Instance != null)
            GameSession.Instance.gold = gold;

        UpdateUI();
    }

    public void SetGold(int value)
    {
        gold = Mathf.Max(0, value);

        if (GameSession.Instance != null)
            GameSession.Instance.gold = gold;

        UpdateUI();
    }

    void UpdateUI()
    {
        if (goldText) goldText.text = gold.ToString();
    }
}