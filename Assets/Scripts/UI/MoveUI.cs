using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;

namespace FightingLegends
{
	[Serializable]
	public class MoveUI
	{
		public Move Move;
		public Image MoveImage;
		public Button MoveButton;
		public Text MoveLabel;
		public Image MoveGlow;
		public ParticleSystem MoveStars;
		public Image Tick;
		public Image Cross;

		public Color TextColourEnabled;
		public Color TextColourDisabled;

		[HideInInspector]
		public IEnumerator pulseMoveCoroutine = null;
		[HideInInspector]
		public IEnumerator pulseTextCoroutine = null;

		private bool active = true;
		private bool pulsing = false;
		private bool moveAvailable = true;					// fighter has this move

		private int siblingIndex = 0;						// default
		private const int pulseSiblingIndex = 100;			// so pulsing move is on top

		private const float activeScale = 1.0f; //  1.2f;	// TODO: active not working properly	// enlarged while active
		private float activePulseTime = 0.15f;


		// returns true if enabled (ie. was disabled)
		public bool Activate(bool enable, bool stars, bool available)
		{
			if (!enable && siblingIndex == 0)		// initialise
			{
				siblingIndex = MoveImage.transform.GetSiblingIndex();
//				Debug.Log("Activate: " + MoveLabel.text + ", siblingIndex = " + siblingIndex);
			}
			
			moveAvailable = available;
			Cross.gameObject.SetActive(!moveAvailable);

			if (enable && active)		// no change (already active)
				return false;

			// don't disable if pulsing
			if (!enable && pulsing)
				return false;

			active = enable;

			MoveImage.enabled = enable;						// black background 
			MoveButton.interactable = enable;
			MoveGlow.gameObject.SetActive(enable);			// glow image / animation
			MoveLabel.color = enable ? TextColourEnabled : TextColourDisabled;

			if (stars && active) 							// activating
				MoveStars.Play();
			
			return active;
		}

		public void SetLabel(string label)
		{
			if (! pulsing)
				MoveLabel.text = label;
		}
			
		public IEnumerator PulseText(float pulseScale, float pulseTime)
		{
			if (pulsing || !moveAvailable)
				yield break;
			
			float t = 0;

			// pulse  up
			Vector3 currentScale = MoveLabel.transform.localScale;
			Vector3 startScale = new Vector3(1, 1, 1);
			Vector3 targetScale = new Vector3(pulseScale, pulseScale, pulseScale);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime);

				MoveLabel.transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
				yield return null;
			}

			// pulse down
			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime);

				MoveLabel.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
				yield return null;
			}

			yield return null;
		}


		public IEnumerator Pulse(float pulseScale, float pulseTime, AudioClip sound, bool stars, bool tick)
		{
			if (!moveAvailable)
				yield break;
			
			pulsing = true;
			float t = 0;

			// pulse  up
			Vector3 currentScale = MoveImage.transform.localScale;
			Vector3 targetScale = new Vector3(pulseScale, pulseScale, pulseScale);

			// make sure button is enabled before enlarging
			Activate(true, false, true);
			MoveGlow.gameObject.SetActive(false);			// disable glow image / animation

			// make sure move is on top of the others while pulsing
//			siblingIndex = MoveImage.transform.GetSiblingIndex();
			MoveImage.transform.SetSiblingIndex(siblingIndex + pulseSiblingIndex);

			if (tick)
				Tick.gameObject.SetActive(true);
			
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime);

				MoveImage.transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
				yield return null;
			}

			// stars, sound and remove tick when large
			if (stars)
				MoveStars.Play();

			if (sound != null)
				AudioSource.PlayClipAtPoint(sound, Vector3.zero, FightManager.SFXVolume);

			// pulse down
			t = 0;
			Vector3 returnScale = active ? new Vector3(activeScale, activeScale, activeScale) : Vector3.one;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime);

				MoveImage.transform.localScale = Vector3.Lerp(targetScale, returnScale, t);
				yield return null;
			}

			MoveImage.transform.SetSiblingIndex(siblingIndex);

			Tick.gameObject.SetActive(false);
			pulsing = false;
			yield return null;
		}


		public IEnumerator ActivateSize()
		{
			float t = 0;

			Vector3 currentScale = MoveImage.transform.localScale;
			Vector3 targetScale = active ? new Vector3(activeScale, activeScale, activeScale) : Vector3.one;

			MoveImage.transform.SetSiblingIndex(active ? siblingIndex + pulseSiblingIndex : siblingIndex);

			if (currentScale == targetScale)
				yield break;
			
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / activePulseTime);

				MoveImage.transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
				yield return null;
			}
			yield return null;
		}


		public Image DuplicateImage
		{
			get
			{
				// create a new image based on the move button
				var moveImage = new GameObject(MoveLabel.text);
				var image = moveImage.AddComponent<Image>();
				image.sprite = MoveButton.image.sprite;

				return image;
			}
		}
	}
}
