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
    [SerializeField] private ViewPoint _initialView;
    [SerializeField] private ViewPoint[] _allViews; // 管理する全てのViewPoint

    [Header("UI References")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _backButton;

    private Stack<ViewPoint> _viewStack = new Stack<ViewPoint>();
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
        if (_leftButton != null) _leftButton.onClick.AddListener(TurnLeft);
        if (_rightButton != null) _rightButton.onClick.AddListener(TurnRight);
        if (_backButton != null) _backButton.onClick.AddListener(Return);

        // 全てのViewを非表示にする
        InitializeAllViews();

        if (_initialView != null)
        {
            ShowView(_initialView);
        }
    }

    /// <summary>
    /// 全てのViewを非表示に初期化する
    /// </summary>
    private void InitializeAllViews()
    {
        foreach (var view in _allViews)
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
            _viewStack.Push(_currentViewPoint);
        }

        ShowView(viewPoint);
    }

    public void Return()
    {
        if (_viewStack.Count > 0)
        {
            ViewPoint previousView = _viewStack.Pop();
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
        
        if (_leftButton != null) _leftButton.gameObject.SetActive(!isZoomed && _currentViewPoint?.leftView != null);
        if (_rightButton != null) _rightButton.gameObject.SetActive(!isZoomed && _currentViewPoint?.rightView != null);
        if (_backButton != null) _backButton.gameObject.SetActive(isZoomed);
    }
}
