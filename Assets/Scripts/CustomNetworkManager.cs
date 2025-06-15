// --- START OF FILE CustomNetworkManager.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using UnityEngine.SceneManagement;
using Steamworks;

public class CustomNetworkManager : NetworkManager
{
    [SerializeField] private PlayerObjectController GamePlayerPrefab;

    public List<PlayerObjectController> GamePlayers { get; } = new List<PlayerObjectController>();

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        if(SceneManager.GetActiveScene().name == "Lobby")
        {
            PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            
            CSteamID lobbyId = new CSteamID(SteamLobby.Instance.CurrentLobbyID);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, memberCount - 1);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public override void Awake()
    {
        transport = GameObject.FindWithTag("SteamManager").GetComponent<Transport>();
        
        base.Awake();
    }
    
    public override void OnStopClient()
    {
        GamePlayers.Clear();
        base.OnStopClient();
    }
    
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if(conn.identity != null)
        {
            var player = conn.identity.GetComponent<PlayerObjectController>();
            if(player != null)
            {
                GamePlayers.Remove(player);
            }
        }
        base.OnServerDisconnect(conn);
    }

    public void StartGame(string SceneName)
    {
        //게임 시작 전 로비 잠금
        SteamMatchmaking.SetLobbyJoinable(new CSteamID(SteamLobby.Instance.CurrentLobbyID), false);
        
        ServerChangeScene(SceneName);
    }
}