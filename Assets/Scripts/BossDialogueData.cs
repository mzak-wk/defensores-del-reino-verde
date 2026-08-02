using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Boss Dialogue Data", fileName = "New Boss Dialogue Data")]
public class BossDialogueData : ScriptableObject
{
    [Header("Boss")]
    [SerializeField] private string bossName = "Boss";
    [SerializeField] private Sprite bossPortrait;

    [Header("Diálogo Lineal")]
    [TextArea(2, 4)]
    [SerializeField] private string[] dialogueLines;
    [SerializeField] private SpeakerType[] dialogueLineSpeakers;

    [Header("Opciones (Forzado a False)")]
    [SerializeField, HideInInspector] private readonly bool hasChoices = false;

    public string BossName => bossName;
    public Sprite BossPortrait => bossPortrait;
    public string[] DialogueLines => dialogueLines;
    public SpeakerType[] DialogueLineSpeakers => dialogueLineSpeakers;
    public bool HasChoices => false;

    public SpeakerType GetDialogueLineSpeaker(int index)
    {
        if (dialogueLineSpeakers != null && index >= 0 && index < dialogueLineSpeakers.Length)
            return dialogueLineSpeakers[index];
        return SpeakerType.NPC;
    }
}
