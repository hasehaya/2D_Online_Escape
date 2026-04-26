using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリの1スロット表示を担当する View コンポーネント。
/// </summary>
public class InventorySlot : MonoBehaviour
{
    [SerializeField] private int _index;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Button _button;
    [NonSerialized] private Action<InventorySlot> _onTapped;

    public int Index => _index;
    public Button Button => _button;

    public void SetIndex(int index)
    {
        _index = index;
    }

    public void BindUI(Action<InventorySlot> onTapped)
    {
        _selectedImage.enabled = false;
        _selectedImage.raycastTarget = false;
        _onTapped = onTapped;
    }

    public void SetSelected(bool isSelected)
    {
        _selectedImage.enabled = isSelected;
    }

    public void Tap()
    {
        _onTapped?.Invoke(this);
    }
}