using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviourPunCallbacks
{
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

        PhotonNetwork.Instantiate("Player", new Vector3(0, 0, 0), Quaternion.identity, 0);
    }
}
