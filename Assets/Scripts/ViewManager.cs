using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム内の視点（カメラワーク）を管理するシングルトンクラス。
/// 4方向（東西南北）の壁の切り替えと、特定のオブジェクトへの「拡大（ズーム）」および「戻る」遷移をスタック構造で管理する。
/// ViewPointのGameObjectをSetActiveで切り替えることで、内部オブジェクトの状態を維持する。
/// </summary>
public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance { get; private set; }

    [Header("Views")]
    [SerializeField] private ViewPoint initialView;
    [SerializeField] private ViewPoint[] allViews; // 管理する全てのViewPoint

    [Header("UI References")]
    [SerializeField] private Button leftButton;
    [SerializeField] private Button rightButton;
    [SerializeField] private Button backButton;

    private Stack<ViewPoint> viewStack = new Stack<ViewPoint>();
    private ViewPoint _currentViewPoint;

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

        // 全てのViewを非表示にする
        InitializeAllViews();

        if (initialView != null)
        {
            ShowView(initialView);
        }
    }

    /// <summary>
    /// 全てのViewを非表示に初期化する
    /// </summary>
    private void InitializeAllViews()
    {
        foreach (var view in allViews)
        {
            if (view != null)
            {
                view.gameObject.SetActive(false);
            }
        }
    }

    private void ShowView(ViewPoint viewPoint)
    {
        // 現在のViewを非表示にする
        if (_currentViewPoint != null)
        {
            _currentViewPoint.gameObject.SetActive(false);
        }

        _currentViewPoint = viewPoint;

        // 新しいViewを表示
        if (viewPoint != null)
        {
            viewPoint.gameObject.SetActive(true);
        }

        UpdateUI();
    }

    public void TurnRight()
    {
        if (_currentViewPoint != null && _currentViewPoint.rightView != null)
        {
            ShowView(_currentViewPoint.rightView);
        }
    }

    public void TurnLeft()
    {
        if (_currentViewPoint != null && _currentViewPoint.leftView != null)
        {
            ShowView(_currentViewPoint.leftView);
        }
    }

    public void ZoomIn(ViewPoint viewPoint)
    {
        if (_currentViewPoint != null)
        {
            viewStack.Push(_currentViewPoint);
        }

        ShowView(viewPoint);
    }

    public void Return()
    {
        if (viewStack.Count > 0)
        {
            ViewPoint previousView = viewStack.Pop();
            ShowView(previousView);
        }
    }

    private void UpdateUI()
    {
        // 拡大画面（isZoomView = true）の場合は戻るボタンを表示し、左右移動を隠す
        // ただし、拡大画面でも左右移動できるケース（机の引き出しの左右など）も考えられるため、
        // ViewDataに移動先が設定されているかどうかで判定するのがより柔軟だが、
        // 今回は仕様通り「拡大中は戻るボタン」とする。
        
        bool isZoomed = _currentViewPoint != null && _currentViewPoint.isZoomView;
        
        if (leftButton != null) leftButton.gameObject.SetActive(!isZoomed && _currentViewPoint?.leftView != null);
        if (rightButton != null) rightButton.gameObject.SetActive(!isZoomed && _currentViewPoint?.rightView != null);
        if (backButton != null) backButton.gameObject.SetActive(isZoomed);
    }
}
