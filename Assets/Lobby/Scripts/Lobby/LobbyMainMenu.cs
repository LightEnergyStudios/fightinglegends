using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using FightingLegends;
using UnityEngine.Networking;
using System;

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

		public Button StartServerButton;	// internet
		public Text StartServerText;	

		public Button FindServerButton;		// internet
		public Text FindServerText;	

		public Button HostButton;	// LAN
		public Text HostText;

		public Button JoinButton;	// LAN
		public Text JoinText;		// find -> join

		public Text scanningText;
		public Text fightCancelledText;

		private bool listeningforHost = false;		// so can cancel if no host found
//		private bool playersCancelled = false;		// on back from lobby player list
		private bool internetReachable = false;
		private bool localNetworkReachable = false;

        public void OnEnable()
        {
			internetReachable = (Application.internetReachability != NetworkReachability.NotReachable);
			localNetworkReachable = (Application.internetReachability == NetworkReachability.ReachableViaLocalAreaNetwork);

			StartServerButton.interactable = internetReachable;
			FindServerButton.interactable = internetReachable;
			HostButton.interactable = localNetworkReachable;
			JoinButton.interactable = localNetworkReachable;
			
            lobbyManager.topPanel.ToggleVisibility(true);

            ipInput.onEndEdit.RemoveAllListeners();
            ipInput.onEndEdit.AddListener(onEndEditIP);

            matchNameInput.onEndEdit.RemoveAllListeners();
            matchNameInput.onEndEdit.AddListener(onEndEditGameName);

			matchNameInput.text = FightingLegends.FightManager.SavedGameStatus.UserId;

			lobbyManager.networkDiscovery.OnHostIP += HostIPReceived;
			lobbyManager.OnQuitLobby += OnQuitLobby;

			ipInput.text = "";
			scanningText.text = FightManager.Translate("scanning") + "...";
			scanningText.gameObject.SetActive(false);
			fightCancelledText.text = FightManager.Translate("cancelled");
			fightCancelledText.gameObject.SetActive(false);

			lobbyManager.lobbyState = LobbyState.None;
			StopDiscovery();  		// stops listening and broadcasting
        }

		public void OnDisable()
		{
			lobbyManager.networkDiscovery.OnHostIP -= HostIPReceived;
			lobbyManager.OnQuitLobby -= OnQuitLobby;
		}

		private void EnableHostButton()
		{
			HostButton.interactable = !listeningforHost && !lobbyManager.networkDiscovery.isClient;		// can't host if listening for host broadcast
		}

		private void ConfigJoinButton()
		{
			if (listeningforHost)
				JoinText.text = FightManager.Translate("stopSearch");
			else
				JoinText.text = string.IsNullOrEmpty(ipInput.text) ? FightManager.Translate("findAGame") : FightManager.Translate("joinGame");
		}

        public void OnClickHost()
        {
			if (lobbyManager.lobbyState != LobbyState.None)
				return;
			
			fightCancelledText.gameObject.SetActive(false);

//			NetworkServer.Reset();

            lobbyManager.StartHost();
			lobbyManager.lobbyState = LobbyState.Host;

			lobbyManager.BroadcastHostIP();			// SM
        }

		private void HostIPReceived(string hostIP)
		{
//			LobbyManager.s_Singleton.networkAddress = hostIP;
//			NetworkManager.singleton.networkAddress = hostIP;
			ipInput.text = hostIP;

			StopDiscovery();  		// stops listening and broadcasting
			StartClient();						// join game immediately host ip received
		}


        public void OnClickJoin()
		{
			fightCancelledText.gameObject.SetActive(false);

			if (listeningforHost)						// stop 'search' (discovery)
			{
				StopDiscovery();  		// stops listening and broadcasting
				return;
			}

			if (string.IsNullOrEmpty(ipInput.text)) 	// find a game (start discovery)
			{
				StartDiscovery();
				return;
			}

			StartClient();
		}

		private void StartClient()
		{
			if (lobbyManager.lobbyState != LobbyState.None)
				return;
			
			Debug.Log("StartClient");
            lobbyManager.ChangeTo(lobbyPanel);

			lobbyManager.networkAddress = ipInput.text;	
            lobbyManager.StartClient();
			lobbyManager.lobbyState = LobbyState.Client;

//            lobbyManager.backDelegate = lobbyManager.StopClientClbk;
            lobbyManager.DisplayIsConnecting();
            lobbyManager.SetServerInfo("Connecting...", lobbyManager.networkAddress);
        }


		private void StartDiscovery()
		{
			lobbyManager.DiscoverHostIP();		// listen as client to host broadcasts 
			listeningforHost = true;
			scanningText.gameObject.SetActive(true);
			ConfigJoinButton();
			EnableHostButton();					// can't host if listening for host broadcast
		}
			
		private void StopDiscovery()
		{
			lobbyManager.StopDiscovery();  		// stops listening and broadcasting
//			StopListeningUI();	
//		}
//
//		private void StopListeningUI()
//		{
			listeningforHost = false;
			scanningText.gameObject.SetActive(false);
			ConfigJoinButton();
			EnableHostButton();	
		}

		private void OnQuitLobby()
		{
			Debug.Log("OnQuitLobby");
//			StopDiscovery();
		}
	
        public void OnClickDedicated()
        {
            lobbyManager.ChangeTo(null);
            lobbyManager.StartServer();

			lobbyManager.lobbyState = LobbyState.Server;
//            lobbyManager.backDelegate = lobbyManager.StopServerClbk;

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

//            lobbyManager.backDelegate = lobbyManager.StopHostClbk;
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
