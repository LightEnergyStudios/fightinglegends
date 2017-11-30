using UnityEngine;
using System.Collections;
//using System.Collections.Generic;

namespace FightingLegends
{
	public class RoundFX : Animation
	{
		private Vector3 originalPosition;

		protected override string CurrentFrameLabel { get { return "ROUND"; } } 	// to match movieclip frame labels


		// initialization
		private void Awake()
		{
			originalPosition = transform.localPosition;
			InitAnimation();
		}


		public void FixedUpdate()
		{
			if (isMovieClip)
				NextAnimationFrame();		// if not paused
		}
			

		public void TriggerRound(float xOffset = 0.0f, float yOffset = 0.0f)
		{
			if (isMovieClip)
			{
				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
					MovieClipFrame = currentAnimation.FirstFrame;
				else
					VoidState();
			}
			else if (animator != null)
			{
				transform.localPosition = new Vector3(originalPosition.x + xOffset,
									originalPosition.y + yOffset, originalPosition.z);

				animator.SetTrigger("Round");
			}

		}
	}
}
