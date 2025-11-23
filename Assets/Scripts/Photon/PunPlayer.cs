using UnityEngine;
using UnityEngine.InputSystem;
using Photon.Pun;
using Photon.Realtime;

public class PunPlayer : MonoBehaviourPunCallbacks, IPunObservable
{
    void Update()
    {
        if (!photonView.IsMine)
        {
            return;
        }
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector3 oldPosition = transform.position;
            
            // カメラからの距離をカメラのz位置から計算
            float distanceFromCamera = Mathf.Abs(Camera.main.transform.position.z);
            Vector3 mouseWorldPosition = new Vector3(mousePosition.x, mousePosition.y, distanceFromCamera);
            Vector3 worldPosition = Camera.main.ScreenToWorldPoint(mouseWorldPosition);
            worldPosition.z = 0;
            
            transform.position = worldPosition;
            
            Debug.Log($"Camera Z: {Camera.main.transform.position.z}");
            Debug.Log($"Screen: {mousePosition} -> World: {worldPosition}");
            Debug.Log($"Moved from {oldPosition} to {transform.position}");
        }
    }
    
    void IPunObservable.OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            // 自分のデータを送信
            stream.SendNext(transform.position);
        }
        if (stream.IsReading)
        {
            // 他人のデータを受信
            transform.position = (Vector3)stream.ReceiveNext();
        }
    }
}
