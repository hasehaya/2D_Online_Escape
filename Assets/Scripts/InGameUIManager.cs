using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// インゲーム（ゲーム本編）のUI表示を管理するクラス。
/// 主にインベントリのデータ変更を監視し、所持アイテム一覧の表示更新を担当する。
/// </summary>
public class InGameUIManager : MonoBehaviour
{
    [Header("Inventory UI")] [SerializeField]
    private Transform _itemSlotContainer;

    [SerializeField] private GameObject _itemSlotPrefab;

    [Header("Item Zoom UI")] [SerializeField]
    private GameObject _itemZoomPanel;

    [SerializeField] private Image _itemZoomImage;

    private readonly List<InventorySlot> _slotViews = new List<InventorySlot>();

    private void Start()
    {
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged += UpdateInventoryUI;
            InventoryManager.Instance.OnSelectedSlotChanged += UpdateSelectionUI;
            UpdateInventoryUI();
        }

        _itemZoomPanel.SetActive(false);
    }

    private void OnDestroy()
    {
        // メモリリークを防ぐため、オブジェクト破棄時にイベント購読を解除する
        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.OnInventoryChanged -= UpdateInventoryUI;
            InventoryManager.Instance.OnSelectedSlotChanged -= UpdateSelectionUI;
        }
    }

    private void UpdateInventoryUI()
    {
        _slotViews.Clear();

        // 既存のスロットを全て削除して作り直す（アイテム数が少ないため、プーリングせずシンプルな実装とする）
        foreach (Transform child in _itemSlotContainer)
        {
            Destroy(child.gameObject);
        }

        // 現在の所持アイテムに合わせてスロットを生成
        foreach (InventorySlot slotData in InventoryManager.Instance.GetSlots())
        {
            GameObject slotObject = Instantiate(_itemSlotPrefab, _itemSlotContainer);
            if (!slotObject.TryGetComponent(out InventorySlot slotView))
            {
                Debug.LogError("Inventory slot prefab is missing InventorySlot component.");
                Destroy(slotObject);
                continue;
            }

            Button slotButton = EnsureSlotButton(slotObject);

            slotView.SetIndex(slotData.Index);
            slotView.BindUI(OnSlotTapped);
            slotButton.onClick.RemoveAllListeners();
            slotButton.onClick.AddListener(slotView.Tap);
            _slotViews.Add(slotView);

            Transform iconTransform = slotObject.transform.Find("Icon");
            if (iconTransform != null && iconTransform.TryGetComponent(out Image iconImage))
            {
                iconImage.sprite = slotData.Item.icon;
                iconImage.enabled = true;
            }

            Transform countTransform = slotObject.transform.Find("CountText");
            if (countTransform != null && countTransform.TryGetComponent(out Text countText))
            {
                countText.text = slotData.Index.ToString();
                countText.enabled = true;
            }
        }

        UpdateSelectionUI(InventoryManager.Instance.GetSelectedSlot());
    }

    private void OnSlotTapped(InventorySlot tappedSlot)
    {
        InventorySlot selectedSlot = InventoryManager.Instance.GetSelectedSlot();
        if (selectedSlot != null && selectedSlot.Index == tappedSlot.Index)
        {
            OpenSelectedItemZoom();
            return;
        }

        InventoryManager.Instance.TrySelectSlot(tappedSlot.Index);
    }

    private void UpdateSelectionUI(InventorySlot selectedSlot)
    {
        foreach (InventorySlot slotView in _slotViews)
        {
            bool isSelected = selectedSlot != null && slotView.Index == selectedSlot.Index;
            slotView.SetSelected(isSelected);
        }
    }

    public void CloseItemZoom()
    {
        _itemZoomPanel.SetActive(false);
    }

    private void OpenSelectedItemZoom()
    {
        ItemData selectedItem = InventoryManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            return;
        }

        _itemZoomImage.sprite = selectedItem.icon;
        _itemZoomImage.enabled = true;
        _itemZoomPanel.SetActive(true);
    }

    private Button EnsureSlotButton(GameObject slot)
    {
        if (slot.TryGetComponent(out Button slotButton))
        {
            return slotButton;
        }

        slotButton = slot.AddComponent<Button>();
        if (!slot.TryGetComponent(out Image targetImage))
        {
            targetImage = slot.AddComponent<Image>();
            targetImage.color = new Color(1f, 1f, 1f, 0f);
        }

        slotButton.targetGraphic = targetImage;
        return slotButton;
    }
}