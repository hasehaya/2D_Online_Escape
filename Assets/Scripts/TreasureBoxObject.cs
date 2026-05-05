using System;
using DG.Tweening;
using Save;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 開閉状態を保存可能な宝箱オブジェクト。
/// 一度開けると閉じることができない不可逆な状態管理を行い、
/// 状態に応じて Sprite を切り替える。
/// </summary>
public class TreasureBoxObject : SaveableBehaviour
{
    [Header("Sprites")] [SerializeField] private Image _image;
    [SerializeField] private Sprite _closedSprite;
    [SerializeField] private Sprite _openSprite;

    [Header("Open Animation")] [SerializeField]
    private float _fadeDuration = 0.2f;

    [SerializeField] private Ease _fadeEase = Ease.Linear;

    [Header("Open SE")] [SerializeField] private SESoundType _openSEType = SESoundType.CorrectBoxOpen;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        ApplySprite();
    }

    /// <summary>
    /// 宝箱を開ける。
    /// 既に開いている場合は何もしない（不可逆）。
    /// </summary>
    public void Open()
    {
        if (_isOpen) return;

        _isOpen = true;

        AudioManager.Instance.PlaySE(_openSEType);

        PlayOpenAnimation();
        PairSaveCoordinator.RequestSaveIfAvailable();
    }

    private void PlayOpenAnimation()
    {
        if (_image == null) return;

        _image.DOKill();

        Sequence sequence = DOTween.Sequence();
        sequence.Append(_image.DOFade(0f, _fadeDuration).SetEase(_fadeEase));
        sequence.AppendCallback(() =>
        {
            if (_openSprite != null)
            {
                _image.sprite = _openSprite;
            }
        });
        sequence.Append(_image.DOFade(1f, _fadeDuration).SetEase(_fadeEase));
    }

    private void ApplySprite()
    {
        if (_image == null) return;

        _image.sprite = _isOpen ? _openSprite : _closedSprite;

        Color color = _image.color;
        color.a = 1f;
        _image.color = color;
    }

    [Serializable]
    private struct TreasureBoxState
    {
        public bool isOpen;
    }

    public override string CaptureState()
    {
        TreasureBoxState state = new TreasureBoxState { isOpen = _isOpen };
        return JsonUtility.ToJson(state);
    }

    public override void RestoreState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson)) return;

        TreasureBoxState state = JsonUtility.FromJson<TreasureBoxState>(stateJson);
        _isOpen = state.isOpen;
        ApplySprite();
    }
}