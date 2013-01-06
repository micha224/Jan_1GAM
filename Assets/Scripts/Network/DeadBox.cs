using UnityEngine;
using System.Collections;

public class DeadBox : Photon.MonoBehaviour {

	GameSystem _gamesystem;
	Vector3 _position;
	int count;
	// Use this for initialization
	void Start () {
	 _gamesystem = GameObject.Find("_System").GetComponent<GameSystem>();
	}
	
	// Update is called once per frame
	void Update () {
	
	}
	
	void OnTriggerEnter(Collider other)
	{
		if(other.tag == "Player")
		{
			int Damage = PlayerPrefs.GetInt("Health");
			Debug.Log("fallen");
			this.count = Random.Range(0,_gamesystem.SpwanPoint.Length);
			this._position = _gamesystem.SpwanPoint[count].transform.position;
			other.transform.position = _position;
			other.GetComponent<ControllerPlayer>().running = false;
			PhotonView pv = other.GetComponent<PhotonView>();
			pv.RPC("Hit", PhotonNetwork.player, Damage, false);
		}
	}
}
