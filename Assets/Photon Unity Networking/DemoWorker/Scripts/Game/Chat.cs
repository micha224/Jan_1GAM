using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// This simple chat example showcases the use of RPC targets and targetting certain players via RPCs.
/// </summary>
public class Chat : Photon.MonoBehaviour
{

    public static Chat SP;
    public List<string> messages = new List<string>();

    private int chatHeight = (int)250;
	private int chatwidth = 200;
    private Vector2 scrollPos = Vector2.zero;
    private string chatInput = "";
	private GameSystem _gameSystem;
	private EndOfGame _end;

    void Awake()
    {
		_gameSystem = GameObject.Find("_System").GetComponent<GameSystem>();
		_end = GameObject.Find("EndOfGame").GetComponent<EndOfGame>();
        SP = this;
    }

    void OnGUI()
    {
		if(!_gameSystem.GameOpen)
		{
			if(!_end.WinnerAnnouncedGUI)
			{
				return;
			}
		}
        GUILayout.BeginArea(new Rect(Screen.width- chatwidth, Screen.height - chatHeight, chatwidth, chatHeight));
        
        //Show scroll list of chat messages
        scrollPos = GUILayout.BeginScrollView(scrollPos, "box");
        GUI.color = Color.white;
        for (int i = 0; i <= messages.Count-1; i++)
        {
            GUILayout.Label(messages[i]);
        }
        GUILayout.EndScrollView();
        GUI.color = Color.white;

        //Chat input
        chatInput = GUILayout.TextField(chatInput);

        //Group target buttons
        GUILayout.BeginHorizontal();
		GUI.color = Color.white;
        if (GUILayout.Button("Send", GUILayout.Height(17)))
		{
            SendChat(PhotonTargets.All);
		}
		if (Event.current.type == EventType.keyDown && Event.current.character == '\n') 
		{
			Debug.Log("ad");
			SendChat(PhotonTargets.All);
		}
        GUILayout.EndHorizontal();
        GUILayout.EndArea();
    }
	
	
    public static void AddMessage(string text)
    {
        SP.messages.Add(text);
        if (SP.messages.Count > 15)
            SP.messages.RemoveAt(0);
    }


    [RPC]
    void SendChatMessage(string text, PhotonMessageInfo info)
    {
        AddMessage("[" + info.sender + "] " + text);
    }

    void SendChat(PhotonTargets target)
    {
        photonView.RPC("SendChatMessage", target, chatInput);
        chatInput = "";
    }

    void SendChat(PhotonPlayer target)
    {
        chatInput = "[PM] " + chatInput;
        photonView.RPC("SendChatMessage", target, chatInput);
        chatInput = "";
    }
}
