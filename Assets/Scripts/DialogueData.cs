using System;
using System.Collections.Generic;
using UnityEngine;

public enum DialoguePortraitId
{
    None,
    Van,
    Player
}

public enum DialogueOptionType
{
    Talk,
    Shop,
    Close,
    Custom
}

public enum SpeakerType
{
    NPC,
    Player
}

[Serializable]
public struct DialogueChoiceData
{
    public string optionText;
    public DialogueOptionType optionType;
}

[CreateAssetMenu(menuName = "Dialogue/NPC Dialogue Data", fileName = "New NPC Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [Header("NPC")]
    [SerializeField] private string npcName = "NPC";
    [SerializeField] private DialoguePortraitId portraitId = DialoguePortraitId.None;

    [Header("Dialogue Type")]
    [Tooltip("Si es false, el diálogo será lineal (sin botones de opciones). Si es true, mostrará los botones de opciones configurados.")]
    [SerializeField] private bool hasChoices = true;

    [Header("Dialogue Content")]
    [TextArea(2, 4)]
    [SerializeField] private string initialGreeting = "Que necesitas?";
    [SerializeField] private SpeakerType greetingSpeaker = SpeakerType.NPC;

    [TextArea(2, 4)]
    [SerializeField] private string[] talkLines;
    [SerializeField] private SpeakerType[] talkLineSpeakers;

    [TextArea(2, 4)]
    [SerializeField] private string farewellLine = "Vale, hasta luego.";
    [SerializeField] private SpeakerType farewellSpeaker = SpeakerType.NPC;

    [Header("Custom Choices (Si hasChoices = true)")]
    [SerializeField] private List<DialogueChoiceData> choices = new List<DialogueChoiceData>();

    public string NpcName => npcName;
    public DialoguePortraitId PortraitId => portraitId;
    public bool HasChoices => hasChoices;
    public string InitialGreeting => initialGreeting;
    public SpeakerType GreetingSpeaker => greetingSpeaker;
    public string[] TalkLines => talkLines;
    public SpeakerType[] TalkLineSpeakers => talkLineSpeakers;
    public string FarewellLine => farewellLine;
    public SpeakerType FarewellSpeaker => farewellSpeaker;
    public IReadOnlyList<DialogueChoiceData> Choices => choices;

    public SpeakerType GetTalkLineSpeaker(int index)
    {
        if (talkLineSpeakers != null && index >= 0 && index < talkLineSpeakers.Length)
            return talkLineSpeakers[index];
        return SpeakerType.NPC;
    }
}
