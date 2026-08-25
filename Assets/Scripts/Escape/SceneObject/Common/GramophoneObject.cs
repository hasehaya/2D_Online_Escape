using System;
using System.Collections;
using Save;
using UnityEngine;
using UnityEngine.UI;

namespace Escape.SceneObject.Common
{
    /// <summary>
    /// 登録された音符Imageを順番に流し、再生状態を保存する蓄音機。
    /// </summary>
    public class GramophoneObject : SaveableBehaviour
    {
        [Header("Notes")] [SerializeField] private Sprite[] _noteSprites;

        [Tooltip("生成した音符Imageの親。未指定の場合はこのオブジェクトの子に生成する。")]
        [SerializeField] private RectTransform _noteContainer;

        [SerializeField] private Vector2 _noteSize = new Vector2(96f, 96f);
        [SerializeField] private bool _preserveAspect = true;

        [Header("Movement")] [Tooltip("各音符が最初に出る位置のオフセット。")]
        [SerializeField] private Vector2 _offset = new Vector2(-200f, 400f);

        [Tooltip("1つの音符が開始位置から終端まで流れる秒数。")]
        [Min(0.01f)] [SerializeField] private float _moveDuration = 2f;

        [Header("Wave")] [Tooltip("進行方向に対して垂直に揺れる幅。")]
        [Min(0f)] [SerializeField] private float _waveAmplitude = 20f;

        [Tooltip("1つの音符が終端へ着くまでに描く波の回数。")]
        [Min(0f)] [SerializeField] private float _waveCount = 1.5f;

        [Tooltip("音符ごとの波の位相差（度）。")]
        [SerializeField] private float _phaseOffset = 45f;

        [Tooltip("次の音符を流し始めるまでの秒数。")]
        [Min(0f)] [SerializeField] private float _noteInterval = 0.3f;

        [Tooltip("移動の終端でフェードアウトする秒数。")]
        [Min(0f)] [SerializeField] private float _fadeOutDuration = 0.5f;

        [Tooltip("すべての音符を流し終えてから、次のループを開始するまでの秒数。")]
        [Min(0f)] [SerializeField] private float _loopInterval = 1f;

        private Image[] _noteImages;
        private RectTransform[] _noteRects;
        private Vector2[] _initialPositions;
        private Color[] _initialColors;
        private Coroutine _playbackCoroutine;
        private bool _isPlaying;

        public bool IsPlaying => _isPlaying;

        private void Awake()
        {
            CreateNoteImages();
            HideAllNotes();
        }

        private void Start()
        {
            ApplyState();
        }

        private void OnEnable()
        {
            if (_isPlaying && _noteRects != null && _playbackCoroutine == null && HasValidNote())
            {
                _playbackCoroutine = StartCoroutine(PlaybackLoop());
            }
        }

        private void OnDisable()
        {
            StopPlayback();
            HideAllNotes();
        }

        /// <summary>
        /// 蓄音機の再生状態を変更し、ペアセーブへ保存する。
        /// UnityEventのbool引数からも呼び出せる。
        /// </summary>
        public void SetPlaying(bool isPlaying)
        {
            if (_isPlaying == isPlaying)
            {
                return;
            }

            _isPlaying = isPlaying;
            ApplyState();
            PairSaveCoordinator.RequestSaveIfAvailable();
        }

        private void ApplyState()
        {
            StopPlayback();
            HideAllNotes();

            if (_isPlaying && isActiveAndEnabled && HasValidNote())
            {
                _playbackCoroutine = StartCoroutine(PlaybackLoop());
            }
        }

        private IEnumerator PlaybackLoop()
        {
            while (_isPlaying)
            {
                float sequenceDuration = _moveDuration + Mathf.Max(0, _noteSprites.Length - 1) * _noteInterval;
                float elapsed = 0f;

                while (elapsed < sequenceDuration && _isPlaying)
                {
                    elapsed += Time.deltaTime;
                    UpdateNotes(elapsed);
                    yield return null;
                }

                HideAllNotes();

                if (_isPlaying && _loopInterval > 0f)
                {
                    yield return new WaitForSeconds(_loopInterval);
                }
            }

            _playbackCoroutine = null;
        }

        private void UpdateNotes(float sequenceTime)
        {
            float fadeDuration = Mathf.Min(_fadeOutDuration, _moveDuration);
            float fadeStartTime = _moveDuration - fadeDuration;

            for (int i = 0; i < _noteImages.Length; i++)
            {
                Image image = _noteImages[i];
                RectTransform rect = _noteRects[i];
                if (image == null || rect == null)
                {
                    continue;
                }

                float noteTime = sequenceTime - i * _noteInterval;
                if (noteTime < 0f || noteTime >= _moveDuration)
                {
                    SetNoteAlpha(i, 0f);
                    continue;
                }

                float progress = Mathf.Clamp01(noteTime / _moveDuration);
                Vector2 waveDirection = GetWaveDirection();
                float phase = i * _phaseOffset * Mathf.Deg2Rad;
                float wave = Mathf.Sin(progress * _waveCount * Mathf.PI * 2f + phase);
                float waveEnvelope = Mathf.Sin(progress * Mathf.PI);
                rect.anchoredPosition = _initialPositions[i] + _offset * (1f - progress)
                                        + waveDirection * (wave * waveEnvelope * _waveAmplitude);

                float alpha = _initialColors[i].a;
                if (fadeDuration > 0f && noteTime >= fadeStartTime)
                {
                    alpha *= 1f - (noteTime - fadeStartTime) / fadeDuration;
                }

                SetNoteAlpha(i, alpha);
            }
        }

        private Vector2 GetWaveDirection()
        {
            if (_offset.sqrMagnitude <= Mathf.Epsilon)
            {
                return Vector2.up;
            }

            Vector2 direction = _offset.normalized;
            return new Vector2(-direction.y, direction.x);
        }

        private void CreateNoteImages()
        {
            int count = _noteSprites?.Length ?? 0;
            _noteImages = new Image[count];
            _noteRects = new RectTransform[count];
            _initialPositions = new Vector2[count];
            _initialColors = new Color[count];

            for (int i = 0; i < count; i++)
            {
                Sprite sprite = _noteSprites[i];
                if (sprite == null)
                {
                    continue;
                }

                GameObject noteObject = new GameObject($"[Generated]Note_{i}", typeof(RectTransform), typeof(Image));
                noteObject.transform.SetParent(_noteContainer != null ? _noteContainer : transform, false);

                Image image = noteObject.GetComponent<Image>();
                image.sprite = sprite;
                image.preserveAspect = _preserveAspect;
                image.raycastTarget = false;

                RectTransform rect = image.rectTransform;
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = _noteSize;

                _noteImages[i] = image;
                _noteRects[i] = rect;
                _initialPositions[i] = rect.anchoredPosition;
                _initialColors[i] = image.color;
            }
        }

        private bool HasValidNote()
        {
            if (_noteSprites == null)
            {
                return false;
            }

            for (int i = 0; i < _noteSprites.Length; i++)
            {
                if (_noteSprites[i] != null)
                {
                    return true;
                }
            }

            return false;
        }

        private void HideAllNotes()
        {
            if (_noteImages == null || _noteRects == null)
            {
                return;
            }

            for (int i = 0; i < _noteImages.Length; i++)
            {
                if (_noteRects[i] != null)
                {
                    _noteRects[i].anchoredPosition = _initialPositions[i];
                }

                SetNoteAlpha(i, 0f);
            }
        }

        private void SetNoteAlpha(int index, float alpha)
        {
            Image image = _noteImages[index];
            if (image == null)
            {
                return;
            }

            Color color = _initialColors[index];
            color.a = Mathf.Clamp01(alpha);
            image.color = color;
        }

        private void StopPlayback()
        {
            if (_playbackCoroutine == null)
            {
                return;
            }

            StopCoroutine(_playbackCoroutine);
            _playbackCoroutine = null;
        }

        [Serializable]
        private struct GramophoneState
        {
            public bool isPlaying;
        }

        public override string CaptureState()
        {
            return JsonUtility.ToJson(new GramophoneState { isPlaying = _isPlaying });
        }

        public override void RestoreState(string stateJson)
        {
            if (string.IsNullOrEmpty(stateJson))
            {
                return;
            }

            GramophoneState state = JsonUtility.FromJson<GramophoneState>(stateJson);
            _isPlaying = state.isPlaying;
            ApplyState();
        }
    }
}
