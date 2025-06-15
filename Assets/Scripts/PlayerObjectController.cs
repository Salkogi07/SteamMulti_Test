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

    public override void OnStartAuthority()
    {
        // 이 객체가 로컬 플레이어의 객체임을 의미합니다.
        CmdSetPlayerName(SteamFriends.GetPersonaName().ToString());
        gameObject.name = "LocalGamePlayer";
        
        // LobbyController에 자신을 직접 등록합니다.
        // LobbyController.Instance가 null일 수 있는 극단적인 경우를 대비
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.SetLocalPlayerController(this);
            LobbyController.Instance.UpdateLobbyName();
        }
        else
        {
            // LobbyController가 아직 준비되지 않았다면, 0.1초 후에 다시 시도
            Invoke(nameof(RetrySetLocalPlayer), 0.1f);
        }
    }

    // ✅ 추가된 부분: 재시도 로직
    void RetrySetLocalPlayer()
    {
        if (LobbyController.Instance != null)
        {
            LobbyController.Instance.SetLocalPlayerController(this);
            LobbyController.Instance.UpdateLobbyName();
        }
    }


    public override void OnStopClient()
    {
        // Manager의 플레이어 리스트에서 제거
        Manager.GamePlayers.Remove(this);
        
        if (LobbyController.Instance != null)
        {
            // 전체 플레이어 UI 리스트 업데이트
            LobbyController.Instance.UpdatePlayerList();

            // ✅ 추가된 부분: 만약 이 객체가 로컬 플레이어였다면, LobbyController의 참조를 명시적으로 null로 만듭니다.
            // 이렇게 함으로써 이전 세션의 '죽은' 참조가 남는 것을 방지합니다.
            if (authority && LobbyController.Instance.LocalplayerController == this)
            {
                LobbyController.Instance.SetLocalPlayerController(null);
            }
        }
    }
    
    // (이하 다른 함수들은 이전과 동일)
    // ...
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
        if (authority)
        {
            // Debug.Log("Changing Ready - Command will be sent.");
            CmdSetPlayerReady();
        }
        else
        {
            // Debug.LogWarning("Tried to change ready status without authority.");
        }
    }

    public override void OnStartClient()
    {
        Manager.GamePlayers.Add(this);
        LobbyController.Instance?.UpdateLobbyName();
        LobbyController.Instance?.UpdatePlayerList();
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