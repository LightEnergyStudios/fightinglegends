using UnityEngine;
using System.Collections;


namespace FightingLegends
{
	public class LogoFX : Animation
	{
		public string LogoLabel { get { return "LOGO"; } }		// first frame (until LOAD frame)

		private AudioSource musicSource;
		private const uint heartFrame = 50;				// start playing music on heart
		private const float drumsStart = 6.0f;			// seconds before drums kick in

		public delegate void DrumsDelegate();
		public DrumsDelegate OnDrums;

		protected override string CurrentFrameLabel { get { return CurrentLogoLabel.ToString().ToUpper(); } } 	// to match movieclip frame labels

		private LogoFrameLabel currentLogoLabel = LogoFrameLabel.None;
		public LogoFrameLabel CurrentLogoLabel
		{
			get { return currentLogoLabel; }

			set
			{
				currentLogoLabel = value;

				if (movieClipStates == null)	// may not have been initialised yet
				{
					Debug.Log("LogoFX: movieClipStates == null!!");
					return;
				}

				currentAnimation = LookupCurrentAnimation;

				if (currentAnimation != null)
					MovieClipFrame = currentAnimation.FirstFrame;
				else
				{
					Debug.Log("LogoFX: CurrentLogoFX KEY NOT FOUND: " + CurrentFrameLabel);
					VoidState();
				}
			}
		}
			

		// initialization
		public void Awake()
		{
			musicSource = GetComponent<AudioSource>();

			InitAnimation();
		}

		public void Start()
		{
			TriggerLogo();
		}

		public void Update()
		{
			if (isMovieClip)
			{
				NextAnimationFrame();		// if not paused

				if (MovieClipFrame == heartFrame)
					StartMusic();
			}
		}

		public void TriggerLogo()
		{
			if (isMovieClip)
			{
				CurrentLogoLabel = LogoFrameLabel.Logo;
			}

//			Debug.Log("TriggerLogo");
			GetComponent<MeshRenderer>().enabled = true;
			StartCoroutine(WaitForDrums());
		}


		private void StartMusic()
		{
			if (musicSource != null)
			{
				musicSource.clip.LoadAudioData();
				musicSource.Play();
			}
		}

		private IEnumerator WaitForDrums()
		{
			yield return new WaitForSeconds(drumsStart);
			Debug.Log("Drums!!");

			GetComponent<MeshRenderer>().enabled = false;

			if (OnDrums != null)
				OnDrums();

			yield return null;
		}


		public override void VoidState()
		{
			currentAnimation = LookupAnimation(LogoLabel);
			if (currentAnimation != null)
				MovieClipFrame = currentAnimation.FirstFrame;
		}
	}
}
