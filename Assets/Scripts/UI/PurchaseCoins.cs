using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class PurchaseCoins : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Text Title;

		public Button Coins100Button;
		public Button Coins1000Button;
		public Button Coins10000Button;
		public Button CancelButton;

		public AudioClip BuySound;
		public AudioClip CancelSound;

		public float fadeTime;

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
			Coins100Button.onClick.AddListener(delegate { PurchaseProduct(Store.Coins100ProductID); });
			Coins1000Button.onClick.AddListener(delegate { PurchaseProduct(Store.Coins1000ProductID); });
			Coins10000Button.onClick.AddListener(delegate { PurchaseProduct(Store.Coins10000ProductID); });

			CancelButton.onClick.AddListener(CancelClicked);
		}

		private void OnDestroy()
		{
			Coins100Button.onClick.RemoveListener(delegate { PurchaseProduct(Store.Coins100ProductID); });
			Coins1000Button.onClick.RemoveListener(delegate { PurchaseProduct(Store.Coins1000ProductID); });
			Coins10000Button.onClick.RemoveListener(delegate { PurchaseProduct(Store.Coins10000ProductID); });

			CancelButton.onClick.RemoveListener(CancelClicked);
		}
			

		public void RequestPurchase()
		{
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


		private void PurchaseProduct(string productID)
		{
			if (BuySound != null)
				AudioSource.PlayClipAtPoint(BuySound, Vector3.zero, FightManager.SFXVolume);

			Store.PurchaseProductID(productID);
			StartCoroutine(Hide());
		}
	
		private void CancelClicked()
		{
			if (CancelSound != null)
				AudioSource.PlayClipAtPoint(CancelSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}
	}


}
