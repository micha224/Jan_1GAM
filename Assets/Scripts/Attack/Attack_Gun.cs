using UnityEngine;
using System.Collections;

public class Attack_Gun : Photon.MonoBehaviour {
	
	public bool Attacking = false;
	public ControllerPlayer controller;
	public GameObject BulletPrefab;
	
	// Use this for initialization
	void Start () {
		this.transform.collider.isTrigger = true;
	}
	
	// Update is called once per frame
	void Update () {
		if(controller.InControl)
		{
			//if(this.animation.IsPlaying("Attack"))
			//{
			//	this.Attacking = true;
			//}
			//else
			//{
			//	this.Attacking = false;	
			//}
			if(!this.Attacking)
			{
				if(Input.GetButtonDown("Fire1"))
				{
					this.transform.parent.GetComponent<PhotonView>().RPC("AttackAnimation", PhotonTargets.All);
					this.Attacking = true;
					StartCoroutine(ReloadTime());
				}
			}
			if(Input.GetButtonDown("Fire2"))
			{
				this.Attacking = false;	
			}
		}
		else
		{
			
		}
		
	}
	/*
	void OnTriggerEnter(Collider other)
	{
		if(this.Attacking)
		{
			if(other.tag == "Player")
			{
				int Damage = 15;
				Debug.Log("hello");
				PhotonView pv = other.GetComponent<PhotonView>();
				pv.RPC("Fall",PhotonNetwork.player, Damage, false);
			}	
		}
	}
	*/
    public void AttackInstantiate()
    {
		Debug.Log("Attack");
		GameObject bullet = GameObject.Instantiate(BulletPrefab, this.transform.position, Quaternion.identity) as GameObject;
		//bullet.transform.parent = this.transform;
    }
	
	IEnumerator ReloadTime()
	{
		if(this.Attacking)
		{
			yield return new WaitForSeconds(1);
			this.Attacking = false;
		}
	}
}
