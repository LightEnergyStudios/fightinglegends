using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class Curtain : MonoBehaviour
	{
		private Image curtain;
	
		public Color curtainColour;	
		public float fadeTime;

		private bool isBlackedOut = false;

		public static bool ReadyToRaise = false;


		void Awake()
		{
			curtain = GetComponent<Image>();
			curtain.color = Color.clear;
		}

		public IEnumerator FadeToBlack()
		{
//			Debug.Log("FadeToBlack: isBlackedOut = " + isBlackedOut);
			if (isBlackedOut)
				yield break;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				curtain.color = Color.Lerp(Color.clear, curtainColour, t);
				yield return null;
			}
				
			isBlackedOut = true;
//			Debug.Log("FadeToBlack complete");
			yield return null;
		}

		public IEnumerator CurtainUp(bool force = false)
		{
//			Debug.Log("CurtainUp start: isBlackedOut = " + isBlackedOut);
			if (!isBlackedOut && !force)
				yield break;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				curtain.color = Color.Lerp(curtainColour, Color.clear, t);
				yield return null;
			}
				
			isBlackedOut = false;
//			Debug.Log("CurtainUp complete");
			yield return null;
		}


		// fade to black until ReadyToRaise set to true
		public IEnumerator FadeUntilReady()
		{
			ReadyToRaise = false;

			yield return StartCoroutine(FadeToBlack());

			while (! ReadyToRaise)
			{
				yield return null;
			}

			yield return StartCoroutine(CurtainUp());

			ReadyToRaise = false;		// reset
			yield return null;
		}

		public void BlackOut()
		{
			curtain.color = curtainColour;
		}

		public void Clear()
		{
			curtain.color = Color.clear;
		}
	}
}
