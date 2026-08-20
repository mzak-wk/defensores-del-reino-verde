using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Playables;
using UnityEngine.Video;

public class CinematicSceneController : MonoBehaviour
{
    [Header("Escena Destino")]
    [SerializeField] private string nextSceneName = "LevelHUB";

    [Header("Referencias de Cinemática")]
    [SerializeField] private PlayableDirector playableDirector;
    [SerializeField] private VideoPlayer videoPlayer;

    [Header("Configuración")]
    [SerializeField] private float startCooldown = 0.5f;

    private AsyncOperation asyncLoad;
    private bool cinematicFinished;
    private float timer;

    private void Start()
    {
        asyncLoad = SceneManager.LoadSceneAsync(nextSceneName);
        asyncLoad.allowSceneActivation = false;

        if (playableDirector != null)
        {
            playableDirector.stopped += OnPlayableDirectorStopped;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached += OnVideoPlayerFinished;
        }
    }

    private void Update()
    {
        if (timer < startCooldown)
        {
            timer += Time.deltaTime;
            return;
        }

        if (asyncLoad.progress < 0.9f)
        {
            return;
        }

        if (Input.anyKeyDown || cinematicFinished)
        {
            asyncLoad.allowSceneActivation = true;
        }
    }

    private void OnPlayableDirectorStopped(PlayableDirector director)
    {
        cinematicFinished = true;
    }

    private void OnVideoPlayerFinished(VideoPlayer vp)
    {
        cinematicFinished = true;
    }

    private void OnDestroy()
    {
        if (playableDirector != null)
        {
            playableDirector.stopped -= OnPlayableDirectorStopped;
        }

        if (videoPlayer != null)
        {
            videoPlayer.loopPointReached -= OnVideoPlayerFinished;
        }
    }
}
