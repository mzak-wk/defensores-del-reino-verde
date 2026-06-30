using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
public class IntroManager : MonoBehaviour
{
    [Header("Referencias")]
    public VideoPlayer videoPlayer; // Componente Video Player

    void Start()
    {
        // Cuando el video termine
        videoPlayer.loopPointReached += OnVideoEnd;
    }

    void Update()
    {
        // Si el jugador presiona ESC, salta la intro
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            SkipIntro();
        }
    }

    void SkipIntro()
    {
        LoadMenu();
    }

    void OnVideoEnd(VideoPlayer vp)
    {
        LoadMenu();
    }

    void LoadMenu()
    {
        SceneManager.LoadScene("MenuScene"); // Usa el nombre exacto de tu escena de menú
    }
}
