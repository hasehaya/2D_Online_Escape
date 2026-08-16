using UnityEngine;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// 指定フラグが立つまではズーム対象として扱い、完了後はアイテムとして取得できるオブジェクト。
    /// </summary>
    public class FlaggedZoomPickupObject : PickupObject
    {
        [Header("Unlock Settings")]
        [SerializeField] private FlagType _pickupUnlockFlag;
        [SerializeField] private ViewNode _zoomViewNode;

        protected override void Interact()
        {
            if (GameStateService.Instance != null &&
                GameStateService.Instance.GetFlag(_pickupUnlockFlag))
            {
                base.Interact();
                return;
            }

            if (_zoomViewNode != null && ViewController.Instance != null)
            {
                ViewController.Instance.ZoomIn(_zoomViewNode);
            }
        }
    }
}
