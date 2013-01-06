using UnityEngine;
using System.Collections;

public class Main_Menu : MonoBehaviour {

	// Use this for initialization
	void Start () {
        //connecting to the server
        if (!PhotonNetwork.connected)
        {
            PhotonNetwork.ConnectUsingSettings("1.0");
            Debug.Log("connected");
        }

        //checking if the name is empty
        if (string.IsNullOrEmpty(PhotonNetwork.playerName))
        {
            PhotonNetwork.playerName = "Random" + Random.Range(0, 921);
        }
	}
	
	// Update is called once per frame
	void Update () {
	
	}

    void OnGUI()
    {
        //setting up and saving the name
        GUI.Label(new Rect(0, 0, 100, 30), "Name:");
        PhotonNetwork.playerName = GUI.TextField(new Rect(50, 0, 100, 25), PhotonNetwork.playerName);
		GUI.Label(new Rect(0,40,200,30),"Ping:" +PhotonNetwork.GetPing().ToString());
		
        if(GUI.Button(new Rect(155,0,55,25), "submit"))
        {
            PlayerPrefs.SetString("playerName", PhotonNetwork.playerName);
            Debug.Log(PhotonNetwork.playerName);
        }
		if(PhotonNetwork.connected)
		{
	        if(GUI.Button(new Rect(Screen.width-200,Screen.height/2-30,200,30),"Start Game", "Label"))
	        {
	            Application.LoadLevel("MatchMaking");
	        }
		}
		else
		{
			GUI.Label(new Rect(Screen.width-200,Screen.height/2-30,200,30),"Connecting...");
		}

        if (GUI.Button(new Rect(Screen.width - 200, Screen.height / 2 + 30, 200, 30), "Exit", "Label"))
        {
            Application.Quit();
        }
    }
}
