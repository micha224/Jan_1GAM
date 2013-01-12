using UnityEngine;
using System.Collections;

public class NetworkMovement : Photon.MonoBehaviour {


    private Vector3 correctPlayerPos = Vector3.zero; //We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; //We lerp towards this
	private int health = 0;
	private int lives = 0;
	
	// Use this for initialization
	void Awake () {
		GameSystem _gameSystem = GameObject.Find("_System").GetComponent<GameSystem>();
        ControllerPlayer controllerScript = GetComponent<ControllerPlayer>();
		Stats statsScript = GetComponent<Stats>();

        if (photonView.isMine)
        {
            controllerScript.enabled = true;
			statsScript.enabled = true;
        }
        else
        {

            controllerScript.enabled = true;
			statsScript.enabled = true;
            controllerScript.InControl = false;
			statsScript.InControl = false;
        }

	}

    void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.isWriting)
        {
            //We own this player: send the others our data
            stream.SendNext(transform.position);
            stream.SendNext(transform.rotation);
			
			Stats statsScript = GetComponent<Stats>();
			stream.SendNext(statsScript.Health);
			stream.SendNext(statsScript.Lives);
        }
        else
        {
            //Network player, receive data
            this.correctPlayerPos = (Vector3)stream.ReceiveNext();
            this.correctPlayerRot = (Quaternion)stream.ReceiveNext();
			
			Stats statsScript = GetComponent<Stats>();
			statsScript.Health = (int)stream.ReceiveNext();
			statsScript.Lives = (int)stream.ReceiveNext();
        }
    }
	// Update is called once per frame
	void Update () {
		if(photonView.isMine)
		{
			GameSystem _gameSystem = GameObject.Find("_System").GetComponent<GameSystem>();
			ControllerPlayer controllerScript = GetComponent<ControllerPlayer>();
			
		  	if(_gameSystem.GameOpen)
			{
				controllerScript.InControl = false;
			}
			else if(!_gameSystem.GameOpen)
			{
				controllerScript.InControl = true;
			}
		}
		     if (!photonView.isMine)
        {
        	if(correctPlayerPos==Vector3.zero) return;
        	
            //Update remote player (smooth this, this looks good, at the cost of some accuracy)
            if (Vector3.Distance(correctPlayerPos, transform.position) < 4)
            {
                transform.position = Vector3.Lerp(transform.position, correctPlayerPos, Time.deltaTime * 5);
                transform.rotation = Quaternion.Lerp(transform.rotation, correctPlayerRot, Time.deltaTime * 5);
            }
            else
            {
                 transform.position =  correctPlayerPos;
                 transform.rotation = correctPlayerRot;
            }
        }
	}
	
}
