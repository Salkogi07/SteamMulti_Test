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
    
    // 로비 떠남 콜백
    protected Callback<LobbyChatUpdate_t> LobbyChatUpdate;
    
    List<CSteamID> lobbyIDs = new List<CSteamID>();

    //Variables
    public ulong CurrentLobbyID;
    private const string HostAddressKey = "HostAddress";
    private CustomNetworkManager manager;
    
    private string currentLobbyName;


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
        
        LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    public void LeaveLobby()
    {
        CSteamID currentOwner = SteamMatchmaking.GetLobbyOwner(new CSteamID(CurrentLobbyID));
        CSteamID me = SteamUser.GetSteamID();
        var lobby = new CSteamID(CurrentLobbyID);
        List<CSteamID> members = new List<CSteamID>();

        int count = SteamMatchmaking.GetNumLobbyMembers(lobby);

        for (int i = 0; i < count; i++)
        {
            members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobby, i));
        }

        if (CurrentLobbyID != 0)
        {
            SteamMatchmaking.LeaveLobby(new CSteamID(CurrentLobbyID));
            CurrentLobbyID = 0;
        }

        if (NetworkServer.active && currentOwner == me)
        {
            NetworkManager.singleton.StopHost();
        }
        else if (NetworkClient.isConnected)
        {
            NetworkManager.singleton.StopClient();
        }
    }
    
    public void HostLobby(ELobbyType lobbyType, string lobbyName)
    {
        currentLobbyName = lobbyName; // 로비 이름 저장
        SteamMatchmaking.CreateLobby(lobbyType, manager.maxConnections);
    }

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if(callback.m_eResult != EResult.k_EResultOK) 
        {
            return; 
        }

        Debug.Log("Lobby created Successfully");
        
        manager.StartHost();
        
        CurrentLobbyID = callback.m_ulSteamIDLobby;
        CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);

        SteamMatchmaking.SetLobbyData(lobbyId, HostAddressKey, SteamUser.GetSteamID().ToString());
        SteamMatchmaking.SetLobbyData(lobbyId, "name", currentLobbyName);
    }

    private void OnJoinRequest(GameLobbyJoinRequested_t callback)
    {
        Debug.Log("Request To Join Lobby");
        SteamMatchmaking.JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        CurrentLobbyID = callback.m_ulSteamIDLobby;
        
        if(LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdateLobbyName();
        }
        
        if(NetworkServer.active) { return; }

        manager.networkAddress = SteamMatchmaking.GetLobbyData(new CSteamID(callback.m_ulSteamIDLobby), HostAddressKey);
        manager.StartClient();
    }
    
    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if ((EChatMemberStateChange)callback.m_rgfChatMemberStateChange != EChatMemberStateChange.k_EChatMemberStateChangeEntered)
        {
            if(NetworkServer.active)
            {
                // 로비에 없는 플레이어를 서버에서 추방합니다.
                StartCoroutine(DelayedDisconnectCheck());
            }
        }
    }
    
    private IEnumerator DelayedDisconnectCheck()
    {
        // Steam 정보가 업데이트될 시간을 약간 줍니다.
        yield return new WaitForSeconds(0.5f);

        if (!NetworkServer.active) yield break;

        List<PlayerObjectController> playersToDisconnect = new List<PlayerObjectController>();
        int numLobbyMembers = SteamMatchmaking.GetNumLobbyMembers(new CSteamID(CurrentLobbyID));
        List<ulong> steamLobbyMembers = new List<ulong>();

        for (int i = 0; i < numLobbyMembers; i++)
        {
            steamLobbyMembers.Add(SteamMatchmaking.GetLobbyMemberByIndex(new CSteamID(CurrentLobbyID), i).m_SteamID);
        }

        foreach (var player in manager.GamePlayers)
        {
            if (!steamLobbyMembers.Contains(player.PlayerSteamID))
            {
                playersToDisconnect.Add(player);
            }
        }

        foreach (var player in playersToDisconnect)
        {
            Debug.Log($"Player {player.PlayerName} (SteamID: {player.PlayerSteamID}) is no longer in the Steam lobby. Disconnecting.");
            player.connectionToClient.Disconnect();
        }
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
            
            // 친구가 우리 게임을 하고 있는지 확인
            if (SteamFriends.GetFriendGamePlayed(friendSteamId, out FriendGameInfo_t friendGameInfo) && friendGameInfo.m_gameID.AppID() == SteamUtils.GetAppID())
            {
                // 친구가 로비에 있는지 확인
                if (friendGameInfo.m_steamIDLobby.IsValid())
                {
                    Debug.Log($"Friend {SteamFriends.GetFriendPersonaName(friendSteamId)} is in a lobby. Requesting data.");
                    // 중복 추가 방지
                    if (!lobbyIDs.Contains(friendGameInfo.m_steamIDLobby))
                    {
                        lobbyIDs.Add(friendGameInfo.m_steamIDLobby);
                    }
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
        if (LobbyListManager.instance != null && LobbyListManager.instance.listOfLobbies.Count > 0)
        {
            LobbyListManager.instance.DestroyLobbies();
        }

        for(int i=0; i < result.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyID = SteamMatchmaking.GetLobbyByIndex(i);
            if (!lobbyIDs.Contains(lobbyID))
            {
                lobbyIDs.Add(lobbyID);
            }
            SteamMatchmaking.RequestLobbyData(lobbyID);
        }
    }
}