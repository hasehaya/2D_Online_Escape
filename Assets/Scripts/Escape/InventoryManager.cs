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
    private const int MaxLocalItemCount = 3;
    private const int SharedSlotIndex = 0;

    public static InventoryManager Instance { get; private set; }

    [Header("Item Database")] [SerializeField]
    private ItemDatabase _itemDatabase;

    [Header("Inventory UI")] [SerializeField]
    private Transform _itemSlotContainer;

    [SerializeField] private GameObject _itemSlotPrefab;

    [SerializeField] private Transform _magicSackItemSlot;

    [Header("Item Zoom UI")] [SerializeField]
    private ItemZoomPanel _itemZoomPanel;

    [Header("Debug Initial State")] [SerializeField, HideInInspector]
    private bool _useDebugInitialState;

    [SerializeField, HideInInspector] private List<ItemType> _debugInitialItems = new List<ItemType>();

    private readonly List<ItemType> _items = new List<ItemType>();
    private readonly List<InventorySlot> _slotViews = new List<InventorySlot>();

    private int _selectedIndex = -1;
    private bool _hasMagicSack;
    private bool _sharedSlotUnlocked;
    private ItemType _sharedSlotItem = ItemType.None;
    private ItemType _pendingSharedTransferItem = ItemType.None;
    private ItemType _pendingSharedTakeItem = ItemType.None;

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
        PhotonNetwork.NetworkingClient.OpResponseReceived += OnOperationResponseReceived;
    }

    private void Start()
    {
        ReadSharedSlotFromRoom();
        PublishLocalMagicSackState();
        RefreshUI();
        CloseItemZoom();
    }

    private void OnDisable()
    {
        PhotonNetwork.RemoveCallbackTarget(this);
        PhotonNetwork.NetworkingClient.OpResponseReceived -= OnOperationResponseReceived;
    }

    public bool TryAddItem(ItemType item)
    {
        if (item == ItemType.None)
        {
            return false;
        }

        if (item == ItemType.MagicSack)
        {
            return TryAcquireMagicSack();
        }

        if (_items.Count < MaxLocalItemCount)
        {
            _items.Add(item);
            RefreshUI();
            NotifyStateChanged(ToSlotIndex(_items.Count - 1), item);

            PairSaveCoordinator.RequestSaveIfAvailable();
            return true;
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

        int localItemIndex = ToLocalItemIndex(index);
        if (localItemIndex < 0 || localItemIndex >= _items.Count)
        {
            return false;
        }

        ItemType removedItem = _items[localItemIndex];
        _items.RemoveAt(localItemIndex);

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

        return TryRemoveItemAt(ToSlotIndex(index));
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
        RefreshUI();
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
        if (!_hasMagicSack)
        {
            return _items;
        }

        List<ItemType> items = new List<ItemType>(_items) { ItemType.MagicSack };
        return items;
    }

    public ItemType GetSelectedItem()
    {
        if (!TryGetItemAt(_selectedIndex, out ItemType item))
        {
            return ItemType.None;
        }

        return item;
    }

    public bool TryRemoveSelectedItem()
    {
        if (_selectedIndex < 0 || !TryGetItemAt(_selectedIndex, out ItemType selectedItem))
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
            index = ToSlotIndex(index);
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

    public void SetVisible(bool isVisible)
    {
        _itemSlotContainer.gameObject.SetActive(isVisible);

        if (!isVisible)
        {
            CloseItemZoom();
        }
    }

    private void RefreshUI()
    {
        _magicSackItemSlot.gameObject.SetActive(_hasMagicSack);
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
            slotView.SetShared(IsSharedSlotIndex(i));

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

                slotButton.interactable = !IsSharedSlotIndex(i) || IsSharedSlotEnabled;
            }
            else
            {
                slotView.ClearItemIcon();
                slotButton.interactable = IsSharedSlotEnabled &&
                                          (IsSharedSlotIndex(i) || IsSharedSlotIndex(_selectedIndex));
            }
        }

        if (!TryGetItemAt(_selectedIndex, out ItemType selectedItem) || selectedItem == ItemType.None)
        {
            _selectedIndex = -1;
        }

        UpdateSelectionVisual();

        if (_itemSlotContainer is RectTransform containerRect)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(containerRect);
        }
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
        if (!IsSharedSlotIndex(tappedSlot.Index) && IsSharedSlotIndex(_selectedIndex) &&
            TryMoveSharedItemToLocalInventory())
        {
            return;
        }

        if (IsSharedSlotIndex(tappedSlot.Index) && _sharedSlotItem == ItemType.None)
        {
            TryMoveSelectedItemToSharedSlot();
            return;
        }

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

        bool containedLegacyMagicSack = false;
        for (int i = 0; i < items.Count; i++)
        {
            if (items[i] == ItemType.MagicSack)
            {
                containedLegacyMagicSack = true;
                continue;
            }

            if (_items.Count < MaxLocalItemCount)
            {
                _items.Add(items[i]);
            }
        }

        _selectedIndex = -1;
        _hasMagicSack = containedLegacyMagicSack;
        RefreshUI();
        NotifySelectionChanged();
        CloseItemZoom();

        if (publishSharedUnlock)
        {
            PublishLocalMagicSackState();
            TryEnableSharedSlotIfReady();
        }
    }

    public void ReconcileSharedSlotUnlock()
    {
        PublishLocalMagicSackState();
        TryEnableSharedSlotIfReady();
    }

    private bool TryAcquireMagicSack()
    {
        if (_hasMagicSack)
        {
            return false;
        }

        _hasMagicSack = true;
        PublishLocalMagicSackState();
        TryEnableSharedSlotIfReady();
        RefreshUI();
        NotifyStateChanged(SharedSlotIndex, ItemType.MagicSack);
        PairSaveCoordinator.RequestSaveIfAvailable();
        return true;
    }

    private bool IsSharedSlotEnabled => _sharedSlotUnlocked && HaveAllPlayersAcquiredMagicSack();

    private bool IsSharedSlotIndex(int index)
    {
        return _hasMagicSack && index == SharedSlotIndex;
    }

    private int GetVisibleSlotCount()
    {
        return MaxLocalItemCount + (_hasMagicSack ? 1 : 0);
    }

    private bool TryGetItemAt(int index, out ItemType item)
    {
        if (IsSharedSlotIndex(index))
        {
            item = _sharedSlotItem;
            return true;
        }

        int localItemIndex = ToLocalItemIndex(index);
        if (localItemIndex >= 0 && localItemIndex < _items.Count)
        {
            item = _items[localItemIndex];
            return true;
        }

        item = ItemType.None;
        return false;
    }

    private int ToLocalItemIndex(int slotIndex)
    {
        return _hasMagicSack ? slotIndex - 1 : slotIndex;
    }

    private int ToSlotIndex(int localItemIndex)
    {
        return _hasMagicSack ? localItemIndex + 1 : localItemIndex;
    }

    private void EnsureSharedSlotUnlocked()
    {
        if (!_sharedSlotUnlocked)
        {
            if (_selectedIndex >= 0)
            {
                _selectedIndex++;
                NotifySelectionChanged();
            }

            _sharedSlotUnlocked = true;
        }

        // MagicSack can be picked up after the inventory UI has already been rebuilt
        // (for example, after a scene transition). Always resync the visible slot
        // count even when the shared-slot state was already known.
        RefreshUI();

        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        Hashtable properties = new Hashtable
        {
            { PhotonRoomPropertyKeys.InventorySharedSlotUnlocked, true },
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)_sharedSlotItem }
        };
        PhotonNetwork.CurrentRoom.SetCustomProperties(properties);
    }

    private void PublishLocalMagicSackState()
    {
        if (!PhotonNetwork.InRoom)
        {
            return;
        }

        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { PhotonRoomPropertyKeys.InventoryHasMagicSack, _hasMagicSack }
        });
    }

    private void TryEnableSharedSlotIfReady()
    {
        if (!HaveAllPlayersAcquiredMagicSack())
        {
            RefreshUI();
            return;
        }

        EnsureSharedSlotUnlocked();
    }

    private bool HaveAllPlayersAcquiredMagicSack()
    {
        if (!PhotonNetwork.InRoom)
        {
            return _hasMagicSack;
        }

        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            return false;
        }

        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!player.CustomProperties.TryGetValue(PhotonRoomPropertyKeys.InventoryHasMagicSack,
                    out object hasMagicSackValue) ||
                !(hasMagicSackValue is bool hasMagicSack) || !hasMagicSack)
            {
                return false;
            }
        }

        return true;
    }

    private bool TryMoveSelectedItemToSharedSlot()
    {
        if (!IsSharedSlotEnabled || _sharedSlotItem != ItemType.None || IsSharedSlotIndex(_selectedIndex) ||
            !TryGetItemAt(_selectedIndex, out ItemType item) || item == ItemType.None)
        {
            return false;
        }

        if (!PhotonNetwork.InRoom)
        {
            int sourceIndex = _selectedIndex;
            ApplySharedSlotState(true, item);
            return TryRemoveItemAt(sourceIndex);
        }

        _pendingSharedTransferItem = item;
        Hashtable properties = new Hashtable
        {
            { PhotonRoomPropertyKeys.InventorySharedSlotUnlocked, true },
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)item }
        };
        Hashtable expectedProperties = new Hashtable
        {
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)ItemType.None }
        };

        bool requested = PhotonNetwork.CurrentRoom.SetCustomProperties(properties, expectedProperties);
        if (!requested)
        {
            _pendingSharedTransferItem = ItemType.None;
        }

        return requested;
    }

    private bool TryMoveSharedItemToLocalInventory()
    {
        if (!IsSharedSlotEnabled || _sharedSlotItem == ItemType.None || _items.Count >= MaxLocalItemCount)
        {
            return false;
        }

        ItemType item = _sharedSlotItem;
        if (!PhotonNetwork.InRoom)
        {
            ApplySharedSlotState(true, ItemType.None);
            _items.Add(item);
            RefreshUI();
            NotifyStateChanged(ToSlotIndex(_items.Count - 1), item);
            PairSaveCoordinator.RequestSaveIfAvailable();
            return true;
        }

        _pendingSharedTakeItem = item;
        Hashtable properties = new Hashtable
        {
            { PhotonRoomPropertyKeys.InventorySharedSlotUnlocked, true },
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)ItemType.None }
        };
        Hashtable expectedProperties = new Hashtable
        {
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)item }
        };

        bool requested = PhotonNetwork.CurrentRoom.SetCustomProperties(properties, expectedProperties);
        if (!requested)
        {
            _pendingSharedTakeItem = ItemType.None;
        }

        return requested;
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
            { PhotonRoomPropertyKeys.InventorySharedSlotUnlocked, true },
            { PhotonRoomPropertyKeys.InventorySharedSlotItem, (int)item }
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
        if (properties.TryGetValue(PhotonRoomPropertyKeys.InventorySharedSlotUnlocked, out object unlockedValue) &&
            unlockedValue is bool unlockedBool)
        {
            unlocked = unlockedBool;
        }

        if (properties.TryGetValue(PhotonRoomPropertyKeys.InventorySharedSlotItem, out object itemValue))
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
        bool sharedSlotWasUnlocked = _sharedSlotUnlocked;
        bool changed = _sharedSlotUnlocked != unlocked || _sharedSlotItem != item;
        _sharedSlotUnlocked = unlocked;
        _sharedSlotItem = item;

        if (!changed)
        {
            return;
        }

        if (!sharedSlotWasUnlocked && unlocked && _selectedIndex >= 0)
        {
            _selectedIndex++;
            NotifySelectionChanged();
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
        if (!propertiesThatChanged.ContainsKey(PhotonRoomPropertyKeys.InventorySharedSlotUnlocked) &&
            !propertiesThatChanged.ContainsKey(PhotonRoomPropertyKeys.InventorySharedSlotItem))
        {
            return;
        }

        bool unlocked = _sharedSlotUnlocked;
        ItemType item = _sharedSlotItem;
        ReadSharedSlotProperties(propertiesThatChanged, ref unlocked, ref item);
        ApplySharedSlotState(unlocked, item);
        NotifyStateChanged(SharedSlotIndex, item);

        if (_pendingSharedTransferItem != ItemType.None && item == _pendingSharedTransferItem)
        {
            ItemType transferredItem = _pendingSharedTransferItem;
            _pendingSharedTransferItem = ItemType.None;
            int localItemIndex = _items.FindIndex(x => x == transferredItem);
            if (localItemIndex >= 0)
            {
                TryRemoveItemAt(ToSlotIndex(localItemIndex));
            }
        }

        if (_pendingSharedTakeItem != ItemType.None && item == ItemType.None)
        {
            ItemType takenItem = _pendingSharedTakeItem;
            _pendingSharedTakeItem = ItemType.None;
            if (_items.Count < MaxLocalItemCount)
            {
                _items.Add(takenItem);
                RefreshUI();
                NotifyStateChanged(ToSlotIndex(_items.Count - 1), takenItem);
                PairSaveCoordinator.RequestSaveIfAvailable();
            }
        }
    }

    private void OnOperationResponseReceived(OperationResponse response)
    {
        if ((_pendingSharedTransferItem == ItemType.None && _pendingSharedTakeItem == ItemType.None) ||
            response.OperationCode != OperationCode.SetProperties ||
            response.ReturnCode != ErrorCode.InvalidOperation)
        {
            return;
        }

        _pendingSharedTransferItem = ItemType.None;
        _pendingSharedTakeItem = ItemType.None;
        ReadSharedSlotFromRoom();
        Debug.Log("Shared inventory slot changed before the item transfer completed; no local inventory change was made.");
    }

    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        TryEnableSharedSlotIfReady();
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshUI();
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        if (!changedProps.ContainsKey(PhotonRoomPropertyKeys.InventoryHasMagicSack))
        {
            return;
        }

        TryEnableSharedSlotIfReady();
    }
}
