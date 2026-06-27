using System.Collections;
using Escape.SceneObject.Common;
using UnityEngine;

namespace Escape.SceneObject.Elias.Prepare
{
    /// <summary>
    /// ピアノの鍵盤を表すクラス。
    /// InteractableObjectを継承し、クリック時にGameObjectを表示/非表示して親のGimmickPianoに通知する。
    /// </summary>
    public class PianoKey : InteractableObject
    {
        [Header("Piano Key Settings")] [SerializeField]
        private GameObject _pressedObject;

        [SerializeField] private float _pressDuration = 0.3f;
        [SerializeField] private int _keyIndex;

        private GimmickPiano _piano;
        private Coroutine _pressCoroutine;

        private void Start()
        {
            _piano = GetComponentInParent<GimmickPiano>();

            if (_pressedObject != null)
            {
                _pressedObject.SetActive(false);
            }
        }

        protected override void Interact()
        {
            if (_pressCoroutine != null)
            {
                StopCoroutine(_pressCoroutine);
            }

            _pressCoroutine = StartCoroutine(PressKey());

            if (_piano != null)
            {
                _piano.OnKeyPressed(_keyIndex);
            }
        }

        private IEnumerator PressKey()
        {
            if (_pressedObject != null)
            {
                _pressedObject.SetActive(true);
            }

            yield return new WaitForSeconds(_pressDuration);

            if (_pressedObject != null)
            {
                _pressedObject.SetActive(false);
            }
        }
    }
}