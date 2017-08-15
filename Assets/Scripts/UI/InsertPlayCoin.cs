using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class InsertPlayCoin : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Text walletCoins;

		public Image coinsPanel;
		public Text coinsValue;

		private int coinsToInsert = 0;

		public Text InsertMessage;
		public float fadeTime;

		private const float insertCoinPause = 0.5f;			// pause to show wallet updated

		public Button insertButton;
		public Text insertText;
		public Button cancelButton;
		public Text cancelText;

		private Action actionOnYes;
		private Animator animator;

		public AudioClip FadeSound;
		public AudioClip InsertSound;
		public AudioClip CancelSound;
		public AudioClip CoinSound;

//		public delegate void YesClickedDelegate();
//		public static YesClickedDelegate OnConfirmYes;

//		public delegate void NoClickedDelegate();
//		public static NoClickedDelegate OnCancelInsert;


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;

			coinsPanel.gameObject.SetActive(false);

//			insertText.text = FightManager.Translate("insertCoin", false, true);
			insertText.text = FightManager.Translate("play");
			cancelText.text = FightManager.Translate("cancel");

			InsertMessage.text = FightManager.Translate("insertCoinToPlay");

			animator = GetComponent<Animator>();
		}

		private void OnEnable()
		{
			insertButton.onClick.AddListener(InsertClicked);
			cancelButton.onClick.AddListener(CancelClicked);

//			SetWalletValue(FightManager.Coins, false);				// set current value
		}

		private void OnDestroy()
		{
			insertButton.onClick.RemoveListener(InsertClicked);
			cancelButton.onClick.RemoveListener(CancelClicked);

			animator.enabled = false;
		}
			
		public void ConfirmInsertCoin(Action onConfirm, string message = "", int coins = 1) 
		{
			actionOnYes = onConfirm;

			SetWalletValue(FightManager.Coins, false);				// set current value

//			animator.enabled = true;
			StartCoroutine(Show(message, coins));
		}
			
		private IEnumerator Show(string message, int coins)
		{
			if (coins <= 0)
				yield break;
			
			if (string.IsNullOrEmpty(message))
				InsertMessage.text = FightManager.Translate("insertCoinToPlay");
			else
				InsertMessage.text = message;

			animator.enabled = true;

			background.enabled = true;
			panel.gameObject.SetActive(true);

			insertButton.interactable = true;
			cancelButton.interactable = true;

			coinsValue.text = "x " + string.Format("{0:N0}", coins);
			coinsPanel.gameObject.SetActive(true);
			coinsToInsert = coins;	
			
			panel.transform.localScale = Vector3.zero;
			background.color = Color.clear;

			if (FadeSound != null)
				AudioSource.PlayClipAtPoint(FadeSound, Vector3.zero, FightManager.SFXVolume);

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
			
		private IEnumerator Hide(bool pause)
		{
//			if (FadeSound != null)
//				AudioSource.PlayClipAtPoint(FadeSound, Vector3.zero, FightManager.SFXVolume);

//			animator.enabled = false;

			if (pause)
				yield return new WaitForSeconds(insertCoinPause);			// to show wallet updated

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

//				panel.transform.localScale = Vector3.Lerp(panelScale, Vector3.zero, t);
				background.color = Color.Lerp(backgroundColour, Color.clear, t);
				yield return null;
			}

			background.enabled = false;
			panel.gameObject.SetActive(false);
//			panel.transform.localScale = panelScale;
				
			coinsPanel.gameObject.SetActive(false);
//			panel.transform.localScale = Vector3.one;

			animator.enabled = false;
			yield return null;
		}


		private void InsertClicked()
		{
			insertButton.interactable = false;
			cancelButton.interactable = false;
			animator.enabled = false;

			// call the passed-in delegate
			if (actionOnYes != null)
			{
				// spend the coins!
				if (coinsToInsert > 0)
				{
					FightManager.Coins -= coinsToInsert;
					coinsToInsert = 0;
				}

				DeductCoins();		// display only
				actionOnYes();
			}

//			if (InsertSound != null)
//				AudioSource.PlayClipAtPoint(InsertSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide(true));

//			if (OnConfirmYes != null)
//				OnConfirmYes();
		}

		private void CancelClicked()
		{
			insertButton.interactable = false;
			cancelButton.interactable = false;

			if (CancelSound != null)
				AudioSource.PlayClipAtPoint(CancelSound, Vector3.zero, FightManager.SFXVolume);
			
			StartCoroutine(Hide(false));

//			if (OnCancelInsert != null)
//				OnCancelInsert();
		}

		public void ResetCoins()
		{
			SetWalletValue(FightManager.Coins, false);
		}

		private void DeductCoins()
		{
			SetWalletValue(FightManager.Coins - coinsToInsert, true);
		}

		public void PlayCoinSound()
		{
			if (InsertSound != null)
				AudioSource.PlayClipAtPoint(InsertSound, Vector3.zero, FightManager.SFXVolume);
		}

		private void SetWalletValue(int coins, bool sound)
		{	
			walletCoins.text = string.Format("{0:N0}", coins);		// thousands separator

			if (sound && CoinSound != null)
				AudioSource.PlayClipAtPoint(CoinSound, Vector3.zero, FightManager.SFXVolume);
		}
	}
}
