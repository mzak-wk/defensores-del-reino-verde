using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class CharacterSelectUI : MonoBehaviour
{
    [Header("Panel de selección")]
    public GameObject selectionPanel;

    [Header("Feedback visual")]
    public Image[] characterFrames;
    public Color normalColor = Color.white;
    public Color selectedColor = Color.yellow;

    [Header("Escena de juego")]
    public string gameplaySceneName = "LevelHUB";

    // -1 = nada seleccionado aún
    int currentIndex = -1;
    bool hasChosen = false;

    void Start()
    {
        // IMPORTANTE: ya NO lo apagamos aquí.
        // Déjalo apagado en la escena desde el inspector y listo.
        //
        // if (selectionPanel != null)
        //     selectionPanel.SetActive(false);

        UpdateVisualSelection();
    }

    // llamado por el botón START del menú (o por MenuManager)
    public void OpenSelectionPanel()
    {
        hasChosen = false;
        currentIndex = -1;
        UpdateVisualSelection();

        if (selectionPanel != null)
            selectionPanel.SetActive(true);
    }

    // botón "X" o "Back"
    public void CloseSelectionPanel()
    {
        if (selectionPanel != null)
            selectionPanel.SetActive(false);
    }

    // botón "Back to Menu"
    public void BackToMenu()
    {
        SceneManager.LoadScene("MenuScene");   // pon aquí el nombre de tu escena de menú
    }

    // BOTONES de personajes (0,1,2…)
    public void SelectCharacter(int index)
    {
        currentIndex = Mathf.Clamp(index, 0, characterFrames.Length - 1);
        hasChosen = true;
        UpdateVisualSelection();
    }

    // BOTÓN Confirmar
    public void ConfirmSelection()
    {
        if (!hasChosen || currentIndex < 0)
        {
            Debug.Log("[CharacterSelectUI] Aún no has elegido personaje.");
            return;
        }

        if (SelectedCharacter.Instance != null)
            SelectedCharacter.Instance.selectedIndex = currentIndex;

        SceneManager.LoadScene(gameplaySceneName);
    }

    void UpdateVisualSelection()
    {
        if (characterFrames == null) return;

        for (int i = 0; i < characterFrames.Length; i++)
        {
            if (!characterFrames[i]) continue;

            bool isSelected = hasChosen && (i == currentIndex);
            characterFrames[i].color = isSelected ? selectedColor : normalColor;
        }
    }
}
