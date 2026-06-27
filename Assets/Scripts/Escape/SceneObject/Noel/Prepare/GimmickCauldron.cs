using System;
using System.Collections.Generic;
using Escape.SceneObject.Common;
using Save;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Escape.SceneObject.Noel.Prepare
{
    /// <summary>
    /// 2つのルートを持つ大釜ギミック。
    /// 各ルートは2回のアイテム投入で完成し、最後に空の瓶を使うと完成品を生成して初期状態に戻る。
    /// 手順を間違えると初期状態にリセットされる。
    /// </summary>
    public class GimmickCauldron : InteractableObject, ISaveable
    {
        [Serializable]
        public struct CauldronStep
        {
            [SerializeField] private ItemType _firstRequiredItem; // 1回目に投入するアイテム
            [SerializeField] private Color _firstStateColor; // 1回目成功時に切り替わる色
            [SerializeField] private string _firstStateAnimationName; // 1回目成功時の中身アニメーション状態名
            [SerializeField] private ItemType _secondRequiredItem; // 2回目に投入するアイテム
            [SerializeField] private Color _secondStateColor; // 2回目成功時に切り替わる色
            [SerializeField] private string _secondStateAnimationName; // 2回目成功時の中身アニメーション状態名
            [SerializeField] private ItemType _bottleItem; // このルートで使う空の瓶
            [SerializeField] private ItemType _resultItem; // このルートで得られる満タンの瓶

            public ItemType FirstRequiredItem => _firstRequiredItem;
            public Color FirstStateColor => _firstStateColor;
            public string FirstStateAnimationName => _firstStateAnimationName;
            public ItemType SecondRequiredItem => _secondRequiredItem;
            public Color SecondStateColor => _secondStateColor;
            public string SecondStateAnimationName => _secondStateAnimationName;
            public ItemType BottleItem => _bottleItem;
            public ItemType ResultItem => _resultItem;
        }

        [Header("UI References")] [SerializeField]
        private Image _cauldronImage;

        [SerializeField] private Color _defaultColor;

        [Header("Recipe Settings")] [SerializeField]
        private List<CauldronStep> _recipeSteps;

        [SerializeField] private List<ItemType> _cauldronItems = new List<ItemType>();

        [Header("Fire Settings")] [SerializeField]
        private ItemType _ignitionItem = ItemType.Match;

        [SerializeField] private bool _consumeIgnitionItem = true;
        [SerializeField] private SpriteLoopAnimator _fireLoop;
        [SerializeField] private string _fireLoopStateName = "";
        [SerializeField] private bool _hideFireOnReset = true;

        [Header("Content Animation")] [SerializeField]
        private SpriteLoopAnimator _contentLoop;

        [SerializeField] private string _emptyContentStateName = "";
        [SerializeField] private bool _hideContentOnReset;

        [Header("Save")] [SerializeField] private string _saveId;

        [Header("Events")] [SerializeField] private UnityEvent _onIgnited;
        [SerializeField] private UnityEvent _onStepAdvanced;
        [SerializeField] private UnityEvent _onReset;
        [SerializeField] private UnityEvent _onCompleted;

        private int _currentRouteIndex = -1;
        private int _currentStepIndex;
        private bool _isLit;

        public string SaveId => _saveId;

        protected override void Reset()
        {
            base.Reset();
            EnsureSaveId();
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            EnsureSaveId();
            EnsureUniqueSaveIdInScene();
        }

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

            if (!_isLit)
            {
                TryIgnite(selectedItem);
                return;
            }

            if (!CanUseItemInCauldron(selectedItem))
            {
                Debug.Log($"[GimmickCauldron] Item cannot be put in cauldron: {selectedItem}");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                return;
            }

            // 完成後は瓶詰めフェーズ
            if (_currentStepIndex >= 2)
            {
                if (_currentRouteIndex < 0 || _currentRouteIndex >= _recipeSteps.Count)
                {
                    ResetCauldron(true, true);
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
                    ResetCauldron(false, true);
                }
                else
                {
                    Debug.Log($"[GimmickCauldron] Failed to bottle, wrong item used: {selectedItem}. Resetting state.");
                    AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                    ResetCauldron(true, true);
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
                    ResetCauldron(true, true);
                    return;
                }

                CauldronStep route = _recipeSteps[_currentRouteIndex];
                InventoryManager.Instance.TryRemoveItem(selectedItem);

                AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

                _currentStepIndex = 1;
                ApplyContentVisualState();
                Debug.Log($"[GimmickCauldron] Route {_currentRouteIndex} advanced to step 1. Added: {selectedItem}");
                _onStepAdvanced?.Invoke();
                PairSaveCoordinator.RequestSaveIfAvailable();
                return;
            }

            // 2回目の投入
            if (_currentRouteIndex < 0 || _currentRouteIndex >= _recipeSteps.Count)
            {
                ResetCauldron(true, true);
                return;
            }

            CauldronStep currentRoute = _recipeSteps[_currentRouteIndex];
            if (selectedItem == currentRoute.SecondRequiredItem)
            {
                InventoryManager.Instance.TryRemoveItem(selectedItem);

                AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

                _currentStepIndex = 2;
                ApplyContentVisualState();
                Debug.Log($"[GimmickCauldron] Route {_currentRouteIndex} advanced to step 2. Added: {selectedItem}");
                _onStepAdvanced?.Invoke();
                PairSaveCoordinator.RequestSaveIfAvailable();
            }
            else
            {
                Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true, true);
            }
        }

        private void TryIgnite(ItemType selectedItem)
        {
            if (selectedItem != _ignitionItem)
            {
                Debug.Log($"[GimmickCauldron] Cauldron is not lit. Use {_ignitionItem} before adding items.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                return;
            }

            if (_consumeIgnitionItem && !InventoryManager.Instance.TryRemoveItem(selectedItem))
            {
                Debug.Log($"[GimmickCauldron] Failed to consume ignition item: {selectedItem}");
                return;
            }

            _isLit = true;
            Debug.Log($"[GimmickCauldron] Cauldron ignited with {selectedItem}.");
            AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);
            ApplyFireVisualState();
            _onIgnited?.Invoke();
            PairSaveCoordinator.RequestSaveIfAvailable();
        }

        private bool CanUseItemInCauldron(ItemType item)
        {
            if (item == _ignitionItem)
            {
                return false;
            }

            return _cauldronItems.Contains(item);
        }

        private void StartFireLoop()
        {
            if (_fireLoop == null)
            {
                return;
            }

            if (!_fireLoop.PlayLoop(_fireLoopStateName))
            {
                _fireLoop.Clear(_hideFireOnReset);
            }
        }

        private void StopFireLoop()
        {
            if (_fireLoop == null)
            {
                return;
            }

            _fireLoop.Clear(_hideFireOnReset);
        }

        private void ApplyVisualState()
        {
            ApplyFireVisualState();
            ApplyContentVisualState();
        }

        private void ApplyFireVisualState()
        {
            if (_isLit)
            {
                StartFireLoop();
            }
            else
            {
                StopFireLoop();
            }
        }

        private void ApplyContentVisualState()
        {
            if (_currentStepIndex <= 0 || _currentRouteIndex < 0 || _currentRouteIndex >= _recipeSteps.Count)
            {
                _cauldronImage.color = _defaultColor;
                PlayContentState(_emptyContentStateName, _hideContentOnReset);
                return;
            }

            CauldronStep route = _recipeSteps[_currentRouteIndex];
            if (_currentStepIndex == 1)
            {
                _cauldronImage.color = route.FirstStateColor;
                PlayContentState(route.FirstStateAnimationName, false);
                return;
            }

            _cauldronImage.color = route.SecondStateColor;
            PlayContentState(route.SecondStateAnimationName, false);
        }

        private void PlayContentState(string stateName, bool hideIfMissing)
        {
            if (_contentLoop == null)
            {
                return;
            }

            if (string.IsNullOrEmpty(stateName))
            {
                _contentLoop.Clear(hideIfMissing);
                return;
            }

            if (!_contentLoop.PlayLoop(stateName))
            {
                _contentLoop.Clear(hideIfMissing);
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
        private void ResetCauldron(bool invokeEvent, bool requestSave = false)
        {
            _currentRouteIndex = -1;
            _currentStepIndex = 0;
            _isLit = false;
            ApplyVisualState();

            if (invokeEvent)
            {
                _onReset?.Invoke();
            }

            if (requestSave)
            {
                PairSaveCoordinator.RequestSaveIfAvailable();
            }
        }

        [Serializable]
        private struct CauldronState
        {
            public bool isLit;
            public int currentRouteIndex;
            public int currentStepIndex;
        }

        public string CaptureState()
        {
            CauldronState state = new CauldronState
            {
                isLit = _isLit,
                currentRouteIndex = _currentRouteIndex,
                currentStepIndex = _currentStepIndex
            };
            return JsonUtility.ToJson(state);
        }

        public void RestoreState(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson))
            {
                return;
            }

            CauldronState state = JsonUtility.FromJson<CauldronState>(stateJson);
            _isLit = state.isLit;
            _currentRouteIndex = state.currentRouteIndex;
            _currentStepIndex = state.currentStepIndex;

            if (_currentRouteIndex < -1 || _currentRouteIndex >= _recipeSteps.Count)
            {
                _currentRouteIndex = -1;
                _currentStepIndex = 0;
            }

            if (_currentRouteIndex == -1)
            {
                _currentStepIndex = 0;
            }

            _currentStepIndex = Mathf.Clamp(_currentStepIndex, 0, 2);
            if (_currentStepIndex == 0)
            {
                _currentRouteIndex = -1;
            }

            ApplyVisualState();
        }

        private void EnsureSaveId()
        {
            if (!string.IsNullOrEmpty(_saveId))
            {
                return;
            }

            _saveId = Guid.NewGuid().ToString("N");
        }

        private void EnsureUniqueSaveIdInScene()
        {
            if (string.IsNullOrEmpty(_saveId))
            {
                return;
            }

            MonoBehaviour[] behaviours =
                FindObjectsByType<MonoBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < behaviours.Length; i++)
            {
                ISaveable other = behaviours[i] as ISaveable;
                if (other == null || ReferenceEquals(other, this))
                {
                    continue;
                }

                if (other.SaveId == _saveId)
                {
                    _saveId = Guid.NewGuid().ToString("N");
                    break;
                }
            }
        }
    }
}