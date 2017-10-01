using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Prototype.NetworkLobby
{
    //Main menu, mainly only a bunch of callback called by the UI (setup throught the Inspector)
    public class LobbyMainMenu : MonoBehaviour 
    {
        public LobbyManager lobbyManager;

        public RectTransform lobbyServerList;
        public RectTransform lobbyPanel;

        public InputField ipInput;
        public InputField matchNameInput;

		public Button HostButton;	// LAN
		public Button JoinButton;	// LAN
		public Text JoinText;		// find -> join

        public void OnEnable()
        {
            lobbyManager.topPanel.ToggleVisibility(true);

            ipInput.onEndEdit.RemoveAllListeners();
            ipInput.onEndEdit.AddListener(onEndEditIP);

            matchNameInput.onEndEdit.RemoveAllListeners();
            matchNameInput.onEndEdit.AddListener(onEndEditGameName);

			matchNameInput.text = FightingLegends.FightManager.SavedGameStatus.UserId;

			lobbyManager.networkDiscovery.OnHostIP += HostIPReceived;

			ipInput.text = "";
			EnableHostButton();
			ConfigJoinButton();
        }

		public void OnDisable()
		{
			lobbyManager.networkDiscovery.OnHostIP -= HostIPReceived;
		}

		private void EnableHostButton()
		{
			HostButton.interactable = !lobbyManager.networkDiscovery.isClient;		// can't host if listening for host broadcast
		}

		private void ConfigJoinButton()
		{
			JoinText.text = string.IsNullOrEmpty(ipInput.text) ? "FIND A GAME" : "JOIN GAME";
//			JoinButton.interactable = !string.IsNullOrEmpty(ipInput.text);
		}

        public void OnClickHost()
        {
            lobbyManager.StartHost();
			lobbyManager.BroadcastHostIP();			// SM
        }

		private void HostIPReceived(string hostIP)
		{
			ipInput.text = hostIP;

			ConfigJoinButton();			// join game
		}

        public void OnClickJoin()
        {
			if (string.IsNullOrEmpty(ipInput.text)) 	// find a game
			{
				lobbyManager.DiscoverHostIP();		// listen as client to host broadcasts 
				EnableHostButton();					// can't host if listening for host broadcast
				return;
			}
			
            lobbyManager.ChangeTo(lobbyPanel);

            lobbyManager.networkAddress = ipInput.text;
            lobbyManager.StartClient();

            lobbyManager.backDelegate = lobbyManager.StopClientClbk;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Connecting...", lobbyManager.networkAddress);
        }
	

        public void OnClickDedicated()
        {
            lobbyManager.ChangeTo(null);
            lobbyManager.StartServer();

            lobbyManager.backDelegate = lobbyManager.StopServerClbk;

            lobbyManager.SetServerInfo("Dedicated Server", lobbyManager.networkAddress);
        }

        public void OnClickCreateMatchmakingGame()
        {
            lobbyManager.StartMatchMaker();
            lobbyManager.matchMaker.CreateMatch(
                matchNameInput.text,				// preset to UserId
                (uint)lobbyManager.maxPlayers,
                true,
				"", "", "", 0, 0,
				lobbyManager.OnMatchCreate);

            lobbyManager.backDelegate = lobbyManager.StopHost;
            lobbyManager._isMatchmaking = true;
            lobbyManager.DisplayIsConnecting();

            lobbyManager.SetServerInfo("Matchmaker Host", lobbyManager.matchHost);
        }

        public void OnClickOpenServerList()
        {
            lobbyManager.StartMatchMaker();
//			lobbyManager.backDelegate = lobbyManager.SimpleBackClbk;
            lobbyManager.ChangeTo(lobbyServerList);
        }

        void onEndEditIP(string text)
        {
			ConfigJoinButton();

            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickJoin();
            }
        }

        void onEndEditGameName(string text)
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                OnClickCreateMatchmakingGame();
            }
        }

    }
}
