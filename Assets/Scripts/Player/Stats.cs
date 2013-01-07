
using UnityEngine;
using System.Collections;

public class Stats : Photon.MonoBehaviour {

    public int Health;
    public int Lives;
    private GameObject End;
    private PhotonView _End;
	public bool InControl = true;
	private bool death = false;
	private bool DamageDealt = false;
	private int count;
	private Vector3 _position;
	private GameSystem _gamesystem;
	private bool Damaged = false;

	// Use this for initialization
	void Start () {
       		End = GameObject.Find("EndOfGame");
        	_End = End.GetComponent<PhotonView>();
			_gamesystem = GameObject.Find("_System").GetComponent<GameSystem>();
	}
	
	// Update is called once per frame
	void Update () {
		if(InControl)
		{
			//example how to do a input with every client registering the input 
			//from the player that does the input
			if(Input.GetKeyDown("k") && this.Health > 0)
			{
				this.photonView.RPC("Damage", PhotonTargets.All, Health);
			}
			if(Damaged)
			{
				this.photonView.RPC("Damage", PhotonTargets.All, 15);
				Damaged = false;
			}
		}
	
	}

/*
	void OnControllerColliderHit(ControllerColliderHit hit) 
	{
		Collider other = hit.collider;
		if(other.transform.CompareTag("Bullet"))
		{	
			this.photonView.RPC("Damage", PhotonTargets.All, 15);
			Destroy(other.gameObject);
		}
		if(other.transform.CompareTag("Sword"))
		{
			Attack_Sword sword = other.GetComponent<Attack_Sword>();
			if(sword.Attacking)
			{
				this.photonView.RPC("Damage", PhotonTargets.All, 15);
			}
			else
			{
			}
		}
	}
	*/
	void OnCollisionEnter(Collision other)
	{
		if(other.transform.CompareTag("Bullet"))
		{	
			Destroy(other.gameObject);
			Damaged = true;
		}
		if(other.transform.CompareTag("Sword"))
		{
			Attack_Sword sword = other.collider.GetComponent<Attack_Sword>();
			if(sword.Attacking)
			{
				this.photonView.RPC("Damage", PhotonTargets.All, 15);
			}
			else
			{
			}
		}
	}
	
    [RPC]
    public void SetHealthAndLives(int health, int lives, PhotonMessageInfo info)
    {
        Debug.Log("Health and Lives are set." +info.photonView.viewID);
        if (info.photonView.isMine)
        {
            Health = health;
            Lives = lives;
        }
        else if (!info.photonView.isMine)
        {
            Health = health;
            Lives = lives;
        }
        PlayerPrefs.SetInt("Health", Health);
        PlayerPrefs.SetInt("Lives", Lives);
    }

    [RPC]
    public void GetHealthAndLives()
    {
        Debug.Log("Health and Lives are retrieved.");
        Health = PlayerPrefs.GetInt("Health");
        Lives = PlayerPrefs.GetInt("Lives");
    }
	
	[RPC]
    void Damage(int dmg, PhotonMessageInfo info)
    {
			Debug.Log("Damage recieved.");
	        this.Health -= dmg;
	
	        if (this.Health <= 0)
	        {
	            this.Lives -= 1;
	            this.Health = PlayerPrefs.GetInt("Health");
				this.count = Random.Range(0,_gamesystem.SpwanPoint.Length);
				this._position = _gamesystem.SpwanPoint[count].transform.position;
				info.photonView.transform.position = _position;
	        }
	        if (this.Lives <= 0)
	        {
				if(!this.death)
				{
		            this.Lives = 0;
					this.photonView.RPC("PlayDeath", PhotonTargets.All);
		            this._End.RPC("Death", PhotonNetwork.player);
					this.death = true;
				}
	        }
			DamageDealt = true;
    }
	
	[RPC]
	public void Hit(int dmg1,bool dmg)
	{
		DamageDealt = dmg;
		Debug.Log(dmg1);
		if(DamageDealt == false)
		{
			this.photonView.RPC("Damage",PhotonTargets.All,dmg1);
		}
	}
}
