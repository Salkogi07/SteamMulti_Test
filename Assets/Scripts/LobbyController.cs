// --- START OF FILE LobbyController.cs ---

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Mirror;
using Steamworks;
using UnityEngine.UI;
using System.Linq;

public class LobbyController : MonoBehaviour
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

    // ✅ 추가: LeaveLobby 버튼 클릭 이벤트 핸들러
    public void OnClick_LeaveLobby()
    {
        SteamLobby.Instance.LeaveLobby();
    }

    public void ReadyPlayer()
    {
        if (LocalplayerController != null)
        {
            LocalplayerController.ChangeReady();
        }
    }

    public void UpdateButton()
    {
        if (LocalplayerController == null) return;
        ReadyButtonText.text = LocalplayerController.Ready ? "Unready" : "Ready";
    }

    public void CheckIfAllReady()
    {
        if (LocalplayerController == null)
        {
            if (StartGameButton != null) StartGameButton.interactable = false;
            return;
        }

        // ✅ 수정: 방장이 아니면 시작 버튼을 항상 비활성화 하도록 명확히 함
        if (LocalplayerController.PlayerIdNumber != 1)
        {
             if (StartGameButton != null) StartGameButton.interactable = false;
             return;
        }

        bool allReady = Manager.GamePlayers.All(player => player.Ready);

        if (StartGameButton != null)
        {
            StartGameButton.interactable = allReady;
        }
    }

    public void UpdateLobbyName()
    {
        if (Manager.GetComponent<SteamLobby>() == null || Manager.GetComponent<SteamLobby>().CurrentLobbyID == 0) return;

        CurrentLobbyID = Manager.GetComponent<SteamLobby>().CurrentLobbyID;
        LobbyNameText.text = SteamMatchmaking.GetLobbyData(new CSteamID(CurrentLobbyID), "name");
        if (LeaveLobbyButton != null) LeaveLobbyButton.gameObject.SetActive(true);
    }
    
    // ✅ 추가: 로비에서 나갈 때 UI 초기화
    public void ClearLobby()
    {
        PlayerItemCreated = false;
        foreach (var item in PlayerListItems)
        {
            Destroy(item.gameObject);
        }
        PlayerListItems.Clear();
        if (StartGameButton != null) StartGameButton.interactable = false;
        if (LeaveLobbyButton != null) LeaveLobbyButton.gameObject.SetActive(false);
        LocalplayerController = null;
        LocalPlayerObject = null;
    }


    public void UpdatePlayerList()
    {
        if (!PlayerItemCreated) { CreateHostPlayerItem(); }
        if (PlayerListItems.Count < Manager.GamePlayers.Count) { CreateClientPlayerItem(); }
        if (PlayerListItems.Count > Manager.GamePlayers.Count) { RemovePlayerItem(); }
        if (PlayerListItems.Count == Manager.GamePlayers.Count) { UpdatePlayerItem(); }
    }
    
    public void SetLocalPlayerController(PlayerObjectController controller)
    {
        LocalplayerController = controller;
        if (LocalplayerController != null)
        {
            LocalPlayerObject = LocalplayerController.gameObject;
            UpdateButton(); 
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
        // ✅ 수정: 방장만 게임 시작 가능
        if (LocalplayerController != null && LocalplayerController.PlayerIdNumber == 1)
        {
            LocalplayerController.CanStartGame(SceneName);
        }
    }
}