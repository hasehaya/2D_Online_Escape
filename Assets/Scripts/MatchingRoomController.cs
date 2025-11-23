using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

public class MatchingRoomController : MonoBehaviourPunCallbacks
{
    [Header("UI References")]
    [SerializeField] private TextMeshProUGUI roomNameText;
    [SerializeField] private TextMeshProUGUI player1NameText;
    [SerializeField] private TextMeshProUGUI player2NameText;
    [SerializeField] private GameObject player1ReadyIcon;
    [SerializeField] private GameObject player2ReadyIcon;
    [SerializeField] private Button readyButton;
    [SerializeField] private TextMeshProUGUI readyButtonText;
    [SerializeField] private Button leaveRoomButton;
    [SerializeField] private TextMeshProUGUI statusText;

    private bool isReady = false;
    private Dictionary<int, bool> playerReadyStatus = new Dictionary<int, bool>();

    private void Start()
    {
        // ボタンのイベント設定
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // 部屋名を表示
        if (roomNameText != null)
        {
            roomNameText.text = $"部屋名: {PhotonNetwork.CurrentRoom.Name}";
        }

        // 初期化
        UpdatePlayerList();
        UpdateReadyStatus();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"プレイヤーが入室しました: {newPlayer.NickName}");
        UpdatePlayerList();
        
        if (PhotonNetwork.CurrentRoom.PlayerCount == 2)
        {
            statusText.text = "全員揃いました！準備ができたらOKボタンを押してください";
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"プレイヤーが退室しました: {otherPlayer.NickName}");
        
        // 退室したプレイヤーの準備状態をクリア
        if (playerReadyStatus.ContainsKey(otherPlayer.ActorNumber))
        {
            playerReadyStatus.Remove(otherPlayer.ActorNumber);
        }
        
        UpdatePlayerList();
        statusText.text = "プレイヤーが退室しました。新しいプレイヤーを待っています...";
        
        // 自分の準備状態もリセット
        if (isReady)
        {
            isReady = false;
            UpdateReadyButton();
        }
    }

    private void OnReadyButtonClicked()
    {
        isReady = !isReady;
        
        // 準備状態を全員に送信
        photonView.RPC("UpdatePlayerReady", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, isReady);
        
        UpdateReadyButton();
        Debug.Log($"準備状態を変更: {isReady}");
    }

    [PunRPC]
    private void UpdatePlayerReady(int actorNumber, bool ready)
    {
        playerReadyStatus[actorNumber] = ready;
        UpdateReadyStatus();
        
        // 全員の準備が完了したかチェック
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        // 2人揃っているかチェック
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            return;
        }

        // 全員が準備完了しているかチェック
        bool allReady = true;
        foreach (Player player in PhotonNetwork.PlayerList)
        {
            if (!playerReadyStatus.ContainsKey(player.ActorNumber) || !playerReadyStatus[player.ActorNumber])
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            statusText.text = "ゲームを開始します...";
            
            // マスタークライアントのみがシーンをロード
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("全員準備完了！ゲームシーンをロードします");
                PhotonNetwork.CurrentRoom.IsOpen = false; // 部屋を閉じる
                PhotonNetwork.LoadLevel("GameScene"); // ゲーム本編シーン
            }
        }
    }

    private void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;
        
        // プレイヤー1
        if (players.Length > 0)
        {
            player1NameText.text = players[0].NickName;
            player1NameText.gameObject.SetActive(true);
        }
        else
        {
            player1NameText.text = "待機中...";
            player1NameText.gameObject.SetActive(true);
        }

        // プレイヤー2
        if (players.Length > 1)
        {
            player2NameText.text = players[1].NickName;
            player2NameText.gameObject.SetActive(true);
        }
        else
        {
            player2NameText.text = "待機中...";
            player2NameText.gameObject.SetActive(true);
        }

        // ステータステキスト更新
        if (players.Length < 2)
        {
            statusText.text = $"プレイヤーを待っています... ({players.Length}/2)";
        }
        else
        {
            statusText.text = "全員揃いました！準備ができたらOKボタンを押してください";
        }
    }

    private void UpdateReadyStatus()
    {
        Player[] players = PhotonNetwork.PlayerList;
        
        // プレイヤー1の準備状態
        if (players.Length > 0 && player1ReadyIcon != null)
        {
            bool ready = playerReadyStatus.ContainsKey(players[0].ActorNumber) && 
                        playerReadyStatus[players[0].ActorNumber];
            player1ReadyIcon.SetActive(ready);
        }

        // プレイヤー2の準備状態
        if (players.Length > 1 && player2ReadyIcon != null)
        {
            bool ready = playerReadyStatus.ContainsKey(players[1].ActorNumber) && 
                        playerReadyStatus[players[1].ActorNumber];
            player2ReadyIcon.SetActive(ready);
        }
    }

    private void UpdateReadyButton()
    {
        if (readyButtonText != null)
        {
            readyButtonText.text = isReady ? "キャンセル" : "OK";
        }
        
        // 2人揃っていない場合は準備ボタンを無効化
        readyButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 2;
    }

    private void OnLeaveRoomClicked()
    {
        statusText.text = "部屋から退出中...";
        PhotonNetwork.LeaveRoom();
    }

    public override void OnLeftRoom()
    {
        Debug.Log("部屋から退出しました");
        // タイトル画面に戻る
        PhotonNetwork.LoadLevel("TitleScene");
    }

    private void Update()
    {
        // 準備ボタンの状態を常に更新
        if (readyButton != null)
        {
            readyButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 2;
        }
    }
}

