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
        if(SceneManager.GetActiveScene().name == "Lobby") // 혹은 onlineScene 이름
        {
            PlayerObjectController GamePlayerInstance = Instantiate(GamePlayerPrefab);

            GamePlayerInstance.ConnectionID = conn.connectionId;
            GamePlayerInstance.PlayerIdNumber = GamePlayers.Count + 1;
            // ✅ 수정된 부분: 호스트가 나갔다 다시 방을 만들었을 때를 대비해 GamePlayers.Count가 아닌 실제 로비 멤버 수로 계산
            int memberCount = SteamMatchmaking.GetNumLobbyMembers((CSteamID)SteamLobby.Instance.CurrentLobbyID);
            GamePlayerInstance.PlayerSteamID = (ulong)SteamMatchmaking.GetLobbyMemberByIndex((CSteamID)SteamLobby.Instance.CurrentLobbyID, memberCount - 1);

            NetworkServer.AddPlayerForConnection(conn, GamePlayerInstance.gameObject);
        }
    }

    public void StartGame(string SceneName)
    {
        foreach (var player in GamePlayers)
        {
            player.RpcShowLoadingScreen();
        }
        ServerChangeScene(SceneName);
    }
}