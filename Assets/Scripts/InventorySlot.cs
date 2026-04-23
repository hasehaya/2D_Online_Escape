using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリ内の1種類のアイテム状態を保持する。
/// </summary>
[Serializable]
public class InventorySlot
{
    [SerializeField] private int _index;
    [SerializeField] private ItemData _item;

    [NonSerialized] private Image _selectedImage;
    [NonSerialized] private Action<InventorySlot> _onTapped;

    public int Index => _index;
    public ItemData Item => _item;

    public InventorySlot(int index, ItemData item)
    {
        _index = index;
        _item = item;
    }

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void BindUI(Image selectedImage, Action<InventorySlot> onTapped)
    {
        _selectedImage = selectedImage;
        _onTapped = onTapped;
    }

    public void SetSelected(bool isSelected)
    {
        if (_selectedImage != null)
        {
            _selectedImage.enabled = isSelected;
        }
    }

    public void Tap()
    {
        _onTapped?.Invoke(this);
    }
}