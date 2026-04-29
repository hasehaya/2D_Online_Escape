using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// インベントリスロットのクリックと選択表示を担当する。
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    private InventorySlot _slot;
    private Action<InventorySlot> _onClicked;
    private Image _selectedImage;

    public InventorySlot Slot => _slot;

    public void Initialize(InventorySlot slot, Image selectedImage, Action<InventorySlot> onClicked)
    {
        _slot = slot;
        _selectedImage = selectedImage;
        _onClicked = onClicked;
    }

    public void SetSelected(bool isSelected)
    {
        if (_selectedImage != null)
        {
            _selectedImage.enabled = isSelected;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        _onClicked?.Invoke(_slot);
    }
}

