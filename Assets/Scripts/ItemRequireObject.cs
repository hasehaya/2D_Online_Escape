using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 特定のアイテムを選択した状態でクリックするとアクションを実行するインタラクタブルオブジェクト。
/// </summary>
public class ItemRequireObject : InteractableObject
{
    [Header("Item Requirements")] [SerializeField]
    private ItemType _requiredItem;

    [SerializeField] private bool _consumeItemOnUse = true;

    [Header("Events")] [SerializeField] private UnityEvent _onItemUsed;
    [SerializeField] private UnityEvent _onWrongItemUsed;

    protected override void Interact()
    {
        base.Interact();

        if (InventoryManager.Instance == null)
        {
            Debug.LogError("InventoryManager instance is null.");
            return;
        }

        ItemType selectedItem = InventoryManager.Instance.GetSelectedItem();

        if (selectedItem == _requiredItem)
        {
            // アイテム使用時の処理
            if (_consumeItemOnUse)
            {
                InventoryManager.Instance.TryRemoveItem(selectedItem);
            }

            Debug.Log($"[{gameObject.name}] '{_requiredItem}' was used successfully.");
            _onItemUsed?.Invoke();
        }
        else
        {
            // アイテム不一致、もしくは未選択の場合の処理
            Debug.Log($"[{gameObject.name}] Item not match or not selected.");
            _onWrongItemUsed?.Invoke();
        }
    }
}