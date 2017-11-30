
using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

using GAF.Core;


namespace FightingLegends
{
	// base class for any gameobject that comprises a GAF animation (eg. fighter, FX)
	public class Animation : MonoBehaviour
    {
		public GAFBakedMovieClip movieClip{ get; private set; }				// gameobject component
		protected Dictionary<string, AnimationState> movieClipStates;		// for lookup of frame number by state (frame label)
		protected uint movieClipFrame;
//		protected uint lastMovieClipFrame = 0;

		protected uint gameLoopCount = 0;

		public AudioClip firstFrameAudio; 

		protected AnimationState currentAnimation = null;					// start/end frames etc
		protected virtual string CurrentFrameLabel { get { return ""; } } 	// to match movieclip frame labels
		private const string voidState = "VOID";
		private const string defaultState = "Default";

		protected bool isMovieClip = false;
		private bool isPaused = false;

		// event fired at start of every state
		public delegate void StartStateDelegate(AnimationState startingState);
		public StartStateDelegate OnStartState;

		// event fired at end of every state
		public delegate void EndStateDelegate(AnimationState endingState);
		public EndStateDelegate OnEndState;

		// event fired on every non-idle frame
		public delegate void AnimationFrameDelegate(string stateLabel);
		public AnimationFrameDelegate OnAnimationFrame;

		// retain support for Animator approach
		protected Animator animator;					// Animator component of FX prefab
		private float animatorSpeed = 2.0f;				// from 30fps (imported by GAF) to 60fps


		private void OnEnable()
		{
			FightManager.OnFightPaused += PauseAnimation;
		}

		private void OnDisable()
		{
			FightManager.OnFightFrozen -= PauseAnimation;
		}


		public uint MovieClipFrame
		{
			get { return movieClipFrame; }
			set
			{
				if (movieClipFrame == value)
					return;		// no change

				movieClipFrame = value;

				if (movieClip != null)
				{
//					Debug.Log("MovieClipFrame: gotoAndStop " + movieClipFrame);
					movieClip.gotoAndStop(movieClipFrame);
				}
			}
		}

		public bool AtLastFrame
		{
			get { return MovieClipFrame >= currentAnimation.LastFrame; }
		}


		protected void InitAnimation()
		{
			movieClip = GetComponent<GAFBakedMovieClip>();

			if (movieClip != null)
			{
				isMovieClip = true;
				BuildMovieClipStates();		// build state dictionary from frame labels in movieClip

				MovieClipFrame = 0;

				isPaused = false;
				VoidState();			// init to void state
			}
			else 
			{
				animator = GetComponent<Animator>();	

				if (animator != null)
				{
					isMovieClip = false;
					animator.speed *= animatorSpeed;
				}
			}
		}
			

		// called by subclass's Update or FixedUpdate
		protected void NextAnimationFrame()
		{
//			Debug.Log("NextAnimationFrame: currentAnimation = " + currentAnimation.StateLabel);

			if (currentAnimation == null)
				return;

			if (isPaused)
				return;

			if (currentAnimation.StateLabel == voidState)
				return;
				
			if (currentAnimation.StateLabel == defaultState)
				return;
				
//			if (OnAnimationFrame != null)
//				OnAnimationFrame(CurrentFrameLabel);

//			if (MovieClipFrame == lastMovieClipFrame)
//			{
//				VoidState();
//				return;
//			}

			if (MovieClipFrame == currentAnimation.FirstFrame)
			{
				currentAnimation.HasEnded = false;
				StartState();			// virtual

				if (firstFrameAudio != null)
					AudioSource.PlayClipAtPoint(firstFrameAudio, Vector3.zero, FightManager.SFXVolume);
			}

			// if currently on the last frame of a state, either loop or end current state
			if (MovieClipFrame == currentAnimation.LastFrame)
			{
//				Debug.Log("NextAnimationFrame: LAST FRAME of " + currentAnimation.StateLabel);

				if (currentAnimation.StateLoops)
				{
					MovieClipFrame = currentAnimation.FirstFrame;
				}
				else
				{
					if (! currentAnimation.HasEnded)
						EndState(); 		// virtual
					
					currentAnimation.HasEnded = true;
				}
			}
			else
			{
				MovieClipFrame++;
//				Debug.Log("NextAnimationFrame: " + currentAnimation.StateLabel + ", frame = " + MovieClipFrame);
			}
			
			OnNextAnimationFrame();		// virtual
		}

		protected virtual void OnNextAnimationFrame()
		{
			// overrides can do whatever is required on each beat
		}

		protected virtual void StartState()
		{
//			Debug.Log("Animation StartState: " + currentAnimation.StateLabel + ", frame = " + MovieClipFrame);

			// broadcast start of state event
			if (OnStartState != null && currentAnimation != null)
				OnStartState(currentAnimation);
		}

		protected virtual void EndState()
		{
//			Debug.Log("Animation EndState: " + currentAnimation.StateLabel + ", frame = " + MovieClipFrame);

			// broadcast end of state event
			if (OnEndState != null && currentAnimation != null)
				OnEndState(currentAnimation);
		}

		public virtual void VoidState()
		{
			currentAnimation = LookupAnimation(voidState);
			if (currentAnimation != null)
				MovieClipFrame = currentAnimation.FirstFrame;
		}

//		protected void TogglePauseAnimation()
//		{
//			isPaused = !isPaused;
//		}

		protected void PauseAnimation(bool pause)
		{
			isPaused = pause;
		}

		protected AnimationState LookupCurrentAnimation
		{
			get { return LookupAnimation(CurrentFrameLabel); }
		}

		protected AnimationState LookupAnimation(string stateLabel)
		{
			if (movieClipStates == null)
				return null;

			AnimationState state;
			bool found = movieClipStates.TryGetValue(stateLabel, out state);

			if (state != null)
				state.HasEnded = false;

			return found ? state : null;
		}

		private void BuildMovieClipStates()
		{
			movieClipStates = new Dictionary<string, AnimationState>();
			var clipSequences = movieClip.asset.getSequences(movieClip.timelineID); 

			foreach (var sequence in clipSequences)
			{
				var key = sequence.name;		// frame label

				// seem to be multiple VOIDs and Defaults... (yuk! GAF?)
				if (movieClipStates.ContainsKey(key))		
					continue;

				var animation = new AnimationState();
				animation.StateLabel = key;
				animation.FirstFrame = sequence.startFrame;
				animation.LastFrame = sequence.endFrame;
				animation.StateLoops = StateLoops(key);		// virtual 

				movieClipStates.Add(key, animation);

				// record very last frame number
//				lastMovieClipFrame = animation.FirstFrame;
			}
		}

		public virtual bool StateLoops(string stateLabel)
		{
			return false;		// default for FX
		}


		// event on first frame of animation timeline
		public void AnimationStart()
		{
			if (animator == null)
				return;

			if (firstFrameAudio != null)
				AudioSource.PlayClipAtPoint(firstFrameAudio, Vector3.zero, FightManager.SFXVolume);
		}
    }
}
