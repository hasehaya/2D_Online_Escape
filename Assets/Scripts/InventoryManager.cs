using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// プレイヤーの所持アイテムを管理するシングルトンクラス。
/// アイテムの追加・削除を行い、その変更をイベントを通じてUI等のリスナーに通知する役割を持つ。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();
    private InventorySlot _selectedSlot;

    public event Action OnInventoryChanged;
    public event Action<InventorySlot> OnSelectedSlotChanged;

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

    public bool TryAddItem(ItemData item)
    {
        InventorySlot newSlot = new InventorySlot(_slots.Count, item);
        _slots.Add(newSlot);
        NotifyInventoryChanged(newSlot);

        return true;
    }

    public bool TryRemoveItemAt(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return false;
        }

        InventorySlot removedSlot = _slots[index];
        _slots.RemoveAt(index);
        ReindexSlots(index);

        if (_selectedSlot == removedSlot)
        {
            _selectedSlot = null;
            NotifySelectedSlotChanged();
        }

        NotifyInventoryChanged(removedSlot);
        return true;
    }

    public bool TryRemoveItem(ItemData item)
    {
        int index = _slots.FindIndex(slot => slot.Item == item);
        if (index < 0)
        {
            return false;
        }

        return TryRemoveItemAt(index);
    }

    public bool HasItem(ItemData item)
    {
        return _slots.Exists(slot => slot.Item == item);
    }

    public bool TrySelectSlot(int index)
    {
        if (index < 0 || index >= _slots.Count)
        {
            return false;
        }

        InventorySlot newSelectedSlot = _slots[index];
        if (_selectedSlot == newSelectedSlot)
        {
            return true;
        }

        _selectedSlot = newSelectedSlot;
        NotifySelectedSlotChanged();
        return true;
    }

    public void ClearSelectedSlot()
    {
        if (_selectedSlot == null)
        {
            return;
        }

        _selectedSlot = null;
        NotifySelectedSlotChanged();
    }

    public IReadOnlyList<InventorySlot> GetSlots()
    {
        return _slots;
    }

    public InventorySlot GetSelectedSlot()
    {
        return _selectedSlot;
    }

    public ItemData GetSelectedItem()
    {
        return _selectedSlot != null ? _selectedSlot.Item : null;
    }

    public bool TryGetSlot(ItemData item, out InventorySlot slot)
    {
        int index = _slots.FindIndex(x => x.Item == item);
        if (index >= 0)
        {
            slot = _slots[index];
            return true;
        }

        slot = null;
        return false;
    }

    private void NotifyInventoryChanged(InventorySlot slot)
    {
        OnInventoryChanged?.Invoke();
        Debug.Log($"Inventory updated: [{slot.Index}] {slot.Item.itemName}");
    }

    private void NotifySelectedSlotChanged()
    {
        OnSelectedSlotChanged?.Invoke(_selectedSlot);
    }

    private void ReindexSlots(int startIndex)
    {
        for (int i = startIndex; i < _slots.Count; i++)
        {
            _slots[i].SetIndex(i);
        }
    }
}