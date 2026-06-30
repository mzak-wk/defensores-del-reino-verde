using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MenuManager : MonoBehaviour
{
    [Header("Botones del Menú")]
    public Button startButton;
    public Button creditsButton;
    public Button exitButton;

    [Header("Panel de selección de personaje")]
    public GameObject characterSelectPanel;

    [Header("Escenas")]
    public string gameplaySceneName = "Level1Scene";
    public string creditsSceneName = "CreditsScene";

    void Start()
    {
        startButton.onClick.AddListener(OnStartClicked);
        creditsButton.onClick.AddListener(OpenCredits);

        if (exitButton != null)
            exitButton.onClick.AddListener(ExitGame);

        // Ocultar selección de personaje al iniciar
        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(false);
    }

    // ⬅️ ESTA ES LA FUNCIÓN QUE NECESITAS
    public void OnStartClicked()
    {
        Debug.Log("Start presionado → Abriendo selección de personaje");

        if (characterSelectPanel != null)
            characterSelectPanel.SetActive(true);
    }

    public void OpenCredits()
    {
        SceneManager.LoadScene(creditsSceneName);
    }

    public void ExitGame()
    {
        Application.Quit();
        Debug.Log("Cerrando juego (solo en build)");
    }
}
