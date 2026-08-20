using UnityEngine;

/// <summary>
/// Colocar este componente en el GameObject raíz del HUD de la escena (HUD_Root).
/// Auto-registra el HUD con el UIManager al cargar la escena.
/// </summary>
public class HUDRoot : MonoBehaviour
{
    private void Awake()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.RegisterHUD(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (UIManager.Instance != null)
        {
            UIManager.Instance.UnregisterHUD();
        }
    }
}
