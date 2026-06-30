using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerHealthRefiller : MonoBehaviour
{
    public static bool refillOnNextLoad = false;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    static void OnSceneLoaded(Scene s, LoadSceneMode mode)
    {
        if (!refillOnNextLoad) return;
        refillOnNextLoad = false;

        var player = GameObject.FindGameObjectWithTag("Player");
        if (player == null) return;

        // Intentos por SendMessage (no rompe si no existe el método)
        player.SendMessage("FillFull", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("SetFullHealth", SendMessageOptions.DontRequireReceiver);
        player.SendMessage("HealToMax", SendMessageOptions.DontRequireReceiver);
    }
}
