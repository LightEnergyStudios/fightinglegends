
using System;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;


namespace FightingLegends
{
	public class FighterUI : MonoBehaviour
	{
		public Text Player1UI;
		public Text Player2UI;

		public Image Player1Coin;
		public Image Player2Coin;

		public ParticleSystem Player1Stars;
		public ParticleSystem Player2Stars;

		public float PauseTime;
		public float FloatTime;
		public float CoinFloatTime;
		public float CoinFadeTime;
		public float CoinSpinTime;
		public float FloatDistanceX;
		public float FloatDistanceY;

		private Vector2 P1StartPosition;
		private Vector2 P2StartPosition;
		private Color P1TextColour;
		private Color P2TextColour;

		private Vector2 P1CoinPosition;
		private Vector2 P2CoinPosition;
		private Color P1CoinColour;
		private Color P2CoinColour;

		private Outline[] P1Outlines;
		private Outline[] P2Outlines;
		private Shadow[] P1Shadows;
		private Shadow[] P2Shadows;

		private Color[] P1OutlineColours;
		private Color[] P2OutlineColours;
		private Color[] P1ShadowColours;
		private Color[] P2ShadowColours;

		private IEnumerator P1TextCoroutine;			// so it can be interrupted
		private IEnumerator P2TextCoroutine;			// so it can be interrupted
		private IEnumerator P1CoinFloatCoroutine;		// so it can be interrupted
		private IEnumerator P2CoinFloatCoroutine;		// so it can be interrupted
		private IEnumerator P1CoinFadeCoroutine;		// so it can be interrupted
		private IEnumerator P2CoinFadeCoroutine;		// so it can be interrupted
		private IEnumerator P1CoinSpinCoroutine;		// so it can be interrupted
		private IEnumerator P2CoinSpinCoroutine;		// so it can be interrupted


		private void OnEnable()
		{
			SaveAllValues();
		}


		public void FighterUIText(bool player1, string text)
		{
			if (player1)
			{
				if (P1TextCoroutine != null)
				{
					StopCoroutine(P1TextCoroutine);
					ResetTextValues(true);
				}
				
				P1TextCoroutine = FadeText(true, text);
				StartCoroutine(P1TextCoroutine);
			}
			else
			{
				if (P2TextCoroutine != null)
				{
					StopCoroutine(P2TextCoroutine);
					ResetTextValues(false);
				}
				
				P2TextCoroutine = FadeText(false, text);
				StartCoroutine(P2TextCoroutine);
			}
		}
			
		private IEnumerator FadeText(bool player1, string text)
		{
			float t = 0.0f;

			var fighterUI = player1 ? Player1UI : Player2UI;
			var startColour = player1 ? P1TextColour : P2TextColour;
			var startPosition = player1 ? P1StartPosition : P2StartPosition;
			var floatDistanceX = player1 ? -FloatDistanceX : FloatDistanceX;
			var targetPosition = new Vector2(startPosition.x + floatDistanceX, startPosition.y + FloatDistanceY);

			var outlines = player1 ? P1Outlines : P2Outlines;
			var shadows = player1 ? P1Shadows : P2Shadows;
			var outlineColours = player1 ? P1OutlineColours : P2OutlineColours;
			var shadowColours = player1 ? P1ShadowColours : P2ShadowColours;

			fighterUI.text = text;
			yield return new WaitForSeconds(PauseTime); 		// don't fade immediately

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / FloatTime); 

				fighterUI.rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
				fighterUI.color = Color.Lerp(startColour, Color.clear, t);

				for (int i = 0; i < outlines.Length; i++)
					outlines[i].effectColor = Color.Lerp(outlineColours[i], Color.clear, t);
				
				for (int i = 0; i < shadows.Length; i++)
					shadows[i].effectColor = Color.Lerp(shadowColours[i], Color.clear, t);

				yield return null;
			}
				
			ResetTextValues(player1);
			yield return null;
		}


		public void FighterUICoin(bool player1)
		{
			StartCoroutine(AnimateCoin(player1));
		}

		private void ResetCoin(bool player1)
		{
			ResetCoinValues(player1);

			if (player1)
			{
				if (P1CoinFloatCoroutine != null)
					StopCoroutine(P1CoinFloatCoroutine);

				if (P1CoinFadeCoroutine != null)
					StopCoroutine(P1CoinFadeCoroutine);

				if (P1CoinSpinCoroutine != null)
					StopCoroutine(P1CoinSpinCoroutine);
			}
			else
			{
				if (P2CoinFloatCoroutine != null)
					StopCoroutine(P2CoinFloatCoroutine);

				if (P2CoinFadeCoroutine != null)
					StopCoroutine(P2CoinFadeCoroutine);
				
				if (P2CoinSpinCoroutine != null)
					StopCoroutine(P2CoinSpinCoroutine);
			}
		}

		private IEnumerator AnimateCoin(bool player1)
		{
			ResetCoin(player1);

			// show coin image, float after pause, spin after pause, fade after pause

			var fighterCoin = player1 ? Player1Coin : Player2Coin;
			fighterCoin.gameObject.SetActive(true);

			yield return new WaitForSeconds(PauseTime * 2);

			// float the coin away from fighter
			if (player1)
			{
				P1CoinFloatCoroutine = FloatCoin(true);
				StartCoroutine(P1CoinFloatCoroutine);
			}
			else
			{
				P2CoinFloatCoroutine = FloatCoin(false);
				StartCoroutine(P2CoinFloatCoroutine);
			}

			yield return new WaitForSeconds(PauseTime); 

			TwinkleCoin(player1);

			// spin the coin
			int numSpins = 3;

			if (player1)
			{
				P1CoinSpinCoroutine = SpinCoin(true, numSpins);
				StartCoroutine(P1CoinSpinCoroutine);
			}
			else
			{
				P2CoinSpinCoroutine = SpinCoin(false, numSpins);
				StartCoroutine(P2CoinSpinCoroutine);
			}

			yield return new WaitForSeconds(CoinSpinTime - CoinFadeTime); 

			// fade the coin
			if (player1)
			{
				P1CoinFadeCoroutine = FadeCoin(true);
				StartCoroutine(P1CoinFadeCoroutine);
			}
			else
			{
				P2CoinFadeCoroutine = FadeCoin(false);
				StartCoroutine(P2CoinFadeCoroutine);
			}

			yield return null;
		}

		private IEnumerator FloatCoin(bool player1)
		{
			float t = 0.0f;

			var startPosition = player1 ? P1CoinPosition : P2CoinPosition;
			var floatDistanceX = player1 ? -(FloatDistanceX * 4) : (FloatDistanceX * 4);
			var targetPosition = new Vector2(startPosition.x + floatDistanceX, startPosition.y + FloatDistanceY);

			var fighterCoin = player1 ? Player1Coin : Player2Coin;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / CoinFloatTime); 

				fighterCoin.rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			ResetCoinValues(player1);
			TwinkleCoin(player1);
			yield return null;
		}

		private IEnumerator FadeCoin(bool player1)
		{
			float t = 0.0f;

			var startColour = player1 ? P1CoinColour : P2CoinColour;
			var fighterCoin = player1 ? Player1Coin : Player2Coin;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / CoinFadeTime); 

				fighterCoin.color = Color.Lerp(startColour, Color.clear, t);
				yield return null;
			}
		}

		private IEnumerator SpinCoin(bool player1, int numSpins)
		{
			if (numSpins <= 0)
				yield break;

			var fighterCoin = player1 ? Player1Coin : Player2Coin;
			var startRotation = Vector3.zero;
			var targetRotation = new Vector3(0, 180, 0);

			numSpins *= 2; 		// 180 deg each
			var spinTime = CoinSpinTime / (float)numSpins;

			while (numSpins > 0)
			{
				float t = 0.0f;

				while (t < 1.0f)
				{
					t += Time.deltaTime * (Time.timeScale / spinTime); 

					fighterCoin.rectTransform.localRotation = Quaternion.Lerp(Quaternion.Euler(startRotation), Quaternion.Euler(targetRotation), t);
					yield return null;
				}

				numSpins--;
			}

			// reset rotation
			fighterCoin.rectTransform.rotation = Quaternion.Euler(Vector3.zero);
			yield return null;
		}

		private void TwinkleCoin(bool player1)
		{
			var coinStars = player1 ? Player1Stars : Player2Stars;
			coinStars.Play();
		}


		private void SaveAllValues()
		{
			// save all original values before lerping
			P1StartPosition = Player1UI.rectTransform.anchoredPosition;
			P2StartPosition = Player2UI.rectTransform.anchoredPosition;
			P1TextColour = Player1UI.color;
			P2TextColour = Player2UI.color;

			P1CoinPosition = Player1Coin.rectTransform.anchoredPosition;
			P2CoinPosition = Player2Coin.rectTransform.anchoredPosition;
			P1CoinColour = Player1Coin.color;
			P2CoinColour = Player2Coin.color;

			P1Outlines = Player1UI.GetComponents<Outline>();
			P2Outlines = Player2UI.GetComponents<Outline>();
			P1Shadows = Player1UI.GetComponents<Shadow>();
			P2Shadows = Player2UI.GetComponents<Shadow>();

			P1OutlineColours = new Color[P1Outlines.Length];
			for (int i = 0; i < P1Outlines.Length; i++)
				P1OutlineColours[i] = P1Outlines[i].effectColor;

			P2OutlineColours = new Color[P2Outlines.Length];
			for (int i = 0; i < P2Outlines.Length; i++)
				P2OutlineColours[i] = P2Outlines[i].effectColor;

			P1ShadowColours = new Color[P1Shadows.Length];
			for (int i = 0; i < P1Shadows.Length; i++)
				P1ShadowColours[i] = P1Shadows[i].effectColor;

			P2ShadowColours = new Color[P2Shadows.Length];
			for (int i = 0; i < P2Shadows.Length; i++)
				P2ShadowColours[i] = P2Shadows[i].effectColor;
		}


		private void ResetTextValues(bool player1)
		{
			if (player1)
			{
				Player1UI.text = "";
				Player1UI.rectTransform.anchoredPosition = P1StartPosition;
				Player1UI.color = P1TextColour;

				for (int i = 0; i < P1Outlines.Length; i++)
					P1Outlines[i].effectColor = P1OutlineColours[i];

				for (int i = 0; i < P1Shadows.Length; i++)
					P1Shadows[i].effectColor = P1ShadowColours[i];
			}
			else
			{
				Player2UI.text = "";
				Player2UI.rectTransform.anchoredPosition = P2StartPosition;
				Player2UI.color = P2TextColour;

				for (int i = 0; i < P2Outlines.Length; i++)
					P2Outlines[i].effectColor = P2OutlineColours[i];

				for (int i = 0; i < P2Shadows.Length; i++)
					P2Shadows[i].effectColor = P2ShadowColours[i];
			}
		}

		private void ResetCoinValues(bool player1)
		{
			if (player1)
			{
				Player1Coin.rectTransform.anchoredPosition = P1CoinPosition;
				Player1Coin.rectTransform.rotation = Quaternion.Euler(Vector3.zero);
				Player1Coin.color = P1CoinColour;
				Player1Coin.gameObject.SetActive(false);
			}
			else
			{
				Player2Coin.rectTransform.anchoredPosition = P2CoinPosition;
				Player2Coin.rectTransform.rotation = Quaternion.Euler(Vector3.zero);
				Player2Coin.color = P2CoinColour;
				Player2Coin.gameObject.SetActive(false);
			}
		}
	}
}
