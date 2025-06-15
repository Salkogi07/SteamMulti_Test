using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Steamworks;

public class LobbyEntryData : MonoBehaviour
{
    //Lobby Data
    public CSteamID lobbySteamID;
    public string lobbyName;
    public Text lobbyNameText;
    public Button joinLobbyButton;


    public void SetLobbyName()
    {
        if(lobbyName == "")
        {
            lobbyNameText.text = "No Name Lobby";
        }
        else
        {
            lobbyNameText.text = lobbyName;
        }
    }
  
    public void JoinLobby()
    {
        SteamLobby.Instance.JoinLobby(lobbySteamID);
    }

}