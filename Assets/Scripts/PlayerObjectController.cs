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
            if(manager != null) { return manager; }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }

    private void Start()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    [ClientRpc]
    public void RpcShowLoadingScreen()
    {
        if (LobbyController.Instance != null && LobbyController.Instance.LoadingScreen != null)
        {
            LobbyController.Instance.LoadingScreen.SetActive(true);
        }
    }

    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isClient)
        {
            LobbyController.Instance?.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerReady()
    {
        this.Ready = !this.Ready;
    }

    public void ChangeReady()
    {
        Debug.Log("Changing Out IF Ready");
        
        if (authority)
        {
            Debug.Log("Changing Ready");
            CmdSetPlayerReady();
        }
    }

    public override void OnStartAuthority()
    {
        // 이 객체가 로컬 플레이어의 객체임을 의미합니다.
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer"; // 디버깅을 위해 이름은 그대로 둡니다.
        
        // ✅ 수정된 부분: LobbyController에 자신을 직접 등록합니다.
        LobbyController.Instance.SetLocalPlayerController(this);
        
        LobbyController.Instance.UpdateLobbyName();
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance?.UpdateLobbyName();
        LobbyController.Instance?.UpdatePlayerList();
    }

    public override void OnStopClient()
    {
        Manager.GamePlayers.Remove(this);
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.UpdatePlayerList();
        }
    }

    [Command]
    private void CmdSetPlayerName(string PlayeName)
    {
        this.PlayerName = PlayeName;
    }

    public void PlayerNameUpdate(string OldValue, string NewValue)
    {
        if (isClient)
        {
            LobbyController.Instance?.UpdatePlayerList();
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