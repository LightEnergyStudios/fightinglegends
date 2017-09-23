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

        [Header("Unity UI Lobby")]
        [Tooltip("Time in second between all players ready & match start")]
        public float prematchCountdown = 5.0f;

		[Header("Custom UI")]
		public Image lobbyUIPanel;			// entire lobby UI
		public Text userID;					// used for matchmaker game name 
		public AudioClip EntrySound;

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

        //Client numPlayers from NetworkManager is always 0, so we count (throught connect/destroy in LobbyPlayer) the number
        //of players, so that even client know how many player there is.
        [HideInInspector]
        public int _playerNumber = 0;

		private string localUserId = ""; //FightManager.SavedGameStatus.UserId; // == "" ? "Dudos!" : FightManager.SavedGameStatus.UserId;// TODO: remove!

        //used to disconnect a client properly when exiting the matchmaker
        [HideInInspector]
        public bool _isMatchmaking = false;

        protected bool _disconnectServer = false;
		protected ulong _currentMatchID;
        protected LobbyHook _lobbyHooks;

		private Opening opening;

//		public delegate void ExitLobbyDelegate();
//		public static ExitLobbyDelegate OnExitLobby;


        void Start()
        {
            s_Singleton = this;
            _lobbyHooks = GetComponent<Prototype.NetworkLobby.LobbyHook>();
            currentPanel = mainMenuPanel;

//            backButton.gameObject.SetActive(false);
            GetComponent<Canvas>().enabled = true;

            DontDestroyOnLoad(gameObject);

            SetServerInfo("Offline", "None");

			backDelegate = ExitLobby;
        }

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

			backDelegate = ExitLobby;

			localUserId = FightManager.SavedGameStatus.UserId;
			userID.text = localUserId;

			ChangeTo(mainMenuPanel);

			Network.Disconnect();		// TODO: ok?

//			// start reloading combat scene asap
//			if (opening != null)
//				opening.PreloadCombat();	
		}

		public void HideLobbyUI()
		{
			ChangeTo(mainMenuPanel);

			if (lobbyUIPanel != null)
				lobbyUIPanel.gameObject.SetActive(false);
		}

		// back button
		private void ExitLobby()
		{
			StopClientClbk();
			StopHostClbk();
			StopServerClbk();

			HideLobbyUI();

			FightManager.IsNetworkFight = false;

			SceneSettings.ShowLobbyUI = false;
			SceneSettings.DirectToFighterSelect = false;

//			if (opening != null)
//			{
//				Opening.OnPreloadComplete += PreloadCombatComplete;
//				opening.PreloadCombat();
//			}

//			ServerChangeScene(playScene);			// sets clients to not ready

			SceneLoader.LoadScene(SceneLoader.CombatScene);
//			if (opening != null)
//				StartCoroutine(opening.ActivateWhenPreloaded());
			
//			if (OnExitLobby != null)
//				OnExitLobby();

//			Network.Disconnect();		// TODO: ok?
		}

		public void EntryComplete()
		{
			if (EntrySound != null)
				AudioSource.PlayClipAtPoint(EntrySound, Vector3.zero, FightManager.SFXVolume);
		}

//		private void PreloadCombatComplete(string scene)
//		{
//			HideLobbyUI();
//			opening.ActivatePreloadedScene();
//		}
			
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
							backDelegate = StopHostClbk;		// SM ExitLobby
                        }
                        else
                        {
							backDelegate = StopClientClbk;		// SM ExitLobby
                        }
                    }
                    else
                    {
                        if (conn.playerControllers[0].unetView.isClient)
                        {
							backDelegate = StopHostClbk;		// SM ExitLobby
                        }
                        else
                        {
							backDelegate = StopClientClbk;		// SM ExitLobby
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

				backDelegate = ExitLobby;
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
//			topPanel.isInGame = false;
        }
			

        // ----------------- Server management

        public void AddLocalPlayer()
        {
//            TryToAddPlayer();
        }

        public void RemovePlayer(LobbyPlayer player)
        {
            player.RemovePlayer();
        }

//        public void SimpleBackClbk()
//        {
//            ChangeTo(mainMenuPanel);
//        }
			
     
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

        //we want to disable the button JOIN if we don't have enough player
        //But OnLobbyClientConnect isn't called on hosting player. So we override the lobbyPlayer creation
        public override GameObject OnLobbyServerCreateLobbyPlayer(NetworkConnection conn, short playerControllerId)
        {
            GameObject obj = Instantiate(lobbyPlayerPrefab.gameObject) as GameObject;

            LobbyPlayer newPlayer = obj.GetComponent<LobbyPlayer>();
            newPlayer.ToggleJoinButton(numPlayers + 1 >= minPlayers);

//			newPlayer.playerName = localUserId;

//			Debug.Log("OnLobbyServerCreateLobbyPlayer: " + newPlayer.UserId);

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

		// called on the server when a client disconnects
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

//			// TODO: SM
//			if (numPlayers == 0)
//				conn.Disconnect();
        }

		// called when switching from lobby (opening) scene to play (combat) scene
		// replaces lobby player with game player
        public override bool OnLobbyServerSceneLoadedForPlayer(GameObject lobbyPlayer, GameObject gamePlayer)
        {
            //This hook allows you to apply state data from the lobby-player to the game-player
            //just subclass "LobbyHook" and add it to the lobby object.

            if (_lobbyHooks)
                _lobbyHooks.OnLobbyServerSceneLoadedForPlayer(this, lobbyPlayer, gamePlayer);

            return true;
        }

        // --- Countdown management

		// called on the server when all the players in the lobby are ready
        public override void OnLobbyServerPlayersReady()
        {
			bool allready = true;
			for (int i = 0; i < lobbySlots.Length; ++i)
			{
				if (lobbySlots[i] != null)
					allready &= lobbySlots[i].readyToBegin;
			}

			if (allready)
			{
//				Debug.Log("OnLobbyServerPlayersReady");
				ServerChangeScene(playScene);		// replaces lobby player with game player via hook (NetworkFighter)
//				StartCoroutine(ServerCountdownCoroutine());
			}
        }
			
		private void CombatFighterSelect()
		{
//			HideLobbyUI();
			ServerChangeScene(playScene);		// replaces lobby player with game player via hook (NetworkFighter)
		}

//        public IEnumerator ServerCountdownCoroutine()
//        {
//            float remainingTime = prematchCountdown;
//            int floorTime = Mathf.FloorToInt(remainingTime);
//
//            while (remainingTime > 0)
//            {
//                yield return null;
//
//                remainingTime -= Time.deltaTime;
//                int newFloorTime = Mathf.FloorToInt(remainingTime);
//
//                if (newFloorTime != floorTime)
//                {//to avoid flooding the network of message, we only send a notice to client when the number of plain seconds change.
//                    floorTime = newFloorTime;
//
//                    for (int i = 0; i < lobbySlots.Length; ++i)
//                    {
//                        if (lobbySlots[i] != null)
//                        {//there is maxPlayer slots, so some could be == null, need to test it before accessing!
//                            (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(floorTime);
//                        }
//                    }
//                }
//            }
//
//            for (int i = 0; i < lobbySlots.Length; ++i)
//            {
//                if (lobbySlots[i] != null)
//                {
//                    (lobbySlots[i] as LobbyPlayer).RpcUpdateCountdown(0);
//                }
//            }
//
//			HideLobbyUI();
//            ServerChangeScene(playScene);
//        }

        // ----------------- Client callbacks ------------------

		// called when a scene has completed loading, when the scene load was initiated by the server
		public override void OnClientSceneChanged(NetworkConnection conn)
		{
			HideLobbyUI();
		}
			
        public override void OnClientConnect(NetworkConnection conn)
        {
            base.OnClientConnect(conn);

//			ClientScene.Ready(conn);		// TODO: SM ?

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
        }

        public override void OnClientError(NetworkConnection conn, int errorCode)
        {
            ChangeTo(mainMenuPanel);
            infoPanel.Display("Cient error : " + (errorCode == 6 ? "timeout" : errorCode.ToString()), "Close", null);
        }
    }
}
