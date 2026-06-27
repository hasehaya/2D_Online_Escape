using System.Collections.Generic;
using Escape.SceneObject.Common;
using UnityEngine;
using UnityEngine.Events;

namespace Escape.SceneObject.Noel.Prepare
{
    /// <summary>
    /// 選択中のアイチE��を捨てるため�Eゴミ箱オブジェクト、E/// </summary>
    public class TrashObject : InteractableObject
    {
        [Header("Discard Settings")] [SerializeField]
        private List<ItemType> _discardableItems = new List<ItemType>();

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

            if (!_discardableItems.Contains(selectedItem))
            {
                Debug.Log($"[TrashObject] {selectedItem} cannot be discarded.");
                _onCannotDiscard?.Invoke();
                return;
            }

            if (!InventoryManager.Instance.TryRemoveSelectedItem())
            {
                Debug.Log($"[TrashObject] Failed to remove selected item: {selectedItem}.");
                _onCannotDiscard?.Invoke();
                return;
            }

            Debug.Log($"[TrashObject] Discarded {selectedItem}.");
            _onItemDiscarded?.Invoke();
        }
    }
}