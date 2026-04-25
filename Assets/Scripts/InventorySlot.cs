using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリ内の1種類のアイテム状態を保持する。
/// </summary>
[Serializable]
public class InventorySlot : MonoBehaviour
{
    [SerializeField] private int _index;
    [SerializeField] private ItemData _item;
    [SerializeField] private Image _selectedImage;
    [NonSerialized] private Action<InventorySlot> _onTapped;

    public int Index => _index;
    public ItemData Item => _item;
    public Image SelectedImage => _selectedImage;

    public InventorySlot(int index, ItemData item)
    {
        _index = index;
        _item = item;
    }

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void BindUI(Action<InventorySlot> onTapped)
    {
        if (_selectedImage != null)
        {
            _selectedImage.enabled = false;
            _selectedImage.raycastTarget = false;
        }

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