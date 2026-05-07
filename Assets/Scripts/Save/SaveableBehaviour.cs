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

        [Tooltip("チェックを入れると、シーン内で同じSaveIdの使用を許可します（他のオブジェクトと状態を同期したい場合に使用）")] [SerializeField]
        private bool _allowSharedSaveId;

        public string SaveId => _saveId;

        protected virtual void OnValidate()
        {
            EnsureSaveId();
            EnsureUniqueSaveIdInScene();
        }

        private void EnsureSaveId()
        {
            if (string.IsNullOrEmpty(_saveId))
            {
                Debug.LogWarning($"[{gameObject.name}] SaveIdが設定されていません！インスペクタから手動で設定してください。", gameObject);
            }
        }

        private void EnsureUniqueSaveIdInScene()
        {
            if (_allowSharedSaveId || string.IsNullOrEmpty(_saveId))
            {
                return;
            }

            SaveableBehaviour[] saveables =
                FindObjectsByType<SaveableBehaviour>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            for (int i = 0; i < saveables.Length; i++)
            {
                SaveableBehaviour other = saveables[i];
                if (other == this)
                {
                    continue;
                }

                if (other._allowSharedSaveId)
                {
                    continue;
                }

                if (other._saveId == _saveId)
                {
                    Debug.LogWarning(
                        $"[{gameObject.name}] SaveId '{_saveId}' が重複しています！(対象: {other.gameObject.name}) 意図した重複の場合は 'Allow Shared Save Id' にチェックを入れてください。",
                        gameObject);
                    break;
                }
            }
        }

        public abstract string CaptureState();
        public abstract void RestoreState(string stateJson);
    }
}