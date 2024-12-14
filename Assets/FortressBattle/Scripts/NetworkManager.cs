using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;
using Unity.Cinemachine;
using StarterAssets;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public CinemachineVirtualCamera playerFollowCamera;
    public UICanvasControllerInput canvasControllerInput;

    //MonoBehaviourPunCallbacks用以获取服务器的反馈
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();//使用之前的preset
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        //创建房间
        PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() {MaxPlayers = 20 }, default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        GameObject clone = PhotonNetwork.Instantiate("PlayerSamurai", new Vector3(0, 5, 0), Quaternion.identity, 0);
        playerFollowCamera.Follow = clone.GetComponent<PlayerInfo>().cameraTrans;
        canvasControllerInput.starterAssetsInputs = clone.GetComponent<StarterAssetsInputs>();
    }
}
