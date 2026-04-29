using System;
using Save;
using UnityEngine;

/// <summary>
/// 取得可能なインタラクトオブジェクト（保存対応）。
/// InteractableObjectを継承してUIセットアップとGizmo描画を再利用する。
/// </summary>
public class PickupObject : InteractableObject, ISaveable
{
    [Header("Save")] [SerializeField] private string _saveId;

    public string SaveId => _saveId;

    protected override void Reset()
    {
        _interactionType = InteractionType.Pickup;
        base.Reset();
    }

    protected override void Awake()
    {
        _interactionType = InteractionType.Pickup;
        base.Awake();
    }

    private void OnValidate()
    {
        _interactionType = InteractionType.Pickup;
        EnsureSaveId();
        EnsureUniqueSaveIdInScene();
    }

    protected override bool TryPickup()
    {
        bool picked = base.TryPickup();
        if (picked)
        {
            PairSaveCoordinator.RequestSaveIfAvailable();
        }

        return picked;
    }

    [Serializable]
    private struct PickupState
    {
        public bool isActive;
    }

    public string CaptureState()
    {
        PickupState s = new PickupState { isActive = gameObject.activeSelf };
        return JsonUtility.ToJson(s);
    }

    public void RestoreState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson))
        {
            return;
        }

        PickupState s = JsonUtility.FromJson<PickupState>(stateJson);
        gameObject.SetActive(s.isActive);
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