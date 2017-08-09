using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System;


namespace FightingLegends
{
	public class InsertContinueCoin : MonoBehaviour
	{
		public Image countdownPanel;
		public Text continueMessage;
		public Button continueButton;
		public Text continueLabel;

		public AudioClip MusicTrack;
		public AudioClip FadeSound;
		public AudioClip ContinueSound;

		private Action continueAction = null;				// if 'insert coin' clicked
		private Action exitAction = null;					// if timed-out

		private IEnumerator countdownCoroutine;				// so it can be stopped

		private const float fadeInTime = 0.25f;				// continue message / button
		private const float countdownPause = 3.0f;			// before countdown starts
		private const float exitPause = 2.0f;				// before countdown exits

		private const float feedbackX = -140.0f;
		private const float feedbackY = -50.0f;
		private const string feedbackLayer = "Curtain";		// so curtain camera picks it up

		private FightManager fightManager;


		void Awake()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

			continueLabel.text = FightManager.Translate("continue", false, true);
		}


		private void OnEnable()
		{
			continueButton.onClick.AddListener(Continue);
		}


		private void OnDisable()
		{
			continueButton.onClick.RemoveListener(Continue);
		}
			

		public void Countdown(Action actionOnContinue, Action actionOnExit, string message = null)
		{
			continueAction = actionOnContinue;
			exitAction = actionOnExit;

			if (MusicTrack != null)
				SceneryManager.PlayMusicTrack(MusicTrack);

			StartCoroutine(StartCountdown(message));
		}
			

		private IEnumerator StartCountdown(string message)
		{
			yield return new WaitForSeconds(countdownPause);

//			countdownPanel.gameObject.SetActive(true);

			if (ContinueSound != null)
				AudioSource.PlayClipAtPoint(ContinueSound, Vector3.zero, FightManager.SFXVolume);

			if (string.IsNullOrEmpty(message))
				continueMessage.text = FightManager.Translate("insertContinueCoin");
			else
				continueMessage.text = message;

			StartCoroutine(CountdownFadeIn());

			countdownCoroutine = CountdownFX();
			yield return StartCoroutine(countdownCoroutine);

			// didn't insert coin...
			if (exitAction != null)
				exitAction();

			countdownPanel.gameObject.SetActive(false);
			yield return null;
		}


		private IEnumerator CountdownFadeIn()
		{
			continueMessage.color = Color.clear;
			continueButton.image.color = Color.clear;
			countdownPanel.gameObject.SetActive(true);

			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeInTime); 

				continueMessage.color = Color.Lerp(Color.clear, Color.white, t);
				continueButton.image.color = Color.Lerp(Color.clear, Color.white, t);
				yield return null;
			}

			yield return null;
		}

		private IEnumerator CountdownFX()
		{
			continueButton.interactable = true;

			for (int counter = 9; counter >= 0; counter--)
			{
				fightManager.TriggerNumberFX(counter, feedbackX, feedbackY, feedbackLayer);
				fightManager.PlayNumberSound(counter);

				yield return new WaitForSeconds(1.0f);
			}

			// timed out!
			continueButton.interactable = false;

			yield return new WaitForSeconds(exitPause);			// so music plays out
		}


		private void StopCountdown()
		{
			if (countdownCoroutine != null)
				StopCoroutine(countdownCoroutine);

			fightManager.CancelFeedbackFX();

			if (MusicTrack != null)
				SceneryManager.StopMusic();
		}


		private void Continue()
		{
			if (ContinueSound != null)
				AudioSource.PlayClipAtPoint(ContinueSound, Vector3.zero, FightManager.SFXVolume);

			StopCountdown();

			// insert coin!
			FightManager.Coins--;

			if (continueAction != null)
				continueAction();
			
			countdownPanel.gameObject.SetActive(false);
		}
	}
}
