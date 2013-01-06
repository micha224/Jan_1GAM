using UnityEngine;
using System.Collections;

public class NetworkMovement : Photon.MonoBehaviour {


    private Vector3 correctPlayerPos = Vector3.zero; //We lerp towards this
    private Quaternion correctPlayerRot = Quaternion.identity; //We lerp towards this
	private int health = 0;
	private int lives = 0;
	private GameSystem _gameSystem;
    ControllerPlayer controllerScript;
	Stats statsScript;
	// Use this for initialization
	void Awake () {
		_gameSystem = GameObject.Find("_System").GetComponent<GameSystem>();
        controllerScript = GetComponent<ControllerPlayer>();
		statsScript = GetComponent<Stats>();

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
			//stream.SendNext(statsScript.Health);
			//stream.SendNext(statsScript.Lives);
        }
        else
        {
            //Network player, receive data
            correctPlayerPos = (Vector3)stream.ReceiveNext();
            correctPlayerRot = (Quaternion)stream.ReceiveNext();
			//health = (int)stream.ReceiveNext();
			//lives = (int)stream.ReceiveNext();
        }
    }
	// Update is called once per frame
	void Update () {
		if(photonView.isMine)
		{
			
		  	if(_gameSystem.GameOpen)
			{
				controllerScript.InControl = false;
			}
			else if(!_gameSystem.GameOpen)
			{
				controllerScript.InControl = true;
			}
		}
	
	}
}
