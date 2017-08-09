
using System;
using System.Collections;
using UnityEngine;
using Lean;


namespace FightingLegends
{
	// Lean touch 'mediator' which subscribes to the LeanTouch events required by the game
	// and fires corresponding events, which are subscribed to by (non AI) Fighters
	public class GestureListener : MonoBehaviour
	{
		// Delegates (type-safe function pointer with built-in iterator (multi-cast))
		// denote signature of event handlers (implemented by Fighters)
		public delegate void TapAction();			
		public static event TapAction OnTap;

		public delegate void TwoFingerTapAction();			
		public static event TwoFingerTapAction OnTwoFingerTap;

		public delegate void ThreeFingerTapAction();			
		public static event ThreeFingerTapAction OnThreeFingerTap;

		public delegate void FourFingerTapAction();			
		public static event FourFingerTapAction OnFourFingerTap;

		public delegate void SwipeLeftAction();
		public static event SwipeLeftAction OnSwipeLeft;

		public delegate void SwipeRightAction();
		public static event SwipeLeftAction OnSwipeRight;

		public delegate void SwipeLeftRightAction();
		public static event SwipeLeftRightAction OnSwipeLeftRight;

		public delegate void SwipeRightLeftAction();
		public static event SwipeRightLeftAction OnSwipeRightLeft;


		public delegate void SwipeUpAction();
		public static event SwipeUpAction OnSwipeUp;

		public delegate void SwipeDownAction();
		public static event SwipeDownAction OnSwipeDown;

		public delegate void SwipeDownUpAction();
		public static event SwipeDownUpAction OnSwipeDownUp;

		public delegate void SwipeUpDownAction();
		public static event SwipeUpDownAction OnSwipeUpDown;


		public delegate void HoldDownAction();
		public static event HoldDownAction OnHoldStart;

		public delegate void HoldUpAction();
		public static event HoldUpAction OnHoldEnd;


		public delegate void SwipeCountDelegate(int swipeCount);
		public static SwipeCountDelegate OnSwipeCount;


//		public delegate void FingerTouchAction();
		public delegate void FingerTouchAction(Vector3 position);
//		public static event FingerTouchAction OnFingerTouch;
		public static FingerTouchAction OnFingerTouch;

//		public delegate void FingerReleaseAction();
		public delegate void FingerReleaseAction(Vector3 position);
//		public static event FingerReleaseAction OnFingerRelease;
		public static FingerReleaseAction OnFingerRelease;


		public Vector3 LastFingerPosition { get; private set; }

		#region inspector variables

		[Tooltip("Seconds required between a finger down/up for a tap to be registered")] 
		public float TapSeconds = 0.15f;

//		[Tooltip("Max pixels of movement (relative to ScalePixels) allowed within TapSeconds for a tap to be triggered")] 
//		public float TapPixels = 10.0f;		// 10/200 = 5% of screen

		[Tooltip("A drag over SwipePixels within SwipeSeconds is considered a swipe")] 
		public float SwipeSeconds;

		[Tooltip("Seconds a finger must be held down for a HoldDownAction event")] 
		public float HoldSeconds;

		[Tooltip("Pixels of movement (relative to ScalePixels) required within TapSeconds for a drag movement to be recognised")] 
		public float DragPixels;	// 10/200 = 5% of screen

		[Tooltip("Pixels of movement (relative to ScalePixels) required within TapSeconds for a swipe to be triggered")] 
		public float SwipePixels;	// 50/200 = 25% of screen

		[Tooltip("The default DPI that swipe scaling is based on")] 
		public int ScalePixels;		// 50/200 = 25% of screen

		public GameObject touchSparkPrefab; 			// on touch, looping
		public GameObject trailSparkPrefab; 			// moves with drag, looping
		public GameObject ripplePrefab; 				// expands

		public GameObject okSparkPrefab; 				// successful gesture. one time
		public GameObject notOkSparkPrefab; 			// unsuccessful gesture. one time
		public GameObject releaseSparkPrefab; 			// on finger up. one time
		public Camera sparkCamera;						// orthographic (curtain camera)

		private CameraController cameraController;

		#endregion

		private Spark touchSpark; 						// instance
		private Spark trailSpark; 						// instance
		private Spark okSpark; 							// instance
		private Spark okSpark2; 						// instance
		private Spark notOkSpark; 						// instance
		private Spark releaseSpark; 					// instance

		private Ripple ripple; 							// instance

		// used to detect 2-directional swipe (ie. no finger up between swipes)
		private bool dragSwipedLeft;
		private bool dragSwipedRight;
		private bool dragSwipedDown;
		private bool dragSwipedUp;
		private bool dragSwipedLeftRight;
		private bool dragSwipedRightLeft;
		private bool dragSwipedDownUp;
		private bool dragSwipedUpDown;

		private bool dragged = false;
		private bool validSwipe = false;
		private int swipeCount = 0;


		#region gesture sparks

		private bool touching = false;						// true while finger down (playing sparks)

		private const long minTicksBetweenTaps = 2500000;	// 0.25 seconds (10 million ticks per second)
		private long lastTapTicks = 0;						// ticks at time of last tap (GetMouseButtonDown)

		private const float horizSwipeDistance = 800.0f;
		private const float vertSwipeDistance = 500.0f;
		private const float pressBothDistance = 200.0f;

		private const float gestureX = 0; // -300.0f;
		private const float gestureY = -100.0f; // 150.0f;
		private const float randomVariation = 20.0f; // 30.0f;		// +/- (x and y)

		private const float tapTime = 0.06f;				// very quick spark
		private const float holdTime = 2.0f;
		private const float swipeTime = 0.25f;

		#endregion


		// to prevent multiple gesture detection
		private enum GestureType
		{
			None = 0,
			SwipedLeft = 1,
			SwipedRight = 2,
			SwipedLeftRight = 3,
			SwipedRightLeft = 4,
			SwipedUp = 5,
			SwipedDown = 6,
			SwipedDownUp = 7,
			SwipedUpDown = 8,
			SingleFingerTapped = 9,
			TwoFingerTapped = 10,
			ThreeFingerTapped = 11,
			FourFingerTapped = 12,
			HeldDown = 13,
			HeldRelease = 14
		}
				
		private bool fingerDragged = false;						// to prevent a very fast swipe being registered as a tap
		private GestureType gesturedCaptured = GestureType.None;  // reset every animation frame
		public bool InputEnabled = true;

	
		private void Start()
		{
			LeanTouch.Instance.TapThreshold = TapSeconds;
			LeanTouch.Instance.SwipeThreshold = SwipePixels;
			LeanTouch.Instance.HeldThreshold = HoldSeconds;
			LeanTouch.Instance.ReferenceDpi = ScalePixels;

			cameraController = Camera.main.GetComponent<CameraController>();

			LastFingerPosition = Vector2.zero;
			Reset();

			CreateSparks(out touchSpark, out trailSpark, out okSpark, out okSpark2, out notOkSpark, out releaseSpark);
//			CreateRipple();
		}

		private void OnEnable()
		{
			FightManager.OnMoveCuedFeedback += MoveCuedFeedback;
			StartListening(true);
		}

		private void OnDisable()
		{
			FightManager.OnMoveCuedFeedback -= MoveCuedFeedback;
			StopListening();
		}

		private void OnDestroy()
		{
			if (touchSpark != null)
				Destroy(touchSpark.gameObject); 
			if (trailSpark != null)
				Destroy(trailSpark.gameObject); 
			if (okSpark != null)
				Destroy(okSpark.gameObject); 
			if (okSpark2 != null)
				Destroy(okSpark2.gameObject); 
			if (notOkSpark != null)
				Destroy(notOkSpark.gameObject);
			if (ripple != null)
				Destroy(ripple.gameObject);  
		}

		// create / move touch / trail sparks (handled separately from Lean events for clarity)
		// mouse detection sufficient for spark visual effects - translates to touch on a device
		private void Update()
		{
			if (! InputEnabled)
				return;

			Vector3 touchPoint = Vector3.zero;

			if (Input.GetMouseButtonDown(0) || Input.GetMouseButton(0))		// down or holding
			{
				touchPoint = sparkCamera.ScreenToWorldPoint(Input.mousePosition);
				touchPoint.z = 50.0f; 	// to drop from camera height to game level
			
				// sparks tracks finger
				touchSpark.transform.position = touchPoint;
				trailSpark.transform.position = touchPoint;
			}
				
			if (Input.GetMouseButtonDown(0))	
			{
				var ticks = DateTime.Now.Ticks;

				if (ticks - lastTapTicks > minTicksBetweenTaps)
				{
					touching = true;
					lastTapTicks = ticks;

					touchSpark.Play();
					trailSpark.Play();

//					if (! FightManager.SavedStatus.CompletedBasicTraining)
//						TriggerRipple(touchPoint, false);		// touchPoint already adjusted for camera position
				}
			}
			else if (Input.GetMouseButtonUp(0))
			{
				touchPoint = sparkCamera.ScreenToWorldPoint(Input.mousePosition);
				touchPoint.z = 50.0f; 	// to drop from camera height to game level

				releaseSpark.transform.position = touchPoint;
				releaseSpark.Play();		// one time

				touchSpark.Stop();
				trailSpark.Stop();

				touching = false;
			}
		}

		private void FixedUpdate()
		{
			gesturedCaptured = GestureType.None;		// reset
		}


		#region simulated gestures (sparks)

		public IEnumerator FeedbackFXSparks(FeedbackFXType fxType)
		{
			if (touching)
				yield break;

			switch (fxType)
			{
				case FeedbackFXType.Press:
					{
						var tapPosition = new Vector3(gestureX, gestureY, 50);
						tapPosition = RandomisePoint(tapPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, tapPosition, tapPosition, tapTime, 0));
//						TriggerRipple(tapPosition);
						break;
					}

				case FeedbackFXType.Hold:
					{
						var holdPosition = new Vector3(gestureX, gestureY, 50);
						holdPosition = RandomisePoint(holdPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, holdPosition, holdPosition, holdTime, 0));
//						TriggerRipple(holdPosition);
						break;
					}

				case FeedbackFXType.Swipe_Forward:
					{
						var startPosition = new Vector3(gestureX - horizSwipeDistance / 2.0f, gestureY, 50);
						var endPosition = new Vector3(gestureX + horizSwipeDistance / 2.0f, gestureY, 50);
						startPosition = RandomisePoint(startPosition);
						endPosition = RandomisePoint(endPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, startPosition, endPosition, 0, swipeTime));
						break;
					}

				case FeedbackFXType.Swipe_Back:
					{
						var startPosition = new Vector3(gestureX + horizSwipeDistance / 2.0f, gestureY, 50);
						var endPosition = new Vector3(gestureX - horizSwipeDistance / 2.0f, gestureY, 50);
						startPosition = RandomisePoint(startPosition);
						endPosition = RandomisePoint(endPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, startPosition, endPosition, 0, swipeTime));
						break;
					}

				case FeedbackFXType.Swipe_Up:
					{
						var startPosition = new Vector3(gestureX, gestureY - vertSwipeDistance, 50);
						var endPosition = new Vector3(gestureX, gestureY, 50);
						startPosition = RandomisePoint(startPosition);
						endPosition = RandomisePoint(endPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, startPosition, endPosition, 0, swipeTime));
						break;
					}

				case FeedbackFXType.Swipe_Down:
					{
						var startPosition = new Vector3(gestureX, gestureY, 50);
						var endPosition = new Vector3(gestureX, gestureY - vertSwipeDistance, 50);
						startPosition = RandomisePoint(startPosition);
						endPosition = RandomisePoint(endPosition);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, startPosition, endPosition, 0, swipeTime));
						break;
					}

				case FeedbackFXType.Swipe_Vengeance:
					{
						var startPosition = new Vector3(gestureX + horizSwipeDistance / 2.0f, gestureY, 50);
						var endPosition = new Vector3(gestureX - horizSwipeDistance / 2.0f, gestureY, 50);
						startPosition = RandomisePoint(startPosition);
						endPosition = RandomisePoint(endPosition);
						var returnPosition = RandomisePoint(startPosition);

						yield return StartCoroutine(GestureSparks(touchSpark, trailSpark, null, startPosition, endPosition, 0, swipeTime));
						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, endPosition, returnPosition, 0, swipeTime));
						break;
					}

				case FeedbackFXType.Press_Both:
					{
						var tap1Position = new Vector3(gestureX + pressBothDistance, gestureY + pressBothDistance, 50);
						var tap2Position = new Vector3(gestureX - pressBothDistance, gestureY - pressBothDistance, 50);
						tap1Position = RandomisePoint(tap1Position);
						tap2Position = RandomisePoint(tap2Position);

						// instantiate a second set of sparks for roman cancel two fingers
						Spark touchSpark2;
						Spark trailSpark2;
						CreateSparks(out touchSpark2, out trailSpark2);

						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, tap1Position, tap1Position, tapTime, 0));
						StartCoroutine(GestureSparks(touchSpark2, trailSpark2, okSpark2, tap2Position, tap2Position, tapTime, 0));

						// destroy the second sparks on expiry
						var particleLifetime = trailSpark2.GetComponent<ParticleSystem>().startLifetime;
						yield return new WaitForSeconds(tapTime + particleLifetime);
						Destroy(touchSpark2);
						Destroy(trailSpark2);
						break;
					}

				case FeedbackFXType.Mash:
					{
						var tapPosition = new Vector3(gestureX, gestureY, 50);
						tapPosition = RandomisePoint(tapPosition);

						// 3 taps req'd for fire element special extra
//						TriggerRipple(tapPosition);
						yield return StartCoroutine(GestureSparks(touchSpark, trailSpark, null, tapPosition, tapPosition, tapTime, 0));
//						TriggerRipple(tapPosition);
						yield return StartCoroutine(GestureSparks(touchSpark, trailSpark, null, tapPosition, tapPosition, tapTime, 0));
//						TriggerRipple(tapPosition);
						StartCoroutine(GestureSparks(touchSpark, trailSpark, okSpark, tapPosition, tapPosition, 0.1f, 0));
						break;
					}

				default:
					break;
			}
		}
			
		private void CreateSparks(out Spark touchSpark, out Spark trailSpark)
		{
			var touchSparkObject = Instantiate(touchSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			var trailSparkObject = Instantiate(trailSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;

			touchSpark = touchSparkObject.GetComponent<Spark>();
			trailSpark = trailSparkObject.GetComponent<Spark>();
		}

		private void CreateSparks(out Spark touchSpark, out Spark trailSpark, out Spark okSpark, out Spark okSpark2, out Spark notOkSpark, out Spark releaseSpark)
		{
			CreateSparks(out touchSpark, out trailSpark);

			var okSparkObject = Instantiate(okSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			var okSpark2Object = Instantiate(okSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			var notOkSparkObject = Instantiate(notOkSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;
			var releaseSparkObject = Instantiate(releaseSparkPrefab, Vector3.zero, Quaternion.identity) as GameObject;

			okSpark = okSparkObject.GetComponent<Spark>();
			okSpark2 = okSpark2Object.GetComponent<Spark>();
			notOkSpark = notOkSparkObject.GetComponent<Spark>();
			releaseSpark = releaseSparkObject.GetComponent<Spark>();
		}

		private Vector3 RandomisePoint(Vector3 point)
		{
			float randomX = UnityEngine.Random.Range(-randomVariation, randomVariation);
			float randomY = UnityEngine.Random.Range(-randomVariation, randomVariation);

			return new Vector3(point.x + randomX, point.y + randomY, point.z);
		}
			
		private IEnumerator GestureSparks(Spark touchSpark, Spark trailSpark, Spark okSpark, Vector3 startPosition, Vector3 endPosition, float holdTime, float moveTime)
		{
			var startPoint = startPosition + cameraController.SnapshotPosition;
			var endPoint = endPosition + cameraController.SnapshotPosition;

			touchSpark.transform.position = startPoint;
			trailSpark.transform.position = startPoint;

			touchSpark.Play();		// looping
			trailSpark.Play();		// looping

			yield return new WaitForSeconds(holdTime);

			if (moveTime > 0)
			{
				float t = 0;

				while (t < 1.0f)
				{
					if (touching)
					{
						touchSpark.Stop();
						trailSpark.Stop();
						yield break;
					}
					
					t += Time.deltaTime * (Time.timeScale / moveTime);
					
					touchSpark.transform.position = Vector3.Lerp(startPoint, endPoint, t);
					trailSpark.transform.position = Vector3.Lerp(startPoint, endPoint, t);
					yield return null;
				}
			}

			if (okSpark != null)
			{
				okSpark.transform.position = endPoint;
				okSpark.Play();
			}

			touchSpark.Stop();
			trailSpark.Stop();

			yield return null;
		}


//		private void CreateRipple()
//		{
//			var rippleObject = Instantiate(ripplePrefab, Vector3.zero, Quaternion.identity) as GameObject;
//			ripple = rippleObject.GetComponent<Ripple>();
//		}
//
//		private void TriggerRipple(Vector3 position, bool adjustToCamera = true)
//		{
//			if (adjustToCamera)
//				position += cameraController.SnapshotPosition;
//			
//			ripple.Expand(position, false);
//		}

		#endregion


		#region LeanTouch event handlers

		public void StartListening(bool force = false)
		{
			if (InputEnabled && ! force)
				return;
			
//			LeanTouch.OnFingerTap += OnFingerTap;		// ripple

			// fired when at least one finger taps the screen (int = Highest Finger Count) - when all fingers are released
			LeanTouch.OnMultiTap += OnMultiFingerTap;

			// fired when a finger swipes the screen (when a finger starts and stops touching the screen within the 'TapThreshold' time, and also moves more than the 'SwipeThreshold' distance) (LeanFinger = The current finger)
			LeanTouch.OnFingerSwipe += OnFingerSwipe;

			// fired when a finger moves across the screen (LeanFinger = The current finger)
			LeanTouch.OnFingerDrag += OnFingerDrag;

			// fired when a finger begins touching the screen (LeanFinger = The current finger)
			LeanTouch.OnFingerDown += OnFingerDown;

			// fired when a finger stops touching the screen (LeanFinger = The current finger)
			LeanTouch.OnFingerUp += OnFingerUp;

			// fired when a finger begins being held on the screen (when a finger has been set for longer than the 'HeldThreshold' time) (LeanFinger = The current finger)
			LeanTouch.OnFingerHeldDown += OnHoldDown;

			// fired when a finger stops being held on the screen (when a finger has been set for longer than the 'HeldThreshold' time) (LeanFinger = The current finger)
			LeanTouch.OnFingerHeldUp += OnHoldRelease;

			InputEnabled = true;
		}

		public void StopListening()
		{
			if (! InputEnabled)
				return;
			
//			LeanTouch.OnFingerTap -= OnFingerTap;		// ripple

			LeanTouch.OnMultiTap -= OnMultiFingerTap;

			LeanTouch.OnFingerSwipe -= OnFingerSwipe;
			LeanTouch.OnFingerDrag -= OnFingerDrag;

			LeanTouch.OnFingerDown -= OnFingerDown;
			LeanTouch.OnFingerUp -= OnFingerUp;

			LeanTouch.OnFingerHeldDown -= OnHoldDown;
			LeanTouch.OnFingerHeldUp -= OnHoldRelease;

			InputEnabled = false;
		}

		#endregion  // LeanTouch event handlers


		private void Reset()
		{
			ResetDrag();			// used for 2-directional swipe detection
			gesturedCaptured = GestureType.None;

			dragged = false;
			swipeCount = 0;
		}

		// played if move cued as a result of gesture
		private void PlayOkSpark(Vector2 position)
		{
			okSpark.transform.position = new Vector3(position.x, position.y, 50.0f);
			okSpark.Play();
		}

		// played if no move cued as a result of gesture
		private void PlayNotOkSpark(Vector2 position)
		{
			notOkSpark.transform.position = new Vector3(position.x, position.y, 50.0f);
			notOkSpark.Play();
		}

		#region fire events for subscribers


		// true if finger was not dragged more than TapPixels within TapSeconds
//		private bool WasTapped(LeanFinger finger)
//		{
//			bool tapped = finger.GetScaledSnapshotDelta(SwipeSeconds).magnitude < TapPixels;
//			return tapped;
//		}

		private void OnMultiFingerTap(int highestFingerCount)
		{
			if (! InputEnabled)
				return;

//			if (! WasTapped)
//				return;

			if (fingerDragged)
			{
//				Debug.Log("OnMultiFingerTap: fingerDragged = " + fingerDragged);
				return;
			}

			if (highestFingerCount == 1)
			{
				if (gesturedCaptured != GestureType.None)
					return;
				
				gesturedCaptured = GestureType.SingleFingerTapped;

				if (OnTap != null)		// error if an event with no subscribers is invoked
					OnTap();			// fire event
			}
			else if (highestFingerCount == 2)
			{
				// NOTE: 2 finger tap translates to Roman Cancel, which overrides any previous gestures
				gesturedCaptured = GestureType.TwoFingerTapped;

				if (OnTwoFingerTap != null)		// error if an event with no subscribers is invoked
					OnTwoFingerTap();
			}
			else if (highestFingerCount == 3)
			{
				if (gesturedCaptured != GestureType.None)
					return;
				
				gesturedCaptured = GestureType.ThreeFingerTapped;

				if (OnThreeFingerTap != null)	// error if an event with no subscribers is invoked
					OnThreeFingerTap();
			}
			else if (highestFingerCount == 4)
			{
				if (gesturedCaptured != GestureType.None)
					return;

				gesturedCaptured = GestureType.FourFingerTapped;

				if (OnFourFingerTap != null)	// error if an event with no subscribers is invoked
					OnFourFingerTap();
			}
		}
			

		private void OnFingerSwipe(LeanFinger finger)
		{
			if (! InputEnabled)
				return;

			if (gesturedCaptured != GestureType.None)
				return;

			if (! WasSwiped(finger))
				return;
			
			// Store the swipe delta in a temp variable
			var swipe = finger.SwipeDelta;

			if (swipe.x < -Mathf.Abs(swipe.y))			// left
			{
				gesturedCaptured = GestureType.SwipedLeft;
				validSwipe = true;
				swipeCount++;

				if (OnSwipeCount != null)
					OnSwipeCount(swipeCount);	

				if (OnSwipeLeft != null)	// error if an event with no subscribers is invoked
					OnSwipeLeft();				
			}
			else if (swipe.x > Mathf.Abs(swipe.y))		// right
			{
				gesturedCaptured = GestureType.SwipedRight;
				validSwipe = true;
				swipeCount++;

				if (OnSwipeCount != null)
					OnSwipeCount(swipeCount);	

				if (OnSwipeRight != null)	// error if an event with no subscribers is invoked
					OnSwipeRight();			
			}
			else if (swipe.y < -Mathf.Abs(swipe.x))		// down
			{
				gesturedCaptured = GestureType.SwipedDown;
				validSwipe = true;
				swipeCount++;

				if (OnSwipeCount != null)
					OnSwipeCount(swipeCount);	

				if (OnSwipeDown != null)	// error if an event with no subscribers is invoked
					OnSwipeDown();
			}
			else if (swipe.y > Mathf.Abs(swipe.x))		// up
			{
				gesturedCaptured = GestureType.SwipedUp;
				validSwipe = true;
				swipeCount++;

				if (OnSwipeCount != null)
					OnSwipeCount(swipeCount);	

				if (OnSwipeUp != null)	// error if an event with no subscribers is invoked
					OnSwipeUp();
			}
		}


		private void OnHoldDown(LeanFinger finger)
		{
			if (! InputEnabled)
				return;

			if (gesturedCaptured != GestureType.None)		
				return;

			// was finger dragged far enough and fast enough to be considered a swipe?
			if (WasSwiped(finger))
				return;

			gesturedCaptured = GestureType.HeldDown;

			if (OnHoldStart != null)
				OnHoldStart();			// block idle start
		}

		private void OnHoldRelease(LeanFinger finger)
		{
			if (gesturedCaptured != GestureType.HeldDown)
				return;

			gesturedCaptured = GestureType.HeldRelease;

			if (OnHoldEnd != null)
				OnHoldEnd();			// block idle end
		}


		// true if finger was dragged far enough within SwipeSeconds to be considered a swipe
		private bool WasSwiped(LeanFinger finger)
		{
			bool swiped = finger.GetScaledSnapshotDelta(SwipeSeconds).magnitude >= SwipePixels;

			// not valid if too quick - ie. within TapSeconds
//			if (swiped && finger.GetScaledSnapshotDelta(TapSeconds).magnitude >= SwipePixels)
//				swiped = false;
			
			return swiped;
		}

		// true if finger was dragged far enough within TapSeconds to be considered a deliberate movement
		private bool WasDragged(LeanFinger finger)
		{
//			return finger.GetScaledSnapshotDelta(SwipeSeconds).magnitude >= DragPixels;
			fingerDragged = finger.GetScaledSnapshotDelta(TapSeconds).magnitude >= DragPixels;	// TODO: was SwipeSeconds  review all this
			return fingerDragged;
		}
			
		#endregion
			

		#region 2-directional swipe detection

		private void OnFingerDrag(LeanFinger finger)
		{
			if (! InputEnabled)
				return;

			if (gesturedCaptured != GestureType.None)		// already captured a gesture
				return;

			// was finger dragged far enough and fast enough to be considered a drag?
			if (! WasDragged(finger))
				return;

			dragged = true;
			var swipe = finger.SwipeDelta;

			if (swipe.x < -Mathf.Abs(swipe.y))
			{
				if (dragSwipedRight)
					dragSwipedRightLeft = true;		// left-right swipe event fired by OnFingerUp;
				else
					dragSwipedLeft = true;			// left-right swipe event fired by OnFingerUp
			}

			else if (swipe.x > Mathf.Abs(swipe.y))
			{
				if (dragSwipedLeft)
					dragSwipedLeftRight = true;		// left-right swipe event fired by OnFingerUp;
				else
					dragSwipedRight = true;		// left-right swipe event fired by OnFingerUp
			}

			else if (swipe.y < -Mathf.Abs(swipe.x))
			{
				if (dragSwipedUp)
					dragSwipedUpDown = true;		// left-right swipe event fired by OnFingerUp;
				else
					dragSwipedDown = true;		// left-right swipe event fired by OnFingerUp;
			}

			else if (swipe.y > Mathf.Abs(swipe.x))
			{
				if (dragSwipedDown)
					dragSwipedDownUp = true;		// left-right swipe event fired by OnFingerUp;
				else
					dragSwipedUp = true;		// left-right swipe event fired by OnFingerUp
			}
		}


		private void OnFingerDown(LeanFinger finger)
		{
			if (! InputEnabled)
				return;

			SaveFingerPosition(finger);

			fingerDragged = false;
//			Debug.Log("OnFingerDown");

			if (OnFingerTouch != null)
				OnFingerTouch(LastFingerPosition);	

			Reset();
		}

		private void OnFingerUp(LeanFinger finger)
		{
			if (! InputEnabled)
				return;

			fingerDragged = finger.GetScaledSnapshotDelta(TapSeconds).magnitude >= DragPixels;	// TODO: was SwipeSeconds  review all this

//			if (fingerDragged)
//				Debug.Log("OnFingerUp: fingerDragged = " + fingerDragged);

			if (gesturedCaptured == GestureType.None)
			{
				if (dragSwipedLeftRight)
				{
					gesturedCaptured = GestureType.SwipedLeftRight;
					validSwipe = true;

					if (OnSwipeLeftRight != null)	// error if an event with no subscribers is invoked
						OnSwipeLeftRight();

					ResetDrag();
				}
				else if (dragSwipedRightLeft)
				{
					gesturedCaptured = GestureType.SwipedRightLeft;

					if (OnSwipeRightLeft != null)	// error if an event with no subscribers is invoked
						OnSwipeRightLeft();

					ResetDrag();
				}
				else if (dragSwipedDownUp)
				{
					gesturedCaptured = GestureType.SwipedDownUp;

					if (OnSwipeDownUp != null)	// error if an event with no subscribers is invoked
						OnSwipeDownUp();

					ResetDrag();
				}
				else if (dragSwipedUpDown)
				{
					gesturedCaptured = GestureType.SwipedUpDown;

					if (OnSwipeUpDown != null)	// error if an event with no subscribers is invoked
						OnSwipeUpDown();

					ResetDrag();
				}
			}
				
			SaveFingerPosition(finger);

			if (OnFingerRelease != null)
				OnFingerRelease(LastFingerPosition);	

			validSwipe = false;
			dragged = false;
			swipeCount = 0;
		}


		private void MoveCuedFeedback(bool ok, Vector3 position)
		{
			if (ok)
				PlayOkSpark(position);
			else
				PlayNotOkSpark(position);
		}
			
		private Vector3 SaveFingerPosition(LeanFinger finger)
		{
			LastFingerPosition = sparkCamera.ScreenToWorldPoint(new Vector3(finger.ScreenPosition.x, finger.ScreenPosition.y, 50.0f));
			return LastFingerPosition;
		}

//		public Vector2 FirstFingerPosition
//		{
//			get
//			{
//				var fingers = LeanTouch.Fingers;
//
//				if (fingers.Count > 0)
////					return fingers[0].ScreenPosition;
//					return FingerPosition(fingers[0]);
//
//				return Vector3.zero;
//			}
//		}
			
		private void ResetDrag()
		{
			dragSwipedLeft = false;
			dragSwipedRight = false;
			dragSwipedUp = false;
			dragSwipedDown = false;

			dragSwipedLeftRight = false;
			dragSwipedRightLeft = false;
			dragSwipedDownUp = false;
			dragSwipedUpDown = false;
		}
			
		#endregion
	}
}
