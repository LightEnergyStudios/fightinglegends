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

		private const float knockOutCameraTime = 2.0f;
		private const float knockOutCameraDistance = 500.0f;

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

			KOFreeze();			// freeze for effect ... on next frame - a KO hit will freeze until KO feedback ends
		}

		protected override IEnumerator KnockOutCamera()
		{
			StartCoroutine(MoveFightersBack(IsPlayer1));

			var cameraController = Camera.main.GetComponent<CameraController>();
			var targetPosition = new Vector3(transform.localPosition.x + (IsPlayer1 ? -knockOutCameraDistance : knockOutCameraDistance),
																					transform.localPosition.y, transform.localPosition.z);
			yield return StartCoroutine(cameraController.TrackTo(targetPosition, knockOutCameraTime));	// track flying brain
		}

		private IEnumerator MoveFightersBack(bool AIWinner)
		{
			var travelTime = ProfileData.ExpiryTime;

			travelTime /= fightManager.AnimationSpeed; 			// scale travelTime according to animation speed

			var player1 = fightManager.Player1;
			var player2 = fightManager.Player2;
			var	player1Distance = player1.ProfileData.ExpiryDistance;
			var	player2Distance = player2.ProfileData.ExpiryDistance;

			var player1Start = player1.transform.position;
			var player2Start = player2.transform.position;
			var player1Target = new Vector3(player1Start.x + (AIWinner ? -player1Distance : player1Distance), player1Start.y, player1Start.z);
			var player2Target = new Vector3(player2Start.x + (AIWinner ? -player2Distance : player2Distance), player2Start.y, player2Start.z);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / travelTime); 

				player1.transform.position = Vector3.Lerp(player1Start, player1Target, t);
				player2.transform.position = Vector3.Lerp(player2Start, player2Target, t);

				yield return null;
			}
		}


		public override bool TravelOnExpiry { get { return false; } }

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
