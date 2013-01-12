using UnityEngine;
using System.Collections;

public class Bullet : Photon.MonoBehaviour {
	//public Vector3 _mousePosition;
	
	// Use this for initialization
	void Start () {
		//this.collider.isTrigger  = true;
		//	if(Input.mousePosition.x < Screen.width/2)
		//	{
	//			_mousePosition = -this.transform.right;	
		//	}
	//		else if(Input.mousePosition.x > Screen.width/2)
	//		{
	//			_mousePosition = this.transform.right;	
	//		}
	}
	
	// Update is called once per frame
	void Update () 
	{
	}

	void OnCollisionEnter(Collision other)
	{
		if(other.collider.transform.tag == "Player")
		{
			
		}
		else if(other.collider.transform.tag == "Weapon" || other.collider.transform.tag == "Sword")
		{
			
		}
		else if(other.collider.transform.tag == "Wall")
		{
			Destroy(this.gameObject);	
		}
		else
		{
			Destroy(this.gameObject);
		}
	}
	
	
	
}
