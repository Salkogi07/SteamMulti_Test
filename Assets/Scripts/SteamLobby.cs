// --- START OF FILE SteamLobby.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;

public class SteamLobby : MonoBehaviour
{
    public static SteamLobby Instance;

    //Main Callbacks
    protected Callback<LobbyCreated_t> LobbyCreated;
    protected Callback<GameLobbyJoinRequested_t> JoinRequest;
    protected Callback<LobbyEnter_t> LobbyEntered;

    //Lobby List Callbacks
    protected Callback<LobbyMatchList_t> LobbyList;
    protected Callback<LobbyDataUpdate_t> LobbyDataUpdate;

    List<CSteamID> lobbyIDs = new List<CSteamID>();

    //Variables
    public ulong CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager manager;


    private void Start()
    {
        if (!SteamManager.Initialized) { return; }
        if(Instance == null) { Instance = this; }

        manager = GetComponent<CustomNetworkManager>();

        LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        JoinRequest = Callback<GameLobbyJoinRequested_t>.Create(OnJoinRequest);
        LobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);

        LobbyList = Callback<LobbyMatchList_t>.Create(OnGetLobbyList);
        LobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdate);
    }

    public void HostLobby()
    {
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, manager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK) { return; }

        Debug.Log("Lobby created Successfully");
        
        manager.StartHost();
        
        CurrentLobbyID = callback.m_ulSteamIDLobby; // Store lobby ID

        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), "name", SteamFriends.GetPersonaName().ToString() + "'S LOBBY");
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Request To Join Lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;

        if(NetworkServer.active) { return; }

        manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        manager.StartClient();
    }

    public void JoinLobby(CSteamID lobbyID)
    {
        SteamMatchmaking.JoinLobby(lobbyID);
    }
    
    public void GetPublicLobbies()
    {
        if (lobbyIDs.Count > 0) { lobbyIDs.Clear(); }
        LobbyListManager.instance.DestroyLobbies();
        SteamMatchmaking.AddRequestLobbyListResultCountFilter(50);
        SteamMatchmaking.RequestLobbyList();
    }

    public void GetFriendLobbies()
    {
        if (lobbyIDs.Count > 0) { lobbyIDs.Clear(); }
        LobbyListManager.instance.DestroyLobbies();

        int friendCount = SteamFriends.GetFriendCount(EFriendFlags.k_EFriendFlagAll);
        Debug.Log($"Found {friendCount} friends.");

        for (int i = 0; i < friendCount; i++)
        {
            CSteamID friendSteamId = SteamFriends.GetFriendByIndex(i, EFriendFlags.k_EFriendFlagAll);

            // 친구가 게임을 하고 있는지, 그리고 그 게임이 '우리 게임'인지 확인
            if (SteamFriends.GetFriendGamePlayed(friendSteamId, out FriendGameInfo_t friendGameInfo) && friendGameInfo.m_gameID.AppID() == SteamUtils.GetAppID())
            {
                // 친구가 로비에 있다면, 해당 로비 정보를 요청
                if (friendGameInfo.m_steamIDLobby.IsValid())
                {
                    Debug.Log($"Friend {SteamFriends.GetFriendPersonaName(friendSteamId)} is in a lobby. Requesting data.");
                    lobbyIDs.Add(friendGameInfo.m_steamIDLobby);
                    SteamMatchmaking.RequestLobbyData(friendGameInfo.m_steamIDLobby);
                }
            }
        }
        
        if (lobbyIDs.Count == 0)
        {
            Debug.Log("No friends are currently playing in a lobby.");
        }
    }

    void OnLobbyDataUpdate(LobbyDataUpdate_t result)
    {
        if (LobbyListManager.instance != null)
        {
            LobbyListManager.instance.DisplayLobbies(lobbyIDs, result);
        }
    }

    void OnGetLobbyList(LobbyMatchList_t result)
    {
        if (LobbyListManager.instance.listOfLobbies.Count > 0)
        {
            LobbyListManager.instance.DestroyLobbies();
        }

        for(int i=0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            lobbyIDs.Add(lobbyID);
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }
}