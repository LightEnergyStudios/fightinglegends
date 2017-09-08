using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;

namespace FightingLegends
{
	// network player spawned by NetworkManager
	// server (host) represents player1, client represents player2
	public class FighterController : NetworkBehaviour
	{
		private FightManager fightManager;

		public void Start()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			if (FightManager.MultiPlayerFight && isLocalPlayer)
				StartListeningForInput();
		}

		private void OnDestroy()
		{
			if (FightManager.MultiPlayerFight && isLocalPlayer)
				StopListeningForInput();
		}
			

		// called every Time.fixedDeltaTime seconds
		// 0.0666667 = 1/15 sec
		private void FixedUpdate()
		{
			if (FightManager.MultiPlayerFight && isServer)
				RpcUpdateAnimation();
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcUpdateAnimation()
		{
			if (!isLocalPlayer)
				return;
			
			if (fightManager != null)
				fightManager.UpdateAnimation();
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
			if (isLocalPlayer)
				CmdSingleFingerTap(isServer);		// player1 if host, else player2
		}

		[Command]
		// called from client, runs on server
		private void CmdSingleFingerTap(bool player1)
		{
			RpcSingleFingerTap(player1);
		}

		[ClientRpc]
		// called on server, runs on clients
		private void RpcSingleFingerTap(bool player1)
		{
			if (!isLocalPlayer)
				return;

			if (player1)
				fightManager.Player1.SingleFingerTap();
			else
				fightManager.Player2.SingleFingerTap();
		}

		private void HoldDown()
		{

		}

		private void HoldRelease()
		{

		}

		private void SwipeLeft()
		{

		}

		private void SwipeRight()
		{

		}

		private void SwipeLeftRight()
		{

		}

		private void SwipeDown()
		{

		}

		private void SwipeUp()
		{

		}

		private void TwoFingerTap()
		{

		}

		private void FingerTouch(Vector3 position)
		{

		}

		private void FingerRelease(Vector3 position)
		{

		}

	}
}
