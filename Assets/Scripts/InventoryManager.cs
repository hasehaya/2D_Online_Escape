using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// プレイヤーの所持アイテムを管理するシングルトンクラス。
/// アイテムの追加・削除を行い、その変更をイベントを通じてUI等のリスナーに通知する役割を持つ。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private List<ItemData> items = new List<ItemData>();
    
    // UI側で表示を更新するために、インベントリの変更を通知するイベント
    public event Action OnInventoryChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void AddItem(ItemData item)
    {
        // 重複取得を防ぐ
        if (!items.Contains(item))
        {
            items.Add(item);
            OnInventoryChanged?.Invoke();
            Debug.Log($"Item added: {item.itemName}");
        }
    }

    public void RemoveItem(ItemData item)
    {
        if (items.Contains(item))
        {
            items.Remove(item);
            OnInventoryChanged?.Invoke();
            Debug.Log($"Item removed: {item.itemName}");
        }
    }

    public bool HasItem(ItemData item)
    {
        return items.Contains(item);
    }

    public List<ItemData> GetItems()
    {
        return items;
    }
}
