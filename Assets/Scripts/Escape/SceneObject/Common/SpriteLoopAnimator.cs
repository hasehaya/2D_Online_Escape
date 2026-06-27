using System;
using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// Plays a looping sprite animation on a UI Image.
    /// Use named states when one object needs multiple loops, such as fire and cauldron contents.
    /// </summary>
    public class SpriteLoopAnimator : MonoBehaviour
    {
        [Serializable]
        public class SpriteLoopState
        {
            [SerializeField] private string _stateName;
            [SerializeField] private Sprite[] _sprites;

            public string StateName => _stateName;
            public Sprite[] Sprites => _sprites;
        }

        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _sprites;
        [SerializeField] private SpriteLoopState[] _states;

        [Header("Timing")] [Tooltip("Seconds to keep each sprite visible.")] [SerializeField]
        private float _frameInterval = 0.1f;

        [Tooltip("Seconds used for fade-out and fade-in.")] [SerializeField]
        private float _fadeDuration = 0.05f;

        [SerializeField] private Ease _fadeEase = Ease.Linear;

        [Header("Playback")] [SerializeField] private bool _playOnEnable = true;
        [SerializeField] private string _initialStateName = "";

        private int _currentIndex;
        private Coroutine _loopCoroutine;
        private Sprite[] _currentSprites;
        private bool _initialized;

        private void Start()
        {
            _initialized = true;

            if (_playOnEnable)
            {
                PlayLoop(_initialStateName);
            }
        }

        private void OnEnable()
        {
            if (!_initialized || !_playOnEnable)
            {
                return;
            }

            PlayLoop(_initialStateName);
        }

        private void OnDisable()
        {
            StopLoop();
        }

        private void OnDestroy()
        {
            _image?.DOKill();
        }

        public bool PlayLoop(string stateName = "")
        {
            if (_image == null)
            {
                return false;
            }

            StopLoop();

            Sprite[] sprites = ResolveSprites(stateName);
            if (sprites == null || sprites.Length == 0)
            {
                return false;
            }

            if (!gameObject.activeSelf)
            {
                gameObject.SetActive(true);
            }

            _currentSprites = sprites;
            _currentIndex = 0;
            _image.sprite = _currentSprites[0];
            SetAlpha(1f);

            if (_currentSprites.Length > 1 && isActiveAndEnabled)
            {
                _loopCoroutine = StartCoroutine(LoopCoroutine());
            }

            return true;
        }

        public void StopLoop()
        {
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }

            _image?.DOKill();
        }

        public void Clear(bool hideGameObject = false)
        {
            StopLoop();
            _currentSprites = null;

            if (_image != null)
            {
                _image.sprite = null;
                SetAlpha(0f);
            }

            if (hideGameObject && gameObject.activeSelf)
            {
                gameObject.SetActive(false);
            }
        }

        private Sprite[] ResolveSprites(string stateName)
        {
            if (string.IsNullOrEmpty(stateName))
            {
                return _sprites;
            }

            if (_states == null)
            {
                Debug.LogWarning($"[SpriteLoopAnimator] State not found: {stateName}", this);
                return null;
            }

            for (int i = 0; i < _states.Length; i++)
            {
                SpriteLoopState state = _states[i];
                if (state == null || state.StateName != stateName)
                {
                    continue;
                }

                return state.Sprites;
            }

            Debug.LogWarning($"[SpriteLoopAnimator] State not found: {stateName}", this);
            return null;
        }

        private IEnumerator LoopCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_frameInterval);

                int nextIndex = (_currentIndex + 1) % _currentSprites.Length;

                if (_fadeDuration > 0f)
                {
                    yield return _image.DOFade(0f, _fadeDuration).SetEase(_fadeEase).WaitForCompletion();
                }

                _image.sprite = _currentSprites[nextIndex];

                if (_fadeDuration > 0f)
                {
                    yield return _image.DOFade(1f, _fadeDuration).SetEase(_fadeEase).WaitForCompletion();
                }
                else
                {
                    SetAlpha(1f);
                }

                _currentIndex = nextIndex;
            }
        }

        private void SetAlpha(float alpha)
        {
            Color color = _image.color;
            color.a = alpha;
            _image.color = color;
        }
    }
}