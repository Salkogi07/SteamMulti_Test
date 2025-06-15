// --- START OF FILE HostLobbyController.cs ---

using UnityEngine;
using UnityEngine.UI;
using Steamworks;
using R3;

public class HostLobbyController : MonoBehaviour
{
    [SerializeField] private GameObject hostLobbyPanel;
    [SerializeField] private InputField lobbyNameInput;
    [SerializeField] private Toggle friendsOnlyToggle;
    
    [SerializeField] private GameObject mainMenuButtonsPanel; // Host, Lobbies 버튼이 있는 부모 객체// Host, Lobbies 버튼이 있는 부모 객체
    
    [SerializeField] private Button createLobbyButton;
    [SerializeField] private Button backButton;
    
    private void Start()
    {
        createLobbyButton.onClick.AddListener(OnCreateLobbyClicked);
        backButton.onClick.AddListener(OnBackButtonClicked);
    }

    public void ShowHostLobbyPanel()
    {
        //mainMenuButtons.SetActive(false);
        hostLobbyPanel.SetActive(true);
        // 기본 로비 이름 설정
        lobbyNameInput.text = SteamFriends.GetPersonaName() + "'s Lobby";
    }

    private void OnCreateLobbyClicked()
    {
        if (string.IsNullOrWhiteSpace(lobbyNameInput.text))
        {
            Debug.LogWarning("Lobby name cannot be empty.");
            return;
        }

        ELobbyType lobbyType = friendsOnlyToggle.isOn ? ELobbyType.k_ELobbyTypeFriendsOnly : ELobbyType.k_ELobbyTypePublic;
        
        SteamLobby.Instance.HostLobby(lobbyType, lobbyNameInput.text);
        hostLobbyPanel.SetActive(false);
    }

    private void OnBackButtonClicked()
    {
        hostLobbyPanel.SetActive(false);
        mainMenuButtonsPanel.SetActive(true);
    }
}