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

		public float IdleDamagedHealth;		// idle / block idle damaged state when health falls below this value


		public override bool HasShove
		{
			get { return false; }
		}

		protected override bool CanBeShoved
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

		protected override void BlockIdleState()
		{
			CurrentMove = Move.Block;

			if (ProfileData.SavedData.Health > IdleDamagedHealth)
				CurrentState = State.Block_Idle;				// fires state change event
			else
				CurrentState = State.Block_Idle_Damaged;		// fires state change event
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

		protected override void EndFalling()
		{
			CurrentState = State.Ready_To_Die;
		}
			

		protected override void KOState(HitFrameData hitData)
		{
			TriggerFeedbackFX(FeedbackFXType.None);				// eg. to cancel special extra prompt in case end of special opportunity not reached

			// doesn't die immediately
			UpdateHealth(-1, false);		// barely alive! needs one more hit to finish him (while ready to die)
			takenLastFatalHit = false;

			CurrentState = State.Fall;	// then ready to die

//			Debug.Log(FullName + ": KOState: health = " + ProfileData.SavedData.Health);

			Opponent.ReturnToDefaultDistance();		// default fighting distance
		}
			
		protected override void KnockOut()
		{
			base.KnockOut();		// feedback FX + expiry countdown + profile data
			CurrentState = State.Die;

			KnockOutFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
		}


		protected override bool TravelOnExpiry { get { return false; } }


		protected override bool FallenState	
		{
			get { return CurrentState == State.Fall || CurrentState == State.Ready_To_Die; }
		}

		public override bool ExpiredState
		{
			get { return CurrentState == State.Die; }
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
