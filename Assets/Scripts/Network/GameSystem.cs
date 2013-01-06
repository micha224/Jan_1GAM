using UnityEngine;
using System.Collections;

public class GameSystem : Photon.MonoBehaviour
{
    public GameObject[] SpwanPoint;
    public Transform Player;
    public GameObject[] _Players;

    private int _health;
    private string _healthS = "100";
    private string _livesS = "3";
    private int _lives;
    private string _timeInMinutes = "1";
    private int TimeInMinutes;
    public int CountDownSeconds;
    private float StartTime;
    private float RestSeconds;
    private float TimeLeft;
	[HideInInspector]
    public bool _CountDownTimer = false;
    private int RRestSeconds;
    private float DisplaySeconds;
    private float DisplayMinutes;
    private bool HealthAndLivesSet = false;
	[HideInInspector]
    public bool GameOpen = true;
    private string PlayerPrefab = "";
    private bool Spwaned = false;
	public GUIStyle LocalPlayer;
	public GUIStyle OtherPlayer;
	private GameObject End;
    private PhotonView _End;
    
	// Use this for initialization
	void Awake () {
		SpwanPoint = GameObject.FindGameObjectsWithTag("Spawnpoint");
        if (SpwanPoint[0] == null)
        {
            Debug.LogError("SpwanPoint or Player is not set");
        }

        if (!PhotonNetwork.connected)
        {
            Application.LoadLevel(0);
            return;
        }
        PhotonNetwork.isMessageQueueRunning = true;

 
	}

    void Start()
    {
	 	End = GameObject.Find("EndOfGame");
    	_End = End.GetComponent<PhotonView>();
       
    }
	
	// Update is called once per frame
	void Update () 
    {
        if (_livesS == "" || _livesS == null || _livesS == "0")
        {
            _livesS = "1";
        }
        if (_healthS == "" || _healthS == null || _livesS == "0")
        {
            _healthS = "1";
        }

        if (_timeInMinutes == "" || _timeInMinutes == null || _timeInMinutes == "0")
        {
            _timeInMinutes = "1";
        }
		if(PhotonNetwork.connected)
		{
	        if (GameOpen)
	        {
	            PhotonNetwork.room.open = true;
	        }
	        else if (!GameOpen)
	        {
	            	PhotonNetwork.room.open = false;
	        }
		}
        _Players = GameObject.FindGameObjectsWithTag("Player");
	}

    void OnGUI()
    {
        if (GameOpen && PhotonNetwork.connected == true)
        {
            if (GUI.Button(new Rect(0, 0, 200, 30), "Return to main menu"))
            {
                PhotonNetwork.LeaveRoom();
            }
            if (Spwaned == false)
            {
                GUILayout.BeginArea(new Rect(30, 30, Screen.width / 2 - 180, Screen.height - 200), "", "box");

                GUILayout.Label("Choose your character:");
                if (GUILayout.Button("Gunner"))
                {
                    PlayerPrefab = "Player_Gunner";
                }

                if (GUILayout.Button("Swordsman"))
                {
                    PlayerPrefab = "Player_Swordsman";
                }

                if (GUILayout.Button("Ready!"))
                {
                    photonView.RPC("Spawning", PhotonNetwork.player, PlayerPrefab);
                    Spwaned = true;
                }

                GUILayout.EndArea();
            }

            GUILayout.BeginArea(new Rect(Screen.width / 2 - 150, 30, 300, Screen.height - 200), "", "box");

            GUILayout.BeginHorizontal();
            GUILayout.Label("Room: " + PhotonNetwork.room.name);
            GUILayout.Label("Players: " + PhotonNetwork.room.playerCount + "/" + PhotonNetwork.room.maxPlayers);
            GUILayout.EndHorizontal();

            int i = 0;
            while (i < PhotonNetwork.playerList.Length)
            {
					string LocalText = "";
					if(PhotonNetwork.playerList[i].isLocal)
					{
						LocalText = "(You)";
					}
					else
					{
						LocalText = "";	
					}
                    if (PhotonNetwork.playerList[i].isMasterClient)
                    {
                        GUILayout.Label(LocalText +PhotonNetwork.playerList[i].name + "(Host)", "box", GUILayout.Width(200));
                    }
                    else
                    {
                        GUILayout.Label(LocalText +PhotonNetwork.playerList[i].name, "box", GUILayout.Width(200));
                    }
                i++;
            }

            if (_Players.Length == PhotonNetwork.playerList.Length)
            {
                if (PhotonNetwork.isMasterClient)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Health:", GUILayout.Width(45));
                    _healthS = GUILayout.TextField(_healthS, GUILayout.Width(40));
                    _health = int.Parse(_healthS);
                    GUILayout.Label("Lives:", GUILayout.Width(40));
                    _livesS = GUILayout.TextField(_livesS, GUILayout.Width(20));
                    _lives = int.Parse(_livesS);

                    if (GUILayout.Button("Set", GUILayout.Width(50)))
                    {
                        PhotonView pv;
                        for (int b = 0; b <= _Players.Length - 1; b++)
                        {
                            pv = _Players[b].GetComponent<PhotonView>();
                            pv.RPC("SetHealthAndLives", PhotonTargets.All, _health, _lives);

                        }
                        HealthAndLivesSet = true;
                    }

                    GUILayout.EndHorizontal();

                    GUILayout.BeginHorizontal();
                    GUILayout.Label("Time Limit:", GUILayout.Width(70));
                    _timeInMinutes = GUILayout.TextField(_timeInMinutes, GUILayout.Width(30));
                    GUILayout.Label("in minutes", GUILayout.Width(70));
                    TimeInMinutes = int.Parse(_timeInMinutes);
                    GUILayout.EndHorizontal();

                    GUILayout.Label("You're the host.");

                    if (GUILayout.Button("Start"))
                    {
                        if (HealthAndLivesSet)
                        {
							//if(_Players.Length == 1)
							//{
							//	  _End.RPC("End", PhotonTargets.All, false, true);	
							//}
                            PhotonView pv;
                            for (int c = 0; c < _Players.Length; c++)
                            {
                                pv = _Players[c].GetComponent<PhotonView>();
                                pv.RPC("GetHealthAndLives", PhotonTargets.All);
                            }
                            photonView.RPC("CountdownTimer", PhotonTargets.All, TimeInMinutes);
                            photonView.RPC("StartGame", PhotonTargets.All);
                        }
                        else
                        {
                            Debug.Log("Health and Lives aren't set.");
                        }
                    }
                }
                else if (!PhotonNetwork.isMasterClient)
                {
                    GUILayout.Label("Waiting on host to start the game.");
                }

            }
                GUILayout.EndArea();
        }

        GUILayout.BeginArea(new Rect(0, Screen.height - 100, Screen.width, 100));

        GUILayout.BeginHorizontal();
        GUILayout.Space(30);

        foreach (GameObject _object in _Players)
        {
			if(_object != null)
			{
	            PhotonView pv = _object.GetComponent<PhotonView>();
	            Stats _stats = _object.GetComponent<Stats>();
				if(pv.isMine)
				{
		            GUILayout.BeginVertical(LocalPlayer, GUILayout.Width(90));
		            GUILayout.Label(pv.owner.name);
		            GUILayout.Label("HP:" +_stats.Health);
		            GUILayout.Label("L:" + _stats.Lives);
		            GUILayout.EndVertical();
				}
				else if(!pv.isMine)
				{
					GUILayout.BeginVertical(OtherPlayer, GUILayout.Width(90));
		            GUILayout.Label(pv.owner.name);
		            GUILayout.Label("HP:" +_stats.Health);
		            GUILayout.Label("L:" + _stats.Lives);
		            GUILayout.EndVertical();				
				}
			}
	
            GUILayout.Space(20);
        }

        GUILayout.EndHorizontal();

        GUILayout.EndArea();

        if (_CountDownTimer)
        {
            TimeLeft = (float)PhotonNetwork.time - StartTime; 
            RestSeconds = CountDownSeconds - TimeLeft;
            if (RestSeconds <= 0)
            {
                RestSeconds = 0;
                _End.RPC("End", PhotonTargets.All, true, false);
                _CountDownTimer = false;
            }
            RRestSeconds = Mathf.CeilToInt(RestSeconds);
            DisplaySeconds = RRestSeconds % 60;
            DisplayMinutes = (RRestSeconds / 60) % 60; 
            GUI.Label(new Rect(Screen.width / 2 - 100, 0, 200, 30), DisplayMinutes.ToString() +":" +DisplaySeconds.ToString(), "box");

        }
    }

    public void OnLeftRoom()
    {
        Debug.Log("OnLeftRoom (local)");
        // back to main menu        
        Application.LoadLevel(0);
    }

    public void OnDisconnectedFromPhoton()
    {
        Debug.Log("OnDisconnectedFromPhoton");
        // Back to main menu        
        Application.LoadLevel(0);
    }

    public void OnPhotonPlayerConnected(PhotonPlayer player)
    {
        Debug.Log("OnPhotonPlayerConnected: " + player);
    }

    public void OnPhotonPlayerDisconnected(PhotonPlayer player)
    {
        Debug.Log("OnPlayerDisconneced: " + player);
    }

    public void OnReceivedRoomList()
    {
        Debug.Log("OnReceivedRoomList");
    }

    public void OnReceivedRoomListUpdate()
    {
        Debug.Log("OnReceivedRoomListUpdate");
    }

    public void OnConnectedToPhoton()
    {
        Debug.Log("OnConnectedToPhoton");
    }

    public void OnFailedToConnectToPhoton()
    {
        Debug.Log("OnFailedToConnectToPhoton");
    }

    public void OnPhotonInstantiate(PhotonMessageInfo info)
    {
        Debug.Log("OnPhotonInstantiate " + info.sender);
    }

    [RPC]
    void CountdownTimer(int Time, PhotonMessageInfo info)
    {
        StartTime = (float)PhotonNetwork.time;
        CountDownSeconds = Time * 60;
        _CountDownTimer = true;

    }

    [RPC]
    void Spawning(string name, PhotonMessageInfo info)
    {
                if (Spwaned == false)
                {
                    int i = Random.Range(0,SpwanPoint.Length);
                        PhotonNetwork.Instantiate(name, SpwanPoint[i].transform.position, Quaternion.identity, 0);
                        Spwaned = true;
                }    
    }

    [RPC]
    void StartGame()
    {
        GameOpen = false;
    }
}
