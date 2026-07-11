using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DialogueManager : MonoBehaviour
{
    private const string DefaultFarewellLine = "Vale, hasta luego.";

    public static DialogueManager Instance { get; private set; }

    private enum DialogueState
    {
        Closed,
        TypingGreeting,
        WaitingChoice,
        TypingTalkLine,
        WaitingTalkAdvance,
        TypingFarewell,
        WaitingFarewellClose
    }

    [Header("UI Root")]
    [SerializeField] private GameObject dialogueRoot;

    [Header("Text")]
    [SerializeField] private TMP_Text npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float characterDelay = 0.035f;

    [Header("Portrait Slots")]
    [SerializeField] private GameObject leftPortraitSlot;
    [SerializeField] private GameObject rightPortraitSlot;

    [Header("Choice Buttons")]
    [SerializeField] private Button talkButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button closeButton;

    [Header("Shop")]
    [SerializeField] private ShopUIController shopUIController;

    [Header("Close Option")]
    [SerializeField] private bool autoCloseAfterFarewell = true;
    [SerializeField] private float farewellAutoCloseDelay = 0.8f;

    public bool IsOpen => state != DialogueState.Closed;

    private DialogueData currentData;
    private IPlayerMovementLock currentMovementLock;
    private Coroutine typewriterCoroutine;
    private Coroutine autoCloseCoroutine;
    private DialogueState state = DialogueState.Closed;
    private string currentFullLine = string.Empty;
    private int talkLineIndex;
    private int ignoreAdvanceInputFrame = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        RegisterButtonListeners();
        HideChoiceButtons();
        SetDialogueRootActive(false);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!IsOpen || state == DialogueState.WaitingChoice)
        {
            return;
        }

        if (Time.frameCount == ignoreAdvanceInputFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E))
        {
            HandleAdvanceInput();
        }
    }

    public void StartDialogue(DialogueData data, IPlayerMovementLock movementLock = null)
    {
        if (data == null)
        {
            Debug.LogWarning("[DialogueManager] Cannot start dialogue without DialogueData.");
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("[DialogueManager] Cannot start dialogue because dialogueText is not assigned.");
            return;
        }

        StopActiveCoroutines();

        currentData = data;
        currentMovementLock = movementLock;
        talkLineIndex = 0;
        ignoreAdvanceInputFrame = Time.frameCount;

        SetPlayerMovementLocked(true);
        SetDialogueRootActive(true);
        HideChoiceButtons();
        SetNpcName(data.NpcName);
        SetPortraitSlots(data.PortraitId);

        StartTyping(GetSafeLine(data.InitialGreeting), DialogueState.TypingGreeting);
    }

    public void CloseDialogue()
    {
        StopActiveCoroutines();
        HideChoiceButtons();
        SetDialogueRootActive(false);
        ClearDialogue();

        state = DialogueState.Closed;
        currentData = null;
        talkLineIndex = 0;

        SetPlayerMovementLocked(false);
        currentMovementLock = null;
    }

    private void RegisterButtonListeners()
    {
        if (talkButton != null)
        {
            talkButton.onClick.RemoveListener(HandleTalkSelected);
            talkButton.onClick.AddListener(HandleTalkSelected);
        }
        else
        {
            Debug.LogWarning("[DialogueManager] talkButton is not assigned.");
        }

        if (shopButton != null)
        {
            shopButton.onClick.RemoveListener(HandleShopSelected);
            shopButton.onClick.AddListener(HandleShopSelected);
        }
        else
        {
            Debug.LogWarning("[DialogueManager] shopButton is not assigned.");
        }

        if (closeButton != null)
        {
            closeButton.onClick.RemoveListener(HandleCloseSelected);
            closeButton.onClick.AddListener(HandleCloseSelected);
        }
        else
        {
            Debug.LogWarning("[DialogueManager] closeButton is not assigned.");
        }
    }

    private void HandleAdvanceInput()
    {
        switch (state)
        {
            case DialogueState.TypingGreeting:
            case DialogueState.TypingTalkLine:
            case DialogueState.TypingFarewell:
                CompleteCurrentLine();
                break;

            case DialogueState.WaitingTalkAdvance:
                AdvanceTalkBranch();
                break;

            case DialogueState.WaitingFarewellClose:
                CloseDialogue();
                break;
        }
    }

    private void HandleTalkSelected()
    {
        if (state != DialogueState.WaitingChoice)
        {
            return;
        }

        HideChoiceButtons();
        talkLineIndex = 0;
        AdvanceTalkBranch();
    }

    private void HandleShopSelected()
    {
        if (state != DialogueState.WaitingChoice)
        {
            return;
        }

        CloseDialogue();

        if (shopUIController != null)
        {
            shopUIController.OpenShop();
        }
        else
        {
            Debug.LogWarning("[DialogueManager] shopUIController is not assigned. Shop cannot be opened.");
        }
    }

    private void HandleCloseSelected()
    {
        if (state != DialogueState.WaitingChoice)
        {
            return;
        }

        HideChoiceButtons();

        string farewellLine = currentData != null && !string.IsNullOrWhiteSpace(currentData.FarewellLine)
            ? currentData.FarewellLine
            : DefaultFarewellLine;

        StartTyping(farewellLine, DialogueState.TypingFarewell);
    }

    private void AdvanceTalkBranch()
    {
        if (currentData == null || currentData.TalkLines == null || talkLineIndex >= currentData.TalkLines.Length)
        {
            CloseDialogue();
            return;
        }

        string nextLine = currentData.TalkLines[talkLineIndex];
        talkLineIndex++;

        if (string.IsNullOrWhiteSpace(nextLine))
        {
            AdvanceTalkBranch();
            return;
        }

        StartTyping(nextLine, DialogueState.TypingTalkLine);
    }

    private void StartTyping(string line, DialogueState typingState)
    {
        StopTypewriter();

        currentFullLine = line;
        state = typingState;
        typewriterCoroutine = StartCoroutine(TypeLine(line, typingState));
    }

    private IEnumerator TypeLine(string line, DialogueState typingState)
    {
        dialogueText.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        typewriterCoroutine = null;
        FinishTypingState(typingState);
    }

    private void CompleteCurrentLine()
    {
        StopTypewriter();
        dialogueText.text = currentFullLine;
        FinishTypingState(state);
    }

    private void FinishTypingState(DialogueState typingState)
    {
        switch (typingState)
        {
            case DialogueState.TypingGreeting:
                state = DialogueState.WaitingChoice;
                ShowChoiceButtons();
                break;

            case DialogueState.TypingTalkLine:
                state = DialogueState.WaitingTalkAdvance;
                break;

            case DialogueState.TypingFarewell:
                state = DialogueState.WaitingFarewellClose;
                StartFarewellAutoClose();
                break;
        }
    }

    private void StartFarewellAutoClose()
    {
        if (!autoCloseAfterFarewell)
        {
            return;
        }

        StopAutoClose();
        autoCloseCoroutine = StartCoroutine(CloseAfterDelay());
    }

    private IEnumerator CloseAfterDelay()
    {
        yield return new WaitForSecondsRealtime(farewellAutoCloseDelay);
        autoCloseCoroutine = null;
        CloseDialogue();
    }

    private void ShowChoiceButtons()
    {
        SetButtonActive(talkButton, true);
        SetButtonActive(shopButton, true);
        SetButtonActive(closeButton, true);
    }

    private void HideChoiceButtons()
    {
        SetButtonActive(talkButton, false);
        SetButtonActive(shopButton, false);
        SetButtonActive(closeButton, false);
    }

    private void SetButtonActive(Button button, bool active)
    {
        if (button != null)
        {
            button.gameObject.SetActive(active);
        }
    }

    private void SetDialogueRootActive(bool active)
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(active);
        }
    }

    private void SetNpcName(string npcName)
    {
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
        }
    }

    private void SetPortraitSlots(DialoguePortraitId portraitId)
    {
        if (leftPortraitSlot != null)
        {
            leftPortraitSlot.SetActive(portraitId != DialoguePortraitId.None);
        }

        if (rightPortraitSlot != null)
        {
            rightPortraitSlot.SetActive(false);
        }
    }

    private void ClearDialogue()
    {
        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
        }

        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        currentFullLine = string.Empty;
    }

    private string GetSafeLine(string line)
    {
        return string.IsNullOrWhiteSpace(line) ? "Que necesitas?" : line;
    }

    private void SetPlayerMovementLocked(bool locked)
    {
        if (currentMovementLock != null)
        {
            currentMovementLock.SetMovementLocked(locked);
        }
    }

    private void StopActiveCoroutines()
    {
        StopTypewriter();
        StopAutoClose();
    }

    private void StopTypewriter()
    {
        if (typewriterCoroutine != null)
        {
            StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = null;
        }
    }

    private void StopAutoClose()
    {
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
            autoCloseCoroutine = null;
        }
    }
}
