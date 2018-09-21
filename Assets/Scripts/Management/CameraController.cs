using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class CameraController : MonoBehaviour
	{
		public bool TrackFighters = true;
		public float FighterTrackSpeed;		// speed at which camera moves to centre of fighters

		public bool TrackingLag;			// camera tracking follows fighter positions TrackingLagFrames ago
		public int TrackingLagFrames;		// camera tracking follows fighter positions this many frames ago

//		private bool TrackAttacker = true;

		private bool trackingFrozen = false;
		private bool trackingHome = false;

		public bool TrackRight = false;
		public bool TrackLeft = false;

		public float TrackSpeedIncrement;			// on each swipe left / right
		private float trackSpeed = 0.0f;			// distance if tracking left or right

		public bool Tracking { get { return TrackRight || TrackLeft; } }
		public float DistancePanned { get { return transform.position.x; } }

		public bool Shaking { get; private set; }
		public float ShakeTime;			// time taken for camera 'shakes' from centre in or out
		public float ShakeDistance;		// in/out movement
		public float HomeTime;			// time taken for camera to return to original position

		private Vector3 originalPosition;

		private FightManager fightManager;


		private void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();
		}


		private void Start()
		{
			originalPosition = Vector3.zero; // transform.position;
			trackSpeed = 0.0f;

			// force camera aspect ratio to 16:9
			Camera.main.aspect = 16.0f / 9.0f;		// as opposed to screen ratio (eg. iPad)
		}
			

		void Update()
		{
			if (!fightManager.HasPlayer1 || !fightManager.HasPlayer2)
				return;

			if (! fightManager.ReadyToFight)
				return;

			if (FightManager.FightPaused)
				return;

			if (fightManager.PreviewMode)
				return;

			if (trackingFrozen)
				return;

			if (trackingHome)
				return;
			
			var P1Position = fightManager.Player1.transform.position;
			var P2Position = fightManager.Player2.transform.position;

			if (FightManager.FightPaused && (TrackLeft || TrackRight))			// rolling through scenery (demo only)
			{
				var distance = (TrackRight) ? trackSpeed : -trackSpeed;
				var newCameraPosition = new Vector3(transform.position.x + distance, originalPosition.y, originalPosition.z);
				transform.position = newCameraPosition;

				// constantly move the fighters to stay on camera...
				fightManager.Player1.transform.position = new Vector3(P1Position.x + distance, P1Position.y, P1Position.z);
				fightManager.Player2.transform.position = new Vector3(P2Position.x + distance, P2Position.y, P2Position.z);
			}
			else if (TrackFighters && !fightManager.FightFrozen)
			{
				var P1TrackPosition = fightManager.Player1.TrackPosition;
				var P2TrackPosition = fightManager.Player2.TrackPosition;
				var P1LagPosition = fightManager.Player1.LagPosition;
				var P2LagPosition = fightManager.Player2.LagPosition;

				var trackSpeed = FighterTrackSpeed / fightManager.AnimationSpeed;

				if (fightManager.EitherFighterExpiredState)
				{
					bool AIWinner = fightManager.HasPlayer1 && fightManager.Player1.ExpiredState;
					bool survivalLoser = FightManager.CombatMode == FightMode.Survival && AIWinner;	
					bool followLoser = FightManager.CombatMode == FightMode.Arcade || FightManager.CombatMode == FightMode.Training
						|| FightManager.CombatMode == FightMode.Dojo || survivalLoser || fightManager.ChallengeLastInTeam(AIWinner);

					var travelOnExpiry = fightManager.Player1.ExpiredState ? fightManager.Player1.TravelOnExpiry : fightManager.Player2.TravelOnExpiry;

					if (followLoser && travelOnExpiry)
					{
						var koPosition = fightManager.Player1.ExpiredState ? P1Position : P2Position;
						var targetPosition = new Vector3(koPosition.x, originalPosition.y, originalPosition.z);

						transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * trackSpeed);
					}
				}
				else
				{
					// constantly keep camera centred between the two fighters
					var fighterCentre = TrackingLag ? (P1LagPosition + P2LagPosition) / 2 : (P1TrackPosition + P2TrackPosition) / 2;
					var targetPosition = new Vector3(fighterCentre, originalPosition.y, originalPosition.z);

//					Debug.Log("CameraController: startPosition = " + transform.position.x + ", targetPosition = " + targetPosition.x);
					transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * trackSpeed);
				}
			}
		}


		public void FreezeTracking()
		{
			trackingFrozen = true;
		}
			
		public void UnfreezeTracking()
		{
			trackingFrozen = false;
		}


		public void StopTracking()
		{
			TrackLeft = false;
			TrackRight = false;

			trackSpeed = 0.0f;
		}

		public void IncreaseTrackSpeed()
		{
			trackSpeed += TrackSpeedIncrement;
		}

		public void DecreaseTrackSpeed()
		{
			trackSpeed -= TrackSpeedIncrement;

			if (trackSpeed == 0.0f)
				StopTracking();
		}
			

		public void ToggleFighterTracking()
		{
			TrackFighters = !TrackFighters;
		}
			

		public Vector3 SnapshotPosition
		{
			get { return transform.position; }
		}

		public IEnumerator TrackHome(bool selectedLocation, bool warp)
		{
			var startPosition = transform.position;
//			Debug.Log("TrackHome: startPosition = " + startPosition.x + ", originalPosition = " + originalPosition.x);

			if (warp)
			{
				transform.position = originalPosition;
			}
			else if (startPosition.x != originalPosition.x)
			{
				trackingHome = true;
				float t = 0.0f;

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / HomeTime);
					transform.position = Vector3.Lerp(startPosition, originalPosition, t);
					yield return null;
				}

				trackingHome = false;
				StopTracking();
			}

			if (selectedLocation)
				fightManager.LoadSelectedScenery();

			yield return null;
		}

		public IEnumerator TrackTo(Vector3 targetPosition, float trackTime)
		{
			var startPosition = transform.position;

			if (startPosition.x == targetPosition.x)
				yield break;

			trackingHome = true;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / trackTime);
				transform.position = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			trackingHome = false;
			StopTracking();
		}


		// a single 'camera shake' is a complete movement inwards, back to centre, out then inwards back to centre
		public IEnumerator Shake(int numberOfShakes, bool increasing = false)
		{
			if (numberOfShakes <= 0 || Shaking)			// camera shakes become too exaggerated when compounded
				yield break;
			
			Shaking = true;
			float increasingFactor = 0.1f;		
			float shakeDistance = increasing ? ShakeDistance * increasingFactor : ShakeDistance; 		// ever decreasing
//			float shakeTime = ShakeTime * (increasing ? 2.0f : 1.0f); 

			// shorten the distance for each complete shake to simulate an 'exponential' spring effect
			for (int i = 0; i < numberOfShakes; i++)
			{
				if (increasing)
					shakeDistance += (i * increasingFactor);
				else
					shakeDistance /= (i + 1);

				StartCoroutine(ShakeMove(true, shakeDistance, ShakeTime));		// in from centre
				yield return new WaitForSeconds(ShakeTime);

				StartCoroutine(ShakeMove(false, shakeDistance, ShakeTime));		// out back to centre
				yield return new WaitForSeconds(ShakeTime);

				StartCoroutine(ShakeMove(false, shakeDistance, ShakeTime));		// out from centre
				yield return new WaitForSeconds(ShakeTime);

				StartCoroutine(ShakeMove(true, shakeDistance, ShakeTime));		// in back to centre
				yield return new WaitForSeconds(ShakeTime);
			}

			Shaking = false;
		}


		private IEnumerator ShakeMove(bool inWards, float distance, float shakeTime)
		{
			if (! inWards)
				distance = -distance;

			var startPosition = transform.position;
			var targetPosition = new Vector3(transform.position.x, transform.position.y, transform.position.z + distance);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / shakeTime); 	// timeScale of 1.0 == real time

				transform.position = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}
		}

//
//		// a single 'camera shake' is a complete movement left, back to centre, right then left back to centre
//		public IEnumerator ShakeLeftRight(int numberOfShakes, float shakeDistanceFactor, float shakeTimeFactor)
//		{
//			if (numberOfShakes <= 0 || Shaking)			// camera shakes become too exaggerated when compounded
//				yield break;
//
//			Shaking = true;
//			float shakeDistance = ShakeDistance * shakeDistanceFactor; 		// ever decreasing
//			float shakeTime = ShakeTime * shakeTimeFactor; 
//
//			// shorten the distance for each complete shake to simulate an 'exponential' spring effect
//			for (int i = 0; i < numberOfShakes; i++)
//			{
//				shakeDistance /= (i + 1);
//
//				StartCoroutine(ShakeLeftRightMove(true, shakeDistance));		// left from centre
//				yield return new WaitForSeconds(shakeTime);
//
//				StartCoroutine(ShakeLeftRightMove(false, shakeDistance));	// right back to centre
//				yield return new WaitForSeconds(shakeTime);
//
//				StartCoroutine(ShakeLeftRightMove(false, shakeDistance));	// right from centre
//				yield return new WaitForSeconds(shakeTime);
//
//				StartCoroutine(ShakeLeftRightMove(true, shakeDistance));		// left back to centre
//				yield return new WaitForSeconds(shakeTime);
//			}
//
//			Shaking = false;
//		}
//
//
//		private IEnumerator ShakeLeftRightMove(bool left, float distance)
//		{
//			if (! left)
//				distance = -distance;
//
//			var startPosition = transform.position;
//			var targetPosition = new Vector3(transform.position.x + distance, transform.position.y, transform.position.z);
//
//			float t = 0.0f;
//
//			while (t < 1.0f)
//			{
//				t += Time.deltaTime * (Time.timeScale / ShakeTime); 	// timeScale of 1.0 == real time
//
//				transform.position = Vector3.Lerp(startPosition, targetPosition, t);
//				yield return null;
//			}
//		}
	}
}
