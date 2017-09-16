using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;

namespace FightingLegends
{
	// network player spawned by NetworkManager
	// in LAN environment server (host) represents player1, client represents player2
	// in WAN environment player number passed through from lobby player - game creator is player1
	public class NetworkFighter : NetworkBehaviour
	{
		// set via lobby (hook)
		public int PlayerNumber = 0;	
	
		// fighters set via FighterSelect delegate - server only
		private static string Fighter1Name;
		private static string Fighter1Colour;
		private static string Fighter2Name;
		private static string Fighter2Colour;

		// location set via WorldMap delegate - server only
		private static string SelectedLocation;
	

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

		public void Start()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			if (PlayerNumber == 0)		// ie. not set via lobby (game creator == Player1)
				PlayerNumber = isServer ? 1 : 2;
			
			if (fightManager.NetworkFight && isLocalPlayer)
				StartListening();
		}

		private void OnDestroy()
		{
			if (fightManager.NetworkFight && isLocalPlayer)
				StopListening();
		}

		private void StartListening()
		{
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
			FightManager.OnFightPaused += PauseFight;
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
			FightManager.OnFightPaused -= PauseFight;
		}
			

		// called every Time.fixedDeltaTime seconds
		// 0.0666667 = 1/15 sec
		[ServerCallback]
		private void FixedUpdate()
		{
			if (fightManager.NetworkFight && isServer)
				RpcUpdateAnimation();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcUpdateAnimation()
		{
			if (!isLocalPlayer)
				return;

			fightManager.UpdateAnimation();
		}
	

		#region fight construction

		private bool CanStartFight { get { return !string.IsNullOrEmpty(Fighter1Name) && !string.IsNullOrEmpty(Fighter1Colour) &&
													!string.IsNullOrEmpty(Fighter2Name) && !string.IsNullOrEmpty(Fighter2Colour) &&
													!string.IsNullOrEmpty(SelectedLocation); }}


		// FighterSelect event handler
		private void FighterSelected(Fighter fighter)
		{
			if (isLocalPlayer)
				CmdSetFighter(IsPlayer1, fighter.FighterName, fighter.ColourScheme);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSetFighter(bool player1, string fighterName, string fighterColour)
		{
			if (!isServer)
				return;

			if (player1)
			{
				Fighter1Name = fighterName;	
				Fighter1Colour = fighterColour;	
			}
			else
			{
				Fighter2Name = fighterName;	
				Fighter2Colour = fighterColour;	
			}

			TryStartFight();		// if both fighters and location set
		}


		private void LocationSelected(string location)
		{
			if (isLocalPlayer)
				CmdSetLocation(location);	
		}

		[Command]
		// called from client, runs on server
		private void CmdSetLocation(string location)
		{
			if (!isServer)
				return;

			SelectedLocation = location;

			TryStartFight(); 		// if both fighters and location set
		}


		private void TryStartFight()
		{
			if (!isServer)
				return;
			
			if (CanStartFight)
			{
				RpcStartFight(Fighter1Name, Fighter1Colour, Fighter2Name, Fighter2Colour, SelectedLocation);

				// reset for next fight
				Fighter1Name = null;
				Fighter1Colour = null;
				Fighter2Name = null;
				Fighter2Colour = null;
				SelectedLocation = null;
			}
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
			fightManager.QuitNetworkFight();
		}

		private void PauseFight(bool paused)
		{
			if (isLocalPlayer)
				CmdPauseFight(paused);
		}

		[Command]
		// called from client, runs on server
		private void CmdPauseFight(bool paused)
		{
			if (!isServer)
				return;

			RpcPauseFight(paused);
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcPauseFight(bool paused)
		{
			fightManager.PauseNetworkFight(paused);
		}



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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SingleFingerTap();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SingleFingerTap();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.HoldDown();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.HoldDown();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.HoldRelease();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.HoldRelease();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SwipeLeft();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SwipeLeft();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SwipeRight();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SwipeRight();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SwipeLeftRight();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SwipeLeftRight();

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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SwipeDown();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SwipeDown();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.SwipeUp();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.SwipeUp();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.TwoFingerTap();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.TwoFingerTap();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.FingerTouch();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.FingerTouch();
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

//			if (player1 && fightManager.HasPlayer1)
//				fightManager.Player1.FingerRelease();
//			else if (fightManager.HasPlayer2)
//				fightManager.Player2.FingerRelease();
		}

		#endregion

		private void BackToLobby()
		{
			FightManager.SwitchToLobby();
//			FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
		}

	}
}
