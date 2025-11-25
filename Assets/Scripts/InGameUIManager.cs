using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インゲーム（ゲーム本編）のUI表示を管理するクラス。
/// 主にインベントリのデータ変更を監視し、所持アイテム一覧の表示更新を担当する。
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("Inventory UI")]
    [SerializeField] private Transform itemSlotContainer;
    [SerializeField] private GameObject itemSlotPrefab;

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
            UpdateInventoryUI();
        }
    }

    private void OnDestroy()
    {
        // メモリリークを防ぐため、オブジェクト破棄時にイベント購読を解除する
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
        }
    }

    private void UpdateInventoryUI()
    {
        // 既存のスロットを全て削除して作り直す（アイテム数が少ないため、プーリングせずシンプルな実装とする）
        foreach (Transform child in itemSlotContainer)
        {
            Destroy(child.gameObject);
        }

        // 現在の所持アイテムに合わせてスロットを生成
        List<ItemData> items = InventoryManager.Instance.GetItems();
        foreach (ItemData item in items)
        {
            GameObject slot = Instantiate(itemSlotPrefab, itemSlotContainer);
            Image iconImage = slot.transform.Find("Icon").GetComponent<Image>();
            if (iconImage != null)
            {
                iconImage.sprite = item.icon;
                iconImage.enabled = true;
            }
        }
    }
}
