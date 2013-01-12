using UnityEngine;
using System.Collections;

public class ControllerPlayer : MonoBehaviour {
	
	private Vector2 _MousePos;
    public bool running = false;
    public float WalkSpeed;
    public float RunSpeed;
    public float JumpSpeed;
    private float speed;
    public float Gravity;
    private Vector3 moveDirection = Vector3.zero;
    public bool InControl = true;
	private Vector3 _PlayerPos;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
    void Update()
    {
		this.transform.position = new Vector3(this.transform.position.x, this.transform.position.y, 0);
        if (InControl)
        {
            if (!running)
            {
                speed = WalkSpeed;
            }
            else if (running)
            {
                speed = RunSpeed;
            }
			_MousePos = Input.mousePosition;
			_PlayerPos = Camera.main.WorldToScreenPoint(this.transform.position);
			if(_MousePos.x < _PlayerPos.x)
			{
				transform.rotation = Quaternion.Euler(0, 180, 0);
			}
			else if (_MousePos.x > _PlayerPos.x || _MousePos.x == _PlayerPos.x)
			{
				transform.rotation = Quaternion.Euler(0,0,0);
			}
            CharacterController controller;
			if(controller = GetComponent<CharacterController>())
			{
				if (controller.isGrounded)
	            {
	           
				if(_MousePos.x < _PlayerPos.x)
				{
	                moveDirection = new Vector3(-Input.GetAxis("Horizontal"), 0, 0);
				}
				else if(_MousePos.x > _PlayerPos.x || _MousePos.x == _PlayerPos.x)
				{
					moveDirection = new Vector3(Input.GetAxis("Horizontal"), 0, 0);
				}
	            moveDirection = transform.TransformDirection(moveDirection);
	            moveDirection *= speed;
					
	                if (Input.GetButtonDown("Jump"))
	                {
	                    moveDirection.y = JumpSpeed;
	                }
	            }
				if (Input.GetButtonDown("Running"))
	            {
	             	running = true;                
	            }
				if (Input.GetButtonUp("Running"))
	            {
	                running = false;
	            }
				if(this.transform.position.z != 0)
				{
					moveDirection.z = 0;	
				}
	            moveDirection.y -= Gravity * Time.deltaTime;
	            controller.Move(moveDirection * Time.deltaTime);
			}
        }
    }
	
	[RPC]
	void PlayDeath(PhotonMessageInfo info)
	{
		Debug.Log("Trigger true");
		//this.animation.Play("Death");
		Destroy(this.GetComponent<CharacterController>());
	}	
}
