using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace FightingLegends
{
	public class ComboStepUI : MonoBehaviour
	{
		private ComboStep comboStep; 

		private bool growing = false;
		private bool shrinking = false;
		private bool pulsing = false;

		private Vector3 imagePosition = default(Vector3);

//		private Vector3 repeatingPulseScale = Vector3.zero;		// to stop ever-diminishing pulse!

		private IEnumerator growCoroutine;
		private IEnumerator shrinkCoroutine;
		private IEnumerator pulseCoroutine;


		public Image StepImage { get { return GetComponent<Image>(); } }
	
		public Image StepPromptImage
		{
			get
			{
				var portrait = transform.Find("Prompt");
				if (portrait != null)
					return portrait.GetComponent<Image>();
				
				return null;

//				var children = transform.chi GetComponentsInChildren<Image>();
//
//				foreach (var child in children)
//				{
//					if (child.name == "Portrait")
//						return child;
//				}
//
//				return null;
			}
		}

		public Image StepBackground
		{
			get
			{
				var background = transform.Find("Background");
				if (background != null)
					return background.GetComponent<Image>();
				return null;

//				var children = GetComponentsInChildren<Image>();
//
//				foreach (var child in children)
//				{
//					if (child.name == "Background")
//						return child;
//				}
//				return null;
			}
		}

		public Spark StepSpark
		{
			get
			{
//				var spark = transform.Find("Spark");
//				if (spark != null)
//					return spark.GetComponent<Spark>();
//				return null;

				var children = GetComponentsInChildren<Spark>();

				foreach (var child in children)
				{
					if (child.name == "Spark")
						return child;
				}
				return null;
			}
		}

		public Image StepTick
		{
			get
			{
//				var tick = transform.Find("Tick");
//				if (tick != null)
//					return tick.GetComponent<Image>();
//				return null;

				var children = GetComponentsInChildren<Image>();

				foreach (var child in children)
				{
					if (child.name == "Tick")
						return child;
				}
				return null;
			}
		}

		public Text StepName
		{
			get
			{
//				var name = transform.Find("Name");
//				if (name != null)
//					return name.GetComponent<Image>();
//				return null;

				var children = GetComponentsInChildren<Text>();

				foreach (var child in children)
				{
					if (child.name == "Name")
						return child;
				}
				return null;
			}
		}


		public IEnumerator Grow(float pulseTime, Vector3 toScale, Vector3? toPosition = null)
		{
			if (growing || shrinking)
				yield break;
			
			var stepImage = StepImage;
			float t = 0.0f;
			Vector3 startPosition = stepImage.transform.localPosition;
			Vector3 targetPosition = toPosition.GetValueOrDefault();

			Vector3 startScale = Vector3.one;
			growing = true;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime); 
				stepImage.transform.localScale = Vector3.Lerp(startScale, toScale, t);

				if (toPosition != null)
					stepImage.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			growing = false;
			yield return null;
		}

		public IEnumerator Shrink(float pulseTime, Vector3? fromScale = null)
		{
			if (growing || shrinking)
				yield break;
			
			var stepImage = StepImage;
			float t = 0.0f;

			if (fromScale == null)
				fromScale = stepImage.transform.localScale;
			
			Vector3 startScale = fromScale.GetValueOrDefault();
			Vector3 startPosition = stepImage.transform.localPosition;

			Vector3 targetScale = Vector3.one;
			shrinking = true;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime); 
				stepImage.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
				stepImage.transform.localPosition = Vector3.Lerp(startPosition, imagePosition, t);
				yield return null;
			}

			shrinking = false;
			yield return null;
		}

		public IEnumerator Pulse(float pulseTime, Vector3 growScale, AudioClip sound, bool pause, bool repeatWhileWaiting = false, Vector3? toPosition = null)
		{
//			if (repeatWhileWaiting && repeatingPulseScale == Vector3.zero)		// to prevent ever-dimishing scale!
//				repeatingPulseScale = growScale / 2.0f;
			
//			pulseCoroutine = GrowShrink(pulseTime, repeatWhileWaiting ? repeatingPulseScale : growScale, sound, pause, repeatWhileWaiting);
			pulseCoroutine = GrowShrink(pulseTime, growScale, sound, pause, repeatWhileWaiting, toPosition);
			yield return StartCoroutine(pulseCoroutine);
		}
			
		private IEnumerator GrowShrink(float pulseTime, Vector3 growScale, AudioClip sound, bool pause, bool repeatWhileWaiting, Vector3? toPosition)
		{
			if (pulsing)
				yield break;

			pulsing = true;				
			
			growCoroutine = Grow(pulseTime, growScale, toPosition);
			yield return StartCoroutine(growCoroutine);

			if (sound != null)
				TriggerSpark(sound);
			
			if (pause)
				yield return new WaitForSeconds(pulseTime);

			shrinkCoroutine = Shrink(pulseTime, growScale);
			yield return StartCoroutine(shrinkCoroutine);

			pulsing = false;

			if (repeatWhileWaiting && comboStep.WaitingForInput)
				StartCoroutine(Pulse(pulseTime, growScale, null, false, true, toPosition));

			yield return null;
		}


		public void Init(ComboStep step)
		{
			comboStep = step;

			if (StepTick != null)
				StepTick.enabled = false;

			if (StepName != null)
				StepName.text = step.StepName;

			imagePosition = StepImage.transform.localPosition;

//			if (StepBackground != null)
//				StepBackground.enabled = false;
		}


		private void TriggerSpark(AudioClip clip)
		{
			if (clip != null)
				AudioSource.PlayClipAtPoint(clip, Vector3.zero, FightManager.SFXVolume);

			var spark = StepSpark;
			if (spark != null)
				spark.Play();
		}
			

		public void StopAnimation()
		{
			if (growCoroutine != null)
				StopCoroutine(growCoroutine);

			if (shrinkCoroutine != null)
				StopCoroutine(shrinkCoroutine);

			if (pulseCoroutine != null)
				StopCoroutine(pulseCoroutine);
		}
	}
}
