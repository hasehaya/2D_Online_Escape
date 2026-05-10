using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BookObject : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] _pages;
    [SerializeField] private CanvasGroup[] _animImages; // 白紙のアニメーション画像 (要素0と1を想定)
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _leftButton;
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _fadeInDelay = 0.2f;

    private int _currentPageIndex;
    private Sequence _currentAnimation;

    private void Start()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].gameObject.SetActive(i == _currentPageIndex);
            _pages[i].alpha = i == _currentPageIndex ? 1f : 0f;
        }

        for (int i = 0; i < _animImages.Length; i++)
        {
            _animImages[i].gameObject.SetActive(false);
            _animImages[i].alpha = 0f;
        }

        _rightButton.onClick.AddListener(TurnRight);
        _leftButton.onClick.AddListener(TurnLeft);
    }

    /// <summary>
    /// 右を押した時（次のページへ）
    /// 1 -> 2 の順で表示を切り替える
    /// </summary>
    public void TurnRight()
    {
        if (_currentPageIndex >= _pages.Length - 1) return;

        int nextIndex = _currentPageIndex + 1;
        PlayFadeAnimation(_currentPageIndex, nextIndex, true);
        _currentPageIndex = nextIndex;
    }

    /// <summary>
    /// 左を押した時（前のページへ）
    /// 2 -> 1 の順で表示を切り替える
    /// </summary>
    public void TurnLeft()
    {
        if (_currentPageIndex <= 0) return;

        int prevIndex = _currentPageIndex - 1;
        PlayFadeAnimation(_currentPageIndex, prevIndex, false);
        _currentPageIndex = prevIndex;
    }

    private void PlayFadeAnimation(int fromIndex, int toIndex, bool isRight)
    {
        if (_currentAnimation != null && _currentAnimation.IsActive())
        {
            _currentAnimation.Complete();
        }

        // 進行方向によって白紙アニメーションの順序を切り替え
        CanvasGroup animFrom = isRight ? _animImages[0] : _animImages[1];
        CanvasGroup animTo = isRight ? _animImages[1] : _animImages[0];

        _currentAnimation = FadeSwitchService.Switch(
            animFrom,
            animTo,
            _fadeDuration,
            _fadeDuration,
            Ease.Linear,
            _fadeInDelay
        );

        // アニメーションが終わったらページを表示（切り替え）する
        _currentAnimation.OnComplete(() =>
        {
            _pages[fromIndex].gameObject.SetActive(false);
            _pages[toIndex].gameObject.SetActive(true);
            _pages[toIndex].alpha = 1f;

            animTo.alpha = 0f;
            animTo.gameObject.SetActive(false);
        });
    }
}