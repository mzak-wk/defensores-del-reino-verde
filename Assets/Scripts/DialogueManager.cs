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
        WaitingFarewellClose,
        TypingBossLine,
        WaitingBossAdvance
    }

    [Header("UI Root")]
    [SerializeField] private GameObject dialogueRoot;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float characterDelay = 0.035f;

    [Header("Portraits")]
    [SerializeField] private Image playerPortrait;
    [SerializeField] private Image npcPortrait;

    [Header("Layout")]
    [Tooltip("RectTransform del panel marco principal del diálogo. Pivot X debe ser 0.5.")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [Tooltip("RectTransform del contenedor hijo donde está el textoMeshPro (para corregir el espejo).")]
    [SerializeField] private RectTransform textContainerRect;

    [Header("Continue Indicator")]
    [SerializeField] private GameObject continueArrow;

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
    public event System.Action OnDialogueClosed;

    private DialogueData currentData;
    private BossDialogueData currentBossData;
    private IPlayerMovementLock currentMovementLock;
    private Coroutine typewriterCoroutine;
    private Coroutine autoCloseCoroutine;
    private DialogueState state = DialogueState.Closed;
    private string currentFullLine = string.Empty;
    private int talkLineIndex;
    private int bossLineIndex;
    private int ignoreAdvanceInputFrame = -1;
    private SpeakerType currentSpeaker = SpeakerType.NPC;

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
        HideContinueArrow();
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

    private bool _isLinearMode = false;

    public void StartDialogue(DialogueData data, IPlayerMovementLock movementLock = null, bool linearMode = false)
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
        currentBossData = null;
        currentMovementLock = movementLock;
        
        _isLinearMode = linearMode || !data.HasChoices;
        
        talkLineIndex = 0;
        ignoreAdvanceInputFrame = Time.frameCount;

        SetPlayerMovementLocked(true);
        SetDialogueRootActive(true);
        HideChoiceButtons();
        SetNpcName(data.NpcName);

        UpdateSpeakerUI(data.GreetingSpeaker);
        StartTyping(GetSafeLine(data.InitialGreeting), DialogueState.TypingGreeting);
    }

    public void StartBossDialogue(BossDialogueData data, IPlayerMovementLock movementLock = null)
    {
        if (data == null || data.DialogueLines == null || data.DialogueLines.Length == 0)
        {
            Debug.LogWarning("[DialogueManager] Cannot start boss dialogue without valid BossDialogueData.");
            OnDialogueClosed?.Invoke();
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("[DialogueManager] Cannot start boss dialogue because dialogueText is not assigned.");
            OnDialogueClosed?.Invoke();
            return;
        }

        StopActiveCoroutines();

        currentData = null;
        currentBossData = data;
        currentMovementLock = movementLock;
        bossLineIndex = 0;
        ignoreAdvanceInputFrame = Time.frameCount;

        SetPlayerMovementLocked(true);
        SetDialogueRootActive(true);
        HideChoiceButtons();
        SetNpcName(data.BossName);

        SpeakerType firstSpeaker = data.GetDialogueLineSpeaker(0);
        UpdateSpeakerUI(firstSpeaker);

        StartTyping(GetSafeLine(data.DialogueLines[0]), DialogueState.TypingBossLine);
    }

    public void CloseDialogue()
    {
        StopActiveCoroutines();
        HideChoiceButtons();
        HideContinueArrow();
        SetDialogueRootActive(false);
        ClearDialogue();

        state = DialogueState.Closed;
        currentData = null;
        currentBossData = null;
        _isLinearMode = false;
        talkLineIndex = 0;
        bossLineIndex = 0;

        ResetPanelRotation();

        SetPlayerMovementLocked(false);
        currentMovementLock = null;

        OnDialogueClosed?.Invoke();
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
            case DialogueState.TypingBossLine:
                CompleteCurrentLine();
                break;

            case DialogueState.WaitingTalkAdvance:
                AdvanceTalkBranch();
                break;

            case DialogueState.WaitingBossAdvance:
                AdvanceBossDialogue();
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

        SpeakerType farewellSpeaker = currentData != null ? currentData.FarewellSpeaker : SpeakerType.NPC;
        UpdateSpeakerUI(farewellSpeaker);

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

        UpdateSpeakerUI(currentData.GetTalkLineSpeaker(talkLineIndex));

        string nextLine = currentData.TalkLines[talkLineIndex];
        talkLineIndex++;

        if (string.IsNullOrWhiteSpace(nextLine))
        {
            AdvanceTalkBranch();
            return;
        }

        StartTyping(nextLine, DialogueState.TypingTalkLine);
    }

    private void AdvanceBossDialogue()
    {
        if (currentBossData == null || currentBossData.DialogueLines == null || bossLineIndex >= currentBossData.DialogueLines.Length)
        {
            CloseDialogue();
            return;
        }

        UpdateSpeakerUI(currentBossData.GetDialogueLineSpeaker(bossLineIndex));

        string nextLine = currentBossData.DialogueLines[bossLineIndex];
        bossLineIndex++;

        if (string.IsNullOrWhiteSpace(nextLine))
        {
            AdvanceBossDialogue();
            return;
        }

        StartTyping(nextLine, DialogueState.TypingBossLine);
    }

    private void StartTyping(string line, DialogueState typingState)
    {
        StopTypewriter();

        currentFullLine = line;
        state = typingState;
        HideContinueArrow();
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
                ShowContinueArrow();
                if (_isLinearMode)
                {
                    state = DialogueState.WaitingTalkAdvance;
                    AdvanceTalkBranch();
                }
                else
                {
                    state = DialogueState.WaitingChoice;
                    ShowChoiceButtons();
                }
                break;

            case DialogueState.TypingTalkLine:
                state = DialogueState.WaitingTalkAdvance;
                ShowContinueArrow();
                break;

            case DialogueState.TypingFarewell:
                state = DialogueState.WaitingFarewellClose;
                ShowContinueArrow();
                StartFarewellAutoClose();
                break;

            case DialogueState.TypingBossLine:
                state = DialogueState.WaitingBossAdvance;
                ShowContinueArrow();
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
        if (_isLinearMode)
        {
            HideChoiceButtons();
            return;
        }

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

    private void UpdateSpeakerUI(SpeakerType speaker)
    {
        currentSpeaker = speaker;

        if (speaker == SpeakerType.Player)
        {
            if (playerPortrait != null)
                playerPortrait.gameObject.SetActive(true);
            if (playerNameText != null)
                playerNameText.gameObject.SetActive(true);
            if (npcPortrait != null)
                npcPortrait.gameObject.SetActive(false);
            if (npcNameText != null)
                npcNameText.gameObject.SetActive(false);

            if (dialoguePanelRect != null)
                dialoguePanelRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
            if (textContainerRect != null)
                textContainerRect.localRotation = Quaternion.Euler(0f, 180f, 0f);
        }
        else
        {
            if (playerPortrait != null)
                playerPortrait.gameObject.SetActive(false);
            if (playerNameText != null)
                playerNameText.gameObject.SetActive(false);
            if (npcPortrait != null)
                npcPortrait.gameObject.SetActive(true);
            if (npcNameText != null)
                npcNameText.gameObject.SetActive(true);

            if (dialoguePanelRect != null)
                dialoguePanelRect.localRotation = Quaternion.identity;
            if (textContainerRect != null)
                textContainerRect.localRotation = Quaternion.identity;
        }

        UpdateSpeakerVisuals(speaker);
    }

    private void UpdateSpeakerVisuals(SpeakerType speaker)
    {
        if (speaker == SpeakerType.NPC)
        {
            if (currentData != null)
            {
                if (npcNameText != null)
                    npcNameText.text = currentData.NpcName;

                if (npcPortrait != null)
                {
                    Sprite npcSprite = GetNpcSpriteFromPortraitId(currentData.PortraitId);
                    if (npcSprite != null)
                        npcPortrait.sprite = npcSprite;
                }
            }
            else if (currentBossData != null)
            {
                if (npcNameText != null)
                    npcNameText.text = currentBossData.BossName;

                if (npcPortrait != null && currentBossData.BossPortrait != null)
                    npcPortrait.sprite = currentBossData.BossPortrait;
            }
        }
        else
        {
            if (playerNameText != null)
                playerNameText.text = "Jugador";
        }
    }

    private Sprite GetNpcSpriteFromPortraitId(DialoguePortraitId portraitId)
    {
        return null;
    }

    private void ResetPanelRotation()
    {
        if (dialoguePanelRect != null)
            dialoguePanelRect.localRotation = Quaternion.identity;
        if (textContainerRect != null)
            textContainerRect.localRotation = Quaternion.identity;
    }

    private void ShowContinueArrow()
    {
        if (continueArrow != null)
            continueArrow.SetActive(true);
    }

    private void HideContinueArrow()
    {
        if (continueArrow != null)
            continueArrow.SetActive(false);
    }

    private void ClearDialogue()
    {
        if (npcNameText != null)
        {
            npcNameText.text = string.Empty;
        }

        if (playerNameText != null)
        {
            playerNameText.text = string.Empty;
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
