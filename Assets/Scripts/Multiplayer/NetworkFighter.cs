using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;
using System;

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
	
		// SyncVar hooks invoked on clients when server changes the value
//		[SyncVar(hook = "SetNetworkFighters")]
//		private NetworkFighters fighters;

		// fighters set via FighterSelect delegate
//		[SyncVar(hook = "SetFighter1Name")]
		private static string Fighter1Name; 		// server only

//		[SyncVar(hook = "SetFighter1Colour")]
		private static string Fighter1Colour; 		// server only

//		[SyncVar(hook = "SetFighter2Name")]
		private static string Fighter2Name; 		// server only

//		[SyncVar(hook = "SetFighter2Colour")]
		private static string Fighter2Colour; 		// server only

		// location set via WorldMap delegate
//		[SyncVar(hook = "SetSelectedLocation")]
		private static string SelectedLocation; 	// server only



		// events invoked on client when the event is called on the server
//		public delegate void StartFightDelegate(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location);
//		public delegate void Fighter1Delegate(string fighter1Name, string fighter1Colour);
//		public delegate void Fighter2Delegate(string fighter2Name, string fighter2Colour);
//		public delegate void LocationDelegate(string location);

//		[SyncEvent]
//		public event StartFightDelegate EventStartFight;

//		[SyncEvent]
//		public event Fighter1Delegate EventFighter1;
//
//		[SyncEvent]
//		public event Fighter1Delegate EventFighter2;
//
//		[SyncEvent]
//		public event LocationDelegate EventLocation;


//		[Command]
//		public void CmdFighter1(string name, string colour)
//		{
//			EventFighter1(name, colour);
//		}
//
//		[Command]
//		public void CmdFighter2(string name, string colour)
//		{
//			EventFighter2(name, colour);
//		}
//
//		[Command]
//		public void CmdLocation(string location)
//		{
//			EventLocation(location);
//		}

//		[Command]
//		public void CmdStartFight(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location)
//		{
//			EventStartFight(fighter1Name, fighter1Colour, fighter2Name, fighter2Colour, location);
//		}


		private const float exitFightPause = 3.0f;		// network message displayed

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

			if (isServer)
			{
				ResetFighters();
				fightStarted = false;
			}
		}

		public void Start()
		{
			if (! FightManager.IsNetworkFight)
				return;

//			Debug.Log("NetworkFighter.Start: IsPlayer1 = " + IsPlayer1 + " - " + PlayerNumber + " / " + PlayerName + " isLocalPlayer = " + isLocalPlayer);

			if (PlayerNumber == 0)		// ie. not set via lobby (game creator == Player1)
				PlayerNumber = isServer ? 1 : 2;
		}

//		// invoked when a client is started
//		public override void OnStartClient ()
//		{
//			base.OnStartClient ();
//
//			SetFighter1Name(Fighter1Name);
//			SetFighter1Colour(Fighter1Colour);
//			SetFighter2Name(Fighter2Name);
//			SetFighter2Colour(Fighter2Colour);
//			SetSelectedLocation(SelectedLocation);
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


		#region assemble fighters and location

		private bool fightStarted = false;

		private bool Player1Set { get { return !string.IsNullOrEmpty(Fighter1Name) && !string.IsNullOrEmpty(Fighter1Colour); }}
		private bool Player2Set { get { return !string.IsNullOrEmpty(Fighter2Name) && !string.IsNullOrEmpty(Fighter2Colour); }}
		private bool LocationSet { get { return !string.IsNullOrEmpty(SelectedLocation); }}
		private bool CanStartFight { get { return !fightStarted && Player1Set && Player2Set && LocationSet; }}

		private bool IsPlayerReady { get { return IsPlayer1 ? Player1Set : Player2Set; } }

//		private bool NetworkPlayer1Ready { get { return !string.IsNullOrEmpty(fighters.Fighter1Name) && !string.IsNullOrEmpty(fighters.Fighter1Colour); }}
//		private bool NetworkPlayer2Ready { get { return !string.IsNullOrEmpty(fighters.Fighter2Name) && !string.IsNullOrEmpty(fighters.Fighter2Colour); }}
//		private bool NetworkCanStartFight { get { return NetworkPlayer1Ready && NetworkPlayer2Ready && !string.IsNullOrEmpty(fighters.Location); }}
//		private bool NetworkIsPlayerReady { get { return IsPlayer1 ? NetworkPlayer1Ready : NetworkPlayer2Ready; } }


		// FighterSelect.OnFighterSelected
		private void FighterSelected(Fighter fighter)
		{
			if (!isLocalPlayer)
				return;
			
			Debug.Log("FighterSelected: IsPlayer1 = " + IsPlayer1 + " : " + fighter.FighterName + " / " + fighter.ColourScheme);

			if (IsPlayer1)
				CmdSetFighter1(fighter.FighterName, fighter.ColourScheme);
			else
				CmdSetFighter2(fighter.FighterName, fighter.ColourScheme);
		}

		[Command]
		// called from client, runs on server
		public void CmdSetFighter1(string name, string colour)
		{
			if (!isServer)
				return;

			if (fightStarted)
				return;
			
			Fighter1Name = name;
			Fighter1Colour = colour;

			Debug.Log("CmdSetFighter1: Fighter1 = " + Fighter1Name + " " + Fighter1Colour + ", Fighter2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			fightStarted = TryStartFight();
		}

		[Command]
		// called from client, runs on server
		public void CmdSetFighter2(string name, string colour)
		{
			if (!isServer)
				return;

			if (fightStarted)
				return;

			Fighter2Name = name;
			Fighter2Colour = colour;

			Debug.Log("CmdSetFighter2: Fighter1 = " + Fighter1Name + " " + Fighter1Colour + ", Fighter2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			fightStarted = TryStartFight();
		}

//		[Command]
//		// called from client, runs on server
//		public void CmdSetNetworkFighter(bool isPlayer1, string name, string colour)
//		{
//			if (!isServer)
//				return;
//
//			if (isPlayer1)
//			{
//				fighters.Fighter1Name = name;
//				fighters.Fighter1Colour = colour;
//			}
//			else
//			{			
//				fighters.Fighter2Name = name;
//				fighters.Fighter2Colour = colour;
//			}
//
//			Debug.Log("CmdSetNetworkFighter: isPlayer1 = " + isPlayer1 + ", Player1 = " + fighters.Fighter1Name + " " + fighters.Fighter1Colour + ", Player2 = " + fighters.Fighter2Name + " " + fighters.Fighter2Colour + ", Location = " + fighters.Location);
//		}
			

		// WorldMap.OnLocationSelected
		private void LocationSelected(string location)
		{
			if (!isLocalPlayer)
				return;
			
			CmdSetLocation(location);
		}


		[Command]
		// called from client, runs on server
		private void CmdSetLocation(string location)
		{
			if (!isServer)
				return;

			if (fightStarted)
				return;

			if (LocationSet)		// opponent got there first! (shouldn't get this far)
				return;

			SelectedLocation = location;		// SyncVar hook tries to start fight

			Debug.Log("CmdSetLocation: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			fightStarted = TryStartFight();
		}

//		[Command]
//		// called from client, runs on server
//		private void CmdSetNetworkLocation(string location)
//		{
//			if (!isServer)
//				return;
//
//			//			SetLocation(location);
//
//			fighters.Location = location;		// SyncVar hook tries to start fight
//
//			Debug.Log("CmdSetNetworkLocation: " + "Player1 = " + fighters.Fighter1Name + " " + fighters.Fighter1Colour + ", Player2 = " + fighters.Fighter2Name + " " + fighters.Fighter2Colour + ", Location = " + fighters.Location);
//
////			TryStartFightRpc();
//		}
//			
//
//		// SyncVar hook - called on client
//		private void SetNetworkFighters(NetworkFighters networkFighters)
//		{
//			fighters = networkFighters;
//
//			Debug.Log("SetNetworkFighters: " + "Player1 = " + fighters.Fighter1Name + " " + fighters.Fighter1Colour + ", Player2 = " + fighters.Fighter2Name + " " + fighters.Fighter2Colour + ", Location = " + fighters.Location);
//
//			NetworkTryStartFight();
//		}

// 		// client (SynVar hook) version
//		private bool NetworkTryStartFight()
//		{
////			if (!isLocalPlayer)
////				return false;
//			
////			Debug.Log("TryStartFight: CanStartFight = " + CanStartFight);
//
//			if (NetworkCanStartFight)
//			{
//				if (IsPlayer1)
//					fightManager.StartNetworkArcadeFight(fighters.Fighter1Name, fighters.Fighter1Colour, fighters.Fighter2Name, fighters.Fighter2Colour, fighters.Location);
//				else
//					fightManager.StartNetworkArcadeFight(fighters.Fighter2Name, fighters.Fighter2Colour, fighters.Fighter1Name, fighters.Fighter1Colour, fighters.Location);
//				
//				// reset for next fight
//				ResetFighters();
//				return true;
//			}
//			else
//				Debug.Log("NetworkTryStartFight: " + "Player1 = " + fighters.Fighter1Name + " " + fighters.Fighter1Colour + ", Player2 = " + fighters.Fighter2Name + " " + fighters.Fighter2Colour + ", Location = " + fighters.Location);
//
//			return false;
//		}

//		// SyncVar hook - called on client
//		public void SetFighter1Name(string name)
//		{
//			Debug.Log("SetFighter1Name: " + name);
//			Fighter1Name = name;
//
//			fightStarted = TryStartClientFight();
//		}
//
//		// SyncVar hook - called on client
//		public void SetFighter1Colour(string colour)
//		{
//			Debug.Log("SetFighter1Colour: " + colour);
//			Fighter1Colour = colour;
//
//			fightStarted = TryStartClientFight();
//		}
//
//		// SyncVar hook - called on client
//		public void SetFighter2Name(string name)
//		{
//			Debug.Log("SetFighter2Name: " + name);
//			Fighter2Name = name;
//
//			fightStarted = TryStartClientFight();
//		}
//
//		// SyncVar hook - called on client
//		public void SetFighter2Colour(string colour)
//		{
//			Debug.Log("SetFighter2Colour: " + colour);
//			Fighter2Colour = colour;
//
//			fightStarted = TryStartClientFight();
//		}
//
//		// SyncVar hook - called on client
//		public void SetSelectedLocation(string location)
//		{
//			Debug.Log("SetSelectedLocation: " + location);
//			SelectedLocation = location;
//
//			fightStarted = TryStartClientFight();
//		}

		// server (client RPC) version
		private bool TryStartFight()
		{
			if (!isServer)
				return false;
			
			if (CanStartFight)
			{
				RpcStartFight(Fighter1Name, Fighter1Colour, Fighter2Name, Fighter2Colour, SelectedLocation);

				// reset for next fight
				ResetFighters();

				RpcNetworkMessage(NetworkMessageType.None);		// disable
				return true;
			}
			else
				Debug.Log("TryStartFight: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);

			return false;
		}

//		// client (SynVar hook) version
//		private bool TryStartClientFight()
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
//				return true;
//			}
//			else
//				Debug.Log("TryStartClientFight: " + "Player1 = " + Fighter1Name + " " + Fighter1Colour + ", Player2 = " + Fighter2Name + " " + Fighter2Colour + ", Location = " + SelectedLocation);
//
//			return false;
//		}

		private void ResetFighters()
		{
			if (!isServer)
				return;
			
			Debug.Log("ResetFighters");

			Fighter1Name = "";
			Fighter1Colour = "";
			Fighter2Name = "";
			Fighter2Colour = "";
			SelectedLocation = "";

//			fighters = new NetworkFighters();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcStartFight(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location)
		{
			Debug.Log("RpcStartFight: IsPlayer1 = " + IsPlayer1 + " : " + fighter1Name + "/" + fighter1Colour + " : " + fighter2Name + "/" + fighter2Colour + " : " + location);

//			if (isServer)
			if (IsPlayer1)
				fightManager.StartNetworkArcadeFight(fighter1Name, fighter1Colour, fighter2Name, fighter2Colour, location);
			else
				fightManager.StartNetworkArcadeFight(fighter2Name, fighter2Colour, fighter1Name, fighter1Colour, location);
		}
			

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


	[Serializable]
	public struct NetworkFighters
	{
		public string Fighter1Name;
		public string Fighter1Colour;

		public string Fighter2Name;
		public string Fighter2Colour;

		public string Location;
	}
}
