using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class ComputerLevelPanel : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Botones de los niveles. Índice 0 = Nivel 1, Índice 1 = Nivel 2, etc.")]
    public Button[] levelButtons;

    [Header("Configuración de Escenas")]
    [Tooltip("Nombres exactos de las escenas por cada nivel (ej: 'Level1', 'Bosque Oscuro'). Debe coincidir en tamaño con levelButtons.")]
    public string[] levelSceneNames;

    private void OnEnable()
    {
        if (GameSession.Instance == null) return;

        int highestUnlocked = GameSession.Instance.highestLevelUnlocked;

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null)
            {
                // El nivel 1 corresponde al índice 0 (1 = 0 + 1)
                if ((i + 1) <= highestUnlocked)
                {
                    levelButtons[i].interactable = true;
                }
                else
                {
                    levelButtons[i].interactable = false;
                }
            }
        }
    }

    /// <summary>
    /// Evento asignable en el OnClick de la UI de cada botón.
    /// Pasa el índice (0 para Nivel 1, 1 para Nivel 2...)
    /// </summary>
    public void LoadLevel(int levelIndex)
    {
        if (levelSceneNames == null || levelIndex < 0 || levelIndex >= levelSceneNames.Length)
        {
            Debug.LogError($"[ComputerLevelPanel] Índice de nivel ({levelIndex}) fuera de rango o levelSceneNames no configurado.");
            return;
        }

        string sceneToLoad = levelSceneNames[levelIndex];
        Debug.Log($"[ComputerLevelPanel] Cargando nivel: {sceneToLoad}");
        SceneManager.LoadScene(sceneToLoad);
    }
}
