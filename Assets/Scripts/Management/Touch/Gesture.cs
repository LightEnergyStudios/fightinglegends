//using Lean;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;
//using UnityEngine;
//
//namespace FightingLegends
//{
//    public class Gesture
//    {
//        protected enum Direction
//        {
//            None,
//            Left,
//            Right,
//            Up,
//            Down
//        }
//			
//		protected List<Direction> Swipes;
//			
//		public Gesture(LeanFinger finger)
//		{
//			Swipes = ParseFinger(finger);
//		}
//
//        protected class Delta
//        {
//            public Direction direction;
//            public float distance;
//
//            public Delta(Vector2 currentPosition, Vector2 previousPosition)
//            {
//                distance = Vector2.Distance(previousPosition, currentPosition);
//                direction = ParseDirection(currentPosition - previousPosition);
//            }
//
//            private Direction ParseDirection(Vector2 delta)
//            {
//                Vector2 positive = Positivize(delta);
//
//                if (positive.x > positive.y)
//                {
//                    return delta.x < 0 ? Direction.Left : Direction.Right;
//                }
//                else
//                {
//                    return delta.y < 0 ? Direction.Down : Direction.Up;
//                }
//            }
//
//            private Vector2 Positivize(Vector2 input)
//            {
//                return new Vector2(input.x < 0 ? input.x * -1 : input.x,
//                    input.y < 0 ? input.y * -1 : input.y);
//            }
//        }
//
//
//        public SwipeDirection SwipeCommand
//		{
//            get { return GetCommandFromSwipes(); }
//            private set { SwipeCommand = value; }
//        }
//
//
//        private List<Direction> ParseFinger(LeanFinger finger)
//        {
//            List<Direction> swipes = new List<Direction>();
//            Direction found = Direction.None;
//            Vector2 maxPos = finger.StartScreenPosition;
//
//            finger.Snapshots.ForEach(snapshot => {
//                Delta delta = new Delta(snapshot.ScreenPosition, maxPos);
//
//                if (swipes.Count() < 1)
//                {
//                    if (delta.distance > ScreenPercent(delta.direction, true))
//                    {
//                        swipes.Add(delta.direction);
//                        found = delta.direction;
//                    }
//                }
//                else if (found != delta.direction)
//                {
//                    if (delta.distance > ScreenPercent(delta.direction, false))
//                    {
//                        swipes.Add(delta.direction);
//                        found = delta.direction;
//                    }
//                }
//
//                if (delta.direction == found)
//                {
//                    maxPos = snapshot.ScreenPosition;
//                }
//            });
//
//            return swipes;
//        }
//
//        private float ScreenPercent(Direction direction, bool followUp)
//        {
//            float percent = 0f;
//
//            if (direction == Direction.Up || direction == Direction.Down)
//            {
//                percent = !followUp ? Touch.Instance.MinSwipeDistanceX : Touch.Instance.MinSwipeReturnDistanceX;
//                return Screen.height * (percent / 100);
//            }
//            else if (direction == Direction.Left || direction == Direction.Right)
//            {
//                percent = !followUp ? Touch.Instance.MinSwipeDistanceY : Touch.Instance.MinSwipeReturnDistanceY;
//                return Screen.width * (percent / 100);
//            }
//
//            throw new Exception("Cannot get percentage of screen for an invalid axis");
//        }
//
//        private SwipeDirection GetCommandFromSwipes()
//        {
//            if (Swipes.Count() == 1)
//            {
//                switch (Swipes[0])
//                {
//                    case Direction.Left:
//                        return SwipeDirection.Left;
//                    case Direction.Right:
//                        return SwipeDirection.Right;
//                    case Direction.Up:
//                        return SwipeDirection.Up;
//                    case Direction.Down:
//                        return SwipeDirection.Down;
//                }
//            }
//            else if (Swipes.Count() == 2)
//            {
//                if (Swipes[0] == Direction.Left && Swipes[1] == Direction.Right)
//                    return SwipeDirection.LeftRight;
//
//				if (Swipes[0] == Direction.Right && Swipes[1] == Direction.Left)
//					return SwipeDirection.RightLeft;
//
//				if (Swipes[0] == Direction.Up && Swipes[1] == Direction.Down)
//					return SwipeDirection.UpDown;
//
//				if (Swipes[0] == Direction.Down && Swipes[1] == Direction.Up)
//					return SwipeDirection.DownUp;
//            }
//
//            return SwipeDirection.None;
//        }
//
//        public bool IsSwipe()
//        {
//            return Swipes.Count() > 0;
//        }
//    }
//}
