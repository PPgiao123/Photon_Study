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

    //MonoBehaviourPunCallbacks���Ի�ȡ�������ķ���
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();//ʹ��֮ǰ��preset
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();

        //��������
        PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() {MaxPlayers = 20 }, default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        GameObject clone = PhotonNetwork.Instantiate("Player", new Vector3(0, 0, 0), Quaternion.identity, 0);
        playerFollowCamera.Follow = clone.GetComponent<Player>().cameraTrans;
        canvasControllerInput.starterAssetsInputs = clone.GetComponent<StarterAssetsInputs>();
    }
}
