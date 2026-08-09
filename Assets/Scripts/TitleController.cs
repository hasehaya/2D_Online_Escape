using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// タイトル画面の制御を行うクラス。
/// Photonサーバーへの接続、部屋（ルーム）の作成、および既存の部屋への参加処理を担当する。
/// アプリケーションのエントリーポイントとしての役割を持つ。
/// </summary>
public class TitleController : MonoBehaviourPunCallbacks
{
    [Header("UI References")] [SerializeField]
    private TMP_InputField _roomIdInputField; // 部屋ID入力用

    [SerializeField] private Button _createRoomButton;
    [SerializeField] private Button _joinRoomButton;
    [SerializeField] private Button _settingsButton;
    [SerializeField] private TextMeshProUGUI _statusText;
    [SerializeField] private GameObject _connectingPanel;
    [SerializeField] private SettingsController _settingsController;
    [SerializeField] private TMP_Dropdown _languageDropdown;

    private List<RoomInfo> _cachedRoomList = new List<RoomInfo>();
    private string _currentStatusKey;
    private object[] _currentStatusArgs;

    private void Start()
    {
        LocalIdentityProvider.GetOrCreateLocalPlayerId();

        EnsureLanguageDropdown();

#if UNITY_EDITOR
        CreateDeleteSaveDataButton();
#endif

        _createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        _joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (_settingsButton != null && _settingsController != null)
        {
            _settingsButton.onClick.AddListener(_settingsController.OpenSettings);
        }

        _languageDropdown.onValueChanged.AddListener(OnLanguageChanged);

        SetInteractable(false);
        SetStatus("title.connecting");

        // サーバーへの接続がまだ確立されていない場合のみ接続処理を行う
        if (!PhotonNetwork.IsConnected)
        {
            PhotonNetwork.ConnectUsingSettings();
        }
        else
        {
            OnConnectedToMaster();
        }
    }

    private void EnsureLanguageDropdown()
    {
        if (_languageDropdown == null)
        {
            Canvas canvas = FindFirstObjectByType<Canvas>();
            GameObject dropdownObject = TMP_DefaultControls.CreateDropdown(new TMP_DefaultControls.Resources());
            dropdownObject.name = "LanguageDropdown";
            dropdownObject.transform.SetParent(canvas.transform, false);
            RectTransform rect = dropdownObject.GetComponent<RectTransform>();
            rect.anchorMin = rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 1f);
            rect.anchoredPosition = new Vector2(-32f, -32f);
            rect.sizeDelta = new Vector2(240f, 52f);
            _languageDropdown = dropdownObject.GetComponent<TMP_Dropdown>();
        }

        _languageDropdown.ClearOptions();
        _languageDropdown.AddOptions(new List<string> { "日本語", "English" });
        int language = LocalizationManager.Instance != null
            ? LocalizationManager.Instance.CurrentLanguageIndex
            : PlayerPrefs.GetInt(LocalizationManager.LanguageKey, 0);
        _languageDropdown.SetValueWithoutNotify(language);
    }

#if UNITY_EDITOR
    private void CreateDeleteSaveDataButton()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            Debug.LogWarning("[TitleController] セーブデータ削除ボタンを配置するCanvasが見つかりません。");
            return;
        }

        GameObject buttonObject = TMP_DefaultControls.CreateButton(new TMP_DefaultControls.Resources());
        buttonObject.name = "DeleteSaveDataDebugButton";
        buttonObject.transform.SetParent(canvas.transform, false);

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0f);
        rect.pivot = new Vector2(0f, 0f);
        rect.anchoredPosition = new Vector2(24f, 24f);
        rect.sizeDelta = new Vector2(240f, 52f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.65f, 0.12f, 0.12f, 1f);

        TextMeshProUGUI label = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
        label.text = "セーブデータ削除";
        label.color = Color.white;

        buttonObject.GetComponent<Button>().onClick.AddListener(OnDeleteSaveDataClicked);
    }

    private void OnDeleteSaveDataClicked()
    {
        bool confirmed = UnityEditor.EditorUtility.DisplayDialog(
            "セーブデータ削除",
            "ローカルのセーブデータと設定をすべて削除します。元に戻せません。削除しますか？",
            "削除",
            "キャンセル");
        if (!confirmed)
        {
            return;
        }

        PlayerPrefs.DeleteAll();
        PlayerPrefs.Save();
        Debug.Log("[TitleController] ローカルのセーブデータをすべて削除しました。");
        SetStatus("セーブデータを削除しました。");
    }
#endif

    private void OnLanguageChanged(int index)
    {
        if (LocalizationManager.Instance != null) LocalizationManager.Instance.SetLanguage(index);
        else
        {
            PlayerPrefs.SetInt(LocalizationManager.LanguageKey, index);
            PlayerPrefs.Save();
        }

        if (!string.IsNullOrEmpty(_currentStatusKey)) SetStatus(_currentStatusKey, _currentStatusArgs);
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon Master Serverに接続しました");

#if UNITY_EDITOR
        // UnityEditor実行時はロビーに参加して部屋リストを取得
        PhotonNetwork.JoinLobby();
        SetStatus("title.connecting_lobby");
#else
        SetStatus("title.ready");
        SetInteractable(true);
        
        if (_connectingPanel != null)
        {
            _connectingPanel.SetActive(false);
        }
#endif
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("ロビーに参加しました");
        SetStatus("title.ready");
        SetInteractable(true);

        if (_connectingPanel != null)
        {
            _connectingPanel.SetActive(false);
        }
    }

    public override void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"部屋リスト更新: {roomList.Count}件");

        // 部屋リストを更新（削除された部屋を除外）
        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
            {
                _cachedRoomList.RemoveAll(r => r.Name == room.Name);
                Debug.Log($"部屋削除: {room.Name}");
            }
            else
            {
                var existingRoom = _cachedRoomList.Find(r => r.Name == room.Name);
                if (existingRoom != null)
                {
                    _cachedRoomList.Remove(existingRoom);
                }

                _cachedRoomList.Add(room);
                Debug.Log(
                    $"部屋: {room.Name}, プレイヤー数: {room.PlayerCount}/{room.MaxPlayers}, 参加可能: {room.IsOpen && room.PlayerCount < room.MaxPlayers}");
            }
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Photonから切断されました: {cause}");
        SetStatus("title.connect_error", cause);
        SetInteractable(false);
    }

    private void OnCreateRoomClicked()
    {
        // ユーザーが入力を省略できるよう、ランダムな6桁の数字を自動生成して部屋名とする
        string roomName = Random.Range(100000, 999999).ToString();

        SetStatus("title.create_room", roomName);
        SetInteractable(false);

        // 特定の相手とだけ遊ぶ想定のため、ロビー一覧には表示せずID入力でのみ参加可能にする
        // ただし、UnityEditor実行時はテストのためにIsVisible=trueにして、Join機能のテストができるようにする
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
#if UNITY_EDITOR
            IsVisible = true, // Editor実行時はロビーに表示する
#else
            IsVisible = false, // ビルド版は非表示
#endif
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    private void OnJoinRoomClicked()
    {
#if UNITY_EDITOR
        // UnityEditor実行時は既存の部屋への自動参加を試みる
        SetStatus("title.search_room");
        SetInteractable(false);

        // GetRoomListで取得した部屋リストの先頭に参加する
        if (PhotonNetwork.InLobby && PhotonNetwork.CountOfRooms > 0)
        {
            // ロビー内の部屋情報を取得して参加
            TypedLobby typedLobby = new TypedLobby("", LobbyType.Default);
            PhotonNetwork.JoinRandomRoom(null, 0, MatchmakingMode.FillRoom, typedLobby, null);
        }
        else
        {
            // ロビーに入っていない場合は、ロビーに入ってから部屋を検索
            PhotonNetwork.JoinLobby();
        }
#else
        string roomName = _roomIdInputField.text.Trim();
        
        if (string.IsNullOrEmpty(roomName))
        {
            SetStatus("title.input_room_id");
            return;
        }

        // IDは必ず6桁の数字であるため、事前チェックで無駄な通信を防ぐ
        if (roomName.Length != 6)
        {
            SetStatus("title.room_id_invalid");
            return;
        }

        SetStatus("title.joining_room", roomName);
        SetInteractable(false);

        PhotonNetwork.JoinRoom(roomName);
#endif
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"部屋に参加しました: {PhotonNetwork.CurrentRoom.Name}");
        SetStatus("title.joined_room");

        // 部屋に入れた時点でマッチング待機画面へ遷移する
        PhotonNetwork.LoadLevel("MatchingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋作成失敗: {message}");
        // ランダム生成したIDが偶然重複した場合などが考えられる
        SetStatus("title.create_room_failed");
        SetInteractable(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋参加失敗: {message}");
        SetStatus("title.join_room_failed", message);
        SetInteractable(true);
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
#if UNITY_EDITOR
        // 既存の部屋が見つからない場合は通常のUI表示に戻す
        Debug.Log("既存の部屋が見つかりませんでした。");
        SetStatus("title.no_room_found");
        SetInteractable(true);

        if (_connectingPanel != null)
        {
            _connectingPanel.SetActive(false);
        }
#endif
    }

    private void SetStatus(string key, params object[] args)
    {
        _currentStatusKey = key;
        _currentStatusArgs = args;
        string template = LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
        _statusText.text = args == null || args.Length == 0 ? template : string.Format(template, args);
    }

    private void SetInteractable(bool interactable)
    {
        if (_createRoomButton != null)
            _createRoomButton.interactable = interactable;

        if (_joinRoomButton != null)
            _joinRoomButton.interactable = interactable;

        if (_roomIdInputField != null)
            _roomIdInputField.interactable = interactable;

        if (_settingsButton != null)
            _settingsButton.interactable = interactable;
    }
}
