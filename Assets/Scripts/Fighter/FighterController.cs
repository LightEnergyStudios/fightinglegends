using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using UnityEngine.Networking;

namespace FightingLegends
{
	public class FighterController : NetworkBehaviour
	{
		private Fighter Fighter;

		void Start()
		{
			if (isServer)
				AttachToFighter(FightManager.Player1);
			else if (isLocalPlayer)
				AttachToFighter(FightManager.Player2);
		}

		private void AttachToFighter(Fighter fighter)
		{
			if (fighter == null)
				return;
			
			if (Fighter != null)
				StopListeningForInput();

			Fighter = fighter;

			StartListeningForInput();
		}

		private void StartListeningForInput()
		{
			GestureListener.OnTap += Fighter.SingleFingerTap;				// strike		
			GestureListener.OnHoldStart += Fighter.HoldDown;				// start block	
			GestureListener.OnHoldEnd += Fighter.HoldRelease;				// end block
			GestureListener.OnSwipeLeft += Fighter.SwipeLeft;				// counter
			GestureListener.OnSwipeRight += Fighter.SwipeRight;				// special
			GestureListener.OnSwipeLeftRight += Fighter.SwipeLeftRight;		// vengeance
			GestureListener.OnSwipeDown += Fighter.SwipeDown;				// shove
			GestureListener.OnSwipeUp += Fighter.SwipeUp;					// power up

			GestureListener.OnTwoFingerTap += Fighter.TwoFingerTap;			// roman cancel		

			GestureListener.OnFingerTouch += Fighter.FingerTouch;			// reset moveCuedOk
			GestureListener.OnFingerRelease += Fighter.FingerRelease;		// to ensure block released
		}

		private void StopListeningForInput()
		{
			GestureListener.OnTap -= Fighter.SingleFingerTap;		

			GestureListener.OnHoldStart -= Fighter.HoldDown;		
			GestureListener.OnHoldEnd -= Fighter.HoldRelease;		
			GestureListener.OnSwipeLeft -= Fighter.SwipeLeft;
			GestureListener.OnSwipeRight -= Fighter.SwipeRight;
			GestureListener.OnSwipeLeftRight -= Fighter.SwipeLeftRight;
			GestureListener.OnSwipeDown -= Fighter.SwipeDown;
			GestureListener.OnSwipeUp -= Fighter.SwipeUp;

			GestureListener.OnTwoFingerTap -= Fighter.TwoFingerTap;	

			GestureListener.OnFingerTouch -= Fighter.FingerTouch;			
			GestureListener.OnFingerRelease -= Fighter.FingerRelease;
		}
	}
}
