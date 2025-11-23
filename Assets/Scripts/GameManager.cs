using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class GameManager : MonoBehaviourPunCallbacks
{
    [Header("Spawn Settings")]
    [SerializeField] private Transform player1SpawnPoint;
    [SerializeField] private Transform player2SpawnPoint;
    [SerializeField] private string playerPrefabName = "NetworkObject";

    void Start()
    {
        Debug.Log($"ゲーム開始！部屋: {PhotonNetwork.CurrentRoom.Name}, プレイヤー数: {PhotonNetwork.CurrentRoom.PlayerCount}");
        
        // プレイヤーをスポーン
        SpawnPlayer();
    }

    private void SpawnPlayer()
    {
        Vector3 spawnPosition = Vector3.zero;
        
        // プレイヤー番号に応じてスポーン位置を決定
        Player[] players = PhotonNetwork.PlayerList;
        for (int i = 0; i < players.Length; i++)
        {
            if (players[i].ActorNumber == PhotonNetwork.LocalPlayer.ActorNumber)
            {
                if (i == 0 && player1SpawnPoint != null)
                {
                    spawnPosition = player1SpawnPoint.position;
                }
                else if (i == 1 && player2SpawnPoint != null)
                {
                    spawnPosition = player2SpawnPoint.position;
                }
                else
                {
                    spawnPosition = new Vector3(i * 2, 0, 0); // デフォルトの位置
                }
                break;
            }
        }
        
        Debug.Log($"プレイヤーをスポーン: {spawnPosition}");
        PhotonNetwork.Instantiate(playerPrefabName, spawnPosition, Quaternion.identity);
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.LogWarning($"プレイヤーが退出しました: {otherPlayer.NickName}");
        // 必要に応じて、ゲームを中断したりタイトルに戻ったりする処理を追加
    }
}
