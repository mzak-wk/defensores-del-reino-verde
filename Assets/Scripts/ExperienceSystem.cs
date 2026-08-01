using UnityEngine;
using System;

public class ExperienceSystem : MonoBehaviour
{
    [Header("Nivelación")]
    public int level = 1;
    public float currentXP = 0f;
    public float xpToNext = 150f;
    [Range(1f, 5f)] public float costMultiplier = 1.6f;

    public Action<int> OnLevelUp;

    void Awake()
    {
        // Cargar desde GameSession si existe (incluyendo temp)
        if (GameSession.Instance != null)
        {
            level = GameSession.Instance.playerLevel;
            currentXP = GameSession.Instance.playerXP + GameSession.Instance.tempXP;
            xpToNext = GameSession.Instance.playerXpToNext;
        }
    }

    public void AddXP(int amount)
    {
        currentXP += amount;
        CheckLevelUps();

        // Guardar en GameSession como temporal (roguelite)
        if (GameSession.Instance != null)
        {
            GameSession.Instance.AddTempXP(amount);
        }

        Debug.Log($"XP +{amount} | Nivel {level} ({currentXP:0}/{xpToNext:0})");
    }

    void CheckLevelUps()
    {
        while (currentXP >= xpToNext)
        {
            currentXP -= xpToNext;
            level++;
            xpToNext = Mathf.Ceil(xpToNext * costMultiplier);
            OnLevelUp?.Invoke(level);
        }
    }

    public float Progress01() => Mathf.Clamp01(currentXP / xpToNext);
}
