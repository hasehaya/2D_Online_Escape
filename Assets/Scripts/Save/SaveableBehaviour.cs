using System;
using UnityEngine;

namespace Save
{
    /// <summary>
    /// Saveableオブジェクトの共通基底クラス。
    /// saveIdの一元管理とシリアライズ契約を提供する。
    /// </summary>
    public abstract class SaveableBehaviour : MonoBehaviour, ISaveable
    {
        [Header("Save")] [SerializeField] private string _saveId;

        public string SaveId => _saveId;

        protected virtual void OnValidate()
        {
            EnsureSaveId();
            EnsureUniqueSaveIdInScene();
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
            SaveableBehaviour[] saveables = FindObjectsOfType<SaveableBehaviour>(true);
            for (int i = 0; i < saveables.Length; i++)
            {
                SaveableBehaviour other = saveables[i];
                if (other == this)
                {
                    continue;
                }

                if (other._saveId == _saveId)
                {
                    _saveId = Guid.NewGuid().ToString("N");
                    break;
                }
            }
        }

        public abstract string CaptureState();
        public abstract void RestoreState(string stateJson);
    }
}