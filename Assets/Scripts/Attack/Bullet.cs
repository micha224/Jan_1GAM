using UnityEngine;
using System.Collections;

public class Bullet : MonoBehaviour {
	public int BulletSpeed;
	public Vector3 _mousePosition;
	
	// Use this for initialization
	void Awake () {
		//this.collider.isTrigger  = true;
		if(Input.mousePosition.x < Screen.width/2)
		{
			_mousePosition = -this.transform.right;	
		}
		else if(Input.mousePosition.x > Screen.width/2)
		{
			_mousePosition = this.transform.right;	
		}
		
	}
	
	// Update is called once per frame
	void Update () 
	{
		this.rigidbody.AddForce(_mousePosition * BulletSpeed);
	}

	void OnCollisionEnter(Collision other)
	{
		if(other.collider.transform.tag == "Player"){}
		else if(other.collider.transform.tag != "Player")
		{
			Destroy(this.gameObject);
		}
	}
	
}
