using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class AreYouSure : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Image coinsPanel;
		public Text coinsValue;

		private int coinsToConfirm = 0;

		public Text ConfirmMessage;
		public float fadeTime;

		public Button yesButton;
		public Text yesText;
		public Button noButton;
		public Text noText;

		public Button okButton;
		public Text okText;

		private Action actionOnYes;

		private Image hilight;			// animated

		public AudioClip FadeSound;
		public AudioClip YesSound;
		public AudioClip NoSound;

		// power-up details panel
		public Image PowerUpPanel;
		public Image PowerUpIcon;
		public Text PowerUpName;
		public Text PowerUpDescription;
		public Text PowerUpCost;
		public Text PowerUpActivation;
		public Text PowerUpCoolDown;
		public Text PowerUpMessage;
		public Image SwipeUpImage;

//		public delegate void YesClickedDelegate();
//		public static YesClickedDelegate OnConfirmYes;

		public delegate void NoClickedDelegate();
		public static NoClickedDelegate OnCancelConfirm;


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;

			ActivateHilight(false);
			coinsPanel.gameObject.SetActive(false);

//			yesText.text = FightManager.Translate("yesPlease", false, true);
			yesText.text = FightManager.Translate("yes", false, true);
//			noText.text = FightManager.Translate("noThanks", false, true);
			noText.text = FightManager.Translate("cancel");
			okText.text = FightManager.Translate("ok");
		}

		private void OnEnable()
		{
			yesButton.onClick.AddListener(YesClicked);
			noButton.onClick.AddListener(NoClicked);
			okButton.onClick.AddListener(OkClicked);
		}

		private void OnDisable()
		{
			yesButton.onClick.RemoveListener(YesClicked);
			noButton.onClick.RemoveListener(NoClicked);
			okButton.onClick.RemoveListener(OkClicked);

			ClearPowerUpDetails();
		}
			
		public void Confirm(string message, int coins, Action onConfirm)
		{
			actionOnYes = onConfirm;
			StartCoroutine(Show(message, coins, false));
		}

		public void Confirm(PowerUpDetails powerUpDetails, Action onConfirm)
		{
			actionOnYes = onConfirm;
			StartCoroutine(Show(powerUpDetails));
		}

		public void ConfirmOk(string message, int coins)
		{
			StartCoroutine(Show(message, coins, true));
		}
			
		private IEnumerator Show(string message, int coins, bool okOnly)
		{
			if (string.IsNullOrEmpty(message))
				ConfirmMessage.text = "Are you sure?";
			else
				ConfirmMessage.text = message;

			yesButton.gameObject.SetActive(!okOnly);
			noButton.gameObject.SetActive(!okOnly);
			okButton.gameObject.SetActive(okOnly);

			background.enabled = true;
			panel.gameObject.SetActive(true);

			PowerUpPanel.gameObject.SetActive(false);

			coinsValue.text = (coins > 0) ? string.Format("{0:N0}", coins) : "";
			coinsPanel.gameObject.SetActive(coins > 0);
			coinsToConfirm = coins;
			
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

//			ActivateHilight(true);
			yield return null;
		}

		private IEnumerator Show(PowerUpDetails powerUpDetails)
		{
			if (powerUpDetails == null)
				yield break;

			okButton.gameObject.SetActive(false);

			PowerUpIcon.sprite = powerUpDetails.Icon;
			PowerUpName.text = powerUpDetails.Name;
			PowerUpDescription.text = powerUpDetails.Description;
			PowerUpCost.text = powerUpDetails.Cost;

			coinsToConfirm = powerUpDetails.CoinValue;

			PowerUpActivation.text = powerUpDetails.Activation;
//			SwipeUpImage.gameObject.SetActive(false);  		// not used      powerUpDetails.IsTrigger);
			PowerUpCoolDown.text = powerUpDetails.Cooldown;

			PowerUpMessage.text = powerUpDetails.Confirmation;

			ConfirmMessage.text = "";

			panel.transform.localScale = Vector3.zero;
			background.color = Color.clear;
			background.enabled = true;
			panel.gameObject.SetActive(true);
//			PowerUpPanel.gameObject.SetActive(true);

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

			PowerUpPanel.gameObject.SetActive(true);

			var animator = PowerUpPanel.GetComponent<Animator>();
			animator.SetTrigger("EnterConfirm");

			yield return null;
		}

		private void ClearPowerUpDetails()
		{
			PowerUpIcon.sprite = null;
			PowerUpName.text = "";
			PowerUpDescription.text = "";
			PowerUpCost.text = "";
			PowerUpActivation.text = "";
			PowerUpCoolDown.text = "";
			PowerUpMessage.text = "";

			coinsToConfirm = 0;
		}
			
		public IEnumerator Hide()
		{
			ActivateHilight(false);

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
				
			coinsPanel.gameObject.SetActive(false);
//			panel.transform.localScale = Vector3.one;

			PowerUpPanel.gameObject.SetActive(false);
			ClearPowerUpDetails();

			yield return null;
		}


		private void YesClicked()
		{
			// call the passed-in delegate
			if (actionOnYes != null)
			{
				// spend the coins!
				if (coinsToConfirm > 0)
				{
					FightManager.Coins -= coinsToConfirm;
					coinsToConfirm = 0;
				}

				actionOnYes();
			}

			if (YesSound != null)
				AudioSource.PlayClipAtPoint(YesSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());

//			if (OnConfirmYes != null)
//				OnConfirmYes();
		}

		private void NoClicked()
		{
			if (NoSound != null)
				AudioSource.PlayClipAtPoint(NoSound, Vector3.zero, FightManager.SFXVolume);
			
			StartCoroutine(Hide());

			if (OnCancelConfirm != null)
				OnCancelConfirm();
		}

		private void OkClicked()
		{
			if (YesSound != null)
				AudioSource.PlayClipAtPoint(YesSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());

			if (OnCancelConfirm != null)
				OnCancelConfirm();
		}

		private void ActivateHilight(bool activate)
		{
			if (hilight == null)
				hilight = transform.Find("Hilight").GetComponent<Image>();

			hilight.gameObject.SetActive(activate);
		}
	}
}
