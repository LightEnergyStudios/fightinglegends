
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using UnityEngine.Networking.Types;
using UnityEngine.Networking.Match;
using System.Collections;
using FightingLegends;


namespace Prototype.NetworkLobby
{
	public class LobbyManager : NetworkLobbyManager 
	{
		static short MsgKicked = MsgType.Highest + 1;

		static public LobbyManager s_Singleton;

		[Header("Custom UI")]
		public Image lobbyUIPanel;			// entire lobby UI
		public Text userID;					// used for matchmaker game name 
		public Text IPAddress;				// own (host) IP - display only
		public AudioClip EntrySound;
		public AudioClip ButtonEntrySound;
		public Image blackOut;				// fade to black

		public GameObject fightManagerPrefab;		// NetworkFightManager
		public LobbyDiscovery networkDiscovery;

//		[Header("Unity UI Lobby")]
//		[Tooltip("Time in second between all players ready & match start")]
//		public float prematchCountdown = 5.0f;

		[Space]
		[Header("UI Reference")]
		public LobbyTopPanel topPanel;

		public RectTransform mainMenuPanel;
		public RectTransform lobbyPanel;

		public LobbyInfoPanel infoPanel;
		public LobbyCountdownPanel countdownPanel;
		public GameObject addPlayerButton;

		protected RectTransform currentPanel;

		public Button backButton;

		public Text statusInfo;
		public Text hostInfo;

		public Text wiFiConnect;
		public Text hostAGame;
		public Text findAGame;

		// Client numPlayers from NetworkManager is always 0, so we count (through connect/destroy in LobbyPlayer)
		// the number of players, so that even client knows how many players there are
		[HideInInspector]
		public int _playerNumber = 0;

		//used to disconnect a client properly when exiting the matchmaker
		[HideInInspector]
		public bool _isMatchmaking = false;

		protected bool _disconnectServer = false;

		protected ulong _currentMatchID;

		protected LobbyHook _lobbyHooks;

		private string localUserId = "";

		private NetworkPlayer networkPlayer;				// for ip address
		private const float fadeTime = 0.75f;
//		private const float fadePause = 0.25f;

//		private LobbyPlayer lobbyPlayer = null;
		public bool playersCancelled = false;				// on back from lobby player list

		private NetworkFightManager networkFightManager;

//		public delegate void LobbyBackDelegate();
//		public LobbyBackDelegate OnLobbyBack;


		void Start()
		{
			s_Singleton = this;
			_lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
			currentPanel = mainMenuPanel;

			backButton.gameObject.SetActive(true);
			backDelegate = QuitLobby;

			GetComponent<Canvas>().enabled = true;

			DontDestroyOnLoad(gameObject);

			SetServerInfo("Offline", "None");

			blackOut.gameObject.SetActive(false);
			blackOut.color = Color.clear;

			wiFiConnect.text = FightManager.Translate("wiFiConnect", false, true);
			hostAGame.text = FightManager.Translate("hostAGame");
			findAGame.text = FightManager.Translate("findAGame");
		}

		public void OnDestroy()
		{
			StopBroadcast();
		}
			

		#region UI

		public void ShowLobbyUI()
		{
			if (lobbyUIPanel != null)
			{
				lobbyUIPanel.gameObject.SetActive(true);
				GetComponent<Animator>().SetTrigger("LobbyEntry");
			}

			backDelegate = QuitLobby;
			playersCancelled = false;

			localUserId = FightManager.SavedGameStatus.UserId;
			if (userID != null)
				userID.text = localUserId;

			networkPlayer = Network.player;
			IPAddress.text = networkPlayer.ipAddress;		// own (host IP address)

			blackOut.gameObject.SetActive(false);
			blackOut.color = Color.clear;

			ChangeTo(mainMenuPanel);
		}

		public void HideLobbyUI()
		{
			StartCoroutine(FadeLobbyUI(false));
			StopBroadcast();		// stops listening and broadcasting
			StopAll();
		}

		private IEnumerator FadeLobbyUI(bool fadeToBlack)
		{
			if (fadeToBlack)
				yield return StartCoroutine(FadeToBlack());
			
			if (lobbyUIPanel != null)
				lobbyUIPanel.gameObject.SetActive(false);

			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;
			yield return null;
		}

		// back button
		private void QuitLobby()
		{
			StartCoroutine(FadeQuitLobby());
		}

		private IEnumerator FadeQuitLobby()
		{
			yield return StartCoroutine(FadeLobbyUI(true));
			HideLobbyUI();

			SceneLoader.LoadScene(SceneLoader.CombatScene);

			FightManager.IsNetworkFight = false;
			SceneSettings.ShowLobbyUI = false;
			SceneSettings.DirectToFighterSelect = false;

//			yield return new WaitForSeconds(fadePause);
			StartCoroutine(ClearFadeToBlack());
		}

		// broadcast for client to discover
		public void BroadcastHostIP()
		{
			networkDiscovery.Initialize();
			networkDiscovery.StartAsServer();
		}

		// start listening for host IP broadcast
		public void DiscoverHostIP()
		{
			networkDiscovery.Initialize();
			networkDiscovery.StartAsClient();
		}

		public void StopBroadcast()
		{
			if (networkDiscovery.running)
				networkDiscovery.StopBroadcast();		// stops listening and broadcasting
		}

		public void EntryComplete()
		{
			if (EntrySound != null)
				AudioSource.PlayClipAtPoint(EntrySound, Vector3.zero, FightManager.SFXVolume);
		}

		public void ButtonEntryComplete()
		{
			if (ButtonEntrySound != null)
				AudioSource.PlayClipAtPoint(ButtonEntrySound, Vector3.zero, FightManager.SFXVolume);
		}
			
		private IEnumerator FadeToBlack()
		{
			blackOut.color = Color.clear;
			blackOut.gameObject.SetActive(true);
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				blackOut.color = Color.Lerp(Color.clear, Color.black, t);
				yield return null;
			}
		}

		private IEnumerator ClearFadeToBlack()
		{
			blackOut.color = Color.black;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				blackOut.color = Color.Lerp(Color.black, Color.clear, t);
				yield return null;
			}

			blackOut.gameObject.SetActive(false);
			yield return null;
		}

		#endregion   // UI


		public override void OnLobbyClientSceneChanged(NetworkConnection conn)
		{
			if (SceneManager.GetSceneAt(0).name == lobbyScene)
			{
				if (topPanel.isInGame)
				{
					ChangeTo(lobbyPanel);
					if (_isMatchmaking)
					{
						if (conn.playerControllers[0].unetView.isServer)
						{
							backDelegate = StopHostClbk;
						}
						else
						{
							backDelegate = StopClientClbk;
						}
					}
					else
					{
						if (conn.playerControllers[0].unetView.isClient)
						{
							backDelegate = StopHostClbk;
						}
						else
						{
							backDelegate = StopClientClbk;
						}
					}
				}
				else
				{
					ChangeTo(mainMenuPanel);
					backDelegate = QuitLobby;
				}

				topPanel.ToggleVisibility(true);
				topPanel.isInGame = false;
			}
			else
			{
				ChangeTo(null);

				Destroy(GameObject.Find("MainMenuUI(Clone)"));

				backDelegate = QuitLobby;
				topPanel.isInGame = true;
				topPanel.ToggleVisibility(false);
			}
		}


        public void ChangeTo(RectTransform newPanel)
        {
            if (currentPanel != null)
            {
                currentPanel.gameObject.SetActive(false);
            }

            if (newPanel != null)
            {
                newPanel.gameObject.SetActive(true);
            }

            currentPanel = newPanel;

			if (currentPanel == mainMenuPanel)
            {
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
        }

		public void DisplayIsConnecting()
		{
			var _this = this;
			infoPanel.Display(FightManager.Translate("connecting") + "...", FightManager.Translate("cancel"), () => { _this.backDelegate(); });
		}

		public void SetServerInfo(string status, string host)
		{
			statusInfo.text = status;
			hostInfo.text = host;
		}


		public delegate void BackButtonDelegate();
		public BackButtonDelegate backDelegate;
		public void GoBackButton()
		{
			backDelegate();
			topPanel.isInGame = false;
		}

		// ----------------- Server management

		public void AddLocalPlayer()
		{
			TryToAddPlayer();
		}

//		private void RemovePlayer() // LobbyPlayer player)
//		{
//			if (lobbyPlayer != null)
//			{
////				lobbyPlayer.RemovePlayer();
//				lobbyPlayer.OnRemovePlayerClick();
//				lobbyPlayer = null;
//			}
//		}

		public void RemoveLocalPlayer()
		{
			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				LobbyPlayer player = lobbySlots[i] as LobbyPlayer;

				if (player.isLocalPlayer)
					player.RemovePlayer();
//					player.OnRemovePlayerClick();
				
//				player.RpcRemovePlayer();
			}
		}

		public void RemoveAllPlayers()
		{
			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				LobbyPlayer player = lobbySlots[i] as LobbyPlayer;
				player.OnRemovePlayerClick();
			}
		}

		public void SimpleBackClbk()
		{
			Debug.Log("SimpleBackClbk");
//			playersCancelled = true;
//
//			if (networkDiscovery.running)
//				networkDiscovery.StopBroadcast();		// stops listening and broadcasting
//
//			StopAll();				// TODO: ok?
//			RemoveAllPlayers();		// TODO: ok?
//			RemoveLocalPlayer();			// TODO: ok?

			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;

//			if (OnLobbyBack != null)
//				OnLobbyBack();
		}

		private void StopAll()
		{
			StopClientClbk();
			StopHostClbk();
//			StopServerClbk();
		}

		public void StopHostClbk()
		{
			Debug.Log("StopHostClbk");
			if (_isMatchmaking)
			{
				matchMaker.DestroyMatch((NetworkID)_currentMatchID, 0, OnDestroyMatch);
				_disconnectServer = true;
			}
			else
			{
				StopHost();
			}
				
			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;
		}

		public void StopClientClbk()
		{
			Debug.Log("StopClientClbk");
			StopClient();

			if (_isMatchmaking)
			{
				StopMatchMaker();
			}
				
			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;
		}

		public void StopServerClbk()
		{
			Debug.Log("StopServerClbk");
			StopServer();
			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;
		}

		class KickMsg : MessageBase { }
		public void KickPlayer(NetworkConnection conn)
		{
			conn.Send(MsgKicked, new KickMsg());
		}
	
		public void KickedMessageHandler(NetworkMessage netMsg)
		{
			infoPanel.Display("Kicked by Server", "Close", null);
			netMsg.conn.Disconnect();
		}

		//===================

		public override void OnStartHost()
		{
			base.OnStartHost();

			ChangeTo(lobbyPanel);
			backDelegate = StopHostClbk;
			SetServerInfo("Hosting", networkAddress);
		}

		public override void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
		{
			base.OnMatchCreate(success, extendedInfo, matchInfo);
			_currentMatchID = (System.UInt64)matchInfo.networkId;
		}

		public override void OnDestroyMatch(bool success, string extendedInfo)
		{
			base.OnDestroyMatch(success, extendedInfo);
			if (_disconnectServer)
			{
				StopMatchMaker();
				StopHost();
			}
		}

		//allow to handle the (+) button to add/remove player
		public void OnPlayersNumberModified(int count)
		{
			_playerNumber += count;

			int localPlayerCount = 0;
			foreach (PlayerController p in ClientScene.localPlayers)
				localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

			addPlayerButton.SetActive(localPlayerCount < maxPlayersPerConnection && _playerNumber < maxPlayers);
		}

		// ----------------- Server callbacks ------------------

//		public override void OnServerAddPlayer(NetworkConnection conn, short playerControllerId)
//		{
//			GameObject player = (GameObject)Instantiate(playerPrefab, Vector3.zero, Quaternion.identity);
//			NetworkServer.AddPlayerForConnection(conn, player, playerControllerId);
//
//			NetworkFighter newPlayer = player.GetComponent<NetworkFighter>();
//			Debug.Log("OnServerAddPlayer: PlayerNumber = " + newPlayer.PlayerNumber);
//		}

		//we want to disable the button JOIN if we don't have enough player
		//But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
		public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
		{
			GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

			LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
			newPlayer = obj.GetComponent<LobbyPlayer>();
			newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);

			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

				if (p != null)
				{
					p.RpcUpdateRemoveButton();
					p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
				}
			}

			return obj;
		}

		public override void OnLobbyServerPlayerRemoved(NetworkConnection conn, short playerControllerId)
		{
			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

				if (p != null)
				{
					p.RpcUpdateRemoveButton();
					p.ToggleJoinButton(numPlayers + 1 >= minPlayers);
				}
			}

			SimpleBackClbk();		// TODO: SM ok?
		}

		public override void OnLobbyServerDisconnect(NetworkConnection conn)
		{
			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				LobbyPlayer p = lobbySlots[i] as LobbyPlayer;

				if (p != null)
				{
					p.RpcUpdateRemoveButton();
					p.ToggleJoinButton(numPlayers >= minPlayers);
				}
			}

			SimpleBackClbk();		// TODO: SM ok?
		}


		private void SpawnNetworkFightManager()
		{
			GameObject managerObject = Instantiate(fightManagerPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			NetworkServer.Spawn(managerObject);

			networkFightManager = managerObject.GetComponent<NetworkFightManager>();
		}
			
		public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
		{
			//This hook allows you to apply state data from the lobby-player to the game-player
			//just subclass "LobbyHook" and add it to the lobby object.

			if (_lobbyHooks)
				_lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

			if (networkFightManager == null)
				SpawnNetworkFightManager();			// server only - to sync start fight / ready to fight / quit fight etc.

			NetworkFighter localPlayer = gamePlayer.GetComponent<NetworkFighter>();
			localPlayer.SetFightManager(networkFightManager);

			networkFightManager.SetPlayer(localPlayer);
			return true;
		}


		public override void OnLobbyServerPlayersReady()
		{
			bool allready = true;
			for(int i = 0; i < lobbySlots.Length; ++i)
			{
				if(lobbySlots[i] != null)
					allready &= lobbySlots[i].readyToBegin;
			}

			if (allready)
				ServerChangeScene(playScene);
		}


		// ----------------- Client callbacks ------------------

//		// called when a scene has completed loading, when the scene load was initiated by the server
//		public override void OnClientSceneChanged(NetworkConnection conn)
//		{
//			base.OnClientSceneChanged(conn);		// calls NetworkFighter.OnStartLocalPlayer!!
//
////			HideLobbyUI();
//		}
//

		// called when a scene has completed loading, when the scene load was initiated by the server
		public override void OnClientSceneChanged(NetworkConnection conn)
		{
			string loadedSceneName = SceneManager.GetSceneAt(0).name;
			if (loadedSceneName == lobbyScene)
			{
				if (client.isConnected)
					CallOnClientEnterLobby();
			}
			else
			{
				CallOnClientExitLobby();
			}

			/// This call is commented out since it causes a unet "A connection has already been set as ready. There can only be one." error.
			/// More info: http://answers.unity3d.com/questions/991552/unet-a-connection-has-already-been-set-as-ready-th.html
			//base.OnClientSceneChanged(conn);
			OnLobbyClientSceneChanged(conn);

			HideLobbyUI();
		}
			
		void CallOnClientEnterLobby()
		{
			OnLobbyClientEnter();
			foreach (var player in lobbySlots)
			{
				if (player == null)
					continue;

				player.readyToBegin = false;
				player.OnClientEnterLobby();
			}
		}

		void CallOnClientExitLobby()
		{
			OnLobbyClientExit();
			foreach (var player in lobbySlots)
			{
				if (player == null)
					continue;

				player.OnClientExitLobby();
			}
		}
			

		public override void OnClientConnect(NetworkConnection conn)
		{
			base.OnClientConnect(conn);

			infoPanel.gameObject.SetActive(false);

			conn.RegisterHandler(MsgKicked, KickedMessageHandler);

			if (!NetworkServer.active)
			{
				//only to do on pure client (not self hosting client)
				ChangeTo(lobbyPanel);
				backDelegate = StopClientClbk;
				SetServerInfo("Client", networkAddress);
			}
		}
			
		public override void OnClientDisconnect(NetworkConnection conn)
		{
			base.OnClientDisconnect(conn);
			ChangeTo(mainMenuPanel);
			backDelegate = QuitLobby;
		}

		public override void OnClientError(NetworkConnection conn, int errorCode)
		{
			ChangeTo(mainMenuPanel);
			infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
		}
	}
}
	
