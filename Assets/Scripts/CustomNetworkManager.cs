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
        if(onlineScene == "" || SceneManager.GetActiveScene().name == onlineScene)
        {
            PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            
            // ✅ 수정: SteamLobby 인스턴스에서 현재 로비 ID를 가져옴
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

    // ✅ 추가: 호스트 중지 시 로비 정리
    public override void OnStopHost()
    {
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.ClearLobby();
        }
        base.OnStopHost();
    }

    // ✅ 추가: 클라이언트 중지 시 로비 정리
    public override void OnStopClient()
    {
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.ClearLobby();
        }
        GamePlayers.Clear(); // 클라이언트에서는 자신의 목록만 정리
        base.OnStopClient();
    }
    
    // ✅ 추가: 서버에서 플레이어가 나갈 때 GamePlayers 리스트에서 제거
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
        // ✅ 추가: 게임 시작 전 로비 잠금
        SteamMatchmaking.SetLobbyJoinable(new CSteamID(SteamLobby.Instance.CurrentLobbyID), false);

        foreach (var player in GamePlayers)
        {
            player.RpcShowLoadingScreen();
        }
        ServerChangeScene(SceneName);
    }
}