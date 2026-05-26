using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// クリック可能なオブジェクトの基底クラス。
/// プレイヤーのクリック操作を検知し、派生クラスで具体的なアクションを実装する。
/// UIオブジェクトにアタッチすると自動的にImageコンポーネントが追加されます。
/// </summary>
public abstract class InteractableObject : MonoBehaviour, IPointerClickHandler
{
    private const string ClickAreaObjectName = "[Generated]InteractableClickArea";

    [Header("Click Area Settings")] [SerializeField]
    private Vector2 _clickAreaSize = new Vector2(100f, 100f);

    [SerializeField] private Vector2 _clickAreaOffset;

    [SerializeField, HideInInspector] private bool _clickAreaInitialized;

    [Header("Debug Settings")] [SerializeField]
    private bool _showClickArea = true;

    [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    protected virtual void Reset()
    {
        CaptureClickAreaFromSource(true);
    }

    protected virtual void Awake()
    {
        CaptureClickAreaFromSource(false);
        SetupUIComponents();
    }

    protected virtual void OnValidate()
    {
        CaptureClickAreaFromSource(false);
    }

    /// <summary>
    /// UIオブジェクトの場合、見た目のRectTransformとは独立したクリック判定用の子要素を自動追加
    /// </summary>
    protected void SetupUIComponents()
    {
        if (!Application.isPlaying)
        {
            return;
        }

        if (TryGetComponent(out RectTransform _))
        {
            SetupUIHitArea();
            return;
        }

        SetupColliderHitArea();
    }

    /// <summary>
    /// 初期化時のみ、クリック判定の初期値をソースからコピーする
    /// </summary>
    protected void CaptureClickAreaFromSource(bool force)
    {
        if (!force && _clickAreaInitialized)
        {
            return;
        }

        if (TryGetComponent(out RectTransform rectTransform))
        {
            _clickAreaSize = rectTransform.rect.size;
            _clickAreaOffset = rectTransform.rect.center;
            _clickAreaInitialized = true;
            return;
        }

        if (TryGetComponent(out BoxCollider2D boxCollider))
        {
            _clickAreaSize = boxCollider.size;
            _clickAreaOffset = boxCollider.offset;
            _clickAreaInitialized = true;
        }
    }

    [ContextMenu("Capture Click Area From Current Source")]
    private void CaptureClickAreaFromCurrentSource()
    {
        CaptureClickAreaFromSource(true);
        SetupUIComponents();
    }

    private void SetupUIHitArea()
    {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null && rootImage.raycastTarget)
        {
            rootImage.raycastTarget = false;
        }

        Transform clickAreaTransform = transform.Find(ClickAreaObjectName);
        GameObject clickAreaObject = clickAreaTransform != null
            ? clickAreaTransform.gameObject
            : new GameObject(ClickAreaObjectName, typeof(RectTransform), typeof(Image), typeof(LayoutElement));

        if (clickAreaTransform == null)
        {
            clickAreaObject.transform.SetParent(transform, false);
            clickAreaObject.transform.SetAsLastSibling();
            Debug.Log($"[InteractableObject] {gameObject.name} に独立したクリック判定エリアを自動追加しました");
        }

        RectTransform clickAreaRect = clickAreaObject.GetComponent<RectTransform>();
        clickAreaRect.anchorMin = new Vector2(0.5f, 0.5f);
        clickAreaRect.anchorMax = new Vector2(0.5f, 0.5f);
        clickAreaRect.pivot = new Vector2(0.5f, 0.5f);
        clickAreaRect.anchoredPosition = _clickAreaOffset;
        clickAreaRect.sizeDelta = _clickAreaSize;
        clickAreaRect.localScale = Vector3.one;

        Image clickAreaImage = clickAreaObject.GetComponent<Image>();
        clickAreaImage.color = new Color(1f, 1f, 1f, 0.01f);
        clickAreaImage.raycastTarget = true;

        LayoutElement layoutElement = clickAreaObject.GetComponent<LayoutElement>();
        layoutElement.ignoreLayout = true;
    }

    private void SetupColliderHitArea()
    {
        if (TryGetComponent(out BoxCollider2D boxCollider))
        {
            boxCollider.size = _clickAreaSize;
            boxCollider.offset = _clickAreaOffset;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Interact();
    }

    // コライダーを持つオブジェクト（非UI）でもクリックを検知できるようにする
    private void OnMouseDown()
    {
        // UI越しのクリックでない場合のみ反応させる
        EventSystem eventSystem = EventSystem.current;
        if (eventSystem == null || !eventSystem.IsPointerOverGameObject())
        {
            Interact();
        }
    }

    protected virtual void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}");
    }

    private void OnDrawGizmos()
    {
        if (!_showClickArea) return;

        Gizmos.color = _gizmoColor;

        if (TryGetComponent(out RectTransform _))
        {
            DrawClickAreaGizmo(_clickAreaSize, _clickAreaOffset);
            return;
        }

        if (TryGetComponent(out BoxCollider2D boxCollider))
        {
            DrawClickAreaGizmo(boxCollider.size, boxCollider.offset);
            return;
        }

        DrawClickAreaGizmo(_clickAreaSize, _clickAreaOffset);
    }

    private void DrawClickAreaGizmo(Vector2 size, Vector2 offset)
    {
        Vector3 center = transform.TransformPoint(new Vector3(offset.x, offset.y, 0f));
        Matrix4x4 oldMatrix = Gizmos.matrix;
        Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation,
            new Vector3(transform.lossyScale.x, transform.lossyScale.y, 1f));
        Gizmos.DrawWireCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
        Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
        Gizmos.matrix = oldMatrix;
    }
}