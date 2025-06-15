// --- START OF FILE PlayerObjectController.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;

public class PlayerObjectController : NetworkBehaviour
{
    //Player Data
    [SyncVar] public int ConnectionID;
    [SyncVar] public int PlayerIdNumber;
    [SyncVar] public ulong PlayerSteamID;
    [SyncVar(hook = nameof(PlayerNameUpdate))] public string PlayerName;
    [SyncVar(hook = nameof(PlayerReadyUpdate))] public bool Ready;

    private CustomNetworkManager manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if(manager != null)
            {
                return manager;
            }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    // ✅ 수정: Ready 상태가 변경될 때 클라이언트에서 호출되는 Hook
    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        // isClient는 호스트와 클라이언트 모두에서 true입니다.
        // Ready 상태가 변경될 때마다 UI를 업데이트하도록 LobbyController에 알립니다.
        if (isClient)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    // ✅ 수정: Ready 상태 변경을 서버에 요청하는 Command
    [Command]
    private void CmdSetPlayerReady()
    {
        // 서버에서 Ready 상태를 토글합니다.
        // 이 SyncVar가 변경되면 모든 클라이언트에서 PlayerReadyUpdate Hook이 자동으로 호출됩니다.
        Ready = !Ready;
    }

    // 로비의 "Ready" 버튼을 눌렀을 때 호출됩니다.
    public void ChangeReady()
    {
        // 로컬 플레이어만이 자신의 상태를 변경할 수 있습니다.
        if (authority)
        {
            CmdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        LobbyController.Instance.FindLocalPlayer();
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance.UpdateLobbyName();
        LobbyController.Instance.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        // OnStopClient는 isClient가 false가 된 후에 호출될 수 있으므로,
        // LobbyController가 null이 아닐 때만 업데이트를 시도합니다.
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
        
        // GamePlayers 리스트에서 제거하는 것은 Manager의 OnServerDisconnect에서도 처리하지만,
        // 클라이언트 측에서 즉시 리스트를 정리하기 위해 여기서도 호출합니다.
        if (Manager.GamePlayers.Contains(this))
        {
            Manager.GamePlayers.Remove(this);
        }
    }

    [Command]
    private void CmdSetPlayerName(string PlayeName)
    {
        this.PlayerNameUpdate(this.PlayerName, PlayeName);
    }

    // 이름이 변경될 때 클라이언트에서 호출되는 Hook
    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if (isServer) //Host
        {
            this.PlayerName = NewValue;
        }
        if (isClient) //Client
        {
            // LobbyController가 아직 준비되지 않았을 수 있으므로 null 체크
            if(LobbyController.Instance != null)
            {
                LobbyController.Instance.UpdatePlayerList();
            }
        }
    }

    //Start Game
    public void CanStartGame(string SceneName)
    {
        if (authority)
        {
            CmdCanStartGame(SceneName);
        }
    }

    [Command]
    public void CmdCanStartGame(string SceneName)
    {
        manager.StartGame(SceneName);
    }
}