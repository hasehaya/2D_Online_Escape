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
    [Header("Debug Settings")] [SerializeField]
    private bool _showClickArea = true;

    [SerializeField] private Color _gizmoColor = new Color(0f, 1f, 0f, 0.3f);

    protected virtual void Reset()
    {
        SetupUIComponents();
    }

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