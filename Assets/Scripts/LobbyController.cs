// --- START OF FILE LobbyController.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using System.Linq;

public class LobbyController : MonoBehaviour // NetworkBehaviour 상속 제거
{
    public static LobbyController Instance;

    //UI Elements
    public Text LobbyNameText;
    public Button LeaveLobbyButton;
    public GameObject LoadingScreen;

    //Player Data
    public GameObject PlayerListViewContent;
    public GameObject PlayerListItemPrefab;
    public GameObject LocalPlayerObject;

    //Other Data
    public ulong CurrentLobbyID;
    public bool PlayerItemCreated = false;
    private List<PlayerListItem> PlayerListItems = new List<PlayerListItem>();
    public PlayerObjectController LocalplayerController;

    //Ready
    public Button StartGameButton;
    public Text ReadyButtonText;

    //Manager
    private CustomNetworkManager manager;

    private CustomNetworkManager Manager
    {
        get
        {
            if (manager != null) { return manager; }
            return manager = CustomNetworkManager.singleton as CustomNetworkManager;
        }
    }
    
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
    }
    
    // ✅ 추가된 부분: 로비 UI 상태를 초기화하는 함수
    public void ClearLobby()
    {
        PlayerItemCreated = false;
        CurrentLobbyID = 0;
        LobbyNameText.text = "Lobby";
        if (LeaveLobbyButton != null) LeaveLobbyButton.gameObject.SetActive(false);
        if (StartGameButton != null) StartGameButton.interactable = false;
        
        // 플레이어 리스트 UI 정리
        foreach (var item in PlayerListItems)
        {
            Destroy(item.gameObject);
        }
        PlayerListItems.Clear();

        LocalplayerController = null;
        LocalPlayerObject = null;
    }

    public void ReadyPlayer()
    {
        // LocalplayerController가 null이 아닐 때만 실행
        if (LocalplayerController != null)
        {
            LocalplayerController.ChangeReady();
        }
    }

    public void UpdateButton()
    {
        if (LocalplayerController == null) return;
        // ✅ 수정된 부분: bool 값에 따라 텍스트를 바로 변경
        ReadyButtonText.text = LocalplayerController.Ready ? "Unready" : "Ready";
    }
    
    // ✅ 추가된 부분: LeaveLobbyButton의 OnClick 이벤트에 연결할 함수
    public void OnClick_LeaveLobby()
    {
        if (SteamLobby.Instance != null)
        {
            SteamLobby.Instance.LeaveLobby();
        }
    }

    public void CheckIfAllReady()
    {
        if (LocalplayerController == null)
        {
            if (StartGameButton != null) StartGameButton.interactable = false;
            return;
        }

        bool allReady = Manager.GamePlayers.All(player => player.Ready);
        
        if (StartGameButton != null)
        {
            // ✅ 수정된 부분: 호스트(PlayerIdNumber == 1)이고 모두 준비되었을 때만 시작 버튼 활성화
            StartGameButton.interactable = allReady && Manager.GamePlayers.Count > 0 && LocalplayerController.PlayerIdNumber == 1;
        }
    }

    public void UpdateLobbyName()
    {
        // ✅ 수정된 부분: SteamLobby 인스턴스에서 CurrentLobbyID를 가져옴
        if (SteamLobby.Instance == null || SteamLobby.Instance.CurrentLobbyID == 0) return;

        CurrentLobbyID = SteamLobby.Instance.CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
        if (LeaveLobbyButton != null) LeaveLobbyButton.gameObject.SetActive(true);
    }

    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated) { CreateHostPlayerItem(); }
        if (PlayerListItems.Count < Manager.GamePlayers.Count) { CreateClientPlayerItem(); }
        if (PlayerListItems.Count > Manager.GamePlayers.Count) { RemovePlayerItem(); }
        if (PlayerListItems.Count == Manager.GamePlayers.Count) { UpdatePlayerItem(); }
    }

    public void FindLocalPlayer()
    {
        LocalPlayerObject = GameObject.Find("LocalGamePlayer");
        if (LocalPlayerObject != null)
        {
            LocalplayerController = LocalPlayerObject.GetComponent<PlayerObjectController>();
        }
    }

    public void CreateHostPlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (!PlayerListItems.Any(b => b.ConnectionID == player.ConnectionID))
            {
                 CreatePlayerItem(player);
            }
        }
        PlayerItemCreated = true;
    }

    public void CreateClientPlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            if (!PlayerListItems.Any(b => b.ConnectionID == player.ConnectionID))
            {
                CreatePlayerItem(player);
            }
        }
    }

    private void CreatePlayerItem(PlayerObjectController player)
    {
        GameObject NewPlayerItem = Instantiate(PlayerListItemPrefab) as GameObject;
        PlayerListItem NewPlayerItemScript = NewPlayerItem.GetComponent<PlayerListItem>();

        NewPlayerItemScript.PlayerName = player.PlayerName;
        NewPlayerItemScript.ConnectionID = player.ConnectionID;
        NewPlayerItemScript.PlayerSteamID = player.PlayerSteamID;
        NewPlayerItemScript.Ready = player.Ready;
        NewPlayerItemScript.SetPlayerValues();

        NewPlayerItem.transform.SetParent(PlayerListViewContent.transform);
        NewPlayerItem.transform.localScale = Vector3.one;

        PlayerListItems.Add(NewPlayerItemScript);
    }

    public void UpdatePlayerItem()
    {
        foreach (PlayerObjectController player in Manager.GamePlayers)
        {
            foreach (PlayerListItem PlayerListItemScript in PlayerListItems)
            {
                if (PlayerListItemScript.ConnectionID == player.ConnectionID)
                {
                    PlayerListItemScript.PlayerName = player.PlayerName;
                    PlayerListItemScript.Ready = player.Ready;
                    PlayerListItemScript.SetPlayerValues();
                    if (player == LocalplayerController)
                    {
                        UpdateButton();
                    }
                }
            }
        }
        CheckIfAllReady();
    }

    public void RemovePlayerItem()
    {
        List<PlayerListItem> playerListItemToRemove = new List<PlayerListItem>();
        foreach (PlayerListItem playerlistItem in PlayerListItems)
        {
            if (!Manager.GamePlayers.Any(b => b.ConnectionID == playerlistItem.ConnectionID))
            {
                playerListItemToRemove.Add(playerlistItem);
            }
        }
        if (playerListItemToRemove.Count > 0)
        {
            foreach (PlayerListItem playerlistItemToRemove in playerListItemToRemove)
            {
                GameObject ObjectToRemove = playerlistItemToRemove.gameObject;
                PlayerListItems.Remove(playerlistItemToRemove);
                Destroy(ObjectToRemove);
            }
        }
    }

    public void StartGame(string SceneName)
    {
        if (LocalplayerController != null)
        {
            LocalplayerController.CanStartGame(SceneName);
        }
    }
}