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
//		public Text captionsHeading;
//		public Text hintsHeading;
		public Text resetHintsLabel;
		public Text resetGameLabel;
		public Text newUserLabel;

		public Text trainingHeading;
		public Text musicHeading;
		public Text sfxHeading;

		public Button newFightButton;		// not used
		public Button trainButton;			// not used
		public Button friendsButton;		// not used
		public Button storeButton;			// not used
		public Button powerUpButton;		// not used
		public Button buyCoinsButton;		// not used

		public Text newFightLabel;

//		public Button simpleButton;
//		public Button easyButton;
//		public Button mediumButton;
//		public Button hardButton;
//		public Button brutalButton;
//
//		public Text simpleLabel;
//		public Text easyLabel;
//		public Text mediumLabel;
//		public Text hardLabel;
//		public Text brutalLabel;
//
//		public Image simpleGlow;		// has animator
//		public Image easyGlow;			// has animator
//		public Image mediumGlow;		// has animator
//		public Image hardGlow;			// has animator
//		public Image brutalGlow;		// has animator

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

		// facebook
		public FacebookManager facebookManager;
		public Text FBUserName;
		public Image FBProfilePic;

//		public Button quitButton;
//		public Text quitText;
//		private int quitClicks = 0;
//		private const int quitTimeout = 3;		// timeout seconds for 2nd click to quit game
//		private Coroutine quitCountdown;

		public Slider sfxSlider;
		public Slider musicSlider;

		public Button slowDownButton;
		public Button speedUpButton;
		public Text gameFPS;

		public Toggle trainingNarrativeToggle;
		public Toggle stateFeedbackToggle;		// captions
		public Toggle hudToggle;				// captions
		public Toggle hintsToggle;				// info bubble

		public Button resetHintsButton;
		public Button resetGameButton;
		public Button newUserButton;

		public Color OnColour;				// green
		public Color OffColour;				// red

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

		public Text Coins;		// not used! (see Options)
		public Text Kudos;		// not used! (see Options)
		public Text Stats;

		private FightManager fightManager;

		public delegate void QuitFightDelegate();
		public static QuitFightDelegate OnQuitFight;


		public void Start()
		{
			FightManager.OnThemeChanged += SetTheme;

			titleLabel.text = FightManager.Translate("settings");
			difficultyHeading.text = FightManager.Translate("arcadeModeDifficulty");
			volumeHeading.text = FightManager.Translate("volume");
			themeHeading.text = FightManager.Translate("theme");
//			captionsHeading.text = FightManager.Translate("captions");
//			hintsHeading.text = FightManager.Translate("hints");
			resetHintsLabel.text = FightManager.Translate("resetHits", true);
			resetGameLabel.text = FightManager.Translate("resetGame", true);
			newUserLabel.text = FightManager.Translate("newUser", true);
			musicHeading.text = FightManager.Translate("music");
			sfxHeading.text = FightManager.Translate("sfx");

			newFightLabel.text = FightManager.Translate("quitFight");

//			simpleLabel.text = FightManager.Translate("simple");
//			easyLabel.text = FightManager.Translate("easy");
//			mediumLabel.text = FightManager.Translate("medium");
//			hardLabel.text = FightManager.Translate("hard");
//			brutalLabel.text = FightManager.Translate("brutal");

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

			FightManager.OnCoinsChanged += CoinsChanged;
			CoinsChanged(FightManager.Coins);				// set current value

			FightManager.OnKudosChanged += KudosChanged;
			KudosChanged(FightManager.Kudos);				// set current value

//			if (facebookManager != null)
//			{
//				// set user name and profile pic if facebookManager already logged in
//				if (facebookManager.IsLoggedIn)
//				{
//					if (facebookManager.UserName != null)
//						SetFBUserName(facebookManager.UserName.text);
//					
//					if (facebookManager.ProfilePic != null)
//						SetFBProfilePic(facebookManager.ProfilePic.sprite);
//					else
//						FBProfilePic.gameObject.SetActive(false);
//				}
//
//				FacebookManager.OnLookupUserName += SetFBUserName;
//				FacebookManager.OnLookupProfilePic += SetFBProfilePic;
//			}

			SetStats();

			trainingNarrativeToggle.isOn = FightManager.SavedGameStatus.ShowTrainingNarrative;
			stateFeedbackToggle.isOn = FightManager.SavedGameStatus.ShowStateFeedback;
			hudToggle.isOn = FightManager.SavedGameStatus.ShowHud;
			hintsToggle.isOn = FightManager.SavedGameStatus.ShowInfoBubbles;

			SetFightCaptionsLabel(stateFeedbackToggle.isOn);
			SetHudLabel(hudToggle.isOn);
			SetHintsLabel(hintsToggle.isOn);
			SetTrainingNarrativeLabel(trainingNarrativeToggle.isOn);

			bool fromFight = NavigatedFrom == MenuType.Combat || NavigatedFrom == MenuType.WorldMap || NavigatedFrom == MenuType.MatchStats;
//			newFightButton.interactable = fromFight;
//			newFightButton.onClick.AddListener(ConfirmQuitFight);

//			trainButton.onClick.AddListener(RepeatTraining);
//			friendsButton.onClick.AddListener(ShowFacebook);
//			storeButton.onClick.AddListener(ShowStore);
//			powerUpButton.onClick.AddListener(ShowStorePowerUp);
//			buyCoinsButton.onClick.AddListener(ShowStoreBuyCoins);

			difficultySelector.EnableDifficulties(!fromFight);

//			simpleButton.interactable = !fromFight;
//			easyButton.interactable = !fromFight;
//			mediumButton.interactable = !fromFight;
//			hardButton.interactable = !fromFight;
//			brutalButton.interactable = !fromFight;

			resetGameButton.interactable = !fromFight;

//			simpleButton.onClick.AddListener(delegate { ConfirmSetDifficulty(AIDifficulty.Simple); });
//			easyButton.onClick.AddListener(delegate { ConfirmSetDifficulty(AIDifficulty.Easy); });
//			mediumButton.onClick.AddListener(delegate { ConfirmSetDifficulty(AIDifficulty.Medium); });
//			hardButton.onClick.AddListener(delegate { ConfirmSetDifficulty(AIDifficulty.Hard); });
//			brutalButton.onClick.AddListener(delegate { ConfirmSetDifficulty(AIDifficulty.Brutal); });

			waterButton.onClick.AddListener(delegate { SetTheme(UITheme.Water); });
			fireButton.onClick.AddListener(delegate { SetTheme(UITheme.Fire); });
			airButton.onClick.AddListener(delegate { SetTheme(UITheme.Air); });
			earthButton.onClick.AddListener(delegate { SetTheme(UITheme.Earth); });

			resetHintsButton.onClick.AddListener(delegate { ConfirmResetHints(); });
			resetGameButton.onClick.AddListener(delegate { ConfirmResetGame(); });
			newUserButton.onClick.AddListener(delegate { RegisterNewUser(); });

			friendsButton.interactable = FacebookManager.FacebookOk;

//			quitButton.onClick.AddListener(QuitClicked);

			sfxSlider.onValueChanged.AddListener(SFXVolumeChanged);
			musicSlider.onValueChanged.AddListener(MusicVolumeChanged);

//			speedUpButton.onClick.AddListener(SpeedUp);
//			slowDownButton.onClick.AddListener(SlowDown);

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
			FightManager.OnCoinsChanged -= CoinsChanged;
			FightManager.OnKudosChanged -= KudosChanged;

//			if (facebookManager != null)
//			{
//				FacebookManager.OnLookupUserName -= SetFBUserName;
//				FacebookManager.OnLookupProfilePic -= SetFBProfilePic;
//			}

//			newFightButton.onClick.RemoveListener(ConfirmQuitFight);
//			trainButton.onClick.RemoveListener(RepeatTraining);
//			friendsButton.onClick.RemoveListener(ShowFacebook);
//			storeButton.onClick.RemoveListener(ShowStore);

//			powerUpButton.onClick.RemoveListener(ShowStorePowerUp);
//			buyCoinsButton.onClick.RemoveListener(ShowStoreBuyCoins);

//			postScoreButton.onClick.RemoveListener(PostFBScore);

//			simpleButton.onClick.RemoveListener(delegate { ConfirmSetDifficulty(AIDifficulty.Simple); });
//			easyButton.onClick.RemoveListener(delegate { ConfirmSetDifficulty(AIDifficulty.Easy); });
//			mediumButton.onClick.RemoveListener(delegate { ConfirmSetDifficulty(AIDifficulty.Medium); });
//			hardButton.onClick.RemoveListener(delegate { ConfirmSetDifficulty(AIDifficulty.Hard); });
//			brutalButton.onClick.RemoveListener(delegate { ConfirmSetDifficulty(AIDifficulty.Brutal); });

			waterButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Water); });
			fireButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Fire); });
			airButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Air); });
			earthButton.onClick.RemoveListener(delegate { SetTheme(UITheme.Earth); });

			resetHintsButton.onClick.RemoveListener(delegate { ConfirmResetHints(); });
			resetGameButton.onClick.RemoveListener(delegate { ConfirmResetGame(); });
			newUserButton.onClick.RemoveListener(delegate { RegisterNewUser(); });


//			quitButton.onClick.RemoveListener(QuitClicked);

//			adButton.onClick.RemoveListener(ShowAdvert);

			sfxSlider.onValueChanged.RemoveListener(SFXVolumeChanged);
			musicSlider.onValueChanged.RemoveListener(MusicVolumeChanged);

//			speedUpButton.onClick.RemoveListener(SpeedUp);
//			slowDownButton.onClick.RemoveListener(SlowDown);

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

		private void SetTheme(UITheme theme)
		{
			if (fightManager.SetTheme(theme))	
				StartCoroutine(HeaderFooterStars());		// sweep if theme changed
			
			ThemeGlow();
		}
			
		private void CoinsChanged(int coins)
		{
			Coins.text = string.Format("{0:N0}", coins);		// thousands separator, for clarity
		}

		private void KudosChanged(float kudos)
		{
			Kudos.text = string.Format("{0:N0}", kudos);
		}

		private void ConfirmQuitFight()
		{
			if (FightManager.CombatMode == FightMode.Dojo)
				FightManager.GetConfirmation(FightManager.Translate("confirmExitDojo"), 0, QuitFight);
			else
				FightManager.GetConfirmation(FightManager.Translate("confirmQuitFight"), 0, QuitFight);
		}

		private void QuitFight()
		{
			fightManager.CleanupFighters();

			if (FightManager.CombatMode == FightMode.Dojo)									
				fightManager.PauseSettingsChoice = MenuType.Dojo;				// triggers fade to black and new menu
			else
				fightManager.PauseSettingsChoice = MenuType.ModeSelect;			// triggers fade to black and new menu

			if (OnQuitFight != null)
				OnQuitFight();
		}

//		private void ShowFacebook()
//		{
//			if (FacebookManager.FacebookOk)
//				fightManager.PauseSettingsChoice = MenuType.Facebook;			// triggers fade to black and new menu
//		}

//		private void ShowStore()
//		{
//			fightManager.PauseSettingsChoice = MenuType.Store;				// triggers fade to black and new menu
//		}

//		private void ShowStorePowerUp()
//		{
//			fightManager.PauseSettingsChoice = MenuType.Store;				// triggers fade to black and new menu
//			fightManager.SelectedMenuOverlay = MenuOverlay.PowerUp;			// requires a selected fighter!
//		}

//		private void ShowStoreBuyCoins()
//		{
//			fightManager.PauseSettingsChoice = MenuType.Store;				// triggers fade to black and new menu
//			fightManager.SelectedMenuOverlay = MenuOverlay.BuyCoins;
//
////			fightManager.StoreBuyOverlay(true);
//		}

//		private void ShowAdvert()
//		{
			//TODO:
//			fightManager.PauseSettingsChoice = MenuType.Advert;				// triggers fade to black and new menu
//		}
			

		private void SetStats()
		{
			var profileData = FightManager.SavedGameStatus;

			Stats.text += string.Format("\nROUNDS WON: {0}", profileData.RoundsWon);
			Stats.text += string.Format("\nROUNDS LOST: {0}", profileData.RoundsLost);

			Stats.text = string.Format("\n\nARCADE MODE WINS: {0}", profileData.MatchesWon);
			Stats.text += string.Format("\nARCADE MODE LOSSES: {0}", profileData.MatchesLost);
			Stats.text += string.Format("\nSURVIVAL MODE BEST: {0}", profileData.BestSurvivalEndurance);

			Stats.text += string.Format("\n\nSUCCESSFUL HITS: {0}", profileData.SuccessfulHits);
			Stats.text += string.Format("\nBLOCKED HITS: {0}", profileData.BlockedHits);		// unsuccessful hits (opponent blocked)
			Stats.text += string.Format("\nHITS TAKEN: {0}", profileData.HitsTaken);
			Stats.text += string.Format("\nHITS BLOCKED: {0}", profileData.HitsBlocked);
			Stats.text += string.Format("\nDAMAGE INFLICTED: {0}", (int) profileData.DamageInflicted);
			Stats.text += string.Format("\nDAMAGE SUSTAINED: {0}", (int) profileData.DamageSustained);
		}

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

	
//		private void ConfirmSetDifficulty(AIDifficulty difficulty)
//		{
//			if (difficulty == FightManager.SavedGameStatus.Difficulty)		// no change
//				return;
//			
//			string message = string.Format(FightManager.Translate("confirmChangeDifficulty"), difficulty.ToString().ToUpper());
//			Action onYes = null;
//
//			switch (difficulty)
//			{
//				case AIDifficulty.Simple:
//					onYes = SetSimple;
//					break;
//
//				case AIDifficulty.Easy:
//					onYes = SetEasy;
//					break;
//
//				case AIDifficulty.Medium:
//					onYes = SetMedium;
//					break;
//
//				case AIDifficulty.Hard:
//					onYes = SetHard;
//					break;
//
//				case AIDifficulty.Brutal:
//					onYes = SetBrutal;
//					break;
//			}
//
//			FightManager.GetConfirmation(message, 0, onYes);
//		}

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
//			DifficultyGlow();
		}
		private void SetEasy()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Easy;
//			DifficultyGlow();
		}
		private void SetMedium()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Medium;
//			DifficultyGlow();
		}
		private void SetHard()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Hard;
//			DifficultyGlow();
		}
		private void SetBrutal()
		{
			FightManager.SavedGameStatus.Difficulty = AIDifficulty.Brutal;
//			DifficultyGlow();
		}

//		private void DifficultyGlow()
//		{
//			var difficulty = FightManager.SavedGameStatus.Difficulty;
//
//			simpleGlow.gameObject.SetActive(difficulty == AIDifficulty.Simple);
//			easyGlow.gameObject.SetActive(difficulty == AIDifficulty.Easy);
//			mediumGlow.gameObject.SetActive(difficulty == AIDifficulty.Medium);
//			hardGlow.gameObject.SetActive(difficulty == AIDifficulty.Hard);
//			brutalGlow.gameObject.SetActive(difficulty == AIDifficulty.Brutal);
//		}


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
			fightManager.SetSFXVolume(value);
		}

		private void MusicVolumeChanged(float value)
		{
			fightManager.SetMusicVolume(value);
		}

//		private void SpeedUp()
//		{
//			fightManager.SpeedUp();
//			UpdateFPS();
//		}
//
//		private void SlowDown()
//		{
//			fightManager.SlowDown();
//			UpdateFPS();
//		}

//		private void QuitClicked()
//		{
//			quitClicks++;
//
//			if (quitClicks == 2)
//			{
//				Application.Quit();
//
//				// does not quit in unity, so reset
//				if (quitCountdown != null)
//					StopCoroutine(quitCountdown);
//				
//				quitClicks = 0;
//				quitText.text = "EXIT GAME";
//			}
//			else
//			{
//				// timeout for second click to quit
//				quitCountdown = StartCoroutine(QuitCountdown());		
//			}
//		}
//
//		private IEnumerator QuitCountdown()
//		{
//			int countdown = quitTimeout;
//
//			while (countdown > 0)
//			{
//				quitText.text = "CONFIRM EXIT ... " + countdown;
//				yield return new WaitForSeconds(1.0f);
//				countdown--;
//			}
//
//			// timed out - reset
//			quitClicks = 0;
//			quitText.text = "QUIT GAME";
//		}

		private void SetVolumes()
		{
			sfxSlider.value = FightManager.SFXVolume;
			musicSlider.value = FightManager.MusicVolume;
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
			SetFightCaptionsLabel(isOn);
		}

		private void HudToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowHud = isOn;
			SetHudLabel(isOn);

			fightManager.GameUIVisible(isOn);
		}

		private void SetFightCaptionsLabel(bool isOn)
		{
			var label = stateFeedbackToggle.transform.Find("Label").GetComponent<Text>();
			label.text = isOn ? FightManager.Translate("captionsOn", true) : FightManager.Translate("captionsOff", true);

			stateFeedbackToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void SetHudLabel(bool isOn)
		{
			var label = hudToggle.transform.Find("Label").GetComponent<Text>();
			label.text = isOn ? FightManager.Translate("hudOn", true) : FightManager.Translate("hudOff", true);

			hudToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void HintsToggled(bool isOn)
		{
			FightManager.SavedGameStatus.ShowInfoBubbles = isOn;
			SetHintsLabel(isOn);
		}

		private void SetHintsLabel(bool isOn)
		{
			var label = hintsToggle.transform.Find("Label").GetComponent<Text>();
			label.text = isOn ? FightManager.Translate("hintsOn", true) : FightManager.Translate("hintsOff", true);

			hintsToggle.GetComponent<Image>().color = isOn ? OnColour : OffColour;
		}

		private void SetTrainingNarrativeLabel(bool isOn)
		{
			var label = trainingNarrativeToggle.transform.Find("Label").GetComponent<Text>();
			label.text = FightManager.Translate("narrative") + " " + (isOn ? FightManager.Translate("on") : FightManager.Translate("off"));
		}


//		private void SetFBUserName(string userName)
//		{
//			if (! string.IsNullOrEmpty(userName))
//				FBUserName.text = userName;
//			else
//				FBUserName.text = "";
//		}
//
//		private void SetFBProfilePic(Sprite profilePic)
//		{
//			if (profilePic != null)
//			{
//				FBProfilePic.sprite = profilePic;
//				FBProfilePic.gameObject.SetActive(true);
//			}
//			else
//				FBProfilePic.gameObject.SetActive(false);
//		}


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
