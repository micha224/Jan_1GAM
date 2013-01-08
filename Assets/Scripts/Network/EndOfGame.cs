using UnityEngine;
using System.Collections;


public class EndOfGame : Photon.MonoBehaviour {

    public int Deaths;
    private int[] Lives;
    private int PlayerWithMostLives;
    private GameObject[] Players;
	private bool WinnerAnnounced = false;
	private bool WinnerAnnouncedGUI = false;
	private string WinnerName;
	public GUIStyle WinnerStyle;
	public GUIStyle WinnerNameStyle;
	private GameSystem _gamesystem;
	
	
	// Use this for initialization
	void Start () {
		_gamesystem = GameObject.Find("_System").GetComponent<GameSystem>();
	
	}
	
	// Update is called once per frame
	void Update () 
	{
		Players = GameObject.FindGameObjectsWithTag("Player");
		if(WinnerAnnounced)
		{
			WinnerAnnouncedGUI = true;
		}
	}
	
	void OnGUI() 
	{
		if(WinnerAnnouncedGUI)
		{
			if(GUI.Button(new Rect(0,0,100,30),"Leave"))
			{
				PhotonNetwork.LeaveRoom();
			}
			GUILayout.BeginArea(new Rect(Screen.width / 2 - 100, Screen.height / 2 - 100, 200, 125), "", "box");
		
			GUILayout.Label("Winner:", WinnerStyle);
			GUILayout.Label(WinnerName, WinnerNameStyle);
			GUILayout.Label("With " +Lives[PlayerWithMostLives] +" Lives",WinnerNameStyle);
			
			GUILayout.EndArea();
		}
		else
		{
			
		}
	}

    void LargestNumber(int[] arr)
    {
        if (arr.Length > 0)
        {
            int small = arr[0];
            int large = arr[0];
            for (int i = 0; i < arr.Length; i++)
            {
                if (large < arr[i])
                {
                    int tmp = large;
                    large = arr[i];
                    arr[i] = large;
                    PlayerWithMostLives = i;
                }
                if (small > arr[i])
                {
                    int tmp = small;
                    small = arr[i];
                    arr[i] = small;
                }
            }
        }
    }

    [RPC]
    void End(bool TimeUp, bool MaxDead, PhotonMessageInfo info)
    {
        Players = GameObject.FindGameObjectsWithTag("Player");
        Lives= new int[Players.Length];
        for (int i = 0; i < Players.Length; i++)
        {
            Stats _stats = Players[i].GetComponent<Stats>();
            Lives[i] = _stats.Lives;
        }
        LargestNumber(Lives);
		_gamesystem._CountDownTimer = false;
        if (TimeUp)
        {
			if(!WinnerAnnounced)
			{
	            PhotonView vp = Players[PlayerWithMostLives].GetComponent<PhotonView>();
	            Debug.Log("Time's up");
	            Debug.Log("Winner is:" + vp.owner.name);
				WinnerAnnounced = true;
				WinnerName = vp.owner.name;
			}
			
        }
        else if (MaxDead)
        {
			if(!WinnerAnnounced)
			{
				PhotonView vp = Players[PlayerWithMostLives].GetComponent<PhotonView>();
	            Debug.Log("4 Deaths");
	            Debug.Log("Winner is:" + vp.owner.name);
				this.WinnerAnnounced = true;
				this.WinnerName = vp.owner.name;
			}
        }
        
    }

    [RPC]
    void Death(PhotonMessageInfo info)
    {
        Debug.Log(info.sender.name + " died.");
        Deaths += 1;

        if (Deaths == Players.Length -1)
        {
            this.photonView.RPC("End", PhotonTargets.All, false, true);
		}
    }
}
