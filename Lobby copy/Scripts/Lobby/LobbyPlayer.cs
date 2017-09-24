using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Prototype.NetworkLobby
{
    //Player entry in the lobby. Handle selecting color/setting name & getting ready for the game
    //Any LobbyHook can then grab it and pass those value to the game player prefab (see the Pong Example in the Samples Scenes)
    public class LobbyPlayer : NetworkLobbyPlayer
    {
//        static Color[] Colors = new Color[] { Color.magenta, Color.red, Color.cyan, Color.blue, Color.green, Color.yellow };
//        //used on server to avoid assigning the same color to two player
//        static List<int> _colorInUse = new List<int>();

//        public Button colorButton;
//		public InputField nameInput;
		public Text playerNumberText;
        public Text playerUserId;
        public Button joinButton;
        public Button waitingPlayerButton;
        public Button removePlayerButton;

        public GameObject localIcone;
        public GameObject remoteIcone;

		//OnPlayerNumber function will be invoked on clients when server changes the value of playerNumber
		[SyncVar(hook = "OnPlayerNumber")]
		public int playerNumber = 0;

		//OnPlayerName function will be invoked on clients when server changes the value of playerName
        [SyncVar(hook = "OnPlayerName")]
		public string playerName = "";

//        [SyncVar(hook = "OnMyColor")]
//        public Color playerColor = Color.white;

//        public Color OddRowColor = new Color(250.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f, 1.0f);
//        public Color EvenRowColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f, 1.0f);

		static Color JoinColor = Color.white; //  new Color(255.0f/255.0f, 0.0f, 101.0f/255.0f,1.0f);
        static Color NotReadyColor = new Color(34.0f / 255.0f, 44 / 255.0f, 55.0f / 255.0f, 1.0f);
        static Color ReadyColor = new Color(0.0f, 204.0f / 255.0f, 204.0f / 255.0f, 1.0f);
        static Color TransparentColor = new Color(0, 0, 0, 0);

        //static Color OddRowColor = new Color(250.0f / 255.0f, 250.0f / 255.0f, 250.0f / 255.0f, 1.0f);
        //static Color EvenRowColor = new Color(180.0f / 255.0f, 180.0f / 255.0f, 180.0f / 255.0f, 1.0f);


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
//          OnMyColor(playerColor);

//			playerUserId.text = playerName;
//			playerNumberText.text = playerNumber.ToString();
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
//            nameInput.interactable = false;
            removePlayerButton.interactable = NetworkServer.active;

            ChangeJoinButtonColor(NotReadyColor);

            joinButton.transform.GetChild(0).GetComponent<Text>().text = "...";
            joinButton.interactable = false;

            OnClientReady(false);
        }

        void SetupLocalPlayer()
        {
//            nameInput.interactable = true;
            remoteIcone.gameObject.SetActive(false);
            localIcone.gameObject.SetActive(true);

            CheckRemoveButton();

//            if (playerColor == Color.white)
//                CmdColorChange();

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

			//have to use child count of player prefab already setup as "this.slot" is not set yet
//            if (playerName == "")
//				CmdNameChanged(FightingLegends.FightManager.SavedGameStatus.UserId);
//				CmdNameChanged("Player" + playerNumber);

            //we switch from simple name display to name input
//            colorButton.interactable = true;
//            nameInput.interactable = true;

//            nameInput.onEndEdit.RemoveAllListeners();
//            nameInput.onEndEdit.AddListener(OnNameChanged);
//
//            colorButton.onClick.RemoveAllListeners();
//            colorButton.onClick.AddListener(OnColorClicked);

            joinButton.onClick.RemoveAllListeners();
            joinButton.onClick.AddListener(OnReadyClicked);

//			Debug.Log("SetupLocalPlayer: " + playerNumber + " / " + playerName);

            //when OnClientEnterLobby is called, the local PlayerController is not yet created, so we need to redo that here to disable
            //the add button if we reach maxLocalPlayer. We pass 0, as it was already counted on OnClientEnterLobby
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
                textComponent.text = "READY";
                textComponent.color = ReadyColor;
                joinButton.interactable = false;
//                colorButton.interactable = false;
//                nameInput.interactable = false;
            }
            else
            {
				ChangeJoinButtonColor(isLocalPlayer ? JoinColor : TransparentColor); // NotReadyColor);

                Text textComponent = joinButton.transform.GetChild(0).GetComponent<Text>();
                textComponent.text = isLocalPlayer ? "JOIN" : "...";
                textComponent.color = Color.white;
                joinButton.interactable = isLocalPlayer;
//                colorButton.interactable = isLocalPlayer;
//                nameInput.interactable = isLocalPlayer;
            }

//			waitingPlayerButton.gameObject.SetActive(false);
        }

//        public void OnPlayerListChanged(int idx)
//        { 
//            GetComponent<Image>().color = (idx % 2 == 0) ? EvenRowColor : OddRowColor;
//        }

        ///===== callback from sync var

	
        public void OnPlayerName(string newName)
        {
           	playerName = newName;
//			nameInput.text = playerName;
            playerUserId.text = playerName;
        }

		public void OnPlayerNumber(int playerNum)
		{
			playerNumberText.text = playerNum.ToString();
		}

//        public void OnMyColor(Color newColor)
//        {
//            playerColor = newColor;
//            colorButton.GetComponent<Image>().color = newColor;
//        }


        //===== UI Handler

        //Note that those handler use Command function, as we need to change the value on the server not locally
        //so that all client get the new value throught syncvar
//        public void OnColorClicked()
//        {
//            CmdColorChange();
//        }

        public void OnReadyClicked()
        {
            SendReadyToBeginMessage();
        }

//        public void OnNameChanged(string str)
//        {
//            CmdNameChanged(str);
//        }

        public void OnRemovePlayerClick()
        {
            if (isLocalPlayer)
            {
                RemovePlayer();
            }
            else if (isServer)
                LobbyManager.s_Singleton.KickPlayer(connectionToClient);
                
        }

        public void ToggleJoinButton(bool enabled)
        {
            joinButton.gameObject.SetActive(enabled); 		// join button
			waitingPlayerButton.gameObject.SetActive(!enabled);
        }

        [ClientRpc]
        public void RpcUpdateCountdown(int countdown)
        {
            LobbyManager.s_Singleton.countdownPanel.UIText.text = "Match Starting in " + countdown;
            LobbyManager.s_Singleton.countdownPanel.gameObject.SetActive(countdown != 0);
        }

        [ClientRpc]
        public void RpcUpdateRemoveButton()
        {
            CheckRemoveButton();
        }
			

        //====== Server Command

//        [Command]
//        public void CmdColorChange()
//        {
//            int idx = System.Array.IndexOf(Colors, playerColor);
//
//            int inUseIdx = _colorInUse.IndexOf(idx);
//
//            if (idx < 0) idx = 0;
//
//            idx = (idx + 1) % Colors.Length;
//
//            bool alreadyInUse = false;
//
//            do
//            {
//                alreadyInUse = false;
//                for (int i = 0; i < _colorInUse.Count; ++i)
//                {
//                    if (_colorInUse[i] == idx)
//                    {//that color is already in use
//                        alreadyInUse = true;
//                        idx = (idx + 1) % Colors.Length;
//                    }
//                }
//            }
//            while (alreadyInUse);
//
//            if (inUseIdx >= 0)
//            {//if we already add an entry in the colorTabs, we change it
//                _colorInUse[inUseIdx] = idx;
//            }
//            else
//            {//else we add it
//                _colorInUse.Add(idx);
//            }
//
//            playerColor = Colors[idx];
//        }

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

        //Cleanup thing when get destroy (which happen when client kick or disconnect)
        public void OnDestroy()
        {
            LobbyPlayerList._instance.RemovePlayer(this);
            if (LobbyManager.s_Singleton != null)
				LobbyManager.s_Singleton.OnPlayersNumberModified(-1);

//            int idx = System.Array.IndexOf(Colors, playerColor);
//
//            if (idx < 0)
//                return;
//
//            for (int i = 0; i < _colorInUse.Count; ++i)
//            {
//                if (_colorInUse[i] == idx)
//                {//that color is already in use
//                    _colorInUse.RemoveAt(i);
//                    break;
//                }
//            }
        }
    }
}
