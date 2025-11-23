using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Photon
{
    public class PunConnector : MonoBehaviourPunCallbacks
    {
        [SerializeField] private string playerNamePrefix = "Player";
        
        void Start()
        {
            // プレイヤー名を設定（ランダムな数字を追加）
            if (string.IsNullOrEmpty(PhotonNetwork.NickName))
            {
                PhotonNetwork.NickName = playerNamePrefix + Random.Range(1000, 9999);
            }
            
            // Photonに接続
            if (!PhotonNetwork.IsConnected)
            {
                Debug.Log("Photonに接続中...");
                PhotonNetwork.ConnectUsingSettings();
            }
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log($"Photon Master Serverに接続しました。プレイヤー名: {PhotonNetwork.NickName}");
        }
        
        public override void OnDisconnected(DisconnectCause cause)
        {
            Debug.LogError($"Photonから切断されました: {cause}");
        }
    }
}
