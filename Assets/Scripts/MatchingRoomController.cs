using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Photon.Pun;
using Photon.Realtime;
using System.Collections.Generic;

/// <summary>
/// マッチング待機室（ロビー）の制御を行うクラス。
/// 参加プレイヤーのリスト表示、準備完了状態の同期、およびゲーム本編へのシーン遷移管理を担当する。
/// </summary>
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
        readyButton.onClick.AddListener(OnReadyButtonClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // プレイヤーが自分の部屋IDを確認できるように表示する
        if (roomNameText != null)
        {
            roomNameText.text = $"部屋ID: {PhotonNetwork.CurrentRoom.Name}";
        }

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
        
        // 退室したプレイヤーの準備完了状態が残っていると、再入室時に不整合が起きるためリセットする
        if (playerReadyStatus.ContainsKey(otherPlayer.ActorNumber))
        {
            playerReadyStatus.Remove(otherPlayer.ActorNumber);
        }
        
        UpdatePlayerList();
        statusText.text = "プレイヤーが退室しました。新しいプレイヤーを待っています...";
        
        // 相手がいなくなったため、自分の準備完了状態も解除して再確認を促す
        if (isReady)
        {
            isReady = false;
            UpdateReadyButton();
        }
    }

    private void OnReadyButtonClicked()
    {
        isReady = !isReady;
        
        // 自分の準備状態を変更し、他のプレイヤーにも同期する
        photonView.RPC("UpdatePlayerReady", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, isReady);
        
        UpdateReadyButton();
        Debug.Log($"準備状態を変更: {isReady}");
    }

    [PunRPC]
    private void UpdatePlayerReady(int actorNumber, bool ready)
    {
        playerReadyStatus[actorNumber] = ready;
        UpdateReadyStatus();
        
        // 全員の準備状況が変わるたびに、ゲーム開始条件を満たしたか確認する
        CheckAllPlayersReady();
    }

    private void CheckAllPlayersReady()
    {
        // 2人揃っていない状態で開始してしまわないようにガード
        if (PhotonNetwork.CurrentRoom.PlayerCount < 2)
        {
            return;
        }

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
            
            // シーン遷移はマスタークライアントが一括で管理・実行する
            if (PhotonNetwork.IsMasterClient)
            {
                Debug.Log("全員準備完了！ゲームシーンをロードします");
                PhotonNetwork.CurrentRoom.IsOpen = false; // 途中参加を防ぐため部屋を閉じる
                PhotonNetwork.LoadLevel("Game");
            }
        }
    }

    private void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;
        
        // プレイヤー1（ホスト）の表示更新
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

        // プレイヤー2（ゲスト）の表示更新
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
        
        if (players.Length > 0 && player1ReadyIcon != null)
        {
            bool ready = playerReadyStatus.ContainsKey(players[0].ActorNumber) && 
                        playerReadyStatus[players[0].ActorNumber];
            player1ReadyIcon.SetActive(ready);
        }

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
        
        // 相手がいない状態で準備完了できてしまうと混乱を招くため無効化する
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
        // ロビーから抜けた場合はタイトル画面に戻す
        PhotonNetwork.LoadLevel("TitleScene");
    }

    private void Update()
    {
        // プレイヤー人数の変動に合わせてボタンの有効状態を常時監視する
        if (readyButton != null)
        {
            readyButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 2;
        }
    }
}

