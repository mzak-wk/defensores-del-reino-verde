using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class CreditsManager : MonoBehaviour
{
    [Header("Scroll")]
    public RectTransform creditsText;
    public float scrollSpeed = 50f;
    public float startY = -400f;
    public float endY = 400f;

    [Header("UI")]
    public Button skipButton;   // botón “Volver/Saltar” (opcional)

    Coroutine scrollCo;
    bool loading;

    void Start()
    {
        if (skipButton) skipButton.onClick.AddListener(BackToMenu);
    }

    void OnEnable()
    {
        if (!creditsText)
        {
            Debug.LogWarning("[CreditsManager] 'creditsText' no asignado; no se iniciará el scroll.");
            return;
        }

        creditsText.anchoredPosition = new Vector2(creditsText.anchoredPosition.x, startY);

        if (scrollCo != null) StopCoroutine(scrollCo);
        scrollCo = StartCoroutine(ScrollCredits());
    }

    IEnumerator ScrollCredits()
    {
        while (creditsText && creditsText.anchoredPosition.y < endY)
        {
            creditsText.anchoredPosition += Vector2.up * scrollSpeed * Time.deltaTime;
            yield return null;
        }

        BackToMenu();
    }

    public void BackToMenu()
    {
        if (loading) return;
        loading = true;

        if (scrollCo != null) { StopCoroutine(scrollCo); scrollCo = null; }
        StartCoroutine(LoadMenuAtEndOfFrame());
    }

    IEnumerator LoadMenuAtEndOfFrame()
    {
        // Deja que UI/command buffers se limpien antes de cargar
        yield return new WaitForEndOfFrame();
        var op = SceneManager.LoadSceneAsync("MenuScene", LoadSceneMode.Single);
        yield return op;
    }

    // Para conectar desde el botón en el Inspector
    public void Btn_SalirMenu() => BackToMenu();
}
