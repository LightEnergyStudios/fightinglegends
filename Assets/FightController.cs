using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

namespace FightingLegends
{
	public class FightController : NetworkBehaviour
	{
//
//		public bool FightPaused { get; private set; }
//		public bool FightProzen { get; private set; }
//
//		private int fightFreezeFramesRemaining;
//		public bool countdownFreeze = true;			// if fightFreezeFramesRemaining

		public FightManager fightManager;

		private void FixedUpdate()
		{
			RpcUpdateAnimation();
		}

		[ClientRpc]
		private void RpcUpdateAnimation()
		{
			fightManager.UpdateAnimation();
		}


		public void UpdateAnimation()
		{
//			if (fightManager.FightPaused)
//				return;
//
//			fightManager.AnimationFrameCount++; 
//
//			if (fightManager.FightFrozen)
//			{
//				if (fightManager.countdownFreeze)
//					fightManager.FightFreezeCountdown();
//
//				if (fightManager.HasPlayer1)
//					fightManager.Player1.UpdateAnimation();
//				if (fightManager.HasPlayer2)
//					fightManager.Player2.UpdateAnimation();
//			}
//			else
//			{
//				if (fightManager.HasPlayer1)
//					fightManager.Player1.UpdateAnimation();
//				if (fightManager.HasPlayer2)
//					fightManager.Player2.UpdateAnimation();
//			}
		}
	}
}
