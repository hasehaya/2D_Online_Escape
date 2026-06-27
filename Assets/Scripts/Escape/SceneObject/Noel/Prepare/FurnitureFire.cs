using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace Map.Noel.Prepare
{
    /// <summary>
    /// 一つの Image に対して複数の Sprite を順番にフェードイン・アウトで切り替えるコンポーネント。
    /// 炎などのパラパラアニメーション表現に使用する。
    /// </summary>
    public class FurnitureFire : MonoBehaviour
    {
        [SerializeField] private Image _image;
        [SerializeField] private Sprite[] _sprites;

        [Header("タイミング")] [Tooltip("各スプライトを表示し続ける時間（秒）")] [SerializeField]
        private float _frameInterval = 0.1f;

        [Tooltip("フェードアウト・フェードインにかける時間（秒）")] [SerializeField]
        private float _fadeDuration = 0.05f;

        [SerializeField] private Ease _fadeEase = Ease.Linear;

        private int _currentIndex;
        private Coroutine _loopCoroutine;
        private bool _initialized;

        private void Start()
        {
            _initialized = true;
            InitializeImage();
            StartLoop();
        }

        private void OnEnable()
        {
            if (!_initialized) return;
            InitializeImage();
            StartLoop();
        }

        private void OnDisable()
        {
            StopLoop();
        }

        private void OnDestroy()
        {
            _image?.DOKill();
        }

        private void InitializeImage()
        {
            _currentIndex = 0;
            if (_image == null || _sprites == null || _sprites.Length == 0) return;

            _image.sprite = _sprites[0];
            SetAlpha(1f);
        }

        private void StartLoop()
        {
            if (_image == null || _sprites == null || _sprites.Length < 2) return;
            _loopCoroutine = StartCoroutine(LoopCoroutine());
        }

        private void StopLoop()
        {
            if (_loopCoroutine != null)
            {
                StopCoroutine(_loopCoroutine);
                _loopCoroutine = null;
            }

            _image?.DOKill();
        }

        private IEnumerator LoopCoroutine()
        {
            while (true)
            {
                yield return new WaitForSeconds(_frameInterval);

                int nextIndex = (_currentIndex + 1) % _sprites.Length;

                // フェードアウト
                yield return _image.DOFade(0f, _fadeDuration).SetEase(_fadeEase).WaitForCompletion();

                // スプライト切り替え
                _image.sprite = _sprites[nextIndex];

                // フェードイン
                yield return _image.DOFade(1f, _fadeDuration).SetEase(_fadeEase).WaitForCompletion();

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