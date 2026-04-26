using System.Collections.Generic;
using ExitGames.Client.Photon;
using Photon.Pun;
using Photon.Realtime;
using Save;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// マッチング待機室（ロビー）の制御を行うクラス。
/// 参加プレイヤーのリスト表示、準備完了状態の同期、およびゲーム本編へのシーン遷移管理を担当する。
/// </summary>
public class MatchingRoomController : MonoBehaviourPunCallbacks
{
    [Header("UI References")] [SerializeField]
    private TextMeshProUGUI _roomNameText;

    [SerializeField] private TextMeshProUGUI _player1NameText;
    [SerializeField] private TextMeshProUGUI _player2NameText;
    [SerializeField] private GameObject _player1ReadyIcon;
    [SerializeField] private GameObject _player2ReadyIcon;
    [SerializeField] private Button _readyButton;
    [SerializeField] private TextMeshProUGUI _readyButtonText;
    [SerializeField] private Button _leaveRoomButton;
    [SerializeField] private TextMeshProUGUI _statusText;

    private bool _isReady = false;
    private Dictionary<int, bool> _playerReadyStatus = new Dictionary<int, bool>();
    private string _localPlayerId;

    private void Start()
    {
        _localPlayerId = LocalIdentityProvider.GetOrCreateLocalPlayerId();
        PhotonNetwork.LocalPlayer.SetCustomProperties(new Hashtable
        {
            { SaveSessionContext.PlayerIdPropertyKey, _localPlayerId }
        });

        _readyButton.onClick.AddListener(OnReadyButtonClicked);
        _leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // プレイヤーが自分の部屋IDを確認できるように表示する
        if (_roomNameText != null)
        {
            _roomNameText.text = $"部屋ID: {PhotonNetwork.CurrentRoom.Name}";
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
            SetStatus("matching.all_joined");
        }
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"プレイヤーが退室しました: {otherPlayer.NickName}");

        // 退室したプレイヤーの準備完了状態が残っていると、再入室時に不整合が起きるためリセットする
        if (_playerReadyStatus.ContainsKey(otherPlayer.ActorNumber))
        {
            _playerReadyStatus.Remove(otherPlayer.ActorNumber);
        }

        UpdatePlayerList();
        SetStatus("matching.player_left");

        // 相手がいなくなったため、自分の準備完了状態も解除して再確認を促す
        if (_isReady)
        {
            _isReady = false;
            UpdateReadyButton();
        }
    }

    private void OnReadyButtonClicked()
    {
        _isReady = !_isReady;

        // 自分の準備状態を変更し、他のプレイヤーにも同期する
        photonView.RPC("UpdatePlayerReady", RpcTarget.AllBuffered, PhotonNetwork.LocalPlayer.ActorNumber, _isReady);

        UpdateReadyButton();
        Debug.Log($"準備状態を変更: {_isReady}");
    }

    [PunRPC]
    private void UpdatePlayerReady(int actorNumber, bool ready)
    {
        _playerReadyStatus[actorNumber] = ready;
        UpdateReadyStatus();

        // 全員の準備状況が変わるたびに、ゲーム開始条件を満たしたか確認する
        CheckAllPlayersReady();
    }

    [PunRPC]
    private void LoadGameScene(string eliasPlayerId, string noelPlayerId)
    {
        string localId = LocalIdentityProvider.GetOrCreateLocalPlayerId();

        if (localId == eliasPlayerId)
        {
            Debug.Log("役割:Eliasとして Game_Elias シーンをロードします");
            PhotonNetwork.LoadLevel("Game_Elias");
            return;
        }

        if (localId == noelPlayerId)
        {
            Debug.Log("役割:Noelとして Game_Noel シーンをロードします");
            PhotonNetwork.LoadLevel("Game_Noel");
            return;
        }

        // 役割情報が取れないケースでは従来のフォールバックを使う
        PhotonNetwork.LoadLevel(PhotonNetwork.IsMasterClient ? "Game_Elias" : "Game_Noel");
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
            if (!_playerReadyStatus.ContainsKey(player.ActorNumber) || !_playerReadyStatus[player.ActorNumber])
            {
                allReady = false;
                break;
            }
        }

        if (allReady)
        {
            SetStatus("matching.starting");

            if (PhotonNetwork.IsMasterClient)
            {
                PhotonNetwork.CurrentRoom.IsOpen = false;

                string eliasPlayerId;
                string noelPlayerId;
                ResolveFixedRoles(out eliasPlayerId, out noelPlayerId);

                Hashtable roomProps = new Hashtable
                {
                    {
                        SaveSessionContext.PairKeyRoomPropertyKey,
                        SaveSessionContext.BuildPairKey(eliasPlayerId, noelPlayerId)
                    },
                    { SaveSessionContext.EliasPlayerIdRoomPropertyKey, eliasPlayerId },
                    { SaveSessionContext.NoelPlayerIdRoomPropertyKey, noelPlayerId }
                };
                PhotonNetwork.CurrentRoom.SetCustomProperties(roomProps);

                photonView.RPC("LoadGameScene", RpcTarget.All, eliasPlayerId, noelPlayerId);
                return;
            }
        }
    }

    private void ResolveFixedRoles(out string eliasPlayerId, out string noelPlayerId)
    {
        Player[] players = PhotonNetwork.PlayerList;
        string playerA = GetPlayerId(players[0]);
        string playerB = GetPlayerId(players[1]);
        string pairKey = SaveSessionContext.BuildPairKey(playerA, playerB);

        PairSaveData existing;
        if (PairSaveRepository.TryLoad(pairKey, out existing))
        {
            if (!string.IsNullOrEmpty(existing.eliasPlayerId) && !string.IsNullOrEmpty(existing.noelPlayerId))
            {
                eliasPlayerId = existing.eliasPlayerId;
                noelPlayerId = existing.noelPlayerId;
                return;
            }
        }

        if (string.CompareOrdinal(playerA, playerB) <= 0)
        {
            eliasPlayerId = playerA;
            noelPlayerId = playerB;
            return;
        }

        eliasPlayerId = playerB;
        noelPlayerId = playerA;
    }

    private string GetPlayerId(Player player)
    {
        object value;
        if (player.CustomProperties.TryGetValue(SaveSessionContext.PlayerIdPropertyKey, out value))
        {
            return value as string ?? string.Empty;
        }

        return string.Empty;
    }

    private void UpdatePlayerList()
    {
        Player[] players = PhotonNetwork.PlayerList;

        // プレイヤー1（ホスト）の表示更新
        if (players.Length > 0)
        {
            _player1NameText.text = players[0].NickName;
            _player1NameText.gameObject.SetActive(true);
        }
        else
        {
            _player1NameText.text = Localize("matching.waiting");
            _player1NameText.gameObject.SetActive(true);
        }

        // プレイヤー2（ゲスト）の表示更新
        if (players.Length > 1)
        {
            _player2NameText.text = players[1].NickName;
            _player2NameText.gameObject.SetActive(true);
        }
        else
        {
            _player2NameText.text = Localize("matching.waiting");
            _player2NameText.gameObject.SetActive(true);
        }

        if (players.Length < 2)
        {
            SetStatus("matching.waiting_players", players.Length);
        }
        else
        {
            SetStatus("matching.all_joined");
        }
    }

    private void UpdateReadyStatus()
    {
        Player[] players = PhotonNetwork.PlayerList;

        if (players.Length > 0 && _player1ReadyIcon != null)
        {
            bool ready = _playerReadyStatus.ContainsKey(players[0].ActorNumber) &&
                         _playerReadyStatus[players[0].ActorNumber];
            _player1ReadyIcon.SetActive(ready);
        }

        if (players.Length > 1 && _player2ReadyIcon != null)
        {
            bool ready = _playerReadyStatus.ContainsKey(players[1].ActorNumber) &&
                         _playerReadyStatus[players[1].ActorNumber];
            _player2ReadyIcon.SetActive(ready);
        }
    }

    private void UpdateReadyButton()
    {
        if (_readyButtonText != null)
        {
            _readyButtonText.text = _isReady ? Localize("matching.ready_cancel") : Localize("matching.ready_ok");
        }

        // 相手がいない状態で準備完了できてしまうと混乱を招くため無効化する
        _readyButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 2;
    }

    private void OnLeaveRoomClicked()
    {
        SetStatus("matching.leave_room");
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
        if (_readyButton != null)
        {
            _readyButton.interactable = PhotonNetwork.CurrentRoom.PlayerCount == 2;
        }
    }

    private void SetStatus(string key, params object[] args)
    {
        string template = Localize(key);
        _statusText.text = args == null || args.Length == 0 ? template : string.Format(template, args);
    }

    private string Localize(string key)
    {
        return LocalizationManager.Instance != null ? LocalizationManager.Instance.Get(key) : key;
    }
}