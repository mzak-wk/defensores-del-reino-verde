using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class XPBarUI : MonoBehaviour
{
    [Header("Referencias")]
    public ExperienceSystem xpSystem;
    public Image fillImage;
    public TMP_Text levelText;

    [Header("Comportamiento")]
    [Tooltip("Si es 0 o menor, NO hay nivel máximo visual.")]
    public int maxLevel = 6;
    public bool lockFillAtMax = true;
    public bool showMaxTag = true;

    void Update()
    {
        if (!xpSystem || !fillImage) return;

        bool hasCap = maxLevel > 0;                           // <-- protección
        bool isMax  = hasCap && xpSystem.level >= maxLevel;

        float fill = xpSystem.Progress01();
        if (lockFillAtMax && isMax) fill = 1f;               // solo si hay cap y se alcanzó

        fillImage.fillAmount = Mathf.Clamp01(fill);

        if (levelText)
        {
            if (isMax && showMaxTag)
                levelText.text = $"Nivel {xpSystem.level} (MAX)";
            else
                levelText.text = $"Nivel {xpSystem.level} ({xpSystem.currentXP:0}/{xpSystem.xpToNext:0})";
        }
    }

#if UNITY_EDITOR
    // Por si en el inspector queda en 0 por accidente, te avisa.
    void OnValidate()
    {
        if (maxLevel < 0) maxLevel = 0;
    }
#endif
}
