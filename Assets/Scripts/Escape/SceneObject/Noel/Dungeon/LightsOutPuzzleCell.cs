using System.Collections;
using Escape.SceneObject.Common;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Escape.SceneObject.Noel.Dungeon
{
    public class LightsOutPuzzleCell : InteractableObject
    {
        [Header("Puzzle Cell")] [SerializeField]
        private int _cellIndex;

        [SerializeField] private UnityEvent<int> _onPressed = new UnityEvent<int>();

        [Header("View")] [SerializeField] private Image _targetImage;
        [SerializeField] private Sprite _normalSprite;
        [SerializeField] private Sprite _pressedSprite;
        [SerializeField] private float _pressedDuration = 0.1f;

        private Coroutine _pressedCoroutine;

        public UnityEvent<int> OnPressed => _onPressed;

        protected override void Awake()
        {
            base.Awake();

            if (_targetImage == null)
            {
                _targetImage = GetComponent<Image>();
            }

            if (_targetImage != null && _normalSprite == null)
            {
                _normalSprite = _targetImage.sprite;
            }
        }

        public void SetCellIndex(int cellIndex)
        {
            _cellIndex = cellIndex;
        }

        protected override void Interact()
        {
            PlayPressedView();
            _onPressed?.Invoke(_cellIndex);
        }

        private void PlayPressedView()
        {
            if (_targetImage == null || _pressedSprite == null) return;

            if (_pressedCoroutine != null)
            {
                StopCoroutine(_pressedCoroutine);
            }

            _pressedCoroutine = StartCoroutine(PressedViewCoroutine());
        }

        private IEnumerator PressedViewCoroutine()
        {
            _targetImage.sprite = _pressedSprite;

            yield return new WaitForSeconds(_pressedDuration);

            if (_normalSprite != null)
            {
                _targetImage.sprite = _normalSprite;
            }

            _pressedCoroutine = null;
        }
    }
}