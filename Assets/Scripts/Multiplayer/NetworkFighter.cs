﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;
using System;
using UnityEngine.Networking.NetworkSystem;

namespace FightingLegends
{
	// network player spawned by Network(Lobby)Manager on scene change
	// in LAN environment server (host) represents player1, client represents player2
	// in WAN environment player number passed through from lobby player - game creator is player1
	public class NetworkFighter : NetworkBehaviour
	{
		// set via lobby (hook)
		public int PlayerNumber = 0;	
		public string PlayerName = "";			// user id

		private NetworkFightManager networkFightManager;	// server only

		private const float exitFightPause = 3.0f;		// network message displayed

		private FightManager fightManager;


		public bool IsPlayer1
		{
			get
			{
				if (PlayerNumber == 0)		// not set via lobby - must be LAN game
					return isServer;		// LAN host == Player1
				
				return PlayerNumber == 1;	// from lobby - first to join (game creator) == Player1
			}
		}


		public void Start()
		{			
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}
			


		public void SetFightManager(NetworkFightManager fightManager)
		{
//			Debug.Log("NetworkFighter.SetFightManager: netId = " + fightManager.netId);
			networkFightManager = fightManager;
		}

		// called when becomes active on the server - includes hosts
//		public override void OnStartServer()
//		{
//			Debug.Log("NetworkFighter.OnStartServer: IsPlayer1 = " + IsPlayer1);
//		}

		// called when the local player object has been set up
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


		[Client]
		private void StartListening()
		{
			if (!isLocalPlayer)
				return;

//			Debug.Log("NetworkFighter.StartListening: " + PlayerNumber + " / " + PlayerName + " isLocalPlayer = " + isLocalPlayer);

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

//			FightManager.OnNextRound += NextRound;
			FightManager.OnNetworkReadyToFight += ReadyToFight;
			FightManager.OnQuitFight += QuitFight;
//			FightManager.OnFightPaused += PauseFight;
		}

		[Client]
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

//			FightManager.OnNextRound -= NextRound;
			FightManager.OnNetworkReadyToFight -= ReadyToFight;
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

	
		[Client]
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


		// FighterSelect.OnFighterSelected
		[Client]
		private void FighterSelected(Fighter fighter)
		{
			if (!isLocalPlayer)
				return;
			
//			Debug.Log("FighterSelected: IsPlayer1 = " + IsPlayer1 + " : " + fighter.FighterName + " / " + fighter.ColourScheme);
			CmdSetFighter(IsPlayer1, fighter.FighterName, fighter.ColourScheme);
		}
			
		[Command]
		// called from client, runs on server
		public void CmdSetFighter(bool isPlayer1, string name, string colour)
		{
			if (!isServer)
				return;

			networkFightManager.SetFighter(isPlayer1, name, colour);		// starts fight (rpc) if all set
		}


		// WorldMap.OnLocationSelected
		[Client]
		private void LocationSelected(string location)
		{
			if (!isLocalPlayer)
				return;
			
			CmdSetLocation(IsPlayer1, location);
		}
			
		[Command]
		// called from client, runs on server
		private void CmdSetLocation(bool isPlayer1, string location)
		{
			if (!isServer)
				return;

			networkFightManager.SetLocation(isPlayer1, location);		// starts fight (rpc) if all set (fighters and location)
		}


		[ClientRpc]
		// called on server, runs on clients
		public void RpcStartFight(string fighter1Name, string fighter1Colour, string fighter2Name, string fighter2Colour, string location)
		{
//			Debug.Log("RpcStartFight: isPlayer1 = " + IsPlayer1 + " : " + fighter1Name + "/" + fighter1Colour + " : " + fighter2Name + "/" + fighter2Colour + " : " + location);

			if (IsPlayer1)
				fightManager.StartNetworkArcadeFight(fighter1Name, fighter1Colour, fighter2Name, fighter2Colour, location);
			else
				fightManager.StartNetworkArcadeFight(fighter2Name, fighter2Colour, fighter1Name, fighter1Colour, location);
		}



		// FightManager.OnNetworkReadyToFight
		[Client]
		private void ReadyToFight(bool ready, bool changed)
		{
			if (!isLocalPlayer)
				return;

			CmdReadyToFight(IsPlayer1, ready);
		}

		[Command]
		// called from client, runs on server
		private void CmdReadyToFight(bool isPlayer1, bool ready)
		{
			if (!isServer)
				return;

			networkFightManager.ReadyToFight(isPlayer1, ready);		// sets both fighters (rpc) if both ready
		}


		[ClientRpc]
		// called on server, runs on clients
		public void RpcReadyToFight(bool ready)
		{
			if (!isLocalPlayer)
				return;

			fightManager.ReadyToFight = ready;
		}


//		[ClientRpc]
//		// called on server, runs on clients
//		private void RpcNetworkMessage(NetworkMessageType messageType)
//		{
//			string message = "";
//
//			switch (messageType)
//			{
//				case NetworkMessageType.WaitingToStart:
//					message = IsPlayerReady ? (FightManager.Translate("waitingForOpponent") + " ...") : (FightManager.Translate("opponentReady", false, true));
//					break;
//
//				case NetworkMessageType.FightEnding:
//					message = FightManager.Translate("fightEnding") + " ...";
//					break;
//
//				default:
//					break;
//			}
//
//			fightManager.NetworkMessage(message);		// disabled if null or empty
//		}

		#endregion

		// FightManager.OnNextRound
//		[Client]
//		private void NextRound(int roundNumber)
//		{
////			CmdNextRound(roundNumber);			// check with NetworkFightManager, which will call RpcNextRound if both players ready
//		}
//
//		[Command]
//		// called from client, runs on server
//		private void CmdNextRound(int roundNumber)
//		{
//			if (!isServer)
//				return;
//
//			networkFightManager.ReadyForNextRound(IsPlayer1, roundNumber);
//		}
//			
//		[ClientRpc]
//		// called on server, runs on clients
//		public void RpcNextRound(int roundNumber)
//		{
//			if (!isLocalPlayer)
//				return;
//
//			fightManager.NetworkNextRound();
//		}

		#region quit / pause fight

		// FightManager.OnQuitFight
		[Client]
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
			
		[Client]
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
	public struct FightContenders
	{
		public string Fighter1Name;
		public string Fighter1Colour;

		public string Fighter2Name;
		public string Fighter2Colour;

		public string Location;
	}
}
