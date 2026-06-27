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
    /// 2�̃��[�g�����劘�M�~�b�N�B
    /// �e���[�g��2��̃A�C�e�������Ŋ������A�Ō�ɋ�̕r���g���Ɗ����i�𐶐����ď�����Ԃɖ߂�B
    /// �菇���ԈႦ��Ə�����ԂɃ��Z�b�g�����B
    /// </summary>
    public class GimmickCauldron : InteractableObject, ISaveable
    {
        [Serializable]
        public struct CauldronStep
        {
            [SerializeField] private ItemType _firstRequiredItem; // 1��ڂɓ�������A�C�e��
            [SerializeField] private Color _firstStateColor; // 1��ڐ������ɐ؂�ւ��F
            [SerializeField] private string _firstStateAnimationName; // 1��ڐ������̒��g�A�j���[�V������Ԗ�
            [SerializeField] private ItemType _secondRequiredItem; // 2��ڂɓ�������A�C�e��
            [SerializeField] private Color _secondStateColor; // 2��ڐ������ɐ؂�ւ��F
            [SerializeField] private string _secondStateAnimationName; // 2��ڐ������̒��g�A�j���[�V������Ԗ�
            [SerializeField] private ItemType _bottleItem; // ���̃��[�g�Ŏg����̕r
            [SerializeField] private ItemType _resultItem; // ���̃��[�g�œ����閞�^���̕r

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
                // �A�C�e�����I���̏ꍇ�͉������Ȃ��i���I�����p�̃A�N�V����������΂����ŌĂԁj
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

            // ������͕r�l�߃t�F�[�Y
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

            // 1��ڂ̓���
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

            // 2��ڂ̓���
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
        /// ����������Ԃɖ߂�
        /// </summary>
        /// <param name="invokeEvent">���Z�b�g���̃C�x���g�𔭉΂��邩�ǂ���</param>
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