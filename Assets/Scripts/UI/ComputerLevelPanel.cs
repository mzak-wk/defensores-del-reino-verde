using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public enum LevelState { Locked, Unlocked, Selected }

[System.Serializable]
public class LevelCard
{
    public Button button;
    public Image stateImage;
    public Sprite spriteLocked;
    public Sprite spriteUnlocked;
    [Tooltip("Sprite opcional al pasar el cursor (Hover/Highlighted) cuando el nivel está desbloqueado.")]
    public Sprite spriteHighlighted;
    public Sprite spriteSelected;
    public int levelIndex;
    public string sceneName;
    [HideInInspector] public LevelState currentState;
}

public class ComputerLevelPanel : MonoBehaviour
{
    [Header("Navegación")]
    [SerializeField] private Button btnBackToMainPanel;

    [Header("Tarjetas de Nivel")]
    [SerializeField] private LevelCard[] levelCards;

    [Header("Botón Iniciar Nivel")]
    [SerializeField] private Button btnStartLevel;

    private LevelCard selectedCard;

    private void Awake()
    {
        if (btnBackToMainPanel != null)
        {
            btnBackToMainPanel.onClick.AddListener(OnBackToMainClicked);
        }

        if (btnStartLevel != null)
        {
            btnStartLevel.onClick.AddListener(OnStartLevelClicked);
            btnStartLevel.interactable = false;
        }
    }

    private void OnEnable()
    {
        InitializeLevelStates();
    }

    private void OnDisable()
    {
        ClearSelection();
    }

    private void InitializeLevelStates()
    {
        if (levelCards == null) return;

        int highestUnlocked = GameSession.Instance.highestLevelUnlocked;

        for (int i = 0; i < levelCards.Length; i++)
        {
            LevelCard card = levelCards[i];
            if (card.button == null) continue;

            if (card.levelIndex == 0)
            {
                card.currentState = LevelState.Unlocked;
            }
            else if ((card.levelIndex + 1) <= highestUnlocked)
            {
                card.currentState = LevelState.Unlocked;
            }
            else
            {
                card.currentState = LevelState.Locked;
            }

            UpdateCardVisual(card);
            RegisterCardListener(card);
        }
    }

    private void RegisterCardListener(LevelCard card)
    {
        card.button.onClick.RemoveAllListeners();
        card.button.onClick.AddListener(() => SelectLevelCard(card));
    }

    private void SelectLevelCard(LevelCard card)
    {
        if (card.currentState == LevelState.Locked) return;
        if (card.currentState == LevelState.Selected) return;

        if (selectedCard != null)
        {
            selectedCard.currentState = LevelState.Unlocked;
            UpdateCardVisual(selectedCard);
        }

        card.currentState = LevelState.Selected;
        UpdateCardVisual(card);
        selectedCard = card;

        if (btnStartLevel != null)
        {
            btnStartLevel.interactable = true;
        }
    }

    private void UpdateCardVisual(LevelCard card)
    {
        if (card.stateImage == null) return;

        switch (card.currentState)
        {
            case LevelState.Locked:
                card.stateImage.sprite = card.spriteLocked;
                card.button.interactable = false;
                break;

            case LevelState.Unlocked:
                card.stateImage.sprite = card.spriteUnlocked;
                card.button.interactable = true;

                // Configurar Sprite Swap en el componente Button para soportar Highlighted / Hover
                if (card.button != null && card.spriteHighlighted != null)
                {
                    card.button.transition = Selectable.Transition.SpriteSwap;
                    SpriteState spriteState = card.button.spriteState;
                    spriteState.highlightedSprite = card.spriteHighlighted;
                    spriteState.pressedSprite = card.spriteSelected != null ? card.spriteSelected : card.spriteHighlighted;
                    card.button.spriteState = spriteState;
                }
                break;

            case LevelState.Selected:
                card.stateImage.sprite = card.spriteSelected != null ? card.spriteSelected : card.spriteUnlocked;
                card.button.interactable = true;

                // En estado seleccionado, mantenemos el visual seleccionado
                if (card.button != null)
                {
                    card.button.transition = Selectable.Transition.SpriteSwap;
                    SpriteState spriteState = card.button.spriteState;
                    spriteState.highlightedSprite = card.spriteSelected;
                    spriteState.pressedSprite = card.spriteSelected;
                    card.button.spriteState = spriteState;
                }
                break;
        }
    }

    private void ClearSelection()
    {
        if (selectedCard != null)
        {
            selectedCard.currentState = LevelState.Unlocked;
            UpdateCardVisual(selectedCard);
            selectedCard = null;
        }

        if (btnStartLevel != null)
        {
            btnStartLevel.interactable = false;
        }
    }

    private void OnStartLevelClicked()
    {
        if (selectedCard == null) return;
        if (string.IsNullOrEmpty(selectedCard.sceneName))
        {
            Debug.LogError($"[ComputerLevelPanel] El nivel {selectedCard.levelIndex} no tiene nombre de escena configurado.");
            return;
        }

        int levelNumber = selectedCard.levelIndex + 1;
        PlayerPrefs.SetInt("SelectedHubLevel", levelNumber);
        PlayerPrefs.Save();

        Debug.Log($"[ComputerLevelPanel] Cargando nivel {levelNumber}: {selectedCard.sceneName}");
        SceneManager.LoadScene(selectedCard.sceneName);
    }

    private void OnBackToMainClicked()
    {
        gameObject.SetActive(false);
        if (HubUIManager.Instance != null)
        {
            HubUIManager.Instance.ShowMainMenu();
        }
    }
}
