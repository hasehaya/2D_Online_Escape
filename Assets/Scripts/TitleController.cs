using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;
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

    private List<RoomInfo> _cachedRoomList = new List<RoomInfo>();

    private void Start()
    {
        _createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        _joinRoomButton.onClick.AddListener(OnJoinRoomClicked);

        if (_settingsButton != null && _settingsController != null)
        {
            _settingsButton.onClick.AddListener(_settingsController.OpenSettings);
        }

        SetInteractable(false);
        _statusText.text = "Photonに接続中...";

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

    public override void OnConnectedToMaster()
    {
        Debug.Log("Photon Master Serverに接続しました");

#if UNITY_EDITOR
        // UnityEditor実行時はロビーに参加して部屋リストを取得
        PhotonNetwork.JoinLobby();
        _statusText.text = "ロビーに接続中...";
#else
        _statusText.text = "部屋を作成するか、IDを入力して参加してください";
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
        _statusText.text = "部屋を作成するか、IDを入力して参加してください";
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

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
#if UNITY_EDITOR
        // 既存の部屋が見つからない場合は通常のUI表示に戻す
        Debug.Log("既存の部屋が見つかりませんでした。");
        _statusText.text = "参加できる部屋が見つかりませんでした。部屋を作成してください。";
        SetInteractable(true);

        if (_connectingPanel != null)
        {
            _connectingPanel.SetActive(false);
        }
#endif
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Photonから切断されました: {cause}");
        _statusText.text = $"接続エラー: {cause}";
        SetInteractable(false);
    }

    private void OnCreateRoomClicked()
    {
        // ユーザーが入力を省略できるよう、ランダムな6桁の数字を自動生成して部屋名とする
        string roomName = Random.Range(100000, 999999).ToString();

        _statusText.text = $"部屋「{roomName}」を作成中...";
        SetInteractable(false);

        // 特定の相手とだけ遊ぶ想定のため、ロビー一覧には表示せずID入力でのみ参加可能にする
        // ただ��、UnityEditor実行時はテストのためにIsVisible=trueにして、Join機能のテストができるようにする
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
        // UnityEditor実行時は既存の部屋に自動参加を試みる
        _statusText.text = "既存の部屋を検索中...";
        SetInteractable(false);

        // GetRoomListで取得した部屋のリストから最初の部屋に参加する
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
            _statusText.text = "部屋IDを入力してください";
            return;
        }

        // IDは必ず6桁の数字であるため、事前チェックで無駄な通信を防ぐ
        if (roomName.Length != 6)
        {
             _statusText.text = "部屋IDは6桁の数字です";
             return;
        }

        _statusText.text = $"部屋「{roomName}」に参加中...";
        SetInteractable(false);

        PhotonNetwork.JoinRoom(roomName);
#endif
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"部屋に参加しました: {PhotonNetwork.CurrentRoom.Name}");
        _statusText.text = "部屋に参加しました！マッチングルームに移動中...";

        // 部屋に入れた時点でマッチング待機画面へ遷移する
        PhotonNetwork.LoadLevel("MatchingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋作成失敗: {message}");
        // ランダム生成したIDが偶然重複した場合などが考えられる
        _statusText.text = "部屋作成に失敗しました。もう一度お試しください。";
        SetInteractable(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋参加失敗: {message}");
        _statusText.text = $"部屋参加失敗: {message}\n（IDが間違っているか、満員です）";
        SetInteractable(true);
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