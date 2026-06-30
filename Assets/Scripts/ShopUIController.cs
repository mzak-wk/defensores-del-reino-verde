using UnityEngine;
using UnityEngine.UI;

public class ShopUIController : MonoBehaviour
{
    [Header("Panel raíz de la tienda")]
    public GameObject shopPanel;   // arrastra el panel desde inspector

    [Header("Botón cerrar (opcional)")]
    public Button btnClose;

    // Propiedad pública nueva y compatibilidad con la antigua
    public bool IsOpen { get; private set; } = false;
    // Compatibilidad: algunos scripts podrían usar la versión en minúsculas
    public bool isOpen { get { return IsOpen; } }

    void Start()
    {
        if (shopPanel != null)
            shopPanel.SetActive(false);

        if (btnClose != null)
        {
            btnClose.onClick.RemoveAllListeners();
            btnClose.onClick.AddListener(CloseShop);
        }
    }

    // --- API "nueva" ---
    public void OpenShop()
    {
        if (IsOpen) return;
        shopPanel.SetActive(true);
        Time.timeScale = 0f;
        IsOpen = true;
    }

    public void CloseShop()
    {
        if (!IsOpen) return;
        shopPanel.SetActive(false);
        Time.timeScale = 1f;
        IsOpen = false;
    }

    // --- SOBRECARGAS / ALIAS para compatibilidad con código viejo ---
    // Llamadas sin parámetros (lo que usa ShopNPC originalmente)
    public void Open() => OpenShop();
    public void Close() => CloseShop();

    // Si por algún motivo algún script pasaba un argumento, lo aceptamos y lo ignoramos
    public void Open(GameObject _ignored) => OpenShop();
    public void Close(GameObject _ignored) => CloseShop();
    public void Open(object _ignored) => OpenShop();
    public void Close(object _ignored) => CloseShop();
}
