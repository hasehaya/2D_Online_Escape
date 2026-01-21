using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// クリック可能なオブジェクトの基底クラス。
/// プレイヤーのクリック操作を検知し、「拡大表示」「アイテム取得」「メッセージ表示」などの具体的なアクションを実行する役割を持つ。
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

    [Header("Interaction Settings")]
    [SerializeField] private InteractionType _interactionType = InteractionType.None;
    
    [Header("Zoom Settings")]
    [SerializeField] private ViewNode _zoomViewNode;

    [Header("Pickup Settings")]
    [SerializeField] private ItemData _itemToPickup;

    [Header("Message Settings")]
    [TextArea] [SerializeField] private string _messageText;

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
                    ViewManager.Instance.ZoomIn(_zoomViewNode);
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
}
