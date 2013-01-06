using UnityEngine;
using System;
using System.Collections;

public class RoomMenu : MonoBehaviour {

    private Vector2 scrollPos = Vector2.zero;
    private bool creatingRoom = false;
    private string _roomName = "Name here.";
    private string _MaxPlayers ="5";
    private int MaxPlayers;
    private string IngameText = "";

	// Use this for initialization
	void Start () {

	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 20, Screen.width - 20, Screen.height - 40));
        GUILayout.BeginHorizontal();
        GUILayout.Label(PhotonNetwork.playerName,GUILayout.Width(100));
        if (GUILayout.Button("Create",GUILayout.Width(70)))
        {

            if (creatingRoom)
            {
                creatingRoom = false;
            }
            else if (!creatingRoom)
            {
                creatingRoom = true;
            }

        }
       // if (PhotonNetwork.GetRoomList().Length > 0)
       // {
            _roomName = GUILayout.TextField(_roomName, GUILayout.Width(200));
            if (GUILayout.Button("Join", GUILayout.Width(70)))
            {
                PhotonNetwork.JoinRoom(_roomName);
            }
       // }
        GUILayout.EndHorizontal();
        if (!creatingRoom)
        {
            if (PhotonNetwork.GetRoomList().Length == 0)
            {
                GUILayout.Label("No games available.");
            }
            else
            {
                // Room listing: simply call GetRoomList: no need to fetch/poll whatever!
                this.scrollPos = GUILayout.BeginScrollView(this.scrollPos, "box");
                foreach (RoomInfo roomInfo in PhotonNetwork.GetRoomList())
                {
                    GUILayout.BeginHorizontal();

                    if (!roomInfo.open)
                    {
                        IngameText = "(In Game)";
                    }
                    else if (roomInfo.open)
                    {
                        IngameText = "(Waiting)";
                        if (GUILayout.Button("JOIN", GUILayout.Width(70)))
                        {
                            PhotonNetwork.JoinRoom(roomInfo.name);
                        }
                    }

                    GUILayout.Label(roomInfo.name + " " + roomInfo.playerCount + "/" + roomInfo.maxPlayers +IngameText, "box");
                    GUILayout.EndHorizontal();
                }

                GUILayout.EndScrollView();
            }
        }
        else if (creatingRoom)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label("Room Name:", GUILayout.Width(100));
            _roomName = GUILayout.TextField(_roomName,GUILayout.Width(150));
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Max Players:", GUILayout.Width(100));
            _MaxPlayers = GUILayout.TextField(_MaxPlayers, GUILayout.Width(30));
            MaxPlayers = Int32.Parse(_MaxPlayers);
            if (MaxPlayers >= 5)
            {
                MaxPlayers = 5;
            }
            else if (MaxPlayers <= 0)
            {
                MaxPlayers = 2;
            }
            GUILayout.EndHorizontal();

            if(GUILayout.Button("Submit",GUILayout.Width(70)))
            {
                Debug.Log("Trying to create a room.");
                PhotonNetwork.CreateRoom(_roomName,true,true,MaxPlayers);
            }


        }
        GUILayout.EndArea();

    }
    // We have two options here: we either joined(by title, list or random) or created a room.
    private void OnJoinedRoom()
    {
        this.StartCoroutine(this.MoveToGameScene());
    }

    private void OnCreatedRoom()
    {
        Debug.Log("OnCreatedRoom");
        this.StartCoroutine(this.MoveToGameScene());
    }

    private IEnumerator MoveToGameScene()
    {
        while (PhotonNetwork.room == null)
        {
            yield return 0;
        }

        // Temporary disable processing of futher network messages
        PhotonNetwork.isMessageQueueRunning = false;
        Application.LoadLevel("test");
    }
}
