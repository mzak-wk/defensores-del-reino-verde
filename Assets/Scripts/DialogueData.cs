using UnityEngine;

public enum DialoguePortraitId
{
    None,
    Van,
    Player
}

[CreateAssetMenu(menuName = "Dialogue/NPC Dialogue Data", fileName = "New NPC Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("NPC")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private DialoguePortraitId portraitId = DialoguePortraitId.None;

    [Header("Dialogue")]
    [TextArea(2, 4)]
    [SerializeField] private string initialGreeting = "Que necesitas?";

    [TextArea(2, 4)]
    [SerializeField] private string[] talkLines;

    [TextArea(2, 4)]
    [SerializeField] private string farewellLine = "Vale, hasta luego.";

    public string NpcName => npcName;
    public DialoguePortraitId PortraitId => portraitId;
    public string InitialGreeting => initialGreeting;
    public string[] TalkLines => talkLines;
    public string FarewellLine => farewellLine;
}
