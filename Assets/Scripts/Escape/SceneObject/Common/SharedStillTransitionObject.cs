using UnityEngine;

namespace Escape.SceneObject.Common
{
    public class SharedStillTransitionObject : InteractableObject
    {
        [SerializeField] private StillNode _stillNode;

        protected override void Interact()
        {
            base.Interact();

            if (ViewController.Instance != null)
            {
                ViewController.Instance.ShowStillForAll(_stillNode);
            }
        }
    }
}
