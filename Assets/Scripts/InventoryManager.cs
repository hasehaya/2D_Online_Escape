using System;
using System.Collections.Generic;
using Save;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリ機能全体を管理するシングルトンクラス。
/// データ管理、スロットUI更新、選択、拡大表示を担当する。
/// </summary>
public class InventoryManager : MonoBehaviour
{
    public static InventoryManager Instance { get; private set; }

    [Header("Inventory UI")] [SerializeField]
    private Transform _itemSlotContainer;

    [SerializeField] private GameObject _itemSlotPrefab;

    [Header("Item Zoom UI")] [SerializeField]
    private GameObject _itemZoomPanel;

    [SerializeField] private Image _itemZoomImage;

    private readonly List<ItemData> _items = new List<ItemData>();
    private readonly List<InventorySlot> _slotViews = new List<InventorySlot>();

    private int _selectedIndex = -1;

    public event Action OnStateChanged;
    public event Action<int> OnSelectionChanged;

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

    private void Start()
    {
        RefreshUI();
        CloseItemZoom();
    }

    public bool TryAddItem(ItemData item)
    {
        _items.Add(item);
        RefreshUI();
        NotifyStateChanged(_items.Count - 1, item);
        PairSaveCoordinator.RequestSaveIfAvailable();
        return true;
    }

    public bool TryRemoveItemAt(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        ItemData removedItem = _items[index];
        _items.RemoveAt(index);

        if (_selectedIndex == index)
        {
            _selectedIndex = -1;
            NotifySelectionChanged();
            CloseItemZoom();
        }
        else if (_selectedIndex > index)
        {
            _selectedIndex--;
            NotifySelectionChanged();
        }

        RefreshUI();
        NotifyStateChanged(index, removedItem);
        PairSaveCoordinator.RequestSaveIfAvailable();
        return true;
    }

    public bool TryRemoveItem(ItemData item)
    {
        int index = _items.FindIndex(x => x == item);
        if (index < 0)
        {
            return false;
        }

        return TryRemoveItemAt(index);
    }

    public bool HasItem(ItemData item)
    {
        return _items.Exists(x => x == item);
    }

    public bool TrySelectSlot(int index)
    {
        if (index < 0 || index >= _items.Count)
        {
            return false;
        }

        if (_selectedIndex == index)
        {
            return true;
        }

        _selectedIndex = index;
        UpdateSelectionVisual();
        NotifySelectionChanged();
        return true;
    }

    public void ClearSelectedSlot()
    {
        if (_selectedIndex < 0)
        {
            return;
        }

        _selectedIndex = -1;
        UpdateSelectionVisual();
        NotifySelectionChanged();
        CloseItemZoom();
    }

    public IReadOnlyList<ItemData> GetItems()
    {
        return _items;
    }

    public ItemData GetSelectedItem()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return null;
        }

        return _items[_selectedIndex];
    }

    public bool TryGetItemIndex(ItemData item, out int index)
    {
        index = _items.FindIndex(x => x == item);
        return index >= 0;
    }

    public void CloseItemZoom()
    {
        _itemZoomPanel.SetActive(false);
    }

    private void RefreshUI()
    {
        _slotViews.Clear();

        foreach (Transform child in _itemSlotContainer)
        {
            Destroy(child.gameObject);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            ItemData item = _items[i];
            GameObject slotObject = Instantiate(_itemSlotPrefab, _itemSlotContainer);
            if (!slotObject.TryGetComponent(out InventorySlot slotView))
            {
                Debug.LogError("Inventory slot prefab is missing InventorySlot component.");
                Destroy(slotObject);
                continue;
            }

            slotView.SetIndex(i);
            slotView.BindUI(OnSlotTapped);

            Button slotButton = slotView.Button;
            if (slotButton == null)
            {
                Debug.LogError("Inventory slot prefab is missing Button reference.");
                Destroy(slotObject);
                continue;
            }

            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(slotView.Tap);
            _slotViews.Add(slotView);

            Transform iconTransform = slotObject.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }

            Transform countTransform = slotObject.transform.Find("CountText");
            if (countTransform != null && countTransform.TryGetComponent(out Text countText))
            {
                countText.text = i.ToString();
                countText.enabled = true;
            }
        }

        if (_selectedIndex >= _items.Count)
        {
            _selectedIndex = -1;
        }

        UpdateSelectionVisual();
    }

    private void UpdateSelectionVisual()
    {
        foreach (InventorySlot slotView in _slotViews)
        {
            slotView.SetSelected(_selectedIndex >= 0 && slotView.Index == _selectedIndex);
        }
    }

    private void OnSlotTapped(InventorySlot tappedSlot)
    {
        if (_selectedIndex == tappedSlot.Index)
        {
            OpenSelectedItemZoom();
            return;
        }

        TrySelectSlot(tappedSlot.Index);
    }

    private void OpenSelectedItemZoom()
    {
        ItemData selectedItem = GetSelectedItem();
        if (selectedItem == null)
        {
            return;
        }

        _itemZoomImage.sprite = selectedItem.icon;
        _itemZoomImage.enabled = true;
        _itemZoomPanel.SetActive(true);
    }

    private void NotifyStateChanged(int index, ItemData item)
    {
        OnStateChanged?.Invoke();
        Debug.Log($"Inventory updated: [{index}] {item.itemName}");
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(_selectedIndex);
    }

    public void SetItems(IReadOnlyList<ItemData> items)
    {
        _items.Clear();

        for (int i = 0; i < items.Count; i++)
        {
            _items.Add(items[i]);
        }

        _selectedIndex = -1;
        RefreshUI();
        NotifySelectionChanged();
        CloseItemZoom();
    }
}