using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// ゲーム内の視点（カメラワーク）を管理するシングルトンクラス。
/// 4方向（東西南北）の壁の切り替えと、特定のオブジェクトへの「拡大（ズーム）」および「戻る」遷移をスタック構造で管理する。
/// カメラの位置を各ViewPointの位置に移動させることで視点を切り替える。
/// </summary>
public class ViewController : MonoBehaviour
{
    public static ViewController Instance { get; private set; }

    [Header("Views")] [SerializeField] private ViewNode _initialView;

    [Header("UI References")] [SerializeField]
    private Button _leftButton;

    [SerializeField] private Button _rightButton;
    [SerializeField] private Button _backButton;

    private Stack<IViewable> _viewStack = new Stack<IViewable>();
    private IViewable _currentViewable;
    private ViewNode _currentViewNode; // ViewNode固有の操作用
    private Camera _mainCamera;

    public bool IsShowingStill => _currentViewable is StillNode;

    private bool IsWakeCompleted => GameStateService.Instance.GetFlag(FlagType.Wake_LaserCompleted);

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

        if (_initialView != null)
        {
            ShowView(_initialView);
        }
    }

    private void OnEnable()
    {
        GameStateService.Instance.OnFlagChanged += OnFlagChanged;
        GameStateService.Instance.OnPropertyChanged += OnPropertyChanged;
    }

    private void OnDisable()
    {
        GameStateService.Instance.OnFlagChanged -= OnFlagChanged;
        GameStateService.Instance.OnPropertyChanged -= OnPropertyChanged;
    }

    public void ShowView(ViewNode viewNode)
    {
        ShowViewable(viewNode);
    }

    /// <summary>
    /// IViewableを表示する汎用メソッド
    /// </summary>
    public void ShowViewable(IViewable viewable)
    {
        _currentViewable = viewable;

        // ViewNodeの場合は_currentViewNodeにも保持
        _currentViewNode = viewable as ViewNode;

        // カメラをViewableの位置に移動
        if (viewable != null && _mainCamera != null)
        {
            Transform viewTransform = viewable.GetTransform();
            Vector3 targetPosition = viewTransform.position;
            Vector3 cameraPosition = _mainCamera.transform.position;
            _mainCamera.transform.position = new Vector3(targetPosition.x, targetPosition.y, cameraPosition.z);
        }

        // 新しいViewableのOnEnterを呼ぶ
        _currentViewable?.OnEnter();

        UpdateUI();
    }

    private void TurnRight()
    {
        if (_currentViewNode != null && _currentViewNode.rightView != null)
        {
            ShowView(_currentViewNode.rightView);
        }
    }

    private void TurnLeft()
    {
        if (_currentViewNode != null && _currentViewNode.leftView != null)
        {
            ShowView(_currentViewNode.leftView);
        }
    }

    public void ZoomIn(ViewNode viewNode)
    {
        if (_currentViewable != null)
        {
            _viewStack.Push(_currentViewable);
        }

        ShowView(viewNode);
    }

    /// <summary>
    /// StillNodeへの遷移（スタックに現在のViewableを保存）
    /// </summary>
    public void ShowStill(StillNode stillNode)
    {
        if (_currentViewable != null)
        {
            _viewStack.Push(_currentViewable);
        }

        ShowViewable(stillNode);
    }

    public void ShowStillForAll(StillNode stillNode)
    {
        if (stillNode == null)
        {
            Debug.LogWarning("[ViewController] 遷移先のStillNodeが設定されていません。");
            return;
        }

        string stillGroup = stillNode.transform.parent != null
            ? stillNode.transform.parent.name
            : stillNode.name;
        if (Photon.Pun.PhotonNetwork.InRoom)
        {
            string request = $"{stillGroup}|{System.Guid.NewGuid():N}";
            GameStateService.Instance.SetString(PhotonRoomPropertyKeys.SharedStillTransition, request);
            return;
        }

        ShowStill(stillNode);
    }

    private void Return()
    {
        if (_viewStack.Count > 0)
        {
            IViewable previousView = _viewStack.Pop();

            // ViewNodeの場合はShowView、それ以外はShowViewableを呼ぶ
            if (previousView is ViewNode viewNode)
            {
                ShowView(viewNode);
            }
            else
            {
                ShowViewable(previousView);
            }
        }
    }

    private void UpdateUI()
    {
        // 拡大画面（isZoomView = true）の場合は戻るボタンを表示し、左右移動を隠す
        // ただし、拡大画面でも左右移動できるケース（机の引き出しの左右など）も考えられるため、
        // ViewDataに移動先が設定されているかどうかで判定するのがより柔軟だが、
        // 今回は仕様通り「拡大中は戻るボタン」とする。

        bool isZoomed = _currentViewNode != null && _currentViewNode.isZoomView;

        if (InventoryManager.Instance != null)
        {
            InventoryManager.Instance.SetVisible(IsWakeCompleted && !IsShowingStill);
        }

        if (_leftButton != null) _leftButton.gameObject.SetActive(!isZoomed && _currentViewNode?.leftView != null);
        if (_rightButton != null) _rightButton.gameObject.SetActive(!isZoomed && _currentViewNode?.rightView != null);
        if (_backButton != null) _backButton.gameObject.SetActive(isZoomed);
    }

    private void OnFlagChanged(FlagType flag, bool value)
    {
        if (flag == FlagType.Wake_LaserCompleted)
        {
            UpdateUI();
        }
    }

    private void OnPropertyChanged(string key, object value)
    {
        if (key != PhotonRoomPropertyKeys.SharedStillTransition || !(value is string request))
        {
            return;
        }

        string stillGroup = request.Split('|')[0];
        StillNode stillNode = null;
        StillNode[] candidates = FindObjectsByType<StillNode>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (StillNode candidate in candidates)
        {
            if (candidate.transform.parent != null && candidate.transform.parent.name == stillGroup)
            {
                stillNode = candidate;
                break;
            }
        }

        if (stillNode == null)
        {
            Debug.LogWarning($"[ViewController] 共有Stillグループ '{stillGroup}' が見つかりません。");
            return;
        }

        ShowStill(stillNode);
    }
}
