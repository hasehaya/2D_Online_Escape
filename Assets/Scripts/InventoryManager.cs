using System;
using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Save;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インベントリ機能全体を管理するシングルトンクラス。
/// データ管理、スロットUI更新、選択、拡大表示を担当する。
/// </summary>
public class InventoryManager : MonoBehaviour, IInRoomCallbacks
{
    private const int MaxItemCount = 4;
    private const string SharedSlotUnlockedKey = "Inventory.SharedSlot.Unlocked";
    private const string SharedSlotItemKey = "Inventory.SharedSlot.Item";

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
    private bool _sharedSlotUnlocked;
    private ItemType _sharedSlotItem = ItemType.None;

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

    private void OnEnable()
    {
        PhotonNetwork.AddCallbackTarget(this);
    }

    private void Start()
    {
        ReadSharedSlotFromRoom();
        RefreshUI();
        CloseItemZoom();
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
    }

    public bool TryAddItem(ItemType item)
    {
        if (item == ItemType.None)
        {
            return false;
        }

        if (_items.Count < MaxItemCount)
        {
            _items.Add(item);
            RefreshUI();
            NotifyStateChanged(_items.Count - 1, item);

            if (item == ItemType.MagicSack)
            {
                EnsureSharedSlotUnlocked();
            }

            PairSaveCoordinator.RequestSaveIfAvailable();
            return true;
        }

        if (IsSharedSlotEnabled && _sharedSlotItem == ItemType.None)
        {
            return TrySetSharedSlotItem(item);
        }

        return false;
    }

    public bool TryRemoveItemAt(int index)
    {
        if (IsSharedSlotIndex(index))
        {
            if (_sharedSlotItem == ItemType.None)
            {
                return false;
            }

            return TrySetSharedSlotItem(ItemType.None);
        }

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
        if (item == ItemType.None)
        {
            return false;
        }

        if (_selectedIndex >= 0 && GetSelectedItem() == item)
        {
            return TryRemoveItemAt(_selectedIndex);
        }

        int index = _items.FindIndex(x => x == item);
        if (index < 0)
        {
            if (IsSharedSlotEnabled && _sharedSlotItem == item)
            {
                return TrySetSharedSlotItem(ItemType.None);
            }

            return false;
        }

        return TryRemoveItemAt(index);
    }

    public bool HasItem(ItemType item)
    {
        if (item == ItemType.None)
        {
            return false;
        }

        return _items.Exists(x => x == item) || (IsSharedSlotEnabled && _sharedSlotItem == item);
    }

    public bool TrySelectSlot(int index)
    {
        if (!TryGetItemAt(index, out ItemType item) || item == ItemType.None)
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
        if (!TryGetItemAt(_selectedIndex, out ItemType item))
        {
            return ItemType.None;
        }

        return item;
    }

    public bool CanDiscardItem(ItemType item)
    {
        return _itemDatabase != null && _itemDatabase.CanDiscard(item);
    }

    public bool CanDiscardSelectedItem()
    {
        return CanDiscardItem(GetSelectedItem());
    }

    public bool TryDiscardSelectedItem()
    {
        if (_selectedIndex < 0 || !TryGetItemAt(_selectedIndex, out ItemType selectedItem))
        {
            return false;
        }

        if (!CanDiscardItem(selectedItem))
        {
            return false;
        }

        return TryRemoveItemAt(_selectedIndex);
    }

    public bool TryGetItemIndex(ItemType item, out int index)
    {
        if (item == ItemType.None)
        {
            index = -1;
            return false;
        }

        index = _items.FindIndex(x => x == item);
        if (index >= 0)
        {
            return true;
        }

        if (IsSharedSlotEnabled && _sharedSlotItem == item)
        {
            index = SharedSlotIndex;
            return true;
        }

        return false;
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

        int visibleSlotCount = GetVisibleSlotCount();
        for (int i = _slotViews.Count; i < visibleSlotCount; i++)
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

            if (TryGetItemAt(i, out ItemType item) && item != ItemType.None)
            {
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

        if (!TryGetItemAt(_selectedIndex, out ItemType selectedItem) || selectedItem == ItemType.None)
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

    public void SetItems(IReadOnlyList<ItemType> items, bool publishSharedUnlock = true)
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

        if (publishSharedUnlock && _items.Contains(ItemType.MagicSack))
        {
            EnsureSharedSlotUnlocked();
        }
    }

    public void ReconcileSharedSlotUnlock()
    {
        if (_items.Contains(ItemType.MagicSack))
        {
            EnsureSharedSlotUnlocked();
        }
    }

    private bool IsSharedSlotEnabled => _sharedSlotUnlocked || _items.Contains(ItemType.MagicSack);
    private int SharedSlotIndex => _items.Count;

    private bool IsSharedSlotIndex(int index)
    {
        return IsSharedSlotEnabled && index == SharedSlotIndex;
    }

    private int GetVisibleSlotCount()
    {
        return _items.Count + (IsSharedSlotEnabled ? 1 : 0);
    }

    private bool TryGetItemAt(int index, out ItemType item)
    {
        if (index >= 0 && index < _items.Count)
        {
            item = _items[index];
            return true;
        }

        if (IsSharedSlotIndex(index))
        {
            item = _sharedSlotItem;
            return true;
        }

        item = ItemType.None;
        return false;
    }

    private void EnsureSharedSlotUnlocked()
    {
        if (!_sharedSlotUnlocked)
        {
            _sharedSlotUnlocked = true;
            RefreshUI();
        }

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Hashtable properties = new Hashtable { { SharedSlotUnlockedKey, true } };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private bool TrySetSharedSlotItem(ItemType item)
    {
        if (!IsSharedSlotEnabled)
        {
            return false;
        }

        if (!PhotonNetwork.InRoom)
        {
            ApplySharedSlotState(_sharedSlotUnlocked, item);
            NotifyStateChanged(SharedSlotIndex, item);
            PairSaveCoordinator.RequestSaveIfAvailable();
            return true;
        }

        Hashtable properties = new Hashtable
        {
            { SharedSlotUnlockedKey, true },
            { SharedSlotItemKey, (int)item }
        };
        return PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private void ReadSharedSlotFromRoom()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        bool unlocked = false;
        ItemType item = ItemType.None;
        ReadSharedSlotProperties(PhotonNetwork.CurrentRoom.CustomProperties, ref unlocked, ref item);
        ApplySharedSlotState(unlocked, item);
    }

    private void ReadSharedSlotProperties(Hashtable properties, ref bool unlocked, ref ItemType item)
    {
        if (properties.TryGetValue(SharedSlotUnlockedKey, out object unlockedValue) &&
            unlockedValue is bool unlockedBool)
        {
            unlocked = unlockedBool;
        }

        if (properties.TryGetValue(SharedSlotItemKey, out object itemValue))
        {
            int itemTypeId;
            try
            {
                itemTypeId = Convert.ToInt32(itemValue);
            }
            catch
            {
                itemTypeId = (int)ItemType.None;
            }

            item = Enum.IsDefined(typeof(ItemType), itemTypeId) ? (ItemType)itemTypeId : ItemType.None;
        }
    }

    private void ApplySharedSlotState(bool unlocked, ItemType item)
    {
        bool changed = _sharedSlotUnlocked != unlocked || _sharedSlotItem != item;
        _sharedSlotUnlocked = unlocked;
        _sharedSlotItem = item;

        if (!changed)
        {
            return;
        }

        if (_selectedIndex == SharedSlotIndex)
        {
            CloseItemZoom();

            if (!IsSharedSlotEnabled || _sharedSlotItem == ItemType.None)
            {
                _selectedIndex = -1;
                NotifySelectionChanged();
            }
        }

        RefreshUI();
    }

    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        if (!propertiesThatChanged.ContainsKey(SharedSlotUnlockedKey) &&
            !propertiesThatChanged.ContainsKey(SharedSlotItemKey))
        {
            return;
        }

        bool unlocked = _sharedSlotUnlocked;
        ItemType item = _sharedSlotItem;
        ReadSharedSlotProperties(propertiesThatChanged, ref unlocked, ref item);
        ApplySharedSlotState(unlocked, item);
        NotifyStateChanged(SharedSlotIndex, item);
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
    }
}