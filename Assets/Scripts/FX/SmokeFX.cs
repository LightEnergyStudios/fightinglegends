using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class SmokeFX : Animation
	{
		protected override string CurrentFrameLabel { get { return CurrentSmoke.ToString().ToUpper(); } } 	// to match movieclip frame labels

		private SmokeFXType currentSmoke = SmokeFXType.None;
		public SmokeFXType CurrentSmoke
		{
			get { return currentSmoke; }

			set
			{
				// don't check for a change in smoke as usual -
				// might want to restart the current smoke
				currentSmoke = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log("SmokeFX: movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
					MovieClipFrame = currentAnimation.FirstFrame;
				else
				{
					Debug.Log("SmokeFX: CurrentElement KEY NOT FOUND: " + CurrentFrameLabel);
					VoidState();	
				}
			}
		}


		// initialization
		public void Awake()
		{
			InitAnimation();
		}

		public void FixedUpdate()
		{
			if (isMovieClip)
				NextAnimationFrame();		// if not paused
		}


		public void TriggerSmoke(SmokeFXType smoke, float xOffset = 0.0f, float yOffset = 0.0f)
		{
			if (smoke == SmokeFXType.None)
				return;

			if (isMovieClip)
			{
//				transform.localPosition = new Vector3(originalPosition.x + xOffset,
//					originalPosition.y + yOffset, originalPosition.z);

//				if (smoke == SmokeFXType.None)
//				{
//					VoidState();		// cancels feedback
//				}
//				else
				{
					CurrentSmoke = smoke;
				}
			}
			else if (animator != null)
			{
//				transform.localPosition = new Vector3(originalPosition.x + xOffset,
//					originalPosition.y + yOffset, originalPosition.z);

				switch (smoke)
				{
					case SmokeFXType.Small:
						animator.SetTrigger("Small");
						break;

					case SmokeFXType.Straight:
						animator.SetTrigger("Straight");
						break;

					case SmokeFXType.Uppercut:
						animator.SetTrigger("Uppercut");
						break;

					case SmokeFXType.Mid:
						animator.SetTrigger("Mid");
						break;

					case SmokeFXType.Hook:
						animator.SetTrigger("Hook");
						break;

					case SmokeFXType.Counter:
						animator.SetTrigger("Counter");
						break;

					default:
						break;
				}
			}
		}
	}
}
