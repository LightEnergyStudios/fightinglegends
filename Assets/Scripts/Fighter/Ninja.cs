using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	public class Ninja : Fighter
	{
		private const string NinjaToAll = "You cannot defeat Neko–Ryu Ninjitsu!";

		public override bool HasSpecialExtra
		{
			get { return false; }
		}

		protected override void EndSpecial()
		{
			CompleteMove();		// no special opportunity
		}

		// Ninja cannot counter
//		public override bool HasCounter 
//		{
//			get { return false; }
//		}

		// ninja uses her tutorial punch in place of a counter attack (no counter taunt/trigger etc)
		protected override void CueCounter()
		{
			if (CanContinue || IsBlocking)
				CueContinuation(Move.Tutorial_Punch);
			else
				CueMove(Move.Tutorial_Punch);
		}


		// counter attack chained or continued, so no start (ie. wind-up)
		protected override void CounterAttack(bool chained)
		{
			CurrentMove = Move.Tutorial_Punch;
			CurrentState = State.Tutorial_Punch;

			StartCoroutine(StrikeTravel());

			if (ProfileData.counterWhiff != null)
				AudioSource.PlayClipAtPoint(ProfileData.counterWhiff, Vector3.zero, FightManager.SFXVolume);
		}


		public override bool TutorialPunch(bool continuing)			// AI only, in training mode
		{
			if (fightManager.EitherFighterExpiredState)
				return false;

			if (! CanStrikeLight && ! continuing)
				return false;

			if (! UnderAI)			// ninja uses tutorial punch for a counter attack when not AI (ie. training)
			{
				if (!HasCounterGauge)
					return false;

				DeductGauge(ProfileData.CounterGauge);
			}

			CurrentMove = Move.Tutorial_Punch;
			CurrentState = State.Tutorial_Punch_Start;
			CurrentPriority = Tutorial_Punch_Start_Priority;	

			ResetFrameCounts();		// reset (start of move)

			StartCoroutine(StrikeTravel());
			return true;
		}

		protected override bool Counter(bool continuing)
		{
			return false;		// uses tutorial punch instead
		}


		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
				case "Natalya":
				case "Hoi Lun":
				case "Leoni":
				case "Danjuma":
				case "Jackson":
				case "Alazne":
				case "Shiyang":
				case "Ninja":
				case "Skeletron":
					return NinjaToAll;

				default:
					return "";
			}
		}
	}
}
