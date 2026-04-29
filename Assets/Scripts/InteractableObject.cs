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
        Pickup
    }

    [Header("Interaction Settings")] [SerializeField]
    protected InteractionType _interactionType = InteractionType.None;

    [Header("Zoom Settings")] [SerializeField]
    private ViewNode _zoomViewNode;

    [Header("Pickup Settings")] [SerializeField]
    protected ItemData _itemToPickup;

    [Header("Debug Settings")] [SerializeField]
    private bool _showClickArea = true;

    [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    // エディタでコンポーネントをアタッチした時に自動実行
    protected virtual void Reset()
    {
        SetupUIComponents();
    }

    // 実行時に必要なコンポーネントを確認・追加
    protected virtual void Awake()
    {
        SetupUIComponents();
    }

    /// <summary>
    /// UIオブジェクトの場合、クリック判定に必要なImageコンポーネントを自動追加
    /// </summary>
    protected void SetupUIComponents()
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

    public bool TryGetPickupItem(out ItemData item)
    {
        if (_interactionType == InteractionType.Pickup && _itemToPickup != null)
        {
            item = _itemToPickup;
            return true;
        }

        item = null;
        return false;
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
                TryPickup();
                break;
        }
    }

    protected virtual bool TryPickup()
    {
        if (_itemToPickup == null)
        {
            return false;
        }

        if (!InventoryManager.Instance.TryAddItem(_itemToPickup))
        {
            return false;
        }

        gameObject.SetActive(false); // 取得したアイテムはシーンから消す
        return true;
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
    }
}