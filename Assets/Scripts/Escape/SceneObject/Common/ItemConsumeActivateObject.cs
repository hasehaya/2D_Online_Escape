using System;
using Save;
using UnityEngine;
using UnityEngine.Events;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// 選択中の必要アイテムを消費し、対象のGameObjectを有効化する。
    /// 有効化状態はペアセーブシステムで保存・復元される。
    /// </summary>
    public class ItemConsumeActivateObject : InteractableObject, ISaveable
    {
        [Header("Item Requirement")] [SerializeField]
        private ItemType _requiredItem = ItemType.None;

        [SerializeField] private bool _consumeItemOnUse = true;

        [Header("Activation")] [SerializeField]
        private GameObject _targetObject;

        [Header("Events")] [SerializeField] private UnityEvent _onActivated;
        [SerializeField] private UnityEvent _onWrongItemUsed;
        [SerializeField] private UnityEvent _onAlreadyActivated;

        [Header("Save")] [SerializeField] private string _saveId;

        private bool _isActivated;

        public string SaveId => _saveId;
        public bool IsActivated => _isActivated;

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

        private void Start()
        {
            ApplyState();
        }

        protected override void Interact()
        {
            base.Interact();

            if (_isActivated)
            {
                _onAlreadyActivated?.Invoke();
                return;
            }

            if (_requiredItem == ItemType.None)
            {
                Debug.LogWarning($"[{gameObject.name}] Required Item is not set.", gameObject);
                _onWrongItemUsed?.Invoke();
                return;
            }

            if (InventoryManager.Instance == null)
            {
                Debug.LogError($"[{gameObject.name}] InventoryManager instance is null.", gameObject);
                return;
            }

            ItemType selectedItem = InventoryManager.Instance.GetSelectedItem();
            if (selectedItem != _requiredItem)
            {
                Debug.Log($"[{gameObject.name}] Item not match or not selected.");
                _onWrongItemUsed?.Invoke();
                return;
            }

            if (_consumeItemOnUse && !InventoryManager.Instance.TryRemoveItem(selectedItem))
            {
                Debug.LogWarning($"[{gameObject.name}] Failed to consume item: {selectedItem}", gameObject);
                return;
            }

            _isActivated = true;
            ApplyState();
            _onActivated?.Invoke();
            PairSaveCoordinator.RequestSaveIfAvailable();
        }

        private void ApplyState()
        {
            if (_targetObject != null)
            {
                _targetObject.SetActive(_isActivated);
            }
        }

        [Serializable]
        private struct ActivateState
        {
            public bool isActivated;
        }

        public string CaptureState()
        {
            ActivateState state = new ActivateState { isActivated = _isActivated };
            return JsonUtility.ToJson(state);
        }

        public void RestoreState(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson))
            {
                return;
            }

            ActivateState state = JsonUtility.FromJson<ActivateState>(stateJson);
            _isActivated = state.isActivated;
            ApplyState();
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