using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class ChallengeResult : MonoBehaviour
	{
		public Image panel;
		public Color panelColour;	
		private Vector3 panelScale;

		private Image background;
		public Color backgroundColour;	

		public Image coinsPanel;
		public Text coinsValue;

		private ChallengeData completedChallenge = null;
		private bool challengeWon = false;		// this user

		public Text Congratuations;				// or commiserations
		public Text WinMessage;					// or loss message
		public Text ChallengeName;
		public Text ChallengeUserId;
		public float fadeTime;

		public ParticleSystem Fireworks;
		public ParticleSystem Stars;

		public Button okButton;
		public Text okText;

		private Action actionOnOk;

		public AudioClip ShowSound;
		public AudioClip OkSound;

		private const float pauseBeforeCanOk = 1.0f;
		private bool canOk = false;

//		public delegate void YesClickedDelegate();
//		public static YesClickedDelegate OnConfirmYes;
//
//		public delegate void NoClickedDelegate();
//		public static NoClickedDelegate OnCancelConfirm;


		void Awake()
		{
			background = GetComponent<Image>();
			background.enabled = false;
			background.color = backgroundColour;

			panel.gameObject.SetActive(false);
			panel.color = panelColour;
			panelScale = panel.transform.localScale;

			coinsPanel.gameObject.SetActive(false);

//			WinMessage.text = FightManager.Translate("yourTeamWon", false, true);
			okText.text = FightManager.Translate("ok", false, true);
		}

		private void OnEnable()
		{
			okButton.onClick.AddListener(TakeWinnings);
		}

		private void OnDisable()
		{
			okButton.onClick.RemoveListener(TakeWinnings);
		}

		private void Update() 
		{
			if (canOk && ((Input.touchCount == 1 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0)))	// left button
			{
//				if (panel.gameObject.activeSelf)
					TakeWinnings();
			}
		}
			
		// public entry point 
		public void Notify(ChallengeData challenge, bool wonChallenge, Action onOk)
		{
			completedChallenge = challenge;
			challengeWon = wonChallenge;
			actionOnOk = onOk;

			Congratuations.text = wonChallenge ? FightManager.Translate("congratulations", false, true) : FightManager.Translate("betterLuckNextTime", false, true);
			WinMessage.text = wonChallenge ? FightManager.Translate("yourTeamWon", false, true) : FightManager.Translate("yourTeamLost", false, true);
			ChallengeName.text = challenge.Name;
			ChallengeUserId.text = challenge.UserId;

			StartCoroutine(Show());
		}
			

		private IEnumerator Show()
		{
			if (completedChallenge == null)
				yield break;
		
			background.enabled = true;
			panel.gameObject.SetActive(true);

			var winnings = TeamSelect.ChallengePot(completedChallenge);

			coinsValue.text = string.Format("{0:N0}", winnings);
			coinsPanel.gameObject.SetActive(winnings > 0);

			panel.transform.localScale = Vector3.zero;
			background.color = Color.clear;

			Stars.Play();

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

			if (challengeWon)
				Fireworks.Play();

			yield return new WaitForSeconds(pauseBeforeCanOk);
			canOk = true;

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
				
			coinsPanel.gameObject.SetActive(false);
//			panel.transform.localScale = Vector3.one;

			yield return null;
		}


		private void TakeWinnings()
		{
			canOk = false;

			// call the passed-in delegate
			if (actionOnOk != null)
			{
				var winnings = TeamSelect.ChallengePot(completedChallenge);

				// take the coins!
				if (winnings > 0)
				{
					FightManager.Coins += winnings;

					FightManager.SavedGameStatus.TotalChallengeWinnings += winnings;
					FirebaseManager.PostLeaderboardScore(Leaderboard.ChallengeWinnings, FightManager.SavedGameStatus.TotalChallengeWinnings);
				}

				actionOnOk();
			}

			if (OkSound != null)
				AudioSource.PlayClipAtPoint(OkSound, Vector3.zero, FightManager.SFXVolume);

			StartCoroutine(Hide());
		}
	}
}
