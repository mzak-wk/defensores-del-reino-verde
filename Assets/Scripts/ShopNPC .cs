using UnityEngine;

public class ShopNPC : MonoBehaviour
{
    public ShopUIController shop;    // arrastra el ShopUIController desde el inspector

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (shop != null)
                shop.OpenShop();
        }
    }
}
