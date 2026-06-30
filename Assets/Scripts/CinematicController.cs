using UnityEngine;
using UnityEngine.Video; // Necesario para controlar el video
using UnityEngine.SceneManagement;

public class CinematicController : MonoBehaviour
{
    [Header("Configuración")]
    public VideoPlayer videoPlayer; // Arrastra el componente aquí
    public string creditsSceneName = "CreditsScene"; // Nombre exacto de tu escena de créditos

    void Start()
    {
        // Nos aseguramos de que haya un video asignado
        if (videoPlayer == null)
            videoPlayer = GetComponent<VideoPlayer>();

        // Suscribirse al evento: Cuando el video termine (llegue al punto de loop/fin)
        // llamará automáticamente a la función OnVideoFinished
        videoPlayer.loopPointReached += OnVideoFinished;
    }

    void Update()
    {
        // Opción para saltar la cinemática
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            LoadCredits();
        }
    }

    // Esta función se llama automáticamente cuando el video termina
    void OnVideoFinished(VideoPlayer vp)
    {
        LoadCredits();
    }

    void LoadCredits()
    {
        Debug.Log("Cinemática terminada o saltada. Cargando créditos...");
        
        // Verificación de seguridad por si olvidaste añadir la escena al Build Settings
        if (Application.CanStreamedLevelBeLoaded(creditsSceneName))
        {
            SceneManager.LoadScene(creditsSceneName);
        }
        else
        {
            Debug.LogError($"[CinematicController] La escena '{creditsSceneName}' no se encuentra en Build Settings.");
        }
    }
}