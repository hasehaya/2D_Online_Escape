using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム内の視点（カメラワーク）を管理するシングルトンクラス。
/// 4方向（東西南北）の壁の切り替えと、特定のオブジェクトへの「拡大（ズーム）」および「戻る」遷移をスタック構造で管理する。
/// </summary>
public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance { get; private set; }

    [Header("Initial View")]
    [SerializeField] private ViewData initialView;

    [Header("UI References")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button backButton;
    [SerializeField] private Transform viewContainer; // Prefabを生成する親オブジェクト

    private Stack<ViewData> viewStack = new Stack<ViewData>();
    private ViewData currentViewData;
    private GameObject currentViewInstance; // 現在表示中のPrefabのインスタンス

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (leftButton != null) leftButton.onClick.AddListener(TurnLeft);
        if (rightButton != null) rightButton.onClick.AddListener(TurnRight);
        if (backButton != null) backButton.onClick.AddListener(Return);

        if (initialView != null)
        {
            ShowView(initialView);
        }
    }

    private void ShowView(ViewData viewData)
    {
        // 現在の表示を削除（または非表示）
        if (currentViewInstance != null)
        {
            Destroy(currentViewInstance); // シンプルにするため毎回生成・破棄する
        }

        currentViewData = viewData;

        // 新しいViewを生成
        if (viewData != null && viewData.viewPrefab != null)
        {
            currentViewInstance = Instantiate(viewData.viewPrefab, viewContainer);
        }

        UpdateUI();
    }

    public void TurnRight()
    {
        if (currentViewData != null && currentViewData.rightView != null)
        {
            ShowView(currentViewData.rightView);
        }
    }

    public void TurnLeft()
    {
        if (currentViewData != null && currentViewData.leftView != null)
        {
            ShowView(currentViewData.leftView);
        }
    }

    public void ZoomIn(ViewData viewData)
    {
        if (currentViewData != null)
        {
            viewStack.Push(currentViewData);
        }

        ShowView(viewData);
    }

    public void Return()
    {
        if (viewStack.Count > 0)
        {
            ViewData previousView = viewStack.Pop();
            ShowView(previousView);
        }
    }

    private void UpdateUI()
    {
        // 拡大画面（isZoomView = true）の場合は戻るボタンを表示し、左右移動を隠す
        // ただし、拡大画面でも左右移動できるケース（机の引き出しの左右など）も考えられるため、
        // ViewDataに移動先が設定されているかどうかで判定するのがより柔軟だが、
        // 今回は仕様通り「拡大中は戻るボタン」とする。
        
        bool isZoomed = currentViewData != null && currentViewData.isZoomView;
        
        if (leftButton != null) leftButton.gameObject.SetActive(!isZoomed && currentViewData?.leftView != null);
        if (rightButton != null) rightButton.gameObject.SetActive(!isZoomed && currentViewData?.rightView != null);
        if (backButton != null) backButton.gameObject.SetActive(isZoomed);
    }
}
