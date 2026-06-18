using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// アイテム定義の一覧を保持するScriptableObject。
/// Inspectorで List を増やし、各 ItemData をインライン編集する。
/// </summary>
[CreateAssetMenu(fileName = "New Item Database", menuName = "EscapeGame/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [SerializeField] private List<ItemData> _items = new List<ItemData>();

    public IReadOnlyList<ItemData> Items => _items;

    public bool TryGetItem(ItemType itemType, out ItemData item)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            ItemData candidate = _items[i];
            if (candidate != null && candidate.itemType == itemType)
            {
                item = candidate;
                return true;
            }
        }

        item = null;
        return false;
    }

    public bool TryGetIcon(ItemType itemType, out Sprite icon)
    {
        ItemData item;
        if (TryGetItem(itemType, out item) && item.icon != null)
        {
            icon = item.icon;
            return true;
        }

        icon = null;
        return false;
    }

    public bool CanDiscard(ItemType itemType)
    {
        if (itemType == ItemType.None)
        {
            return false;
        }

        ItemData item;
        return TryGetItem(itemType, out item) && item.canDiscard;
    }
}