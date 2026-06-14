using System;
using DG.Tweening;
using Save;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Saveable bookshelf that slides left when its public request event fires.
/// </summary>
public class BookShelfObject : SaveableBehaviour
{
    [Header("Slide Target")] [SerializeField]
    private Transform _slideTarget;

    [Header("Slide Settings")] [SerializeField]
    private float _slideDistance = 300f;

    [SerializeField] private float _slideDuration = 0.4f;
    [SerializeField] private Ease _slideEase = Ease.InOutQuad;

    public UnityEvent OnSlideRequested = new UnityEvent();

    private RectTransform _slideRectTransform;
    private Vector2 _initialAnchoredPosition;
    private Vector3 _initialLocalPosition;
    private Tween _slideTween;
    private bool _isSlid;
    private bool _initialized;

    public bool IsSlid => _isSlid;

    private void Awake()
    {
        Initialize();
        OnSlideRequested.AddListener(SlideLeft);
    }

    private void Start()
    {
        ApplySlideState();
    }

    private void OnDestroy()
    {
        OnSlideRequested.RemoveListener(SlideLeft);
        _slideTween?.Kill();
    }

    /// <summary>
    /// Entry point for Inspector UnityEvents and other scripts.
    /// </summary>
    public void RequestSlide()
    {
        OnSlideRequested?.Invoke();
    }

    public void SlideLeft()
    {
        Initialize();

        if (_isSlid || _slideTarget == null)
        {
            return;
        }

        _isSlid = true;
        _slideTween?.Kill();

        if (_slideRectTransform != null)
        {
            Vector2 targetPosition = GetSlidAnchoredPosition();
            _slideTween = _slideRectTransform.DOAnchorPos(targetPosition, _slideDuration);
        }
        else
        {
            Vector3 targetPosition = GetSlidLocalPosition();
            _slideTween = _slideTarget.DOLocalMove(targetPosition, _slideDuration);
        }

        _slideTween
            .SetEase(_slideEase)
            .OnComplete(() => { PairSaveCoordinator.RequestSaveIfAvailable(); });
    }

    private void Initialize()
    {
        if (_initialized)
        {
            return;
        }

        if (_slideTarget == null)
        {
            _slideTarget = transform;
        }

        _slideRectTransform = _slideTarget as RectTransform;
        if (_slideRectTransform != null)
        {
            _initialAnchoredPosition = _slideRectTransform.anchoredPosition;
        }

        _initialLocalPosition = _slideTarget.localPosition;
        _initialized = true;
    }

    private void ApplySlideState()
    {
        Initialize();

        if (_slideTarget == null)
        {
            return;
        }

        _slideTween?.Kill();

        if (_slideRectTransform != null)
        {
            _slideRectTransform.anchoredPosition = _isSlid ? GetSlidAnchoredPosition() : _initialAnchoredPosition;
            return;
        }

        _slideTarget.localPosition = _isSlid ? GetSlidLocalPosition() : _initialLocalPosition;
    }

    private Vector2 GetSlidAnchoredPosition()
    {
        return _initialAnchoredPosition + Vector2.left * _slideDistance;
    }

    private Vector3 GetSlidLocalPosition()
    {
        return _initialLocalPosition + Vector3.left * _slideDistance;
    }

    [Serializable]
    private struct BookShelfState
    {
        public bool isSlid;
    }

    public override string CaptureState()
    {
        BookShelfState state = new BookShelfState { isSlid = _isSlid };
        return JsonUtility.ToJson(state);
    }

    public override void RestoreState(string stateJson)
    {
        if (string.IsNullOrEmpty(stateJson))
        {
            return;
        }

        BookShelfState state = JsonUtility.FromJson<BookShelfState>(stateJson);
        _isSlid = state.isSlid;
        ApplySlideState();
    }
}