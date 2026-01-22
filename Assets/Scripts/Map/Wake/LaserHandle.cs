using UnityEngine;
using UnityEngine.EventSystems;

public enum LaserHandleAxis
{
    Vertical, // 上下移動のみ
    Horizontal // 左右移動のみ
}

public class LaserHandle : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    [Header("Movement Settings")] [SerializeField]
    private LaserHandleAxis _axis = LaserHandleAxis.Vertical;

    [SerializeField] private float _minLimit = -200f;
    [SerializeField] private float _maxLimit = 200f;

    private RectTransform _rectTransform;
    private Canvas _canvas;
    private bool _isDragging;
    private Vector2 _initialAnchoredPosition;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvas = GetComponentInParent<Canvas>();
        _initialAnchoredPosition = _rectTransform.anchoredPosition;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        _isDragging = true;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        _isDragging = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!_isDragging) return;

        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            _rectTransform.parent as RectTransform,
            eventData.position,
            eventData.pressEventCamera,
            out localPoint
        );

        Vector2 newPosition = _rectTransform.anchoredPosition;

        if (_axis == LaserHandleAxis.Vertical)
        {
            // Y軸のみ移動可能
            float clampedY = Mathf.Clamp(localPoint.y, _initialAnchoredPosition.y + _minLimit,
                _initialAnchoredPosition.y + _maxLimit);
            newPosition.y = clampedY;
        }
        else
        {
            // X軸のみ移動可能
            float clampedX = Mathf.Clamp(localPoint.x, _initialAnchoredPosition.x + _minLimit,
                _initialAnchoredPosition.x + _maxLimit);
            newPosition.x = clampedX;
        }

        _rectTransform.anchoredPosition = newPosition;
    }

    /// <summary>
    /// レーザーの交点位置を取得（RectTransformのanchoredPositionを使用）
    /// </summary>
    public Vector2 GetIntersectionPoint()
    {
        return _rectTransform.anchoredPosition;
    }

    /// <summary>
    /// ワールド座標での交点位置を取得
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        return _rectTransform.position;
    }

    /// <summary>
    /// 移動軸を取得
    /// </summary>
    public LaserHandleAxis Axis => _axis;
}