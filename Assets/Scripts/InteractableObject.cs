using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// クリック可能なオブジェクトの基底クラス。
/// プレイヤーのクリック操作を検知し、「拡大表示」「アイテム取得」「メッセージ表示」などの具体的なアクションを実行する役割を持つ。
/// このコンポーネントをアタッチするだけで、UIオブジェクトの場合は自動的にImageコンポーネントが追加されます。
/// </summary>
public class InteractableObject : MonoBehaviour, IPointerClickHandler
{
    public enum InteractionType
    {
        None,
        Zoom,
        Pickup,
        Message
    }

    [Header("Interaction Settings")] [SerializeField]
    private InteractionType _interactionType = InteractionType.None;

    [Header("Zoom Settings")] [SerializeField]
    private ViewNode _zoomViewNode;

    [Header("Pickup Settings")] [SerializeField]
    private ItemData _itemToPickup;

    [Header("Message Settings")] [TextArea] [SerializeField]
    private string _messageText;

    [Header("Debug Settings")] [SerializeField]
    private bool _showClickArea = true;

    [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    // エディタでコンポーネントをアタッチした時に自動実行
    private void Reset()
    {
        SetupUIComponents();
    }

    // 実行時に必要なコンポーネントを確認・追加
    private void Awake()
    {
        SetupUIComponents();
    }

    /// <summary>
    /// UIオブジェクトの場合、クリック判定に必要なImageコンポーネントを自動追加
    /// </summary>
    private void SetupUIComponents()
    {
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // UIオブジェクトの場合、Imageコンポーネントが必要
            Image image = GetComponent<Image>();
            if (image == null)
            {
                image = gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.01f); // ほぼ透明（完全に0だとクリック判定が取れない場合がある）
                image.raycastTarget = true;
                Debug.Log($"[InteractableObject] {gameObject.name} に透明なImageを自動追加しました");
            }
            else if (!image.raycastTarget)
            {
                // Imageはあるがraycastが無効の場合、有効にする
                image.raycastTarget = true;
                Debug.Log($"[InteractableObject] {gameObject.name} のImage.raycastTargetを有効にしました");
            }
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
        if (!EventSystem.current.IsPointerOverGameObject())
        {
            Interact();
        }
    }

    protected virtual void Interact()
    {
        Debug.Log($"Interacted with {gameObject.name}");

        switch (_interactionType)
        {
            case InteractionType.Zoom:
                if (_zoomViewNode != null)
                {
                    ViewController.Instance.ZoomIn(_zoomViewNode);
                }

                break;

            case InteractionType.Pickup:
                if (_itemToPickup != null)
                {
                    InventoryManager.Instance.AddItem(_itemToPickup);
                    gameObject.SetActive(false); // 取得したアイテムはシーンから消す
                }

                break;

            case InteractionType.Message:
                Debug.Log($"Message: {_messageText}");
                // TODO: UIにメッセージを表示する処理を実装する
                break;
        }
    }

    private void OnDrawGizmos()
    {
        if (!_showClickArea) return;

        Gizmos.color = _gizmoColor;

        // RectTransform（UI）の場合
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            // ワールド座標の四隅を取得
            Vector3[] corners = new Vector3[4];
            rectTransform.GetWorldCorners(corners);

            // 四角形を描画
            Gizmos.DrawLine(corners[0], corners[1]);
            Gizmos.DrawLine(corners[1], corners[2]);
            Gizmos.DrawLine(corners[2], corners[3]);
            Gizmos.DrawLine(corners[3], corners[0]);

            // 塗りつぶし（簡易版）
            Vector3 center = (corners[0] + corners[2]) / 2f;
            Vector3 size = corners[2] - corners[0];
            Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
            Gizmos.DrawCube(Vector3.zero, new Vector3(size.x, size.y, 0.01f));
            Gizmos.matrix = Matrix4x4.identity;
        }
        else
        {
            // Collider2Dの場合
            BoxCollider2D boxCollider = GetComponent<BoxCollider2D>();
            if (boxCollider != null)
            {
                Vector3 center = transform.TransformPoint(boxCollider.offset);
                Vector3 size = new Vector3(
                    boxCollider.size.x * transform.lossyScale.x,
                    boxCollider.size.y * transform.lossyScale.y,
                    0.01f
                );
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            CircleCollider2D circleCollider = GetComponent<CircleCollider2D>();
            if (circleCollider != null)
            {
                Vector3 center = transform.TransformPoint(circleCollider.offset);
                float radius = circleCollider.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y);

                // 円を描画
                DrawCircleGizmo(center, radius);
            }

            PolygonCollider2D polygonCollider = GetComponent<PolygonCollider2D>();
            if (polygonCollider != null)
            {
                for (int i = 0; i < polygonCollider.pathCount; i++)
                {
                    Vector2[] points = polygonCollider.GetPath(i);
                    for (int j = 0; j < points.Length; j++)
                    {
                        Vector3 p1 = transform.TransformPoint(points[j]);
                        Vector3 p2 = transform.TransformPoint(points[(j + 1) % points.Length]);
                        Gizmos.DrawLine(p1, p2);
                    }
                }
            }

            // 3D Colliderの場合
            BoxCollider boxCollider3D = GetComponent<BoxCollider>();
            if (boxCollider3D != null)
            {
                Vector3 center = transform.TransformPoint(boxCollider3D.center);
                Vector3 size = Vector3.Scale(boxCollider3D.size, transform.lossyScale);
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawCube(Vector3.zero, size);
                Gizmos.DrawWireCube(Vector3.zero, size);
                Gizmos.matrix = Matrix4x4.identity;
            }

            SphereCollider sphereCollider = GetComponent<SphereCollider>();
            if (sphereCollider != null)
            {
                Vector3 center = transform.TransformPoint(sphereCollider.center);
                float radius = sphereCollider.radius *
                               Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Gizmos.DrawWireSphere(center, radius);
            }
        }
    }

    private void DrawCircleGizmo(Vector3 center, float radius)
    {
        int segments = 32;
        float angle = 0f;
        Vector3 lastPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;

        for (int i = 1; i <= segments; i++)
        {
            angle = i * 2f * Mathf.PI / segments;
            Vector3 newPoint = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Gizmos.DrawLine(lastPoint, newPoint);
            lastPoint = newPoint;
        }

        // 塗りつぶし（簡易版）
        for (int i = 0; i < segments; i++)
        {
            angle = i * 2f * Mathf.PI / segments;
            Vector3 point = center + new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0) * radius;
            Gizmos.DrawLine(center, point);
        }
    }
}