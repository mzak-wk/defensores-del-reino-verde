using UnityEngine;

public class PausarJuego : MonoBehaviour
{
    [Tooltip("Panel que se mostrará cuando el juego esté en pausa")]
    public GameObject menuPausa;
    public bool juegoPausado = false;

    private void Update()
    {
        // Si la tienda está abierta, ignoramos la pausa normal
        var shop = FindAnyObjectByType<ShopUIController>();
        if (shop != null && shop.IsOpen)
            return;

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (juegoPausado)
            {
                Renaudar();
            }
            else
            {
                Pausar();
            }
        }
    }

    public void Renaudar()
    {
        if (menuPausa != null)
            menuPausa.SetActive(false);
        Time.timeScale = 1f;
        juegoPausado = false;
    }
    
    public void Pausar() 
    {
        if (menuPausa != null)
            menuPausa.SetActive(true);
        Time.timeScale = 0f;
        juegoPausado = true;
    }
}
