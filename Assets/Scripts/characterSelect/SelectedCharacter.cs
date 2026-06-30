using UnityEngine;

public class SelectedCharacter : MonoBehaviour
{
    public static SelectedCharacter Instance { get; private set; }

    [Header("Índice del personaje elegido")]
    public int selectedIndex = 0; // 0,1,2...

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject); // no se destruye al cambiar de escena
    }
}
