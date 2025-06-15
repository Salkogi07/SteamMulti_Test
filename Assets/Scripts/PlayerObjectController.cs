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
        // isClient: 모든 클라이언트에서 실행
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
        if (authority)
        {
            CmdSetPlayerReady();
            // ✅ 추가된 부분: 커맨드를 보낸 클라이언트에서 즉시 UI가 바뀌도록 함. (서버 응답을 기다리지 않음)
            // 서버에서 SyncVar가 업데이트되면 다른 클라이언트들도 변경됨.
            if(LobbyController.Instance != null)
            {
                LobbyController.Instance.UpdateButton();
            }
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
        
        // ✅ 추가된 부분: 로컬 플레이어가 나갈 때 LocalplayerController 참조를 null로 설정
        if (authority && LobbyController.Instance != null && LobbyController.Instance.LocalplayerController == this)
        {
            LobbyController.Instance.LocalplayerController = null;
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