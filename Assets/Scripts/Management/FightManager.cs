﻿
using System;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using Prototype.NetworkLobby;


namespace FightingLegends
{
	public class FightManager : MenuCanvas
	{
//		[HideInInspector]
		public static FightMode CombatMode = FightMode.Arcade;
		public DojoUI dojoUI;											// dojo training 'hud'	

		public static SavedStatus SavedGameStatus = new SavedStatus();		// version, kudos, coins, settings, fight status etc.

		// fighter prefabs
		public GameObject HoiLunP1Prefab;
		public GameObject HoiLunP2Prefab;
		public GameObject HoiLunP3Prefab;
		public GameObject NinjaP1Prefab;
		public GameObject NinjaP2Prefab;
		public GameObject NinjaP3Prefab;
		public GameObject DanjumaP1Prefab;
		public GameObject DanjumaP2Prefab;
		public GameObject DanjumaP3Prefab;
		public GameObject LeoniP1Prefab;
		public GameObject LeoniP2Prefab;
		public GameObject LeoniP3Prefab;
		public GameObject NatalyaP1Prefab;
		public GameObject NatalyaP2Prefab;
		public GameObject NatalyaP3Prefab;
		public GameObject ShiroP1Prefab;
		public GameObject ShiroP2Prefab;
		public GameObject ShiroP3Prefab;
		public GameObject AlazneP1Prefab;
		public GameObject AlazneP2Prefab;
		public GameObject AlazneP3Prefab;
		public GameObject JacksonP1Prefab;
		public GameObject JacksonP2Prefab;
		public GameObject JacksonP3Prefab;
		public GameObject ShiyangP1Prefab;
		public GameObject ShiyangP2Prefab;
		public GameObject ShiyangP3Prefab;
		public GameObject SkeletronP1Prefab;
		public GameObject SkeletronP2Prefab;
		public GameObject SkeletronP3Prefab;

		private Queue<string> fighterNames;		// FIFO
		private Queue<string> fighterColours;	// FIFO
//		private Queue<string> AINames;			// FIFO
//		private Queue<string> AIColours;		// FIFO
//		private Queue<string> TrainingAINames;	// FIFO
//		private Queue<string> TrainingAIColours;// FIFO
		private Queue<string> BossNames;		// FIFO
		private Queue<string> BossColours;		// FIFO

		private string TrainingFighterName = "Leoni";
//		private string TrainingFighterColour = "P1";
		private string TrainingAIName = "Ninja";		// only Ninja has tutorial punch

		private float textFeedbackTime = 2.5f;			// seconds text feedback stays visible
		private float stateFeedbackTime = 2.0f;			// seconds state feedback stays visible
		private float powerUpFeedbackTime = 1.0f;		// seconds state feedback stays visible

		private float newRoundTime = 1.0f;				// pauses between feedback FX
		private float roundNumberOffset = 230.0f;		// x offset for round number FX

		private IEnumerator newFightCoroutine;			// so it can be interrupted
		private bool animatingNewFightEntry = false;					// start of fight / training suspended

		public static float SFXVolume
		{
			get { return SavedGameStatus.SFXVolume; }
			set
			{
				if (value == SavedGameStatus.SFXVolume)
					return;

				SavedGameStatus.SFXVolume = value;		

				if (OnSFXVolumeChanged != null)
					OnSFXVolumeChanged(SavedGameStatus.SFXVolume);
			}
		}

		public static float MusicVolume
		{
			get { return SavedGameStatus.MusicVolume; }
			set
			{
				if (value == SavedGameStatus.MusicVolume)
					return;

				SavedGameStatus.MusicVolume = value;

				if (OnMusicVolumeChanged != null)
					OnMusicVolumeChanged(SavedGameStatus.MusicVolume);
			}
		}

		public AudioClip KOSound;
		public AudioClip SuccessSound;
		public AudioClip RoundSound;
		public AudioClip ReadyToFightSound;
		public AudioClip FightSound;
		public AudioClip OKSound;
		public AudioClip BlingSound;
		public AudioClip CoinSound;
		public AudioClip WrongSound;
		public AudioClip ThemeSound;

		public AudioClip OneSound;
		public AudioClip TwoSound;
		public AudioClip ThreeSound;
		public AudioClip FourSound;
		public AudioClip FiveSound;
		public AudioClip SixSound;
		public AudioClip SevenSound;
		public AudioClip EightSound;
		public AudioClip NineSound;
		public AudioClip ZeroSound;

		public AudioClip ErrorSound;
		public AudioClip PowerUpSound;

		public AudioClip BackSound;
		public AudioClip ForwardSound;

		public AudioClip TrainingPromptSound;

		// fighters currently in play
		[HideInInspector]
		public Fighter Player1;	 
		[HideInInspector]
		public Fighter Player2;

		// multiplayer
		public static bool IsNetworkFight = false; // true;				// fighter animation and gesture input handled by NetworkFighter
		private const int networkArcadeFightCountdown = 3;		// before starting new network fight
		private const float networkArcadeFightPause = 0.5f;		// before starting countdown
		private const string countdownLayer = "Curtain";		// so curtain camera picks it up

		public bool HasPlayer1 { get { return Player1 != null; } }
		public bool HasPlayer2 { get { return Player2 != null; } }

		public bool TrainingInProgress { get { return HasPlayer1 && Player1.InTraining; } }

		public bool EitherFighterExpiredState { get { return (HasPlayer1 && Player1.ExpiredState) || (HasPlayer2 && Player2.ExpiredState); } }
		public bool EitherFighterExpiredHealth { get { return (HasPlayer1 && Player1.ExpiredHealth) || (HasPlayer2 && Player2.ExpiredHealth); } }
		public bool EitherFighterExpired { get { return EitherFighterExpiredState || EitherFighterExpiredHealth; } }

		public bool BothFightersIdle { get { return (HasPlayer1 && Player1.IsIdle) && (HasPlayer2 && Player2.IsIdle); } }

		private bool readyToFight;
		public bool ReadyToFight
		{ 
			get { return readyToFight; }
			set
			{
				bool changed = (readyToFight != value);

				if (changed)
					readyToFight = value;

//				Debug.Log("ReadyToFight = " + readyToFight);

//				if (readyToFight && ReadyToFightSound != null)
//					AudioSource.PlayClipAtPoint(ReadyToFightSound, Vector3.zero, SFXVolume);

				if (OnReadyToFight != null)
					OnReadyToFight(readyToFight, changed, CombatMode);
			}
		}
			
		public static bool FightPaused { get; private set; }

		public const float xOffset = 750; 					// when instantiated in default fight position
		public const float yOffset = 525;
		public const float zOffset = 300;

		private const float onTopZOffset = -80.0f;			// z-offset to move 'on top of' opponent when higher priority

		private const float xWaitingOffset = xOffset * 2;	// when instantiated in the wings

		public float FightingDistance { get { return xOffset * 2; } }	// default distance between fighters

		private float DefaultAnimationSpeed = 1.0f;				// not FPS!
		public float AnimationSpeed { get; private set; }		// effective speed (can be adjusted)
		private float adjustedAnimationSpeed;					// speed only adjusted on animation frames

		private const float speedAdjustmentFactor = 0.5f;		// factor by which animation speed is adjusted +/-
		private const float minSpeedFactor = 0.0625f;			// about 1fps (0.94)
		private const float maxSpeedFactor = 4.0f;				// 60fps

//		private const float DefaultAnimationFPS = 15.0f;		// 1/15 sec = 0.0666667  Time.fixedDeltaTime
		private const float DefaultAnimationFPS = 60.0f;		// 1/60 sec = 0.0166667  Time.fixedDeltaTime
//		private const float TurboAnimationFPS = 24.0f;			// 1/15 sec = 0.0416667  Time.fixedDeltaTime
		public float AnimationFPS { get; private set; }			// effective FPS after animation speed adjustments
		public float AnimationFrameInterval { get { return (1.0f / AnimationFPS); } }  // time between animation frames
		public int AnimationFrameCount { get; private set; }	// incremented according to AnimationFPS
//		private const int FxTargetFPS = 60;						// for FX

		private int GameTickCount = 0;					// incremented every FixedUpdate (60fps) - used to determine fighter frames (15fps)
		private const int FighterAnimationFrameInterval = 4;	// 60 / 4 = 15
		public bool IsFighterAnimationFrame { get { return (GameTickCount % FighterAnimationFrameInterval == 0); }}

		private StatusUI statusUI;
		private bool statusUIVisible = false;					// by default
		private GameUI gameUI;						
		private bool gameUIVisible = true;						// by default	

		private NetworkUI networkUI;			
		private TrainingUI trainingUI;			

		private Vector3 expiryCameraPosition;					// camera 'snapshot' position at expiry

		public static Sprite ThemeHeader;
		public static Sprite ThemeFooter;

		private static int SurvivalPowerUpRound = 5;			// AI powerups not used until this many rounds won
		private static int SurvivalDifficultyRound = 5;			// AI difficulty increases each increment of rounds won

		#region menu canvases

		private List<MenuType> menuStack;						// index 0 is treated as top of stack, etc

		[HideInInspector]
		public MenuType CurrentMenuCanvas = MenuType.None;		// not necessarily in menuStack (eg. pause settings)
		 
		public MatchStats matchStats;							// set in inspector
		[HideInInspector]
		public MenuType MatchStatsChoice = MenuType.None;		// tapped
		public bool MatchStatsRestartMatch = false;				// after arcade mode loss (insert coin)
		private const float endMatchReturnTime = 0.3f;			// to match winner reveal animation time

		public PauseSettings pauseSettings;						// set in inspector
		[HideInInspector]
		public MenuType PauseSettingsChoice = MenuType.None;

		public WorldMap worldMap;								// set in inspector
		[HideInInspector]
		public string SelectedLocation = "";					// from world map (obviously!)
		[HideInInspector]
		public string SelectedAIName = "";						// selected in world map (canvas)
//		[HideInInspector]
//		public string SelectedAIColour = "";
		[HideInInspector]
		public MenuType WorldMapChoice = MenuType.None;
		[HideInInspector]
		public Vector3 WorldMapPosition = Vector3.zero;

		// names to match scenery names to lookup prefabs etc
		public const string hawaii = "Hawaii Beach";			// training
		public const string china = "China High Street";
		public const string tokyo = "Tokyo Car Park";
		public const string ghetto = "Ghetto Street";
		public const string cuba = "Cuba Gas Station";
		public const string nigeria = "Nigerian Fight Club";
		public const string soviet = "Soviet Air Museum";
		public const string hongKong = "Hong Kong High School";
		public const string dojo = "Dojo";
		public const string spaceStation = "Space Station";

		public const int NumberOfEarthLocations = 8;	// to enable space station
		private bool worldTourCompleted = false;		// arcade mode, after winning match at last location (space station)

		public bool PlayerCreatedChallenges = false;

		public FighterSelect arcadeFighterSelect;		// set in inspector - arcade mode
		public SurvivalSelect survivalFighterSelect;	// set in inspector - survival mode
		[HideInInspector]
		public MenuType FighterSelectChoice = MenuType.None;

		public TeamSelect teamSelect;					// set in inspector
		[HideInInspector]
		public MenuType TeamSelectChoice = MenuType.None;

		public ModeSelect modeSelect;						// set in inspector
		[HideInInspector]
		public MenuType ModeSelectChoice = MenuType.None;

		public Store storeManager;
		[HideInInspector]
		public MenuType StoreChoice = MenuType.None;			// from store (dojo)

		public FacebookManager FBManager;						// set in inspector
		[HideInInspector]
		public MenuType FacebookChoice = MenuType.None;			// menu selection in FB menu (not currently used)

		public LeaderboardManager LeaderboardManager;				// set in inspector
		[HideInInspector]
		public MenuType LeaderboardsChoice = MenuType.None;		// menu selection in leaderboards menu (not currently used)

//		public AdManager adManager;
//		[HideInInspector]
//		public MenuType AdChoice = MenuType.None;					// from adManager

		public Options options;

		[HideInInspector]
		public MenuOverlay SelectedMenuOverlay = MenuOverlay.None;		// for DirectToOverlay

		#endregion 		// menu canvases

			
		private FeedbackUI feedbackUI;	
		// reference to coroutine (IEnumerator)
		private IEnumerator Player1ComboFeedback;		// so coroutine can be interrupted and restarted
		private IEnumerator Player2ComboFeedback;		// so coroutine can be interrupted and restarted
		private IEnumerator Player1GaugeFeedback;		// so coroutine can be interrupted and restarted
		private IEnumerator Player2GaugeFeedback;		// so coroutine can be interrupted and restarted
		private IEnumerator Player1StateFeedback;		// so coroutine can be interrupted and restarted
		private IEnumerator Player2StateFeedback;		// so coroutine can be interrupted and restarted

		public int MatchBestOf;
		public int EndMatchWins		// wins required to end match
		{
			get
			{
				if ((HasPlayer1 && Player1.ProfileData.FighterClass == FighterClass.Boss) ||
						(HasPlayer2 && Player2.ProfileData.FighterClass == FighterClass.Boss))
					return 1;

				return (MatchBestOf / 2) + 1;
			}
		}

		public int RoundNumber { get; private set; }
		public int MatchCount { get; private set; }

		private const int survivalPowerUpRound = 5;		// AI assigned random power-ups after this round

		public string FrozenStateUI { get; private set; }
		public bool FightFrozen { get; private set; }		// both fighters together
		private int fightFreezeFramesRemaining;
		public bool countdownFreeze = true;			// if fightFreezeFramesRemaining

//		private const int levelUpFreezeFrames = 10;
		private bool levelUpFrozen = false;			// true while frozen for level-up
		private IEnumerator levelUpBlackOut = null;

		private bool powerUpFrozen = false;			// true while frozen for power-up
		private IEnumerator powerUpWhiteOut = null;
		public bool PowerUpFeedbackActive { get; private set; } 		// true while powerup feedback playing
//		public float switchPositionTime = 0.5f;		// time to lerp scale switch

		private const float pulseTextTime = 0.1f;	// default value for PulseText
		private const float pulseTextScale = 1.25f;	// default value for PulseText

		public AudioClip dashInAudio;
		public float dashInTime;
		public float recycleTime;

		// challenge mode

		public ChallengeCategory ChosenCategory { get; private set; }	// set by TeamSelect
		private Queue<FighterCard> challengeTeam;			// removed as defeated
		private Queue<FighterCard> challengeAITeam;			// removed as defeated

		private TeamChallenge challengeInProgress = null;	
		public int ChallengePot { get; private set; }
		public const float ChallengeFee = 0.0f; // 10.0f;

		private List<ChallengeRoundResult> ChallengeRoundResults = new List<ChallengeRoundResult>();
		private AIDifficulty arcadeAIDifficulty;		// current setting saved at start of challenge mode, to restore at end of match

		// scenery

		private SceneryManager sceneryManager;

		// camera

		private CameraController cameraController;
		public Vector3 CameraSnapshot { get { return cameraController.SnapshotPosition; } }
		private Curtain curtain;

		private HitFlash hitFlash;

		public static AreYouSure areYouSure;
		public static InsertPlayCoin insertCoinToPlay;
		public static PurchaseCoins purchaseCoins;
		public static UserRegistration userRegistration;
		public static ChallengeUpload challengeUpload;
		public static ChallengeResult challengeResult;
		public static FighterUnlock fighterUnlock;

		// gesture listener
		private GestureListener gestureListener;			// for spawning gesture sparks for (feedbackFX)

//		public bool ShowTrainingNarrative = false;			// show narrative panel during training
//		public bool ShowStateFeedback = true;				// show state feedback + fireworks

		public delegate void UserProfileDelegate(string userId, UserProfile profile);
		public static UserProfileDelegate OnUserProfileChanged;

		public delegate void SavedStatusLoadedDelegate(SavedStatus status);
		public static SavedStatusLoadedDelegate OnLoadSavedStatus;

		public delegate void MenuDelegate(MenuType newMenu, bool canGoBack, bool canPause, bool coinsVisible, bool kudosVisible);
		public static MenuDelegate OnMenuChanged;

		public delegate void BackClickedDelegate(MenuType menu);
		public static BackClickedDelegate OnBackClicked;

		public delegate void MusicVolumeDelegate(float volume);
		public static MusicVolumeDelegate OnMusicVolumeChanged;

		public delegate void SFXVolumeDelegate(float volume);
		public static SFXVolumeDelegate OnSFXVolumeChanged;

		public delegate void PowerUpInventoryDelegate(PowerUp powerUp, int quantity);
		public static PowerUpInventoryDelegate OnPowerUpInventoryChanged;

		public delegate void FreezeFightDelegate(bool frozen);
		public static FreezeFightDelegate OnFightFrozen;

		public delegate void PauseFightDelegate(bool paused);
		public static PauseFightDelegate OnFightPaused;

		public delegate void MoveCuedOkDelegate(bool ok, Vector3 position);
		public static MoveCuedOkDelegate OnMoveCuedFeedback;

		public delegate void FeedbackStateEndDelegate(AnimationState endingState);
		public static FeedbackStateEndDelegate OnFeedbackStateEnd;

		public delegate void ThemeChangedDelegate(UITheme theme, Sprite header, Sprite footer);
		public static ThemeChangedDelegate OnThemeChanged;

		public delegate void ReadyToFightDelegate(bool ReadyToFight, bool changed, FightMode fightMode);
		public static ReadyToFightDelegate OnReadyToFight;

		public delegate void NetworkReadyToFightDelegate(bool readyToFight);
		public static NetworkReadyToFightDelegate OnNetworkReadyToFight;

//		public delegate void FighterChangedDelegate(Fighter fighter, bool isPlayer1);
//		public static FighterChangedDelegate OnFighterChanged;

		public delegate void NewFightDelegate(FightMode fightMode);
		public static NewFightDelegate OnNewFight;

		public delegate void KnockOutDelegate(bool isPlayer1);
		public static KnockOutDelegate OnKnockOut;

		public delegate void NextRoundDelegate(int roundNumber);
		public static NextRoundDelegate OnNextRound;

		public delegate void QuitFightDelegate();
		public static QuitFightDelegate OnQuitFight;

		public delegate void AIBlockDelegate();
		public static AIBlockDelegate OnAIBlock;

		public delegate void HideInfoBubbleDelegate();
		public static HideInfoBubbleDelegate OnInfoBubbleRead;

		public delegate void GameResetDelegate();
		public static GameResetDelegate OnGameReset;

		public int InitialCoins;

		public const float DefaultMusicVolume = 0.4f;
		public const float DefaultSFXVolume = 0.6f;
	
		public float HitDamageFactor;			// increase for more damage globally


		public static int Coins
		{
			get { return SavedGameStatus.Coins; }
			set
			{
//				Debug.Log("Coins: old " + SavedGameStatus.Coins + " new " + value);
				if (value == SavedGameStatus.Coins)
					return;

				SavedGameStatus.Coins = value;

				if (SavedGameStatus.Coins < 0)
					SavedGameStatus.Coins = 0;

				if (OnCoinsChanged != null)
					OnCoinsChanged(SavedGameStatus.Coins);
			}
		}
			
		public static float Kudos
		{
			get { return SavedGameStatus.Kudos; }
			private set
			{
				if (value == SavedGameStatus.Kudos)
					return;

				SavedGameStatus.Kudos = value;

				if (OnKudosChanged != null)
					OnKudosChanged(SavedGameStatus.Kudos);
			}
		}

		public static void IncreaseKudos(float increase, bool applyDifficulty)
		{
			if (CombatMode == FightMode.Dojo)
				increase *= KudosDojoFactor;
			else if (CombatMode == FightMode.Training)
				increase *= KudosTrainingFactor;

			if (applyDifficulty)
				increase *= DifficultyKudosFactor;

			Kudos += increase;
		}

		private static float DifficultyKudosFactor
		{
			get
			{
				if ((CombatMode == FightMode.Arcade && !IsNetworkFight && !SavedGameStatus.NinjaSchoolFight) || CombatMode == FightMode.Challenge)
				{
					switch (SavedGameStatus.Difficulty)
					{
						case AIDifficulty.Simple:
							return SimpleKudosFactor;

						case AIDifficulty.Easy:
							return EasyKudosFactor;

						case AIDifficulty.Medium:
							return MediumKudosFactor;

						case AIDifficulty.Hard:
							return HardKudosFactor;

						case AIDifficulty.Brutal:
							return BrutalKudosFactor;
					}
				}

				return 1.0f;
			}
		}

		public static string SelectedFighterName
		{
			get { return SavedGameStatus.SelectedFighterName; }
			set
			{
				if (value == SavedGameStatus.SelectedFighterName)
					return;

				SavedGameStatus.SelectedFighterName = value;
			}
		}

		public static string SelectedFighterColour
		{
			get { return SavedGameStatus.SelectedFighterColour; }
			set
			{
				if (value == SavedGameStatus.SelectedFighterColour)
					return;

				SavedGameStatus.SelectedFighterColour = value;
			}
		}

		private string SelectedFighter2Name = "";		// multiplayer only
		private string SelectedFighter2Colour = "";		// multiplayer only

		private static UserProfile userLoginProfile;
		public static UserProfile UserLoginProfile
		{
			get
			{
				if (userLoginProfile != null)
					return userLoginProfile;

				if (SavedGameStatus.UserId != "")
					FirebaseManager.GetUserProfile(SavedGameStatus.UserId);		// callback below

				return null;
			}
			set
			{
				userLoginProfile = value;

				if (OnUserProfileChanged != null)
					OnUserProfileChanged(userLoginProfile.UserID, userLoginProfile);
			}
		}

		// callback from Firebase
		private void OnGetUserProfile(string userId, UserProfile profile, bool success)
		{
			if (success && profile != null && userId == SavedGameStatus.UserId)	
			{
				UserLoginProfile = profile;			// broadcasts
			}
		}
				
		public delegate void CoinsDelegate(int coins);
		public static CoinsDelegate OnCoinsChanged;

		public delegate void KudosDelegate(float kudos);
		public static KudosDelegate OnKudosChanged;


		#region fighter selection

		public bool PreviewMode { get; private set; }			// for instantiating / destroying fighters only

		#endregion 	// fighter selection

		#region XP

		// relative XP points
		public const int LightStrikeXP = 1;
		public const int MediumStrikeXP = 2;
		public const int HeavyStrikeXP = 5;
		public const int SpecialXP = 5;
		public const int SpecialExtraXP = 5;
		public const int CounterAttackXP = 5;
		public const int CounterTriggerXP = 10;
		public const int BlockXP = 1;
		public const int ShoveXP = 1;
		public const int VengeanceXP = 10;
		public const int Combo20XP = 20;
		public const int Combo30XP = 30;
		public const int Combo40XP = 40;
		public const int Combo50XP = 50;

		public const float XPFactor = 1.0f;			// to scale down relative XP
//		public const float blockedXPFactor = 0.25f;	// to scale down XP if strike blocked

		#endregion 	// XP


		#region kudos

		public const int KudosTrainingComplete = 1000;			// for completing basic training
		public const int KudosStartGame = 100;					// for starting the app

		public const float KudosBlockedFactor = 0.5f;			// less kudos if a hit is blocked (applied to damage)
		public const float KudosReceivedFactor = 0.25f;			// less kudos if receiving damage (applied to damage)

		public const int KudosShove = 5;						// for executing a shove (no damage involved)

		public const int KudosResetLevel = 100000;				// multiplied by level (also 'curve'/level difficulty factor?)

		public const int KudosKnockOut = 1000;					// any combat mode
		public const int KudosWinMatch = 2000;					// for winning a match
		public const float KudosLoserFactor = 0.1f;				// much less kudos if knocked out / lost match

		public const int KudosCombo20 = 200;
		public const int KudosCombo30 = 300;
		public const int KudosCombo40 = 400;
		public const int KudosCombo50 = 500;

		public const float KudosTrainingFactor = 0.5f;			// less kudos if training
		public const float KudosDojoFactor = 0.25f;				// less kudos if practicing in dojo

		private const float SimpleKudosFactor = 0.25f;			// AI difficulty
		private const float EasyKudosFactor = 0.5f;	
		private const float MediumKudosFactor = 1.0f;	
		private const float HardKudosFactor = 2.0f;
		private const float BrutalKudosFactor = 4.0f;

		#endregion 	// kudos

		#region themes

		// header and footer sprites (theme)
		public Sprite waterHeader;
		public Sprite airHeader;
		public Sprite fireHeader;
		public Sprite earthHeader;
		public Sprite waterFooter;
		public Sprite airFooter;
		public Sprite fireFooter;
		public Sprite earthFooter;

		#endregion 		// themes


		// 'constructor'
		private void Awake()
		{
//			#if UNITY_IPHONE
//				Handheld.SetActivityIndicatorStyle(iOSActivityIndicatorStyle.Gray);
//			#elif UNITY_ANDROID
//				Handheld.SetActivityIndicatorStyle(AndroidActivityIndicatorStyle.Small);
//			#endif

			var statusUIObject = GameObject.Find("StatusUI");
			if (statusUIObject != null)
			{
				statusUI = statusUIObject.GetComponent<StatusUI>();
				statusUI.gameObject.SetActive(statusUIVisible);
			}

			var gameUIObject = GameObject.Find("GameUI");
			if (gameUIObject != null)
			{
				gameUI = gameUIObject.GetComponent<GameUI>();
				gameUI.gameObject.SetActive(gameUIVisible);
			}

			var networkUIObject = GameObject.Find("NetworkUI");
			if (networkUIObject != null)
				networkUI = networkUIObject.GetComponent<NetworkUI>();

			var trainingUIObject = GameObject.Find("TrainingUI");
			if (trainingUIObject != null)
				trainingUI = trainingUIObject.GetComponent<TrainingUI>();

			var feedbackUIObject = GameObject.Find("FeedbackUI");
			if (feedbackUIObject != null)
			{
				feedbackUI = feedbackUIObject.GetComponent<FeedbackUI>();
				feedbackUI.feedbackFX.OnEndState += FeedbackStateEnd;			// listening for feedback state ends
			}

			var gestureListenerObject = GameObject.Find("GestureListener");
			if (gestureListenerObject != null)
				gestureListener = gestureListenerObject.GetComponent<GestureListener>();

			var sceneryManagerObject = GameObject.Find("SceneryManager");
			if (sceneryManagerObject != null)
				sceneryManager = sceneryManagerObject.GetComponent<SceneryManager>();

			var curtainObject = GameObject.Find("Curtain");
			if (curtainObject != null)
				curtain = curtainObject.GetComponent<Curtain>();

			var hitFlashObject = GameObject.Find("HitFlash");
			if (hitFlashObject != null)
				hitFlash = hitFlashObject.GetComponent<HitFlash>();

			var areYouSureObject = GameObject.Find("AreYouSure");
			if (areYouSureObject != null)
				areYouSure = areYouSureObject.GetComponent<AreYouSure>();

			var insertCoinToPlayObject = GameObject.Find("InsertCoinToPlay");
			if (insertCoinToPlayObject != null)
				insertCoinToPlay = insertCoinToPlayObject.GetComponent<InsertPlayCoin>();

			var purchaseCoinsObject = GameObject.Find("PurchaseCoins");
			if (purchaseCoinsObject != null)
				purchaseCoins = purchaseCoinsObject.GetComponent<PurchaseCoins>();

			var userRegistrationObject = GameObject.Find("UserRegistration");
			if (userRegistrationObject != null)
				userRegistration = userRegistrationObject.GetComponent<UserRegistration>();

			var challengeUploadObject = GameObject.Find("ChallengeUpload");
			if (challengeUploadObject != null)
				challengeUpload = challengeUploadObject.GetComponent<ChallengeUpload>();

			var challengeResultObject = GameObject.Find("ChallengeResult");
			if (challengeResultObject != null)
				challengeResult = challengeResultObject.GetComponent<ChallengeResult>();
		
			var fighterUnlockObject = GameObject.Find("FighterUnlock");
			if (fighterUnlockObject != null)
				fighterUnlock = fighterUnlockObject.GetComponent<FighterUnlock>();
			
			cameraController = Camera.main.GetComponent<CameraController>();

			HideAllMenus();

			fighterNames = new Queue<string>();
			fighterColours = new Queue<string>();
//			AINames = new Queue<string>();
			BossNames = new Queue<string>();
			BossColours = new Queue<string>();
		}

		private void HideAllMenus()
		{
			HideDojoUI();

			if (worldMap != null)
				worldMap.Hide();

			if (arcadeFighterSelect != null)
				arcadeFighterSelect.Hide();

			if (survivalFighterSelect != null)
				survivalFighterSelect.Hide();

			if (modeSelect != null)
				modeSelect.Hide();

			if (teamSelect != null)
				teamSelect.Hide();

			if (storeManager != null)
				storeManager.Hide();

//			if (adManager != null)
//				adManager.Hide();
		}


		// initialization
		private void Start()
		{
			QualitySettings.vSyncCount = 0;				// stop syncing FPS with monitor's refresh rate
//			Application.targetFrameRate = FxTargetFPS;	// for FX / feedback etc

//			IsMobileDevice = DeviceDetector.IsMobile;

			// initialise to default animation speed
			SetDefaultAnimationSpeed();
//			AnimationSpeed = adjustedAnimationSpeed = DefaultAnimationSpeed;
//			AnimationFPS = DefaultAnimationFPS;

			Time.fixedDeltaTime = AnimationFrameInterval;		// just to make sure it's set correctly

			// create lists of fighter names and colours
			RegisterFighterNames();
//			RegisterAINames();
			RegisterBossNames();

			StartGame();
		}

		private void OnDestroy()
		{
			SaveGameStatus();

			DestroyFighter(Player1);		// also saves profile
			DestroyFighter(Player2);
		}

		private void StopTime()
		{
			Time.timeScale = 0;
			Time.fixedDeltaTime = 0;
		}

		private void RestartTime()
		{
			Time.timeScale = 1;
			Time.fixedDeltaTime = AnimationFrameInterval;
		}



//		public void OnApplicationFocus(bool hasFocus)
//		{
//			PauseAll(!hasFocus);
//		}

		// OnApplicationPause(false) is called for a fresh launch and when resumed from background
		public void OnApplicationPause(bool paused)
		{
			PauseAll(paused);
		}

		private void PauseAll(bool paused)
		{
			CancelFX();

			if (paused)
				StopTime();
			else
				RestartTime();

			if (FightPaused == paused)
				return;
			
			PauseFight(paused);

			// save status and Player1 profile if going to background
			if (paused)
			{
				SaveGameStatus();

				if (HasPlayer1)
					Player1.SaveProfile();
			}
		}
			
		// called every Time.fixedDeltaTime seconds
		// 0.0666667 = 1/15 sec
		// 0.0166667 = 1/60 sec
		private void FixedUpdate()
		{
			GameTickCount++;		// always - needed for fighter preview idling in all modes

			if (IsNetworkFight && CombatMode == FightMode.Arcade)		// handled by NetworkFighter
				return;

			UpdateAnimation();
		}
			
		// driven by NetworkFightManager if vs mode
		public void UpdateAnimation()
		{
			if (FightPaused)
				return;

//			UpdateAnimationSpeed();		// in case it was adjusted
			AnimationFrameCount++; 

			if (!IsNetworkFight && !IsFighterAnimationFrame)
				return;
			
			if (FightFrozen)
			{
				if (countdownFreeze)
					FightFreezeCountdown();
			}
				
			if (HasPlayer1)
				Player1.UpdateAnimation();
			if (HasPlayer2)
				Player2.UpdateAnimation();
		}


		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
			{
				ToggleStatusUI();
				ToggleGameUI();
			}
				
			if ((Input.touchCount >= 1 && Input.GetTouch(0).phase == TouchPhase.Began) || Input.GetMouseButtonDown(0))	// left button
			{
				if (TrainingUI.InfoBubbleShowing)
				{
					if (OnInfoBubbleRead != null)
						OnInfoBubbleRead();				// hides bubble and marks message as read
				}
			}
		}

	
		public static void CheckForChallengeResult()
		{
			if (SavedGameStatus.UserId != "")
				FirebaseManager.GetUserProfile(SavedGameStatus.UserId);		// callback below checks for result and coins to collect
		}


		#region touch event handlers

		private void OnEnable()
		{
			AreYouSure.OnCancelConfirm += OnConfirmNo;			// don't quit fight
			FirebaseManager.OnGetUserProfile += OnGetUserProfile;		

//			FBManager.OnLoginFail += FBLoginFail;
//			FBManager.OnLoginSuccess += FBLoginSuccess;

		}

		private void OnDisable()
		{
			if (feedbackUI != null)					// subscribed in Awake()
			{
				feedbackUI.feedbackFX.OnEndState -= FeedbackStateEnd;
			}
				
			AreYouSure.OnCancelConfirm -= OnConfirmNo;
			FirebaseManager.OnGetUserProfile -= OnGetUserProfile;	

//			FBManager.OnLoginFail -= FBLoginFail;
//			FBManager.OnLoginSuccess -= FBLoginSuccess;
		}

//		private void StartListeningForInput()
//		{
//			GestureListener.OnFingerTouch += FingerTouch;			// info bubble read

//			GestureListener.OnSwipeUp += SwipeUp;			// cycle fighters / scenery
//			GestureListener.OnSwipeLeft += SwipeLeft;		// pan scenery (if game paused)
//			GestureListener.OnSwipeRight += SwipeRight;		// pan scenery (if game paused)

			// change animation speed
//			GestureListener.OnSwipeUpDown += SwipeUpDown;
//			GestureListener.OnSwipeDownUp += SwipeDownUp;

//			GestureListener.OnThreeFingerTap += ThreeFingerTap;		// HUD visibility
//			GestureListener.OnFourFingerTap += FourFingerTap;		// toggle pause

//			GestureListener.OnSwipeRightLeft += SwipeRightLeft;		// toggle turbo
//		}

//		private void StopListeningForInput()
//		{
//			GestureListener.OnFingerTouch -= FingerTouch;			// info bubble read

//			GestureListener.OnSwipeUp -= SwipeUp;				// cycle fighters
//			GestureListener.OnSwipeLeft -= SwipeLeft;			// pan scenery (if game paused)
//			GestureListener.OnSwipeRight -= SwipeRight;			// pan scenery (if game paused)

			// change animation speed
//			GestureListener.OnSwipeUpDown -= SwipeUpDown;
//			GestureListener.OnSwipeDownUp -= SwipeDownUp;

//			GestureListener.OnThreeFingerTap -= ThreeFingerTap;	// HUD visibility
//			GestureListener.OnFourFingerTap -= FourFingerTap;	// toggle pause

//			GestureListener.OnSwipeRightLeft -= SwipeRightLeft; // toggle turbo
//		}

		#endregion 		// touch event handlers

		public void FailedInputBubble(FailedInput failedInput)
		{
			if (trainingUI != null)
				trainingUI.FailedInputBubble(failedInput);
		}

		// no info bubble while dojo shadow is attacking (playing back)
		public bool BlockInfoBubble { get { return HasPlayer2 && Player2.IsDojoShadow && !Player2.IsIdle && !Player2.IsStunned; }} 


		public static bool WasInfoBubbleMessageRead(InfoBubbleMessage message)
		{
//			Debug.Log("WasInfoBubbleMessageRead: " + message + ", read = " + ((SavedGameStatus.InfoMessagesRead & message) != 0));
			return (SavedGameStatus.InfoMessagesRead & message) != 0;
		}

		public static void SetInfoBubbleMessageRead(InfoBubbleMessage message)
		{
//			Debug.Log("SetInfoBubbleMessageRead: " + message);
			SavedGameStatus.InfoMessagesRead |= message;
		}

		public static void ClearInfoBubbleMessageRead(InfoBubbleMessage message)
		{
//			Debug.Log("ClearInfoBubbleMessageRead: " + message);
			SavedGameStatus.InfoMessagesRead &= ~message;
		}

		public static void InfoMessageRead(InfoBubbleMessage message, bool read = true)
		{
			if (read)
				SetInfoBubbleMessageRead(message);
			else
				ClearInfoBubbleMessageRead(message);
		}


		public void NetworkMessage(string message)
		{
			if (networkUI != null)
				networkUI.NetworkMessage(message);		// disabled if null or empty
		}

		public bool SetTheme(UITheme theme)
		{
			var themeChanged = SavedGameStatus.Theme != theme;	

			SavedGameStatus.Theme = theme;

			switch (theme)
			{
				case UITheme.Water:
				default:
					ThemeHeader = waterHeader;
					ThemeFooter = waterFooter;
					break;

				case UITheme.Air:
					ThemeHeader = airHeader;
					ThemeFooter = airFooter;
					break;

				case UITheme.Fire:
					ThemeHeader = fireHeader;
					ThemeFooter = fireFooter;
					break;

				case UITheme.Earth:
					ThemeHeader = earthHeader;
					ThemeFooter = earthFooter;
					break;
			}

			if (themeChanged && OnThemeChanged != null)
			{
				if (ThemeSound != null)
					AudioSource.PlayClipAtPoint(ThemeSound, Vector3.zero, SFXVolume);
				
				OnThemeChanged(theme, ThemeHeader, ThemeFooter);
			}

			return themeChanged;
		}

		// freeze both fighters using local counter (as opposed to each Fighter's freeze counter)
		// freeze countdown if freezeFrames > 0
		public void FreezeFight(int freezeFrames = 0)
		{
//			Debug.Log("FreezeFight freezeFrames = " + freezeFrames);

			if (freezeFrames > 0)
			{
				if (freezeFrames > fightFreezeFramesRemaining)
					fightFreezeFramesRemaining = freezeFrames;
				
				countdownFreeze = true;
			}
			else
			{
				countdownFreeze = false;		// stay frozen until UnfreezeFight called
			}

			// stop animation (for freezeFrames or until end of KO feedback)
			if (HasPlayer1)
				Player1.Freeze();	
			if (HasPlayer2)
				Player2.Freeze();

			FightFrozen = true;

			if (OnFightFrozen != null)
				OnFightFrozen(true);

//			if (freezeFrames == 0 && OnFightFrozen != null)
//				OnFightFrozen(true);
		}

		public void UnfreezeFight()
		{
//			Debug.Log("UnfreezeFight");

			if (HasPlayer1)
				Player1.Unfreeze();	
			if (HasPlayer2)
				Player2.Unfreeze();

			FightFrozen = false;
			FrozenStateUI = "";
			fightFreezeFramesRemaining = 0;
			countdownFreeze = true;

			if (OnFightFrozen != null)
				OnFightFrozen(false);
		}
			

		private void FightFreezeCountdown()
		{
			if (! countdownFreeze)
				return;
			
			if (FightPaused)
				return;

			if (EitherFighterExpiredHealth)		// unfrozen at end of KO feedback
				return;

			if (fightFreezeFramesRemaining > 0)
			{
//				Debug.Log("FightFreezeCountdown " + fightFreezeFramesRemaining + " frames remaining" + ", levelUpFrozen = " + levelUpFrozen);
				FrozenStateUI = "[ Fight FROZEN... " + fightFreezeFramesRemaining + " ]";
				fightFreezeFramesRemaining--;
			}
			else
			{
				UnfreezeFight();

				// if freeze was instigated by a hit, receiver recoils now, after the freeze effect
				Player1.RecoilFromAttack();		// return to default fighting distance if attacked
				Player2.RecoilFromAttack();

				if (levelUpFrozen)
					levelUpFrozen = false;

				if (powerUpFrozen)
					powerUpFrozen = false;
			}
		}


		#region power-up inventory

		// returns quantity in inventory
		public int IncreaseInventory(PowerUp powerUp, int quantity)
		{
			var inventoryItem = GetInventoryPowerUp(powerUp);

			if (inventoryItem != null)
			{
				inventoryItem.Quantity += quantity;
				inventoryItem.TimeUpdated = DateTime.Now;
			}
			else 		// create new inventory powerup
			{
				inventoryItem = new InventoryPowerUp();
				inventoryItem.PowerUp = powerUp;
				inventoryItem.Quantity = quantity;
				inventoryItem.TimeCreated = DateTime.Now;
				inventoryItem.TimeUpdated = DateTime.Now;

				SavedGameStatus.PowerUpInventory.Add(inventoryItem);
			}

			if (OnPowerUpInventoryChanged != null)
				OnPowerUpInventoryChanged(powerUp, inventoryItem.Quantity);
			
			return inventoryItem.Quantity;
		}

		// returns quantity remaining
		public int ReduceInventory(PowerUp powerUp, int quantity)
		{
			var inventoryItem = GetInventoryPowerUp(powerUp);

			if (inventoryItem != null)
			{
				inventoryItem.Quantity -= quantity;
				inventoryItem.TimeUpdated = DateTime.Now;

				if (inventoryItem.Quantity <= 0)
				{
					inventoryItem.Quantity = 0;
					SavedGameStatus.PowerUpInventory.Remove(inventoryItem);
				}

				if (OnPowerUpInventoryChanged != null)
					OnPowerUpInventoryChanged(powerUp, inventoryItem.Quantity);
				
				return inventoryItem.Quantity;
			}

			// not in inventory
			return 0;
		}

		public int InventoryQuantity(PowerUp powerUp)
		{
			var inventoryItem = GetInventoryPowerUp(powerUp);
			if (inventoryItem != null)
				return inventoryItem.Quantity;
			
			return 0;
		}

		public InventoryPowerUp GetInventoryPowerUp(PowerUp powerUp)
		{
			foreach (var item in SavedGameStatus.PowerUpInventory)
			{
				if (item.PowerUp == powerUp)
					return item;
			}
			return null;
		}

		#endregion 		// power-up inventory


		public void SetSFXVolume(float volume) 		// 0-1
		{
			SFXVolume = volume;

			if (OnSFXVolumeChanged != null)
				OnSFXVolumeChanged(SFXVolume);
		}

		public void SetMusicVolume(float volume)		// 0-1
		{
			MusicVolume = volume;

			if (OnMusicVolumeChanged != null)
				OnMusicVolumeChanged(MusicVolume);
		}

		public void Success(float yOffset = 0.0f, string layer = null)
		{
			TriggerFeedbackFX(FeedbackFXType.Success, 0.0f, yOffset, layer); 

			BlingAudio();

			if (SuccessSound != null)
				AudioSource.PlayClipAtPoint(SuccessSound, Vector3.zero, SFXVolume);
		}

		public void BlingAudio()
		{
			if (BlingSound != null)
				AudioSource.PlayClipAtPoint(BlingSound, Vector3.zero, SFXVolume);
		}

		public void BackAudio()
		{
			if (BackSound != null)
				AudioSource.PlayClipAtPoint(BackSound, Vector3.zero, SFXVolume);
		}

		public void ForwardAudio()
		{
			if (ForwardSound != null)
				AudioSource.PlayClipAtPoint(ForwardSound, Vector3.zero, SFXVolume);
		}

		public void CoinAudio()
		{
			if (CoinSound != null)
				AudioSource.PlayClipAtPoint(CoinSound, Vector3.zero, SFXVolume);
		}


		public void Wrong(float xOffset)
		{
			TriggerFeedbackFX(FeedbackFXType.Wrong, xOffset, 0.0f); 
			WrongAudio();
		}

		public void WrongAudio()
		{
			if (WrongSound != null)
				AudioSource.PlayClipAtPoint(WrongSound, Vector3.zero, SFXVolume);
		}

		public void PowerUpAudio()
		{
			if (PowerUpSound != null)
				AudioSource.PlayClipAtPoint(PowerUpSound, Vector3.zero, SFXVolume);
		}


		public void SnapshotCameraPosition(bool toZero = false)
		{
			expiryCameraPosition = toZero ? Vector3.zero : CameraSnapshot;
		}

		private void FeedbackStateEnd(AnimationState endingState)
		{
			CancelFeedbackFX();

			// relay event to any subscribers (eg. store left/right swipe loop)
			if (OnFeedbackStateEnd != null)
				OnFeedbackStateEnd(endingState);
		}

		private void CancelFX()
		{
			CancelFeedbackFX();
			CancelRoundFX();

			if (HasPlayer1)
				Player1.CancelFX();
			
			if (HasPlayer2)
				Player2.CancelFX();
		}

		#region fighter recycling 

		public Fighter InstantiateFighter(GameObject prefab)
		{
			var fighter = Instantiate(prefab, Vector3.zero, Quaternion.identity) as GameObject;

			// fighter is a child of FightManager
			fighter.transform.parent = transform;

			return fighter.GetComponent<Fighter>();
		}

		// fill queues of names and colours
		private void RegisterFighterNames()
		{
			fighterNames.Enqueue("Leoni");	
			fighterNames.Enqueue("Danjuma");	
			fighterNames.Enqueue("Hoi Lun");	
			fighterNames.Enqueue("Shiro");	
			fighterNames.Enqueue("Natalya");	
			fighterNames.Enqueue("Alazne");	
			fighterNames.Enqueue("Jackson");
			fighterNames.Enqueue("Shiyang");	
			fighterNames.Enqueue("Ninja");	

			fighterColours.Enqueue("P1");
			fighterColours.Enqueue("P2");
			fighterColours.Enqueue("P3");
		}

		private void RegisterBossNames()
		{
			BossNames.Enqueue("Skeletron");

			BossColours.Enqueue("P1");
			BossColours.Enqueue("P2");
			BossColours.Enqueue("P3");
		}

		private string RandomFighterName
		{
			get { return fighterNames.ToArray()[ UnityEngine.Random.Range(0, fighterNames.Count - 1) ]; }
		}

		private string RandomFighterColour
		{
			get { return fighterColours.ToArray()[ UnityEngine.Random.Range(0, fighterColours.Count - 1) ]; }
		}

		private void RandomFighter(out string name, out string colour)
		{
			var player1Name = HasPlayer1 ? Player1.FighterName : "";
			var player2Name = HasPlayer2 ? Player2.FighterName : "";
			var player1Colour = HasPlayer1 ? Player1.ColourScheme : "";
			var player2Colour = HasPlayer2 ? Player2.ColourScheme : "";

			string randomName = "";
			string randomColour = "";

			// avoid exactly the same name and colour as either current fighter
			do
			{
				randomName = RandomFighterName;
				randomColour = RandomFighterColour;
			}
			while ((randomName == player1Name && randomColour == player1Colour) || (randomName == player2Name && randomColour == player2Colour));

			name = randomName;
			colour = randomColour;
		}


		private float OnTopZOffset(bool player1)
		{
			var onTopOffset = zOffset;

			if (player1)
			{
				if (HasPlayer1 && Player1.IsOnTop)
					onTopOffset += onTopZOffset;
			}
			else
			{
				if (HasPlayer2 && Player2.IsOnTop)
					onTopOffset += onTopZOffset;
			}

			return onTopOffset;
		}

		public Vector3 GetFighterPosition(bool player1, bool waiting, bool defaultPosition)
		{
			if (! defaultPosition)
			{
				// player 1 on left, player 2 on right
				if (player1 && HasPlayer1)
					return waiting ? new Vector3(Player1.transform.position.x - xOffset, yOffset, OnTopZOffset(player1)) : Player1.transform.position;
				else if (!player1 && HasPlayer2)
					return waiting ? new Vector3(Player2.transform.position.x + xOffset, yOffset, OnTopZOffset(player1)) : Player2.transform.position;
			}

			// default fighting position
			return (player1) ? new Vector3(waiting ? -xWaitingOffset : -xOffset, yOffset, OnTopZOffset(true) ) :
				new Vector3(waiting ? xWaitingOffset : xOffset, yOffset, OnTopZOffset(false));
		}

		public Vector3 GetFighterScale(bool player1)
		{
			// player 1 faces right, player 2 faces left
			return player1 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
		}
			
		// position relative to opponent for default fighting distance
		public Vector3 GetRelativeDefaultPosition(bool player1)
		{
			if (player1)
				return new Vector3(Player2.transform.position.x - (xOffset * 2.0f), yOffset, OnTopZOffset(true));
			else
				return new Vector3(Player1.transform.position.x + (xOffset * 2.0f), yOffset, OnTopZOffset(false));
		}

		// position relative to opponent for strike contact
		public Vector3 GetRelativeStrikePosition(bool player1)
		{
			var defaultPosition = GetRelativeDefaultPosition(player1);
			var strikeOffset = player1 ? Player1.ProfileData.AttackDistance : -Player2.ProfileData.AttackDistance;

			return new Vector3(defaultPosition.x + strikeOffset, defaultPosition.y, defaultPosition.z);
		}


		private Fighter CreateNextFighter(bool player1, bool underAI, bool inTraining, bool waiting, bool toDefaultPosition, bool firstFighters, bool pause, bool random = false)
		{
			string nextFighterName;
			string nextFighterColour;

			if (IsNetworkFight && CombatMode == FightMode.Arcade)
				underAI = false;

			if (underAI && random)		// survival mode - next AI is selected randomly
			{
				RandomFighter(out nextFighterName, out nextFighterColour);		// ensures a different name/colour to both current fighters
			}
			else if (underAI && SelectedAIName != "") // && SelectedAIColour != "")
			{
//				Debug.Log("EnterNextFighter AI: nextFighter " + SelectedAIName + " " + SelectedAIName);

				nextFighterName = SelectedAIName;
				nextFighterColour = AIFighterColour; // SelectedAIColour;

				// reset
				SelectedAIName = "";
			}
			else
			{
				if (!underAI && firstFighters)
				{
					nextFighterName = SelectedFighterName;
					nextFighterColour = SelectedFighterColour;
				}
				else
				{
					// always cycle non-AI colours...
					nextFighterColour = underAI ? AIFighterColour : fighterColours.Dequeue();
					nextFighterName = firstFighters ? "" : (player1 ? Player1.FighterName : Player2.FighterName);

					if (underAI)
					{
						if (firstFighters || nextFighterColour == "P1")		// cycle to next fighter name
						{
//							if (!SavedGameStatus.CompletedBasicTraining)
							if (CombatMode == FightMode.Training)
							{
								nextFighterName = TrainingAIName;
							}
							else
							{
								nextFighterName = fighterNames.Dequeue();
								fighterNames.Enqueue(nextFighterName);
							}
						}
					}
					else
					{
						fighterColours.Enqueue(nextFighterColour);		// back to end of queue (to keep looping)

						if (firstFighters || nextFighterColour == "P1")		// cycle to next fighter name
						{
							nextFighterName = fighterNames.Dequeue();
							fighterNames.Enqueue(nextFighterName);
						}
					}
				}
			}

//			Debug.Log("EnterNextFighter: nextFighter " + nextFighterName + " " + nextFighterColour);

   			var fighter = CreateFighter(nextFighterName, nextFighterColour, underAI, ! SavedGameStatus.CompletedBasicTraining);
			fighter.InTraining = inTraining && !underAI;

			// position player 1 on left, player 2 on right
			fighter.transform.position = GetFighterPosition(player1, waiting, toDefaultPosition);

			// scale player 1 to face right, player 2 faces left
			fighter.transform.localScale = GetFighterScale(player1); 

			fighter.ResetHealth();			// set initial health

			// put new fighter into Player1 / Player2 slot
			if (player1)
				Player1 = fighter;
			else
				Player2 = fighter;

			if (statusUI != null)
				statusUI.SetFighters();

			if (gameUI != null)
				gameUI.SetFighters(gameUIVisible);

			if (trainingUI != null && player1 && fighter.InTraining)
				trainingUI.SetTrainer();

//			Debug.Log("EnterNextFighter: " + fighter.FullName);
			return fighter;
		}


		public static string NextFighterColour(string currentColour)
		{
			if (currentColour == "P1")
				return "P2";
			else if (currentColour == "P2")
				return "P3";
			else if (currentColour == "P3")
				return "P1";

			return currentColour;
		}

		private string AIFighterColour
		{
			get
			{
				// ensure AI fighter is not same name and colour as player1
				if (HasPlayer1 && Player1.FighterName == SelectedAIName)
				{
					return NextFighterColour(Player1.ColourScheme);
				}

				return "P1";
			}
		}

		public void DestroyFighter(Fighter fighter)
		{
			// AI fighter stops watching and non-AI stops being watched
			if (fighter != null)
			{
//				Debug.Log("DestroyFighter: " + fighter.FullName + ", PreviewMode = " + PreviewMode);

				if (! PreviewMode && ! fighter.UnderAI)
					fighter.SaveProfile();
				
				Destroy(fighter.gameObject);
			}
		}

		// destroy fighter after getting the next in the queue
		// if defaultPosition is false, the new fighter is positioned as per the 'outgoing' fighter
		public Fighter RecycleFighter(Fighter fighter, bool toDefaultPosition, bool random = false)
		{
//			Debug.Log("RecycleFighter: " + fighter.FullName);

			if (fighter.UnderAI)
				fighter.StopWatching();
			else
				fighter.Opponent.StopWatching();
			
			var newFighter = CreateNextFighter(fighter.IsPlayer1, fighter.UnderAI, ! SavedGameStatus.CompletedBasicTraining && !fighter.UnderAI, false, toDefaultPosition, false, true, random);
			DestroyFighter(fighter);

			return newFighter;
		}


		public Fighter CreateFighter(string name, string colourScheme, bool underAI, bool training, bool listenForInput = true)
		{
			Fighter newFighter = null;

			if (IsNetworkFight && CombatMode == FightMode.Arcade)
				underAI = false;

			if (training)
				name = underAI ? TrainingAIName : TrainingFighterName;

			switch (name)
			{
				case "Danjuma":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(DanjumaP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(DanjumaP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(DanjumaP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Hoi Lun":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(HoiLunP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(HoiLunP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(HoiLunP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Ninja":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(NinjaP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(NinjaP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(NinjaP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Shiro":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(ShiroP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(ShiroP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(ShiroP3Prefab);
							break;

						default:
							break;
					}
					break;

				case "Natalya":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(NatalyaP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(NatalyaP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(NatalyaP3Prefab);
							break;

						default:
							break;
					}
					break;

				case "Leoni":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(LeoniP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(LeoniP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(LeoniP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Alazne":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(AlazneP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(AlazneP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(AlazneP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Jackson":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(JacksonP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(JacksonP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(JacksonP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Shiyang":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(ShiyangP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(ShiyangP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(ShiyangP3Prefab);
							break;
						default:
							break;
					}
					break;

				case "Skeletron":
					switch (colourScheme)
					{
						case "P1":
							newFighter = InstantiateFighter(SkeletronP1Prefab);
							break;
						case "P2":
							newFighter = InstantiateFighter(SkeletronP2Prefab);
							break;
						case "P3":
							newFighter = InstantiateFighter(SkeletronP3Prefab);
							break;
						default:
							break;
					}
					break;

				default:
					break;
			}

			if (newFighter != null)
			{
				newFighter.UnderAI = underAI;

				if (! underAI)
					newFighter.LoadProfile();		// if it exists

				if (listenForInput)
					newFighter.StartListeningForInput();
			}

//			if (fighter == null)
//				throw new Exception("NewFighter: Unknown fighter '" + name + "'" + ", scheme " + colourScheme);

			return newFighter;
		}
			
		#endregion 	// fighter recycling


		#region fight control

		public void PauseFight(bool pause)
		{
			if (FightPaused == pause)		// no change
				return;

			FightPaused = pause;

			if (OnFightPaused != null)
				OnFightPaused(FightPaused);
		}

		private void ConfirmQuitFight()
		{
			if (! (CurrentMenuCanvas == MenuType.Combat || CurrentMenuCanvas == MenuType.WorldMap))
				return;

			if (!IsNetworkFight)
//				PauseFight(true);
				FreezeFight();

			if (CombatMode == FightMode.Dojo)
				GetConfirmation(Translate("confirmExitDojo"), 0, QuitFight);
			else if (CombatMode == FightMode.Training)
				GetConfirmation(Translate("confirmExitTraining"), 0, QuitFight);
			else
				GetConfirmation(Translate("confirmQuitFight"), 0, QuitFight);
		}

		private void OnConfirmNo()
		{
			if (SavedGameStatus.FightInProgress)
				UnfreezeFight();
		}


		public void ExitFight()
		{
			// quitting a challenge automatically loses the pot to the defender
			if (CombatMode == FightMode.Challenge)
				PayoutChallengePot(Player2);

			IsNetworkFight = false;

			CleanupFighters();
			HideDojoUI();

			SaveGameStatus();
			GameUIVisible(false);

			CancelFX();

			ActivateMenu(MenuType.ModeSelect);
		}

		private void QuitFight()
		{
			if (OnQuitFight != null)
				OnQuitFight();			// stop training / sync network fight quit

			if (! IsNetworkFight)		// synced quit handled by NetworkFighter
				ExitFight();			
		}

		public void CleanupFighters()
		{
//			Debug.Log("CleanupFighters");

			if (TrainingInProgress)
				Player1.StopTraining();
			
			DestroyFighter(Player1);		
			DestroyFighter(Player2);

			Player1 = null;
			Player2 = null;

			CancelFeedbackFX();
			CancelRoundFX();

//			if (OnCleanupFight != null)
//				OnCleanupFight();
		}

		public void StartNetworkArcadeFight(string player1Name, string player1Colour, string player2Name, string player2Colour, string location)
		{
			CombatMode = FightMode.Arcade;

//			Debug.Log("StartNetworkArcadeFight: player1 = " + player1Name + " player2 = " + player2Name + " location = " + location);

			SelectedFighterName = player1Name;
			SelectedFighterColour = player1Colour;
			SelectedFighter2Name = player2Name;
			SelectedFighter2Colour = player2Colour;
			SelectedLocation = location;

			StartCoroutine(CountdownNetworkArcadeFight());
		}
			
		private IEnumerator CountdownNetworkArcadeFight()
		{
			yield return new WaitForSeconds(networkArcadeFightPause);

			for (int count = networkArcadeFightCountdown; count > 0; count--)
			{
				TriggerNumberFX(count, 0, 0, countdownLayer);
				BlingAudio();
//				PlayNumberSound(count);

				yield return new WaitForSeconds(1.0f);
			}

			// assume arcade network fight started from world map
			WorldMapChoice = MenuType.Combat;  // default starts new fight	
			yield return null;
		}

		// took too long for both fighters to be selected
		public void ExitNetworkFighterSelect()
		{
			IsNetworkFight = false;
			ActivateMenu(MenuType.ModeSelect);
		}

		public void Arcade()
		{
			ModeSelectChoice = MenuType.ArcadeFighterSelect;
		}

	
		private IEnumerator StartNewFight(bool curtainDelay)
		{
			if (PreviewMode)
				yield break;
			
//			Debug.Log("StartNewFight: Mode = " + CombatMode + " - " + SelectedLocation);

			CleanupFighters();

			SavedGameStatus.FightInProgress = true;
			SavedGameStatus.FightStartKudos = Kudos;
			SavedGameStatus.FightStartCoins = Coins;

			RoundNumber = 1;
			MatchCount = 1;

			ReadyToFight = false;

			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training)
			{
				if (SelectedFighterName != "" && SelectedFighterColour != "")
				{
					if (Player1 == null)
						Player1 = CreateFighter(SelectedFighterName, SelectedFighterColour, false, !SavedGameStatus.CompletedBasicTraining || CombatMode == FightMode.Training);
					
					Player1.ResetPosition();
					Player1.ResetHealth();
					Player1.Reveal();
					Player1.UnderAI = false;	

					Player1.InTraining = !SavedGameStatus.CompletedBasicTraining || CombatMode == FightMode.Training;
				}
				else
				{
					if (Player1 != null)
						RecycleFighter(Player1, true);
					else
						CreateNextFighter(true, false, !SavedGameStatus.CompletedBasicTraining, false, true, true, true);
				}

				// player 2 always AI - unless multiplayer!
				if (Player2 == null)
				{
					if (IsNetworkFight && CombatMode == FightMode.Arcade)
					{
						Player2 = CreateFighter(SelectedFighter2Name, SelectedFighter2Colour, false, false); 

						Player2.ResetPosition(true);
						Player2.ResetHealth();
						Player2.Reveal();
					}
					else
						CreateNextFighter(false, true, false, false, true, true, true);
				}
				else if (SavedGameStatus.CompletedBasicTraining) 			// otherwise keep same fighter (restarted training)
					RecycleFighter(Player2, true);

//				Debug.Log("StartNewFight: " + Player2.FullName + " StartWatching " + Player1.FullName);
				Player2.StartWatching();				// AI watches non-AI

				if (CombatMode == FightMode.Training && trainingUI != null && Player1.InTraining)
					trainingUI.SetTrainer();

				if (IsNetworkFight && CombatMode == FightMode.Arcade)
				{
					if (statusUI != null)
						statusUI.SetFighters();

					if (gameUI != null)
						gameUI.SetFighters(gameUIVisible);
				}
			}
			else if (CombatMode == FightMode.Dojo)
			{
				if (SelectedFighterName != "" && SelectedFighterColour != "")
				{
					Player1 = CreateFighter(SelectedFighterName, SelectedFighterColour, false, false, true);
					Player1.ResetPosition();
					Player1.ResetHealth();
					Player1.Reveal();

					// player 2 is shadow of player1
					Player2 = CreateFighter(SelectedFighterName, NextFighterColour(SelectedFighterColour), false, false, false);
					Player2.ResetPosition(true);		// player 2 faces left
					Player2.ResetHealth();
					Player2.Reveal();

					if (statusUI != null)
						statusUI.SetFighters();

					if (gameUI != null)
						gameUI.SetFighters(gameUIVisible);
				}
			}
			else 		// survival and challenge modes
			{
				if (curtainDelay)			// wait until curtain is half way 'up' before dashing in!
					yield return new WaitForSeconds(curtain.fadeTime / 2);

				// fighters dash in simultaneously
				StartCoroutine(NextFighterDashIn(true, true));					// player 1
				yield return StartCoroutine(NextFighterDashIn(false, true));	// player 2

				// survival AI opponent starts fight at same level as Player1
				if (CombatMode == FightMode.Survival)
				{
					SetSurvivalAILevel(false);				// AI same as fighter
					SetSurvivalAIDifficulty();				// according to match rounds won
				}

				// save current arcade mode difficulty, to restore at end of challenge match
				if (CombatMode == FightMode.Challenge)
					arcadeAIDifficulty = FightManager.SavedGameStatus.Difficulty;

				Player2.StartWatching();
			}

			gameUI.SetupCombatMode();			// to set fight 'title' and traffic lights

			ShowFighterLevels();		// if survival or challenge mode
			ShowDojoUI();				// only in dojo combat mode or for first round of first fight after ninja school
			ResetMatchScore();

			while (animatingNewFightEntry)
				yield return null;
			
			yield return StartCoroutine(NewRoundFeedback(curtainDelay && (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training || CombatMode == FightMode.Dojo) ? curtain.fadeTime : 0.0f));

			if (CombatMode == FightMode.Training && HasPlayer1)
			{
				Player1.InTraining = true;
				Player1.StartTraining();
			}

			if (OnNewFight != null)
				OnNewFight(CombatMode);
				
			if (IsNetworkFight)
			{
				if (OnNetworkReadyToFight != null)
					OnNetworkReadyToFight(true);
			}
			else
				ReadyToFight = true;		// just to make sure (set by NewRoundFeedback)

			yield return null;
		}
			
		private void ResetMatchScore()
		{
			Player1.ProfileData.SavedData.MatchRoundsWon = 0;
			Player1.ProfileData.SavedData.MatchRoundsLost = 0;
			Player1.ProfileData.SavedData.FightStartLevel = Player1.Level;

			Player2.ProfileData.SavedData.MatchRoundsWon = 0;
			Player2.ProfileData.SavedData.MatchRoundsLost = 0;
			Player2.ProfileData.SavedData.FightStartLevel = Player2.Level;		// not used but just for completeness

			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training) // || CombatMode == FightMode.Dojo)
			{
				if (Player1.OnScoreChanged != null)
					Player1.OnScoreChanged(0);

				if (Player2.OnScoreChanged != null)
					Player2.OnScoreChanged(0);
			}
		}
			
		public void ResetWorldTour()
		{
			Debug.Log("ResetWorldTour");
			ResetCompletedLocations();
			worldTourCompleted = false;				// reset for next arcade world tour
		}
	
		public IEnumerator NextMatch(Fighter winner) 
		{
//			Debug.Log("NextMatch: winner = " + winner.FullName);
			// deliberately no fade to black!

			// track to expiry position and reveal winner simultaneously
			if (CombatMode != FightMode.Training)		// no match stats at end of training - restart match for first fight!
				StartCoroutine(cameraController.TrackTo(expiryCameraPosition, endMatchReturnTime));	// track back to expiry position, same scenery

			if (CombatMode != FightMode.Training && CombatMode != FightMode.Dojo)
			{
				bool fighterUnlocked = UpdateMatchStats(winner);			// announces an unlocked fighter (arcade mode only)

				if (CombatMode == FightMode.Challenge)
				{
					PayoutChallengePot(winner);
					yield return StartCoroutine(ShowMatchStatsCanvas(winner, ChallengeRoundResults));	// loop thru round results (FighterCards)
				}
				else if (CombatMode == FightMode.Arcade && worldTourCompleted) 		
					yield return StartCoroutine(ShowMatchStatsCanvas(winner, null, true));		// winner image + congrats - until user taps
				else if (! fighterUnlocked) 	// includes VS
					yield return StartCoroutine(ShowMatchStatsCanvas(winner, null));		// winner image + fight stats - until user taps
			}

			if (IsNetworkFight)
			{
				yield break;
			}

			if (CombatMode == FightMode.Arcade && winner.UnderAI)			// player lost - MatchStats offers option to insert coin to continue...
			{
				yield break;
			}

//			Debug.Log("NextMatch: winner = " + winner.FullName + " CombatMode = " + CombatMode);

			// player won
			if (CombatMode == FightMode.Arcade && ! worldTourCompleted) 	// world map to choose next location 	
			{
				Player1.ResetPosition();		// stay as same fighter
				Player1.Reset();

				yield return StartCoroutine(SelectWorldLocation());			// location + AI fighter - yields until location selected
			}
			else 		// arcade world tour complete, completed training or survival/challenge mode
			{
				if (CombatMode == FightMode.Training)			// follow training with an arcade fight vs ninja
				{
					CombatMode = FightMode.Arcade;
					SavedGameStatus.NinjaSchoolFight = true;	// shows dojo-style move UI
					SaveGameStatus();							// completed training!

					gameUI.SetupCombatMode();					// title etc.
					yield return StartCoroutine(RestartMatch());					
				}
//				else if (CombatMode == FightMode.Challenge)
//				{
////					// challenge defender is awarded coins
//					if (winner.UnderAI)
//						PayoutChallenge(!winner.UnderAI);
//					
//					yield return StartCoroutine(ShowModeSelectCanvas());
//				}
				else
					yield return StartCoroutine(ShowModeSelectCanvas());
			}
		}

		// called by fighter if network fight
		public void NetworkKnockOut(bool isPlayer1)
		{
			if (OnKnockOut != null)			// NetworkFighter
				OnKnockOut(isPlayer1);
		}

		// called by NetworkFighter rpc to sync KO
		public void KnockOutFighter(bool isPlayer1)
		{
//			if (isPlayer1)
//				Player1.KnockOut()
//			else
//				Player2.KnockOut()
		}
			

		private void PayoutChallengePot(Fighter winner)
		{
			if (winner == null)
				return;
			
			if (CombatMode != FightMode.Challenge)
				return;
			
			if (!winner.UnderAI)
			{
				if (ChallengePot > 0)
				{
					// challenger won - payout pot coins immediately
					Coins += ChallengePot;

					FightManager.SavedGameStatus.TotalChallengeWinnings += ChallengePot;
					FirebaseManager.PostLeaderboardScore(Leaderboard.ChallengeWinnings, FightManager.SavedGameStatus.TotalChallengeWinnings);
				}
			}

			// challenge defender is awarded coins for later collection or notified of challenge defeat
			FirebaseManager.CompleteChallenge(challengeInProgress, winner.UnderAI, ChallengePot);		// callback on successful update

			challengeInProgress = null;
			ChallengePot = 0;
		}

		// new arcade match, same fighters and location
		private IEnumerator RestartMatch()
		{
			if (CombatMode != FightMode.Arcade)
				yield break;

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}
				
			SavedGameStatus.FightInProgress = true;
			SavedGameStatus.FightStartKudos = Kudos;
			SavedGameStatus.FightStartCoins = Coins;

			RoundNumber = 1;
			MatchCount = 1;

			ReadyToFight = false;

			yield return StartCoroutine(cameraController.TrackHome(false, true));		// track back to zero, same scenery

			Player1.Reset();
			Player1.ResetPosition();
			Player2.Reset();
			Player2.ResetPosition();

			ResetMatchScore();
			ShowDojoUI();				// only in dojo combat mode or for first round of first fight after ninja school

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			StartCoroutine(NewRoundFeedback());
		
			PauseFight(false);
			UnfreezeFight();
			yield return null;
		}
			
		public void CompleteCurrentLocation()
		{
			if (CombatMode != FightMode.Arcade)
				return;

			if (Player1.ProfileData.SavedData.CompletedLocations.Contains(SelectedLocation))		// already completed this location
				return;

			Player1.ProfileData.SavedData.CompletedLocations.Add(SelectedLocation);
//			Debug.Log("CompleteCurrentLocation: SelectedLocation = " + SelectedLocation + ", CompletedEarthLocations = " + CompletedEarthLocations);

			if (CompletedEarthLocations)
			{
				if (SelectedLocation == spaceStation)		// completed world tour
				{
					Player1.ProfileData.SavedData.WorldTourCompletions++;
					FightManager.SavedGameStatus.WorldTourCompletions++;		// all fighters
					FirebaseManager.PostLeaderboardScore(Leaderboard.ArcadeWorldTours, FightManager.SavedGameStatus.WorldTourCompletions);

					ResetCompletedLocations();
					worldTourCompleted = true;					// to prevent returning to world map

//					Debug.Log("CompleteCurrentLocation: WorldTourCompletions = " + Player1.ProfileData.SavedData.WorldTourCompletions);
				}
			}

			Player1.SaveProfile();
		}

		public bool CompletedEarthLocations
		{
			get { return Player1 != null && Player1.ProfileData.SavedData.CompletedLocations.Count >= NumberOfEarthLocations; }
		}

		private void ResetCompletedLocations()
		{
			if (CombatMode != FightMode.Arcade)
				return;
			
			Player1.ProfileData.SavedData.CompletedLocations.Clear();
			Player1.SaveProfile();
		}


		private bool UpdateMatchStats(Fighter winner)
		{
			if (winner == null)
				return false;
			
			var loser = winner.Opponent;
			bool fighterUnlocked = false;

			winner.Hide();
			winner.ProfileData.SavedData.MatchesWon++;
			loser.ProfileData.SavedData.MatchesLost++;

			if (CombatMode == FightMode.Arcade && !FightManager.IsNetworkFight && !FightManager.SavedGameStatus.NinjaSchoolFight)
			{
				switch (FightManager.SavedGameStatus.Difficulty)
				{
					case AIDifficulty.Simple:
						if (winner.UnderAI)
						{
							winner.ProfileData.SavedData.SimpleLosses++;
							FightManager.SavedGameStatus.SimpleArcadeLosses++;
						}
						else  // loser is AI - unlock if defeated enough times
						{
//							if (loser.CanUnlockOrder && FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty)
//							{
								loser.ProfileData.SavedData.SimpleWins++;
								FightManager.SavedGameStatus.SimpleArcadeWins++;

//								if (loser.ProfileData.SavedData.SimpleWins >= loser.ProfileData.SavedData.UnlockDefeats)
//								{
//									fighterUnlocked = true;
//									FightManager.UnlockFighter(loser);
//								}
//							}
						}
						break;

					case AIDifficulty.Easy:
						if (winner.UnderAI)
						{
							winner.ProfileData.SavedData.EasyLosses++;
							FightManager.SavedGameStatus.EasyArcadeLosses++;
						}
						else  // loser is AI - unlock if defeated enough times
						{
//							if (loser.CanUnlockOrder && FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty)
//							{
								loser.ProfileData.SavedData.EasyWins++;
								FightManager.SavedGameStatus.EasyArcadeWins++;

//								if (loser.ProfileData.SavedData.EasyWins >= loser.ProfileData.SavedData.UnlockDefeats)
//								{
//									fighterUnlocked = true;
//									FightManager.UnlockFighter(loser);
//								}
//							}
						}
						break;

					case AIDifficulty.Medium:
						if (winner.UnderAI)
						{
							winner.ProfileData.SavedData.MediumLosses++;
							FightManager.SavedGameStatus.MediumArcadeLosses++;
						}
						else  // loser is AI - unlock if defeated enough times
						{
//							if (loser.CanUnlockOrder && FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty)
//							{
								loser.ProfileData.SavedData.MediumWins++;
								FightManager.SavedGameStatus.MediumArcadeWins++;

//								if (loser.ProfileData.SavedData.MediumWins >= loser.ProfileData.SavedData.UnlockDefeats)
//								{
//									fighterUnlocked = true;
//									FightManager.UnlockFighter(loser);
//								}
//							}
						}
						break;

					case AIDifficulty.Hard:
						if (winner.UnderAI)
						{
							winner.ProfileData.SavedData.HardLosses++;
							FightManager.SavedGameStatus.HardArcadeLosses++;
						}
						else  // loser is AI - unlock if defeated enough times
						{
//							if (loser.CanUnlockOrder && FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty)
//							{
								loser.ProfileData.SavedData.HardWins++;
								FightManager.SavedGameStatus.HardArcadeWins++;

//								if (loser.ProfileData.SavedData.HardWins >= loser.ProfileData.SavedData.UnlockDefeats)
//								{
//									fighterUnlocked = true;
//									FightManager.UnlockFighter(loser);
//								}
//							}
						}
						break;

					case AIDifficulty.Brutal:
						if (winner.UnderAI)
						{
							winner.ProfileData.SavedData.BrutalLosses++;
							FightManager.SavedGameStatus.BrutalArcadeLosses++;
						}
						else  // loser is AI - unlock if defeated enough times
						{
//							if (loser.CanUnlockOrder && FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty)
//							{
								loser.ProfileData.SavedData.BrutalWins++;
								FightManager.SavedGameStatus.BrutalArcadeWins++;

//								if (loser.ProfileData.SavedData.BrutalWins >= loser.ProfileData.SavedData.UnlockDefeats)
//								{
//									fighterUnlocked = true;
//									FightManager.UnlockFighter(loser);
//								}
//							}
						}
						break;
				}

				// check if we won and if the AI loser has been defeated enough times to unlock
				bool canUnlockLoser = !winner.UnderAI && loser.CanUnlockOrder &&
										FightManager.SavedGameStatus.Difficulty >= loser.ProfileData.SavedData.UnlockDifficulty &&
										loser.CanUnlockDefeats;

				if (canUnlockLoser)
				{
					fighterUnlocked = true;
					UnlockFighter(loser);		// will check for ninja unlock on hide
				}
				else
				{
					CheckUnlockNinja();
				}
			}

			if (winner.IsPlayer2)
			{
				SavedGameStatus.MatchesLost++;
				MatchWonKudos(true);
			}
			else
			{
				SavedGameStatus.MatchesWon++;
				MatchWonKudos(false);
			}

			// round scores reset to zero
			winner.ProfileData.SavedData.MatchRoundsWon = 0;
			winner.ProfileData.SavedData.MatchRoundsLost = 0;
			loser.ProfileData.SavedData.MatchRoundsWon = 0;
			loser.ProfileData.SavedData.MatchRoundsLost = 0;

			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training)
			{
				if (winner.OnScoreChanged != null)
					winner.OnScoreChanged(0);
				if (loser.OnScoreChanged != null)
					loser.OnScoreChanged(0);
			}

			ReadyToFight = false;
			RoundNumber = 1;
			MatchCount++;		// not sure what this will be used for

			return fighterUnlocked;
		}


		public void CheckUnlockNinja()
		{
			var ninjaProfile = Profile.GetFighterProfile("Ninja");

			if (!ninjaProfile.IsLocked || !ninjaProfile.CanUnlockOrder)
				return;
			
			var totalDefeats = 0;

			switch (ninjaProfile.UnlockDifficulty)
			{
				case AIDifficulty.Simple:
					totalDefeats = FightManager.SavedGameStatus.SimpleArcadeWins +
									FightManager.SavedGameStatus.EasyArcadeWins +
									FightManager.SavedGameStatus.MediumArcadeWins +
									FightManager.SavedGameStatus.HardArcadeWins +
									FightManager.SavedGameStatus.BrutalArcadeWins;
					break;

				case AIDifficulty.Easy:
					totalDefeats = FightManager.SavedGameStatus.EasyArcadeWins +
									FightManager.SavedGameStatus.MediumArcadeWins +
									FightManager.SavedGameStatus.HardArcadeWins +
									FightManager.SavedGameStatus.BrutalArcadeWins;
					break;

				case AIDifficulty.Medium:
					totalDefeats = FightManager.SavedGameStatus.MediumArcadeWins +
									FightManager.SavedGameStatus.HardArcadeWins +
									FightManager.SavedGameStatus.BrutalArcadeWins;
					break;

				case AIDifficulty.Hard:
					totalDefeats = FightManager.SavedGameStatus.HardArcadeWins +
									FightManager.SavedGameStatus.BrutalArcadeWins;
					break;

				case AIDifficulty.Brutal:
					totalDefeats = FightManager.SavedGameStatus.BrutalArcadeWins;
					break;			
			}

			if (totalDefeats >= ninjaProfile.UnlockDefeats)
			{
				var ninja = CreateFighter("Ninja", "P1", false, false, false);
				UnlockFighter(ninja);
				DestroyFighter(ninja);
			}
		}


		public IEnumerator NextRound(bool resetRound) 
		{
//			CancelFX();
			ReadyToFight = false;

			if (CombatMode == FightMode.Challenge)
			{
				UnfreezeFight();		// TODO: is this necessary / working?
				yield return StartCoroutine(ReplaceLoserFromTeam());
			}
			else if (CombatMode == FightMode.Survival)		
			{
				UnfreezeFight();		// TODO: is this necessary / working?
				yield return StartCoroutine(RandomAIDashIn());
			}
			else 		// arcade, training or dojo mode - same fighters and location
			{
				if (curtain != null)
				{
					curtain.gameObject.SetActive(true);
					yield return StartCoroutine(curtain.FadeToBlack());
				}
			
				if (resetRound)
					RoundNumber = 1;
				else
					RoundNumber++;

				ShowDojoUI();				// only in dojo combat mode or for first round of first fight after ninja school

				yield return StartCoroutine(cameraController.TrackHome(false, true));		// track back to zero, same scenery

				Player1.ResetPosition();
				Player1.Reset();
				Player2.ResetPosition();
				Player2.Reset();

				if (curtain != null)
				{
					yield return StartCoroutine(curtain.CurtainUp());
					curtain.gameObject.SetActive(false);
				}

				if (CombatMode == FightMode.Dojo)
				{
					Player1.UpdateGauge(Fighter.maxGauge, true);
					Player2.UpdateGauge(Fighter.maxGauge, true);
				}
			}

			if (OnNextRound != null)
				OnNextRound(RoundNumber);

			yield return StartCoroutine(NewRoundFeedback());			// ReadyToFight set to true at end of feedback
		}

		public void NetworkNextRound()
		{
			if (FightManager.IsNetworkFight)
				StartCoroutine(NewRoundFeedback());
		}


		private Fighter NextFighterInTeam(bool player1)
		{
			if (CombatMode != FightMode.Challenge)
				return null;

			var existingFighter = player1 ? Player1 : Player2;
			FighterCard nextFighterCard = null;

//			Debug.Log("NextFighterInTeam: player1 = " + player1 + ", challengeTeam.Count = " + challengeTeam.Count + ", challengeAITeam.Count = " + challengeAITeam.Count);

			if (player1)
			{
				if (challengeTeam.Count <= 0)					// assumes we should not have got this far if either team depleted
					return null;

				if (existingFighter != null)					// don't dequeue if first fighter in team
				{
					var loser = challengeTeam.Dequeue();		// remove from head of queue
					var winner = challengeAITeam.Peek();

//					Debug.Log("NextFighterInTeam: P1 lost: winner = " + winner.FighterName + " loser = " + loser.FighterName );
					ChallengeRoundResults.Add(new ChallengeRoundResult(winner, loser, true));			// player1 lost
				}

				nextFighterCard = challengeTeam.Peek();
			}
			else
			{
				if (challengeAITeam.Count <= 0)					// assumes we should not have got this far if either team depleted
					return null;

				if (existingFighter != null)					// don't dequeue if first fighter in team
				{
					var loser = challengeAITeam.Dequeue();					// remove from head of queue
					var winner = challengeTeam.Peek();

//					Debug.Log("NextFighterInTeam: P1 won: winner = " + winner.FighterName + " loser = " + loser.FighterName );
					ChallengeRoundResults.Add(new ChallengeRoundResult(winner, loser, false));			// player1 won
				}

				nextFighterCard = challengeAITeam.Peek();
			}

			if (nextFighterCard == null)		// shouldn't happen!
			{
//				Debug.Log("NextFighterInTeam: null nextFighterCard!!!");
				return null;
			}

			var nextFighter = CreateFighter(nextFighterCard.FighterName, player1 ? nextFighterCard.FighterColour : AIFighterColour, !player1, false);
			nextFighter.transform.position = GetFighterPosition(player1, true, false);
			nextFighter.transform.localScale = player1 ? new Vector3(1, 1, 1) : new Vector3(-1, 1, 1);
//			nextFighter.ResetHealth();			// set initial health

			nextFighter.Level = nextFighterCard.Level;
			nextFighter.XP = nextFighterCard.XP;
			nextFighter.TriggerPowerUp = nextFighterCard.TriggerPowerUp;
			nextFighter.StaticPowerUp = nextFighterCard.StaticPowerUp;

			nextFighter.ProfileData.SavedData.TriggerPowerUpCoolDown = storeManager.GetPowerUpCooldown(nextFighter.TriggerPowerUp);

			nextFighter.ResetHealth();			// set initial health - according to level

			// temporarily set global AI difficulty, otherwise used for arcade mode AI
			if (nextFighter.UnderAI)
				FightManager.SavedGameStatus.Difficulty = nextFighterCard.Difficulty;

			// put next fighter into Player1 / Player2 slot
			if (player1)
				Player1 = nextFighter;
			else
				Player2 = nextFighter;

			if (statusUI != null)
				statusUI.SetFighters();

			if (gameUI != null)
				gameUI.SetFighters(gameUIVisible);	

			if (trainingUI != null && player1 && Player1.InTraining)
				trainingUI.SetTrainer();

			if (existingFighter != null)		// next fighter dashes in - existing fighter is destroyed
				DestroyFighter(existingFighter);

			// update scores to show number in each team
			if (nextFighter.OnScoreChanged != null)
				nextFighter.OnScoreChanged(nextFighter.UnderAI ? challengeAITeam.Count : challengeTeam.Count);

			return nextFighter;
		}

		private IEnumerator NextFighterDashIn(bool player1, bool firstFighter)
		{
			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training || CombatMode == FightMode.Dojo)
				yield break;

			var existingFighter = player1 ? Player1 : Player2;
			Fighter nextFighter = null;

			if (CombatMode == FightMode.Challenge)
			{
				nextFighter = NextFighterInTeam(player1);
			}
			else if (CombatMode == FightMode.Survival)
			{
				// random next fighter
				if (existingFighter != null)
					nextFighter = RecycleFighter(existingFighter, false, true);			// must be AI (else end of match)
				else
					nextFighter = CreateNextFighter(player1, !player1, false, true, false, firstFighter, true, true); 

				if (nextFighter.OnScoreChanged != null)
					nextFighter.OnScoreChanged(0);
			}

			if (nextFighter != null)
			{
				nextFighter.StartingState = State.Dash;

				if (existingFighter == null)
					yield return StartCoroutine(nextFighter.Slide(dashInTime, GetFighterPosition(player1, false, true)));
				else
					yield return StartCoroutine(nextFighter.Slide(dashInTime, GetRelativeDefaultPosition(player1)));

				nextFighter.ReturnToIdle();

//				nextFighter.ProfileData.SavedData.TriggerPowerUpCoolDown =
//									storeManager.GetPowerUpCooldown(nextFighter.ProfileData.SavedData.TriggerPowerUp);

//				if (dashInAudio != null)
//					AudioSource.PlayClipAtPoint(dashInAudio, Vector2.zero, SFXVolume);
			}
				
			yield return null;
		}

		private IEnumerator ReplaceLoserFromTeam()
		{
			if (CombatMode != FightMode.Challenge)
				yield break;
			
			var loser = Player1.ExpiredHealth ? Player1 : Player2;

			yield return StartCoroutine(NextFighterDashIn(loser.IsPlayer1, false));

			Player2.StartWatching();

			if (IsNetworkFight)
			{
				if (OnNetworkReadyToFight != null)
					OnNetworkReadyToFight(true);
			}
			else
				ReadyToFight = true;
			
			yield return null;
		}


		// called for 2nd survival mode opponent onwards
		private IEnumerator RandomAIDashIn()
		{
			if (CombatMode != FightMode.Survival)
				yield break;

			yield return StartCoroutine(NextFighterDashIn(false, false));

			// each new AI fighter increases in level (based on Player1 level)
			SetSurvivalAILevel(true);
			// AI difficulty increases in bands of rounds won
			SetSurvivalAIDifficulty();

			if (RoundNumber == survivalPowerUpRound)
				SetAIRandomPowerUps();

			Player2.StartWatching();

			if (IsNetworkFight)
			{
				if (OnNetworkReadyToFight != null)
					OnNetworkReadyToFight(true);
			}
			else
				ReadyToFight = true;
			
			yield return null;
		}

		private void SetSurvivalAILevel(bool addRoundsWon)
		{
			if (CombatMode != FightMode.Survival)
				return;
			
			Player2.Level = Player1.Level + (addRoundsWon ? Player1.ProfileData.SavedData.MatchRoundsWon : 0);		// not saved
			Player2.ResetHealth();					// according to level
		}

		// survival difficulty increases with match rounds won
		private void SetSurvivalAIDifficulty()
		{
			int difficultyBand = Player1.ProfileData.SavedData.MatchRoundsWon / SurvivalDifficultyRound;

			if (difficultyBand < 0)
				FightManager.SavedGameStatus.Difficulty = AIDifficulty.Simple;
			else if (difficultyBand < 1)
				FightManager.SavedGameStatus.Difficulty = AIDifficulty.Easy;
			else if (difficultyBand < 2)
				FightManager.SavedGameStatus.Difficulty = AIDifficulty.Medium;
			else if (difficultyBand < 3)
				FightManager.SavedGameStatus.Difficulty = AIDifficulty.Hard;
			else
				FightManager.SavedGameStatus.Difficulty = AIDifficulty.Brutal;
		}

		private void SetAIRandomPowerUps()
		{
			// no AI powerups until 5 wins
			if (Player1.ProfileData.SavedData.MatchRoundsWon < SurvivalPowerUpRound)
				return;
			
			Player2.StaticPowerUp = Store.RandomStaticPowerUp;
			Player2.TriggerPowerUp = Store.RandomTriggerPowerUp;

			Player2.ProfileData.SavedData.TriggerPowerUpCoolDown = storeManager.GetPowerUpCooldown(Player2.TriggerPowerUp);

			Debug.Log("SetAIRandomPowerUps: StaticPowerUp = "  + Player2.StaticPowerUp + ", TriggerPowerUp = " + Player2.TriggerPowerUp);
		}
			

		// challenge mode

		public void SetupChallenge(TeamChallenge challenge, List<FighterCard> selectedTeam, int selectedTeamPrizeCoins, List<FighterCard> selectedAITeam)
		{
			ChallengeRoundResults = new List<ChallengeRoundResult>();

			SavedGameStatus.CompletedBasicTraining = true;		// just to make sure
			challengeInProgress = challenge;

			ChallengePot = (challenge.PrizeCoins + selectedTeamPrizeCoins) - (int)((float)ChallengePot * ChallengeFee / 100.0f);

			ChosenCategory = challenge.ChallengeCategory;
			SelectedLocation = challenge.Location;
			gameUI.SetupCombatMode();							// update 'title'

			challengeTeam = new Queue<FighterCard>();
			challengeAITeam = new Queue<FighterCard>();

			// set up queues so first in list is first to fight etc
			for (int i = 0; i < selectedTeam.Count; i++)
			{
//				Debug.Log("SetupChallenge selectedTeam: " + selectedTeam[i].FighterName + ", static " + selectedTeam[i].StaticPowerUp + ", trigger " + selectedTeam[i].TriggerPowerUp);
				challengeTeam.Enqueue(selectedTeam[i].Duplicate());			// ?? new instance needed for challenge results
			}
				
			for (int i = 0; i < selectedAITeam.Count; i++)
			{
//				Debug.Log("SetupChallenge selectedAITeam: " + selectedAITeam[i].FighterName + ", static " + selectedAITeam[i].StaticPowerUp + ", trigger " + selectedAITeam[i].TriggerPowerUp);
				challengeAITeam.Enqueue(selectedAITeam[i].Duplicate());		// ?? new instance needed for challenge results
			}
				
//			Debug.Log("SetupChallenge: challengeTeam " + challengeTeam.Count + ", challengeAITeam " + challengeAITeam.Count);
		}
			
		public bool ChallengeLastInTeam(bool AIWinner)
		{
			if (CombatMode != FightMode.Challenge)
				return false;
			
			return AIWinner ? challengeTeam.Count == 1 : challengeAITeam.Count == 1;
		}

		// record results of final round of challenge match
		public void RecordFinalChallengeResult(bool AIWinner)
		{
			FighterCard winner = (AIWinner) ? challengeAITeam.Peek() : challengeTeam.Peek();
			FighterCard loser = (AIWinner) ? challengeTeam.Peek() : challengeAITeam.Peek();

//			Debug.Log("RecordFinalChallengeResult: winner = " + winner.FighterName + " loser = " + loser.FighterName );
			ChallengeRoundResults.Add(new ChallengeRoundResult(winner, loser, AIWinner));

			// restore arcade mode difficulty at end of challenge match
			if (CombatMode == FightMode.Challenge)
				FightManager.SavedGameStatus.Difficulty = arcadeAIDifficulty;
		}

		#endregion 		// fight control


		#region animation

		public void SetDefaultAnimationSpeed()
		{
			AnimationSpeed = adjustedAnimationSpeed = DefaultAnimationSpeed;
			AnimationFPS = DefaultAnimationFPS;
		}

		private void AdjustAnimationSpeed(float factor)
		{
			adjustedAnimationSpeed = AnimationSpeed * factor;

			if (adjustedAnimationSpeed < minSpeedFactor)
				adjustedAnimationSpeed = minSpeedFactor;
			else if (adjustedAnimationSpeed > maxSpeedFactor)
				adjustedAnimationSpeed = maxSpeedFactor;
		}

		private void UpdateAnimationSpeed()			// called on animation frames
		{
			if (AnimationSpeed == adjustedAnimationSpeed)
				return; 		// no change
			
			AnimationFPS *= (adjustedAnimationSpeed / AnimationSpeed);
			Time.fixedDeltaTime = AnimationFrameInterval;		// based on FPS
			AnimationSpeed = adjustedAnimationSpeed;
		}
			
//		private void OnAnimationSpeedChanged(float value)		// 0 - 1
//		{
//			AdjustAnimationSpeed(value / speedAdjustmentFactor);
//			UpdateAnimationSpeed();								// FPS
//		}

		#endregion		// animation


		#region kudos

		public void DamageKudos(float damage, int priority, bool receivedHit, bool hitBlocked)
		{
			// for kudos, multiply damage by priority to reward harder moves
			// (damage already factored by level)
			float kudosDamage = damage * ((priority > 0) ? priority : 1);

			if (hitBlocked)
				kudosDamage *= KudosBlockedFactor;			// less kudos if a hit is blocked

			if (receivedHit)
				kudosDamage *= KudosReceivedFactor;		// less kudos if receiving damage

			FightManager.IncreaseKudos(kudosDamage, true); 
		}

		// kudos for shoving and also (less) for receiving shove
		public void ShoveKudos(bool receivedShove)
		{
			if (! receivedShove)
				FightManager.IncreaseKudos(KudosShove, true);
			
//			FightManager.IncreaseKudos(KudosShove * (receivedShove ? KudosReceivedFactor : 1));
		}

		public void StartGameKudos()
		{
			FightManager.IncreaseKudos(KudosStartGame, false);
		}

		public void TrainingCompleteKudos()
		{
			FightManager.IncreaseKudos(KudosTrainingComplete, false);
		}

		// kudos for KO and also (less) for being knocked out
		public void KnockOutKudos(bool knockedOut)
		{
			FightManager.IncreaseKudos(KudosKnockOut * (knockedOut ? KudosLoserFactor : 1), true);
		}

		// kudos for winning and also (less) for losing
		public void MatchWonKudos(bool loser)
		{
			FightManager.IncreaseKudos(KudosWinMatch * (loser ? KudosLoserFactor : 1), true);
		}

		// kudos for resetting level to 1
		public void ResetLevelKudos(int level)
		{
			FightManager.IncreaseKudos(KudosResetLevel * level, false);
		}

		#endregion


		#region feedback

		public void ComboFeedback(bool player1, int comboCount)
		{
			if (feedbackUI != null)
			{
				// interrupt previous feedback if still running
				StopComboFeedback(player1);
				ClearComboFeedback(player1);

				if (player1)
				{
					Player1ComboFeedback = feedbackUI.ComboFeedback(true, comboCount, textFeedbackTime);
					StartCoroutine(Player1ComboFeedback);
				}
				else
				{
					Player2ComboFeedback = feedbackUI.ComboFeedback(false, comboCount, textFeedbackTime);
					StartCoroutine(Player2ComboFeedback);
				}
			}
		}

		public void StopComboFeedback(bool player1)
		{
			if (player1)
			{
				if (Player1ComboFeedback != null)
				{
					StopCoroutine(Player1ComboFeedback);
				}
			}
			else
			{
				if (Player2ComboFeedback != null)
				{
					StopCoroutine(Player2ComboFeedback);
				}
			}
		}

		public void ClearComboFeedback(bool player1)
		{
			if (feedbackUI != null)
				feedbackUI.ClearComboFeedback(player1);
		}



		public void GaugeFeedback(bool player1, string feedback)
		{
			if (feedbackUI != null)
			{
				// interrupt previous feedback if still running
				StopGaugeFeedback(player1);
				ClearGaugeFeedback(player1);

				if (player1)
				{
					Player1GaugeFeedback = feedbackUI.GaugeFeedback(true, feedback, textFeedbackTime);
					StartCoroutine(Player1GaugeFeedback);
				}
				else
				{
					Player2GaugeFeedback = feedbackUI.GaugeFeedback(false, feedback, textFeedbackTime);
					StartCoroutine(Player2GaugeFeedback);
				}
			}
		}

		public void StopGaugeFeedback(bool player1)
		{
			if (player1)
			{
				if (Player1GaugeFeedback != null)
				{
					StopCoroutine(Player1GaugeFeedback);
				}
			}
			else
			{
				if (Player2GaugeFeedback != null)
				{
					StopCoroutine(Player2GaugeFeedback);
				}
			}
		}

		public void ClearGaugeFeedback(bool player1)
		{
			if (feedbackUI != null)
				feedbackUI.ClearGaugeFeedback(player1);
		}


		public void StateFeedback(bool player1, string feedback, bool stars, bool silent, string layer = null)
		{
			if (!SavedGameStatus.ShowStateFeedback)
				return;
			
			if (feedbackUI != null)
			{
				// interrupt previous feedback if still running
				StopStateFeedback(player1);
				ClearStateFeedback(player1);

				if (player1)
				{
					Player1StateFeedback = feedbackUI.StateFeedback(true, feedback, stateFeedbackTime, stars, silent, layer);
					StartCoroutine(Player1StateFeedback);
				}
				else
				{
					Player2StateFeedback = feedbackUI.StateFeedback(false, feedback, stateFeedbackTime, stars, silent, layer);
					StartCoroutine(Player2StateFeedback);
				}
			}
		}

		public void StopStateFeedback(bool player1)
		{
			if (player1)
			{
				if (Player1StateFeedback != null)
				{
					StopCoroutine(Player1StateFeedback);
				}
			}
			else
			{
				if (Player2StateFeedback != null)
				{
					StopCoroutine(Player2StateFeedback);
				}
			}
		}

		public void ClearStateFeedback(bool player1)
		{
			if (feedbackUI != null)
			{
				feedbackUI.ClearStateFeedback(player1);
			}
		}


		public void LevelUpFeedback(int level, bool stars, bool silent)
		{
			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training || CombatMode == FightMode.Dojo)
				return;
			
			if (feedbackUI != null)
				StartCoroutine(feedbackUI.LevelUpFeedback(level, stateFeedbackTime, stars, silent));

			levelUpFrozen = true;

			if (hitFlash != null)
			{
				levelUpBlackOut = hitFlash.BlackOut(2.0f);
				StartCoroutine(levelUpBlackOut);
			}
		}

		public void ClearLevelUpFeedback()
		{
			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training || CombatMode == FightMode.Dojo)
				return;
			
			if (levelUpBlackOut != null)
				StopCoroutine(levelUpBlackOut);
			
			if (feedbackUI != null)
				StartCoroutine(feedbackUI.ClearLevelUpFeedback());
		}


		public IEnumerator PowerUpFeedback(PowerUp powerUp, bool stars, bool silent)
		{
			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training)
				yield break;

//			Debug.Log("PowerUpFeedback: " + powerUp);

			powerUpFrozen = true;
			PowerUpFeedbackActive = true;

			if (hitFlash != null)
			{
				powerUpWhiteOut = hitFlash.WhiteOut(2.0f);
				StartCoroutine(powerUpWhiteOut);
			}

			if (feedbackUI != null)
				yield return StartCoroutine(feedbackUI.PowerUpFeedback(powerUp, powerUpFeedbackTime, stars, silent));

			PowerUpFeedbackActive = false;
			yield return null;

//			powerUpFrozen = true;
//
//			if (hitFlash != null)
//			{
//				powerUpWhiteOut = hitFlash.WhiteOut(2.0f);
//				StartCoroutine(powerUpWhiteOut);
//			}
		}

		public void ClearPowerUpFeedback()
		{
			if (CombatMode == FightMode.Arcade || CombatMode == FightMode.Training)
				return;

			if (powerUpWhiteOut != null)
				StopCoroutine(powerUpWhiteOut);

			if (feedbackUI != null)
				feedbackUI.ClearPowerUpFeedback();
		}


		public void ShowFighterLevels()
		{
			if (gameUI != null)
				gameUI.ShowFighterLevels(CombatMode == FightMode.Survival || CombatMode == FightMode.Challenge);
		}

		private void ShowDojoUI()
		{
//			Debug.Log("ShowDojoUI: CombatMode = " + CombatMode + ", NinjaSchoolFight = " + SavedStatus.NinjaSchoolFight);
			if (dojoUI != null)
				dojoUI.gameObject.SetActive(CombatMode == FightMode.Dojo);
//				dojoUI.gameObject.SetActive(CombatMode == FightMode.Dojo || (CombatMode == FightMode.Arcade && SavedGameStatus.NinjaSchoolFight && RoundNumber == 1));
		}

		public void HideDojoUI()
		{
			dojoUI.gameObject.SetActive(false);
		}

		public static IEnumerator GrowText(Text text, float fadeTime, bool grow)
		{
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				if (grow)
				{
					text.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, t);
				}
				else
				{
					text.transform.localScale = Vector3.Lerp(Vector3.one, Vector3.zero, t);
				}

				yield return null;
			}
			yield return null;
		}
			

		public static IEnumerator AnimateShatter(Image image, Sprite shatter)
		{
			var startSprite = image.sprite;
			var startColour = image.color;
			var targetColour = new Color(image.color.r, image.color.g, image.color.b, 0);
			float t = 0;

			image.sprite = shatter;
			image.gameObject.SetActive(true);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / 0.25f); 	

				image.transform.localScale = Vector3.Lerp(Vector3.one, new Vector3(2, 2, 2), t);
				image.color = Color.Lerp(startColour, targetColour, t);
				yield return null;
			}

			image.gameObject.SetActive(false);

			image.sprite = startSprite;
			image.color = startColour;
			image.transform.localScale = Vector3.one;
			yield return null;
		}

		public static IEnumerator FadeText(Text text, float fadeTime, bool fadeOut)
		{
			// save colours so they can be restored at the end of lerps
			var textColour = text.color;
			var shadows = text.GetComponents<Shadow>();
			var shadowColours = new Color[shadows.Length];
			for (int i = 0; i < shadows.Length; i++)
				shadowColours[i] = shadows[i].effectColor;
			
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				if (fadeOut)
				{
					text.color = Color.Lerp(textColour, Color.clear, t);

					// fade shadows
					for (int i = 0; i < shadows.Length; i++)
						shadows[i].effectColor = Color.Lerp(shadowColours[i], Color.clear, t);
				}
				else
				{
					text.color = Color.Lerp(Color.clear, textColour, t);

					// fade shadows
					for (int i = 0; i < shadows.Length; i++)
						shadows[i].effectColor = Color.Lerp(Color.clear, shadowColours[i], t);
				}

				yield return null;
			}

			// restore for next time
			if (fadeOut)
			{
				text.text = "";
				text.color = textColour;
				for (int i = 0; i < shadows.Length; i++)
					shadows[i].effectColor = shadowColours[i];
			}

			yield return null;
		}


		public static IEnumerator FadePanel(Image panel, float fadeTime, bool fadeOut, AudioClip fadeSound, Color background)
		{
			// save colour and scale so they can be restored at the end of lerps
			var panelColour = background; // Color.white; //  panel.color;
			var panelScale = Vector3.one; // panel.rectTransform.localScale;
			var fadedScale = new Vector3(0, 1, 1);	

			panel.rectTransform.SetPivot(PivotPresets.MiddleCentre);

			float t = 0.0f;

			panel.gameObject.SetActive(true);

			if (fadeSound != null)
				AudioSource.PlayClipAtPoint(fadeSound, Vector3.zero, SFXVolume);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / fadeTime); 

				if (fadeOut)
				{
					panel.color = Color.Lerp(panelColour, Color.clear, t);
					panel.rectTransform.localScale = Vector3.Lerp(panelScale, fadedScale, t);
				}
				else
				{
					panel.color = Color.Lerp(Color.clear, panelColour, t);
					panel.rectTransform.localScale = Vector3.Lerp(fadedScale, panelScale, t);
				}

				yield return null;
			}

			// restore for next time
			if (fadeOut)
			{
				panel.gameObject.SetActive(false);
				panel.color = panelColour;
				panel.rectTransform.localScale = panelScale;
			}

			yield return null;
		}


		public static IEnumerator PulseImage(Image image, float pulseTime, float pulseScale, bool pause, AudioClip pulseSound)
		{
			Vector3 growScale = new Vector3(pulseScale, pulseScale, pulseScale);

			// grow
			float t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime); 
				image.transform.localScale = Vector3.Lerp(Vector3.zero, growScale, t);
				yield return null;
			}

			if (pulseSound != null)
				AudioSource.PlayClipAtPoint(pulseSound, Vector3.zero, SFXVolume);

			if (pause)
				yield return new WaitForSeconds(pulseTime);

			// shrink 
			t = 0.0f;

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTime); 
				image.transform.localScale = Vector3.Lerp(growScale, Vector3.one, t);
				yield return null;
			}

			yield return null;
		}

		public static IEnumerator PulseText(Text text, AudioClip sound = null)
		{
			float t = 0;

			// pulse  up
			Vector3 currentScale = text.transform.localScale;
			Vector3 startScale = new Vector3(1, 1, 1);
			Vector3 targetScale = new Vector3(pulseTextScale, pulseTextScale, pulseTextScale);

			while (t < 1.0f)
			{
				t += Time.deltaTime * (Time.timeScale / pulseTextTime);

				text.transform.localScale = Vector3.Lerp(currentScale, targetScale, t);
				yield return null;
			}

			if (sound != null)
				AudioSource.PlayClipAtPoint(sound, Vector3.zero, FightManager.SFXVolume);

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


		public void TrainingPrompt(string trainingPrompt)
		{
			if (trainingPrompt == null || feedbackUI == null)
				return;
	
			feedbackUI.TrainingPrompt.text = trainingPrompt;

			if (string.IsNullOrEmpty(trainingPrompt))
			{
				feedbackUI.TrainingHorizontalFlash.gameObject.SetActive(false);
			}
			else
			{
				feedbackUI.TrainingHorizontalFlash.gameObject.SetActive(true);			// animated
				AudioSource.PlayClipAtPoint(TrainingPromptSound, Vector3.zero, SFXVolume);
			}
		}


		public void PlayNumberSound(int number)
		{
			AudioClip sound = null;

			switch (number)
			{
				case 0:
					sound = ZeroSound;
					break;
				case 1:
					sound = OneSound;
					break;
				case 2:
					sound = TwoSound;
					break;
				case 3:
					sound = ThreeSound;
					break;
				case 4:
					sound = FourSound;
					break;
				case 5:
					sound = FiveSound;
					break;
				case 6:
					sound = SixSound;
					break;
				case 7:
					sound = SevenSound;
					break;
				case 8:
					sound = EightSound;
					break;
				case 9:
					sound = NineSound;
					break;
				default:
					sound = null;
					break;
			}

			if (sound != null)
				AudioSource.PlayClipAtPoint(sound, Vector3.zero, SFXVolume);
		}
			

		public void TriggerFeedbackFX(FeedbackFXType feedback, float xOffset = 0.0f, float yOffset = 0.0f, string layer = null)
		{
			if (feedbackUI != null)
				feedbackUI.TriggerFeedbackFX(feedback, xOffset, yOffset, layer);

			if (CurrentMenuCanvas == MenuType.Combat && CombatMode == FightMode.Training)
				GestureSparks(feedback);
		}
			
		public void GestureSparks(FeedbackFXType feedback)
		{
			if (gestureListener != null)
				StartCoroutine(gestureListener.FeedbackFXSparks(feedback));
		}


		public void TriggerRoundFX(int roundNumber, float xOffset = 0.0f, float yOffset = 0.0f, bool silent = false)
		{
			if (feedbackUI != null)
				feedbackUI.TriggerRoundFX(xOffset, yOffset);
			
			if (!silent && RoundSound != null)
				AudioSource.PlayClipAtPoint(RoundSound, Vector3.zero, SFXVolume);

			StartCoroutine(NumberFX(roundNumber, silent));
		}

		private IEnumerator NumberFX(int roundNumber, bool silent = false)
		{
			yield return new WaitForSeconds(newRoundTime);	// looks better if round plays slightly before number

			TriggerNumberFX(roundNumber, roundNumberOffset, 0);

			if (! silent)
				PlayNumberSound(roundNumber);
		}
			
		public void TriggerNumberFX(int number, float xOffset = 0.0f, float yOffset = 0.0f, string layer = null, bool silent = true)
		{
			if (feedbackUI != null)
				feedbackUI.TriggerNumberFX(number, xOffset, yOffset, layer);

			if (!silent && ReadyToFightSound != null)
				AudioSource.PlayClipAtPoint(ReadyToFightSound, Vector3.zero, SFXVolume);
		}

			
		public void CancelFeedbackFX()
		{
			if (feedbackUI != null)
				feedbackUI.CancelFeedbackFX();

//			HideSplatPaintStroke();
		}

		public void CancelRoundFX()
		{
			if (feedbackUI != null)
				feedbackUI.CancelRoundFX();
		}


		private IEnumerator NewRoundFeedback(float delay = 0.0f)
		{
			if (PreviewMode)
			{
				ReadyToFight = true;
				yield break;
			}

			ReadyToFight = false;

			if (delay > 0.0f)
				yield return new WaitForSeconds(delay);

			if (CombatMode == FightMode.Training || CombatMode == FightMode.Dojo)
			{
				yield return new WaitForSeconds(newRoundTime / 4.0f);	// short pause to allow fx to initialise ... or something (GAF?)
				ReadyToFight = true;
				yield break;
			}
				
			if (CombatMode == FightMode.Arcade)
			{
				yield return new WaitForSeconds(newRoundTime / 4.0f);	// short pause to allow fx to initialise ... or something (GAF?)

				// round number - separate animator to allow 2 animations to play at once...
				if (Player2.ProfileData.FighterClass == FighterClass.Boss)
				{
					TriggerFeedbackFX(FeedbackFXType.Boss_Alert);
					yield return new WaitForSeconds(newRoundTime);	// time for feedback to play out
				}
				else
				{
					TriggerRoundFX(RoundNumber);	
//					if (RoundSound != null)
//						AudioSource.PlayClipAtPoint(RoundSound, Vector3.zero, SFXVolume);
//				
//					yield return new WaitForSeconds(newRoundTime);	// looks better if round plays slightly before number
//
//					TriggerNumberFX(RoundNumber, roundNumberOffset, 0);
//					PlayNumberSound(RoundNumber);
				}

				yield return new WaitForSeconds(newRoundTime * 2.0f);	// time for round number to play out
			}

			TriggerFeedbackFX(FeedbackFXType.Fight);
			if (FightSound != null)
				AudioSource.PlayClipAtPoint(FightSound, Vector3.zero, SFXVolume);

			if (CombatMode == FightMode.Training)
				yield return new WaitForSeconds(newRoundTime);			// time for fight! to play out

			if (IsNetworkFight)
			{
				if (OnNetworkReadyToFight != null)
					OnNetworkReadyToFight(true);
			}
			else
				ReadyToFight = true;
			
			yield return null;
		}

		#endregion 	// feedback


		// feedback to show if a move was successfully cued / continued
		private void MoveCuedFeedback(bool ok, Vector3 position)
		{
			if (OnMoveCuedFeedback != null)
				OnMoveCuedFeedback(ok, position);
		}

		public void MoveCuedFeedback(bool ok)
		{
			MoveCuedFeedback(ok, gestureListener.LastFingerPosition);
		}
			

//		public void SlowDown()
//		{
////			if (FightPaused)
////				return;	
//
//			AdjustAnimationSpeed(speedAdjustmentFactor);	// slow fighters down
//			UpdateAnimationSpeed();							// FPS
//		}
//
//		public void SpeedUp()
//		{
////			if (FightPaused)
////				return;	
//
//			AdjustAnimationSpeed(1.0f / speedAdjustmentFactor);	// speed fighters up
//			UpdateAnimationSpeed();								// FPS
//		}


		public bool LoadSelectedScenery()
		{
			var locationFound = false;

			if (SelectedLocation != "")		
				locationFound = sceneryManager.BuildScenery(SelectedLocation);		// destroys current scenery - won't load scenery if already there

			return locationFound;
		}

		public void DestroyCurrentScenery()
		{
			sceneryManager.DestroyCurrentScenery(false);
		}


		#region UI 

		public void ToggleStatusUI()
		{
 			if (statusUI != null)
			{
				statusUIVisible = !statusUIVisible;
				statusUI.gameObject.SetActive(statusUIVisible);
			}
		}

		public void ToggleGameUI()
		{
			if (gameUI != null)
			{
				gameUIVisible = !gameUIVisible;
				gameUI.gameObject.SetActive(gameUIVisible);
			}
		}

		public void GameUIVisible(bool visible)
		{
			if (gameUI != null)
			{
				gameUIVisible = visible;
				gameUI.gameObject.SetActive(gameUIVisible);
			}
		}

		#endregion 	// UI


		#region menus 

		public void HideOptions()
		{
			options.gameObject.SetActive(false);
		}

		public void RevealOptions()
		{
			options.gameObject.SetActive(true);
		}


		public void BackClicked()
		{
			if (CanGoBack)
			{
				if (CurrentMenuCanvas == MenuType.PauseSettings)	// not pushed onto menu stack
				{
					ActivatePreviousMenu(true, false);
					SaveGameStatus();
				}
				else if (CurrentMenuCanvas == MenuType.WorldMap)		// not pushed onto menu stack
				{
					ConfirmQuitFight();
				}
				else
				{
					if (CurrentMenuCanvas == MenuType.Combat)
						ConfirmQuitFight();
					else if (! (CurrentMenuCanvas == MenuType.ArcadeFighterSelect && IsNetworkFight))		// synced by NetworkFightManager
						ActivatePreviousMenu(true, true);
				}

//				BackAudio();		// see ActivateMenu()

				if (OnBackClicked != null)
					OnBackClicked(CurrentMenuCanvas);
			}
		}

		public void PauseClicked()
		{
			if (CanSettings)
			{
				ActivateMenu(MenuType.PauseSettings);
//				ForwardAudio();
			}
				
			SaveGameStatus();
		}
			

		// game 'entry point'
		private void StartGame()
		{
			FightPaused = true;

			LoadSavedData();

			SavedGameStatus.PlayCount++;

			GameUIVisible(false);
			InitMenus();

			StartGameKudos();
		}

		private void LoadSavedData()
		{
			// restore coins, kudos, settings, inventory, fight status, etc
//			if (RestoreStatus())
//			{
////				Coins = 99000;		// TODO: remove this!!
//				
//				// TODO: restore fight status (2 fighters)
//			}
//			else

			if (!RestoreStatus())
			{
				// no saved status to restore - init default values
				ResetSettings();
//				Coins = InitialCoins;
//				Kudos = 0;
//				SFXVolume = DefaultSFXVolume;
//				MusicVolume = DefaultMusicVolume;
			}
				
			SetTheme(SavedGameStatus.Theme);

			if (OnLoadSavedStatus != null)
				OnLoadSavedStatus(SavedGameStatus);
		}

		private void ResetSettings()
		{
			Coins = InitialCoins;
			Kudos = 0;
			SFXVolume = DefaultSFXVolume;
			MusicVolume = DefaultMusicVolume;
		}


		public static void ResetInfoBubbleMessages()
		{
//			Debug.Log("ResetInfoBubbleMessages");
			SavedGameStatus.InfoMessagesRead = InfoBubbleMessage.None;
		}

		private void InitMenus()
		{
			menuStack = new List<MenuType>();

//			if (SceneSettings.BackFromLobby)
//				curtain.BlackOut();
		
			if (FightManager.IsNetworkFight && SceneSettings.DirectToFighterSelect)
				NetworkFighterSelect();
			else if (!SavedGameStatus.CompletedBasicTraining)
				StartCoroutine(FirstPlayerExperience());
			else
			{
//				StartCoroutine(CurtainUpFromLobby());
				ActivateMenu(MenuType.ModeSelect);
			}
		}

		// multiplayer - direct from lobby
		private void NetworkFighterSelect()
		{
//			if (curtain != null)
//			{
//				curtain.gameObject.SetActive(true);
//				yield return StartCoroutine(curtain.FadeToBlack());
//			}
//
			BaseModeSelectMenu();			// not shown / broadcast

			ActivateMenu(MenuType.ArcadeFighterSelect); 
			SceneSettings.DirectToFighterSelect = false;
//			yield return null;
		}
			
		private IEnumerator FirstPlayerExperience()
		{
			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			BaseModeSelectMenu();			// not shown / broadcast

			CombatMode = FightMode.Training;
			SavedGameStatus.NinjaSchoolFight = false;	

			SelectedLocation = FightManager.hawaii;

			GameUIVisible(SavedGameStatus.ShowHud);

			FightManager.IsNetworkFight = false;
			ActivateMenu(MenuType.Combat);
			yield return null;
		}
			
//		private IEnumerator CurtainUpFromLobby()
//		{
//			yield return StartCoroutine(curtain.CurtainUp(true));
//			curtain.gameObject.SetActive(false);
//
//			SceneSettings.BackFromLobby = false;
//		}

		private bool IsFirstMenu
		{
			get { return (menuStack.Count == 1 && CurrentMenuCanvas == MenuType.ModeSelect); }
		}
			
		public override bool CanNavigateBack { get { return true; } }			// quit fight

		private bool CanGoBack
		{
			get { return (!IsFirstMenu && CurrentMenu.CanNavigateBack); }
		}

		private bool CanSettings
		{
//			get { return ! (CurrentMenuCanvas == MenuType.MatchStats || CurrentMenuCanvas == MenuType.PauseSettings || CurrentMenuCanvas == MenuType.WorldMap); }
			get { return !FightManager.IsNetworkFight && (! (CurrentMenuCanvas == MenuType.MatchStats || CurrentMenuCanvas == MenuType.PauseSettings || CurrentMenuCanvas == MenuType.WorldMap)); }
		}

		private bool CoinsVisible
		{
			get { return (CurrentMenuCanvas == MenuType.Dojo || CurrentMenuCanvas == MenuType.TeamSelect || CurrentMenuCanvas == MenuType.PauseSettings
											|| CurrentMenuCanvas == MenuType.ArcadeFighterSelect || CurrentMenuCanvas == MenuType.SurvivalFighterSelect); }
		}

		private bool KudosVisible
		{
			get { return ((CurrentMenuCanvas == MenuType.Combat && CombatMode != FightMode.Training) || CurrentMenuCanvas == MenuType.PauseSettings
								|| CurrentMenuCanvas == MenuType.Dojo || CurrentMenuCanvas == MenuType.Facebook || CurrentMenuCanvas == MenuType.Leaderboards); }
		}

		private void MenuPop()
		{
			if (menuStack.Count == 0)
				return;
			
			menuStack.RemoveAt(0);
//			ListMenuStack("POPPED");
		}

		private MenuType MenuPeek()
		{
			if (menuStack.Count == 0)
				return MenuType.None;
			
			return menuStack[0];
		}

		private void MenuPush(MenuType menu)
		{
			// if menu is already in menuStack, remove it before pushing on top
//			if (menuStack.Contains(menu))
//				menuStack.Remove(menu);

			if (menuStack.Contains(menu))			// if menu is already in menuStack, pop all on top to 'rewind' to it
			{
				while (MenuPeek() != menu)
					ActivatePreviousMenu(false, true);
			}
			else
			{
				menuStack.Insert(0, menu);
//				ListMenuStack("PUSHED");
			}
		}
			

		public bool IsMenuOnTop(MenuType menu)
		{
			return MenuPeek() == menu;
		}

		private void ListMenuStack(string heading)
		{
			string stack = heading + " ";
			for (int i = menuStack.Count-1; i >= 0; i--)
			{
				stack += " | " + menuStack[i];
			}
			Debug.Log("MenuStack: " + stack);
		}

		private void ResetMenuStack()
		{
			// clear all except first
			if (menuStack.Count <= 1)
				return;

			for (int i = 0; i < menuStack.Count-1; i++)
			{
				menuStack.RemoveAt(i);
			}
		}


		private void ActivatePreviousMenu(bool backClicked, bool pop)
		{
			if (menuStack.Count < 1)		// make sure there is something to pop!
				return;

//			var currentMenu = CurrentMenuCanvas;

			if (backClicked)		// if the current menu has an active overlay, simply hide it
			{
//				Debug.Log("ActivatePreviousMenu: backClicked = " + backClicked + ", pop = " + pop);

				switch (CurrentMenuCanvas)
				{
					case MenuType.ModeSelect:
						if (! modeSelect.HideActiveOverlay())		// returns true if menu was activated directly to an overlay
							return;				
						break;

					case MenuType.Dojo:	
						if (! storeManager.HideActiveOverlay())
							return;
						break;

					case MenuType.Advert:
//						if (! adManager.HideActiveOverlay())
//							return;
						break;

					case MenuType.ArcadeFighterSelect:
						if (! arcadeFighterSelect.HideActiveOverlay())
							return;
						break;

					case MenuType.SurvivalFighterSelect:
						if (! survivalFighterSelect.HideActiveOverlay())
							return;
						break;

					case MenuType.TeamSelect:
//						teamSelect.HideAllOverlays();
						if (! teamSelect.HideActiveOverlay())
							return;
						break;

					case MenuType.WorldMap:
						if (! worldMap.HideActiveOverlay())
							return;
						break;

					case MenuType.MatchStats:
						if (! matchStats.HideActiveOverlay())
							return;
						break;

					case MenuType.Facebook:
						if (! FBManager.HideActiveOverlay())
							return;				
						break;

					case MenuType.Leaderboards:
						if (! LeaderboardManager.HideActiveOverlay())
							return;				
						break;

					case MenuType.PauseSettings:
						break;

					// can't go back from combat canvas
					case MenuType.Combat:			// not strictly a menu canvas
					case MenuType.None:
					default:
						break;
				}
			}

			if (pop && menuStack.Count > 1)		// stack can't be empty! (default mode select was first)
				MenuPop();
				
			ActivateMenu(MenuPeek(), false, backClicked);

//			if (OnPreviousMenu != null)
//				OnPreviousMenu(currentMenu, backClicked);
		}

		private bool ActivateMenu(MenuType menu, bool push = true, bool navigatingBack = false)
		{
			if (menu == MenuType.Combat)				// fight music according to location
				sceneryManager.PlayCurrentSceneryTrack();

//			Debug.Log("ActivateMenu: " + menu + ", Count = " + menuStack.Count + ", CurrentMenuCanvas = " + CurrentMenuCanvas + ", navigatingBack = " + navigatingBack);

//			// nothing to do if no change...
			if (menu == CurrentMenuCanvas)
			{
				if (menu == MenuType.Combat)	// eg. continuing arcade match from MatchStats (which is not in menu stack)
				{
					// broadcast menu changed event
					if (OnMenuChanged != null)
						OnMenuChanged(CurrentMenuCanvas, CanGoBack, CanSettings, CoinsVisible, KudosVisible);
				}
				return false;
			}

			var navigatedFrom = CurrentMenuCanvas;

			DeactivateCurrentMenu();
			CurrentMenuCanvas = menu;		// not necessarily on stack (eg. pauseSettings)
			
			if (! navigatingBack)
			{
				CurrentMenu.NavigatedFrom = navigatedFrom;
//				ForwardAudio();
			}
			else
				BackAudio();
				
			switch (menu)
			{
				case MenuType.None:
					return false;

				case MenuType.ModeSelect:
					ResetMenuStack();										// clear all except first
					StartCoroutine(ShowModeSelectCanvas());
					break;

				case MenuType.Combat:	
					StartCoroutine(ShowCombatCanvas(! navigatingBack));		// start new fight if not navigating back
					break;

				case MenuType.Dojo:		
					StartCoroutine(ShowDojoCanvas());
					break;

				case MenuType.Advert:
//					StartCoroutine(ShowAdCanvas());
					break;

				case MenuType.ArcadeFighterSelect:
					StartCoroutine(ShowArcadeSelectCanvas());
					break;

				case MenuType.SurvivalFighterSelect:
					StartCoroutine(ShowSurvivalSelectCanvas());
					break;

				case MenuType.TeamSelect:
					StartCoroutine(ShowTeamSelectCanvas());
					break;

				case MenuType.WorldMap:
					StartCoroutine(ShowWorldMapCanvas());
					push = false;
					break;

				case MenuType.Leaderboards:
					StartCoroutine(ShowLeaderboardsCanvas());
					break;

				case MenuType.Facebook:
					StartCoroutine(ShowFacebookCanvas());
					break;

				// TODO: should match stats be in menu stack?  (or like pause menu?)
//				case MenuType.MatchStats:
//					StartCoroutine(ShowMatchStatsCanvas(winner));
//					push = false;			// don't want in menu stack!
//					break;

				case MenuType.PauseSettings:
					StartCoroutine(ShowPauseSettingsCanvas());
					push = false;			// pause is special case - don't want it in menu stack!
					break;
			}
					
			if (push)
				MenuPush(menu);		// top of stack
	
			if (CurrentMenuCanvas != MenuType.None)
				CurrentMenu.OnDeactivate += GoBack;

			// play menu music if not combat (music acording to location) and not pausesettings from combat
			if (CurrentMenuCanvas != MenuType.Combat && !(CurrentMenuCanvas == MenuType.PauseSettings && navigatedFrom == MenuType.Combat))		// fight music according to menu
				CurrentMenu.PlayMusic();
			
			// broadcast menu changed event
			if (OnMenuChanged != null)
				OnMenuChanged(CurrentMenuCanvas, CanGoBack, CanSettings, CoinsVisible, KudosVisible);

			return true;
		}

		private void BaseModeSelectMenu()
		{
			CurrentMenuCanvas = MenuType.ModeSelect;
			MenuPush(MenuType.ModeSelect);		// top of stack
		}

		// previous menu activated by MenuCanvas DirectToOverlay concept - as if back clicked
		private void GoBack()
		{
			ActivatePreviousMenu(false, true);		
		}

		private void DeactivateCurrentMenu()
		{
			if (CurrentMenuCanvas != MenuType.None)
				CurrentMenu.OnDeactivate -= GoBack;
			
			switch (CurrentMenuCanvas)
			{
				case MenuType.ModeSelect:
					HideModeSelectCanvas();
					break;

				case MenuType.Dojo:	
					HideDojoCanvas();
					break;

				case MenuType.Advert:
//					HideAdCanvas();
					break;

				case MenuType.ArcadeFighterSelect:
					HideArcadeSelectCanvas();
					break;

				case MenuType.SurvivalFighterSelect:
					HideSurvivalSelectCanvas();
					break;

				case MenuType.TeamSelect:
					HideTeamSelectCanvas();
					break;

				case MenuType.Facebook:
					HideFacebookCanvas();
					break;

				case MenuType.Leaderboards:
					HideLeaderboardsCanvas();
					break;

				case MenuType.WorldMap:
					HideWorldMapCanvas();
					break;

				case MenuType.PauseSettings:
					HidePauseSettingsCanvas();
					break;

				case MenuType.Combat:	
				case MenuType.None:
				default:
					break;
			}
		}

		public MenuCanvas CurrentMenu
		{
			get
			{
				switch (CurrentMenuCanvas)
				{
					case MenuType.ModeSelect:
						return modeSelect;

					case MenuType.Dojo:	
						return storeManager;

//					case MenuType.Advert:
//						return adManager;

					case MenuType.ArcadeFighterSelect:
						return arcadeFighterSelect;

					case MenuType.SurvivalFighterSelect:
						return survivalFighterSelect;

					case MenuType.TeamSelect:
						return teamSelect;

					case MenuType.WorldMap:
						return worldMap;

					case MenuType.Facebook:
						return FBManager;

					case MenuType.Leaderboards:
						return LeaderboardManager;

					case MenuType.PauseSettings:
						return pauseSettings;

					case MenuType.Combat:	
						return this; 

					case MenuType.None:
					default:
						return null;
				}
			}
		}
			

		public static void GetConfirmation(string message, int coins, Action actionOnYes)
		{
			areYouSure.Confirm(message, coins, actionOnYes);
		}

		public static void GetPowerUpConfirmation(PowerUpDetails powerUpDetails, Action actionOnYes)
		{
			areYouSure.Confirm(powerUpDetails, actionOnYes);
		}

		public static void GetOkConfirmation(string message, int coins)
		{
			areYouSure.ConfirmOk(message, coins);
		}

		public static void InsertCoinToPlay(Action actionOnYes, string message = "", int coins = 1)
		{
			insertCoinToPlay.ConfirmInsertCoin(actionOnYes, message, coins);
		}

		public static void RequestPurchase()
		{
			purchaseCoins.RequestPurchase();
		}
			
		public static void BuyCoinsToPlay(Action actionOnYes)
		{
			string message = FightManager.Translate("confirmBuyCoins");
			areYouSure.Confirm(message, 0, actionOnYes);
		}

		public static void RegisterNewUser()
		{
			userRegistration.PromptForNewUserId();
		}

		public static void ConfirmChallengeUpload(ChallengeData challenge, Action actionOnYes)
		{
			challengeUpload.Confirm(challenge, actionOnYes);
		}

		public static void ShowChallengeResult(int challengePot, bool defenderWon, string challengerId, Action actionOnOk)
		{
			challengeResult.Notify(challengePot, defenderWon, challengerId, actionOnOk);
		}

		public static void ShowLockedFighter(FighterCard fighterCard, Fighter fighter)
		{
			fighterUnlock.ShowLockedFighter(fighterCard, fighter);
		}

		public static void UnlockFighter(Fighter fighter)
		{
			fighterUnlock.UnlockFighter(fighter);
		}

		#endregion 		// menus


//		private void FBLoginFail(string error)
//		{
//
//		}
//
//		private void FBLoginSuccess()
//		{
//
//		}

		#region pause settings 

		private IEnumerator SelectPauseSettings()
		{
			FreezeFight();
			pauseSettings.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			PauseSettingsChoice = MenuType.None;
			while (PauseSettingsChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(PauseSettingsChoice);
			HidePauseSettingsCanvas();
		}


		private IEnumerator ShowPauseSettingsCanvas()
		{	
			if (! IsNetworkFight)
				PauseFight(true);
			
			yield return StartCoroutine(SelectPauseSettings());
		}

		private void HidePauseSettingsCanvas()
		{
			PauseFight(false);
			pauseSettings.Hide();
		}

		#endregion


		#region match stats 

		private IEnumerator MatchEndStats(Fighter winner, List<ChallengeRoundResult> roundResults, bool completedWorldTour)
		{
//			Debug.Log("MatchEndStats: winner = " + (winner == null ? " NULL!" : winner.FullName));

			FreezeFight();
			GameUIVisible(false);

			matchStats.Show();

			if (winner.IsPlayer1)		// also works for vs mode
				matchStats.PlayMusic();

			// doesn't go through ActivateMenu (not part of menu stack)
			if (OnMenuChanged != null)
				OnMenuChanged(MenuType.MatchStats, false, false, false, false);

			if (CombatMode == FightMode.Challenge)
				matchStats.FirstChallengeResult(roundResults);				// loop through round results, showing FighterCards
			else
				matchStats.RevealWinner(winner, completedWorldTour);		// set image and animate entry from side
			
			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			MatchStatsChoice = MenuType.None;
			while (MatchStatsChoice == MenuType.None)			// set when tapped on match stats
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(MatchStatsChoice);
			HideMatchStats();

			if (MatchStatsRestartMatch)		// coin inserted to continue
			{
				StartCoroutine(RestartMatch());
				MatchStatsRestartMatch = false;

				GameUIVisible(SavedGameStatus.ShowHud);
			}
		}


		private IEnumerator ShowMatchStatsCanvas(Fighter winner, List<ChallengeRoundResult> roundResults, bool completedWorldTour = false)
		{
			yield return StartCoroutine(MatchEndStats(winner, roundResults, completedWorldTour));
		}

		private void HideMatchStats()
		{
			matchStats.Hide();
		}

		#endregion


		#region combat

		private IEnumerator ShowCombatCanvas(bool newFight)
		{
			bool animateEntry = newFight && (CombatMode == FightMode.Training || CombatMode == FightMode.Dojo);

			if (newFight)
			{
				PreviewMode = false;
		
				if (newFightCoroutine != null)
					StopCoroutine(newFightCoroutine);

				yield return StartCoroutine(cameraController.TrackHome(true, true));		// track back to zero, load selected scenery

				if (animateEntry)
				{
					GameUIVisible(false);			// animate entry on curtain up
					animatingNewFightEntry = true;	// to suspend StartNewFight new round feedback / start training
				}

				newFightCoroutine = StartNewFight(true);		// create fighters, but delay round/fight feedback until curtain is up
				StartCoroutine(newFightCoroutine);	
			}

			GameUIVisible(SavedGameStatus.ShowHud);

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}
				
			if (animateEntry)
			{
//				GameUIVisible(SavedGameStatus.ShowHud);
				gameUI.TriggerEntry();
			}
				
			PauseFight(false);
			UnfreezeFight();

			yield return null;
		}

		public void NewFightFillGauge()				// called by OnEntryStart
		{
			Player1.UpdateGauge(4, false);					// temporarily, for entry animation
			Player2.UpdateGauge(4, false);					// temporarily, for entry animation
		}

		public void NewFightEntryComplete()			// called by OnEntryComplete
		{
			var gauge = FightManager.CombatMode == FightMode.Dojo ? Fighter.maxGauge : 0;

			if (HasPlayer1)
				Player1.UpdateGauge(gauge, true);
			if (HasPlayer2)
				Player2.UpdateGauge(gauge, true);

			animatingNewFightEntry = false;

//			PauseFight(false);
//			UnfreezeFight();
		}

		#endregion

		#region world map 

		private IEnumerator SelectWorldLocation()
		{
//			Debug.Log("SelectWorldLocation");
			FreezeFight();
			worldMap.Show();

//			// doesn't go through ActivateMenu (not part of menu stack)
//			if (OnMenuChanged != null)
//				OnMenuChanged(MenuType.WorldMap, false, true, false, false);

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}
				
			WorldMapChoice = MenuType.None;
			while (WorldMapChoice == MenuType.None)			// set when location selected on world map
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			yield return StartCoroutine(cameraController.TrackHome(true, true));			// track back to zero, load selected scenery

//			Debug.Log("SelectWorldLocation: WorldMapChoice = " + WorldMapChoice);
			ActivateMenu(WorldMapChoice);					// combat (new match)
			HideWorldMapCanvas();
		}


		private IEnumerator ShowWorldMapCanvas()
		{
//			Debug.Log("ShowWorldMapCanvas");
			yield return StartCoroutine(SelectWorldLocation());
		}

		private void HideWorldMapCanvas()
		{
			worldMap.Hide();
		}

		#endregion

		#region arcade mode fighter select

		private IEnumerator ArcadeSelectFighter()
		{
			FreezeFight();
			arcadeFighterSelect.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}
				
			SceneSettings.DirectToFighterSelect = false;

			FighterSelectChoice = MenuType.None;
			while (FighterSelectChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

//			PreviewMode = false;

			ActivateMenu(FighterSelectChoice);
			HideArcadeSelectCanvas();
		}


		private IEnumerator ShowArcadeSelectCanvas()
		{
			PreviewMode = true;
			yield return StartCoroutine(ArcadeSelectFighter());
		}

		private void HideArcadeSelectCanvas()
		{
			arcadeFighterSelect.DestroyPreview();
			arcadeFighterSelect.Hide();
		}

		#endregion

		#region survival mode fighter select

		private IEnumerator SurvivalSelectFighter()
		{
			FreezeFight();
			survivalFighterSelect.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			FighterSelectChoice = MenuType.None;
			while (FighterSelectChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(FighterSelectChoice);
			HideSurvivalSelectCanvas();
		}
			
		private IEnumerator ShowSurvivalSelectCanvas()
		{
			PreviewMode = true;
			yield return StartCoroutine(SurvivalSelectFighter());
		}

		private void HideSurvivalSelectCanvas()
		{
			survivalFighterSelect.DestroyPreview();
			survivalFighterSelect.Hide();
		}

		#endregion

	
		#region challenge mode team select

		private IEnumerator SelectChallengeTeam()
		{
			FreezeFight();
			teamSelect.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			TeamSelectChoice = MenuType.None;
			while (TeamSelectChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(TeamSelectChoice);
			HideTeamSelectCanvas();
		}


		private IEnumerator ShowTeamSelectCanvas()
		{
			yield return StartCoroutine(SelectChallengeTeam());
		}

		private void HideTeamSelectCanvas()
		{
			teamSelect.Hide();
		}

		#endregion

		#region fight mode select 

		private IEnumerator ModeSelect()
		{
			FreezeFight();
			modeSelect.Show();

			FightManager.IsNetworkFight = false;
			SceneSettings.DirectToFighterSelect = false;

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}
				
			ModeSelectChoice = MenuType.None;
			while (ModeSelectChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}
				
			gameUI.SetupCombatMode();			// update 'title'

			ActivateMenu(ModeSelectChoice);
			HideModeSelectCanvas();
		}
			
		private IEnumerator ShowModeSelectCanvas()
		{
			yield return StartCoroutine(ModeSelect());
		}

		private void HideModeSelectCanvas()
		{
			modeSelect.Hide();
		}

		#endregion

		#region leaderboards

		private IEnumerator ShowLeaderboards()
		{
			LeaderboardManager.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			FacebookChoice = MenuType.None;
			while (LeaderboardsChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(LeaderboardsChoice);
			HideFacebookCanvas();
		}

		private IEnumerator ShowLeaderboardsCanvas() 
		{
			yield return StartCoroutine(ShowLeaderboards());
		}

		private void HideLeaderboardsCanvas()
		{
			LeaderboardManager.Hide();
		}

		#endregion

		#region facebook

		private IEnumerator ShowFacebook()
		{
			FreezeFight();
			FBManager.Show();

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			FBManager.Login();
	
			FacebookChoice = MenuType.None;
			while (FacebookChoice == MenuType.None)
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

			ActivateMenu(FacebookChoice);
			HideFacebookCanvas();
		}

		private IEnumerator ShowFacebookCanvas() 
		{
			yield return StartCoroutine(ShowFacebook());
		}

		private void HideFacebookCanvas()
		{
			FBManager.Hide();
		}

		#endregion

		#region dojo

		private IEnumerator ShowDojo()
		{
			FreezeFight();
			storeManager.Show();

			switch (SelectedMenuOverlay)
			{
				case MenuOverlay.None:
					break;

//				case MenuOverlay.SpendCoins:
//					DojoSpendOverlay(true);
//					break;
//
//				case MenuOverlay.BuyCoins:
//					DojoBuyOverlay(true);
//					break;

				case MenuOverlay.PowerUp:
					DojoPowerUpOverlay(SelectedFighterName, SelectedFighterColour, true);
					break;
			}

			SelectedMenuOverlay = MenuOverlay.None;

			if (curtain != null)
			{
				yield return StartCoroutine(curtain.CurtainUp());
				curtain.gameObject.SetActive(false);
			}

			StoreChoice = MenuType.None;
			while (StoreChoice == MenuType.None)	
			{
				yield return null;
			}

			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				yield return StartCoroutine(curtain.FadeToBlack());
			}

//			gameUI.SetCombatMode();			// update 'title'

			ActivateMenu(StoreChoice);
			HideDojoCanvas();
		}
			
		private IEnumerator ShowDojoCanvas()
		{
			PreviewMode = true;
			yield return StartCoroutine(ShowDojo());
		}

		private void HideDojoCanvas()
		{
			storeManager.DestroyPreviewFighter();
			storeManager.Hide();
		}

//		private void DojoSpendOverlay(bool direct)
//		{
//			storeManager.DirectToOverlay = direct;
//			storeManager.ShowSpendOverlay();
//		}
//
//		private void DojoBuyOverlay(bool direct)
//		{
//			storeManager.DirectToOverlay = direct;
//			storeManager.ShowBuyOverlay();
//		}

		private void DojoPowerUpOverlay(string fighterName, string fighterColour, bool direct)
		{
//			Debug.Log("StorePowerUpOverlay: " + fighterName + ", direct = " + direct);
			storeManager.DirectToOverlay = direct;
			storeManager.ShowPowerUpOverlay(fighterName, fighterColour);
		}

//		public IEnumerator DojoBuyCoins()
//		{
//			storeManager.DirectToOverlay = true;
//			SelectedMenuOverlay = MenuOverlay.BuyCoins;
//			yield return StartCoroutine(ShowDojo());
//		}
			
		#endregion

//		#region advert

//		private IEnumerator WatchAd()
//		{
//			FreezeFight();
//			adManager.Show();
//
//			if (curtain != null)
//			{
//				yield return StartCoroutine(curtain.CurtainUp());
//				curtain.gameObject.SetActive(false);
//			}
//
//			AdChoice = MenuType.None;
//			while (AdChoice == MenuType.None)	
//			{
//				yield return null;
//			}
//
//			if (curtain != null)
//			{
//				curtain.gameObject.SetActive(true);
//				yield return StartCoroutine(curtain.FadeToBlack());
//			}
//
//			HideAdCanvas();
//		}
//
//
//		private IEnumerator ShowAdCanvas()
//		{
//			yield return StartCoroutine(WatchAd());
//		}
//
//		private void HideAdCanvas()
//		{
//			adManager.Hide();
//		}

//		#endregion


//		public void TogglePauseFight()
//		{
//			PauseFight(! FightPaused);
//		}

//		public void ToggleTurbo()
//		{
//			if (FightPaused)
//				return;
//
//			TurboMode = !TurboMode;
//			AnimationFPS = TurboMode ? TurboAnimationFPS : DefaultAnimationFPS;
//			Time.fixedDeltaTime = AnimationFrameInterval;		// based on FPS
//		}

		public void SaveGameStatus()
		{
//			Debug.Log("Saving status: " + CombatMode + " Difficulty = " + SavedGameStatus.Difficulty);

			// save fight status if not in training
//			if (HasPlayer1 && Player1.InTraining)
//				return;

			FirebaseManager.PostLeaderboardScore(Leaderboard.Kudos, SavedGameStatus.Kudos);

			// save critical data as PlayerPrefs (as well as serialized file)
			SavePlayerPrefs();
			
			BinaryFormatter bf = new BinaryFormatter();
			FileStream fileStream = File.Open(FilePath(), FileMode.OpenOrCreate);

			SavedGameStatus.SavedTime = DateTime.Now;

			// save fight status if not in training
			if (HasPlayer1 && !Player1.InTraining)
			{
				// save P1 / P2 / challenge teams
				SavedGameStatus.CombatMode = CombatMode;
				SavedGameStatus.FightLocation = SelectedLocation;

				// save current health etc. of current fighters in all cases
				if (HasPlayer1)
					SavedGameStatus.Player1 = ProfileFromFighter(Player1);
				if (HasPlayer2)
					SavedGameStatus.Player2 = ProfileFromFighter(Player2);

				if (CombatMode == FightMode.Challenge && challengeTeam != null && challengeAITeam != null)
				{
					SavedGameStatus.playerTeam = new SavedProfile[challengeTeam.Count];
					int teamCounter = 0;
					foreach (var fighterCard in challengeTeam)
					{
						SavedGameStatus.playerTeam[teamCounter] = ProfileFromCard(fighterCard);
						teamCounter++;
					}

					SavedGameStatus.AITeam = new SavedProfile[challengeAITeam.Count];
					teamCounter = 0;
					foreach (var AICard in challengeAITeam)
					{
						SavedGameStatus.AITeam[teamCounter] = ProfileFromCard(AICard);
						teamCounter++;
					}
				}
			}

			try
			{
				bf.Serialize(fileStream, SavedGameStatus);
			}
			catch (Exception ex)
			{
				Debug.Log("SaveStatus: Serialize failed: " + ex.Message);
			}
			finally
			{
				fileStream.Close();
			}

			Debug.Log("Saved status: Coins = " + Coins);
		}
			

//		private void DeleteStatusFile()
//		{
//			var filePath = FilePath(CombatMode);
//
//			if (File.Exists(filePath))
//				File.Delete(filePath);
//		}

		private void OnSavedStatusChanged()
		{
			if (OnCoinsChanged != null)
				OnCoinsChanged(Coins);

			if (OnKudosChanged != null)
				OnKudosChanged(Kudos);

			if (OnMusicVolumeChanged != null)
				OnMusicVolumeChanged(MusicVolume);

			if (OnSFXVolumeChanged != null)
				OnSFXVolumeChanged(SFXVolume);

			foreach (var item in SavedGameStatus.PowerUpInventory)
			{
				if (OnPowerUpInventoryChanged != null)
					OnPowerUpInventoryChanged(item.PowerUp, item.Quantity);
			}
		}


		// save important data as PlayerPrefs (as well as serialized file)
		private void SavePlayerPrefs()
		{
			PlayerPrefs.SetString("FL_UserId", SavedGameStatus.UserId);
			PlayerPrefs.SetString("FL_VersionNumber", SavedGameStatus.VersionNumber);
			PlayerPrefs.SetInt("FL_LimitedVersion", SavedGameStatus.LimitedVersion);
			PlayerPrefs.SetFloat("FL_Kudos", Kudos);
			PlayerPrefs.SetInt("FL_Coins", Coins);
			PlayerPrefs.SetInt("FL_BestDojoDamage", SavedGameStatus.BestDojoDamage);
			PlayerPrefs.SetInt("FL_BestSurvivalEndurance", SavedGameStatus.BestSurvivalEndurance);
			PlayerPrefs.SetInt("FL_TotalChallengeWinnings", SavedGameStatus.TotalChallengeWinnings);
			PlayerPrefs.SetInt("FL_WorldTourCompletions", SavedGameStatus.WorldTourCompletions);
			PlayerPrefs.SetInt("FL_VSVictoryPoints", SavedGameStatus.VSVictoryPoints);
			PlayerPrefs.SetInt("FL_CompletedTraining", SavedGameStatus.CompletedBasicTraining ? 1 : 0);
			PlayerPrefs.SetString("FL_SelectedFighter", SelectedFighterName);
			PlayerPrefs.SetString("FL_SelectedColour", SelectedFighterColour);
			PlayerPrefs.Save();
		}

		private void RestorePlayerPrefs()
		{
			SavedGameStatus.UserId = PlayerPrefs.GetString("FL_UserId", "");
			SavedGameStatus.VersionNumber = PlayerPrefs.GetString("FL_VersionNumber", "1.0");
			SavedGameStatus.LimitedVersion = PlayerPrefs.GetInt("FL_LimitedVersion", 0);
			Kudos = PlayerPrefs.GetFloat("FL_Kudos", 0);
			Coins = PlayerPrefs.GetInt("FL_Coins", 0);
			SavedGameStatus.BestDojoDamage = PlayerPrefs.GetInt("FL_BestDojoDamage", 0);
			SavedGameStatus.BestSurvivalEndurance = PlayerPrefs.GetInt("FL_BestSurvivalEndurance", 0);
			SavedGameStatus.TotalChallengeWinnings = PlayerPrefs.GetInt("FL_TotalChallengeWinnings", 0);
			SavedGameStatus.WorldTourCompletions = PlayerPrefs.GetInt("FL_WorldTourCompletions", 0);
			SavedGameStatus.VSVictoryPoints = PlayerPrefs.GetInt("FL_VSVictoryPoints", 0);
			SavedGameStatus.CompletedBasicTraining = PlayerPrefs.GetInt("FL_CompletedTraining", 0) != 0;
			SelectedFighterName = PlayerPrefs.GetString("FL_SelectedFighter", "Leoni");
			SelectedFighterColour = PlayerPrefs.GetString("FL_SelectedColour", "P1");
		}

		private bool RestoreStatus(bool clearInventory = false)
		{
			var filePath = FilePath();

			if (File.Exists(filePath))
			{
				try
				{
					BinaryFormatter bf = new BinaryFormatter();
					FileStream file = File.Open(filePath, FileMode.Open);
					SavedGameStatus = (SavedStatus) bf.Deserialize(file);
					file.Close();

					if (SavedGameStatus.PowerUpInventory == null || clearInventory)
						SavedGameStatus.PowerUpInventory = new List<InventoryPowerUp>();

					if (string.IsNullOrEmpty(SelectedFighterName))
					{
						SelectedFighterName = "Leoni";
						SelectedFighterColour = "P1";
					}

					Debug.Log("RestoreStatus ok: Coins = " + Coins + ", Kudos = " + Kudos);
					OnSavedStatusChanged();
					return true;
				}
				catch (Exception)
				{
					// TODO: most likely change in SavedStatus - handle 'upgrades'
					Debug.Log("RestoreStatus: Deleting invalid file: " + filePath);

					File.Delete(filePath);

					SavedGameStatus = new SavedStatus();
					SavedGameStatus.VersionNumber = Application.version;

					RestorePlayerPrefs();			// important data saved as PlayerPrefs too
					return false;
				}
			}

			SavedGameStatus = new SavedStatus();
			SavedGameStatus.VersionNumber = Application.version;

			RestorePlayerPrefs();					// important data saved as PlayerPrefs too	

			Debug.Log("RestoreStatus: No saved file");
			return true;
		}
			

		private string FilePath()
		{
			var fileName = "Status.dat";
			return Application.persistentDataPath + "/" + fileName;
		}


		public void ResetGame()
		{
			if (curtain != null)
			{
				curtain.gameObject.SetActive(true);
				StartCoroutine(curtain.FadeToBlack());
			}
				
			if (TrainingUI.InfoBubbleShowing)
			{
				if (OnInfoBubbleRead != null)
					OnInfoBubbleRead();				// hides bubble and marks message as read
			}
				
			HideAllMenus();
				
			var filePath = FilePath();

			if (File.Exists(filePath))
				File.Delete(filePath);

			SavedGameStatus = new SavedStatus();
			SavedGameStatus.VersionNumber = Application.version;

			ResetSettings();

			SaveGameStatus();

			PlayerPrefs.SetString("FL_UserId", "");
			PlayerPrefs.SetFloat("FL_Kudos", 0);
			PlayerPrefs.SetInt("FL_Coins", 0);
			PlayerPrefs.SetInt("FL_BestDojoDamage", 0);
			PlayerPrefs.SetInt("FL_BestSurvivalEndurance", 0);
			PlayerPrefs.SetInt("FL_TotalChallengeWinnings", 0);
			PlayerPrefs.SetInt("FL_WorldTourCompletions", 0);
			PlayerPrefs.SetInt("FL_CompletedTraining", 0);
			PlayerPrefs.SetString("FL_SelectedFighter", "Leoni");
			PlayerPrefs.SetString("FL_SelectedColour", "P1");
			PlayerPrefs.Save();

			Profile.DeleteFighterProfile("Leoni");	
			Profile.DeleteFighterProfile("Shiro");	

			Profile.DeleteFighterProfile("Danjuma");
			Profile.DeleteFighterProfile("Natalya");	
			Profile.DeleteFighterProfile("Hoi Lun");	
			Profile.DeleteFighterProfile("Alazne");	
			Profile.DeleteFighterProfile("Jackson");
			Profile.DeleteFighterProfile("Shiyang");	
			Profile.DeleteFighterProfile("Ninja");	
			Profile.DeleteFighterProfile("Skeletron");	


			Profile.InitFighterLockStatus("Leoni", false, 0, 0, 0, AIDifficulty.Easy);	
			Profile.InitFighterLockStatus("Shiro", false, 0, 0, 0, AIDifficulty.Easy);	
			Profile.InitFighterLockStatus("Danjuma", false, 0, 0, 0, AIDifficulty.Easy);	
			Profile.InitFighterLockStatus("Natalya", false, 0, 0, 0, AIDifficulty.Easy);	

			Profile.InitFighterLockStatus("Hoi Lun", false, 0, 0, 0, AIDifficulty.Easy);		
			Profile.InitFighterLockStatus("Jackson", false, 0, 0, 0, AIDifficulty.Easy);	
			Profile.InitFighterLockStatus("Alazne", false, 0, 0, 0, AIDifficulty.Easy);		
			Profile.InitFighterLockStatus("Shiyang", false, 0, 0, 0, AIDifficulty.Easy);		

//			Profile.InitFighterLockStatus("Hoi Lun", true, 1, 900, 5, AIDifficulty.Medium);	
//			Profile.InitFighterLockStatus("Jackson", true, 1, 900, 5, AIDifficulty.Medium);
//			Profile.InitFighterLockStatus("Alazne", true, 1, 900, 5, AIDifficulty.Medium);	
//			Profile.InitFighterLockStatus("Shiyang", true, 1, 900, 5, AIDifficulty.Medium);	

			Profile.InitFighterLockStatus("Ninja", true, 1, 5000, 100, AIDifficulty.Medium); 	// beat any opponent in arcade mode (never face AI Ninja)
			Profile.InitFighterLockStatus("Skeletron", true, 1, 90000, 3, AIDifficulty.Hard); 	

			CleanupFighters();
			StartGame();

			if (OnGameReset != null)
				OnGameReset();
		}

		private SavedProfile ProfileFromCard(FighterCard card)
		{
			SavedProfile profile = new SavedProfile();

			profile.FighterName = card.FighterName;
			profile.FighterColour = card.FighterColour;
			profile.Level = card.Level;
			profile.XP = card.XP;
			profile.TriggerPowerUp = card.TriggerPowerUp;
			profile.StaticPowerUp = card.StaticPowerUp;

			profile.TriggerPowerUpCoolDown = storeManager.GetPowerUpCooldown(profile.TriggerPowerUp);

			return profile;
		}

		private SavedProfile ProfileFromFighter(Fighter fighter)
		{
			SavedProfile profile = new SavedProfile();

			var savedData = fighter.ProfileData.SavedData;

			profile.FighterName = savedData.FighterName;
			profile.FighterColour = savedData.FighterColour;
			profile.Level = savedData.Level;
			profile.XP = savedData.XP;
			profile.TotalXP = savedData.TotalXP;

			profile.TriggerPowerUp = savedData.TriggerPowerUp;
			profile.StaticPowerUp = savedData.StaticPowerUp;

			profile.TriggerPowerUpCoolDown = storeManager.GetPowerUpCooldown(profile.TriggerPowerUp);

			profile.RoundsWon = savedData.RoundsWon;
			profile.RoundsLost = savedData.RoundsLost;
			profile.MatchRoundsWon = savedData.MatchRoundsWon;
			profile.MatchRoundsLost = savedData.MatchRoundsLost;

			return profile;
		}


		public static string Translate(string text, bool wrap = false, bool exclaim = false, bool toUpper = false)
		{
			return Translator.Instance.LookupString(text, wrap, exclaim, toUpper);
		}
			

		public void HealthDebugText(bool player1, string text)
		{
			if (player1)
				gameUI.Player1Debug.text = text;
			else
				gameUI.Player2Debug.text = text;
		}
	}
}
