
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
		public Text IPAddress;				// (own (host) IP - display only
		public AudioClip EntrySound;
		public Image curtain;				// blackout

		public GameObject fightManagerPrefab;		// NetworkFightManager
		public LobbyDiscovery networkDiscovery;

		[Header("Unity UI Lobby")]
		[Tooltip("Time in second between all players ready & match start")]
		public float prematchCountdown = 5.0f;

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

		private Opening opening;
		private string localUserId = "";

		private NetworkPlayer networkPlayer;				// for ip address
		private const float curtainfadeTime = 0.75f;

		private NetworkFightManager networkFightManager;

		private const int LobbyTimeout = 30;			// once NetworkFighters spawned - waiting to select fighters and location
		private IEnumerator expiryCountdownCoroutine = null;


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

			curtain.gameObject.SetActive(false);
			curtain.color = Color.clear;

//			SceneManager.activeSceneChanged += OnActiveSceneChanged;					// on 100% load

//			var openingObject = GameObject.Find("Opening");
//			if (openingObject != null)
//				opening = openingObject.GetComponent<Opening>();
			
//			Opening.OnPreloadComplete += PreloadCombatComplete;
		}

		public void OnDestroy()
		{
//			networkDiscovery.StopBroadcast();
//			SceneManager.activeSceneChanged -= OnActiveSceneChanged;					// on 100% load
		}
			

		#region UI

		public void ShowLobbyUI()
		{
			var openingObject = GameObject.Find("Opening");
			if (openingObject != null)
				opening = openingObject.GetComponent<Opening>();
			
			if (lobbyUIPanel != null)
			{
				lobbyUIPanel.gameObject.SetActive(true);
				GetComponent<Animator>().SetTrigger("LobbyEntry");
			}

			backDelegate = QuitLobby;

			localUserId = FightManager.SavedGameStatus.UserId;
			if (userID != null)
				userID.text = localUserId;

			networkPlayer = Network.player;
			IPAddress.text = networkPlayer.ipAddress;		// own (host IP address)

			curtain.gameObject.SetActive(false);
			curtain.color = Color.clear;

			ChangeTo(mainMenuPanel);

//			// start reloading combat scene asap
//			if (opening != null)
//				opening.PreloadCombat();	
		}

		public void HideLobbyUI(bool fadeToBlack)
		{
			if (networkDiscovery.running)
				networkDiscovery.StopBroadcast();

			StartCoroutine(FadeLobbyUI(fadeToBlack));

//			Network.Disconnect();		// TODO: ok?
		}

		private IEnumerator FadeLobbyUI(bool fadeToBlack)
		{
			if (fadeToBlack)
				yield return StartCoroutine(FadeToBlack());
			
			if (lobbyUIPanel != null)
				lobbyUIPanel.gameObject.SetActive(false);

			ChangeTo(mainMenuPanel);

//			if (fade)
//				yield return StartCoroutine(CurtainUp());
			
			yield return null;
		}

		// back button
		private void QuitLobby()
		{
			StopClientClbk();
			StopHostClbk();
			StopServerClbk();

			HideLobbyUI(true);

			SceneLoader.LoadScene(SceneLoader.CombatScene);
			//			if (opening != null)
			//				StartCoroutine(opening.ActivateWhenPreloaded());

//			StartCoroutine(CurtainUp());

			FightManager.IsNetworkFight = false;

			SceneSettings.ShowLobbyUI = false;
			SceneSettings.DirectToFighterSelect = false;

			NetworkServer.Shutdown();		// TODO: ok?
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

		private void StartTimeoutCountdown()
		{
			if (expiryCountdownCoroutine != null)
				StopCoroutine(expiryCountdownCoroutine);

			expiryCountdownCoroutine = StartExpiryCountdown();
			StartCoroutine(expiryCountdownCoroutine);
		}

		private IEnumerator StartExpiryCountdown()
		{
			for (int i = LobbyTimeout; i >= 0; i--)
			{
				Debug.Log("StartLobbyCountdown: " + i);
				yield return new WaitForSeconds(1.0f);
			}

			NetworkServer.Shutdown();
			yield return null;
		}

//		private void OnActiveSceneChanged(Scene oldScene, Scene newScene)
//		{
//			Debug.Log("OnActiveSceneChanged: " + oldScene.name + " --> " + newScene.name);
//
//			if (newScene.name == "Combat")
//				StartCoroutine(CurtainUp());
//		}

		public void EntryComplete()
		{
			if (EntrySound != null)
				AudioSource.PlayClipAtPoint(EntrySound, Vector3.zero, FightManager.SFXVolume);
		}
			
		private IEnumerator FadeToBlack()
		{
			if (curtain.gameObject.activeSelf)
				yield break;

			curtain.color = Color.clear;
			curtain.gameObject.SetActive(true);
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / curtainfadeTime); 

				curtain.color = Color.Lerp(Color.clear, Color.black, t);
				yield return null;
			}

			yield return null;
		}

		private IEnumerator CurtainUp()
		{
			if (!curtain.gameObject.activeSelf)
				yield break;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / curtainfadeTime); 

				curtain.color = Color.Lerp(Color.black, Color.clear, t);
				yield return null;
			}

			curtain.gameObject.SetActive(false);
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

//            if (currentPanel != mainMenuPanel)
//            {
//                backButton.gameObject.SetActive(true);
//            }
//            else
			if (currentPanel == mainMenuPanel)
            {
                SetServerInfo("Offline", "None");
                _isMatchmaking = false;
            }
        }

		public void DisplayIsConnecting()
		{
			var _this = this;
			infoPanel.Display("Connecting...", "Cancel", () => { _this.backDelegate(); });
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

		public void RemovePlayer(LobbyPlayer player)
		{
			player.RemovePlayer();
		}

		public void SimpleBackClbk()
		{
			ChangeTo(mainMenuPanel);
		}

		public void StopHostClbk()
		{
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
		}

		public void StopClientClbk()
		{
			StopClient();

			if (_isMatchmaking)
			{
				StopMatchMaker();
			}

			ChangeTo(mainMenuPanel);
		}

		public void StopServerClbk()
		{
			StopServer();
			ChangeTo(mainMenuPanel);
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

			StartTimeoutCountdown();		// TODO: here??
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
			{
				if (expiryCountdownCoroutine != null)
					StopCoroutine(expiryCountdownCoroutine);
				
				ServerChangeScene(playScene);
//				StartCoroutine(ServerCountdownCoroutine());
			}
		}


		// --- Countdown management

//		public IEnumerator ServerCountdownCoroutine()
//		{
//			float remainingTime = prematchCountdown;
//			int floorTime = Mathf.FloorToInt(remainingTime);
//
//			while (remainingTime > 0)
//			{
//				yield return null;
//
//				remainingTime -= Time.deltaTime;
//				int newFloorTime = Mathf.FloorToInt(remainingTime);
//
//				if (newFloorTime != floorTime)
//				{//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
//					floorTime = newFloorTime;
//
//					for (int i = 0; i < lobbySlots.Length; ++i)
//					{
//						if (lobbySlots[i] != null)
//						{//there is maxPlayer slots, so some could be == null, need to test it before accessing!
//							(lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
//						}
//					}
//				}
//			}
//
//			for (int i = 0; i < lobbySlots.Length; ++i)
//			{
//				if (lobbySlots[i] != null)
//				{
//					(lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
//				}
//			}
//
//			ServerChangeScene(playScene);
//		}

		// ----------------- Client callbacks ------------------

		// called when a scene has completed loading, when the scene load was initiated by the server
		public override void OnClientSceneChanged(NetworkConnection conn)
		{
			base.OnClientSceneChanged(conn);		// calls NetworkFighter.OnStartLocalPlayer!!

			HideLobbyUI(false);
		}


		public override void OnClientConnect(NetworkConnection conn)
		{
			base.OnClientConnect(conn);

			infoPanel.gameObject.SetActive(false);

			conn.RegisterHandler(MsgKicked, KickedMessageHandler);

			StartTimeoutCountdown();		// TODO: here???

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
		}

		public override void OnClientError(NetworkConnection conn, int errorCode)
		{
			ChangeTo(mainMenuPanel);
			infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
		}
	}
}
	
