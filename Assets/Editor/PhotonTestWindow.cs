using System.Collections.Generic;
using System.IO;
using System.Linq;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// ParallelSync使用時のPhotonマッチングテスト用EditorWindow
/// オリジナル: CreateRoomを実行してGame_Aに遷移
/// 複製(Clone): JoinRoomを2秒��きに試行してGame_Bに遷移
/// </summary>
public class PhotonTestWindow : EditorWindow, ILobbyCallbacks, IConnectionCallbacks, IMatchmakingCallbacks,
    IInRoomCallbacks
{
    private static PhotonTestWindow _window;
    private bool _isClone;
    private string _roomName;
    private bool _isConnecting;
    private bool _isJoining;
    private double _lastJoinAttemptTime;
    private const float JOIN_RETRY_INTERVAL = 2f;
    private List<RoomInfo> _availableRooms = new List<RoomInfo>();
    private bool _waitingForPlayMode = false;
    private bool _isCreateRoomMode = false;
    private bool _pendingCreateRoom = false;

    [MenuItem("Tools/Photon Test Window")]
    public static void ShowWindow()
    {
        _window = GetWindow<PhotonTestWindow>("Photon Test");
        _window.Show();
    }

    private void OnEnable()
    {
        // ParallelSyncのClone判定（プロジェクトパスに"_clone_"が含まれているか確認）
        _isClone = Application.dataPath.Contains("_clone_");

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        // Photonコールバックを登録
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.AddCallbackTarget(this);
        }
    }

    private void OnDisable()
    {
        EditorApplication.update -= OnEditorUpdate;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;

        // Photonコールバックを解除
        if (PhotonNetwork.NetworkingClient != null)
        {
            PhotonNetwork.NetworkingClient.RemoveCallbackTarget(this);
        }
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(10);

        // 現在の状態を表示
        EditorGUILayout.LabelField("環境", _isClone ? "Clone (JoinRoom)" : "Original (CreateRoom)",
            EditorStyles.boldLabel);
        EditorGUILayout.Space(5);

        // Photon接続状態
        EditorGUILayout.LabelField("Photon状態", PhotonNetwork.NetworkClientState.ToString());

        if (!string.IsNullOrEmpty(_roomName))
        {
            EditorGUILayout.LabelField("部屋名", _roomName);
        }

        if (PhotonNetwork.InRoom)
        {
            EditorGUILayout.LabelField("現在の部屋", PhotonNetwork.CurrentRoom.Name);
            EditorGUILayout.LabelField("プレイヤー数", $"{PhotonNetwork.CurrentRoom.PlayerCount}/2");
        }

        // 部屋リスト表示（Clone側のみ）
        if (_isClone && PhotonNetwork.InLobby && _availableRooms.Count > 0)
        {
            EditorGUILayout.Space(5);
            EditorGUILayout.LabelField("利用可能な部屋:", EditorStyles.boldLabel);
            foreach (var room in _availableRooms.Where(r => !r.RemovedFromList))
            {
                EditorGUILayout.LabelField($"  {room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
            }
        }

        EditorGUILayout.Space(10);

        // 再生中またはマッチング処理中はボタンを無効化
        GUI.enabled = !EditorApplication.isPlaying && !_isConnecting && !_isJoining && !_waitingForPlayMode;

        if (_isClone)
        {
            // Clone: JoinRoomボタン
            if (GUILayout.Button("JoinRoom (自動リトライ)", GUILayout.Height(40)))
            {
                StartJoinRoom();
            }
        }
        else
        {
            // Original: CreateRoomボタン
            if (GUILayout.Button("CreateRoom", GUILayout.Height(40)))
            {
                StartCreateRoom();
            }
        }

        GUI.enabled = true;

        EditorGUILayout.Space(10);

        // リセットボタン
        if (GUILayout.Button("接続をリセット"))
        {
            ResetConnection();
        }

        if (_isJoining)
        {
            EditorGUILayout.HelpBox("JoinRoom試行中... 2秒おきにリトライします", MessageType.Info);
        }

        if (_isConnecting)
        {
            EditorGUILayout.HelpBox("Photonに接続中...", MessageType.Info);
        }
    }

    private void OnEditorUpdate()
    {
        // 自動Join処理（2秒おきにリトライ）
        if (_isJoining)
        {
            if (EditorApplication.timeSinceStartup - _lastJoinAttemptTime >= JOIN_RETRY_INTERVAL)
            {
                _lastJoinAttemptTime = EditorApplication.timeSinceStartup;

                if (PhotonNetwork.IsConnectedAndReady && !PhotonNetwork.InRoom && PhotonNetwork.InLobby)
                {
                    // 部屋リストから参加可能な部屋を検索
                    var availableRoom = _availableRooms.FirstOrDefault(r =>
                        r.IsOpen &&
                        r.PlayerCount < r.MaxPlayers &&
                        !r.RemovedFromList);

                    if (availableRoom != null)
                    {
                        Debug.Log($"[PhotonTestWindow] JoinRoom試行: {availableRoom.Name}");
                        _roomName = availableRoom.Name;
                        PhotonNetwork.JoinRoom(availableRoom.Name);
                    }
                    else
                    {
                        Debug.Log("[PhotonTestWindow] 参加可能な部屋が見つかりません。リトライします...");
                    }
                }
            }
        }

        Repaint();
    }

    private void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode && _waitingForPlayMode)
        {
            _waitingForPlayMode = false;
            _isConnecting = true;

            Debug.Log("[PhotonTestWindow] 再生モードに入りました。マッチング処理を開始します");

            // 再生モード開始時にコールバックを再登録
            EditorApplication.delayCall += () =>
            {
                if (PhotonNetwork.NetworkingClient != null)
                {
                    PhotonNetwork.NetworkingClient.AddCallbackTarget(this);
                    Debug.Log("[PhotonTestWindow] Photonコールバックを登録しました");
                }

                // 少し待ってから接続状態をチェック
                EditorApplication.delayCall += () => { CheckConnectionAndProceed(); };
            };
        }
        else if (state == PlayModeStateChange.ExitingPlayMode)
        {
            // 再生終了時に状態をリセット
            Debug.Log("[PhotonTestWindow] 再生モード終了。状態をリセットします");
            ResetConnection();
        }
    }

    private void CheckConnectionAndProceed()
    {
        Debug.Log($"[PhotonTestWindow] 接続状態チェック: {PhotonNetwork.NetworkClientState}");
        Debug.Log(
            $"[PhotonTestWindow] IsConnected: {PhotonNetwork.IsConnected}, IsConnectedAndReady: {PhotonNetwork.IsConnectedAndReady}");
        Debug.Log($"[PhotonTestWindow] InLobby: {PhotonNetwork.InLobby}, InRoom: {PhotonNetwork.InRoom}");

        if (!PhotonNetwork.IsConnected)
        {
            Debug.Log("[PhotonTestWindow] Photonに接続中...");
            PhotonNetwork.ConnectUsingSettings();
        }
        else if (PhotonNetwork.IsConnectedAndReady)
        {
            Debug.Log("[PhotonTestWindow] 既に接続済みです");

            if (_isCreateRoomMode)
            {
                Debug.Log("[PhotonTestWindow] CreateRoomモード: 部屋を作成します");
                TryCreateRoomIfReady();
            }
            else if (_isJoining)
            {
                if (!PhotonNetwork.InLobby)
                {
                    Debug.Log("[PhotonTestWindow] JoinRoomモード: ロビーに参加します");
                    PhotonNetwork.JoinLobby();
                }
                else
                {
                    Debug.Log("[PhotonTestWindow] JoinRoomモード: 既にロビーに参加済みです");
                }
            }
        }
        else
        {
            // 接続中の場合は少し待ってから再チェック
            Debug.Log("[PhotonTestWindow] 接続処理中... 再チェックします");
            EditorApplication.delayCall += () => { CheckConnectionAndProceed(); };
        }
    }

    private void StartCreateRoom()
    {
        _isCreateRoomMode = true;
        _waitingForPlayMode = true;
        _roomName = Random.Range(100000, 999999).ToString();

        Debug.Log($"[PhotonTestWindow] CreateRoom開始: {_roomName}");

        // TitleSceneに移動
        LoadTitleScene();

        // シーンロード後に再生モードに入る
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[PhotonTestWindow] 再生モードを開始します");
                EditorApplication.isPlaying = true;
            }
        };
    }

    private void StartJoinRoom()
    {
        _isCreateRoomMode = false;
        _waitingForPlayMode = true;
        _isJoining = true;
        _lastJoinAttemptTime = 0;

        Debug.Log("[PhotonTestWindow] JoinRoom開始 (自動リトライ)");

        // TitleSceneに移動
        LoadTitleScene();

        // シーンロード後に再生モードに入る
        EditorApplication.delayCall += () =>
        {
            if (!EditorApplication.isPlaying)
            {
                Debug.Log("[PhotonTestWindow] 再生モードを開始します");
                EditorApplication.isPlaying = true;
            }
        };
    }

    private void LoadGameScene()
    {
        string sceneName = _isClone ? "Game_B" : "Game_A";
        Debug.Log($"[PhotonTestWindow] マッチング完了！{sceneName}に遷移します");

        _isJoining = false;

        // 部屋を閉じる
        if (PhotonNetwork.IsMasterClient && PhotonNetwork.CurrentRoom != null)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }

        // 再生モード中はPhotonNetwork.LoadLevelを使用
        if (EditorApplication.isPlaying)
        {
            PhotonNetwork.LoadLevel(sceneName);
        }
        else
        {
            // Editorモード中はEditorSceneManagerを使用
            EditorApplication.delayCall += () =>
            {
                string scenePath = $"Assets/Scenes/{sceneName}.unity";
                if (File.Exists(scenePath))
                {
                    EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
                }
                else
                {
                    Debug.LogError($"[PhotonTestWindow] シーンが見つかりません: {scenePath}");
                }
            };
        }
    }

    private void LoadTitleScene()
    {
        string scenePath = "Assets/Scenes/TitleScene.unity";
        if (File.Exists(scenePath))
        {
            EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);
            Debug.Log("[PhotonTestWindow] TitleSceneをロードしました");
        }
        else
        {
            Debug.LogError($"[PhotonTestWindow] TitleSceneが見つかりません: {scenePath}");
        }
    }

    private void ResetConnection()
    {
        _isConnecting = false;
        _isJoining = false;
        _isCreateRoomMode = false;
        _waitingForPlayMode = false;
        _pendingCreateRoom = false;
        _roomName = null;
        _availableRooms.Clear();

        if (PhotonNetwork.IsConnected)
        {
            PhotonNetwork.Disconnect();
        }

        Debug.Log("[PhotonTestWindow] 接続をリセットしました");
    }

    // ILobbyCallbacks実装
    public void OnJoinedLobby()
    {
        Debug.Log("[PhotonTestWindow] ロビーに参加しました");
        if (_isCreateRoomMode && _pendingCreateRoom)
        {
            TryCreateRoomIfReady();
        }
    }

    public void OnLeftLobby()
    {
        Debug.Log("[PhotonTestWindow] ロビーから退出しました");
        _availableRooms.Clear();
    }

    public void OnRoomListUpdate(List<RoomInfo> roomList)
    {
        Debug.Log($"[PhotonTestWindow] 部屋リスト更新: {roomList.Count}件");

        foreach (var room in roomList)
        {
            if (room.RemovedFromList)
            {
                _availableRooms.RemoveAll(r => r.Name == room.Name);
                Debug.Log($"[PhotonTestWindow] 部屋削除: {room.Name}");
            }
            else
            {
                var existingRoom = _availableRooms.Find(r => r.Name == room.Name);
                if (existingRoom != null)
                {
                    _availableRooms.Remove(existingRoom);
                }

                _availableRooms.Add(room);
                Debug.Log($"[PhotonTestWindow] 部屋追加/更新: {room.Name} ({room.PlayerCount}/{room.MaxPlayers})");
            }
        }
    }

    public void OnLobbyStatisticsUpdate(List<TypedLobbyInfo> lobbyStatistics)
    {
        // 不要
    }

    // IConnectionCallbacks実装
    public void OnConnected()
    {
        Debug.Log("[PhotonTestWindow] Photonサーバーに接続しました");
    }

    public void OnConnectedToMaster()
    {
        Debug.Log("[PhotonTestWindow] マスターサーバーに接続しました");

        _isConnecting = false;

        if (_isCreateRoomMode)
        {
            // 部屋を作成
            Debug.Log("[PhotonTestWindow] 部屋作成処理を開始します");
            TryCreateRoomIfReady();
        }
        else if (_isJoining)
        {
            // ロビーに参加
            if (!PhotonNetwork.InLobby)
            {
                Debug.Log("[PhotonTestWindow] ロビーに参加します");
                PhotonNetwork.JoinLobby();
            }
            else
            {
                Debug.Log("[PhotonTestWindow] 既にロビーに参加しています");
            }
        }
    }

    public void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"[PhotonTestWindow] 切断されました: {cause}");
        _isConnecting = false;
        _isJoining = false;
    }

    public void OnRegionListReceived(RegionHandler regionHandler)
    {
        // 不要
    }

    public void OnCustomAuthenticationResponse(Dictionary<string, object> data)
    {
        // 不要
    }

    public void OnCustomAuthenticationFailed(string debugMessage)
    {
        // 不要
    }

    // IMatchmakingCallbacks実装
    public void OnFriendListUpdate(List<FriendInfo> friendList)
    {
        // 不要
    }

    public void OnCreatedRoom()
    {
        Debug.Log($"[PhotonTestWindow] 部屋を作成しました: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log(
            $"[PhotonTestWindow] 部屋のプロパティ - IsOpen: {PhotonNetwork.CurrentRoom.IsOpen}, IsVisible: {PhotonNetwork.CurrentRoom.IsVisible}, MaxPlayers: {PhotonNetwork.CurrentRoom.MaxPlayers}");
    }

    public void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"[PhotonTestWindow] 部屋作成失敗: {message}");
        _isConnecting = false;
        _isCreateRoomMode = false;
    }

    public void OnJoinedRoom()
    {
        Debug.Log($"[PhotonTestWindow] 部屋に参加しました: {PhotonNetwork.CurrentRoom.Name}");
        Debug.Log($"[PhotonTestWindow] 現在のプレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}/2");

        // 2人揃ったらゲームシーンに遷移
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            LoadGameScene();
        }
    }

    public void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonTestWindow] 部屋参加失敗: {message} (リトライします)");
    }

    public void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.LogWarning($"[PhotonTestWindow] ランダム参加失敗: {message}");
    }

    public void OnLeftRoom()
    {
        Debug.Log("[PhotonTestWindow] 部屋から退出しました");
    }

    private void CreateRoom()
    {
        if (!PhotonNetwork.IsConnectedAndReady)
        {
            Debug.LogWarning($"[PhotonTestWindow] 部屋作成をスキップ: 接続状態が不正です (State: {PhotonNetwork.NetworkClientState})");
            _isConnecting = true;
            return;
        }

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        Debug.Log($"[PhotonTestWindow] 部屋作成リクエスト送信: {_roomName}");
        Debug.Log($"[PhotonTestWindow] 接続状態: {PhotonNetwork.NetworkClientState}");

        PhotonNetwork.CreateRoom(_roomName, roomOptions, TypedLobby.Default);

        _isConnecting = false;
    }

    private void TryCreateRoomIfReady()
    {
        if (!_isCreateRoomMode || string.IsNullOrEmpty(_roomName))
        {
            return;
        }

        if (!PhotonNetwork.IsConnected)
        {
            _pendingCreateRoom = true;
            Debug.Log("[PhotonTestWindow] 未接続のため部屋作成を保留します");
            return;
        }

        if (PhotonNetwork.NetworkClientState == ClientState.JoiningLobby)
        {
            _pendingCreateRoom = true;
            Debug.Log("[PhotonTestWindow] ロビー参加中のため部屋作成を保留します");
            return;
        }

        if (!PhotonNetwork.IsConnectedAndReady)
        {
            _pendingCreateRoom = true;
            Debug.Log($"[PhotonTestWindow] 接続準備中のため部屋作成を保留します (State: {PhotonNetwork.NetworkClientState})");
            return;
        }

        _pendingCreateRoom = false;
        CreateRoom();
    }

    // IInRoomCallbacks実装
    public void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[PhotonTestWindow] プレイヤーが入室しました: {newPlayer.NickName}");
        Debug.Log($"[PhotonTestWindow] 現在のプレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}/2");

        // 2人揃ったらゲームシーンに遷移
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            LoadGameScene();
        }
    }

    public void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[PhotonTestWindow] プレイヤーが退室しました: {otherPlayer.NickName}");
    }

    public void OnRoomPropertiesUpdate(Hashtable propertiesThatChanged)
    {
        // 不要
    }

    public void OnPlayerPropertiesUpdate(Player targetPlayer, Hashtable changedProps)
    {
        // 不要
    }

    public void OnMasterClientSwitched(Player newMasterClient)
    {
        Debug.Log($"[PhotonTestWindow] マスタークライアント変更: {newMasterClient.NickName}");
    }
}