using System;
using UnityEngine;
using UnityEngine.UI;

public class HubUIManager : MonoBehaviour
{
    private const string SelectedHubLevelKey = "SelectedHubLevel";

    [Header("Canvas Root")]
    [SerializeField] private GameObject computerCanvasRoot;

    [Header("Panel Principal (Menú Base)")]
    [SerializeField] private GameObject mainMenuPanel;
    [SerializeField] private Button btnGoToWeapons;
    [SerializeField] private Button btnGoToLevels;
    [SerializeField] private Button btnGoToItems;
    [SerializeField] private Button btnCloseComputer;

    [Header("Sub-Paneles")]
    [SerializeField] private GameObject weaponsPanel;
    [SerializeField] private ComputerWeaponPanel computerWeaponPanel;
    [SerializeField] private GameObject levelSelectorPanel;
    [SerializeField] private ComputerLevelPanel computerLevelPanel;
    [SerializeField] private GameObject itemsPanel;
    [SerializeField] private ComputerItemPanel computerItemPanel;

    public static HubUIManager Instance { get; private set; }

    public event Action OnComputerClosed;

    public bool IsOpen { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (computerCanvasRoot != null)
        {
            computerCanvasRoot.SetActive(false);
        }

        SetupButtons();
        ShowMainMenu();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    private void SetupButtons()
    {
        if (btnGoToWeapons != null)
        {
            btnGoToWeapons.onClick.AddListener(OnGoToWeaponsClicked);
        }

        if (btnGoToLevels != null)
        {
            btnGoToLevels.onClick.AddListener(OnGoToLevelsClicked);
        }

        if (btnGoToItems != null)
        {
            btnGoToItems.onClick.AddListener(OnGoToItemsClicked);
        }

        if (btnCloseComputer != null)
        {
            btnCloseComputer.onClick.AddListener(CloseComputer);
        }
    }

    public void OpenComputer()
    {
        if (IsOpen) return;

        IsOpen = true;

        if (computerCanvasRoot != null)
        {
            computerCanvasRoot.SetActive(true);
        }

        ShowMainMenu();
    }

    public void CloseComputer()
    {
        if (!IsOpen) return;

        IsOpen = false;

        if (computerCanvasRoot != null)
        {
            computerCanvasRoot.SetActive(false);
        }

        OnComputerClosed?.Invoke();
    }

    public void ShowMainMenu()
    {
        HideAllSubPanels();

        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(true);
        }
    }

    public void ShowSubPanel(GameObject subPanel)
    {
        if (mainMenuPanel != null)
        {
            mainMenuPanel.SetActive(false);
        }

        HideAllSubPanels();

        if (subPanel != null)
        {
            subPanel.SetActive(true);
        }
    }

    private void HideAllSubPanels()
    {
        SetPanelActive(weaponsPanel, false);
        SetPanelActive(levelSelectorPanel, false);
        SetPanelActive(itemsPanel, false);
    }

    private void SetPanelActive(GameObject panel, bool active)
    {
        if (panel != null)
        {
            panel.SetActive(active);
        }
    }

    private void OnGoToWeaponsClicked()
    {
        ShowSubPanel(weaponsPanel);
        computerWeaponPanel?.Refresh();
    }

    private void OnGoToLevelsClicked()
    {
        ShowSubPanel(levelSelectorPanel);
    }

    private void OnGoToItemsClicked()
    {
        ShowSubPanel(itemsPanel);
        computerItemPanel?.RefreshPanel();
    }
}
