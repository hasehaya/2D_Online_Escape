using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリの1スロット表示を担当する View コンポーネント。
/// </summary>
public class InventorySlot : MonoBehaviour
{
    private static readonly Color SharedSlotColor = new Color(1f, 0.96f, 0.72f, 1f);
    private static readonly Color SharedSlotSelectedColor = new Color(0.1f, 0.65f, 1f, 0.65f);

    [SerializeField] private int _index;
    [SerializeField] private Image _backgroundImage;
    [SerializeField] private Image _selectedImage;
    [SerializeField] private Button _button;
    [NonSerialized] private Action<InventorySlot> _onTapped;
    [NonSerialized] private Color _defaultSelectedColor;

    private void Awake()
    {
        _defaultSelectedColor = _selectedImage.color;
    }

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

    public void SetShared(bool isShared)
    {
        _backgroundImage.color = isShared ? SharedSlotColor : Color.white;
        _selectedImage.color = isShared ? SharedSlotSelectedColor : _defaultSelectedColor;
    }

    public void SetItemIcon(Sprite icon)
    {
        _button.image.sprite = icon;
        _button.image.preserveAspect = true;
        _button.image.enabled = true;
    }

    public void ClearItemIcon()
    {
        _button.image.sprite = null;
        _button.image.enabled = true;
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
