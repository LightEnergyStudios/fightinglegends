using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FightingLegends;

namespace Prototype.NetworkLobby
{
    // Player entry in the lobby. Handle selecting number / name & getting ready for the game
    // Any LobbyHook can then pass those values to the game player prefab
    public class LobbyPlayer : NetworkLobbyPlayer
    {
		public Text playerNumberText;
        public Text playerUserId;
        public Button joinButton;
        public Button waitingPlayerButton;
        public Button removePlayerButton;

		public Text waitingText;
		public Text joinText;

        public GameObject localIcone;
        public GameObject remoteIcone;

		// OnPlayerNumber function will be invoked on clients when server changes the value of playerNumber
		[SyncVar(hook = "OnPlayerNumber")]
		public int playerNumber = 0;

		// OnPlayerName function will be invoked on clients when server changes the value of playerName
        [SyncVar(hook = "OnPlayerName")]
		public string playerName = "";

		static Color JoinColor = Color.white;
//        static Color NotReadyColor = new Color(34.0f / 255.0f, 44 / 255.0f, 55.0f / 255.0f, 1.0f);
		static Color ReadyColor = Color.cyan; // new Color(0.0f, 204.0f / 255.0f, 204.0f / 255.0f, 1.0f);
        static Color TransparentColor = new Color(0, 0, 0, 0);


		private void Start()
		{
			waitingText.text = FightManager.Translate("waitingForOpponent");
			joinText.text = FightManager.Translate("join");
		}

        public override void OnClientEnterLobby()
        {
            base.OnClientEnterLobby();

            if (LobbyManager.s_Singleton != null)
				LobbyManager.s_Singleton.OnPlayersNumberModified(1);

            LobbyPlayerList._instance.AddPlayer(this);
            LobbyPlayerList._instance.DisplayDirectServerWarning(isServer && LobbyManager.s_Singleton.matchMaker == null);

            if (isLocalPlayer)
            {
                SetupLocalPlayer();
            }
            else
            {
                SetupOtherPlayer();
            }
				
            //setup the player data on UI. The value are SyncVar so the player
            //will be created with the right value currently on server
			OnPlayerName(playerName);
			OnPlayerNumber(playerNumber);
        }

        public override void OnStartAuthority()
        {
            base.OnStartAuthority();

            //if we return from a game, color of text can still be the one for "Ready"
            joinButton.transform.GetChild(0).GetComponent<Text>().color = Color.white;

            SetupLocalPlayer();
        }

        void ChangeJoinButtonColor(Color c)
        {
            ColorBlock b = joinButton.colors;
            b.normalColor = c;
            b.pressedColor = c;
            b.highlightedColor = c;
            b.disabledColor = c;
            joinButton.colors = b;
        }

        void SetupOtherPlayer()
        {
            removePlayerButton.interactable = NetworkServer.active;

			ChangeJoinButtonColor(TransparentColor); // (NotReadyColor);

            joinButton.transform.GetChild(0).GetComponent<Text>().text = "...";
            joinButton.interactable = false;

            OnClientReady(false);
        }

        void SetupLocalPlayer()
        {
            remoteIcone.gameObject.SetActive(false);
            localIcone.gameObject.SetActive(true);

            CheckRemoveButton();

			if (playerName == "")
				CmdNameChanged(FightingLegends.FightManager.SavedGameStatus.UserId);

            ChangeJoinButtonColor(JoinColor);

            joinButton.transform.GetChild(0).GetComponent<Text>().text = "JOIN";
            joinButton.interactable = true;

            // use child count of player prefab for Player 1 / 2
			int playerNumber = LobbyPlayerList._instance.playerListContentTransform.childCount-1;
			CmdPlayerNumber(playerNumber);

			// set network fight static variables as early as possible (must be before server changes to playScene)
			FightingLegends.FightManager.IsNetworkFight = true;
			FightingLegends.SceneSettings.DirectToFighterSelect = true;		// for VS FighterSelect

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnReadyClicked);

            // when OnClientEnterLobby is called, the local PlayerController is not yet created, so we need to redo that here to disable
            // the add button if we reach maxLocalPlayer. We pass 0, as it was already counted on OnClientEnterLobby
            if (LobbyManager.s_Singleton != null)
				LobbyManager.s_Singleton.OnPlayersNumberModified(0);
        }

        //This enable/disable the remove button depending on if that is the only local player or not
        public void CheckRemoveButton()
        {
            if (!isLocalPlayer)
                return;

            int localPlayerCount = 0;
            foreach (PlayerController p in ClientScene.localPlayers)
                localPlayerCount += (p == null || p.playerControllerId == -1) ? 0 : 1;

            removePlayerButton.interactable = localPlayerCount > 1;
        }

        public override void OnClientReady(bool readyState)
        {
            if (readyState)
            {
                ChangeJoinButtonColor(TransparentColor);

                Text textComponent = joinButton.transform.GetChild(0).GetComponent<Text>();
				textComponent.text = FightManager.Translate("ready");
                textComponent.color = ReadyColor;
                joinButton.interactable = false;
            }
            else
            {
				ChangeJoinButtonColor(isLocalPlayer ? JoinColor : TransparentColor); // NotReadyColor);

                Text textComponent = joinButton.transform.GetChild(0).GetComponent<Text>();
				textComponent.text = isLocalPlayer ? FightManager.Translate("join") : "...";
                textComponent.color = Color.white;
                joinButton.interactable = isLocalPlayer;
            }
        }
			

        ///===== callbacks from sync var

	
        public void OnPlayerName(string newName)
        {
           	playerName = newName;
            playerUserId.text = playerName;
        }

		public void OnPlayerNumber(int playerNum)
		{
			playerNumber = playerNum;
			playerNumberText.text = playerNum.ToString();
		}


        //===== UI Handlers

        //Note that those handler use Command function, as we need to change the value on the server not locally
        //so that all client get the new value throught syncvar

        public void OnReadyClicked()
        {
            SendReadyToBeginMessage();
        }

        public void OnRemovePlayerClick()
        {
            if (isLocalPlayer)
                RemovePlayer();
            else if (isServer)
                LobbyManager.s_Singleton.KickPlayer(connectionToClient);
        }

        public void ToggleJoinButton(bool enabled)
        {
            joinButton.gameObject.SetActive(enabled); 		// join button
			waitingPlayerButton.gameObject.SetActive(!enabled);
        }

//        [ClientRpc]
//        public void RpcUpdateCountdown(int countdown)
//        {
//            LobbyManager.s_Singleton.countdownPanel.UIText.text = "Match Starting in " + countdown;
//            LobbyManager.s_Singleton.countdownPanel.gameObject.SetActive(countdown != 0);
//        }

        [ClientRpc]
        public void RpcUpdateRemoveButton()
        {
            CheckRemoveButton();
        }

		[ClientRpc]
		public void RpcRemovePlayer()
		{
			OnRemovePlayerClick();
		}
			

        //====== Server Commands
	
        [Command]
		public void CmdNameChanged(string name)
        {
            playerName = name;
        }

		[Command]
		public void CmdPlayerNumber(int playerNum)
		{
			playerNumber = playerNum;
		}

        // Cleanup thing when get destroy (which happen when client kick or disconnect)
        public void OnDestroy()
        {
            LobbyPlayerList._instance.RemovePlayer(this);
            if (LobbyManager.s_Singleton != null)
				LobbyManager.s_Singleton.OnPlayersNumberModified(-1);
        }
    }
}
