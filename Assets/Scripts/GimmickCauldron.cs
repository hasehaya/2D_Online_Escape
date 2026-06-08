using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// 2つのルートを持つ大釜ギミック。
/// 各ルートは2回のアイテム投入で完成し、最後に空の瓶を使うと完成品を生成して初期状態に戻る。
/// 手順を間違えると初期状態にリセットされる。
/// </summary>
public class GimmickCauldron : InteractableObject
{
    [Serializable]
    public struct CauldronStep
    {
        [SerializeField] private ItemType _firstRequiredItem; // 1回目に投入するアイテム
        [SerializeField] private Color _firstStateColor; // 1回目成功時に切り替わる色
        [SerializeField] private ItemType _secondRequiredItem; // 2回目に投入するアイテム
        [SerializeField] private Color _secondStateColor; // 2回目成功時に切り替わる色
        [SerializeField] private ItemType _bottleItem; // このルートで使う空の瓶
        [SerializeField] private ItemType _resultItem; // このルートで得られる満タンの瓶

        public ItemType FirstRequiredItem => _firstRequiredItem;
        public Color FirstStateColor => _firstStateColor;
        public ItemType SecondRequiredItem => _secondRequiredItem;
        public Color SecondStateColor => _secondStateColor;
        public ItemType BottleItem => _bottleItem;
        public ItemType ResultItem => _resultItem;
    }

    [Header("UI References")] [SerializeField]
    private Image _cauldronImage;

    [SerializeField] private Color _defaultColor;

    [Header("Recipe Settings")] [SerializeField]
    private List<CauldronStep> _recipeSteps;

    [Header("Events")] [SerializeField] private UnityEvent _onStepAdvanced;
    [SerializeField] private UnityEvent _onReset;
    [SerializeField] private UnityEvent _onCompleted;

    private int _currentRouteIndex = -1;
    private int _currentStepIndex;

    protected override void Awake()
    {
        base.Awake();
        ResetCauldron(false);
    }

    protected override void Interact()
    {
        base.Interact();

        if (InventoryManager.Instance == null) return;

        ItemType selectedItem = InventoryManager.Instance.GetSelectedItem();
        if (selectedItem == ItemType.None)
        {
            // アイテム未選択の場合は何もしない（未選択時用のアクションがあればここで呼ぶ）
            return;
        }

        // 完成後は瓶詰めフェーズ
        if (_currentStepIndex >= 2)
        {
            if (_currentRouteIndex < 0 || _currentRouteIndex >= _recipeSteps.Count)
            {
                ResetCauldron(true);
                return;
            }

            CauldronStep completedRoute = _recipeSteps[_currentRouteIndex];
            if (selectedItem == completedRoute.BottleItem)
            {
                InventoryManager.Instance.TryRemoveItem(selectedItem);
                InventoryManager.Instance.TryAddItem(completedRoute.ResultItem);

                Debug.Log(
                    $"[GimmickCauldron] Successfully bottled! Generated: {completedRoute.ResultItem}. Resetting state.");

                AudioManager.Instance.PlaySE(SESoundType.CauldronComplete);

                _onCompleted?.Invoke();
                ResetCauldron(false);
            }
            else
            {
                Debug.Log($"[GimmickCauldron] Failed to bottle, wrong item used: {selectedItem}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true);
            }

            return;
        }

        // 1回目の投入
        if (_currentStepIndex == 0)
        {
            if (!TryFindRouteByFirstItem(selectedItem, out _currentRouteIndex))
            {
                Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true);
                return;
            }

            CauldronStep route = _recipeSteps[_currentRouteIndex];
            InventoryManager.Instance.TryRemoveItem(selectedItem);
            _cauldronImage.color = route.FirstStateColor;

            AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

            _currentStepIndex = 1;
            Debug.Log($"[GimmickCauldron] Route {_currentRouteIndex} advanced to step 1. Added: {selectedItem}");
            _onStepAdvanced?.Invoke();
            return;
        }

        // 2回目の投入
        if (_currentRouteIndex < 0 || _currentRouteIndex >= _recipeSteps.Count)
        {
            ResetCauldron(true);
            return;
        }

        CauldronStep currentRoute = _recipeSteps[_currentRouteIndex];
        if (selectedItem == currentRoute.SecondRequiredItem)
        {
            InventoryManager.Instance.TryRemoveItem(selectedItem);
            _cauldronImage.color = currentRoute.SecondStateColor;

            AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

            _currentStepIndex = 2;
            Debug.Log($"[GimmickCauldron] Route {_currentRouteIndex} advanced to step 2. Added: {selectedItem}");
            _onStepAdvanced?.Invoke();
        }
        else
        {
            Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem}. Resetting state.");
            AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
            ResetCauldron(true);
        }
    }

    private bool TryFindRouteByFirstItem(ItemType item, out int routeIndex)
    {
        for (int i = 0; i < _recipeSteps.Count; i++)
        {
            if (_recipeSteps[i].FirstRequiredItem == item)
            {
                routeIndex = i;
                return true;
            }
        }

        routeIndex = -1;
        return false;
    }

    /// <summary>
    /// 釜を初期状態に戻す
    /// </summary>
    /// <param name="invokeEvent">リセット時のイベントを発火するかどうか</param>
    private void ResetCauldron(bool invokeEvent)
    {
        _currentRouteIndex = -1;
        _currentStepIndex = 0;
        _cauldronImage.color = _defaultColor;

        if (invokeEvent)
        {
            _onReset?.Invoke();
        }
    }
}