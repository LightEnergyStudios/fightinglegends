//using System;
//using System.Collections;
//using Lean;
//using UnityEngine;
//using System.Collections.Generic;
//using Output = System.Diagnostics.Debug;
//using System.Linq;
//
//namespace FightingLegends
//{
//    public class Touch : MonoBehaviour
//    {
//        private class Timer
//        {
//            public float Current { get; private set; }
//            private float Original;
//
//            public Timer(float seconds)
//            {
//                Current = seconds;
//                Original = seconds;
//            }
//
//            public void Reset()
//            {
//                Current = Original;
//            }
//
//            public void Subtract(float seconds)
//            {
//                if (Current <= 0)
//                    return;
//
//                Current = (Current - seconds < 0) ? 0 : Current - seconds;
//            }
//        }
//
//        [HideInInspector]
//        public bool Enabled = true;
//
//        [Tooltip("Time before beginning to block")]
//        public float HoldTimeoutSeconds = 0.1f;
//
//        [Tooltip("Horizontal percentage of the screen to traverse before a swipe is registered")]
//        public float MinSwipeDistanceX = 5f;
//
//        [Tooltip("Vertical percentage of the screen to traverse before a swipe is registered")]
//        public float MinSwipeDistanceY = 10f;
//
//        [Tooltip("Horizontal Percentage of the screen to traverse in order to register a second swipe")]
//        public float MinSwipeReturnDistanceX = 5f;
//
//        [Tooltip("Vertical Percentage of the screen to traverse in order to register a second swipe")]
//        public float MinSwipeReturnDistanceY = 10f;
//
//		private Fighter fighter;
//		private FightManager fightManager;
//        
//		private bool HoldWait = false;
//        private bool Holding = false;
//        private bool Down = false;
//
//        private Timer Timeout;
//
//        private bool EventsAttached;
//
//        public static Touch Instance;
//
//
//        public void Awake()
//        {
//            Instance = this;
//        }
//
//        public void Start()
//        {
//			var fightManagerObject = GameObject.Find("FightManager");
//			fightManager = fightManagerObject.GetComponent<FightManager>();
//
//            fighter = fightManager.Player1;
//
//            Timeout = new Timer(HoldTimeoutSeconds);
//
////			EnableTouch();		
//        }
//
////		private IEnumerator WaitForAnnouncer()
////        {
////            while (!Announcer.instance.AllowInput())
////            {
////                yield return null;
////            }
////
////            while (! fightManager.ReadyToFight)
////            {
////                yield return null;
////            }
////
////            if (!EventsAttached)
////            {
////                LeanTouch.OnFingerDown += OnDown;
////                LeanTouch.OnFingerUp += OnUp;
////                LeanTouch.OnFingerDrag += OnDrag;
////            }
////
////            EventsAttached = true;
////        }
//
//        public void OnDestroy()
//        {
//            DisableTouch();
//        }
//
//        public void EnableTouch()
//        {
////			StartCoroutine(WaitForAnnouncer());
//
//			if (! EventsAttached)
//			{
//				LeanTouch.OnFingerDown += OnDown;
//				LeanTouch.OnFingerUp += OnUp;
//				LeanTouch.OnFingerDrag += OnDrag;
//
//				EventsAttached = true;
//			}
//        }
//
//        public void DisableTouch()
//        {
//            if (EventsAttached)
//            {
//                LeanTouch.OnFingerDown -= OnDown;
//                LeanTouch.OnFingerUp -= OnUp;
//                LeanTouch.OnFingerDrag -= OnDrag;
//
//                EventsAttached = false;
//            }
//        }
//
//        private void OnUp(LeanFinger finger)
//        { 
//            HoldWait = false;
//            Down = false;
//
//            Output.WriteLine("up");
//
//            if (Holding)
//            {
//                // was a holding move
//                Holding = false;
////				fighter.HoldRelease();
//                return;
//            }
//
//            // check for tap or swipe
//            Gesture gesture = new Gesture(finger);
//            
//            if (gesture.IsSwipe())
//            {
//				switch (gesture.SwipeCommand)
//				{
//					case SwipeDirection.Up:
////						if (fightManager.FightPaused)
////							fightManager.SwipeUp();			// switch fighters / scenery (if paused)
////						else
//							fighter.SwipeUp();				// power up
//						break;
//
//					case SwipeDirection.Down:
//						fighter.SwipeDown();				// shove
//						break;
//
//					case SwipeDirection.Left:
//						if (fightManager.FightPaused)
//							fightManager.SwipeLeft();		// pan right thru scenery
//						else
//							fighter.SwipeLeft();			// counter
//						break;
//
//					case SwipeDirection.Right:
//						if (fightManager.FightPaused)
//							fightManager.SwipeRight();		// pan left thru scenery
//						else
//							fighter.SwipeRight();			// special
//						break;
//
//					case SwipeDirection.LeftRight:				// vengeance
//						fighter.SwipeLeftRight();
//						break;
//
////					case SwipeDirection.RightLeft:	
////						fightManager.SwipeRightLeft();		// toggle turbo mode
////						break;
//
////					case SwipeDirection.UpDown:
////						fightManager.SwipeUpDown();			// speed up animation
////						break;
////
////					case SwipeDirection.DownUp:
////						fightManager.SwipeDownUp();			// slow down animation
////						break;
//				}
////                Player.DoMove(gesture.Command);
//            }
//            else if (Vector2.Distance(finger.StartScreenPosition, finger.ScreenPosition) < Screen.height * (MinSwipeDistanceY / 100))
//            {
//				fighter.SingleFingerTap();  				// strike
////                Player.DoMove(CommandType.BasicAttack);
//            }
//        }
//
//        private void OnDrag(LeanFinger finger)
//        {
//            if (finger.GetDistance(finger.StartScreenPosition) > Screen.width * (MinSwipeDistanceY / 100))
//            {
//                Output.WriteLine("drag");
//                HoldWait = false;
//            }
//        }
//
//        private void OnDown(LeanFinger finger)
//        {
//            Output.WriteLine("down");
//            if (Down)
//            {
//                Timeout.Reset();
//            }
//
//            if (LeanTouch.Fingers.Count > 1 && LeanTouch.Fingers[0].GetDistance(LeanTouch.Fingers[1].ScreenPosition) > Screen.width * (MinSwipeDistanceY / 100))
//            {
//                Holding = false;
//                HoldWait = false;
//
//				switch (LeanTouch.Fingers.Count)
//				{
//					case 2:
//						fighter.TwoFingerTap();			// roman cancel
//						break;
//
//					case 3:
//						fightManager.ThreeFingerTap();	// toggle HUD
//						break;
//
////					case 4:
////						fightManager.FourFingerTap();	// toggle pause
////						break;
//							
//					default:
//						break;
//				}
//
////                Player.DoMove(CommandType.RomanCancel);
//            }
//            else
//            {
//                Down = true;
//
//                if (Timeout.Current <= 0 || Timeout.Current == HoldTimeoutSeconds)
//                {
//                    StartCoroutine(HoldTimeout(finger));
//                }
//            }
//        }
//
//        private IEnumerator HoldTimeout(LeanFinger finger)
//        {
//            HoldWait = true;
//
//            while (Timeout.Current > 0 && HoldWait)
//            {
//                Timeout.Subtract(Time.deltaTime);
//                yield return null;
//            }
//
//            Timeout.Reset();
//
//            if (HoldWait)
//            {
//                Holding = true;
//                HoldWait = false;
//
//				fighter.HoldDown();				// block
//
////                while (Holding)
////                {
////                    Player.DoMove(CommandType.Block);
////                    yield return null;
////                }
//            }
//        }
//
////        private string RandomString()
////        {
////            string chars = "ABCDEF0123456789";
////            System.Random random = new System.Random();
////            char[] output = Enumerable.Repeat(chars, 8)
////                .Select(s => s[random.Next(s.Length)])
////                .ToArray();
////            return new string(output);
////        }
//    }
//}
