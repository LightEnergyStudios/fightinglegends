using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace FightingLegends
{
	public class PauseSettings : MenuCanvas
	{
		public Text titleLabel;
		public Text difficultyHeading;
		public Text volumeHeading;
		public Text themeHeading;

		public Text resetHintsLabel;
		public Text resetGameLabel;
		public Text newUserLabel;

//		public Text trainingHeading;
		public Text musicHeading;
		public Text sfxHeading;

		public DifficultySelector difficultySelector;

		// theme 
		public Button waterButton;
		public Button fireButton;
		public Button airButton;
		public Button earthButton;

		public Text waterLabel;
		public Text fireLabel;
		public Text airLabel;
		public Text earthLabel;

		public Image waterGlow;			// has animator
		public Image fireGlow;			// has animator
		public Image airGlow;			// has animator
		public Image earthGlow;			// has animator

		public ParticleSystem headerStars;		// when theme changed
		public ParticleSystem footerStars;		// when theme changed
		public float starSweepTime;
		private const float starSweepDistance = 390.0f;

//		// facebook
//		public FacebookManager facebookManager;
//		public Text FBUserName;
//		public Image FBProfilePic;

//		public Button quitButton;
//		public Text quitText;
//		private int quitClicks = 0;
//		private const int quitTimeout = 3;		// timeout seconds for 2nd click to quit game
//		private Coroutine quitCountdown;

		public Slider sfxSlider;
		public Slider musicSlider;
		public Text musicVolume;				// number (1-11)
		public Text sfxVolume;					// number (1-11)

		public Button slowDownButton;
		public Button speedUpButton;
		public Text gameFPS;

		public Toggle trainingNarrativeToggle;
		public Toggle stateFeedbackToggle;		// captions
		public Toggle hudToggle;				// captions
		public Toggle hintsToggle;				// info bubble

		public Text stateFeedbackLabel;
		public Text stateFeedbackOnOff;

		public Text hudLabel;
		public Text hudOnOff;

		public Text hintsLabel;
		public Text hintsOnOff;

		public Button resetHintsButton;
		public Button resetGameButton;
		public Button newUserButton;

		public Button buyCoinsButton;
		public Button trainingButton;
		public Text buyCoinsLabel;
		public Text trainingLabel;

//		public Color OnColour;				// green
//		public Color OffColour;				// red

		public Toggle smartAIToggle;
		public Toggle proactiveToggle;
		public Toggle reactiveToggle;
		public Toggle iterateToggle;
		public Toggle isolateToggle;

		private bool smartAI = false;
		private bool reactiveStrategies = true;
		private bool proactiveStrategies = true;
		private bool iterateStrategies = false;
		private bool isolateStrategy = false;

//		public Text Coins;		// not used! (see Options)
//		public Text Kudos;		// not used! (see Options)
//		public Text Stats;

		// animated entry
		private Animator animator;
		private bool animatingEntry = false;
		private bool animatedEntry = false;

		private FightManager fightManager;


		public void Start()
		{
			FightManager.OnThemeChanged += SetTheme;

			titleLabel.text = FightManager.Translate("settings");
			difficultyHeading.text = FightManager.Translate("arcadeModeDifficulty");
			volumeHeading.text = FightManager.Translate("volume");
			themeHeading.text = FightManager.Translate("theme");
//			captionsHeading.text = FightManager.Translate("captions");
//			hintsHeading.text = FightManager.Translate("hints");
			resetHintsLabel.text = FightManager.Translate("resetHints", true);
			resetGameLabel.text = FightManager.Translate("resetGame", true);

			stateFeedbackLabel.text = FightManager.Translate("captions");
			hudLabel.text = FightManager.Translate("hud");
			hintsLabel.text = FightManager.Translate("hints");

			buyCoinsLabel.text = FightManager.Translate("buy");
			trainingLabel.text = FightManager.Translate("ninjaSchool", true);

//			newUserLabel.text = FightManager.Translate("newUser", true);
			musicHeading.text = FightManager.Translate("music");
			sfxHeading.text = FightManager.Translate("sfx");

//			newFightLabel.text = FightManager.Translate("quitFight");

			waterLabel.text = FightManager.Translate("water");
			fireLabel.text = FightManager.Translate("fire");
			airLabel.text = FightManager.Translate("air");
			earthLabel.text = FightManager.Translate("earth");

			smartAIToggle.isOn = smartAI;
			reactiveToggle.isOn = reactiveStrategies;
			proactiveToggle.isOn = proactiveStrategies;
			iterateToggle.isOn = iterateStrategies;
			isolateToggle.isOn = isolateStrategy;

			ConfigAIToggles();
			UpdateFPS();
			SetVolumes();

			ThemeGlow();
		}

		private void OnEnable()
		{
			var fightManagerObject = GameObject.Find("FightManager");
			fightManager = fightManagerObject.GetComponent<FightManager>();

//			FightManager.OnCoinsChanged += CoinsChanged;
//			CoinsChanged(FightManager.Coins);				// set current value
//
//			FightManager.OnKudosChanged += KudosChanged;
//			KudosChanged(FightManager.Kudos);				// set current value

//			SetStats();

			AnimateEntry();

			trainingNarrativeToggle.isOn = FightManager.SavedGameStatus.ShowTrainingNarrative;
			stateFeedbackToggle.isOn = FightManager.SavedGameStatus.ShowStateFeedback;
			hudToggle.isOn = FightManager.SavedGameStatus.ShowHud;
			hintsToggle.isOn = FightManager.SavedGameStatus.ShowInfoBubbles;

			SetFightCaptionsOnOff(stateFeedbackToggle.isOn);
			SetHudOnOff(hudToggle.isOn);
			SetHintsOnOff(hintsToggle.isOn);
			SetTrainingNarrativeLabel(trainingNarrativeToggle.isOn);

			bool fromFight = NavigatedFrom == MenuType.Combat || NavigatedFrom == MenuType.WorldMap || NavigatedFrom == MenuType.MatchStats;

			difficultySelector.EnableDifficulties(!fromFight);

			resetGameButton.interactable = !fromFight;
			trainingButton.interactable = !fromFight;

			waterButton.onClick.AddListener(delegate { SetTheme(UITheme.Water); });
			fireButton.onClick.AddListener(delegate { SetTheme(UITheme.Fire); });
			airButton.onClick.AddListener(delegate { SetTheme(UITheme.Air); });
			earthButton.onClick.AddListener(delegate { SetTheme(UITheme.Earth); });

			resetHintsButton.onClick.AddListener(delegate { ConfirmResetHints(); });
			resetGameButton.onClick.AddListener(delegate { ConfirmResetGame(); });
			newUserButton.onClick.AddListener(delegate { RegisterNewUser(); });

			buyCoinsButton.onClick.AddListener(delegate { BuyCoins(); });
			trainingButton.onClick.AddListener(delegate { Training(); });

			sfxSlider.onValueChanged.AddListener(SFXVolumeChanged);
			musicSlider.onValueChanged.AddListener(MusicVolumeChanged);

			trainingNarrativeToggle.onValueChanged.AddListener(TrainingNarrativeToggled);
			stateFeedbackToggle.onValueChanged.AddListener(FightCaptionsToggled);
			hudToggle.onValueChanged.AddListener(HudToggled);
			hintsToggle.onValueChanged.AddListener(HintsToggled);

			smartAIToggle.onValueChanged.AddListener(SmartAIToggled);
			proactiveToggle.onValueChanged.AddListener(ProactiveToggled);
			reactiveToggle.onValueChanged.AddListener(ReactiveToggled);
			iterateToggle.onValueChanged.AddListener(IterateToggled);
			isolateToggle.onValueChanged.AddListener(IsolateToggled);

			if (difficultySelector != null)
				difficultySelector.OnDifficultySelected += SetDifficulty;
		}

		private void OnDisable()
		{
//			FightManager.OnCoinsChanged -= CoinsChanged;
//			FightManager.OnKudosChanged -= KudosChanged;

			waterButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Water); });
			fireButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Fire); });
			airButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Air); });
			earthButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Earth); });

			resetHintsButton.onClick.RemoveListener(delegate { ConfirmResetHints(); });
			resetGameButton.onClick.RemoveListener(delegate { ConfirmResetGame(); });
			newUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });

			sfxSlider.onValueChanged.RemoveListener(SFXVolumeChanged);
			musicSlider.onValueChanged.RemoveListener(MusicVolumeChanged);

			trainingNarrativeToggle.onValueChanged.RemoveListener(TrainingNarrativeToggled);
			stateFeedbackToggle.onValueChanged.RemoveListener(FightCaptionsToggled);
			hudToggle.onValueChanged.RemoveListener(HudToggled);
			hintsToggle.onValueChanged.RemoveListener(HintsToggled);

			smartAIToggle.onValueChanged.RemoveListener(SmartAIToggled);
			reactiveToggle.onValueChanged.RemoveListener(ReactiveToggled);
			proactiveToggle.onValueChanged.RemoveListener(ProactiveToggled);
			iterateToggle.onValueChanged.RemoveListener(IterateToggled);
			isolateToggle.onValueChanged.RemoveListener(IsolateToggled);

			if (difficultySelector != null)
				difficultySelector.OnDifficultySelected -= SetDifficulty;
		}

		private void OnDestroy()
		{
			FightManager.OnThemeChanged -= SetTheme;
		}

		private void AnimateEntry()
		{
			animator = GetComponent<Animator>();
			if (animator == null)
				return;

			animatingEntry = true;

			animator.enabled = true;
			animator.SetTrigger("SettingsEntry");
		}

		public void EntryComplete()
		{
			animatingEntry = false;
			animatedEntry = true;
			animator.enabled = false;
		}

		private void SetTheme(UITheme theme)
		{
			if (fightManager.SetTheme(theme))	
				StartCoroutine(HeaderFooterStars());		// sweep if theme changed
			
			ThemeGlow();
		}
			
//		private void CoinsChanged(int coins)
//		{
//			Coins.text = string.Format("{0:N0}", coins);		// thousands separator, for clarity
//		}
//
//		private void KudosChanged(float kudos)
//		{
//			Kudos.text = string.Format("{0:N0}", kudos);
//		}
	

//		private void SetStats()
//		{
//			var profileData = FightManager.SavedGameStatus;
//
//			Stats.text += string.Format("\nROUNDS WON: {0}", profileData.RoundsWon);
//			Stats.text += string.Format("\nROUNDS LOST: {0}", profileData.RoundsLost);
//
//			Stats.text = string.Format("\n\nARCADE MODE WINS: {0}", profileData.MatchesWon);
//			Stats.text += string.Format("\nARCADE MODE LOSSES: {0}", profileData.MatchesLost);
//			Stats.text += string.Format("\nSURVIVAL MODE BEST: {0}", profileData.BestSurvivalEndurance);
//
//			Stats.text += string.Format("\n\nSUCCESSFUL HITS: {0}", profileData.SuccessfulHits);
//			Stats.text += string.Format("\nBLOCKED HITS: {0}", profileData.BlockedHits);		// unsuccessful hits (opponent blocked)
//			Stats.text += string.Format("\nHITS TAKEN: {0}", profileData.HitsTaken);
//			Stats.text += string.Format("\nHITS BLOCKED: {0}", profileData.HitsBlocked);
//			Stats.text += string.Format("\nDAMAGE INFLICTED: {0}", (int) profileData.DamageInflicted);
//			Stats.text += string.Format("\nDAMAGE SUSTAINED: {0}", (int) profileData.DamageSustained);
//		}

		private void ConfirmResetHints()
		{
			string message = string.Format(FightManager.Translate("confirmResetHints"));
			FightManager.GetConfirmation(message, 0, ResetHints);
		}

		private void ResetHints()
		{
			FightManager.ResetInfoBubbleMessages();
		}


		private void ConfirmResetGame()
		{
			string message = string.Format(FightManager.Translate("confirmResetGame"));
			FightManager.GetConfirmation(message, 0, ResetGame);
		}

		private void ResetGame()
		{
			Hide();
			fightManager.ResetGame();
		}

		private void RegisterNewUser()
		{
			FightManager.RegisterNewUser();
		}

		private void BuyCoins()
		{

		}

		private void Training()
		{
			FightManager.SavedGameStatus.CompletedBasicTraining = false;		// TODO: for testing purposes only

			FightManager.CombatMode = FightMode.Training;
			FightManager.SavedGameStatus.NinjaSchoolFight = false;				// only after completed training

			fightManager.CleanupFighters();
			fightManager.SelectedLocation = FightManager.hawaii;
			fightManager.PauseSettingsChoice = MenuType.Combat;				// triggers fade to black and new menu
		}

		private void SetDifficulty(AIDifficulty difficulty)
		{
			switch (difficulty)
			{
				case AIDifficulty.Simple:
					SetSimple();
					break;

				case AIDifficulty.Easy:
					SetEasy();
					break;

				case AIDifficulty.Medium:
					SetMedium();
					break;

				case AIDifficulty.Hard:
					SetHard();
					break;

				case AIDifficulty.Brutal:
					SetBrutal();
					break;
			}
		}

		private void SetSimple()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Simple;
		}
		private void SetEasy()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Easy;
		}
		private void SetMedium()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Medium;
		}
		private void SetHard()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Hard;
		}
		private void SetBrutal()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Brutal;
		}
			
		private void ThemeGlow()
		{
			var theme = FightManager.SavedGameStatus.Theme;
	
			waterGlow.gameObject.SetActive(theme == UITheme.Water);
			fireGlow.gameObject.SetActive(theme == UITheme.Fire);
			airGlow.gameObject.SetActive(theme == UITheme.Air);
			earthGlow.gameObject.SetActive(theme == UITheme.Earth);
		}


		private IEnumerator HeaderFooterStars()
		{
			yield return StartCoroutine(ThemeStars(headerStars));
			StartCoroutine(ThemeStars(footerStars));
		}

		private IEnumerator ThemeStars(ParticleSystem stars)
		{
			float t = 0;
			Vector3 startPosition = new Vector3(-starSweepDistance, stars.transform.localPosition.y, stars.transform.localPosition.z);
			Vector3 endPosition = new Vector3(starSweepDistance, stars.transform.localPosition.y, stars.transform.localPosition.z);

			stars.Play();

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / starSweepTime); 
	
				stars.transform.localPosition = Vector3.Lerp(startPosition, endPosition, t);
				yield return null;
			}

			yield return null;
		}

		private void SFXVolumeChanged(float value)
		{
			float volume = value / sfxSlider.maxValue;
			fightManager.SetSFXVolume(volume);

			sfxVolume.text = ((int)value).ToString();
		}

		private void MusicVolumeChanged(float value)
		{
			float volume = value / musicSlider.maxValue;
			fightManager.SetMusicVolume(volume);

			musicVolume.text = ((int)value).ToString();
		}
			
		private void SetVolumes()
		{
			sfxSlider.value = (FightManager.SFXVolume > 0) ? sfxSlider.maxValue / FightManager.SFXVolume : sfxSlider.minValue;
			musicSlider.value = (FightManager.MusicVolume > 0) ? musicSlider.maxValue / FightManager.MusicVolume : musicSlider.minValue;
		}
			
		private void UpdateFPS()
		{
			gameFPS.text = string.Format("{0:0.##} FPS", fightManager.AnimationFPS);
		}

		private void TrainingNarrativeToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowTrainingNarrative = isOn;
			SetTrainingNarrativeLabel(isOn);
		}

		private void FightCaptionsToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowStateFeedback = isOn;
			SetFightCaptionsOnOff(isOn);
		}

		private void HudToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowHud = isOn;
			SetHudOnOff(isOn);

			fightManager.GameUIVisible(isOn);
		}

		private void SetFightCaptionsOnOff(bool isOn)
		{
//			var label = stateFeedbackToggle.transform.Find("Label").GetComponent<Text>();
//			label.text = isOn ? FightManager.Translate("captionsOn", true) : FightManager.Translate("captionsOff", true);
			stateFeedbackOnOff.text = isOn ? FightManager.Translate("on") : FightManager.Translate("off");

//			stateFeedbackToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void SetHudOnOff(bool isOn)
		{
//			var label = hudToggle.transform.Find("Label").GetComponent<Text>();
//			label.text = isOn ? FightManager.Translate("hudOn", true) : FightManager.Translate("hudOff", true);
			hudOnOff.text = isOn ? FightManager.Translate("on") : FightManager.Translate("off");

//			hudToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void HintsToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowInfoBubbles = isOn;
			SetHintsOnOff(isOn);
		}

		private void SetHintsOnOff(bool isOn)
		{
//			var label = hintsToggle.transform.Find("Label").GetComponent<Text>();
//			label.text = isOn ? FightManager.Translate("hintsOn", true) : FightManager.Translate("hintsOff", true);
			hintsOnOff.text = isOn ? FightManager.Translate("on") : FightManager.Translate("off");

//			hintsToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void SetTrainingNarrativeLabel(bool isOn)
		{
			var label = trainingNarrativeToggle.transform.Find("Label").GetComponent<Text>();
			label.text = FightManager.Translate("narrative") + " " + (isOn ? FightManager.Translate("on") : FightManager.Translate("off"));
		}

		#region AI testing

		private void ConfigAIToggles()
		{
			reactiveToggle.interactable = smartAI;
			proactiveToggle.interactable = smartAI;
			iterateToggle.interactable = smartAI; 
			isolateToggle.interactable = smartAI && iterateStrategies;

			if (! iterateStrategies)
				isolateStrategy = false;
		}

		private void SmartAIToggled(bool isOn)
		{
			smartAI = isOn;
			ConfigAIToggles();
			SaveSettings();
		}

		private void ReactiveToggled(bool isOn)
		{
			reactiveStrategies = isOn;
			SaveSettings();
		}

		private void ProactiveToggled(bool isOn)
		{
			proactiveStrategies = isOn;
			SaveSettings();
		}

		private void IterateToggled(bool isOn)
		{
			iterateStrategies = isOn;

//			if (! iterateStrategies)
//				isolateStrategy = false;
			
			ConfigAIToggles();
			SaveSettings();
		}

		private void IsolateToggled(bool isOn)
		{
			isolateStrategy = isOn;
			SaveSettings();
		}

		private void SaveSettings()
		{
//			PlayerPrefs.SetInt("SmartAI", smartAI ? 1 : 0);
//			PlayerPrefs.SetInt("ReactiveAI", reactiveStrategies ? 1 : 0);
//			PlayerPrefs.SetInt("ProactiveAI", proactiveStrategies ? 1 : 0);
//			PlayerPrefs.SetInt("IterateAI", iterateStrategies ? 1 : 0);
//			PlayerPrefs.SetInt("IsolateAI", isolateStrategy ? 1 : 0);
//			PlayerPrefs.Save();
		}

		#endregion
	}
}
