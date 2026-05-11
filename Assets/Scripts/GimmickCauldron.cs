using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 特定の手順でアイテムを投入し、最後に瓶を使用することでアイテムを生成する大釜ギミック。
/// 手順を間違えると初期状態にリセットされる。
/// </summary>
public class GimmickCauldron : InteractableObject
{
    [Serializable]
    public struct CauldronStep
    {
        public ItemData RequiredItem; // 投入するアイテム
        public Sprite StateSprite; // 投入成功時に切り替わる画像
    }

    [Header("UI References")] [SerializeField]
    private Image _cauldronImage;

    [SerializeField] private Sprite _initialSprite;

    [Header("Recipe Settings")] [SerializeField]
    private List<CauldronStep> _recipeSteps = new List<CauldronStep>();

    [Header("Final Phase (Bottling)")] [SerializeField]
    private ItemData _bottleItem; // 最後に使用する空瓶など

    [SerializeField] private ItemData _resultItem; // 汲み取って得られる完成品アイテム

    [Header("Events")] [SerializeField] private UnityEvent _onStepAdvanced;
    [SerializeField] private UnityEvent _onReset;
    [SerializeField] private UnityEvent _onCompleted;

    private int _currentStepIndex = 0;

    protected override void Awake()
    {
        base.Awake();
        ResetCauldron(false);
    }

    protected override void Interact()
    {
        base.Interact();

        if (InventoryManager.Instance == null) return;

        ItemData selectedItem = InventoryManager.Instance.GetSelectedItem();
        if (selectedItem == null)
        {
            // アイテム未選択の場合は何もしない（未選択時用のアクションがあればここで呼ぶ）
            return;
        }

        // 素材投入フェーズ
        if (_currentStepIndex < _recipeSteps.Count)
        {
            if (selectedItem == _recipeSteps[_currentStepIndex].RequiredItem)
            {
                // 正解のアイテムを投入
                InventoryManager.Instance.TryRemoveItem(selectedItem);

                _cauldronImage.sprite = _recipeSteps[_currentStepIndex].StateSprite;

                AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

                _currentStepIndex++;
                Debug.Log($"[GimmickCauldron] Advanced to step {_currentStepIndex}. Added: {selectedItem.itemName}");
                _onStepAdvanced?.Invoke();
            }
            else
            {
                // 手順を間違えた場合はリセット
                Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem.itemName}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true);
            }
        }
        // 最終フェーズ（瓶詰め）
        else
        {
            if (selectedItem == _bottleItem)
            {
                // 空瓶に汲む処理
                InventoryManager.Instance.TryRemoveItem(selectedItem);
                InventoryManager.Instance.TryAddItem(_resultItem);
                Debug.Log(
                    $"[GimmickCauldron] Successfully bottled! Generated: {_resultItem.itemName}. Resetting state.");

                AudioManager.Instance.PlaySE(SESoundType.CauldronComplete);

                _onCompleted?.Invoke();
                ResetCauldron(false);
            }
            else
            {
                // 違うアイテムを入れた場合はリセット
                Debug.Log(
                    $"[GimmickCauldron] Failed to bottle, wrong item used: {selectedItem.itemName}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true);
            }
        }
    }

    /// <summary>
    /// 釜を初期状態に戻す
    /// </summary>
    /// <param name="invokeEvent">リセット時のイベントを発火するかどうか</param>
    private void ResetCauldron(bool invokeEvent)
    {
        _currentStepIndex = 0;
        _cauldronImage.sprite = _initialSprite;

        if (invokeEvent)
        {
            _onReset?.Invoke();
        }
    }
}