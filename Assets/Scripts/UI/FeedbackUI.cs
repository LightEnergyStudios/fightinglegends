using UnityEngine;
using System.Collections;
using UnityEngine.UI;


namespace FightingLegends
{
	public class FeedbackUI : MonoBehaviour
	{
		public FeedbackFX feedbackFX;		// Animation
		public RoundFX roundFX;

		public Text Player1ComboCount;
		public Text Player1ComboLabel;	
		public Text Player2ComboCount;
		public Text Player2ComboLabel;

		public Text Player1State;
		public Text Player2State;

		public Text Player1Gauge;		// insufficient gauge message
		public Text Player2Gauge;

		public ParticleSystem Player1Fireworks;	
		public ParticleSystem Player2Fireworks;

		public ParticleSystem Player1Stars;			// sweep across state feedback
		public ParticleSystem Player2Stars;

		private Vector3 Player1StarsPosition;
		private Vector3 Player2StarsPosition;

		public AudioClip starAudio;

		private const string fxLayer = "FeedbackFX";		// default layer

		// level up feedback
		public Image LevelUpPanel;
		public Text LevelUpText;
		public ParticleSystem LevelUpFireworks;	
		public ParticleSystem LevelUpStars;			// sweep across level-up feedback
		private Vector3 LevelUpStarsPosition;
		public AudioClip levelUpSweepAudio;
		public AudioClip levelUpFireworksAudio;

		private const float levelUpWidth = 150.0f;	// for star sweep
		private const float levelUpTime = 0.25f;	// pulse level number
		private const float levelUpEntryTime = 0.1f;		// entry / exit of 'level'

		// power-up feedback
		public Image PowerUpPanel;
		public Text PowerUpText;
		public ParticleSystem PowerUpFireworks;	
		public ParticleSystem PowerUpStars;			// sweep across level-up feedback
		private Vector3 PowerUpStarsPosition;
		public AudioClip powerUpSweepAudio;
		public AudioClip powerUpFireworksAudio;

		public Animator powerUpAnimator;

		// trigger power-ups
		public Image TriggerPowerUp;				// displayed with feedback
		public Sprite VengeanceBooster;
		public Sprite Ignite;
		public Sprite HealthBooster;
		public Sprite PowerAttack;
		public Sprite SecondLife;

//		private const float powerUpTime = 0.25f;			// pulse level number
		private const float powerUpEntryTime = 0.15f;		// entry / exit of power-up panel
		private const float powerUpImageTime = 0.3f;		// entry / exit of power-up panel

		private const float stateCharWidth = 20.0f;			// approx width of each char in state feedback text (for star sweep)

		private const float pulseTextTime = 0.1f;
		private const float pulseTextScale = 1.75f;
		private const float pulseNoGaugeScale = 1.4f;
		private const float pulseNoGaugePause = 0.15f;		// pause while enlarged

		// training
		public Image NarrativePanel;
		public Text TrainingNarrative;
		public Text TrainingHeader;
		public Text TrainingDetails;
		public Image TrainingImage;
		public Text TrainingPrompt;
		public Image TrainingHorizontalFlash;

		public Sprite OkSprite;							// green tick
		public Sprite NotOkSprite;						// red cross
//		public AudioClip ResetSound;
//		public AudioClip OkSound;
//		public AudioClip NotOkSound;


		public delegate void StateFeedbackDelegate(bool player1, string stateFeedback);
		public static StateFeedbackDelegate OnStateFeedback;


		// 'Constructor'
		// NOT called when returning from background
		private void Awake()
		{
			
		}
	
		// initialization
		void Start()
		{
			feedbackFX.gameObject.SetActive(false);
			roundFX.gameObject.SetActive(false);

			// store star start positions for resetting sweep
			Player1StarsPosition = Player1Stars.transform.localPosition;
			Player2StarsPosition = Player2Stars.transform.localPosition;

			LevelUpStarsPosition = LevelUpStars.transform.localPosition;
		}

		private void OnDestroy()
		{
		}


		public void TriggerFeedbackFX(FeedbackFXType feedback, float xOffset = 0.0f, float yOffset = 0.0f, string layer = null)
		{
			if (feedbackFX != null)
			{
//				Debug.Log("TriggerFeedbackFX: " + feedback);
				feedbackFX.gameObject.SetActive(true);
				feedbackFX.gameObject.layer = LayerMask.NameToLayer((layer != null) ? layer : fxLayer);

				feedbackFX.TriggerFeedback(feedback, xOffset, yOffset);
			}
		}

		public void TriggerNumberFX(int number, float xOffset = 0.0f, float yOffset = 0.0f, string layer = null)
		{
			if (feedbackFX != null)
			{
				feedbackFX.gameObject.SetActive(true);
				feedbackFX.gameObject.layer = LayerMask.NameToLayer((layer != null) ? layer : fxLayer);

				feedbackFX.TriggerNumber(number, xOffset, yOffset);
			}
		}

		public void TriggerRoundFX(float xOffset = 0.0f, float yOffset = 0.0f)
		{
			if (roundFX != null)
			{
				roundFX.gameObject.SetActive(true);
				roundFX.TriggerRound(xOffset, yOffset);
			}
		}

		public void CancelFeedbackFX()
		{
//			Debug.Log("CancelFeedbackFX");

			if (feedbackFX != null)
				feedbackFX.VoidState();
		}

		public void CancelRoundFX()
		{
			if (roundFX != null)
				roundFX.VoidState();
		}


		public IEnumerator ComboFeedback(bool player1, int comboCount, float displayTime)
		{
			var label = player1 ? Player1ComboLabel : Player2ComboLabel;
			label.text = FightManager.Translate("hitCombo");

			var count = player1 ? Player1ComboCount : Player2ComboCount;
			count.transform.localScale = Vector3.one;
			count.text = comboCount.ToString();

			StartCoroutine(PulseText(count, pulseTextScale));

			if (displayTime <= 0.0f)
				yield break;
			
			yield return new WaitForSeconds(displayTime);
			ClearComboFeedback(player1);
		}

		public void ClearComboFeedback(bool player1)
		{
//			var feedback = player1 ? Player1Combo : Player2Combo;
//			feedback.text = "";

			if (player1)
			{
				Player1ComboCount.text = "";
				Player1ComboLabel.text = "";
			}
			else
			{
				Player2ComboCount.text = "";
				Player2ComboLabel.text = "";
			}
		}


		public IEnumerator GaugeFeedback(bool player1, string text, float displayTime)
		{
			var feedback = player1 ? Player1Gauge : Player2Gauge;

			feedback.text = text;
			StartCoroutine(PulseText(feedback, pulseNoGaugeScale, pulseNoGaugePause));

			if (displayTime <= 0.0f)
				yield break;

			yield return new WaitForSeconds(displayTime);
			ClearGaugeFeedback(player1);
		}

		public void ClearGaugeFeedback(bool player1)
		{
			var feedback = player1 ? Player1Gauge : Player2Gauge;
			feedback.text = "";
		}


		private IEnumerator StarSweep(bool player1, float distance)
		{
			var stars = player1 ? Player1Stars : Player2Stars;
			var fireworks = player1 ? Player1Fireworks : Player2Fireworks;
	
			var startPosition = player1 ? Player1StarsPosition : Player2StarsPosition;
			var targetPosition = new Vector3(startPosition.x + (player1 ? distance : -distance), startPosition.y, startPosition.z);
			float sweepTime = stars.main.duration;
			float t = 0.0f;

//			stars.gameObject.SetActive(true);
			stars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / sweepTime);

				stars.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			fireworks.transform.localPosition = targetPosition;
			fireworks.Play();

//			stars.gameObject.SetActive(false);
//			stars.transform.localPosition = player1 ? Player1StarsPosition : Player2StarsPosition;
			yield return null;
		}

		public IEnumerator StateFeedback(bool player1, string text, float displayTime, bool stars, bool silent, string layer = null)
		{
			var feedback = player1 ? Player1State : Player2State;

//			FeedbackText.layer = LayerMask.NameToLayer((layer != null) ? layer : fxLayer);
			feedback.gameObject.layer = LayerMask.NameToLayer((layer != null) ? layer : fxLayer);

			feedback.text = text;
//			StartCoroutine(PulseText(feedback));

			if (OnStateFeedback != null)
				OnStateFeedback(player1, text);

			if (stars)
			{
//				var fireworks = player1 ? Player1Fireworks : Player2Fireworks;
//				var fireworksWidth = text.Length * 25;
//				fireworks.transform.localPosition = new Vector3(player1 ? fireworksWidth / 2 : -fireworksWidth / 2, 25, 50);

				float textWidth = (float)text.Length * stateCharWidth;
				StartCoroutine(StarSweep(player1, textWidth));
			}
				
			if (displayTime <= 0.0f)
				yield break;

			yield return new WaitForSeconds(displayTime);
			ClearStateFeedback(player1);
		}

		public void ClearStateFeedback(bool player1)
		{
			var feedback = player1 ? Player1State : Player2State;
//			Debug.Log("ClearStateFeedback: " + feedback.text + " CLEARED at " + Time.frameCount);

//			feedback.gameObject.SetActive(false);
			feedback.text = "";

			if (OnStateFeedback != null)
				OnStateFeedback(player1, "");
		}


		private IEnumerator LevelUpStarSweep(float distance, bool silent)
		{
			var startPosition = new Vector3(LevelUpStarsPosition.x - distance, LevelUpStarsPosition.y, LevelUpStarsPosition.z);
			var targetPosition = new Vector3(startPosition.x + (distance * 2), startPosition.y, startPosition.z);
			float sweepTime = LevelUpStars.main.duration;
			float t = 0.0f;

			if (!silent && levelUpSweepAudio != null)
				AudioSource.PlayClipAtPoint(levelUpSweepAudio, Vector3.zero, FightManager.SFXVolume);

			LevelUpStars.gameObject.SetActive(true);
			LevelUpStars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / sweepTime);

				LevelUpStars.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			LevelUpStars.transform.localPosition = LevelUpStarsPosition;  // reset
				
//			LevelUpFireworks.transform.localPosition = targetPosition;
			LevelUpFireworks.Play();

			if (!silent && levelUpFireworksAudio != null)
				AudioSource.PlayClipAtPoint(levelUpFireworksAudio, Vector3.zero, FightManager.SFXVolume);
			
			yield return null;
		}

		public IEnumerator LevelUpFeedback(int level, float displayTime, bool stars, bool silent)
		{
			LevelUpText.text = level.ToString();
			LevelUpText.transform.localScale = Vector3.zero;

			var levelUpStartScale = new Vector3(0, 1, 1);				// expands along y
			var levelUpLargeScale = new Vector3(1.5f, 1.5f, 1.5f);		
			LevelUpPanel.transform.localScale = levelUpStartScale;
			LevelUpPanel.gameObject.SetActive(true);

			float t = 0;

			Color levelUpColour = LevelUpText.color;
			LevelUpText.color = Color.clear;

			// scale in  - expand along y
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / levelUpEntryTime);

				LevelUpPanel.transform.localScale = Vector3.Lerp(levelUpStartScale, levelUpLargeScale, t);
				yield return null;
			}

			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / (levelUpEntryTime / 2.0f));

				LevelUpPanel.transform.localScale = Vector3.Lerp(levelUpLargeScale, Vector3.one, t);
				yield return null;
			}

			// alpha in level number
			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / levelUpTime);

				LevelUpText.color = Color.Lerp(Color.clear, levelUpColour, t);
				LevelUpText.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
				yield return null;
			}

			// pulse level number up
			Vector3 startScale = new Vector3(1, 1, 1);
			Vector3 targetScale = new Vector3(3, 3, 3);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / levelUpTime);

				LevelUpText.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
				yield return null;
			}
				
			// stars and fireworks when large
			if (stars)
			{
				float textWidth = levelUpWidth; 		// (float)text.Length * 20.0f;
				yield return StartCoroutine(LevelUpStarSweep(textWidth, silent));
			}

			// pulse level number down
			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / levelUpTime);

				LevelUpText.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
				yield return null;
			}

			yield return new WaitForSeconds(displayTime);
			StartCoroutine(ClearLevelUpFeedback());
		}
			
		public IEnumerator ClearLevelUpFeedback()
		{
			if (! LevelUpPanel.gameObject.activeSelf)
				yield break;
			
			float t = 0;
			var targetScale = new Vector3(0,1,1);			// scale out along y

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / levelUpEntryTime);

				LevelUpPanel.transform.localScale = Vector3.Lerp(Vector3.one, targetScale, t);
				yield return null;
			}

			LevelUpPanel.gameObject.SetActive(false);
			LevelUpPanel.transform.localScale = Vector3.one;		// reset
			LevelUpText.text = "";
			yield return null;
		}


		private string TriggerPowerUpText(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case FightingLegends.PowerUp.SecondLife:
					return FightManager.Translate("secondLife", false, true);

				case FightingLegends.PowerUp.Ignite:
					return FightManager.Translate("ignite", false, true);

				case FightingLegends.PowerUp.HealthBooster:
					return FightManager.Translate("healthBoost", false, true);

				case FightingLegends.PowerUp.PowerAttack:
					return FightManager.Translate("powerAttack", false, true);

				case FightingLegends.PowerUp.VengeanceBooster:
					return FightManager.Translate("vengeanceBoost", false, true);

				case FightingLegends.PowerUp.None:
				default:
					return FightManager.Translate("powerUp", false, true);
			}
		}

		private IEnumerator PowerUpStarSweep(float distance, bool silent)
		{
			PowerUpStarsPosition = PowerUpStars.transform.localPosition;

			var startPosition = new Vector3(PowerUpStarsPosition.x - distance, PowerUpStarsPosition.y, PowerUpStarsPosition.z);
			var targetPosition = new Vector3(startPosition.x + (distance * 2), startPosition.y, startPosition.z);
			float sweepTime = PowerUpStars.main.duration;
			float t = 0.0f;

			if (!silent && levelUpSweepAudio != null)
				AudioSource.PlayClipAtPoint(powerUpSweepAudio, Vector3.zero, FightManager.SFXVolume);

			PowerUpStars.gameObject.SetActive(true);
			PowerUpStars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / sweepTime);

				PowerUpStars.transform.localPosition = Vector3.Lerp(startPosition, targetPosition, t);
				yield return null;
			}

			PowerUpStars.transform.localPosition = PowerUpStarsPosition;  // reset
			PowerUpFireworks.Play();

			if (!silent && powerUpFireworksAudio != null)
				AudioSource.PlayClipAtPoint(powerUpFireworksAudio, Vector3.zero, FightManager.SFXVolume);

			yield return null;
		}

		public IEnumerator PowerUpFeedback(PowerUp powerUp, float displayTime, bool stars, bool silent)
		{
			SetPowerUpImage(powerUp);
			PowerUpText.text = TriggerPowerUpText(powerUp);
			PowerUpPanel.gameObject.SetActive(true);

			powerUpAnimator.SetTrigger("PowerUpTrigger");

			// stars and fireworks
			if (stars)
			{
				float textWidth =  (float)PowerUpText.text.Length * stateCharWidth;
				StartCoroutine(PowerUpStarSweep(textWidth, silent));
			}
				
			yield return new WaitForSeconds(displayTime);
			ClearPowerUpFeedback();
		}

		private void SetPowerUpImage(PowerUp powerUp)
		{
			switch (powerUp)
			{
				case PowerUp.VengeanceBooster:
					TriggerPowerUp.sprite = VengeanceBooster;
					break;

				case PowerUp.Ignite:
					TriggerPowerUp.sprite = Ignite;
					break;

				case PowerUp.HealthBooster:
					TriggerPowerUp.sprite = HealthBooster;
					break;

				case PowerUp.PowerAttack:
					TriggerPowerUp.sprite = PowerAttack;
					break;

				case PowerUp.SecondLife:
					TriggerPowerUp.sprite = SecondLife;
					break;

				default:
					TriggerPowerUp.sprite = null;
					break;
			}

			TriggerPowerUp.gameObject.SetActive(TriggerPowerUp.sprite != null);
//			TriggerPowerUp.transform.localScale = Vector3.zero;
		}

			
		public void ClearPowerUpFeedback()
		{
			PowerUpPanel.gameObject.SetActive(false);
			PowerUpText.text = "";
		}


		private IEnumerator PulseText(Text text, float pulseScale, float pulsePause = 0)
		{
			float t = 0;

			// pulse  up
			Vector3 currentScale = text.transform.localScale;
			Vector3 startScale = new Vector3(1, 1, 1);
			Vector3 targetScale = new Vector3(pulseScale, pulseScale, pulseScale);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTextTime);

				text.transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
				yield return null;
			}

			if (pulsePause > 0)
				yield return new WaitForSeconds(pulsePause);

			// pulse down
			t = 0;
			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTextTime);

				text.transform.localScale = Vector3.Lerp(targetScale, startScale, t);
				yield return null;
			}

			yield return null;
		}

//		public void ShowLevel(bool player1, int level)
//		{
//			var levelText = player1 ? Player1Level : Player2Level;
//			levelText.gameObject.SetActive(true);
//			levelText.text = "Lv" + level;
//		}
//
//		public void HideLevel(bool player1)
//		{
//			var levelText = player1 ? Player1Level : Player2Level;
//			levelText.gameObject.SetActive(false);
//			levelText.text = "";
//		}
	}
}
