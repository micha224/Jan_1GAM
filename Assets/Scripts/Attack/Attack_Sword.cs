using UnityEngine;
using System.Collections;

public class Attack_Sword : Photon.MonoBehaviour {
	
	public bool Attacking = false;
	public ControllerPlayer controller;
	// Use this for initialization
	void Start () {
		this.transform.collider.isTrigger = true;
	}
	
	// Update is called once per frame
	void Update () {
		if(controller.InControl)
		{
			if(this.transform.parent.animation.IsPlaying("Attack"))
			{
				this.Attacking = true;
			}
			else
			{
				this.Attacking = false;	
				controller._characterState = CharacterState.Walking;
			}
			if(!Attacking)
			{
				if(Input.GetButtonDown("Fire1"))
				{
					this.transform.root.GetComponent<PhotonView>().RPC("AttackAnimation", PhotonTargets.All, false);
					StartCoroutine(reload());
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
	
	void OnTriggerEnter(Collider other)
	{
		if(this.Attacking)
		{
			if(other.CompareTag("Player"))
			{
				Debug.Log("Got Hit");
				if(other.GetComponent<Stats>())
				{
					other.collider.GetComponent<PhotonView>().RPC("Damage", PhotonTargets.All, 5);
					Debug.Log("ah");
				}
				this.Attacking = false;
				//controller._characterState = CharacterState.Walking;
			}	
		}
	}
	
	
	
    IEnumerator reload()
    {
		Debug.Log("Attack");
        yield return new WaitForSeconds(1);
		this.transform.root.GetComponent<PhotonView>().RPC("AttackAnimation", PhotonTargets.All, true);
		//controller._characterState = CharacterState.Walking;
   }
	
	public void SendHit(Collider other)
	{
		int Damage = 15;
		Debug.Log("hello");
		PhotonView pv = other.GetComponent<PhotonView>();
		pv.RPC("Hit",PhotonNetwork.player, Damage, false);
	}
}
