using System;
using System.Collections.Generic;
using Escape.SceneObject.Common;
using RunconaLib.Audio;
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
        public struct CauldronRecipeStep
        {
            [SerializeField] private ItemType _requiredItem;
            [SerializeField] private Sprite[] _stateSprites;

            public CauldronRecipeStep(ItemType requiredItem, Sprite[] stateSprites)
            {
                _requiredItem = requiredItem;
                _stateSprites = stateSprites;
            }

            public ItemType RequiredItem => _requiredItem;
            public Sprite[] StateSprites => _stateSprites;
        }

        [Serializable]
        public struct CauldronRecipe
        {
            [SerializeField] private List<CauldronRecipeStep> _steps;
            [SerializeField] private ItemType _bottleItem;
            [SerializeField] private ItemType _resultItem;

            public int StepCount => _steps?.Count ?? 0;
            public ItemType BottleItem => _bottleItem;
            public ItemType ResultItem => _resultItem;

            public bool TryGetStep(int stepIndex, out CauldronRecipeStep step)
            {
                if (_steps != null && stepIndex >= 0 && stepIndex < _steps.Count)
                {
                    step = _steps[stepIndex];
                    return true;
                }

                step = default;
                return false;
            }

            public bool HasStepThatRequires(ItemType item)
            {
                if (_steps == null)
                {
                    return false;
                }

                for (int i = 0; i < _steps.Count; i++)
                {
                    if (_steps[i].RequiredItem == item)
                    {
                        return true;
                    }
                }

                return false;
            }
        }

        [Header("UI References")] [SerializeField]
        private Image _cauldronImage;

        [SerializeField] private SpriteLoopAnimator _cauldronImageLoop;

        [SerializeField] private Color _defaultColor;
        [SerializeField] private Sprite _defaultSprite;

        [Header("Recipe Settings")] [SerializeField]
        private List<CauldronRecipe> _recipes;

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

        private int _currentRecipeIndex = -1;
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

            if (IsCompletionStepReached())
            {
                if (_currentRecipeIndex < 0 || _currentRecipeIndex >= _recipes.Count)
                {
                    ResetCauldron(true, true);
                    return;
                }

                CauldronRecipe completedRecipe = _recipes[_currentRecipeIndex];
                if (selectedItem == completedRecipe.BottleItem)
                {
                    InventoryManager.Instance.TryRemoveItem(selectedItem);
                    InventoryManager.Instance.TryAddItem(completedRecipe.ResultItem);

                    PublishCompletedFlagIfAllResultsAcquired();

                    Debug.Log(
                        $"[GimmickCauldron] Successfully bottled! Generated: {completedRecipe.ResultItem}. Resetting state.");

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

            if (_currentStepIndex == 0 && !TryFindRecipeByFirstItem(selectedItem, out _currentRecipeIndex))
            {
                Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true, true);
                return;
            }

            if (!TryAdvanceCurrentStep(selectedItem))
            {
                Debug.Log($"[GimmickCauldron] Wrong material inserted: {selectedItem}. Resetting state.");
                AudioManager.Instance.PlaySE(SESoundType.CauldronFail);
                ResetCauldron(true, true);
            }
        }

        private void PublishCompletedFlagIfAllResultsAcquired()
        {
            if (GameStateService.Instance == null || _recipes == null || _recipes.Count == 0)
            {
                return;
            }

            for (int i = 0; i < _recipes.Count; i++)
            {
                ItemType resultItem = _recipes[i].ResultItem;
                if (resultItem == ItemType.None || !InventoryManager.Instance.HasItem(resultItem))
                {
                    return;
                }
            }

            GameStateService.Instance.SetFlag(FlagType.Prepare_CauldronCompleted, true);
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

            if (_cauldronItems.Contains(item))
            {
                return true;
            }

            for (int i = 0; i < _recipes.Count; i++)
            {
                if (_recipes[i].BottleItem == item || _recipes[i].HasStepThatRequires(item))
                {
                    return true;
                }
            }

            return false;
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
            if (_currentStepIndex <= 0 || _currentRecipeIndex < 0 || _currentRecipeIndex >= _recipes.Count)
            {
                ApplyCauldronImageState(_defaultSprite, null);
                PlayContentState(_emptyContentStateName, _hideContentOnReset);
                return;
            }

            CauldronRecipe recipe = _recipes[_currentRecipeIndex];
            if (recipe.StepCount <= 0)
            {
                ApplyCauldronImageState(_defaultSprite, null);
                PlayContentState(_emptyContentStateName, _hideContentOnReset);
                return;
            }

            int visualStepIndex = Mathf.Clamp(_currentStepIndex - 1, 0, recipe.StepCount - 1);
            if (!recipe.TryGetStep(visualStepIndex, out CauldronRecipeStep step))
            {
                ApplyCauldronImageState(_defaultSprite, null);
                PlayContentState(_emptyContentStateName, _hideContentOnReset);
                return;
            }

            ApplyCauldronImageState(_defaultSprite, step.StateSprites);
            PlayContentState(string.Empty, true);
        }

        private void ApplyCauldronImageState(Sprite fallbackSprite, Sprite[] animatedSprites)
        {
            if (_cauldronImage != null)
            {
                _cauldronImage.color = _defaultColor;
            }

            if (_cauldronImageLoop != null)
            {
                if (animatedSprites != null && animatedSprites.Length > 0)
                {
                    if (_cauldronImageLoop.PlayLoop(animatedSprites))
                    {
                        return;
                    }
                }
                else if (fallbackSprite != null && _cauldronImageLoop.ShowSprite(fallbackSprite))
                {
                    return;
                }

                _cauldronImageLoop.Clear(false);
            }

            if (_cauldronImage != null)
            {
                if (animatedSprites != null && animatedSprites.Length > 0)
                {
                    _cauldronImage.sprite = animatedSprites[0];
                    return;
                }

                _cauldronImage.sprite = fallbackSprite;
            }
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

        private bool TryFindRecipeByFirstItem(ItemType item, out int recipeIndex)
        {
            for (int i = 0; i < _recipes.Count; i++)
            {
                if (_recipes[i].TryGetStep(0, out CauldronRecipeStep firstStep) &&
                    firstStep.RequiredItem == item)
                {
                    recipeIndex = i;
                    return true;
                }
            }

            recipeIndex = -1;
            return false;
        }

        private bool TryAdvanceCurrentStep(ItemType selectedItem)
        {
            if (_currentRecipeIndex < 0 || _currentRecipeIndex >= _recipes.Count)
            {
                return false;
            }

            CauldronRecipe recipe = _recipes[_currentRecipeIndex];
            if (!recipe.TryGetStep(_currentStepIndex, out CauldronRecipeStep step))
            {
                return false;
            }

            if (selectedItem != step.RequiredItem)
            {
                return false;
            }

            InventoryManager.Instance.TryRemoveItem(selectedItem);
            AudioManager.Instance.PlaySE(SESoundType.CauldronInsert);

            _currentStepIndex++;
            ApplyContentVisualState();
            Debug.Log(
                $"[GimmickCauldron] Recipe {_currentRecipeIndex} advanced to step {_currentStepIndex}. Added: {selectedItem}");
            _onStepAdvanced?.Invoke();
            PairSaveCoordinator.RequestSaveIfAvailable();
            return true;
        }

        private bool IsCompletionStepReached()
        {
            if (_currentRecipeIndex < 0 || _currentRecipeIndex >= _recipes.Count)
            {
                return false;
            }

            int stepCount = _recipes[_currentRecipeIndex].StepCount;
            return stepCount > 0 && _currentStepIndex >= stepCount;
        }

        private void NormalizeProgressState()
        {
            if (_currentRecipeIndex < -1 || _currentRecipeIndex >= _recipes.Count)
            {
                _currentRecipeIndex = -1;
                _currentStepIndex = 0;
                _isLit = false;
                return;
            }

            if (_currentRecipeIndex == -1)
            {
                _currentStepIndex = 0;
                return;
            }

            int maxStepIndex = _recipes[_currentRecipeIndex].StepCount;
            _currentStepIndex = Mathf.Clamp(_currentStepIndex, 0, maxStepIndex);

            if (_currentStepIndex <= 0)
            {
                _currentRecipeIndex = -1;
                return;
            }

            _isLit = true;
        }

        private void ResetCauldron(bool invokeEvent, bool requestSave = false)
        {
            _currentRecipeIndex = -1;
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
            public int currentRecipeIndex;
            public int currentStepIndex;
        }

        public string CaptureState()
        {
            CauldronState state = new CauldronState
            {
                isLit = _isLit,
                currentRecipeIndex = _currentRecipeIndex,
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
            _currentRecipeIndex = state.currentRecipeIndex;
            _currentStepIndex = state.currentStepIndex;
            NormalizeProgressState();
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
