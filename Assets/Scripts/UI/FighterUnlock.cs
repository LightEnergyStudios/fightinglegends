using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class FighterUnlock : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Text FighterName;	
		public Image FighterPortrait;	

		public Text UnlockStatus;
		public Text DefeatCount;
		public Text UnlockCoins;

		public Button BuyButton;
		public Button OkButton;

		public Sprite leoniPortrait;
		public Sprite hoiLunPortrait;
		public Sprite danjumaPortrait;
		public Sprite alaznePortrait;
		public Sprite jacksonPortrait;
		public Sprite shiroPortrait;
		public Sprite shiyangPortrait;
		public Sprite natalyaPortrait;
		public Sprite ninjaPortrait;
		public Sprite skeletronPortrait;

		public AudioClip ShowSound;
		public AudioClip OkSound;

		public float fadeTime;

		private Fighter unlockFighter;
		private Action actionOnBuy;
		private Action actionOnOk;


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;
		}

		private void OnEnable()
		{
			BuyButton.onClick.AddListener(BuyClicked);
			OkButton.onClick.AddListener(OkClicked);

			//			SetWalletValue(FightManager.Coins, false);				// set current value
		}

		private void OnDestroy()
		{
			BuyButton.onClick.RemoveListener(BuyClicked);
			OkButton.onClick.RemoveListener(OkClicked);
		}

		// public entry point
		public void FighterUnlockStatus(Fighter fighter, Action onOk)
		{
			actionOnOk = onOk;
			unlockFighter = fighter;

			FighterName.text = fighter.FighterName.ToUpper();

			switch (fighter.FighterName)
			{
				case "Shiro":
					FighterPortrait.sprite = shiroPortrait;
					break;

				case "Natalya":
					FighterPortrait.sprite = natalyaPortrait;
					break;

				case "Hoi Lun":
					FighterPortrait.sprite = hoiLunPortrait;
					break;

				case "Leoni":
					FighterPortrait.sprite = leoniPortrait;
					break;

				case "Danjuma":
					FighterPortrait.sprite = danjumaPortrait;
					break;

				case "Jackson":
					FighterPortrait.sprite = jacksonPortrait;
					break;

				case "Alazne":
					FighterPortrait.sprite = alaznePortrait;
					break;

				case "Shiyang":
					FighterPortrait.sprite = shiyangPortrait;
					break;

				case "Ninja":
					FighterPortrait.sprite = ninjaPortrait;
					break;

				case "Skeletron":
					FighterPortrait.sprite = skeletronPortrait;
					break;

				default:
					break;
			}

			StartCoroutine(Show());
		}


		private IEnumerator Show()
		{
			background.enabled = true;
			panel.gameObject.SetActive(true);

			panel.transform.localScale = Vector3.zero;
			background.color = Color.clear;

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				panel.transform.localScale = Vector3.Lerp(Vector3.zero, panelScale, t);
				background.color = Color.Lerp(Color.clear, backgroundColour, t);
				yield return null;
			}

			if (ShowSound != null)
				AudioSource.PlayClipAtPoint(ShowSound, Vector3.zero, FightManager.SFXVolume);

			yield return null;
		}

		private IEnumerator Hide()
		{
			//			if (FadeSound != null)
			//				AudioSource.PlayClipAtPoint(FadeSound, Vector3.zero, FightManager.SFXVolume);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				panel.transform.localScale = Vector3.Lerp(panelScale, Vector3.zero, t);
				background.color = Color.Lerp(backgroundColour, Color.clear, t);
				yield return null;
			}

			background.enabled = false;
			panel.gameObject.SetActive(false);
			panel.transform.localScale = panelScale;

			yield return null;
		}

		private void BuyClicked()
		{
			// call the passed-in delegate
			if (actionOnBuy != null)
			{
				actionOnBuy();
			}

			//			if (BuySound != null)
			//				AudioSource.PlayClipAtPoint(BuySound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}

		private void OkClicked()
		{
			if (OkSound != null)
				AudioSource.PlayClipAtPoint(OkSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}
	}


}
