using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class TitleController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TMP_InputField roomNameInputField;
    [SerializeField] private Button createRoomButton;
    [SerializeField] private Button joinRoomButton;
    [SerializeField] private TextMeshProUGUI statusText;
    [SerializeField] private GameObject connectingPanel;

    private void Start()
    {
        // ボタンのイベント設定
        createRoomButton.onClick.AddListener(OnCreateRoomClicked);
        joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
        
        // 初期状態
        SetInteractable(false);
        statusText.text = "Photonに接続中...";
        
        // Photonに接続
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
        statusText.text = "部屋名を入力してください";
        SetInteractable(true);
        
        if (connectingPanel != null)
        {
            connectingPanel.SetActive(false);
        }
    }

    public override void OnDisconnected(DisconnectCause cause)
    {
        Debug.LogError($"Photonから切断されました: {cause}");
        statusText.text = $"接続エラー: {cause}";
        SetInteractable(false);
    }

    private void OnCreateRoomClicked()
    {
        string roomName = roomNameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "部屋名を入力してください";
            return;
        }

        statusText.text = $"部屋「{roomName}」を作成中...";
        SetInteractable(false);

        // 2人専用の部屋を作成
        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
            IsOpen = true
        };

        PhotonNetwork.CreateRoom(roomName, roomOptions, TypedLobby.Default);
    }

    private void OnJoinRoomClicked()
    {
        string roomName = roomNameInputField.text.Trim();
        
        if (string.IsNullOrEmpty(roomName))
        {
            statusText.text = "部屋名を入力してください";
            return;
        }

        statusText.text = $"部屋「{roomName}」に参加中...";
        SetInteractable(false);

        PhotonNetwork.JoinRoom(roomName);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"部屋に参加しました: {PhotonNetwork.CurrentRoom.Name}");
        statusText.text = "部屋に参加しました！マッチングルームに移動中...";
        
        // マッチングルームシーンに移動
        PhotonNetwork.LoadLevel("MatchingRoom");
    }

    public override void OnCreateRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋作成失敗: {message}");
        statusText.text = $"部屋作成失敗: {message}\n（部屋名が既に使われている可能性があります）";
        SetInteractable(true);
    }

    public override void OnJoinRoomFailed(short returnCode, string message)
    {
        Debug.LogError($"部屋参加失敗: {message}");
        statusText.text = $"部屋参加失敗: {message}\n（部屋が存在しないか、満員です）";
        SetInteractable(true);
    }

    private void SetInteractable(bool interactable)
    {
        if (createRoomButton != null)
            createRoomButton.interactable = interactable;
        
        if (joinRoomButton != null)
            joinRoomButton.interactable = interactable;
        
        if (roomNameInputField != null)
            roomNameInputField.interactable = interactable;
    }
}

