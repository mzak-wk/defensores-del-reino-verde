using UnityEngine;
using UnityEngine.SceneManagement;

public class PausarJuego : MonoBehaviour
{
    [Header("Paneles")]
    [Tooltip("Panel principal del menú de pausa")]
    [SerializeField] private GameObject panelPausa;

    [Tooltip("Panel de opciones/configuración")]
    [SerializeField] private GameObject panelOpciones;

    [Header("Escenas de Salida")]
    [SerializeField] private string sceneHubName = "LevelHUB";
    [SerializeField] private string sceneMenuName = "MenuScene";

    public bool juegoPausado { get; private set; } = false;

    private void Awake()
    {
        if (panelPausa != null) panelPausa.SetActive(false);
        if (panelOpciones != null) panelOpciones.SetActive(false);
        juegoPausado = false;
    }

    private void Update()
    {
        if (UIManager.Instance != null && UIManager.Instance.IsGameOver)
            return;

        // Si no está pausado y no se puede pausar (diálogo, tienda u otro overlay activo), ignoramos
        if (!juegoPausado && UIManager.Instance != null && !UIManager.Instance.CanPause)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            // Si las opciones están abiertas, Escape regresa primero al menú de pausa
            if (panelOpciones != null && panelOpciones.activeSelf)
            {
                Btn_CerrarOpciones();
            }
            else if (juegoPausado)
            {
                Btn_Reanudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    // ==========================================
    // MÉTODOS PÚBLICOS PARA EVENTOS DE UI (OnClick)
    // ==========================================

    public void Btn_Reanudar()
    {
        if (UIManager.Instance != null)
            UIManager.Instance.UnregisterOverlay("Pause");

        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;
    }

    public void Pausar()
    {
        if (panelPausa != null)
            panelPausa.SetActive(true);

        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        Time.timeScale = 0f;
        AudioListener.pause = true;
        juegoPausado = true;

        if (UIManager.Instance != null)
            UIManager.Instance.RegisterOverlay("Pause", () => Btn_Reanudar());
    }

    public void Btn_Opciones()
    {
        if (panelPausa != null)
            panelPausa.SetActive(false);

        if (panelOpciones != null)
            panelOpciones.SetActive(true);
    }

    public void Btn_CerrarOpciones()
    {
        if (panelOpciones != null)
            panelOpciones.SetActive(false);

        if (panelPausa != null)
            panelPausa.SetActive(true);
    }

    public void Btn_SalirHUB()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ForceCloseAllOverlays();

        SceneManager.LoadScene(sceneHubName);
    }

    public void Btn_SalirMenu()
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        juegoPausado = false;

        if (UIManager.Instance != null)
            UIManager.Instance.ForceCloseAllOverlays();

        SceneManager.LoadScene(sceneMenuName);
    }
}
