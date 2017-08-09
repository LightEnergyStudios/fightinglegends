using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class FeedbackFX : Animation
	{
		private Vector3 originalPosition;

		protected override string CurrentFrameLabel { get { return CurrentFeedback.ToString().ToUpper(); } } 	// to match movieclip frame labels

		private FeedbackFXType currentFeedback = FeedbackFXType.None;
		public FeedbackFXType CurrentFeedback
		{
			get { return currentFeedback; }

			set
			{
				// don't check for a change in feedback as usual -
				// might want to restart the current feedback
				currentFeedback = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log("FeedbackFX: movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
				{
					MovieClipFrame = currentAnimation.FirstFrame;
//					Debug.Log("FeedbackFX: CurrentFeedback = " + CurrentFrameLabel + ", MovieClipFrame = " + MovieClipFrame);
				}
				else
				{
					Debug.Log("FeedbackFX: CurrentFeedback KEY NOT FOUND: " + CurrentFrameLabel);
					VoidState();		
				}
			}
		}


		// initialization
		private void Awake()
		{
			originalPosition = transform.localPosition;

			InitAnimation();
		}

		public void Update()
		{
			if (isMovieClip)
				NextAnimationFrame();		// if not paused
		}
			

		public void TriggerFeedback(FeedbackFXType feedback, float xOffset = 0.0f, float yOffset = 0.0f)
		{
			if (isMovieClip)
			{
				transform.localPosition = new Vector3(originalPosition.x + xOffset,
					originalPosition.y + yOffset, originalPosition.z);

//				Debug.Log("TriggerFeedback " + feedback.ToString());

				if (feedback == FeedbackFXType.None)
				{
					VoidState();		// cancels feedback
				}
				else
				{
					CurrentFeedback = feedback;
				}
			}
			else if (animator != null)
			{
				transform.localPosition = new Vector3(originalPosition.x + xOffset,
					originalPosition.y + yOffset, originalPosition.z);

				switch (feedback)
				{
					case FeedbackFXType.One:
						animator.SetTrigger("One");
						break;

					case FeedbackFXType.Two:
						animator.SetTrigger("Two");
						break;

					case FeedbackFXType.Three:
						animator.SetTrigger("Three");
						break;

					case FeedbackFXType.Four:
						animator.SetTrigger("Four");
						break;

					case FeedbackFXType.Five:
						animator.SetTrigger("Five");
						break;

					case FeedbackFXType.Six:
						animator.SetTrigger("Six");
						break;

					case FeedbackFXType.Seven:
						animator.SetTrigger("Seven");
						break;

					case FeedbackFXType.Eight:
						animator.SetTrigger("Eight");
						break;

					case FeedbackFXType.Nine:
						animator.SetTrigger("Nine");
						break;

					case FeedbackFXType.Zero:
						animator.SetTrigger("Zero");
						break;

					case FeedbackFXType.Round:
						animator.SetTrigger("Round");
						break;

					case FeedbackFXType.KO:
						animator.SetTrigger("KO");
						break;

					case FeedbackFXType.Fight:
						animator.SetTrigger("Fight");
						break;

					case FeedbackFXType.Wrong:
						animator.SetTrigger("Wrong");
						break;

					// gestures

					case FeedbackFXType.Mash:
						animator.SetTrigger("Mash");
						break;

					case FeedbackFXType.Hold:
						animator.SetTrigger("Hold");
						break;

					case FeedbackFXType.Press:
						animator.SetTrigger("Press");
						break;

					case FeedbackFXType.Press_Both:
						animator.SetTrigger("Press_Both");
						break;

					case FeedbackFXType.Swipe_Forward:
						animator.SetTrigger("Swipe_Forward");
						break;

					case FeedbackFXType.Swipe_Back:
						animator.SetTrigger("Swipe_Back");
						break;

					case FeedbackFXType.Swipe_Up:
						animator.SetTrigger("Swipe_Up");
						break;

					case FeedbackFXType.Swipe_Down:
						animator.SetTrigger("Swipe_Down");
						break;

					case FeedbackFXType.Swipe_Vengeance:
						animator.SetTrigger("Swipe_Vengeance");
						break;

					// armour etc

					case FeedbackFXType.Armour_Down:
						animator.SetTrigger("Armour_Down");
						break;

					case FeedbackFXType.Armour_Up:
						animator.SetTrigger("Armour_Up");
						break;

					case FeedbackFXType.On_Fire:
						animator.SetTrigger("On_Fire");
						break;

					case FeedbackFXType.Health_Up:
						animator.SetTrigger("Health_Up");
						break;

					case FeedbackFXType.Success:
						animator.SetTrigger("Success");
						break;

					case FeedbackFXType.OK:
						animator.SetTrigger("Ok");
						break;

					case FeedbackFXType.Boss_Alert:
						animator.SetTrigger("Boss_Alert");
						break;

					default:
						break;
				}
			}
		}

//		// called once when animation is constructed
//		public override bool StateLoops(string stateLabel)
//		{
//			return  false;

			// TODO: find a way to set looping at time of trigger (to override AnimationState.StateLoops
//			return (stateLabel == "MASH" ||
//					stateLabel == "HOLD" ||
//					stateLabel == "PRESS" ||
//					stateLabel == "PRESS_BOTH" ||
//					stateLabel == "SWIPE_FORWARD" ||
//					stateLabel == "SWIPE_BACK" ||
//					stateLabel == "SWIPE_UP" ||
//					stateLabel == "SWIPE_DOWN" ||
//					stateLabel == "SWIPE_VENGEANCE");
//		}
			

		public void Reset()
		{
			if (animator != null)
				animator.SetTrigger("Reset");
		}


		public void TriggerNumber(int number, float xOffset = 0.0f, float yOffset = 0.0f)
		{
			transform.localPosition = new Vector3(originalPosition.x + xOffset,
									originalPosition.y + yOffset, originalPosition.z);

			switch (number)
			{
				case 1:
					TriggerFeedback(FeedbackFXType.One, xOffset, yOffset);
					break;

				case 2:
					TriggerFeedback(FeedbackFXType.Two, xOffset, yOffset);
					break;

				case 3:
					TriggerFeedback(FeedbackFXType.Three, xOffset, yOffset);
					break;

				case 4:
					TriggerFeedback(FeedbackFXType.Four, xOffset, yOffset);
					break;

				case 5:
					TriggerFeedback(FeedbackFXType.Five, xOffset, yOffset);
					break;

				case 6:
					TriggerFeedback(FeedbackFXType.Six, xOffset, yOffset);
					break;

				case 7:
					TriggerFeedback(FeedbackFXType.Seven, xOffset, yOffset);
					break;

				case 8:
					TriggerFeedback(FeedbackFXType.Eight, xOffset, yOffset);
					break;

				case 9:
					TriggerFeedback(FeedbackFXType.Nine, xOffset, yOffset);
					break;

				default:
				case 0:
					TriggerFeedback(FeedbackFXType.Zero, xOffset, yOffset);
					break;
			}
		}
	}
}
