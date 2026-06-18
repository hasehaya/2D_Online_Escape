using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 選択中のアイテムを捨てるためのゴミ箱オブジェクト。
/// </summary>
public class TrashObject : InteractableObject
{
    [Header("Events")] [SerializeField] private UnityEvent _onItemDiscarded;
    [SerializeField] private UnityEvent _onCannotDiscard;
    [SerializeField] private UnityEvent _onNoItemSelected;

    protected override void Interact()
    {
        base.Interact();

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("[TrashObject] InventoryManager instance is null.");
            return;
        }

        ItemType selectedItem = InventoryManager.Instance.GetSelectedItem();
        if (selectedItem == ItemType.None)
        {
            Debug.Log("[TrashObject] No item selected.");
            _onNoItemSelected?.Invoke();
            return;
        }

        if (!InventoryManager.Instance.TryDiscardSelectedItem())
        {
            Debug.Log($"[TrashObject] {selectedItem} cannot be discarded.");
            _onCannotDiscard?.Invoke();
            return;
        }

        Debug.Log($"[TrashObject] Discarded {selectedItem}.");
        _onItemDiscarded?.Invoke();
    }
}