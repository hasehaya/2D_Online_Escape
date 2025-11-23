using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

namespace Photon
{
    public class PunConnector : MonoBehaviourPunCallbacks
    {
        void Start()
        {
            PhotonNetwork.ConnectUsingSettings();
        }

        // Update is called once per frame
        void Update()
        {
            
        }

        public override void OnConnectedToMaster()
        {
            Debug.Log("Connected to Photon Master Server");
            PhotonNetwork.JoinOrCreateRoom("TestRoom", new RoomOptions { MaxPlayers = 2 }, TypedLobby.Default);
        }
    
        public override void OnJoinedRoom()
        {
            Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);
            PhotonNetwork.Instantiate("NetworkObject", Vector3.zero, Quaternion.identity);
        }
    }
}
