using UnityEngine;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// クリックで拡大表示を行うインタラクタブルオブジェクト。
    /// </summary>
    public class ZoomObject : InteractableObject
    {
        [Header("Zoom Settings")] [SerializeField]
        private ViewNode _zoomViewNode;

        protected override void Interact()
        {
            base.Interact();
            if (_zoomViewNode != null)
            {
                ViewController.Instance.ZoomIn(_zoomViewNode);
            }
        }
    }
}