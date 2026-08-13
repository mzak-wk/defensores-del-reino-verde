using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "New Item Database", menuName = "Game/Items/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> allItems = new List<ItemData>();

    public IReadOnlyList<ItemData> AllItems => allItems;

    public ItemData GetItemById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        foreach (var item in allItems)
        {
            if (item != null && item.ItemId == id)
            {
                return item;
            }
        }

        Debug.LogWarning($"[ItemDatabase] No se encontró ítem con ID: {id}");
        return null;
    }
}
