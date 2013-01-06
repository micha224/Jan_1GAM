using UnityEngine;
using System.Collections;

public class Attack_Sword : Photon.MonoBehaviour {
	
	public bool Attacking = false;
	public ControllerPlayer controller;
	// Use this for initialization
	void Start () {
		//this.transform.collider.isTrigger = true;
	}
	
	// Update is called once per frame
	void Update () {
		if(controller.InControl)
		{
			if(this.animation.IsPlaying("Attack"))
			{
				this.Attacking = true;
			}
			else
			{
				this.Attacking = false;	
			}
			if(!Attacking)
			{
				if(Input.GetButtonDown("Fire1"))
				{
					this.transform.parent.GetComponent<PhotonView>().RPC("AttackAnimation", PhotonTargets.All);
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
				SendHit(other);
			}	
		}
	}
	*/
    public void AttackAnimation()
    {
		Debug.Log("Attack");
        this.animation.Play("Attack");
    }
	
	public void SendHit(Collider other)
	{
		int Damage = 15;
		Debug.Log("hello");
		PhotonView pv = other.GetComponent<PhotonView>();
		pv.RPC("Hit",PhotonNetwork.player, Damage, false);
	}
}
