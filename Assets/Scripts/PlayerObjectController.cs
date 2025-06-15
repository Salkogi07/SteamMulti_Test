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
        // 로비 UI를 숨기고 로딩 화면을 보여주도록 수정
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.gameObject.SetActive(false); // 로비 UI 전체 비활성화
            if (LobbyController.Instance.LoadingScreen != null)
            {
                LobbyController.Instance.LoadingScreen.SetActive(true);
            }
        }
    }

    private void PlayerReadyUpdate(bool oldValue, bool newValue)
    {
        if (isClient)
        {
            LobbyController.Instance?.UpdatePlayerItem();
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
        }
    }

    public override void OnStartAuthority()
    {
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        
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
        // ✅ 수정: 로컬 플레이어가 나갈 때만 처리하도록 변경
        if (isLocalPlayer)
        {
            Manager.GamePlayers.Remove(this);
            if (LobbyController.Instance != null)
            {
                LobbyController.Instance.UpdatePlayerList();
            }
        }
        
        // 방장이 나갔을 때 모든 클라이언트의 플레이어 리스트 업데이트
        if (LobbyController.Instance != null && isClientOnly)
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
            LobbyController.Instance?.UpdatePlayerItem();
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