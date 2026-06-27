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
    [Header("Images")] [SerializeField] private Image _closedImage;
    [SerializeField] private Image _openImage;

    [Header("Open Animation")] [SerializeField]
    private float _fadeDuration = 0.2f;

    [SerializeField] private Ease _fadeEase = Ease.Linear;

    [Header("Open SE")] [SerializeField] private SESoundType _openSEType = SESoundType.CorrectBoxOpen;

    private bool _isOpen;

    public bool IsOpen => _isOpen;

    private void Start()
    {
        ApplyImageState();
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
        if (_closedImage == null || _openImage == null) return;

        _closedImage.DOKill();
        _openImage.DOKill();

        FadeSwitchService.Switch(_closedImage, _openImage, _fadeDuration, _fadeEase);
    }

    private void ApplyImageState()
    {
        if (_closedImage != null)
        {
            _closedImage.gameObject.SetActive(!_isOpen);
            Color c = _closedImage.color;
            c.a = 1f;
            _closedImage.color = c;
        }

        if (_openImage != null)
        {
            _openImage.gameObject.SetActive(_isOpen);
            Color c = _openImage.color;
            c.a = 1f;
            _openImage.color = c;
        }
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
        ApplyImageState();
    }
}