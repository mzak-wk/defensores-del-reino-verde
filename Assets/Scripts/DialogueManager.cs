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
    [SerializeField] private TextMeshProUGUI npcNameText;
    [SerializeField] private TextMeshProUGUI playerNameText;
    [SerializeField] private TMP_Text dialogueText;
    [SerializeField] private float characterDelay = 0.035f;

    [Header("Portraits")]
    [Tooltip("Imagen del retrato del jugador.")]
    [SerializeField] private Image playerPortrait;
    [Tooltip("Imagen del retrato del NPC / Boss.")]
    [SerializeField] private Image npcPortrait;
    [Tooltip("Slot/RectTransform del retrato izquierdo.")]
    [SerializeField] private RectTransform leftPortraitSlot;
    [Tooltip("Slot/RectTransform del retrato derecho.")]
    [SerializeField] private RectTransform rightPortraitSlot;
    [Tooltip("Lista de retratos mapeados por Hablante + Emoción. Si no hay coincidencia, se oculta el retrato.")]
    [SerializeField] private List<PortraitMapping> portraitMappings = new List<PortraitMapping>();

    [Header("Layout")]
    [Tooltip("RectTransform del panel marco principal del diálogo. Pivot X debe ser 0.5.")]
    [SerializeField] private RectTransform dialoguePanelRect;
    [Tooltip("RectTransform del contenedor hijo donde está el textoMeshPro (para corregir el espejo).")]
    [SerializeField] private RectTransform textContainerRect;

    [Header("Continue Indicator")]
    [SerializeField] private GameObject continueArrow;

    [Header("Shop & Fallbacks")]
    [SerializeField] private ShopUIController shopUIController;
    [SerializeField] private Sprite defaultNpcPortrait;
    [SerializeField] private Sprite defaultPlayerPortrait;

    [Header("Choice Buttons")]
    [SerializeField] private Button talkButton;
    [SerializeField] private Button shopButton;
    [SerializeField] private Button closeButton;
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
        EnsureDialogueUIReferences();
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
            _instance = null;
        }
    }

    private void Update()
    {
        if (!IsOpen || state == DialogueState.Closed)
        {
            return;
        }

        if (HasActiveChoices())
        {
            return;
        }

        if (Time.frameCount <= ignoreAdvanceInputFrame)
        {
            return;
        }

        if (Input.GetKeyDown(KeyCode.E) || Input.GetMouseButtonDown(0))
        {
            HandleAdvanceInput();
        }
    }

    private bool HasActiveChoices()
    {
        if (spawnedChoiceButtons.Count > 0) return true;
        if (talkButton != null && talkButton.gameObject.activeSelf) return true;
        if (shopButton != null && shopButton.gameObject.activeSelf) return true;
        if (closeButton != null && closeButton.gameObject.activeSelf) return true;
        if (choiceButtonContainer != null && choiceButtonContainer.gameObject.activeSelf && choiceButtonContainer.childCount > 0)
        {
            for (int i = 0; i < choiceButtonContainer.childCount; i++)
            {
                var child = choiceButtonContainer.GetChild(i);
                if (child != null && child.gameObject.activeSelf) return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Inicia un diálogo con los datos proporcionados.
    /// </summary>
    /// <param name="data">Datos del diálogo a reproducir.</param>
    /// <param name="movementLock">Referencia opcional para bloquear el movimiento del jugador.</param>
    public void StartDialogue(DialogueData data, IPlayerMovementLock movementLock = null)
    {
        EnsureDialogueUIReferences();

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

        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterOverlay("Dialogue", () => CloseDialogue());
        }

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
    /// Cierra el diálogo actual y limpia el estado sin apagar el componente DialogueManager.
    /// </summary>
    public void CloseDialogue()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterOverlay("Dialogue");
        }

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
    }

    /// <summary>
    /// Busca y conecta automáticamente la distribución de Canvas_Dialogo del HUB si faltan referencias.
    /// </summary>
    public void EnsureDialogueUIReferences()
    {
        if (dialogueRoot == null)
        {
            var allCanvases = Object.FindObjectsByType<Canvas>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var c in allCanvases)
            {
                if (c != null && c.name.Contains("Canvas_Dialogo"))
                {
                    dialogueRoot = c.gameObject;
                    break;
                }
            }

            if (dialogueRoot == null)
            {
                var canvasGO = GameObject.Find("Canvas_Dialogo");
                if (canvasGO != null) dialogueRoot = canvasGO;
            }
        }

        if (dialogueRoot != null)
        {
            if (dialogueText == null)
            {
                var texts = dialogueRoot.GetComponentsInChildren<TMP_Text>(true);
                foreach (var t in texts)
                {
                    if (t.name.Contains("Text_Dialogo") || t.name.Contains("DialogueText"))
                    {
                        dialogueText = t;
                        break;
                    }
                }
            }

            if (speakerNameText == null)
            {
                var texts = dialogueRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.name.Contains("NPCName") || t.name.Contains("Speaker"))
                    {
                        speakerNameText = t;
                        break;
                    }
                }
            }

            if (npcNameText == null && speakerNameText != null)
            {
                npcNameText = speakerNameText;
            }

            if (playerNameText == null)
            {
                var texts = dialogueRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var t in texts)
                {
                    if (t.name.Contains("PlayerName") || (t.name.Contains("Player") && !t.name.Contains("Portrait")))
                    {
                        playerNameText = t;
                        break;
                    }
                }
            }

            if (dialoguePanelRect == null)
            {
                var rts = dialogueRoot.GetComponentsInChildren<RectTransform>(true);
                foreach (var rt in rts)
                {
                    if (rt.name.Contains("Caja_Texto") || rt.name.Contains("Panel"))
                    {
                        dialoguePanelRect = rt;
                        if (textContainerRect == null) textContainerRect = rt;
                        break;
                    }
                }
            }

            if (continueArrow == null)
            {
                var trans = dialogueRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in trans)
                {
                    if (t.name.ToLower().Contains("arrow") || t.name.ToLower().Contains("flecha"))
                    {
                        continueArrow = t.gameObject;
                        break;
                    }
                }
            }

            if (choiceButtonContainer == null)
            {
                var trans = dialogueRoot.GetComponentsInChildren<Transform>(true);
                foreach (var t in trans)
                {
                    if (t.name.Contains("Botones") || t.name.Contains("Buttons"))
                    {
                        choiceButtonContainer = t;
                        break;
                    }
                }
            }

            if (choiceButtonContainer != null)
            {
                var buttons = choiceButtonContainer.GetComponentsInChildren<Button>(true);
                foreach (var b in buttons)
                {
                    string bName = b.name.ToLower();
                    if (talkButton == null && (bName.Contains("hablar") || bName.Contains("talk")))
                    {
                        talkButton = b;
                    }
                    else if (shopButton == null && (bName.Contains("tienda") || bName.Contains("shop")))
                    {
                        shopButton = b;
                    }
                    else if (closeButton == null && (bName.Contains("cerrar") || bName.Contains("close") || bName.Contains("salir")))
                    {
                        closeButton = b;
                    }
                }

                if (choiceButtonPrefab == null && buttons.Length > 0)
                {
                    choiceButtonPrefab = buttons[0];
                }

                if (talkButton == null && buttons.Length > 0)
                {
                    talkButton = buttons[0];
                }
            }

            if (npcPortrait == null)
            {
                var imgs = dialogueRoot.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.name.Contains("NPCPortrait") || img.name.Contains("NPC"))
                    {
                        npcPortrait = img;
                        break;
                    }
                }
            }

            if (playerPortrait == null)
            {
                var imgs = dialogueRoot.GetComponentsInChildren<Image>(true);
                foreach (var img in imgs)
                {
                    if (img.name.Contains("Player") && !img.name.Contains("Name"))
                    {
                        playerPortrait = img;
                        break;
                    }
                }
            }

            if (shopUIController == null)
            {
                shopUIController = Object.FindFirstObjectByType<ShopUIController>();
            }
        }
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
            StopAutoClose();
            CloseDialogue();
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

        if (currentData != null && currentLineIndex >= 0 && currentLineIndex < currentData.Lines.Count)
        {
            DialogueLine line = currentData.Lines[currentLineIndex];
            if (line.choices != null && line.choices.Count > 0)
            {
                ShowChoiceButtons(line.choices);
                return;
            }
        }

        ShowContinueArrow();
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
        EnsureDialogueUIReferences();

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(true);
        }

        if (choiceButtonContainer == null && talkButton == null && shopButton == null && closeButton == null && choiceButtonPrefab == null)
        {
            Debug.LogWarning("[DialogueManager] Choice buttons not configured. Falling back to continue.");
            ShowContinueArrow();
            state = DialogueState.WaitingInput;
            return;
        }

        state = DialogueState.WaitingInput;

        // Limpiar botones anteriores
        DestroyChoiceButtons();

        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(true);
        }

        // Colección de botones estáticos disponibles
        List<Button> availableStaticButtons = new List<Button>();
        if (talkButton != null) availableStaticButtons.Add(talkButton);
        if (shopButton != null) availableStaticButtons.Add(shopButton);
        if (closeButton != null) availableStaticButtons.Add(closeButton);

        for (int i = 0; i < choices.Count; i++)
        {
            DialogueChoice choice = choices[i];
            Button buttonToUse = null;

            // 1. Intentar emparejar por actionType con botones dedicados
            if (choice.actionType == DialogueActionType.OpenShop && shopButton != null && availableStaticButtons.Contains(shopButton))
            {
                buttonToUse = shopButton;
                availableStaticButtons.Remove(shopButton);
            }
            else if (choice.actionType == DialogueActionType.CloseDialogue && closeButton != null && availableStaticButtons.Contains(closeButton))
            {
                buttonToUse = closeButton;
                availableStaticButtons.Remove(closeButton);
            }
            else if (choice.actionType == DialogueActionType.None && talkButton != null && availableStaticButtons.Contains(talkButton))
            {
                buttonToUse = talkButton;
                availableStaticButtons.Remove(talkButton);
            }
            // 2. Si no emparejó por acción pero quedan botones estáticos, usar el siguiente libre
            else if (availableStaticButtons.Count > 0)
            {
                buttonToUse = availableStaticButtons[0];
                availableStaticButtons.RemoveAt(0);
            }
            // 3. Si no hay botones estáticos disponibles, instanciar del prefab
            else if (choiceButtonPrefab != null && choiceButtonContainer != null)
            {
                buttonToUse = Instantiate(choiceButtonPrefab, choiceButtonContainer);
                spawnedChoiceButtons.Add(buttonToUse);
            }

            if (buttonToUse != null)
            {
                SetupChoiceButton(buttonToUse, choice);
            }
        }
    }

    private void SetupChoiceButton(Button button, DialogueChoice choice)
    {
        if (button == null) return;
        button.gameObject.SetActive(true);

        // Actualizar texto en TextMeshProUGUI, TMP_Text o Text legacy
        TextMeshProUGUI tmpText = button.GetComponentInChildren<TextMeshProUGUI>(true);
        if (tmpText != null)
        {
            tmpText.text = choice.optionText;
        }
        else
        {
            TMP_Text genericTmp = button.GetComponentInChildren<TMP_Text>(true);
            if (genericTmp != null)
            {
                genericTmp.text = choice.optionText;
            }
            else
            {
                UnityEngine.UI.Text legacyText = button.GetComponentInChildren<UnityEngine.UI.Text>(true);
                if (legacyText != null)
                {
                    legacyText.text = choice.optionText;
                }
            }
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(() => OnChoiceSelected(choice));
    }

    private void OnChoiceSelected(DialogueChoice choice)
    {
        StopActiveCoroutines();
        DestroyChoiceButtons();
        HideContinueArrow();
        ignoreAdvanceInputFrame = Time.frameCount + 2;

        bool dialogueClosed = ExecuteAction(choice.actionType, choice.customEventKey);
        if (dialogueClosed) return;

        if (choice.fullResponseText != null && choice.fullResponseText.Length > 0)
        {
            ignoreAdvanceInputFrame = Time.frameCount + 2;
            StartTyping(choice.fullResponseText);
        }
        else if (choice.nextDialogueBlock != null)
        {
            currentData = choice.nextDialogueBlock;
            currentLineIndex = 0;
            ignoreAdvanceInputFrame = Time.frameCount + 2;

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
        else
        {
            ignoreAdvanceInputFrame = Time.frameCount + 2;
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
                CloseDialogue();
                ShopUIController shop = shopUIController != null ? shopUIController : Object.FindFirstObjectByType<ShopUIController>();
                if (shop != null)
                {
                    shop.OpenShop();
                }
                else
                {
                    Debug.LogWarning("[DialogueManager] ShopUIController not found in scene.");
                }
                return true;

            case DialogueActionType.OpenWeaponChoice:
                InitialWeaponChoiceUI weaponUI = Object.FindFirstObjectByType<InitialWeaponChoiceUI>();
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

        if (talkButton != null)
        {
            talkButton.onClick.RemoveAllListeners();
            talkButton.gameObject.SetActive(false);
        }
        if (shopButton != null)
        {
            shopButton.onClick.RemoveAllListeners();
            shopButton.gameObject.SetActive(false);
        }
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.gameObject.SetActive(false);
        }
        if (choiceButtonContainer != null)
        {
            choiceButtonContainer.gameObject.SetActive(false);
        }
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        CloseDialogue();
    }

    private readonly Dictionary<EmotionType, Sprite> cachedPlayerPortraits = new Dictionary<EmotionType, Sprite>();

    private Sprite LoadPlayerPortraitFallback(EmotionType emotion)
    {
        if (cachedPlayerPortraits.TryGetValue(emotion, out Sprite cached) && cached != null)
        {
            return cached;
        }

        string resourcePath = "Portraits/Andrius/andrius_serio";
        switch (emotion)
        {
            case EmotionType.Feliz:
                resourcePath = "Portraits/Andrius/andriusfeli";
                break;
            case EmotionType.Triste:
                resourcePath = "Portraits/Andrius/tristandrius";
                break;
            case EmotionType.Enojado:
                resourcePath = "Portraits/Andrius/andrius_enojao";
                break;
            default:
                resourcePath = "Portraits/Andrius/andrius_serio";
                break;
        }

        Sprite loaded = Resources.Load<Sprite>(resourcePath);
        if (loaded == null)
        {
            loaded = Resources.Load<Sprite>("Portraits/Andrius/andrius_serio");
        }

        if (loaded != null)
        {
            cachedPlayerPortraits[emotion] = loaded;
        }

        return loaded;
    }

    private void UpdateSpeakerUI(SpeakerType speaker, EmotionType emotion)
    {
        currentSpeaker = speaker;

        bool isPlayer = speaker == SpeakerType.Jugador;

        if (isPlayer)
        {
            // Solo Andrius a la izquierda, la derecha queda totalmente vacía
            if (playerNameText != null)
            {
                playerNameText.gameObject.SetActive(true);
                playerNameText.text = "Andrius";
            }

            if (speakerNameText != null && speakerNameText != playerNameText)
            {
                speakerNameText.gameObject.SetActive(false);
                speakerNameText.text = string.Empty;
            }

            if (npcNameText != null && npcNameText != playerNameText)
            {
                npcNameText.gameObject.SetActive(false);
                npcNameText.text = string.Empty;
            }
        }
        else
        {
            // Habla NPC / Sistema a la derecha, la izquierda queda vacía
            if (playerNameText != null && playerNameText != speakerNameText)
            {
                playerNameText.gameObject.SetActive(false);
                playerNameText.text = string.Empty;
            }

            string targetName = (speaker == SpeakerType.Sistema)
                ? "Registro Ecológico"
                : ((npcNameText != null && !string.IsNullOrEmpty(npcNameText.text) && npcNameText != speakerNameText) ? npcNameText.text : "Opina");

            if (speakerNameText != null)
            {
                speakerNameText.gameObject.SetActive(true);
                speakerNameText.text = targetName;
            }

            if (npcNameText != null && npcNameText != speakerNameText)
            {
                npcNameText.gameObject.SetActive(true);
                npcNameText.text = targetName;
            }
        }

        Sprite portrait = GetPortrait(speaker, emotion);

        if (playerPortrait != null)
        {
            playerPortrait.gameObject.SetActive(isPlayer && portrait != null);
            if (isPlayer && portrait != null)
            {
                playerPortrait.sprite = portrait;
                playerPortrait.color = Color.white;
            }
        }

        if (npcPortrait != null)
        {
            npcPortrait.gameObject.SetActive(!isPlayer && portrait != null);
            if (!isPlayer && portrait != null)
            {
                npcPortrait.sprite = portrait;
                npcPortrait.color = Color.white;
            }
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
                if (portraitMappings[i].portraitSprite != null)
                    return portraitMappings[i].portraitSprite;
            }
        }

        for (int i = 0; i < portraitMappings.Count; i++)
        {
            if (portraitMappings[i].speaker == speaker)
            {
                if (portraitMappings[i].portraitSprite != null)
                    return portraitMappings[i].portraitSprite;
            }
        }

        if (speaker == SpeakerType.NPC)
        {
            if (defaultNpcPortrait != null) return defaultNpcPortrait;
            if (npcPortrait != null && npcPortrait.sprite != null) return npcPortrait.sprite;
        }
        else if (speaker == SpeakerType.Jugador)
        {
            if (defaultPlayerPortrait != null) return defaultPlayerPortrait;
            Sprite fallback = LoadPlayerPortraitFallback(emotion);
            if (fallback != null) return fallback;
            if (playerPortrait != null && playerPortrait.sprite != null) return playerPortrait.sprite;
        }

        return null;
    }

    private void SetDialogueRootActive(bool active)
    {
        if (dialogueRoot != null)
        {
            dialogueRoot.SetActive(active);

            if (active)
            {
                // Auto-sanar escala: si el objeto o su RectTransform se guardó con escala (0,0,0), forzar escala normal
                if (dialogueRoot.transform.localScale.sqrMagnitude < 0.01f)
                {
                    dialogueRoot.transform.localScale = Vector3.one;
                }

                if (dialoguePanelRect != null && dialoguePanelRect.localScale.sqrMagnitude < 0.01f)
                {
                    dialoguePanelRect.localScale = Vector3.one;
                }

                if (transform.localScale.sqrMagnitude < 0.01f)
                {
                    transform.localScale = Vector3.one;
                }

                // Asegurar orden de renderizado en Canvas para no quedar tapado por el fondo o game elements
                Canvas canvas = dialogueRoot.GetComponent<Canvas>() ?? GetComponentInParent<Canvas>() ?? GetComponent<Canvas>();
                if (canvas != null)
                {
                    if (canvas.transform.localScale.sqrMagnitude < 0.01f)
                    {
                        canvas.transform.localScale = Vector3.one;
                    }
                    canvas.overrideSorting = true;
                    canvas.sortingOrder = 200;
                }
            }
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
            speakerNameText.gameObject.SetActive(false);
        }

        if (npcNameText != null)
        {
            npcNameText.gameObject.SetActive(false);
        }

        if (playerNameText != null)
        {
            playerNameText.gameObject.SetActive(false);
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
