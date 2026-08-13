using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// DialogueManager universal para el sistema de diálogos desacoplado y extensible.
/// Maneja cualquier DialogueData con líneas, opciones dinámicas y saltos entre bloques.
/// </summary>
public class DialogueManager : MonoBehaviour
{
    private static DialogueManager _instance;
    private static bool isQuitting = false;

    public static DialogueManager Instance
    {
        get
        {
            if (isQuitting)
                return null;

            if (_instance == null)
            {
                DialogueManager[] found = FindObjectsByType<DialogueManager>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                _instance = found != null && found.Length > 0 ? found[0] : null;
                if (_instance != null)
                    _instance.gameObject.SetActive(true); // Garantiza el Canvas de diálogo activo
            }
            return _instance;
        }
    }

    /// <summary>
    /// Mapeo dinámico de retrato por Hablante y Emoción.
    /// Permite configurar en el Inspector un sprite diferente para cada combinación.
    /// </summary>
    [System.Serializable]
    public struct PortraitMapping
    {
        public SpeakerType speaker;
        public EmotionType emotion;
        public Sprite portraitSprite;
    }

    private enum DialogueState
    {
        Closed,
        Typing,
        WaitingInput
    }

    [Header("UI Root")]
    [SerializeField] private GameObject dialogueRoot;

    [Header("Text")]
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float characterDelay = 0.035f;

    [Header("Portraits")]
    [Tooltip("Imagen del retrato del jugador.")]
    [SerializeField] private Image playerPortrait;
    [Tooltip("Imagen del retrato del NPC / Boss.")]
    [SerializeField] private Image npcPortrait;
    [Tooltip("Lista de retratos mapeados por Hablante + Emoción. Si no hay coincidencia, se oculta el retrato.")]
    [SerializeField] private List<PortraitMapping> portraitMappings = new List<PortraitMapping>();

    [Header("Layout")]
    [Tooltip("RectTransform del panel marco principal del diálogo. Pivot X debe ser 0.5.")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [Tooltip("RectTransform del contenedor hijo donde está el textoMeshPro (para corregir el espejo).")]
    [SerializeField] private RectTransform textContainerRect;

    [Header("Continue Indicator")]
    [SerializeField] private GameObject continueArrow;

    [Header("Choice Buttons")]
    [Tooltip("Prefab del botón de opción. Debe tener TextMeshProUGUI como hijo.")]
    [SerializeField] private Button choiceButtonPrefab;
    [Tooltip("Contenedor donde se instanciarán los botones de opción dinámicamente.")]
    [SerializeField] private Transform choiceButtonContainer;

    [Header("Close Option")]
    [SerializeField] private bool autoCloseAfterLastLine = true;
    [SerializeField] private float autoCloseDelay = 0.8f;

    public bool IsOpen => state != DialogueState.Closed;
    public event System.Action OnDialogueClosed;
    public event System.Action<string> OnCustomEvent;

    private DialogueState state = DialogueState.Closed;
    private DialogueData currentData;
    private IPlayerMovementLock currentMovementLock;
    private Coroutine typewriterCoroutine;
    private Coroutine autoCloseCoroutine;
    private string currentFullLine = string.Empty;
    private int currentLineIndex;
    private int ignoreAdvanceInputFrame = -1;
    private SpeakerType currentSpeaker = SpeakerType.NPC;
    private readonly List<Button> spawnedChoiceButtons = new List<Button>();

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // Auto-curar la bandera: la escena anterior pudo marcar isQuitting al
        // destruirse. Un nuevo Canvas de diálogo reinicia el sistema.
        isQuitting = false;

        _instance = this;
        SetDialogueRootActive(false);
        HideContinueArrow();
    }

    private void OnApplicationQuit()
    {
        isQuitting = true;
    }

    private void OnDestroy()
    {
        if (_instance == this)
        {
            isQuitting = true;
            _instance = null;
        }
    }

    private void Update()
    {
        if (!IsOpen || state == DialogueState.Closed)
        {
            return;
        }

        if (spawnedChoiceButtons.Count > 0)
        {
            return;
        }

        if (Time.frameCount == ignoreAdvanceInputFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            HandleAdvanceInput();
        }
    }

    /// <summary>
    /// Inicia un diálogo con los datos proporcionados.
    /// </summary>
    /// <param name="data">Datos del diálogo a reproducir.</param>
    /// <param name="movementLock">Referencia opcional para bloquear el movimiento del jugador.</param>
    public void StartDialogue(DialogueData data, IPlayerMovementLock movementLock = null)
    {
        if (data == null || data.Lines == null || data.Lines.Count == 0)
        {
            Debug.LogWarning("[DialogueManager] Cannot start dialogue with null or empty DialogueData.");
            return;
        }

        if (dialogueText == null)
        {
            Debug.LogWarning("[DialogueManager] dialogueText is not assigned.");
            return;
        }

        StopActiveCoroutines();
        DestroyChoiceButtons();

        currentData = data;
        currentMovementLock = movementLock;
        currentLineIndex = 0;
        ignoreAdvanceInputFrame = Time.frameCount;

        // Asegurar que el Canvas/GameObject del diálogo esté activo antes de mostrarlo.
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        SetPlayerMovementLocked(true);
        SetDialogueRootActive(true);
        HideContinueArrow();

        DialogueLine firstLine = currentData.Lines[currentLineIndex];
        UpdateSpeakerUI(firstLine.speaker, firstLine.emotion);
        if (firstLine.choices != null && firstLine.choices.Count > 0)
        {
            StartTypingWithOptions(firstLine.text, firstLine.choices);
        }
        else
        {
            StartTyping(firstLine.text);
        }
    }

    /// <summary>
    /// Cierra el diálogo actual y limpia el estado.
    /// </summary>
    public void CloseDialogue()
    {
        StopActiveCoroutines();
        DestroyChoiceButtons();
        HideContinueArrow();
        SetDialogueRootActive(false);
        ClearDialogueText();

        state = DialogueState.Closed;
        currentData = null;
        currentLineIndex = 0;

        ResetPanelRotation();

        SetPlayerMovementLocked(false);
        currentMovementLock = null;

        OnDialogueClosed?.Invoke();

        // Apagar el Canvas del diálogo al concluir la conversación.
        gameObject.SetActive(false);
    }

    private void HandleAdvanceInput()
    {
        switch (state)
        {
            case DialogueState.Typing:
                CompleteCurrentLine();
                break;

            case DialogueState.WaitingInput:
                AdvanceLine();
                break;
        }
    }

    private void AdvanceLine()
    {
        currentLineIndex++;

        if (currentData == null || currentLineIndex >= currentData.Lines.Count)
        {
            if (autoCloseAfterLastLine)
            {
                StartCoroutine(CloseAfterDelay(autoCloseDelay));
            }
            else
            {
                CloseDialogue();
            }
            return;
        }

        DialogueLine line = currentData.Lines[currentLineIndex];
        UpdateSpeakerUI(line.speaker, line.emotion);

        if (line.choices != null && line.choices.Count > 0)
        {
            StartTypingWithOptions(line.text, line.choices);
        }
        else
        {
            StartTyping(line.text);
        }
    }

    private void StartTyping(string line)
    {
        StopTypewriter();
        DestroyChoiceButtons();

        currentFullLine = line;
        state = DialogueState.Typing;
        HideContinueArrow();
        typewriterCoroutine = StartCoroutine(TypeLine(line));
    }

    private void StartTypingWithOptions(string line, List<DialogueChoice> choices)
    {
        StopTypewriter();
        DestroyChoiceButtons();

        currentFullLine = line;
        state = DialogueState.Typing;
        HideContinueArrow();
        typewriterCoroutine = StartCoroutine(TypeLineWithOptions(line, choices));
    }

    private IEnumerator TypeLine(string line)
    {
        dialogueText.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        typewriterCoroutine = null;
        FinishTyping();
    }

    private IEnumerator TypeLineWithOptions(string line, List<DialogueChoice> choices)
    {
        dialogueText.text = string.Empty;

        for (int i = 0; i < line.Length; i++)
        {
            dialogueText.text += line[i];
            yield return new WaitForSecondsRealtime(characterDelay);
        }

        typewriterCoroutine = null;
        FinishTypingWithOptions(choices);
    }

    private void CompleteCurrentLine()
    {
        StopTypewriter();
        dialogueText.text = currentFullLine;
        state = DialogueState.WaitingInput;
        ignoreAdvanceInputFrame = Time.frameCount;

        DialogueLine line = currentData.Lines[currentLineIndex];
        if (line.choices != null && line.choices.Count > 0)
        {
            ShowChoiceButtons(line.choices);
        }
        else
        {
            ShowContinueArrow();
        }
    }

    private void FinishTyping()
    {
        ShowContinueArrow();
        state = DialogueState.WaitingInput;
    }

    private void FinishTypingWithOptions(List<DialogueChoice> choices)
    {
        ShowChoiceButtons(choices);
    }

    private void ShowChoiceButtons(List<DialogueChoice> choices)
    {
        if (choiceButtonPrefab == null || choiceButtonContainer == null)
        {
            Debug.LogWarning("[DialogueManager] Choice button prefab or container not assigned. Falling back to continue.");
            ShowContinueArrow();
            state = DialogueState.WaitingInput;
            return;
        }

        state = DialogueState.WaitingInput;

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            int capturedIndex = i;

            Button button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            button.gameObject.SetActive(true);

            TextMeshProUGUI buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            if (buttonText != null)
            {
                buttonText.text = choice.optionText;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => OnChoiceSelected(choice));

            spawnedChoiceButtons.Add(button);
        }
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        DestroyChoiceButtons();
        HideContinueArrow();

        bool dialogueClosed = ExecuteAction(choice.actionType, choice.customEventKey);
        if (dialogueClosed) return;

        if (choice.fullResponseText != null && choice.fullResponseText.Length > 0)
        {
            StartTyping(choice.fullResponseText);
        }
        else if (choice.nextDialogueBlock != null)
        {
            DialogueData nextData = choice.nextDialogueBlock;
            CloseDialogue();
            StartDialogue(nextData, currentMovementLock);
        }
        else
        {
            AdvanceLine();
        }
    }

    private bool ExecuteAction(DialogueActionType actionType, string customEventKey)
    {
        switch (actionType)
        {
            case DialogueActionType.None:
                return false;

            case DialogueActionType.OpenShop:
                ShopUIController shop = FindFirstObjectByType<ShopUIController>();
                if (shop != null)
                {
                    shop.OpenShop();
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] ShopUIController not found in scene.");
                }
                return false;

            case DialogueActionType.OpenWeaponChoice:
                InitialWeaponChoiceUI weaponUI = FindFirstObjectByType<InitialWeaponChoiceUI>();
                if (weaponUI != null)
                {
                    weaponUI.OpenPanelFromDialogue();
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] InitialWeaponChoiceUI not found in scene.");
                }
                return true;

            case DialogueActionType.CloseDialogue:
                CloseDialogue();
                return true;

            case DialogueActionType.CustomEvent:
                if (!string.IsNullOrEmpty(customEventKey))
                {
                    OnCustomEvent?.Invoke(customEventKey);
                }
                return false;
        }
        return false;
    }

    private void DestroyChoiceButtons()
    {
        for (int i = 0; i < spawnedChoiceButtons.Count; i++)
        {
            if (spawnedChoiceButtons[i] != null)
            {
                Destroy(spawnedChoiceButtons[i].gameObject);
            }
        }
        spawnedChoiceButtons.Clear();
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        CloseDialogue();
    }

    private void UpdateSpeakerUI(SpeakerType speaker, EmotionType emotion)
    {
        currentSpeaker = speaker;

        bool isPlayer = speaker == SpeakerType.Jugador;

        if (speakerNameText != null)
        {
            speakerNameText.text = isPlayer ? "Jugador" : speaker.ToString();
        }

        Sprite portrait = GetPortrait(speaker, emotion);

        if (playerPortrait != null)
        {
            playerPortrait.gameObject.SetActive(isPlayer && portrait != null);
            if (isPlayer && portrait != null) playerPortrait.sprite = portrait;
        }

        if (npcPortrait != null)
        {
            npcPortrait.gameObject.SetActive(!isPlayer && portrait != null);
            if (!isPlayer && portrait != null) npcPortrait.sprite = portrait;
        }

        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.localRotation = isPlayer
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }

        if (textContainerRect != null)
        {
            textContainerRect.localRotation = isPlayer
                ? Quaternion.Euler(0f, 180f, 0f)
                : Quaternion.identity;
        }
    }

    private Sprite GetPortrait(SpeakerType speaker, EmotionType emotion)
    {
        for (int i = 0; i < portraitMappings.Count; i++)
        {
            if (portraitMappings[i].speaker == speaker && portraitMappings[i].emotion == emotion)
            {
                return portraitMappings[i].portraitSprite;
            }
        }

        for (int i = 0; i < portraitMappings.Count; i++)
        {
            if (portraitMappings[i].speaker == speaker)
            {
                return portraitMappings[i].portraitSprite;
            }
        }

        return null;
    }

    private void SetDialogueRootActive(bool active)
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(active);
        }
    }

    private void ShowContinueArrow()
    {
        if (continueArrow != null)
        {
            continueArrow.SetActive(true);
        }
    }

    private void HideContinueArrow()
    {
        if (continueArrow != null)
        {
            continueArrow.SetActive(false);
        }
    }

    private void ClearDialogueText()
    {
        if (dialogueText != null)
        {
            dialogueText.text = string.Empty;
        }

        if (speakerNameText != null)
        {
            speakerNameText.text = string.Empty;
        }

        if (playerPortrait != null)
        {
            playerPortrait.gameObject.SetActive(false);
        }

        if (npcPortrait != null)
        {
            npcPortrait.gameObject.SetActive(false);
        }

        currentFullLine = string.Empty;
    }

    private void ResetPanelRotation()
    {
        if (dialoguePanelRect != null)
        {
            dialoguePanelRect.localRotation = Quaternion.identity;
        }

        if (textContainerRect != null)
        {
            textContainerRect.localRotation = Quaternion.identity;
        }
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
