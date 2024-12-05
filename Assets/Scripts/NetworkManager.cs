using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    //MonoBehaviourPunCallbacks用以获取服务器的反馈
    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();//使用之前的preset
    }

    public override void OnConnectedToMaster()
    {
        base.OnConnectedToMaster();
        Debug.Log("welcome");

        //创建房间
        PhotonNetwork.JoinOrCreateRoom("Room", new Photon.Realtime.RoomOptions() {MaxPlayers = 20 }, default);
    }

    public override void OnJoinedRoom()
    {
        base.OnJoinedRoom();

        //PhotonNetwork.Instantiate("");
    }
}
