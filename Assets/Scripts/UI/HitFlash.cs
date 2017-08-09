using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;

namespace FightingLegends
{
	public class HitFlash : MonoBehaviour
	{
		private Image flash;

		public Color hitColour;			// semi-transparent white
		public Color cancelColour;		// black

		public float flashTime;


		void Awake()
		{
			flash = GetComponent<Image>();
			flash.color = Color.clear;
		}
		

		public IEnumerator PlayHitFlash()
		{
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / flashTime); 

				flash.color = Color.Lerp(hitColour, Color.clear, t);
				yield return null;
			}
			yield return null;
		}

		public IEnumerator PlayColourFlash(Color flashColour, float flashTime)
		{
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / flashTime); 

				flash.color = Color.Lerp(flashColour, Color.clear, t);
				yield return null;
			}
			yield return null;
		}

		public IEnumerator PlayBlackFlash()
		{
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / flashTime); 

				flash.color = Color.Lerp(cancelColour, Color.clear, t);
				yield return null;
			}
			yield return null;
		}


		public IEnumerator BlackOut(float time)
		{
			flash.color = cancelColour;
			yield return new WaitForSeconds(time);
			flash.color = Color.clear;
		}

		public IEnumerator WhiteOut(float time)
		{
			flash.color = Color.white;
			yield return new WaitForSeconds(time);
			flash.color = Color.clear;
		}

		public void Clear()
		{
			flash.color = Color.clear;
		}
	}
}
