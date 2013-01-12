using UnityEngine;
using System.Collections;

public class Attack_Gun : Photon.MonoBehaviour {
	
	public bool Attacking = false;
	public ControllerPlayer controller;
	public GameObject BulletPrefab;
	public int BulletSpeed;
	public Vector3 _mousePosition;
	
	// Use this for initialization
	void Start () {
		//this.transform.collider.isTrigger = true;
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
			if(Input.mousePosition.x < Screen.width/2)
			{
				_mousePosition = this.transform.right;	
			}
			else if(Input.mousePosition.x > Screen.width/2)
			{
				_mousePosition = this.transform.right;	
			}
			if(!this.Attacking)
			{
				if(Input.GetButtonDown("Fire1"))
				{
					this.transform.GetComponent<PhotonView>().RPC("AttackInstantiate", PhotonTargets.All, _mousePosition);
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
	[RPC]
    public void AttackInstantiate(Vector3 direction)
    {
		Debug.Log("Attack");
		GameObject bullet = GameObject.Instantiate(BulletPrefab, this.transform.position, Quaternion.identity) as GameObject;
		bullet.rigidbody.AddForce(direction * BulletSpeed);
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
