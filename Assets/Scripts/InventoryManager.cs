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
    private const int MaxItemCount = 4;

    public static InventoryManager Instance { get; private set; }

    [Header("Item Database")] [SerializeField]
    private ItemDatabase _itemDatabase;

    [Header("Inventory UI")] [SerializeField]
    private Transform _itemSlotContainer;

    [SerializeField] private GameObject _itemSlotPrefab;

    [Header("Item Zoom UI")] [SerializeField]
    private ItemZoomPanel _itemZoomPanel;

    [Header("Debug Initial State")] [SerializeField, HideInInspector]
    private bool _useDebugInitialState;

    [SerializeField, HideInInspector] private List<ItemType> _debugInitialItems = new List<ItemType>();

    private readonly List<ItemType> _items = new List<ItemType>();
    private readonly List<InventorySlot> _slotViews = new List<InventorySlot>();

    private int _selectedIndex = -1;

    public event Action OnStateChanged;
    public event Action<int> OnSelectionChanged;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;

            if (_useDebugInitialState)
            {
                SetItems(_debugInitialItems);
            }
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

    public bool TryAddItem(ItemType item)
    {
        if (_items.Count >= MaxItemCount)
        {
            return false;
        }

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

        ItemType removedItem = _items[index];
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

    public bool TryRemoveItem(ItemType item)
    {
        int index = _items.FindIndex(x => x == item);
        if (index < 0)
        {
            return false;
        }

        return TryRemoveItemAt(index);
    }

    public bool HasItem(ItemType item)
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
        CloseItemZoom();
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

    public IReadOnlyList<ItemType> GetItems()
    {
        return _items;
    }

    public ItemType GetSelectedItem()
    {
        if (_selectedIndex < 0 || _selectedIndex >= _items.Count)
        {
            return ItemType.None;
        }

        return _items[_selectedIndex];
    }

    public bool TryGetItemIndex(ItemType item, out int index)
    {
        index = _items.FindIndex(x => x == item);
        return index >= 0;
    }

    public void CloseItemZoom()
    {
        _itemZoomPanel.Close();
    }

    private void RefreshUI()
    {
        _slotViews.Clear();

        foreach (Transform child in _itemSlotContainer)
        {
            if (child.TryGetComponent(out InventorySlot slotView))
            {
                _slotViews.Add(slotView);
            }
        }

        for (int i = _slotViews.Count; i < _items.Count; i++)
        {
            GameObject slotObject = Instantiate(_itemSlotPrefab, _itemSlotContainer);
            if (!slotObject.TryGetComponent(out InventorySlot slotView))
            {
                Debug.LogError("Inventory slot prefab is missing InventorySlot component.");
                Destroy(slotObject);
                continue;
            }

            _slotViews.Add(slotView);
        }

        for (int i = 0; i < _slotViews.Count; i++)
        {
            InventorySlot slotView = _slotViews[i];
            slotView.SetIndex(i);
            slotView.BindUI(OnSlotTapped);

            Button slotButton = slotView.Button;
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(slotView.Tap);

            if (i < _items.Count)
            {
                ItemType item = _items[i];
                Sprite icon;
                if (_itemDatabase.TryGetIcon(item, out icon))
                {
                    slotView.SetItemIcon(icon);
                }
                else
                {
                    slotView.ClearItemIcon();
                }

                slotButton.interactable = true;
            }
            else
            {
                slotView.ClearItemIcon();
                slotButton.interactable = false;
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
        ItemType selectedItem = GetSelectedItem();
        if (selectedItem == ItemType.None)
        {
            return;
        }

        Sprite icon;
        if (_itemDatabase.TryGetIcon(selectedItem, out icon))
        {
            _itemZoomPanel.Open(icon);
        }
        else
        {
            _itemZoomPanel.Close();
        }
    }

    private void NotifyStateChanged(int index, ItemType item)
    {
        OnStateChanged?.Invoke();
        Debug.Log($"Inventory updated: [{index}] {item}");
    }

    private void NotifySelectionChanged()
    {
        OnSelectionChanged?.Invoke(_selectedIndex);
    }

    public void SetItems(IReadOnlyList<ItemType> items)
    {
        _items.Clear();

        int count = Mathf.Min(items.Count, MaxItemCount);
        for (int i = 0; i < count; i++)
        {
            _items.Add(items[i]);
        }

        _selectedIndex = -1;
        RefreshUI();
        NotifySelectionChanged();
        CloseItemZoom();
    }
}