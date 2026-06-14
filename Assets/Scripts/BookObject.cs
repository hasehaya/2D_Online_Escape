using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class BookObject : MonoBehaviour
{
    [SerializeField] private CanvasGroup[] _pages;
    [SerializeField] private Image _animImage; // 白紙のアニメーション画像を表示するImage
    [SerializeField] private Sprite[] _animSprites; // 白紙のアニメーション画像 (要素0と1を想定)
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _leftButton;
    [SerializeField] private float _fadeDuration = 0.5f;
    [SerializeField] private float _fadeInDelay = 0.2f;

    private int _currentPageIndex;
    private Sequence _currentAnimation;
    private Sprite _defaultAnimSprite;

    private void Start()
    {
        for (int i = 0; i < _pages.Length; i++)
        {
            _pages[i].gameObject.SetActive(i == _currentPageIndex);
            _pages[i].alpha = i == _currentPageIndex ? 1f : 0f;
        }

        if (_animImage != null)
        {
            _defaultAnimSprite = _animImage.sprite;
            RestoreDefaultAnimImage();
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

        if (_animImage == null || _animSprites == null || _animSprites.Length < 2)
        {
            SwitchPage(fromIndex, toIndex);
            RestoreDefaultAnimImage();
            return;
        }

        int fromSpriteIndex = isRight ? 0 : 1;
        int toSpriteIndex = isRight ? 1 : 0;

        _animImage.gameObject.SetActive(true);
        _animImage.sprite = _animSprites[fromSpriteIndex];
        SetAnimImageAlpha(1f);

        _currentAnimation = DOTween.Sequence();
        _currentAnimation.Append(_animImage.DOFade(0f, _fadeDuration).SetEase(Ease.Linear));
        _currentAnimation.AppendCallback(() => _animImage.sprite = _animSprites[toSpriteIndex]);

        if (_fadeInDelay > 0f)
        {
            _currentAnimation.AppendInterval(_fadeInDelay);
        }

        _currentAnimation.Append(_animImage.DOFade(1f, _fadeDuration).SetEase(Ease.Linear));

        // アニメーションが終わったらページを表示（切り替え）する
        _currentAnimation.OnComplete(() =>
        {
            SwitchPage(fromIndex, toIndex);
            RestoreDefaultAnimImage();
        });
    }

    private void SwitchPage(int fromIndex, int toIndex)
    {
        _pages[fromIndex].gameObject.SetActive(false);
        _pages[toIndex].gameObject.SetActive(true);
        _pages[toIndex].alpha = 1f;
    }

    private void SetAnimImageAlpha(float alpha)
    {
        Color color = _animImage.color;
        color.a = alpha;
        _animImage.color = color;
    }

    private void RestoreDefaultAnimImage()
    {
        if (_animImage == null) return;

        _animImage.gameObject.SetActive(true);
        _animImage.sprite = _defaultAnimSprite;
        SetAnimImageAlpha(1f);
    }
}