using DG.Tweening;
using RunconaLib.Audio;
using UnityEngine;
using UnityEngine.UI;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// タップするたびに開閉状態を切り替えるオブジェクト。
    /// 状態は保存せず、シーン開始時にはインスペクターで指定した初期状態に戻る。
    /// </summary>
    public class OpenCloseObject : InteractableObject
    {
        [Header("Images")] [SerializeField] private Image _closedImage;
        [SerializeField] private Image _openImage;

        [Header("Initial State")] [SerializeField]
        private bool _startsOpen;

        [Header("Animation")] [SerializeField] private float _fadeDuration = 0.2f;

        [SerializeField] private Ease _fadeEase = Ease.Linear;

        [Header("SE")] [SerializeField] private SESoundType _openSEType = SESoundType.CorrectBoxOpen;

        [SerializeField] private SESoundType _closeSEType = SESoundType.CorrectBoxOpen;

        private bool _isOpen;

        public bool IsOpen => _isOpen;

        private void Start()
        {
            _isOpen = _startsOpen;
            ApplyImageState();
        }

        protected override void Interact()
        {
            Toggle();
        }

        public void Toggle()
        {
            SetOpen(!_isOpen);
        }

        public void Open()
        {
            SetOpen(true);
        }

        public void Close()
        {
            SetOpen(false);
        }

        private void SetOpen(bool isOpen)
        {
            if (_isOpen == isOpen) return;

            _isOpen = isOpen;
            AudioManager.Instance.PlaySE(_isOpen ? _openSEType : _closeSEType);
            PlaySwitchAnimation();
        }

        private void PlaySwitchAnimation()
        {
            if (_closedImage == null || _openImage == null)
            {
                ApplyImageState();
                return;
            }

            _closedImage.DOKill();
            _openImage.DOKill();

            Image from = _isOpen ? _closedImage : _openImage;
            Image to = _isOpen ? _openImage : _closedImage;
            FadeSwitchService.Switch(from, to, _fadeDuration, _fadeEase);
        }

        private void ApplyImageState()
        {
            ApplyImageState(_closedImage, !_isOpen);
            ApplyImageState(_openImage, _isOpen);
        }

        private static void ApplyImageState(Image image, bool active)
        {
            if (image == null) return;

            image.gameObject.SetActive(active);
            Color color = image.color;
            color.a = 1f;
            image.color = color;
        }

        private void OnDestroy()
        {
            if (_closedImage != null) _closedImage.DOKill();
            if (_openImage != null) _openImage.DOKill();
        }
    }
}