using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class Skeletron : Fighter
	{
		private const string SkeletronToLeoni = "I cannot show mercy. Even to a slip of a girl like you.";
		private const string SkeletronToShiro = "You may as well pound those tiny fists against the ground. You are nothing.";
		private const string SkeletronToNatalya = "You represent the military but you lack the will to kill? Pathetic!";
		private const string SkeletronToDanjuma = "Interesting... I almost felt that.";
		private const string SkeletronToHoiLun = "Spare the rod... spoil the child.";
		private const string SkeletronToJackson = "You are a relic from an outdated time. Die now, so that a new order can rise.";
		private const string SkeletronToShiyang = "The age of humanity and compassion has long passed. Mankind will kneel before the god of science.";
		private const string SkeletronToAlazne = "In a world where people can only obey, the police will be redundant. Begone!";
		private const string SkeletronToNinja = "Nuisance creature. I don't hesitate to slaughter dumb animals.";
		private const string SkeletronToSkeletron = "You may imitate, but you cannot compare to my power!";

		public float IdleDamagedHealth;		// idle damaged state when health falls below this value


		public override bool HasShove
		{
			get { return false; }
		}

		protected override bool CanBeShoved
		{
			get { return false; }
		}

		public override bool HasBlock  
		{
			get { return false; }
		}

		public override bool HasSpecialExtra
		{
			get { return false; }
		}

		protected override void IdleState()
		{
			CurrentMove = Move.Idle;
			CurrentPriority = Default_Priority;			// fires state change event

			if (ProfileData.SavedData.Health > IdleDamagedHealth)
				CurrentState = State.Idle;				// fires state change event
			else
				CurrentState = State.Idle_Damaged;		// fires state change event
		}


		protected override void EndSpecial()
		{
			// no special opportunity...
			CompleteMove();		// straight back to idle
		}

		protected override void EndCounterTaunt()			// end of state - not struck - back to idle
		{
			CompleteMove();	
		}

		protected override void EndIdleDamaged()
		{
			CurrentState = State.Falling;
		}

		protected override void EndFalling()
		{
			CurrentState = State.Ready_To_Die;
		}

		protected override void EndReadyToDie()
		{
			CurrentState = State.Die;
		}

//		// returns false if damage was fatal
//		public override void CompleteMove()
//		{
//			base.CompleteMove();
//			
//			if (ProfileData.SavedData.Health < IdleDamagedHealth)
//				CurrentState = State.Idle_Damaged;
//		}
			
		protected override void ReadyToKO(HitFrameData hitData)
		{
			TriggerFeedbackFX(FeedbackFXType.None);				// eg. to cancel special extra prompt in case end of special opportunity not reached

			// doesn't die immediately - needs one final strike to finish him
			CurrentState = State.Ready_To_Die;
			ReturnToDefaultDistance();		// default fighting distance

			nextHitWillKO = true;

//			fightManager.TextFeedback(IsPlayer1, "FINISH HIM!!!");		// TODO: remove

//			LastMoveUI = "READY TO DIE... [ " + fightManager.AnimationFrameCount + " ]";
//			Debug.Log(FighterFullName + ": READY TO DIE at [ " + fightManager.AnimationFrameCount + " ]");
		}
			
		protected override void KnockOut()
		{
			base.KnockOut();		// feedback FX + expiry countdown + profile data

//			LastMoveUI = "K.O... [ " + fightManager.AnimationFrameCount + " ]";

			CurrentState = State.Die;

			if (nextHitWillKO)
			{
				nextHitWillKO = false;
				KnockOutFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
			}
		}


		protected override bool TravelOnExpiry { get { return false; } }

		public override bool ExpiredHealth
		{ 
			get { return base.ExpiredHealth && CurrentState != State.Ready_To_Die; }
		}

		public override bool ExpiredState
		{
			get { return CurrentState == State.Die; }
		}


		public override bool StateLoops(string stateLabel)
		{
			if (base.StateLoops(stateLabel))
				return true;
			
			return (stateLabel == "READY_TO_DIE");
		}

		public override string WinQuote(string loserName)
		{
			switch (loserName)
			{
				case "Shiro":
					return SkeletronToShiro;
				case "Natalya":
					return SkeletronToNatalya;
				case "Hoi Lun":
					return SkeletronToHoiLun;
				case "Leoni":
					return SkeletronToLeoni;
				case "Danjuma":
					return SkeletronToDanjuma;
				case "Jackson":
					return SkeletronToJackson;
				case "Alazne":
					return SkeletronToAlazne;
				case "Shiyang":
					return SkeletronToShiyang;
				case "Ninja":
					return SkeletronToNinja;
				case "Skeletron":
					return SkeletronToSkeletron;
				default:
					return "";
			}
		}
	}
}
