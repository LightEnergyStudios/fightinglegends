using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;

namespace FightingLegends
{
	// network player spawned by Network(Lobby)Manager
	// in LAN environment server (host) represents player1, client represents player2
	// in WAN environment player number passed through from lobby player - game creator is player1
	public class NetworkFighter : NetworkBehaviour
	{
		// set via lobby (hook)
		public int PlayerNumber = 0;	
		public string PlayerName = "";			// user id
	
//		// fighters set via FighterSelect delegate - server only
//		private static string Fighter1Name = "";
//		private static string Fighter1Colour = "";
//		private static string Fighter2Name = "";
//		private static string Fighter2Colour = "";
//
//		// location set via WorldMap delegate - server only
//		public string SelectedLocation = "";

		// SyncVar hooks invoked on clients when server changes the value

		// fighters set via FighterSelect delegate
//		[SyncVar] // (hook = "SetFighter1Name")]
		private static string Fighter1Name;

//		[SyncVar] // (hook = "SetFighter1Colour")]
		private static string Fighter1Colour;

//		[SyncVar] // (hook = "SetFighter2Name")]
		private static string Fighter2Name;

//		[SyncVar] // (hook = "SetFighter2Colour")]
		private static string Fighter2Colour;

		// location set via WorldMap delegate
//		[SyncVar] // (hook = "SetSelectedLocation")]
		private static string SelectedLocation;


		private const float exitFightPause = 3.0f;

		private FightManager fightManager;


		private bool IsPlayer1
		{
			get
			{
				if (PlayerNumber == 0)		// not set via lobby - must be LAN game
					return isServer;		// LAN host == Player1
				
				return PlayerNumber == 1;	// from lobby - first to join (game creator) == Player1
			}
		}

		public void Awake()
		{			
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			ResetFight();
		}

		public void Start()
		{
			if (! FightManager.IsNetworkFight)
				return;

//			Debug.Log("NetworkFighter.Start: IsPlayer1 = " + IsPlayer1 + " - " + PlayerNumber + " / " + PlayerName + " isLocalPlayer = " + isLocalPlayer);

			if (PlayerNumber == 0)		// ie. not set via lobby (game creator == Player1)
				PlayerNumber = isServer ? 1 : 2;
		}

//		public override void OnStartClient ()
//		{
//			base.OnStartClient ();
//			OnNameChange(playerName);
//		}

		public override void OnStartLocalPlayer()
		{
//			Debug.Log("NetworkFighter.OnStartLocalPlayer: IsPlayer1 = " + IsPlayer1 + " - " + PlayerNumber + " / " + PlayerName + " isLocalPlayer = " + isLocalPlayer);
			StartListening();
		}

		private void OnDestroy()
		{
			if (FightManager.IsNetworkFight && isLocalPlayer)
				StopListening();
		}

		private void StartListening()
		{
//			Debug.Log("NetworkFighter.StartListening: " + PlayerNumber + " / " + PlayerName + " isLocalPlayer = " + isLocalPlayer);
			if (!isLocalPlayer)
				return;

			GestureListener.OnTap += SingleFingerTap;				// strike		
			GestureListener.OnHoldStart += HoldDown;				// start block	
			GestureListener.OnHoldEnd += HoldRelease;				// end block
			GestureListener.OnSwipeLeft += SwipeLeft;				// counter
			GestureListener.OnSwipeRight += SwipeRight;				// special
			GestureListener.OnSwipeLeftRight += SwipeLeftRight;		// vengeance
			GestureListener.OnSwipeDown += SwipeDown;				// shove
			GestureListener.OnSwipeUp += SwipeUp;					// power up

			GestureListener.OnTwoFingerTap += TwoFingerTap;			// roman cancel		

			GestureListener.OnFingerTouch += FingerTouch;			// reset moveCuedOk
			GestureListener.OnFingerRelease += FingerRelease;		// to ensure block released

			FighterSelect.OnFighterSelected += FighterSelected;
			WorldMap.OnLocationSelected += LocationSelected;

			FightManager.OnQuitFight += QuitFight;
//			FightManager.OnFightPaused += PauseFight;
		}

		private void StopListening()
		{
			if (!isLocalPlayer)
				return;

			GestureListener.OnTap -= SingleFingerTap;		

			GestureListener.OnHoldStart -= HoldDown;		
			GestureListener.OnHoldEnd -= HoldRelease;		
			GestureListener.OnSwipeLeft -= SwipeLeft;
			GestureListener.OnSwipeRight -= SwipeRight;
			GestureListener.OnSwipeLeftRight -= SwipeLeftRight;
			GestureListener.OnSwipeDown -= SwipeDown;
			GestureListener.OnSwipeUp -= SwipeUp;

			GestureListener.OnTwoFingerTap -= TwoFingerTap;	

			GestureListener.OnFingerTouch -= FingerTouch;			
			GestureListener.OnFingerRelease -= FingerRelease;

			FighterSelect.OnFighterSelected -= FighterSelected;
			WorldMap.OnLocationSelected -= LocationSelected;

			FightManager.OnQuitFight -= QuitFight;
//			FightManager.OnFightPaused -= PauseFight;
		}
			
		#region animation

		// called every Time.fixedDeltaTime seconds
		// 0.0666667 = 1/15 sec
		[ServerCallback]
		private void FixedUpdate()
		{
			if (FightManager.IsNetworkFight && isServer)
				RpcUpdateAnimation();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcUpdateAnimation()
		{
			if (fightManager == null)
				return;
			
			if (isLocalPlayer)
				fightManager.UpdateAnimation();
		}

	
		private void Update()
		{
			//TODO: remove this!
			// handle key strokes for testing in Unity
			if (FightManager.IsNetworkFight && ! DeviceDetector.IsMobile)
			{
				if (Input.GetKeyDown(KeyCode.X))
				{
					TwoFingerTap();				// roman cancel (requires gauge)
				}
			}
		}

		#endregion

		#region fight construction

		private bool Player1Ready { get { return !string.IsNullOrEmpty(Fighter1Name) && !string.IsNullOrEmpty(Fighter1Colour); }}
		private bool Player2Ready { get { return !string.IsNullOrEmpty(Fighter2Name) && !string.IsNullOrEmpty(Fighter2Colour); }}
		private bool CanStartFight { get { return Player1Ready && Player2Ready && !string.IsNullOrEmpty(SelectedLocation); }}
		private bool IsPlayerReady { get { return IsPlayer1 ? Player1Ready : Player2Ready; } }


		// FighterSelect.OnFighterSelected
		private void FighterSelected(Fighter fighter)
		{
			if (isServer)
				SetFighter(true, fighter.FighterName, fighter.ColourScheme);
			else
				CmdSetFighter(false, fighter.FighterName, fighter.ColourScheme);

//			if (isServer)
//			{
//				Fighter1Name = fighter.FighterName;
//				Fighter1Colour = fighter.ColourScheme;
//			}
//			else
//			{				
//				Fighter2Name = fighter.FighterName;
//				Fighter2Colour = fighter.ColourScheme;
//			}
//
//			TryStartFight();		// if both fighters and location set

//			if (isLocalPlayer)
//			{
//				CmdSetFighter(IsPlayer1, fighter.FighterName, fighter.ColourScheme);
////				Debug.Log("FighterSelected: IsPlayer1 = " + IsPlayer1 + " - " + fighter.FighterName + " / " + fighter.ColourScheme);
//			}
		}
			
		[Server]
		private void SetFighter(bool isPlayer1, string name, string colour)
		{
			if (!isServer)
				return;
			
			if (isPlayer1)
			{
				Fighter1Name = name;			// SyncVar hook tries to start fight
				Fighter1Colour = colour;		// SyncVar hook tries to start fight
				Debug.Log("SetFighter: isPlayer1 = " + isPlayer1 + ", Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);
			}
			else
			{				
				Fighter2Name = name;			// SyncVar hook tries to start fight
				Fighter2Colour = colour;		// SyncVar hook tries to start fight
				Debug.Log("SetFighter: isPlayer1 = " + isPlayer1 + ", Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);
			}

//			Debug.Log("SetFighter: isPlayer1 = " + isPlayer1 + ", Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);
			TryStartFight();
		}

		[Command]
		// called from client, runs on server
		public void CmdSetFighter(bool isPlayer1, string name, string colour)
		{
			if (!isServer)
				return;

			SetFighter(isPlayer1, name, colour);
		}


//		[Command]
//		// called from client, runs on server
//		private void CmdStartFight()
//		{
//			if (!isServer)
//				return;
//
//			if (player1)
//			{
//				Fighter1Name = fighterName;	
//				Fighter1Colour = fighterColour;	
//			}
//			else
//			{
//				Fighter2Name = fighterName;	
//				Fighter2Colour = fighterColour;	
//			}

//			TryStartFight();		// if both fighters and location set
//		}

//		[Command]
//		// called from client, runs on server
//		private void CmdSetFighter(bool player1, string fighterName, string fighterColour)
//		{
//			if (!isServer)
//				return;
//
//			if (player1)
//			{
//				Fighter1Name = fighterName;	
//				Fighter1Colour = fighterColour;	
//			}
//			else
//			{
//				Fighter2Name = fighterName;	
//				Fighter2Colour = fighterColour;	
//			}
//
//			TryStartFight();		// if both fighters and location set
//		}


		// WorldMap.OnLocationSelected
		private void LocationSelected(string location)
		{
			if (isServer)
				SetLocation(location);
			else
				CmdSetLocation(location);
			
//			SelectedLocation = location;
//
//			TryStartFight();		// if both fighters and location set

//			if (isLocalPlayer)
//			{
//				CmdSetLocation(location);	
////				Debug.Log("LocationSelected: IsPlayer1 = " + IsPlayer1 + " - " + location);
//			}
		}

		[Server]
		private void SetLocation(string location)
		{
			if (!isServer)
				return;
			
			SelectedLocation = location;		// SyncVar hook tries to start fight

			Debug.Log("SetLocation: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			TryStartFight();
		}

		[Command]
		// called from client, runs on server
		private void CmdSetLocation(string location)
		{
			if (!isServer)
				return;
			
			SetLocation(location);
		}


//		[Command]
//		// called from client, runs on server
//		private void CmdSetLocation(string location)
//		{
//			if (!isServer)
//				return;
//
////			SelectedLocation = location;
//
//			if (! TryStartFight()) 		// if both fighters and location set
//				RpcNetworkMessage(NetworkMessageType.WaitingToStart);
//		}


		// SyncVar hook - called on client
		public void SetFighter1Name(string name)
		{
			Debug.Log("SetFighter1Name: " + name);
			Fighter1Name = name;

			TryStartFight();
		}

		// SyncVar hook - called on client
		public void SetFighter1Colour(string colour)
		{
			Debug.Log("SetFighter1Colour: " + colour);
			Fighter1Colour = colour;

			TryStartFight();
		}

		// SyncVar hook - called on client
		public void SetFighter2Name(string name)
		{
			Debug.Log("SetFighter2Name: " + name);
			Fighter2Name = name;

			TryStartFight();
		}

		// SyncVar hook - called on client
		public void SetFighter2Colour(string colour)
		{
			Debug.Log("SetFighter2Colour: " + colour);
			Fighter2Colour = colour;

			TryStartFight();
		}

		// SyncVar hook - called on client
		public void SetSelectedLocation(string location)
		{
			Debug.Log("SetSelectedLocation: " + location);
			SelectedLocation = location;

			TryStartFight();
		}

		private bool TryStartFight()
		{
			if (!isServer)
				return false;
			
			if (CanStartFight)
			{
				RpcStartFight(Fighter1Name, Fighter1Colour, Fighter2Name, Fighter2Colour, SelectedLocation);

				// reset for next fight
				ResetFight();

				RpcNetworkMessage(NetworkMessageType.None);		// disable
				return true;
			}
			else
				Debug.Log("TryStartFight: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			return false;
		}

//		private bool TryStartFight()
//		{
//			if (!isLocalPlayer)
//				return false;
//			
////			Debug.Log("TryStartFight: CanStartFight = " + CanStartFight);
//
//			if (CanStartFight)
//			{
//				if (IsPlayer1)
//					fightManager.StartNetworkArcadeFight(Fighter1Name, Fighter1Colour, Fighter2Name, Fighter2Colour, SelectedLocation);
//				else
//					fightManager.StartNetworkArcadeFight(Fighter2Name, Fighter2Colour, Fighter1Name, Fighter1Colour, SelectedLocation);
//				
//				// reset for next fight
//				ResetFight();
//				return true;
//			}
//			else
//				Debug.Log("TryStartFight: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);
//
//			return false;
//		}

		private void ResetFight()
		{
			Debug.Log("ResetFight");

			Fighter1Name = "";
			Fighter1Colour = "";
			Fighter2Name = "";
			Fighter2Colour = "";
			SelectedLocation = "";
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcStartFight(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location)
		{
			if (isServer)
				fightManager.StartNetworkArcadeFight(fighter1Name, fighter1Colour, fighter2Name, fighter2Colour, location);
			else
				fightManager.StartNetworkArcadeFight(fighter2Name, fighter2Colour, fighter1Name, fighter1Colour, location);
		}

//		private void StartFight(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location)
//		{
//			if (isServer)
//				fightManager.StartNetworkArcadeFight(fighter1Name, fighter1Colour, fighter2Name, fighter2Colour, location);
//			else
//				fightManager.StartNetworkArcadeFight(fighter2Name, fighter2Colour, fighter1Name, fighter1Colour, location);
//		}


		[ClientRpc]
		// called on server, runs on clients
		private void RpcNetworkMessage(NetworkMessageType messageType)
		{
			string message = "";

			switch (messageType)
			{
				case NetworkMessageType.WaitingToStart:
					message = IsPlayerReady ? (FightManager.Translate("waitingForOpponent") + " ...") : (FightManager.Translate("opponentReady", false, true));
					break;

				case NetworkMessageType.FightEnding:
					message = FightManager.Translate("fightEnding") + " ...";
					break;

				default:
					break;
			}

			fightManager.NetworkMessage(message);		// disabled if null or empty
		}

		#endregion

		#region quit / pause fight

		private void QuitFight()
		{
			if (isLocalPlayer)
				CmdQuitFight();
		}

		[Command]
		// called from client, runs on server
		private void CmdQuitFight()
		{
			if (!isServer)
				return;
			
			RpcQuitFight();
		}


		[ClientRpc]
		// called on server, runs on clients
		private void RpcQuitFight()
		{
			if (!isLocalPlayer)
				return;
			
			StartCoroutine(ExitFightAfterPause());
		}
			
		private IEnumerator ExitFightAfterPause()
		{
			fightManager.PauseFight(true);
			fightManager.NetworkMessage(FightManager.Translate("fightEnding") + " ...");

			yield return new WaitForSeconds(exitFightPause);

			fightManager.NetworkMessage("");
			fightManager.ExitFight();
		}
			

//		private void PauseFight(bool paused)
//		{
//			if (isLocalPlayer)
//				CmdPauseFight(paused);
//		}
	
//		[Command]
//		// called from client, runs on server
//		private void CmdPauseFight(bool paused)
//		{
//			if (!isServer)
//				return;
//
//			RpcPauseFight(paused);
//		}

//		[ClientRpc]
//		// called on server, runs on clients
//		private void RpcPauseFight(bool paused)
//		{
//			fightManager.PauseFight(paused, false);
//		}

		#endregion


		#region gesture handlers

		private void SingleFingerTap()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;

			if (isLocalPlayer)
				CmdSingleFingerTap(IsPlayer1);	
		}

		private void HoldDown()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdHoldDown(IsPlayer1);	
		}

		private void HoldRelease()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdHoldRelease(IsPlayer1);	
		}

		private void SwipeLeft()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdSwipeLeft(IsPlayer1);	
		}

		private void SwipeRight()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdSwipeRight(IsPlayer1);	
		}

		private void SwipeLeftRight()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdSwipeLeftRight(IsPlayer1);	
		}

		private void SwipeDown()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdSwipeDown(IsPlayer1);	
		}

		private void SwipeUp()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdSwipeUp(IsPlayer1);	
		}

		private void TwoFingerTap()
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdTwoFingerTap(IsPlayer1);	
		}

		private void FingerTouch(Vector3 position)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdFingerTouch(IsPlayer1);	
		}

		private void FingerRelease(Vector3 position)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (isLocalPlayer)
				CmdFingerRelease(IsPlayer1);	
		}

		#endregion


		#region server commands invoked by local player gestures

		[Command]
		// called from client, runs on server
		private void CmdSingleFingerTap(bool player1)
		{
			if (!isServer)
				return;
			
			RpcSingleFingerTap(player1);
		}

		[Command]
		// called from client, runs on server
		private void CmdHoldDown(bool player1)
		{
			if (!isServer)
				return;

			RpcHoldDown(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdHoldRelease(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcHoldRelease(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSwipeLeft(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcSwipeLeft(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSwipeRight(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcSwipeRight(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSwipeLeftRight(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcSwipeLeftRight(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSwipeDown(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcSwipeDown(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSwipeUp(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcSwipeUp(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdTwoFingerTap(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcTwoFingerTap(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdFingerTouch(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcFingerTouch(player1);	
		}

		[Command]
		// called from client, runs on server
		private void CmdFingerRelease(bool player1)
		{
//			if (! FightManager.SavedGameStatus.FightInProgress)
//				return;
			
			if (!isServer)
				return;

			RpcFingerRelease(player1);	
		}

		#endregion


		#region client rpcs to execute moves

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSingleFingerTap(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SingleFingerTap();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SingleFingerTap();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SingleFingerTap();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SingleFingerTap();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcHoldDown(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;
			
			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.HoldDown();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.HoldDown();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.HoldDown();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.HoldDown();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcHoldRelease(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.HoldRelease();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.HoldRelease();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.HoldRelease();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.HoldRelease();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeLeft(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SwipeLeft();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SwipeLeft();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SwipeLeft();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SwipeLeft();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeRight(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SwipeRight();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SwipeRight();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SwipeRight();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SwipeRight();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeLeftRight(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SwipeLeftRight();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SwipeLeftRight();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SwipeLeftRight();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SwipeLeftRight();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeDown(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SwipeDown();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SwipeDown();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SwipeDown();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SwipeDown();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeUp(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.SwipeUp();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.SwipeUp();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.SwipeUp();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.SwipeUp();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcTwoFingerTap(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.TwoFingerTap();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.TwoFingerTap();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.TwoFingerTap();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.TwoFingerTap();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcFingerTouch(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.FingerTouch();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.FingerTouch();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.FingerTouch();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.FingerTouch();
			}
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcFingerRelease(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (isServer) 
			{
				if (player1 && fightManager.HasPlayer1)
					fightManager.Player1.FingerRelease();
				else if (fightManager.HasPlayer2)
					fightManager.Player2.FingerRelease();
			}
			else
			{
				if (player1 && fightManager.HasPlayer2)
					fightManager.Player2.FingerRelease();
				else if (fightManager.HasPlayer1)
					fightManager.Player1.FingerRelease();
			}
		}

		#endregion



	}
}
