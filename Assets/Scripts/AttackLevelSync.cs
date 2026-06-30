using UnityEngine;

public class AttackLevelSync : MonoBehaviour
{
    public ExperienceSystem xpSystem;
    public PlayerAutoAttack playerAttack;
    public FireMageAutoAttack fireMageAttack;
    public ArcherAutoAttack archerAttack;

    void Awake()
    {
        if (!xpSystem)
            xpSystem = FindAnyObjectByType<ExperienceSystem>();
        if (!playerAttack)
            playerAttack = FindAnyObjectByType<PlayerAutoAttack>();
        if (!fireMageAttack)
            fireMageAttack = FindAnyObjectByType<FireMageAutoAttack>();
        if (!archerAttack)
            archerAttack = FindAnyObjectByType<ArcherAutoAttack>();
    }

    void OnEnable()
    {
        if (xpSystem != null)
            xpSystem.OnLevelUp += HandleLevelUp;
    }

    void OnDisable()
    {
        if (xpSystem != null)
            xpSystem.OnLevelUp -= HandleLevelUp;
    }

    void Start()
    {
        // Al empezar, igualamos el nivel actual de XP al ataque
        if (xpSystem != null)
        {
            ApplyLevelToAttacks(xpSystem.level);
            Debug.Log($"[AttackLevelSync] Start: nivel inicial = {xpSystem.level}");
        }
    }

    void HandleLevelUp(int newLevel)
    {
        ApplyLevelToAttacks(newLevel);
        Debug.Log($"[AttackLevelSync] OnLevelUp -> nivel ataque = {newLevel}");
    }

    void ApplyLevelToAttacks(int newLevel)
    {
        if (playerAttack != null)
            playerAttack.SetLevel(newLevel);
        if (fireMageAttack != null)
            fireMageAttack.SetLevel(newLevel);
        if (archerAttack != null)
            archerAttack.SetLevel(newLevel);
    }
}
