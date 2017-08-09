using UnityEngine;
using System.Collections;

namespace FightingLegends
{
	public class SpotFX : Animation
	{
		protected override string CurrentFrameLabel { get { return CurrentSpotFX.ToString().ToUpper(); } } 	// to match movieclip frame labels

		private SpotFXType currentSpotFX = SpotFXType.None;
		public SpotFXType CurrentSpotFX
		{
			get { return currentSpotFX; }

			set
			{
				// don't check for a change in element as usual -
				// might want to restart the current element
				currentSpotFX = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log("SpotFX: movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
					MovieClipFrame = currentAnimation.FirstFrame;
				else
				{
					Debug.Log("SpotFX: CurrentSpotFX KEY NOT FOUND: " + CurrentFrameLabel);
					VoidState();
				}
			}
		}


		// initialization
		void Awake()
		{
			InitAnimation();
		}
			
		public void Update()
		{
			if (isMovieClip)
				NextAnimationFrame();		// if not paused
		}
		


		public void TriggerEffect(SpotFXType effectType) //, Vector3 position)
		{
			if (isMovieClip)
			{
				// rotate FX randomly around z axis
				//			int randomRotation = Random.Range(0, 90);
				//			transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, randomRotation, transform.rotation.w);

				switch (effectType)
				{
					case SpotFXType.Light:
						CurrentSpotFX = SpotFXType.Light;
						break;

					case SpotFXType.Medium:
						CurrentSpotFX = SpotFXType.Medium;
						break;

					case SpotFXType.Heavy:
						CurrentSpotFX = SpotFXType.Heavy;
						break;

					case SpotFXType.Block:
						CurrentSpotFX = SpotFXType.Block;
						break;

					case SpotFXType.Miss:
						CurrentSpotFX = SpotFXType.Miss;
						break;

					case SpotFXType.Counter:
						CurrentSpotFX = SpotFXType.Counter;
						break;

					case SpotFXType.Vengeance:
						CurrentSpotFX = SpotFXType.Vengeance;
						break;

					case SpotFXType.Chain:
						CurrentSpotFX = SpotFXType.Chain;
						break;

					case SpotFXType.Shove:
						CurrentSpotFX = SpotFXType.Shove;
						break;

					case SpotFXType.Roman_Cancel:
						CurrentSpotFX = SpotFXType.Roman_Cancel;
						break;

					case SpotFXType.Guard_Crush:
						CurrentSpotFX = SpotFXType.Guard_Crush;
						break;

					default:
						break;
				}
			}
			else if (animator != null)
			{
				// rotate FX randomly around z axis
	//			int randomRotation = Random.Range(0, 90);
	//			transform.rotation = new Quaternion(transform.rotation.x, transform.rotation.y, randomRotation, transform.rotation.w);

				switch (effectType)
				{
					case SpotFXType.Light:
						animator.SetTrigger("LightHit");
						break;

					case SpotFXType.Medium:
						animator.SetTrigger("MediumHit");
						break;

					case SpotFXType.Heavy:
						animator.SetTrigger("HeavyHit");
						break;

					case SpotFXType.Block:
						animator.SetTrigger("Block");
						break;

					case SpotFXType.Miss:
						animator.SetTrigger("Miss");		// TODO: what is this?
						break;

					case SpotFXType.Counter:
						animator.SetTrigger("Counter");
						break;

					case SpotFXType.Vengeance:
						animator.SetTrigger("Vengeance");
						break;

					case SpotFXType.Chain:
						animator.SetTrigger("Chain");
						break;

					case SpotFXType.Shove:
						animator.SetTrigger("Shove");
						break;

					case SpotFXType.Roman_Cancel:
						animator.SetTrigger("RomanCancel");
						break;

					case SpotFXType.Guard_Crush:
						animator.SetTrigger("GuardCrush");
						break;

					default:
						break;
				}
			}
		}
	}
}
