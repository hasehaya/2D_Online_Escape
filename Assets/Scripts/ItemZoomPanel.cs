using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// アイテム拡大表示のUI制御を担当するパネル。
/// 表示/非表示と閉じるボタンの挙動を内包する。
/// </summary>
public class ItemZoomPanel : MonoBehaviour
{
    [Header("UI References")] [SerializeField]
    private Image _itemZoomImage;

    [SerializeField] private Button _closeButton;

    private void Start()
    {
        _closeButton.onClick.AddListener(Close);
        Close();
    }

    public void Open(Sprite icon)
    {
        _itemZoomImage.sprite = icon;
        _itemZoomImage.enabled = true;
        gameObject.SetActive(true);
    }

    public void Close()
    {
        _itemZoomImage.sprite = null;
        _itemZoomImage.enabled = false;
        gameObject.SetActive(false);
    }
}