// --- START OF FILE LobbyListManager.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Steamworks;
using System.Linq;

public class LobbyListManager : MonoBehaviour
{
    public static LobbyListManager instance;

    //Lobbies
    public GameObject lobbyListMenu;
    public GameObject lobbyEntryPrefab;
    public GameObject scrollViewContent;

    public List<GameObject> listOfLobbies = new List<GameObject>();

    private void Awake()
    {
        if (instance == null) { instance = this; }
    }

    public void OnClick_GetFriendLobbies()
    {
        lobbyListMenu.SetActive(true);
        DestroyLobbies();
        SteamLobby.Instance.GetFriendLobbies();
    }
    
    public void OnClick_GetPublicLobbies()
    {
        lobbyListMenu.SetActive(true);
        DestroyLobbies();
        SteamLobby.Instance.GetPublicLobbies();
    }

    public void DisplayLobbies(List<CSteamID> lobbyIds, LobbyDataUpdate_t result)
    {
        if (listOfLobbies.Any(b => b.GetComponent<LobbyEntryData>().lobbySteamID.m_SteamID == result.m_ulSteamIDLobby))
        {
            return;
        }

        if (lobbyIds.Any(b => b.m_SteamID == result.m_ulSteamIDLobby))
        {
            GameObject createdLobbyItem = Instantiate(lobbyEntryPrefab);
            LobbyEntryData lobbyData = createdLobbyItem.GetComponent<LobbyEntryData>();
            
            lobbyData.lobbySteamID = (CSteamID)result.m_ulSteamIDLobby;
            lobbyData.lobbyName = SteamMatchmaking.GetLobbyData((CSteamID)result.m_ulSteamIDLobby, "name");
            lobbyData.SetLobbyName();

            createdLobbyItem.transform.SetParent(scrollViewContent.transform);
            createdLobbyItem.transform.localScale = Vector3.one;

            listOfLobbies.Add(createdLobbyItem);
        }
    }

    public void DestroyLobbies()
    {
        foreach (GameObject lobbyItem in listOfLobbies)
        {
            Destroy(lobbyItem);
        }
        listOfLobbies.Clear();
    }
}