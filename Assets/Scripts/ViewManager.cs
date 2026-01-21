using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム内の視点（カメラワーク）を管理するシングルトンクラス。
/// 4方向（東西南北）の壁の切り替えと、特定のオブジェクトへの「拡大（ズーム）」および「戻る」遷移をスタック構造で管理する。
/// カメラの位置を各ViewPointの位置に移動させることで視点を切り替える。
/// </summary>
public class ViewManager : MonoBehaviour
{
    public static ViewManager Instance { get; private set; }

    [Header("Views")]
    [SerializeField] private ViewNode _initialView;
    [SerializeField] private ViewNode[] _allViews; // 管理する全てのViewPoint

    [Header("UI References")]
    [SerializeField] private Button _leftButton;
    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _backButton;

    private Stack<ViewNode> _viewStack = new Stack<ViewNode>();
    private ViewNode _currentViewNode;
    private Camera _mainCamera;

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
        _mainCamera = Camera.main;

        if (_leftButton != null) _leftButton.onClick.AddListener(TurnLeft);
        if (_rightButton != null) _rightButton.onClick.AddListener(TurnRight);
        if (_backButton != null) _backButton.onClick.AddListener(Return);

        // 全てのViewをアクティブにする
        InitializeAllViews();

        if (_initialView != null)
        {
            ShowView(_initialView);
        }
    }

    /// <summary>
    /// 全てのViewをアクティブ状態に初期化する
    /// </summary>
    private void InitializeAllViews()
    {
        foreach (var view in _allViews)
        {
            if (view != null)
            {
                view.gameObject.SetActive(true);
            }
        }
    }

    private void ShowView(ViewNode viewNode)
    {
        _currentViewNode = viewNode;

        // カメラをViewPointの位置に移動（Z座標は維持してRectTransformへの影響を回避）
        if (viewNode != null && _mainCamera != null)
        {
            Vector3 targetPosition = viewNode.transform.position;
            Vector3 cameraPosition = _mainCamera.transform.position;
            _mainCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraPosition.z);
        }

        UpdateUI();
    }

    public void TurnRight()
    {
        if (_currentViewNode != null && _currentViewNode.rightView != null)
        {
            ShowView(_currentViewNode.rightView);
        }
    }

    public void TurnLeft()
    {
        if (_currentViewNode != null && _currentViewNode.leftView != null)
        {
            ShowView(_currentViewNode.leftView);
        }
    }

    public void ZoomIn(ViewNode viewNode)
    {
        if (_currentViewNode != null)
        {
            _viewStack.Push(_currentViewNode);
        }

        ShowView(viewNode);
    }

    public void Return()
    {
        if (_viewStack.Count > 0)
        {
            ViewNode previousView = _viewStack.Pop();
            ShowView(previousView);
        }
    }

    private void UpdateUI()
    {
        // 拡大画面（isZoomView = true）の場合は戻るボタンを表示し、左右移動を隠す
        // ただし、拡大画面でも左右移動できるケース（机の引き出しの左右など）も考えられるため、
        // ViewDataに移動先が設定されているかどうかで判定するのがより柔軟だが、
        // 今回は仕様通り「拡大中は戻るボタン」とする。
        
        bool isZoomed = _currentViewNode != null && _currentViewNode.isZoomView;
        
        if (_leftButton != null) _leftButton.gameObject.SetActive(!isZoomed && _currentViewNode?.leftView != null);
        if (_rightButton != null) _rightButton.gameObject.SetActive(!isZoomed && _currentViewNode?.rightView != null);
        if (_backButton != null) _backButton.gameObject.SetActive(isZoomed);
    }
}
