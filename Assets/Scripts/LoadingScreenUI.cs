using UnityEngine;
using UnityEngine.UI;

public class LoadingScreenUI : MonoBehaviour
{
    [Header("Referencias UI")]
    public GameObject loadingContainer;
    public Slider progressBar;

    private void Awake()
    {
        // Asegurarse de que esté oculto al inicio (el generador lo mostrará)
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(false);
        }
    }

    public void Show()
    {
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(true);
        }
    }

    public void Hide()
    {
        if (loadingContainer != null)
        {
            loadingContainer.SetActive(false);
        }
    }

    public void UpdateProgress(float progress)
    {
        if (progressBar != null)
        {
            progressBar.value = Mathf.Clamp01(progress);
        }
    }
}
