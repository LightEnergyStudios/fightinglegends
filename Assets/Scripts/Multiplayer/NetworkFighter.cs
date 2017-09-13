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
//		public string FighterName;
//		public string FighterColour;

		[SyncVar]
		public int PlayerNumber = 0;		// set via lobby
		[SyncVar]
		public string PlayerName = "";
		[SyncVar]
		public Color PlayerColor = Color.white;

		private bool IsPlayer1
		{
			get
			{
				if (PlayerNumber == 0)
					return isServer;		// LAN host == Player1
				
				return PlayerNumber == 1;	// first to join (game creator) == Player1
			}
		}

		private FightManager fightManager;


		public void Start()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			if (fightManager.MultiPlayerFight && isLocalPlayer)
				StartListeningForInput();
		}

		private void OnDestroy()
		{
			if (fightManager.MultiPlayerFight && isLocalPlayer)
				StopListeningForInput();
		}
			

		// called every Time.fixedDeltaTime seconds
		// 0.0666667 = 1/15 sec
		[ServerCallback]
		private void FixedUpdate()
		{
			if (fightManager.MultiPlayerFight && isServer)
				RpcUpdateAnimation();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcUpdateAnimation()
		{
			if (!isLocalPlayer)
				return;

			fightManager.UpdateAnimation();

//			if (OnAnimationFrame != null)
//				OnAnimationFrame();
		}

	
		private void StartListeningForInput()
		{
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
		}

		private void StopListeningForInput()
		{
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
		}

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

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSingleFingerTap(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SingleFingerTap();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SingleFingerTap();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcHoldDown(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.HoldDown();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.HoldDown();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcHoldRelease(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.HoldRelease();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.HoldRelease();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeLeft(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SwipeLeft();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SwipeLeft();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeRight(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SwipeRight();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SwipeRight();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeLeftRight(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SwipeLeftRight();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SwipeLeftRight();

		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeDown(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SwipeDown();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SwipeDown();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSwipeUp(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.SwipeUp();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.SwipeUp();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcTwoFingerTap(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.TwoFingerTap();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.TwoFingerTap();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcFingerTouch(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.FingerTouch();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.FingerTouch();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcFingerRelease(bool player1)
		{
			if (! FightManager.SavedGameStatus.FightInProgress)
				return;

			if (player1 && fightManager.HasPlayer1)
				fightManager.Player1.FingerRelease();
			else if (fightManager.HasPlayer2)
				fightManager.Player2.FingerRelease();
		}

		private void BackToLobby()
		{
			FightManager.SwitchToLobby();
//			FindObjectOfType<NetworkLobbyManager>().ServerReturnToLobby();
		}

	}
}
