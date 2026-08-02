using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HubUIManager : MonoBehaviour
{
    private const string SelectedHubLevelKey = "SelectedHubLevel";

    [Header("Canvas Root")]
    [SerializeField] private GameObject computerCanvasRoot;

    [Header("Sections")]
    [SerializeField] private GameObject weaponsPanel;
    [SerializeField] private ComputerWeaponPanel computerWeaponPanel;
    [SerializeField] private GameObject upgradesPanel;
    [SerializeField] private GameObject levelSelectorPanel;

    [Header("Level Loading")]
    public string[] levelSceneNames;

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

        ShowWeaponsSection();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void OpenComputer()
    {
        if (IsOpen)
        {
            return;
        }

        IsOpen = true;

        if (computerCanvasRoot != null)
        {
            computerCanvasRoot.SetActive(true);
        }

        computerWeaponPanel?.Refresh();
        ShowWeaponsSection();
    }

    public void CloseComputer()
    {
        if (!IsOpen)
        {
            return;
        }

        IsOpen = false;

        if (computerCanvasRoot != null)
        {
            computerCanvasRoot.SetActive(false);
        }

        OnComputerClosed?.Invoke();
    }

    public void ShowWeaponsSection()
    {
        ShowOnlyPanel(weaponsPanel);
    }

    public void ShowUpgradesSection()
    {
        ShowOnlyPanel(upgradesPanel);
    }

    public void ShowLevelSelectorSection()
    {
        ShowOnlyPanel(levelSelectorPanel);
    }

    public void SelectLevel(int levelIndex)
    {
        int sceneIndex = levelIndex - 1;

        if (levelSceneNames == null || sceneIndex < 0 || sceneIndex >= levelSceneNames.Length)
        {
            Debug.LogError($"[HubUIManager] Level {levelIndex} has not been configured in the inspector.");
            return;
        }

        string sceneName = levelSceneNames[sceneIndex];

        if (string.IsNullOrWhiteSpace(sceneName))
        {
            Debug.LogError($"[HubUIManager] Level {levelIndex} has an empty scene name in the inspector.");
            return;
        }

        PlayerPrefs.SetInt(SelectedHubLevelKey, levelIndex);
        PlayerPrefs.Save();

        SceneManager.LoadScene(sceneName);
    }

    private void ShowOnlyPanel(GameObject targetPanel)
    {
        SetPanelActive(weaponsPanel, targetPanel);
        SetPanelActive(upgradesPanel, targetPanel);
        SetPanelActive(levelSelectorPanel, targetPanel);
    }

    private void SetPanelActive(GameObject panel, GameObject targetPanel)
    {
        if (panel != null)
        {
            panel.SetActive(panel == targetPanel);
        }
    }
}

