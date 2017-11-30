using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class ElementsFX : Animation
	{
		protected override string CurrentFrameLabel { get { return CurrentElement.ToString().ToUpper(); } } 	// to match movieclip frame labels

		private ElementsFXType currentElement = ElementsFXType.None;
		public ElementsFXType CurrentElement
		{
			get { return currentElement; }

			set
			{
				// don't check for a change in element as usual -
				// might want to restart the current element
				currentElement = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log("ElementsFX: movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
					MovieClipFrame = currentAnimation.FirstFrame;
				else
				{
					Debug.Log("ElementsFX: CurrentElement KEY NOT FOUND: " + CurrentFrameLabel);
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

	
		public void TriggerElementEffect(string fighterName, FighterElement element)
		{
			if (isMovieClip)
			{
				switch (element)
				{
					case FighterElement.Air:
						switch (fighterName)
						{
							case "Leoni":
								CurrentElement = ElementsFXType.Air_Leoni;
								break;

							case "Natalya":
								CurrentElement = ElementsFXType.Air_Natayla;
								break;

							case "Hoi Lun":
								CurrentElement = ElementsFXType.Air_Hoi_Lun;
								break;

							case "Shiyang":
								CurrentElement = ElementsFXType.Air_Shiyang;
								break;

							default:
								break;
						}
						break;

					case FighterElement.Fire:
						CurrentElement = ElementsFXType.Fire;
						break;

					case FighterElement.Water:
						CurrentElement = ElementsFXType.Water;
						break;

					case FighterElement.Earth:
						CurrentElement = ElementsFXType.Earth;
						break;

					default:
						break;
				}
			}
			else if (animator != null)
			{
				switch (element)
				{
					case FighterElement.Air:
						switch (fighterName)
						{
							case "Leoni":
								animator.SetTrigger("AirLeoni");
								break;

							case "Natalya":
								animator.SetTrigger("AirNatalya");
								break;

							case "Hoi Lun":
								animator.SetTrigger("AirHoiLun");
								break;

							case "Shiyang":
								animator.SetTrigger("AirShiyang");
								break;

							default:
								break;
						}
						break;

					case FighterElement.Fire:
						animator.SetTrigger("Fire");
						break;

					case FighterElement.Water:
						animator.SetTrigger("Water");
						break;

					case FighterElement.Earth:
						animator.SetTrigger("Earth");
						break;

					default:
						break;
				}
			}
		}
	}
}
